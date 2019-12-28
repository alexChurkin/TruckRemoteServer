using System;

namespace TruckRemoteServer
{
    class TimeUtil
    {
        public static long GetCurrentUnixTime()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }
}
