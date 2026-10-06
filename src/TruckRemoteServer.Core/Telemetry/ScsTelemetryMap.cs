using System;
using System.Text;

namespace TruckRemoteServer.Telemetry
{
    //Shared memory of RenCloud's scs-sdk-plugin (https://github.com/RenCloud/scs-sdk-plugin), revision 12.
    //Offsets are taken from scsTelemetryMap_t in scs-telemetry/inc/scs-telemetry-common.hpp (bool is 1 byte)
    public static class ScsTelemetryMap
    {
        public const string MapName = "Local\\SCSTelemetry";
        public const int Revision = 12;

        //Zone 1: sdkActive, paused, then timestamps (u64): time, simulatedTime (microseconds)
        private const int SdkActiveOffset = 0;
        private const int PausedOffset = 4;
        private const int SimulatedTimeOffset = 16;
        //Zone 2: scs_values.telemetry_plugin_revision and game (1 - ETS2, 2 - ATS)
        private const int PluginRevisionOffset = 40;
        private const int GameOffset = 52;
        //common_ui.time_abs and config_ui.time_abs_delivery: game minutes
        private const int GameTimeOffset = 64;
        private const int DeliveryTimeOffset = 88;
        //config_ui.retarderStepCount, truck_ui.retarderBrake
        private const int RetarderStepCountOffset = 76;
        private const int RetarderLevelOffset = 108;
        //config_ui.truckWheelCount, truck_ui.truck_wheelSubstance[16] (indexes of the substances of zone 13)
        private const int WheelCountOffset = 80;
        private const int WheelSubstanceOffset = 120;
        private const int MaxWheels = 16;
        //Zone 3: common_i.restStop, truck_i.gearDashboard
        private const int RestStopOffset = 500;
        private const int GearboxGearOffset = 504;
        private const int GearOffset = 508;
        //Zone 4: config_f and truck_f (floats)
        private const int FuelCapacityOffset = 704;
        private const int EngineRpmMaxOffset = 740;
        private const int SpeedOffset = 948;
        private const int EngineRpmOffset = 952;
        private const int CruiseSpeedOffset = 988;
        private const int FuelOffset = 1000;
        private const int FuelRangeOffset = 1008;
        //truck_f.wearEngine, wearTransmission, wearCabin, wearChassis, wearWheels
        private const int TruckWearOffset = 1036;
        private const int TruckWearCount = 5;
        private const int RouteDistanceOffset = 1060;
        private const int RouteTimeOffset = 1064;
        private const int SpeedLimitOffset = 1068;
        private const int SuspensionDeflectionOffset = 1072;
        //Zone 5 starts at 1500 with config_b (64 wheel flags, isCargoLoaded, specialJob), truck_b follows
        private const int TruckBoolsOffset = 1566;
        private const int ParkingBrakeOffset = TruckBoolsOffset + 0;
        private const int MotorBrakeOffset = TruckBoolsOffset + 1;
        private const int AirPressureWarningOffset = TruckBoolsOffset + 2;
        private const int AirPressureEmergencyOffset = TruckBoolsOffset + 3;
        private const int FuelWarningOffset = TruckBoolsOffset + 4;
        private const int AdBlueWarningOffset = TruckBoolsOffset + 5;
        private const int OilPressureWarningOffset = TruckBoolsOffset + 6;
        private const int WaterTemperatureWarningOffset = TruckBoolsOffset + 7;
        private const int BatteryVoltageWarningOffset = TruckBoolsOffset + 8;
        private const int EngineEnabledOffset = TruckBoolsOffset + 10;
        private const int WipersOffset = TruckBoolsOffset + 11;
        //"On" is the blinker lamp that flashes, "Active" (offsets 12, 13) is the switch
        private const int BlinkerLeftOnOffset = TruckBoolsOffset + 14;
        private const int BlinkerRightOnOffset = TruckBoolsOffset + 15;
        private const int LightsParkingOffset = TruckBoolsOffset + 16;
        private const int LightsBeamLowOffset = TruckBoolsOffset + 17;
        private const int LightsBeamHighOffset = TruckBoolsOffset + 18;
        private const int LightsBeaconOffset = TruckBoolsOffset + 19;
        //After cruiseControl (23), wheelOnGround[16] and shifterToggle[2]
        private const int DifferentialLockOffset = TruckBoolsOffset + 42;
        private const int LiftAxleOffset = TruckBoolsOffset + 43;
        private const int TrailerLiftAxleOffset = TruckBoolsOffset + 45;
        //truck_b.truck_wheelOnGround[16] follows cruiseControl
        private const int WheelOnGroundOffset = TruckBoolsOffset + 24;
        //Zone 6 starts at 1640 with config_fv (57 floats); truck_fv: lv_acceleration (velocity), av_acceleration,
        //then accelerationX/Y/Z - the linear acceleration
        private const int AccelerationOffset = 1892;
        //Zone 9 starts at 2300 with config_s strings of 64 bytes (UTF-8, null-terminated):
        //truckBrandId, truckBrand, truckId, truckName, cargoId, cargo, cityDstId, cityDst
        private const int StringSize = 64;
        private const int CargoOffset = 2300 + 5 * StringSize;
        private const int DestinationCityOffset = 2300 + 7 * StringSize;
        //Zone 12: special_b events (onJob, jobFinished, jobCancelled, jobDelivered, fined, tollgate, ferry, train);
        //the plugin flips an event's value every time it happens
        private const int JobDeliveredOffset = 4303;
        private const int FinedOffset = 4304;
        private const int TollgateOffset = 4305;
        private const int FerryOffset = 4306;
        private const int TrainOffset = 4307;
        //Zone 13: names of the substances (surfaces), 25 strings
        private const int SubstancesOffset = 4400;
        private const int SubstanceCount = 25;
        //Zone 14 starts at 6000 with trailers; trailer[0]: con_b (64 wheel flags), com_b.wheelOnGround[16], com_b.attached
        private const int TrailerAttachedOffset = 6080;
        //then buffer_b[3], com_ui.wheelSubstance[16], con_ui.wheelCount; com_f at 6152: cargoDamage, wearChassis,
        //wearWheels, wearBody (the cargo isn't a part of the trailer's wear)
        private const int TrailerWearOffset = 6156;
        private const int TrailerWearCount = 3;

