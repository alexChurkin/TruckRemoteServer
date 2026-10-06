using System.Diagnostics;

namespace TruckRemoteServer
{
    public static class MonotonicClock
    {
        //Not affected by system clock changes
        public static long Millis => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
    }
}
