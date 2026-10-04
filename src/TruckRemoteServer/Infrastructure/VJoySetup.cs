using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using TruckRemoteServer.Presentation;
using vJoyInterfaceWrap;

namespace TruckRemoteServer.Infrastructure
{
    //Installs vJoy 2.1.9.1 (its setup is embedded into the exe) and configures device 1.
    //The work needs administrator rights, so the exe starts itself elevated with ELEVATED_ARGUMENT
    public sealed class VJoySetup : IJoystickSetup
    {
        public const string ELEVATED_ARGUMENT = "--setup-vjoy";

        private const uint DEVICE_ID = 1;
        private const string SETUP_RESOURCE = "vJoySetup.exe";
        //As recommended before: steering on X, 8 buttons, force feedback. Pedal axes (Y, Z) are added by the user
        //for analog pedals: unbound axes resting in the middle could press the pedals if the game binds them
        private const string DEVICE_CONFIG = "1 -f -a x -b 8 -e All";
        private const int SETUP_TIMEOUT_MS = 5 * 60 * 1000;
        private const int CONFIG_TIMEOUT_MS = 60 * 1000;

        private static string WorkFolder => Path.Combine(Path.GetTempPath(), "TruckRemoteServer");
        private static string LogFile => Path.Combine(WorkFolder, "vjoy-setup.log");

        public bool NeedsSetup()
        {
            try
            {
                var device = new vJoy();
                if (!device.vJoyEnabled()) return true;
                VjdStat status = device.GetVJDStatus(DEVICE_ID);
                if (status == VjdStat.VJD_STAT_MISS || status == VjdStat.VJD_STAT_UNKN) return true;
                return !device.GetVJDAxisExist(DEVICE_ID, HID_USAGES.HID_USAGE_X);
            }
            catch (Exception e)
            {
                Debug.WriteLine("vJoy check failed: " + e.Message);
                return true;
            }
        }

        public bool Setup()
        {
            var startInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, ELEVATED_ARGUMENT)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    process?.WaitForExit();
                    Debug.WriteLine("vJoy setup finished with code " + process?.ExitCode);
                }
                return true;
            }
            catch (Win32Exception e)
            {
                //Elevation was cancelled
                Debug.WriteLine("vJoy setup wasn't started: " + e.Message);
                return false;
            }
        }

        /* The elevated process */

        //Returns the exit code of the process: 0 - vJoy is ready
        public static int RunElevated()
        {
            try
            {
                Directory.CreateDirectory(WorkFolder);
                File.WriteAllText(LogFile, DateTime.Now + " vJoy setup" + Environment.NewLine);

                string configTool = FindConfigTool();
                //A fresh vJoy has a default device 1 with all axes: it's replaced, a user's device is kept
                bool installedNow = configTool == null;
                if (installedNow)
                {
                    string setup = Path.Combine(WorkFolder, SETUP_RESOURCE);
                    using (Stream resource = typeof(VJoySetup).Assembly.GetManifestResourceStream(SETUP_RESOURCE))
                    using (FileStream file = File.Create(setup))
                    {
                        resource.CopyTo(file);
                    }
                    string setupLog = Path.Combine(WorkFolder, "vjoy-install.log");
                    int code = Run(setup, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /LOG=\"" + setupLog + "\"",
                        SETUP_TIMEOUT_MS);
                    TryDelete(setup);
                    configTool = FindConfigTool();
                    if (code != 0 || configTool == null)
                    {
                        Log("vJoy wasn't installed");
                        return 2;
                    }
                }

                Run(configTool, "enable on", CONFIG_TIMEOUT_MS);
                if (installedNow || DeviceNeedsConfig()) Run(configTool, DEVICE_CONFIG, CONFIG_TIMEOUT_MS);

                bool ready = WaitUntilReady();
                Log(ready ? "vJoy is ready" : "vJoy isn't ready");
                return ready ? 0 : 3;
            }
            catch (Exception e)
            {
                Log("Failed: " + e);
                return 1;
            }
        }

        private static bool DeviceNeedsConfig()
        {
            try
            {
                var device = new vJoy();
                VjdStat status = device.GetVJDStatus(DEVICE_ID);
                return status == VjdStat.VJD_STAT_MISS || status == VjdStat.VJD_STAT_UNKN
                    || !device.GetVJDAxisExist(DEVICE_ID, HID_USAGES.HID_USAGE_X);
            }
            catch (Exception e)
            {
                Log("Device check failed: " + e.Message);
                return true;
            }
        }

        //The driver restarts the device after configuring
        private static bool WaitUntilReady()
        {
            var setup = new VJoySetup();
            for (int i = 0; i < 20; i++)
            {
                if (!setup.NeedsSetup()) return true;
                Thread.Sleep(500);
            }
            return false;
        }

        //vJoyConfig.exe of the system bitness from the installed vJoy
        private static string FindConfigTool()
        {
            string bitness = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            var folders = InstallFolders()
                .Concat(new[]
                {
                    Environment.GetEnvironmentVariable("ProgramW6432"),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                }.Where(path => !string.IsNullOrEmpty(path)).Select(path => Path.Combine(path, "vJoy")));
            foreach (string folder in folders.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string tool = Path.Combine(folder, bitness, "vJoyConfig.exe");
                if (File.Exists(tool)) return tool;
            }
            return null;
        }

        private static string[] InstallFolders()
        {
            const string uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            var views = Environment.Is64BitOperatingSystem
                ? new[] { RegistryView.Registry64, RegistryView.Registry32 }
                : new[] { RegistryView.Registry32 };
            return views.SelectMany(view =>
            {
                try
                {
                    using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey programs = root.OpenSubKey(uninstall))
                    {
                        if (programs == null) return new string[0];
                        return programs.GetSubKeyNames().Select(name =>
                        {
                            using (RegistryKey program = programs.OpenSubKey(name))
                            {
                                string displayName = program?.GetValue("DisplayName") as string;
                                return displayName != null && displayName.StartsWith("vJoy", StringComparison.OrdinalIgnoreCase)
                                    ? program.GetValue("InstallLocation") as string
                                    : null;
                            }
                        }).Where(path => !string.IsNullOrEmpty(path)).ToArray();
                    }
                }
                catch (Exception e)
                {
                    Log("Registry: " + e.Message);
                    return new string[0];
                }
            }).ToArray();
        }

        private static int Run(string program, string arguments, int timeoutMs)
        {
            Log("Run: " + program + " " + arguments);
            var startInfo = new ProcessStartInfo(program, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(startInfo))
            {
                if (!process.WaitForExit(timeoutMs))
                {
                    Log("Timed out");
                    return -1;
                }
                Log("Exit code: " + process.ExitCode);
                return process.ExitCode;
            }
        }

        private static void Log(string text)
        {
            try
            {
                File.AppendAllText(LogFile, DateTime.Now + " " + text + Environment.NewLine);
            }
            catch (IOException)
            {
                //Only for diagnostics
            }
        }

        private static void TryDelete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                //Temporary file
            }
        }
    }
}
