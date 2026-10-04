using TruckRemoteServer.Protocol;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class SequenceGateTests
    {
        [Fact]
        public void OldMessagesAreDropped()
        {
            var gate = new SequenceGate();

            Assert.True(gate.Accept(1));
            Assert.True(gate.Accept(3));
            Assert.False(gate.Accept(2));
            Assert.False(gate.Accept(3));
            Assert.True(gate.Accept(4));
        }

        [Fact]
        public void MessagesWithoutNumberAreAlwaysAccepted()
        {
            var gate = new SequenceGate();

            Assert.True(gate.Accept(null));
            Assert.True(gate.Accept(5));
            Assert.True(gate.Accept(null));
        }

        [Fact]
        public void ResetAllowsNumberingFromStart()
        {
            var gate = new SequenceGate();
            gate.Accept(100);
            gate.Reset();

            Assert.True(gate.Accept(1));
        }
    }
}
