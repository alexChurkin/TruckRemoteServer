using System;
using System.Collections.Generic;
using System.Text;
using TruckRemoteServer.Haptics;
using TruckRemoteServer.Telemetry;

namespace TruckRemoteServer.Protocol
{
    //Compact binary protocol (version 2), used when the controller's hello is "TruckRemoteHello2"
    //and the server answers "Hi!2"; older controllers and servers keep the text protocol.
    //All numbers are little-endian. A binary message starts with its type (a byte below 0x20,
    //text messages start with a printable character).
    //
    //Controller state (16 bytes + 2 per action):
    //  type 0x02 | sequence u32 | steering f32 (m/s², filtered and after the phone's steering settings: ±9.80665 is the full lock) |
    //  flags u16 | gas u16 | brake u16 |
    //  action count u8 | (action id u8, value u8) * count
    //  flags: 0 brake, 1 gas, 2 left signal, 3 right signal, 4 emergency, 5 parking brake, 6 lights,
    //  7 cruise (clicks are toggles), 8 pedal levels present, 9-10 horn (0 off, 1 horn, 2 air horn).
    //  Pedal levels are 0..65535 (finer than the 15-bit joystick axes). Actions: see ControllerActions,
    //  a click counter (mod 256) or 1 while a hold action is held; only clicked and held actions are sent.
    //Paused controller: type 0x03. Goodbye: type 0x04.
    //
    //Server state (43 bytes and 3 per haptic event; older controllers read the first 22 or 40):
    //  type 0x02 | sequence u32 | flags u16 | force feedback duration u16 (ms) |
    //  speed i16 (cm/s, negative when reversing) | speed limit u16 (cm/s, 0 - none) |
    //  cruise speed u16 (cm/s, 0 - off) | gear i8 (negative - reverse; see flags2 12-13) | engine rpm u16 | max rpm u16 |
    //  fuel u8 (percent of the tank) | game u8 (1 - ETS2, 2 - ATS) |
    //  flags2 u16 | retarder level u8 | retarder steps u8 (0 - no retarder) | wear u8 (percent, the most worn part) |
    //  rest stop i16 (game minutes until the driver must rest) | route distance u32 (m) | route time u32 (s) |
    //  server revision u8 (see Revision; a state of 22 bytes is revision 1, of 37 bytes - 2) |
    //  fuel range u16 (km, revision 4+) |
    //  haptics (revision 6+): road vibration u8 (0..255) | surface u8 (see HapticSurface) |
    //  event count u8 | (event id u8, counter u8, strength u8 0..255) * count - all events (see HapticEvent),
    //  an event happened when its counter has changed; the force feedback duration of vJoy is still sent for older
    //  controllers, newer ones play the haptics instead while the telemetry is available
    //  flags: 0 engine, 1 parking brake, 2 left blinker, 3 right blinker, 4 trailer attached, 5 wipers,
    //  6 beacon, 7 analog pedals available, 8-9 lights mode (see ServerMessage),
    //  10 telemetry available (the dashboard values are real)
    //  flags2: 0 air pressure warning, 1 air pressure emergency, 2 oil pressure warning, 3 water temperature warning,
    //  4 battery voltage warning, 5 AdBlue warning, 6 fuel warning, 7 differential lock, 8 lift axle, 9 engine brake,
    //  10 the speed units of the game are known, 11 they are miles per hour (km/h otherwise),
    //  12 the gear is a crawler gear (the game shows C1, C2: the gear field is its number among the crawler gears),
    //  13 the gear is the one the game names "OD". The gear field is counted after the crawler gears, as the game
    //  names it, so the controllers that don't know these flags show the same number as the game
    //
    //Viewers (a dashboard on a tablet or another phone) send the text hello "TruckRemoteViewer2" once a second, get
    //"Hi!2", the server state (without haptics) 20 times per second and the job once a second; type 0x04 is their goodbye.
    //
    //Job (sent once a second): type 0x05 | delivery minutes left i32 (game time, negative when late) |
    //  cargo length u8 | cargo UTF-8 | destination city length u8 | destination city UTF-8; no cargo - no job
    public static class BinaryProtocol
    {
        public const int Version = 2;

        public const byte StateType = 0x02;
        public const byte PausedType = 0x03;
        public const byte GoodbyeType = 0x04;
        public const byte JobType = 0x05;

        //What the server sends: 3 - the job messages and this byte, 4 - the fuel range;
        //5 - no new data, the actions 24-41 of the panel are known (see ControllerActions); 6 - haptics;
        //7 - no new data, the actions 42 and 43 are known (the held light horn and "activate"),
        //the speed units of the game are in flags2
        public const byte Revision = 7;

