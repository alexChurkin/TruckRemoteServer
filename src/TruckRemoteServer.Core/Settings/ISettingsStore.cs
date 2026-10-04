namespace TruckRemoteServer.Settings
{
    //User settings of the server, kept between starts
    public interface ISettingsStore
    {
        int Port { get; set; }

        //1..100, 50 by default
        int Sensitivity { get; set; }

        //The firewall question is asked once, later only the link is shown
        bool FirewallPromptShown { get; set; }

        void Save();
    }
}
