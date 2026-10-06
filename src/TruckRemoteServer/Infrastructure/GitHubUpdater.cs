using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;
using TruckRemoteServer.Updates;

namespace TruckRemoteServer.Infrastructure
{
    //Releases of the server on GitHub; the update replaces the exe in place: the running exe can be renamed on Windows,
    //so it becomes TruckRemoteServer.exe.old, the new one takes its name and is started, and the old file is deleted
    //by the next start
    public sealed class GitHubUpdater : IUpdater
    {
        //The new server waits for the old one (its process id follows): the port is free only after it exits
        public const string AfterUpdateArgument = "--after-update";
        private const string NewSuffix = ".new";
        private const string OldSuffix = ".old";
        private const int WaitForOldMs = 15000;
        //A smaller file isn't the server (an error page, a cut download)
        private const long MinExeSize = 100 * 1024;

        private readonly string programPath;
        private readonly Version version;
        private readonly ILogger<GitHubUpdater> logger;

        public GitHubUpdater(string programPath, Version version, ILogger<GitHubUpdater> logger)
        {
            this.programPath = programPath;
            this.version = version;
            this.logger = logger;
        }

        public ReleaseInfo GetLatestRelease()
        {
            try
            {
                using (WebClient client = CreateClient())
                {
                    client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    return ReleaseInfo.Parse(client.DownloadString(ReleaseInfo.LatestReleaseApi));
                }
            }
            catch (Exception e) when (e is WebException || e is NotSupportedException || e is InvalidOperationException)
            {
                logger.LogInformation("The latest release isn't known: {Message}", e.Message);
                return null;
            }
        }

        public bool Install(ReleaseInfo release)
        {
            if (release?.Exe == null) return false;
            string newPath = programPath + NewSuffix;
            string oldPath = programPath + OldSuffix;
            try
            {
                using (WebClient client = CreateClient())
                {
                    client.DownloadFile(release.Exe, newPath);
                }
                if (!IsProgram(newPath))
                {
                    logger.LogWarning("The downloaded {File} isn't a program", newPath);
                    File.Delete(newPath);
                    return false;
                }
                if (File.Exists(oldPath)) File.Delete(oldPath);
                File.Move(programPath, oldPath);
                try
                {
                    File.Move(newPath, programPath);
                }
                catch (Exception)
                {
                    //The server stays as it was
                    File.Move(oldPath, programPath);
                    throw;
                }
                string pid = Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
                Process.Start(new ProcessStartInfo(programPath, AfterUpdateArgument + " " + pid) { UseShellExecute = false });
                logger.LogInformation("Updated to {Version}", release);
                return true;
            }
            catch (Exception e) when (e is WebException || e is IOException || e is UnauthorizedAccessException
                || e is NotSupportedException || e is System.ComponentModel.Win32Exception)
            {
                //E.g. the server is in Program Files: the update is downloaded from the page then
                logger.LogWarning(e, "The update to {Version} wasn't installed", release);
                TryDelete(newPath);
                return false;
            }
        }

        public void OpenPage(ReleaseInfo release)
        {
            try
            {
                Process.Start(new ProcessStartInfo(release.Page) { UseShellExecute = true });
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception || e is InvalidOperationException)
            {
                logger.LogWarning(e, "The page of the release wasn't opened");
            }
        }

        //At the start: a server started by the update waits for the old one to exit, the old exe is deleted
        public static void FinishUpdate(string programPath, string[] args)
        {
            if (args.Length > 1 && args[0] == AfterUpdateArgument
                && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid))
            {
                try
                {
                    using (Process old = Process.GetProcessById(pid))
                    {
                        old.WaitForExit(WaitForOldMs);
                    }
                }
                catch (ArgumentException)
                {
                    //It has already exited
                }
            }
            TryDelete(programPath + OldSuffix);
        }

        private WebClient CreateClient()
        {
            //.NET Framework 4.7.2 on older Windows doesn't offer TLS 1.2 by itself, GitHub needs it
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "TruckRemoteServer/" + version.ToString(3);
            return client;
        }

        //A Windows program starts with "MZ"
        private static bool IsProgram(string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length < MinExeSize) return false;
            using (FileStream stream = file.OpenRead())
            {
                return stream.ReadByte() == 'M' && stream.ReadByte() == 'Z';
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                //The old exe may be still running for a moment: it's deleted by the next start
            }
        }
    }
}