        private const int ControllerHeaderSize = 16;
        private const int ServerStateBaseSize = 40;
        private const int HapticsHeaderSize = 3;
        private const int HapticEventSize = 3;
        private const int MaxJobTextBytes = 64;
        private const double CentimetersInMeter = 100;
        private const double LevelScale = ushort.MaxValue;

        public static bool IsBinary(byte[] data, int length) => length > 0 && data[0] < 0x20;

        //Returns null if the message is malformed
        public static ControllerMessage ParseControllerState(byte[] data, int length)
        {
            if (length < ControllerHeaderSize || data[0] != StateType) return null;
            int count = data[15];
            if (length < ControllerHeaderSize + count * 2) return null;

            int flags = ReadUInt16(data, 9);
            var actions = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                int offset = ControllerHeaderSize + i * 2;
                actions[data[offset]] = data[offset + 1];
            }
            float steering = BitConverter.ToSingle(LittleEndian(data, 5, 4), 0);
            return new ControllerMessage
            {
                Sequence = ReadUInt32(data, 1),
                Steering = float.IsNaN(steering) || float.IsInfinity(steering) ? 0 : steering,
                SteeringIsFinal = true,
                BrakePressed = Bit(flags, 0),
                GasPressed = Bit(flags, 1),
                LeftSignalClick = Bit(flags, 2),
                RightSignalClick = Bit(flags, 3),
                EmergencyClick = Bit(flags, 4),
                ParkingBrakeClick = Bit(flags, 5),
                LightsClick = Bit(flags, 6),
                CruiseClick = Bit(flags, 7),
                HasPedalLevels = Bit(flags, 8),
                Horn = (flags >> 9) & 0x3,
                GasLevel = ReadUInt16(data, 11) / LevelScale,
                BrakeLevel = ReadUInt16(data, 13) / LevelScale,
                Actions = actions
            };
        }

        public static byte[] FormatServerState(TruckTelemetry truck, int lightsMode, int ffbDuration,
            bool analogPedalsAvailable, long sequence, HapticDetector haptics = null)
        {
            int flags = Flag(truck.EngineOn, 0) | Flag(truck.ParkingBrake, 1) | Flag(truck.LeftBlinker, 2)
                | Flag(truck.RightBlinker, 3) | Flag(truck.TrailerAttached, 4) | Flag(truck.Wipers, 5)
                | Flag(truck.Beacon, 6) | Flag(analogPedalsAvailable, 7) | ((lightsMode & 0x3) << 8)
                | Flag(truck.Available, 10);
            int events = haptics == null ? 0 : HapticDetector.EventCount;
            int hapticsSize = haptics == null ? 0 : HapticsHeaderSize + events * HapticEventSize;
            var message = new byte[ServerStateBaseSize + hapticsSize];
            message[0] = StateType;
            WriteUInt32(message, 1, (uint)sequence);
            WriteUInt16(message, 5, flags);
            WriteUInt16(message, 7, Clamp(ffbDuration, 0, ushort.MaxValue));
            WriteUInt16(message, 9, Clamp(Centimeters(truck.Speed), short.MinValue, short.MaxValue));
            WriteUInt16(message, 11, Clamp(Centimeters(truck.SpeedLimit), 0, ushort.MaxValue));
            WriteUInt16(message, 13, Clamp(Centimeters(truck.CruiseSpeed), 0, ushort.MaxValue));
            //The gear as the game names it, where its name isn't its number (see Gearbox)
            int gear = Gearbox.ShownGear(truck.Gearbox, truck.Gear);
            message[15] = (byte)(sbyte)Clamp(gear, sbyte.MinValue, sbyte.MaxValue);
            WriteUInt16(message, 16, Clamp((int)Math.Round(truck.EngineRpm), 0, ushort.MaxValue));
            WriteUInt16(message, 18, Clamp((int)Math.Round(truck.EngineRpmMax), 0, ushort.MaxValue));
            message[20] = (byte)(truck.FuelCapacity > 0
                ? Clamp((int)Math.Round(truck.Fuel / truck.FuelCapacity * 100), 0, 100)
                : 0);
            message[21] = (byte)Clamp(truck.Game, 0, byte.MaxValue);

            int flags2 = Flag(truck.AirPressureWarning, 0) | Flag(truck.AirPressureEmergency, 1)
                | Flag(truck.OilPressureWarning, 2) | Flag(truck.WaterTemperatureWarning, 3)
                | Flag(truck.BatteryVoltageWarning, 4) | Flag(truck.AdBlueWarning, 5) | Flag(truck.FuelWarning, 6)
                | Flag(truck.DifferentialLock, 7) | Flag(truck.LiftAxle, 8) | Flag(truck.EngineBrake, 9)
                | Flag(truck.SpeedInMph.HasValue, 10) | Flag(truck.SpeedInMph == true, 11)
                | Flag(Gearbox.IsCrawlerGear(truck.Gearbox, truck.Gear), 12)
                | Flag(Gearbox.IsOverdrive(truck.Gearbox, truck.Gear), 13);
            WriteUInt16(message, 22, flags2);
            message[24] = (byte)Clamp(truck.RetarderLevel, 0, byte.MaxValue);
            message[25] = (byte)Clamp(truck.RetarderStepCount, 0, byte.MaxValue);
            message[26] = (byte)Clamp((int)Math.Round(truck.Wear * 100), 0, 100);
            WriteUInt16(message, 27, Clamp(truck.RestStopMinutes, short.MinValue, short.MaxValue));
            WriteUInt32(message, 29, ToUInt32(truck.RouteDistance));
            WriteUInt32(message, 33, ToUInt32(truck.RouteTime));
            message[37] = Revision;
            WriteUInt16(message, 38, (int)Math.Min(ushort.MaxValue, ToUInt32(truck.FuelRange)));

            if (haptics != null)
            {
                message[40] = ToByte(haptics.Road);
                message[41] = (byte)haptics.Surface;
                message[42] = (byte)events;
                for (int i = 0; i < events; i++)
                {
                    var kind = (HapticEvent)(i + 1);
                    int offset = ServerStateBaseSize + HapticsHeaderSize + i * HapticEventSize;
                    message[offset] = (byte)kind;
                    message[offset + 1] = haptics.Counter(kind);
                    message[offset + 2] = ToByte(haptics.Strength(kind));
                }
            }
            return message;
        }

