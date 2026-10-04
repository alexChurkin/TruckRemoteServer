namespace TruckRemoteServer.Telemetry
{
    //The truck state read from the game (all false when the game or the telemetry plugin isn't running)
    public class TruckTelemetry
    {
        public static readonly TruckTelemetry Unknown = new TruckTelemetry();

        public bool EngineOn;
        public bool ParkingBrake;
        public bool LeftBlinker;
        public bool RightBlinker;
        public bool ParkingLights;
        public bool LowBeam;
        public bool HighBeam;
        public bool Wipers;
        public bool Beacon;
        public bool TrailerAttached;
    }
}
