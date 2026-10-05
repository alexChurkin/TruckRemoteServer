using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TruckRemoteServer.Input
{
    //Clicks keys one by one on its own thread. The game reads the keyboard once per frame,
    //so a key pressed and released at once may be missed (especially at low FPS):
    //every key is held for a while, and there is a pause before the next click (the same key may follow)
    public sealed class KeyClicker<TKey> : IDisposable
    {
        public const int DefaultHoldMs = 40;
        public const int DefaultGapMs = 30;

        private readonly BlockingCollection<TKey> queue = new BlockingCollection<TKey>();
        private readonly Action<TKey> press;
        private readonly Action<TKey> release;
        private readonly int holdMs;
        private readonly int gapMs;

        public KeyClicker(Action<TKey> press, Action<TKey> release,
            int holdMs = DefaultHoldMs, int gapMs = DefaultGapMs)
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
        public void Click(TKey key)
        {
            queue.Add(key);
        }

        //Queued clicks are still made, then the thread ends
        public void Dispose()
        {
            queue.CompleteAdding();
        }

        private void ClickQueuedKeys()
        {
            foreach (TKey key in queue.GetConsumingEnumerable())
            {
                try
                {
                    press(key);
                    Thread.Sleep(holdMs);
                    release(key);
                }
                catch (Exception e)
                {
                    Console.WriteLine("INFO: Key click failed: " + e.Message);
                }
                Thread.Sleep(gapMs);
            }
            queue.Dispose();
        }
    }
}
