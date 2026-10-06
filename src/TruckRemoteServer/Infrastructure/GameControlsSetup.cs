using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using TruckRemoteServer.Input;
using TruckRemoteServer.Presentation;

namespace TruckRemoteServer.Infrastructure
{
    //Sets up controls.sii of every ETS2 and ATS profile (Documents\<game>) as GameControlsFile tells: vJoy and the keys.
    //The profiles are checked once in a while: a profile may be created or a game installed while the server is running,
    //and a running game writes its bindings on exit (its profiles are changed after it is closed).
    //The keys the player has bound in the profile played last are the keys the server presses (KeyBindings)
    public sealed class GameControlsSetup : IGameControlsSetup, IDisposable
    {
        private const string ControlsFileName = "controls.sii";
        //The game lists the controllers it has seen there
        private const string GlobalControlsFileName = "global_controls.sii";
        //The bindings as they were before the first change
        private const string BackupFileName = "controls.truckremote.bak";
        private const int CheckIntervalMs = 5000;

        private static readonly (string Folder, string Process)[] Games =
        {
            ("Euro Truck Simulator 2", "eurotrucks2"),
            ("American Truck Simulator", "amtrucks")
        };

        private static readonly string[] ProfileFolders = { "profiles", "steam_profiles" };

        //Keeps every byte of the file as it is
        private static readonly Encoding FileEncoding = Encoding.GetEncoding(28591);

        private readonly ILogger<GameControlsSetup> logger;
        private readonly KeyBindings bindings;
        private readonly object timerLock = new object();
        //Files that have the keys: they are read again only after they are changed
        private readonly Dictionary<string, DateTime> checkedFiles = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private Timer timer;
        private bool disposed;
        private int checking;
        //The controls.sii the bindings were read from, and its time then
        private string bindingsFile;
        private DateTime bindingsTime;

        public GameControlsSetup(ILogger<GameControlsSetup> logger, KeyBindings bindings)
        {
            this.logger = logger;
            this.bindings = bindings;
        }

        public void Apply()
        {
            lock (timerLock)
            {
                if (disposed || timer != null) return;
                timer = new Timer(state => Check(), null, 0, CheckIntervalMs);
            }
        }

        public void Dispose()
        {
            lock (timerLock)
            {
                disposed = true;
                timer?.Dispose();
                timer = null;
            }
        }

        private void Check()
        {
            //A check that takes long (a slow disk) isn't run twice at once
            if (Interlocked.Exchange(ref checking, 1) == 1) return;
            try
            {
                var profiles = new List<string>();
                foreach ((string folder, string process) in Games)
                {
                    Check(folder, process, profiles);
                }
                ReadBindings(profiles);
            }
            finally
            {
                Interlocked.Exchange(ref checking, 0);
            }
        }

        //The profiles of the game are added to the list; a running game's profiles aren't changed
        private void Check(string gameFolder, string processName, List<string> allProfiles)
        {
            try
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var profiles = new List<string>();
                foreach (string profileFolder in ProfileFolders)
                {
                    string folder = Path.Combine(documents, gameFolder, profileFolder);
                    if (Directory.Exists(folder)) profiles.AddRange(Directory.GetDirectories(folder));
                }
                allProfiles.AddRange(profiles);
                if (IsRunning(processName)) return;

                string vJoyDevice = FindVJoyDevice(Path.Combine(documents, gameFolder, GlobalControlsFileName));
                foreach (string profile in profiles)
                {
                    SetUp(Path.Combine(profile, ControlsFileName), vJoyDevice);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                || e is InvalidOperationException || e is System.ComponentModel.Win32Exception)
            {
                //The buttons of these actions don't work until their keys are bound in the game
                logger.LogWarning(e, "Controls of {Game} weren't changed", gameFolder);
            }
        }

        //The bindings of the profile played last (its saves or bindings were written last), read again when it changes
        private void ReadBindings(List<string> profiles)
        {
            string latest = null;
            DateTime latestActivity = DateTime.MinValue;
            foreach (string profile in profiles)
            {
                DateTime activity;
                try
                {
                    activity = LastActivity(profile);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    continue;
                }
                if (activity <= latestActivity) continue;
                latestActivity = activity;
                latest = Path.Combine(profile, ControlsFileName);
            }
            try
            {
                if (latest == null || !File.Exists(latest))
                {
                    if (bindingsFile != null) bindings.Use(null);
                    bindingsFile = null;
                    return;
                }
                DateTime written = File.GetLastWriteTimeUtc(latest);
                if (string.Equals(latest, bindingsFile, StringComparison.OrdinalIgnoreCase) && written == bindingsTime) return;

                IReadOnlyDictionary<GameKey, KeyStroke> keys = PlayerBindings.Parse(File.ReadAllText(latest, FileEncoding));
                bindings.Use(keys);
                bindingsFile = latest;
                bindingsTime = written;
                logger.LogInformation("Key bindings of {File}: {Count} actions", latest, keys.Count);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                //Read again on the next check
                logger.LogWarning(e, "Key bindings of {File} weren't read", latest);
                bindingsFile = null;
            }
        }

        //When the profile was played last: the game writes its saves (autosaves too) and profile.sii into it.
        //Not controls.sii: the server changes it in every profile
        private static DateTime LastActivity(string profile)
        {
            DateTime last = File.GetLastWriteTimeUtc(Path.Combine(profile, "profile.sii"));
            string saves = Path.Combine(profile, "save");
            if (!Directory.Exists(saves)) return last;
            foreach (string save in Directory.GetDirectories(saves))
            {
                last = Max(last, File.GetLastWriteTimeUtc(Path.Combine(save, "info.sii")));
                last = Max(last, File.GetLastWriteTimeUtc(Path.Combine(save, "game.sii")));
            }
            return last;
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private static bool IsRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes) process.Dispose();
            return processes.Length > 0;
        }

        private static string FindVJoyDevice(string globalControlsFile)
        {
            if (!File.Exists(globalControlsFile)) return null;
            return GameControlsFile.FindVJoyDevice(File.ReadAllText(globalControlsFile, FileEncoding));
        }

        private void SetUp(string controlsFile, string vJoyDevice)
        {
            if (!File.Exists(controlsFile)) return;
            DateTime written = File.GetLastWriteTimeUtc(controlsFile);
            if (checkedFiles.TryGetValue(controlsFile, out DateTime checkedAt) && checkedAt == written) return;

            string content = File.ReadAllText(controlsFile, FileEncoding);
            string changed = GameControlsFile.SetUp(content, vJoyDevice);
            if (changed != content)
            {
                string backup = Path.Combine(Path.GetDirectoryName(controlsFile), BackupFileName);
                if (!File.Exists(backup)) File.Copy(controlsFile, backup);
                File.WriteAllText(controlsFile, changed, FileEncoding);
                logger.LogInformation("Controls were set up in {File}", controlsFile);
            }
            //Until the game has seen vJoy the joystick can't be set up: the file is checked again later
            if (vJoyDevice != null) checkedFiles[controlsFile] = File.GetLastWriteTimeUtc(controlsFile);
        }
    }
}
