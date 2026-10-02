using System.Diagnostics;

namespace TruckRemoteServer
{
    class TimeUtil
    {
        //Not affected by system clock changes
        public static long GetMonotonicMillis()
        {
            return Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
        }
    }
}
