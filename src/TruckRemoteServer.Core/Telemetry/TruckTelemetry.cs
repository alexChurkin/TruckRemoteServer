namespace TruckRemoteServer.Telemetry
{
    //The truck state read from the game (all false/0 when the game or the telemetry plugin isn't running)
    public class TruckTelemetry
    {
        public const int GameUnknown = 0;
        public const int GameEts2 = 1;
        public const int GameAts = 2;

        public static readonly TruckTelemetry Unknown = new TruckTelemetry();

        //The game and the plugin are running: the values below are real
        public bool Available { get; set; }
        //GameEts2 (km/h) or GameAts (mph)
        public int Game { get; set; }

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

        //m/s, negative when reversing
        public float Speed { get; set; }
        //m/s, 0 - no limit
        public float SpeedLimit { get; set; }
        //m/s, 0 - the cruise control is off
        public float CruiseSpeed { get; set; }
        //The gear on the dashboard: negative - reverse, 0 - neutral
        public int Gear { get; set; }
        public float EngineRpm { get; set; }
        public float EngineRpmMax { get; set; }
        //Liters
        public float Fuel { get; set; }
        public float FuelCapacity { get; set; }
    }
}
