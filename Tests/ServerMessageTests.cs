using TruckRemoteServer.Protocol;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class ServerMessageTests
    {
        [Theory]
        [InlineData(false, false, false, ServerMessage.LIGHTS_OFF)]
        [InlineData(true, false, false, ServerMessage.LIGHTS_PARKING)]
        [InlineData(true, true, false, ServerMessage.LIGHTS_LOW_BEAM)]
        [InlineData(true, true, true, ServerMessage.LIGHTS_HIGH_BEAM)]
        [InlineData(true, false, true, ServerMessage.LIGHTS_PARKING)]
        [InlineData(false, false, true, ServerMessage.LIGHTS_OFF)]
        public void LightsMode(bool parking, bool lowBeam, bool highBeam, int expected)
        {
            Assert.Equal(expected, ServerMessage.LightsMode(parking, lowBeam, highBeam));
        }

        [Fact]
        public void FormatKeepsTheFormatReadByControllers()
        {
            string message = ServerMessage.Format(true, false, true, false, 2, 150, true, false, true, false, 77);

            //Old controllers read the values by their positions, the number is the last field
            Assert.Equal("True,False,True,False,2,150,1,0,1,0,#77", message);
        }
    }
}
