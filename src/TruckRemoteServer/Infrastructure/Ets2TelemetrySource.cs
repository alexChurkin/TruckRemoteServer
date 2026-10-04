using TruckRemoteServer.Data;
using TruckRemoteServer.Telemetry;

namespace TruckRemoteServer.Infrastructure
{
    //Reads the shared memory of the ets2-telemetry-server plugin (all values are false while the game isn't running)
    public sealed class Ets2TelemetrySource : ITelemetrySource
    {
        public TruckTelemetry Read()
        {
            IEts2TelemetryData data = Ets2TelemetryDataReader.Instance.Read();
            IEts2Truck truck = data.Truck;
            return new TruckTelemetry
            {
                EngineOn = truck.EngineOn,
                ParkingBrake = truck.ParkBrakeOn,
                LeftBlinker = truck.BlinkerLeftOn,
                RightBlinker = truck.BlinkerRightOn,
                ParkingLights = truck.LightsParkingOn,
                LowBeam = truck.LightsBeamLowOn,
                HighBeam = truck.LightsBeamHighOn,
                Wipers = truck.WipersOn,
                Beacon = truck.LightsBeaconOn,
                TrailerAttached = data.Trailer1 != null && data.Trailer1.Attached
            };
        }
    }
}
