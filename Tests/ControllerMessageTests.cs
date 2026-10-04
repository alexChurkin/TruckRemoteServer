using TruckRemoteServer.Protocol;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class ControllerMessageTests
    {
        [Fact]
        public void ParsesBaseMessageOfOldControllers()
        {
            var state = ControllerMessage.Parse("-2.5,true,false,true,false,false,true,false,2,true", 8);

            Assert.NotNull(state);
            Assert.Equal(-2.5, state.Steering);
            Assert.True(state.BrakePressed);
            Assert.False(state.GasPressed);
            Assert.True(state.LeftSignalClick);
            Assert.True(state.ParkingBrakeClick);
            Assert.Equal(2, state.Horn);
            Assert.True(state.CruiseClick);
            Assert.False(state.HasPedalLevels);
            Assert.Empty(state.ActionCounters);
        }

        [Fact]
        public void ParsesPedalLevelsAndActions()
        {
            var state = ControllerMessage.Parse(
                "1.0,false,false,false,false,false,false,false,0,false,0.750,0.125,1,0,3", 8);

            Assert.NotNull(state);
            Assert.True(state.HasPedalLevels);
            Assert.Equal(0.75, state.GasLevel);
            Assert.Equal(0.125, state.BrakeLevel);
            Assert.Equal(new[] { 1, 0, 3 }, state.ActionCounters);
        }

        [Fact]
        public void IgnoresActionsUnknownToServer()
        {
            var state = ControllerMessage.Parse("0,false,false,false,false,false,false,false,0,false,0,0,1,2,3", 2);

            Assert.Equal(new[] { 1, 2 }, state.ActionCounters);
        }

        [Fact]
        public void KotlinBooleansAreAccepted()
        {
            //Android sends "true"/"false", C# writes "True"/"False"
            var state = ControllerMessage.Parse("0,True,FALSE,false,false,false,false,false,0,false", 8);

            Assert.True(state.BrakePressed);
            Assert.False(state.GasPressed);
        }

        [Theory]
        [InlineData("")]
        [InlineData("paused")]
        [InlineData("0,false,false")]
        [InlineData("abc,false,false,false,false,false,false,false,0,false")]
        [InlineData("0,yes,false,false,false,false,false,false,0,false")]
        [InlineData("0,false,false,false,false,false,false,false,0,false,0,0,99999999999")]
        public void MalformedMessageGivesNull(string message)
        {
            Assert.Null(ControllerMessage.Parse(message, 8));
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        public void NotFiniteSteeringBecomesZero(string steering)
        {
            var state = ControllerMessage.Parse(steering + ",false,false,false,false,false,false,false,0,false", 8);

            Assert.Equal(0, state.Steering);
        }
    }
}
