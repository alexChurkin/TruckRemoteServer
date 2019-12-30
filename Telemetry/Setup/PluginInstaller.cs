using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TruckRemoteServer.Setup
{
    public class PluginInstaller
    {
        const string Ets2 = "ETS2";
        const string Ats = "ATS";

        public PluginInstaller()
        {
            try
            {
                Console.WriteLine("Checking plugin DLL files...");

                var ets2State = new GameState(Ets2, Properties.Settings.Default.ETSPath);
                var atsState = new GameState(Ats, Properties.Settings.Default.ATSPath);

                if (ets2State.IsPluginValid() && atsState.IsPluginValid())
                {
                    Status = SetupStatus.Installed;
                }
                else
                {
                    Status = SetupStatus.Uninstalled;
                }
            }
            catch (Exception)
            {
                Status = SetupStatus.Failed;
            }
        }

        public SetupStatus Status { get; private set; }

        public SetupStatus Install(IWin32Window owner)
        {
            if (Status == SetupStatus.Installed)
                return Status;

            try
            {
                var ets2State = new GameState(Ets2, Properties.Settings.Default.ETSPath);
                var atsState = new GameState(Ats, Properties.Settings.Default.ATSPath);

                if (!ets2State.IsPluginValid())
                {
                    ets2State.DetectPath();
                    if (!ets2State.IsPathValid())
                        ets2State.BrowserForValidPath(owner);
                    ets2State.InstallPlugin();
                }

                if (!atsState.IsPluginValid())
                {
                    atsState.DetectPath();
                    if (!atsState.IsPathValid())
                        atsState.BrowserForValidPath(owner);
                    atsState.InstallPlugin();
                }

                Properties.Settings.Default.ETSPath = ets2State.GamePath;
                Properties.Settings.Default.ATSPath = atsState.GamePath;
                Properties.Settings.Default.Save();

                Status = SetupStatus.Installed;
            }
            catch (Exception)
            {
                Status = SetupStatus.Failed;
                throw;
            }

            return Status;
        }

        public SetupStatus Uninstall(IWin32Window owner)
        {
            if (Status == SetupStatus.Uninstalled)
                return Status;

            SetupStatus status;
            try
            {
                var ets2State = new GameState(Ets2, Properties.Settings.Default.ETSPath);
                var atsState = new GameState(Ats, Properties.Settings.Default.ATSPath);
                ets2State.UninstallPlugin();
                atsState.UninstallPlugin();
                status = SetupStatus.Uninstalled;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                status = SetupStatus.Failed;
            }
            return status;
        }

        class GameState
        {
            const string InstallationSkippedPath = "N/A";
            const string TelemetryDllName = "ets2-telemetry-server.dll";
            const string TelemetryX64DllMd5 = "d606f27c94bcae1114d930d8e83b6fa2";

            readonly string _gameName;

            public GameState(string gameName, string gamePath)
            {
                _gameName = gameName;
                GamePath = gamePath;
            }

            string GameDirectoryName {
                get {
                    string fullName = "Euro Truck Simulator 2";
                    if (_gameName == Ats)
                        fullName = "American Truck Simulator";
                    return fullName;
                }
            }

            public string GamePath { get; private set; }

            public bool IsPathValid()
            {
                if (GamePath == InstallationSkippedPath)
                    return true;

                if (string.IsNullOrEmpty(GamePath))
                    return false;

                var baseScsPath = Path.Combine(GamePath, "base.scs");
                var binPath = Path.Combine(GamePath, "bin");
                bool validated = File.Exists(baseScsPath) && Directory.Exists(binPath);
                //Log.InfoFormat("Validating {2} path: '{0}' ... {1}", GamePath, validated ? "OK" : "Fail", _gameName);
                return validated;
            }

            public bool IsPluginValid()
            {
                if (GamePath == InstallationSkippedPath)
                    return true;

                if (!IsPathValid())
                    return false;

                return Md5(GetTelemetryPluginDllFileName(GamePath)) == TelemetryX64DllMd5;
            }

            public void InstallPlugin()
            {
                if (GamePath == InstallationSkippedPath)
                    return;

                string x64DllFileName = GetTelemetryPluginDllFileName(GamePath);

                //Log.InfoFormat("Copying {1} x64 plugin DLL file to: {0}", x64DllFileName, _gameName);
                File.Copy(LocalEts2X64TelemetryPluginDllFileName, x64DllFileName, true);
            }

            public void UninstallPlugin()
            {
                if (GamePath == InstallationSkippedPath)
                    return;

                Console.WriteLine("Backing up plugin DLL files for " + _gameName);
                string x64DllFileName = GetTelemetryPluginDllFileName(GamePath);
                string x64BakFileName = Path.ChangeExtension(x64DllFileName, ".bak");
                if (File.Exists(x64BakFileName))
                    File.Delete(x64BakFileName);
                File.Move(x64DllFileName, x64BakFileName);
            }

            static string GetDefaultSteamPath()
            {
                var steamKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return steamKey?.GetValue("SteamPath") as string;
            }

            static string LocalEts2X64TelemetryPluginDllFileName => Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"Ets2Plugins\win_x64\plugins", TelemetryDllName);

            static string GetPluginPath(string gamePath)
            {
                return Path.Combine(gamePath, @"bin\win_x64\plugins");
            }

            static string GetTelemetryPluginDllFileName(string gamePath)
            {
                string path = GetPluginPath(gamePath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return Path.Combine(path, TelemetryDllName);
            }

            static string Md5(string fileName)
            {
                if (!File.Exists(fileName))
                    return null;
                using (var provider = new MD5CryptoServiceProvider())
                {
                    var bytes = File.ReadAllBytes(fileName);
                    var hash = provider.ComputeHash(bytes);
                    var result = string.Concat(hash.Select(b => $"{b:x02}"));
                    return result;
                }
            }

            public void DetectPath()
            {
                GamePath = GetDefaultSteamPath();
                if (!string.IsNullOrEmpty(GamePath))
                    GamePath = Path.Combine(
                        GamePath.Replace('/', '\\'), @"SteamApps\common\" + GameDirectoryName);
            }

            public void BrowserForValidPath(IWin32Window owner)
            {
                while (!IsPathValid())
                {
                    var result = MessageBox.Show(owner,
                        @"Could not detect " + _gameName + @" game path. " +
                        @"If you do not have this game installed press [Cancel] to skip, " +
                        @"otherwise press [OK] to select path manually." + Environment.NewLine + Environment.NewLine +
                        @"For example:" + Environment.NewLine + @"D:\Steam\SteamApps\Common\" +
                        GameDirectoryName,
                        @"Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                    if (result == DialogResult.Cancel)
                    {
                        GamePath = InstallationSkippedPath;
                        return;
                    }
                    var browser = new FolderBrowserDialog();
                    browser.Description = @"Select " + _gameName + @" game path";
                    browser.ShowNewFolderButton = false;
                    result = browser.ShowDialog(owner);
                    if (result == DialogResult.Cancel)
                        Environment.Exit(1);
                    GamePath = browser.SelectedPath;
                }
            }
        }
    }
}