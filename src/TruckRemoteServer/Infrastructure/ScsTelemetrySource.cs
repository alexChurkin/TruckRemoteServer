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
        private const int CheckInterval = 1000;

        private readonly byte[] buffer = new byte[ScsTelemetryMap.ReadSize];
        private readonly Stopwatch sinceCheck = new Stopwatch();
        private MemoryMappedFile map;
        private MemoryMappedViewAccessor view;
        private readonly GameUnits units;

        public ScsTelemetrySource(GameUnits units)
        {
            this.units = units;
        }

        public TruckTelemetry Read()
        {
            lock (buffer)
            {
                if (!sinceCheck.IsRunning || sinceCheck.ElapsedMilliseconds >= CheckInterval)
                {
                    sinceCheck.Restart();
                    //The map outlives a crashed game while it's open here, and the plugin can't mark it inactive then
                    if (IsGameRunning()) Open();
                    else Close();
                }
                if (view == null) return TruckTelemetry.Unknown;

                view.ReadArray(0, buffer, 0, buffer.Length);
                TruckTelemetry truck = ScsTelemetryMap.Parse(buffer);
                //The units aren't in the telemetry: they are a setting of the game
                if (truck.Available) truck.SpeedInMph = units.Mph(truck.Game);
                return truck;
            }
        }

        private void Open()
        {
            if (view != null) return;
            try
            {
                map = MemoryMappedFile.OpenExisting(ScsTelemetryMap.MapName, MemoryMappedFileRights.Read);
                view = map.CreateViewAccessor(0, ScsTelemetryMap.ReadSize, MemoryMappedFileAccess.Read);
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
