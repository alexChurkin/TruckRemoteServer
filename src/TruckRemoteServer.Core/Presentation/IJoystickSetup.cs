namespace TruckRemoteServer.Presentation
{
    //vJoy driver and its device 1: the phone steers through it
    public interface IJoystickSetup
    {
        //The driver isn't installed or enabled, or device 1 doesn't exist or has no steering axis.
        //A device used by another program doesn't need setup
        bool NeedsSetup();

        //Installs the driver if needed and configures device 1, asks for administrator rights.
        //Returns false if the user refused them (nothing has changed); the result is checked by NeedsSetup
        bool Setup();
    }
}