        //Only the beginning of the map is read (the whole map is 32 KB)
        public const int ReadSize = TrailerWearOffset + TrailerWearCount * 4;

        //Returns Unknown while the game isn't running or another plugin version writes the map
        public static TruckTelemetry Parse(byte[] data)
        {
            if (data == null || data.Length < ReadSize) return TruckTelemetry.Unknown;
            if (data[SdkActiveOffset] == 0 || ReadInt(data, PluginRevisionOffset) != Revision) return TruckTelemetry.Unknown;

            bool trailerAttached = data[TrailerAttachedOffset] != 0;
            float wear = MaxFloat(data, TruckWearOffset, TruckWearCount);
            if (trailerAttached) wear = Math.Max(wear, MaxFloat(data, TrailerWearOffset, TrailerWearCount));
            float damage = SumFloats(data, TruckWearOffset, TruckWearCount);
            if (trailerAttached) damage += SumFloats(data, TrailerWearOffset, TrailerWearCount);

            int wheels = Math.Max(0, Math.Min(MaxWheels, ReadInt(data, WheelCountOffset)));
            var deflection = new float[wheels];
            var onGround = new bool[wheels];
            var roughness = new float[wheels];
            bool rumbleStrip = false;
            for (int i = 0; i < wheels; i++)
            {
                deflection[i] = ReadFloat(data, SuspensionDeflectionOffset + i * 4);
                onGround[i] = data[WheelOnGroundOffset + i] != 0;
                int substance = ReadInt(data, WheelSubstanceOffset + i * 4);
                string name = substance >= 0 && substance < SubstanceCount
                    ? ReadString(data, SubstancesOffset + substance * StringSize)
                    : "";
                roughness[i] = SurfaceRoughness.Of(name);
                rumbleStrip |= onGround[i] && SurfaceRoughness.IsRumbleStrip(name);
            }

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
                FuelRange = ReadFloat(data, FuelRangeOffset),
                EngineOn = data[EngineEnabledOffset] != 0,
                ParkingBrake = data[ParkingBrakeOffset] != 0,
                LeftBlinker = data[BlinkerLeftOnOffset] != 0,
                RightBlinker = data[BlinkerRightOnOffset] != 0,
                ParkingLights = data[LightsParkingOffset] != 0,
                LowBeam = data[LightsBeamLowOffset] != 0,
                HighBeam = data[LightsBeamHighOffset] != 0,
                Wipers = data[WipersOffset] != 0,
                Beacon = data[LightsBeaconOffset] != 0,
                TrailerAttached = trailerAttached,
                AirPressureWarning = data[AirPressureWarningOffset] != 0,
                AirPressureEmergency = data[AirPressureEmergencyOffset] != 0,
                OilPressureWarning = data[OilPressureWarningOffset] != 0,
                WaterTemperatureWarning = data[WaterTemperatureWarningOffset] != 0,
                BatteryVoltageWarning = data[BatteryVoltageWarningOffset] != 0,
                AdBlueWarning = data[AdBlueWarningOffset] != 0,
                FuelWarning = data[FuelWarningOffset] != 0,
                DifferentialLock = data[DifferentialLockOffset] != 0,
                LiftAxle = data[LiftAxleOffset] != 0 || (trailerAttached && data[TrailerLiftAxleOffset] != 0),
                EngineBrake = data[MotorBrakeOffset] != 0,
                RetarderLevel = ReadInt(data, RetarderLevelOffset),
                RetarderStepCount = ReadInt(data, RetarderStepCountOffset),
                Wear = wear,
                RestStopMinutes = ReadInt(data, RestStopOffset),
                RouteDistance = ReadFloat(data, RouteDistanceOffset),
                RouteTime = ReadFloat(data, RouteTimeOffset),
                Cargo = ReadString(data, CargoOffset),
                DestinationCity = ReadString(data, DestinationCityOffset),
                DeliveryMinutesLeft = ReadInt(data, DeliveryTimeOffset) - ReadInt(data, GameTimeOffset),
                Paused = data[PausedOffset] != 0,
                SimulationTime = ReadLong(data, SimulatedTimeOffset),
                AccelerationX = ReadFloat(data, AccelerationOffset),
                AccelerationY = ReadFloat(data, AccelerationOffset + 4),
                AccelerationZ = ReadFloat(data, AccelerationOffset + 8),
                GearboxGear = ReadInt(data, GearboxGearOffset),
                Damage = damage,
                SuspensionDeflection = deflection,
                WheelOnGround = onGround,
                WheelSurfaceRoughness = roughness,
                OnRumbleStrip = rumbleStrip,
                FinedToggle = data[FinedOffset] != 0,
                //Tollgates, ferries and trains are all payments
                PaidToggle = (data[TollgateOffset] ^ data[FerryOffset] ^ data[TrainOffset]) != 0,
                JobDeliveredToggle = data[JobDeliveredOffset] != 0
            };
        }

        private static string ReadString(byte[] data, int offset)
        {
            int length = 0;
            while (length < StringSize && data[offset + length] != 0) length++;
            return Encoding.UTF8.GetString(data, offset, length);
        }

        private static float SumFloats(byte[] data, int offset, int count)
        {
            float sum = 0;
            for (int i = 0; i < count; i++) sum += ReadFloat(data, offset + i * 4);
            return sum;
        }

        private static long ReadLong(byte[] data, int offset)
        {
            return (long)((uint)ReadInt(data, offset) | (ulong)(uint)ReadInt(data, offset + 4) << 32);
        }

        private static float MaxFloat(byte[] data, int offset, int count)
        {
            float max = 0;
            for (int i = 0; i < count; i++) max = Math.Max(max, ReadFloat(data, offset + i * 4));
            return max;
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
