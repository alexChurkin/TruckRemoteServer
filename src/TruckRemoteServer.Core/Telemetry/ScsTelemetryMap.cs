namespace TruckRemoteServer.Telemetry
{
    //Shared memory of RenCloud's scs-sdk-plugin (https://github.com/RenCloud/scs-sdk-plugin), revision 12.
    //Offsets are taken from scsTelemetryMap_t in scs-telemetry/inc/scs-telemetry-common.hpp (bool is 1 byte)
    public static class ScsTelemetryMap
    {
        public const string NAME = "Local\\SCSTelemetry";
        public const int REVISION = 12;

        //Zone 1
        private const int SDK_ACTIVE = 0;
        //Zone 2: scs_values.telemetry_plugin_revision
        private const int PLUGIN_REVISION = 40;
        //Zone 5 starts at 1500 with config_b (64 wheel flags, isCargoLoaded, specialJob), truck_b follows
        private const int TRUCK_BOOLS = 1566;
        private const int PARKING_BRAKE = TRUCK_BOOLS + 0;
        private const int ENGINE_ENABLED = TRUCK_BOOLS + 10;
        private const int WIPERS = TRUCK_BOOLS + 11;
        //"On" is the blinker lamp that flashes, "Active" (offsets 12, 13) is the switch
        private const int BLINKER_LEFT_ON = TRUCK_BOOLS + 14;
        private const int BLINKER_RIGHT_ON = TRUCK_BOOLS + 15;
        private const int LIGHTS_PARKING = TRUCK_BOOLS + 16;
        private const int LIGHTS_BEAM_LOW = TRUCK_BOOLS + 17;
        private const int LIGHTS_BEAM_HIGH = TRUCK_BOOLS + 18;
        private const int LIGHTS_BEACON = TRUCK_BOOLS + 19;
        //Zone 14 starts at 6000 with trailers; trailer[0]: con_b (64 wheel flags), com_b.wheelOnGround[16], com_b.attached
        private const int TRAILER_ATTACHED = 6080;

        //Only the beginning of the map is read (the whole map is 32 KB)
        public const int READ_SIZE = TRAILER_ATTACHED + 1;

        //Returns Unknown while the game isn't running or another plugin version writes the map
        public static TruckTelemetry Parse(byte[] data)
        {
            if (data == null || data.Length < READ_SIZE) return TruckTelemetry.Unknown;
            if (data[SDK_ACTIVE] == 0 || ReadInt(data, PLUGIN_REVISION) != REVISION) return TruckTelemetry.Unknown;

            return new TruckTelemetry
            {
                EngineOn = data[ENGINE_ENABLED] != 0,
                ParkingBrake = data[PARKING_BRAKE] != 0,
                LeftBlinker = data[BLINKER_LEFT_ON] != 0,
                RightBlinker = data[BLINKER_RIGHT_ON] != 0,
                ParkingLights = data[LIGHTS_PARKING] != 0,
                LowBeam = data[LIGHTS_BEAM_LOW] != 0,
                HighBeam = data[LIGHTS_BEAM_HIGH] != 0,
                Wipers = data[WIPERS] != 0,
                Beacon = data[LIGHTS_BEACON] != 0,
                TrailerAttached = data[TRAILER_ATTACHED] != 0
            };
        }

        //Little-endian, as on x86/x64
        private static int ReadInt(byte[] data, int offset)
        {
            return data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;
        }
    }
}
