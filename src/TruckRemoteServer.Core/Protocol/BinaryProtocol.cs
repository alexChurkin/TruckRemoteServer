using System;
using System.Collections.Generic;

namespace TruckRemoteServer.Protocol
{
    //Compact binary protocol (version 2), used when the controller's hello is "TruckRemoteHello2"
    //and the server answers "Hi!2"; older controllers and servers keep the text protocol.
    //All numbers are little-endian. A binary message starts with its type (a byte below 0x20,
    //text messages start with a printable character).
    //
    //Controller state (16 bytes + 2 per action):
    //  type 0x02 | sequence u32 | steering f32 (m/s², as measured) | flags u16 | gas u16 | brake u16 |
    //  action count u8 | (action id u8, value u8) * count
    //  flags: 0 brake, 1 gas, 2 left signal, 3 right signal, 4 emergency, 5 parking brake, 6 lights,
    //  7 cruise (clicks are toggles), 8 pedal levels present, 9-10 horn (0 off, 1 horn, 2 air horn).
    //  Pedal levels are 0..65535 (finer than the 15-bit joystick axes). Actions: see ControllerActions,
    //  a click counter (mod 256) or 1 while a hold action is held; only clicked and held actions are sent.
    //Paused controller: type 0x03. Goodbye: type 0x04.
    //
    //Server state (9 bytes):
    //  type 0x02 | sequence u32 | flags u16 | force feedback duration u16 (ms)
    //  flags: 0 engine, 1 parking brake, 2 left blinker, 3 right blinker, 4 trailer attached, 5 wipers,
    //  6 beacon, 7 analog pedals available, 8-9 lights mode (see ServerMessage)
    public static class BinaryProtocol
    {
        public const int Version = 2;

        public const byte StateType = 0x02;
        public const byte PausedType = 0x03;
        public const byte GoodbyeType = 0x04;

        private const int ControllerHeaderSize = 16;
        private const int ServerStateSize = 9;
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

        public static byte[] FormatServerState(bool engineOn, bool parkingBrake, bool leftBlinker, bool rightBlinker,
            int lightsMode, int ffbDuration,
            bool trailerAttached, bool wipersOn, bool beaconOn, bool analogPedalsAvailable, long sequence)
        {
            int flags = Flag(engineOn, 0) | Flag(parkingBrake, 1) | Flag(leftBlinker, 2) | Flag(rightBlinker, 3)
                | Flag(trailerAttached, 4) | Flag(wipersOn, 5) | Flag(beaconOn, 6) | Flag(analogPedalsAvailable, 7)
                | ((lightsMode & 0x3) << 8);
            var message = new byte[ServerStateSize];
            message[0] = StateType;
            WriteUInt32(message, 1, (uint)sequence);
            WriteUInt16(message, 5, flags);
            WriteUInt16(message, 7, Math.Max(0, Math.Min(ffbDuration, ushort.MaxValue)));
            return message;
        }

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
