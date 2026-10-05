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
        //Kilometers the fuel is enough for (estimated by the game)
        public float FuelRange { get; set; }

        //Warning lamps of the dashboard
        public bool AirPressureWarning { get; set; }
        public bool AirPressureEmergency { get; set; }
        public bool OilPressureWarning { get; set; }
        public bool WaterTemperatureWarning { get; set; }
        public bool BatteryVoltageWarning { get; set; }
        public bool AdBlueWarning { get; set; }
        public bool FuelWarning { get; set; }

        public bool DifferentialLock { get; set; }
        //The lift axle of the truck or of the trailer is raised
        public bool LiftAxle { get; set; }
        public bool EngineBrake { get; set; }
        //0 - off, up to RetarderStepCount
        public int RetarderLevel { get; set; }
        //0 - the truck has no retarder
        public int RetarderStepCount { get; set; }
        //The most worn part of the truck and the trailer: 0..1
        public float Wear { get; set; }
        //Game time until the driver must rest, minutes
        public int RestStopMinutes { get; set; }
        //Navigation: the distance (m) and the estimated time (s) to the end of the route, 0 - no route
        public float RouteDistance { get; set; }
        public float RouteTime { get; set; }

        //The current job: the cargo name is empty without a job
        public string Cargo { get; set; } = "";
        public string DestinationCity { get; set; } = "";
        //Game minutes until the delivery deadline, negative when late
        public int DeliveryMinutesLeft { get; set; }
    }
}
