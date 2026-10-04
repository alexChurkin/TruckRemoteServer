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

        public int Sensitivity
        {
            get => Values.Sensitivity;
            set => Values.Sensitivity = value;
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

        public string Language
        {
            get => Values.Language ?? "";
            set => Values.Language = value ?? "";
        }

        public void Save()
        {
            Values.Save();
        }
    }
}
