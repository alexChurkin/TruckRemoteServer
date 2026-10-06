namespace TruckRemoteServer.Settings
{
    //User settings of the server, kept between starts
    public interface ISettingsStore
    {
        int Port { get; set; }

        //The firewall question is asked once, later only the link is shown
        bool FirewallPromptShown { get; set; }

        //The vJoy setup is offered once, later only the button is shown
        bool JoystickPromptShown { get; set; }

        //The setup wizard is shown on the first start, later it is opened from the Settings menu
        bool SetupWizardShown { get; set; }

        //"" - the language of Windows, otherwise a language code ("en", "ru", "be", "uk")
        string Language { get; set; }

        //The minimized window is hidden to the notification area instead of staying on the taskbar
        bool MinimizeToTray { get; set; }

        void Save();
    }
}
