namespace TruckRemoteServer.Presentation
{
    //Key bindings of the games: actions without a default key get the keys the server presses
    public interface IGameControlsSetup
    {
        //Adds the missing keys to the profiles of ETS2 and ATS. A running game writes its bindings on exit,
        //so its profiles are changed when it's closed
        void Apply();
    }
}
