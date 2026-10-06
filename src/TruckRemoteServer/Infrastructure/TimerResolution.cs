using System;
using System.Runtime.InteropServices;

namespace TruckRemoteServer.Infrastructure
{
    //Windows sleeps at least ~15.6 ms by default, so Thread.Sleep(20) takes ~31 ms.
    //While acquired, the system timer has 1 ms resolution (as games do)
    public sealed class TimerResolution : ITimerResolution
    {
        private const uint PeriodMs = 1;

        public IDisposable Acquire()
        {
            return new Period();
        }

        private sealed class Period : IDisposable
        {
            private bool active;

            public Period()
            {
                try
                {
                    active = timeBeginPeriod(PeriodMs) == 0;
                }
                catch (Exception)
                {
                    //No winmm.dll: the default resolution stays
                }
            }

            public void Dispose()
            {
                if (!active) return;
                active = false;
                _ = timeEndPeriod(PeriodMs);
            }
        }

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint period);
    }
}
