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

        //The vJoy setup is offered once, later only the button is shown
        bool JoystickPromptShown { get; set; }

        //"" - the language of Windows, otherwise a language code ("en", "ru", "be", "uk")
        string Language { get; set; }

        void Save();
    }
}
