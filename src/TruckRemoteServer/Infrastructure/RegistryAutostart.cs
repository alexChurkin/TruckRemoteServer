using Microsoft.Win32;
using TruckRemoteServer.Settings;

namespace TruckRemoteServer.Infrastructure
{
    //The Run key of the current user: no administrator rights are needed
    public sealed class RegistryAutostart : IAutostart
    {
        //The server started by Windows starts minimized
        public const string Argument = "--autostart";

        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "TruckRemoteServer";

        private readonly string programPath;

        public RegistryAutostart(string programPath)
        {
            this.programPath = programPath;
        }

        public bool IsEnabled
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    return key?.GetValue(ValueName) != null;
                }
            }
        }

        public void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled) key.SetValue(ValueName, $"\"{programPath}\" {Argument}");
                else key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
    }
}
