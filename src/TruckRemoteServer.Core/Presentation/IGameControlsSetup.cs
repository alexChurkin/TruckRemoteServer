namespace TruckRemoteServer.Presentation
{
    //Controls of the games: a profile without a joystick gets vJoy with its axes, actions without a default key get
    //the keys the server presses
    public interface IGameControlsSetup
    {
        //Sets up the profiles of ETS2 and ATS, also the ones created later. A running game writes
        //its bindings on exit, so its profiles are changed when it's closed
        void Apply();
    }
}
