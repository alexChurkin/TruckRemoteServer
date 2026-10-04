using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TruckRemoteServer
{
    //Clicks keys one by one on its own thread. The game reads the keyboard once per frame,
    //so a key pressed and released at once may be missed (especially at low FPS):
    //every key is held for a while, and there is a pause before the next click (the same key may follow)
    public class KeyClicker
    {
        public const int DEFAULT_HOLD_MS = 40;
        public const int DEFAULT_GAP_MS = 30;

        private readonly BlockingCollection<short> queue = new BlockingCollection<short>();
        private readonly Action<short> press;
        private readonly Action<short> release;
        private readonly int holdMs;
        private readonly int gapMs;

        public KeyClicker(Action<short> press, Action<short> release,
            int holdMs = DEFAULT_HOLD_MS, int gapMs = DEFAULT_GAP_MS)
        {
            this.press = press;
            this.release = release;
            this.holdMs = holdMs;
            this.gapMs = gapMs;

            Thread thread = new Thread(ClickQueuedKeys)
            {
                IsBackground = true,
                Name = "Key clicker"
            };
            thread.Start();
        }

        //Returns at once, the click is made later in the order of calls
        public void Click(short scanCode)
        {
            queue.Add(scanCode);
        }

        private void ClickQueuedKeys()
        {
            foreach (short scanCode in queue.GetConsumingEnumerable())
            {
                try
                {
                    press(scanCode);
                    Thread.Sleep(holdMs);
                    release(scanCode);
                }
                catch (Exception e)
                {
                    Console.WriteLine("INFO: Key click failed: " + e.Message);
                }
                Thread.Sleep(gapMs);
            }
        }
    }
}
