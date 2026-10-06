using TruckRemoteServer.Protocol;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class ControllerMessageTests
    {
        [Fact]
        public void ParsesBaseMessageOfOldControllers()
        {
            var state = ControllerMessage.Parse("-2.5,true,false,true,false,false,true,false,2,true");

            Assert.NotNull(state);
            Assert.Equal(-2.5, state.Steering);
            Assert.True(state.BrakePressed);
            Assert.False(state.GasPressed);
            Assert.True(state.LeftSignalClick);
            Assert.True(state.ParkingBrakeClick);
            Assert.Equal(2, state.Horn);
            Assert.True(state.CruiseClick);
            Assert.False(state.HasPedalLevels);
            Assert.Empty(state.Actions);
        }

        [Fact]
        public void ParsesPedalLevelsAndActions()
        {
            var state = ControllerMessage.Parse(
                "1.0,false,false,false,false,false,false,false,0,false,0.750,0.125");

            Assert.NotNull(state);
            Assert.True(state.HasPedalLevels);
            Assert.Equal(0.75, state.GasLevel);
            Assert.Equal(0.125, state.BrakeLevel);
            Assert.Empty(state.Actions);
        }

        [Fact]
        public void FieldsOfANewerTextProtocolAreSkipped()
        {
            var state = ControllerMessage.Parse("0,false,false,false,false,false,false,false,0,false,0,0,1,2,engine:4");

            Assert.True(state.HasPedalLevels);
            Assert.Empty(state.Actions);
        }

        [Fact]
        public void KotlinBooleansAreAccepted()
        {
            //Android sends "true"/"false", C# writes "True"/"False"
            var state = ControllerMessage.Parse("0,True,FALSE,false,false,false,false,false,0,false");

            Assert.True(state.BrakePressed);
            Assert.False(state.GasPressed);
        }

        [Fact]
        public void SequenceNumberIsTheLastField()
        {
            var state = ControllerMessage.Parse("0,false,false,false,false,false,false,false,0,false,0.5,0,#42");

            Assert.Equal(42L, state.Sequence);
            Assert.Equal(0.5, state.GasLevel);
            Assert.Null(ControllerMessage.Parse("0,false,false,false,false,false,false,false,0,false").Sequence);
        }

        [Fact]
        public void SequenceDoesntCountAsActions()
        {
            var state = ControllerMessage.Parse("0,false,false,false,false,false,false,false,0,false,0,0,#3");

            Assert.Empty(state.Actions);
            Assert.Equal(3L, state.Sequence);
        }

        [Theory]
        [InlineData("0,false,false,false,false,false,false,false,0,false,#x")]
        [InlineData("0,false,false,false,false,false,false,false,0,#1")]
        public void MalformedSequenceGivesNull(string message)
        {
            Assert.Null(ControllerMessage.Parse(message));
        }

        [Theory]
        [InlineData("")]
        [InlineData("paused")]
        [InlineData("0,false,false")]
        [InlineData("abc,false,false,false,false,false,false,false,0,false")]
        [InlineData("0,yes,false,false,false,false,false,false,0,false")]
        [InlineData("0,false,false,false,false,false,false,false,0,false,x,0")]
        public void MalformedMessageGivesNull(string message)
        {
            Assert.Null(ControllerMessage.Parse(message));
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        public void NotFiniteSteeringBecomesZero(string steering)
        {
            var state = ControllerMessage.Parse(steering + ",false,false,false,false,false,false,false,0,false");

            Assert.Equal(0, state.Steering);
        }
    }
}
