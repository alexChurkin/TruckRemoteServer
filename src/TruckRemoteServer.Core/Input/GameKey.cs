namespace TruckRemoteServer.Input
{
    //Game controls made by keys (default ETS2/ATS key bindings, see the keyboard implementation)
    public enum GameKey
    {
        Gas,
        Brake,
        LeftBlinker,
        RightBlinker,
        HazardLights,
        ParkingBrake,
        //Parking lights and low beam
        Lights,
        HighBeam,
        Horn,
        AirHorn,
        CruiseControl,
        //Additional actions in the order the controller sends their counters
        Engine,
        Trailer,
        Activate,
        Wipers,
        DiffLock,
        LiftAxle,
        Beacon,
        LightHorn
    }
}
