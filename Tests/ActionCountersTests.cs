using Xunit;

namespace TruckRemoteServer.Tests
{
    public class ActionCountersTests
    {
        [Fact]
        public void IncreasedCountersGiveClicks()
        {
            var counters = new ActionCounters(3);

            Assert.Equal(new[] { 1, 0, 2 }, counters.Update(new[] { 1, 0, 2 }));
            Assert.Equal(new[] { 0, 0, 0 }, counters.Update(new[] { 1, 0, 2 }));
            Assert.Equal(new[] { 1, 1, 0 }, counters.Update(new[] { 2, 1, 2 }));
        }

        [Fact]
        public void LateOldMessageDoesntRepeatClicks()
        {
            var counters = new ActionCounters(1);
            counters.Update(new[] { 3 });

            //Message with the previous value came late, then a normal one
            Assert.Equal(new[] { 0 }, counters.Update(new[] { 2 }));
            Assert.Equal(new[] { 0 }, counters.Update(new[] { 3 }));
            Assert.Equal(new[] { 1 }, counters.Update(new[] { 4 }));
        }

        [Fact]
        public void SyncTakesCountersWithoutClicks()
        {
            var counters = new ActionCounters(2);
            counters.Sync(new[] { 10, 20 });

            Assert.Equal(new[] { 0, 1 }, counters.Update(new[] { 10, 21 }));
        }

        [Fact]
        public void BigJumpsAreTakenAsResetWithoutClicks()
        {
            var counters = new ActionCounters(1);
            counters.Update(new[] { 2 });

            Assert.Equal(new[] { 0 }, counters.Update(new[] { 50 }));
            Assert.Equal(new[] { 1 }, counters.Update(new[] { 51 }));
            //Restarted controller counts from 0 again
            Assert.Equal(new[] { 0 }, counters.Update(new[] { 0 }));
            Assert.Equal(new[] { 1 }, counters.Update(new[] { 1 }));
        }

        [Fact]
        public void ShorterOrLongerCounterListsAreAccepted()
        {
            var counters = new ActionCounters(2);

            Assert.Equal(new[] { 1, 0 }, counters.Update(new[] { 1 }));
            Assert.Equal(new[] { 0, 1 }, counters.Update(new[] { 1, 1, 7 }));
        }
    }
}
