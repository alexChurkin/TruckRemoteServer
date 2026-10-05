namespace TruckRemoteServer.Telemetry
{
    //The truck state read from the game (all false when the game or the telemetry plugin isn't running)
    public class TruckTelemetry
    {
        public static readonly TruckTelemetry Unknown = new TruckTelemetry();

        public bool EngineOn { get; set; }
        public bool ParkingBrake { get; set; }
        public bool LeftBlinker { get; set; }
        public bool RightBlinker { get; set; }
        public bool ParkingLights { get; set; }
        public bool LowBeam { get; set; }
        public bool HighBeam { get; set; }
        public bool Wipers { get; set; }
        public bool Beacon { get; set; }
        public bool TrailerAttached { get; set; }
    }
}
