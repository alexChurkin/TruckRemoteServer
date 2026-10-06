using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using TruckRemoteServer.Localization;

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

        sealed class GameState
        {
            const string InstallationSkippedPath = "N/A";
            //RenCloud's scs-sdk-plugin 1.12.1 (revision 12), the usual name lets other telemetry apps share it
            const string TelemetryDllName = "scs-telemetry.dll";
            const string TelemetryX64DllSha256 = "1d03dbc7a975e72203c60a7b9998021ceb8800b836bf28a131279979ad386cd4";
            //Plugin installed by older versions (scs-sdk-plugin of 2019 under another name)
            const string LegacyTelemetryDllName = "ets2-telemetry-server.dll";
            const string LegacyTelemetryX64DllSha256 = "29154a0f0621cb78053f38e9bdf24699ab4797c17261b86d8f6c8cb5a4692f24";

            readonly string gameName;

            public GameState(string gameName, string gamePath)
            {
                this.gameName = gameName;
                GamePath = gamePath;
                RedetectSkipped();
            }

            //A game that was skipped (it wasn't installed then) may be installed now: it is looked for again,
            //without asking the user about it
            void RedetectSkipped()
            {
                if (GamePath != InstallationSkippedPath) return;
                try
                {
                    DetectPath();
                }
                catch (Exception)
                {
                    //Steam isn't found
                }
                if (GamePath == InstallationSkippedPath || !IsPathValid()) GamePath = InstallationSkippedPath;
            }

            string GameDirectoryName
            {
                get
                {
                    string fullName = "Euro Truck Simulator 2";
                    if (gameName == Ats)
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
                //Log.InfoFormat("Validating {2} path: '{0}' ... {1}", GamePath, validated ? "OK" : "Fail", gameName);
                return validated;
            }

            public bool IsPluginValid()
            {
                if (GamePath == InstallationSkippedPath)
                    return true;

                if (!IsPathValid())
                    return false;

                return Sha256(GetTelemetryPluginDllFileName(GamePath)) == TelemetryX64DllSha256;
            }

            public void InstallPlugin()
            {
                if (GamePath == InstallationSkippedPath)
                    return;

                string x64DllFileName = GetTelemetryPluginDllFileName(GamePath);

                //The plugin is embedded into the exe
                using (Stream plugin = typeof(PluginInstaller).Assembly.GetManifestResourceStream(TelemetryDllName))
                using (FileStream file = File.Create(x64DllFileName))
                {
                    plugin.CopyTo(file);
                }
                RemoveLegacyPlugin();
            }

            //The old plugin isn't read anymore, it's removed only if it's exactly the one installed by us
            void RemoveLegacyPlugin()
            {
                string legacyDllFileName = Path.Combine(GetPluginPath(GamePath), LegacyTelemetryDllName);
                try
                {
                    if (Sha256(legacyDllFileName) == LegacyTelemetryX64DllSha256)
                        File.Delete(legacyDllFileName);
                }
                catch (Exception)
                {
                    //The game is running and keeps it loaded: it stays, but it is harmless (nothing reads its memory)
                }
            }

            static string GetDefaultSteamPath()
            {
                var steamKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return steamKey?.GetValue("SteamPath") as string;
            }

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

            static string Sha256(string fileName)
            {
                if (!File.Exists(fileName))
                    return null;
                using (var provider = SHA256.Create())
                {
                    var bytes = File.ReadAllBytes(fileName);
                    var hash = provider.ComputeHash(bytes);
                    var result = string.Concat(hash.Select(b => $"{b:x02}"));
                    return result;
                }
            }

            //The game may be in any Steam library (e.g. on another disk), they are listed in libraryfolders.vdf
            public void DetectPath()
            {
                string steamPath = GetDefaultSteamPath();
                if (string.IsNullOrEmpty(steamPath)) return;
                steamPath = steamPath.Replace('/', '\\');

                foreach (string library in GetSteamLibraries(steamPath))
                {
                    GamePath = Path.Combine(library, @"steamapps\common\" + GameDirectoryName);
                    if (IsPathValid()) return;
                }
                GamePath = Path.Combine(steamPath, @"steamapps\common\" + GameDirectoryName);
            }

            private static string[] GetSteamLibraries(string steamPath)
            {
                string libraries = Path.Combine(steamPath, @"steamapps\libraryfolders.vdf");
                try
                {
                    var paths = File.Exists(libraries)
                        ? Regex.Matches(File.ReadAllText(libraries), "\"path\"\\s+\"([^\"]+)\"")
                            .Cast<Match>()
                            .Select(m => m.Groups[1].Value.Replace(@"\\", @"\"))
                        : Enumerable.Empty<string>();
                    return new[] { steamPath }.Concat(paths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                }
                catch (Exception)
                {
                    return new[] { steamPath };
                }
            }

            public void BrowserForValidPath(IWin32Window owner)
            {
                while (!IsPathValid())
                {
                    var result = MessageBox.Show(owner,
                        Texts.Format(TextKeys.GameNotFound, GameDirectoryName,
                            @"D:\SteamLibrary\steamapps\common\" + GameDirectoryName),
                        "Truck Remote Server", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                    if (result == DialogResult.Cancel)
                    {
                        GamePath = InstallationSkippedPath;
                        return;
                    }
                    var browser = new FolderBrowserDialog();
                    browser.Description = Texts.Format(TextKeys.SelectGameFolder, GameDirectoryName);
                    browser.ShowNewFolderButton = false;
                    result = browser.ShowDialog(owner);
                    //The game is skipped (the program used to close here)
                    if (result == DialogResult.Cancel)
                    {
                        GamePath = InstallationSkippedPath;
                        return;
                    }
                    GamePath = browser.SelectedPath;
                }
            }
        }
    }
}
