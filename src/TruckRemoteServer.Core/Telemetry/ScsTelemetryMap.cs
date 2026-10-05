using System;

namespace TruckRemoteServer.Telemetry
{
    //Shared memory of RenCloud's scs-sdk-plugin (https://github.com/RenCloud/scs-sdk-plugin), revision 12.
    //Offsets are taken from scsTelemetryMap_t in scs-telemetry/inc/scs-telemetry-common.hpp (bool is 1 byte)
    public static class ScsTelemetryMap
    {
        public const string MapName = "Local\\SCSTelemetry";
        public const int Revision = 12;

        //Zone 1
        private const int SdkActiveOffset = 0;
        //Zone 2: scs_values.telemetry_plugin_revision and game (1 - ETS2, 2 - ATS)
        private const int PluginRevisionOffset = 40;
        private const int GameOffset = 52;
        //Zone 3: truck_i.gearDashboard
        private const int GearOffset = 508;
        //Zone 4: config_f and truck_f (floats)
        private const int FuelCapacityOffset = 704;
        private const int EngineRpmMaxOffset = 740;
        private const int SpeedOffset = 948;
        private const int EngineRpmOffset = 952;
        private const int CruiseSpeedOffset = 988;
        private const int FuelOffset = 1000;
        private const int SpeedLimitOffset = 1068;
        //Zone 5 starts at 1500 with config_b (64 wheel flags, isCargoLoaded, specialJob), truck_b follows
        private const int TruckBoolsOffset = 1566;
        private const int ParkingBrakeOffset = TruckBoolsOffset + 0;
        private const int EngineEnabledOffset = TruckBoolsOffset + 10;
        private const int WipersOffset = TruckBoolsOffset + 11;
        //"On" is the blinker lamp that flashes, "Active" (offsets 12, 13) is the switch
        private const int BlinkerLeftOnOffset = TruckBoolsOffset + 14;
        private const int BlinkerRightOnOffset = TruckBoolsOffset + 15;
        private const int LightsParkingOffset = TruckBoolsOffset + 16;
        private const int LightsBeamLowOffset = TruckBoolsOffset + 17;
        private const int LightsBeamHighOffset = TruckBoolsOffset + 18;
        private const int LightsBeaconOffset = TruckBoolsOffset + 19;
        //Zone 14 starts at 6000 with trailers; trailer[0]: con_b (64 wheel flags), com_b.wheelOnGround[16], com_b.attached
        private const int TrailerAttachedOffset = 6080;

        //Only the beginning of the map is read (the whole map is 32 KB)
        public const int ReadSize = TrailerAttachedOffset + 1;

        //Returns Unknown while the game isn't running or another plugin version writes the map
        public static TruckTelemetry Parse(byte[] data)
        {
            if (data == null || data.Length < ReadSize) return TruckTelemetry.Unknown;
            if (data[SdkActiveOffset] == 0 || ReadInt(data, PluginRevisionOffset) != Revision) return TruckTelemetry.Unknown;

            return new TruckTelemetry
            {
                Available = true,
                Game = ReadInt(data, GameOffset),
                Speed = ReadFloat(data, SpeedOffset),
                SpeedLimit = ReadFloat(data, SpeedLimitOffset),
                CruiseSpeed = ReadFloat(data, CruiseSpeedOffset),
                Gear = ReadInt(data, GearOffset),
                EngineRpm = ReadFloat(data, EngineRpmOffset),
                EngineRpmMax = ReadFloat(data, EngineRpmMaxOffset),
                Fuel = ReadFloat(data, FuelOffset),
                FuelCapacity = ReadFloat(data, FuelCapacityOffset),
                EngineOn = data[EngineEnabledOffset] != 0,
                ParkingBrake = data[ParkingBrakeOffset] != 0,
                LeftBlinker = data[BlinkerLeftOnOffset] != 0,
                RightBlinker = data[BlinkerRightOnOffset] != 0,
                ParkingLights = data[LightsParkingOffset] != 0,
                LowBeam = data[LightsBeamLowOffset] != 0,
                HighBeam = data[LightsBeamHighOffset] != 0,
                Wipers = data[WipersOffset] != 0,
                Beacon = data[LightsBeaconOffset] != 0,
                TrailerAttached = data[TrailerAttachedOffset] != 0
            };
        }

        //Little-endian, as on x86/x64
        private static int ReadInt(byte[] data, int offset)
        {
            return data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;
        }

        private static float ReadFloat(byte[] data, int offset)
        {
            float value = BitConverter.ToSingle(BitConverter.IsLittleEndian
                ? data
                : new[] { data[offset + 3], data[offset + 2], data[offset + 1], data[offset] },
                BitConverter.IsLittleEndian ? offset : 0);
            return float.IsNaN(value) || float.IsInfinity(value) ? 0 : value;
        }
    }
}
