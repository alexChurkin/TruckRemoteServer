using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using TruckRemoteServer.Input;
using TruckRemoteServer.Presentation;

namespace TruckRemoteServer.Infrastructure
{
    //Adds the keys of GameControlsFile to controls.sii of every ETS2 and ATS profile (Documents\<game>)
    public sealed class GameControlsSetup : IGameControlsSetup, IDisposable
    {
        private const string ControlsFileName = "controls.sii";
        //The bindings as they were before the first change
        private const string BackupFileName = "controls.truckremote.bak";

        private static readonly (string Folder, string Process)[] Games =
        {
            ("Euro Truck Simulator 2", "eurotrucks2"),
            ("American Truck Simulator", "amtrucks")
        };

        private static readonly string[] ProfileFolders = { "profiles", "steam_profiles" };

        //Keeps every byte of the file as it is
        private static readonly Encoding FileEncoding = Encoding.GetEncoding(28591);

        private readonly ILogger<GameControlsSetup> logger;
        private readonly object processLock = new object();
        //Running games whose exit is awaited
        private readonly List<Process> watched = new List<Process>();
        private bool disposed;

        public GameControlsSetup(ILogger<GameControlsSetup> logger)
        {
            this.logger = logger;
        }

        public void Apply()
        {
            foreach ((string folder, string process) in Games)
            {
                Apply(folder, process);
            }
        }

        public void Dispose()
        {
            lock (processLock)
            {
                disposed = true;
                foreach (Process process in watched) process.Dispose();
                watched.Clear();
            }
        }

        private void Apply(string gameFolder, string processName)
        {
            try
            {
                if (WaitForExit(gameFolder, processName)) return;

                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                foreach (string profileFolder in ProfileFolders)
                {
                    string profiles = Path.Combine(documents, gameFolder, profileFolder);
                    if (!Directory.Exists(profiles)) continue;
                    foreach (string profile in Directory.GetDirectories(profiles))
                    {
                        AddKeys(Path.Combine(profile, ControlsFileName));
                    }
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                || e is InvalidOperationException || e is System.ComponentModel.Win32Exception)
            {
                //The buttons of these actions don't work until their keys are bound in the game
                logger.LogWarning(e, "Controls of {Game} weren't changed", gameFolder);
            }
        }

        //True if the game is running: its profiles are changed after it exits
        private bool WaitForExit(string gameFolder, string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return false;

            lock (processLock)
            {
                foreach (Process process in processes)
                {
                    if (disposed)
                    {
                        process.Dispose();
                        continue;
                    }
                    watched.Add(process);
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) => OnGameExited(process, gameFolder, processName);
                }
            }
            return true;
        }

        private void OnGameExited(Process process, string gameFolder, string processName)
        {
            lock (processLock)
            {
                if (disposed || !watched.Remove(process)) return;
                process.Dispose();
            }
            Apply(gameFolder, processName);
        }

        private static void AddKeys(string controlsFile)
        {
            if (!File.Exists(controlsFile)) return;
            string content = File.ReadAllText(controlsFile, FileEncoding);
            string changed = GameControlsFile.AddMissingKeys(content);
            if (changed == content) return;

            string backup = Path.Combine(Path.GetDirectoryName(controlsFile), BackupFileName);
            if (!File.Exists(backup)) File.Copy(controlsFile, backup);
            File.WriteAllText(controlsFile, changed, FileEncoding);
        }
    }
}
