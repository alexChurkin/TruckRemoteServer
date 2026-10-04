using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class KeyClickerTests
    {
        [Fact]
        public void KeysAreHeldAndClickedInOrder()
        {
            var events = new List<(string, short, long)>();
            var stopwatch = Stopwatch.StartNew();
            var done = new CountdownEvent(3);
            var clicker = new KeyClicker(
                key =>
                {
                    lock (events) events.Add(("down", key, stopwatch.ElapsedMilliseconds));
                },
                key =>
                {
                    lock (events) events.Add(("up", key, stopwatch.ElapsedMilliseconds));
                    done.Signal();
                },
                holdMs: 30,
                gapMs: 10);

            clicker.Click(1);
            clicker.Click(2);
            clicker.Click(1);

            Assert.True(done.Wait(5000));
            lock (events)
            {
                Assert.Equal(6, events.Count);
                short[] expectedKeys = { 1, 1, 2, 2, 1, 1 };
                for (int i = 0; i < events.Count; i++)
                {
                    Assert.Equal(i % 2 == 0 ? "down" : "up", events[i].Item1);
                    Assert.Equal(expectedKeys[i], events[i].Item2);
                }
                for (int i = 0; i < events.Count; i += 2)
                {
                    //Timer resolution may make the sleep a bit shorter
                    Assert.True(events[i + 1].Item3 - events[i].Item3 >= 25);
                }
            }
        }

        [Fact]
        public void FailedClickDoesntStopTheQueue()
        {
            var released = new CountdownEvent(1);
            var clicker = new KeyClicker(
                key =>
                {
                    if (key == 1) throw new System.InvalidOperationException();
                },
                key => released.Signal(),
                holdMs: 1,
                gapMs: 1);

            clicker.Click(1);
            clicker.Click(2);

            Assert.True(released.Wait(5000));
        }
    }
}
