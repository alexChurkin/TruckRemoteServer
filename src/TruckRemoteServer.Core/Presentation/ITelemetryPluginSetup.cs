namespace TruckRemoteServer.Presentation
{
    //Telemetry plugin of the game (shows the truck state on the phone, not needed for controls)
    public interface ITelemetryPluginSetup
    {
        bool IsInstalled { get; }

        //May ask the user for the game folders; throws if the plugin can't be installed
        void Install();
    }
}
