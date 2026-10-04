using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using TruckRemoteServer.Telemetry;

namespace TruckRemoteServer.Infrastructure
{
    //Reads the shared memory of the scs-telemetry plugin (all values are false while the game isn't running)
    public sealed class ScsTelemetrySource : ITelemetrySource, IDisposable
    {
        private static readonly string[] GameProcesses = { "eurotrucks2", "amtrucks" };
        //Opening the map and looking for the game are too slow to do 50 times per second
        private const int CHECK_INTERVAL = 1000;

        private readonly byte[] buffer = new byte[ScsTelemetryMap.READ_SIZE];
        private readonly Stopwatch sinceCheck = new Stopwatch();
        private MemoryMappedFile map;
        private MemoryMappedViewAccessor view;

        public TruckTelemetry Read()
        {
            lock (buffer)
            {
                if (!sinceCheck.IsRunning || sinceCheck.ElapsedMilliseconds >= CHECK_INTERVAL)
                {
                    sinceCheck.Restart();
                    //The map outlives a crashed game while it's open here, and the plugin can't mark it inactive then
                    if (IsGameRunning()) Open();
                    else Close();
                }
                if (view == null) return TruckTelemetry.Unknown;

                view.ReadArray(0, buffer, 0, buffer.Length);
                return ScsTelemetryMap.Parse(buffer);
            }
        }

        private void Open()
        {
            if (view != null) return;
            try
            {
                map = MemoryMappedFile.OpenExisting(ScsTelemetryMap.NAME, MemoryMappedFileRights.Read);
                view = map.CreateViewAccessor(0, ScsTelemetryMap.READ_SIZE, MemoryMappedFileAccess.Read);
            }
            catch (Exception)
            {
                //The plugin isn't loaded (yet)
                Close();
            }
        }

        private void Close()
        {
            view?.Dispose();
            map?.Dispose();
            view = null;
            map = null;
        }

        private static bool IsGameRunning()
        {
            foreach (string name in GameProcesses)
            {
                Process[] processes = Process.GetProcessesByName(name);
                foreach (Process process in processes) process.Dispose();
                if (processes.Length > 0) return true;
            }
            return false;
        }

        public void Dispose()
        {
            lock (buffer)
            {
                Close();
            }
        }
    }
}