        public static byte[] FormatJob(TruckTelemetry truck)
        {
            bool hasJob = !string.IsNullOrEmpty(truck.Cargo);
            byte[] cargo = hasJob ? Utf8Prefix(truck.Cargo) : Array.Empty<byte>();
            byte[] city = hasJob ? Utf8Prefix(truck.DestinationCity ?? "") : Array.Empty<byte>();
            var message = new byte[7 + cargo.Length + city.Length];
            message[0] = JobType;
            WriteUInt32(message, 1, (uint)(hasJob ? truck.DeliveryMinutesLeft : 0));
            message[5] = (byte)cargo.Length;
            cargo.CopyTo(message, 6);
            message[6 + cargo.Length] = (byte)city.Length;
            city.CopyTo(message, 7 + cargo.Length);
            return message;
        }

        //At most MaxJobTextBytes, not cutting a character in the middle
        private static byte[] Utf8Prefix(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            if (bytes.Length <= MaxJobTextBytes) return bytes;
            int length = MaxJobTextBytes;
            while (length > 0 && (bytes[length] & 0xC0) == 0x80) length--;
            var prefix = new byte[length];
            Array.Copy(bytes, prefix, length);
            return prefix;
        }

        //0..1 as 0..255
        private static byte ToByte(float fraction) => (byte)Clamp((int)Math.Round(fraction * byte.MaxValue), 0, byte.MaxValue);

        private static int Centimeters(float metersPerSecond) => (int)Math.Round(metersPerSecond * CentimetersInMeter);

        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

        //A non-negative rounded value that fits u32 (more than enough for meters and seconds of a route)
        private static uint ToUInt32(float value) => (uint)Math.Round(Math.Max(0, Math.Min(uint.MaxValue, (double)value)));

        private static bool Bit(int flags, int bit) => (flags & (1 << bit)) != 0;

        private static int Flag(bool value, int bit) => value ? 1 << bit : 0;

        private static int ReadUInt16(byte[] data, int offset) => data[offset] | data[offset + 1] << 8;

        private static long ReadUInt32(byte[] data, int offset) =>
            (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);

        private static void WriteUInt16(byte[] data, int offset, int value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] data, int offset, uint value)
        {
            for (int i = 0; i < 4; i++) data[offset + i] = (byte)(value >> (8 * i));
        }

        //BitConverter uses the byte order of the machine
        private static byte[] LittleEndian(byte[] data, int offset, int count)
        {
            var bytes = new byte[count];
            Array.Copy(data, offset, bytes, 0, count);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
        }
    }
}
