using System;
using System.Runtime.InteropServices;

namespace TruckRemoteServer
{
    //Windows sleeps at least ~15.6 ms by default, so Thread.Sleep(20) takes ~31 ms.
    //While the object is alive the system timer has 1 ms resolution (as games do)
    public sealed class TimerResolution : IDisposable
    {
        private const uint PERIOD_MS = 1;
        private bool active;

        public TimerResolution()
        {
            try
            {
                active = timeBeginPeriod(PERIOD_MS) == 0;
            }
            catch (Exception)
            {
                //Not Windows: the default resolution stays
            }
        }

        public void Dispose()
        {
            if (!active) return;
            active = false;
            timeEndPeriod(PERIOD_MS);
        }

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint period);
    }
}
