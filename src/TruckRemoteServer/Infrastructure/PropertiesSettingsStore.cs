using System;
using System.Configuration;
using System.IO;
using TruckRemoteServer.Settings;

namespace TruckRemoteServer.Infrastructure
{
    //Application settings of .NET Framework (user.config in the user's AppData)
    public sealed class PropertiesSettingsStore : ISettingsStore
    {
        private static Properties.Settings Values => Properties.Settings.Default;

        public int Port
        {
            get => (int)Values.Port;
            set => Values.Port = value;
        }

        public bool FirewallPromptShown
        {
            get => Values.FirewallPromptShown;
            set => Values.FirewallPromptShown = value;
        }

        public bool JoystickPromptShown
        {
            get => Values.JoystickPromptShown;
            set => Values.JoystickPromptShown = value;
        }

        public bool SetupWizardShown
        {
            get => Values.SetupWizardShown;
            set => Values.SetupWizardShown = value;
        }

        public string Language
        {
            get => Values.Language ?? "";
            set => Values.Language = value ?? "";
        }

        public bool MinimizeToTray
        {
            get => Values.MinimizeToTray;
            set => Values.MinimizeToTray = value;
        }

        //The settings file may be locked (a policy, a read-only profile): the value then lives until the program ends
        public void Save()
        {
            try
            {
                Values.Save();
            }
            catch (Exception e) when (e is ConfigurationErrorsException || e is IOException
                || e is UnauthorizedAccessException)
            {
                //The setting is already applied in memory: only saving it failed, and that must not stop the program
            }
        }
    }
}
