namespace TruckRemoteServer.Settings
{
    //The server starts with Windows (minimized). Its state is kept by Windows itself, not in the settings:
    //it may be turned off there too (e.g. in the Task Manager)
    public interface IAutostart
    {
        bool IsEnabled { get; }

        //Also updates the path of an enabled autostart (the program may have been moved)
        void SetEnabled(bool enabled);
    }
}
