namespace TruckRemoteServer.Haptics
{
    //Events the driver feels; their ids are sent to the phone (see BinaryProtocol), which plays a pattern for each
    public enum HapticEvent
    {
        Collision = 1,
        //A pothole, a kerb, a hard landing of a wheel
        Bump = 2,
        GearShift = 3,
        //The relay of the blinkers: on and off
        Blinker = 4,
        TrailerCoupled = 5,
        TrailerUncoupled = 6,
        EngineStart = 7,
        EngineStop = 8,
        ParkingBrake = 9,
        //A step of the retarder or the engine brake turned on or off
        Retarder = 10,
        //A warning lamp of the dashboard has come on
        Warning = 11,
        Fine = 12,
        //A tollgate, a ferry or a train was paid
        Payment = 13,
        JobDelivered = 14,
    }

    //Surface under the wheels
    public enum HapticSurface
    {
        Road = 0,
        Offroad = 1,
        RumbleStrip = 2,
    }
}
