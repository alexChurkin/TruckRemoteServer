using System.Collections.Generic;
using TruckRemoteServer.Input;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class ActionCountersTests
    {
        private static Dictionary<int, int> Counters(params (int Id, int Value)[] counters)
        {
            var result = new Dictionary<int, int>();
            foreach (var (id, value) in counters) result[id] = value;
            return result;
        }

        [Fact]
        public void IncreasedCountersGiveClicks()
        {
            var counters = new ActionCounters();

            Assert.Equal(Counters((1, 1), (3, 2)), counters.Update(Counters((1, 1), (2, 0), (3, 2))));
            Assert.Empty(counters.Update(Counters((1, 1), (3, 2))));
            Assert.Equal(Counters((1, 1), (2, 1)), counters.Update(Counters((1, 2), (2, 1), (3, 2))));
        }

        [Fact]
        public void LateOldMessageDoesntRepeatClicks()
        {
            var counters = new ActionCounters();
            counters.Update(Counters((1, 3)));

            //Message with the previous value came late, then a normal one
            Assert.Empty(counters.Update(Counters((1, 2))));
            Assert.Empty(counters.Update(Counters((1, 3))));
            Assert.Equal(Counters((1, 1)), counters.Update(Counters((1, 4))));
        }

        [Fact]
        public void SyncTakesCountersWithoutClicks()
        {
            var counters = new ActionCounters();
            counters.Sync(Counters((1, 10), (2, 20)));

            Assert.Equal(Counters((2, 1)), counters.Update(Counters((1, 10), (2, 21))));
        }

        [Fact]
        public void BigJumpsAreTakenAsResetWithoutClicks()
        {
            var counters = new ActionCounters();
            counters.Update(Counters((1, 2)));

            Assert.Empty(counters.Update(Counters((1, 50))));
            Assert.Equal(Counters((1, 1)), counters.Update(Counters((1, 51))));
            //Restarted controller counts from 0 again
            Assert.Empty(counters.Update(Counters((1, 0))));
            Assert.Equal(Counters((1, 1)), counters.Update(Counters((1, 1))));
        }

        [Fact]
        public void CountersWrapAround()
        {
            var counters = new ActionCounters();
            counters.Sync(Counters((1, 254)));

            //Counters are sent as a byte: 255 -> 0 -> 1 are clicks
            Assert.Equal(Counters((1, 3)), counters.Update(Counters((1, 1))));
            //An old message from before the wrap is late, not a reset
            Assert.Empty(counters.Update(Counters((1, 255))));
        }

        [Fact]
        public void ActionClickedFirstTimeGivesClicks()
        {
            var counters = new ActionCounters();
            counters.Sync(Counters());

            //The controller sends only clicked actions: a new id starts from 0
            Assert.Equal(Counters((19, 1)), counters.Update(Counters((19, 1))));
        }
    }
}
