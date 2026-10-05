using TruckRemoteServer.Input;
using TruckRemoteServer.Protocol;
using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class ControllerInputMapperTests
    {
        private readonly FakeKeyboard keyboard = new FakeKeyboard();
        private readonly FakeJoystick joystick = new FakeJoystick();
        private readonly ControllerInputMapper mapper;

        public ControllerInputMapperTests()
        {
            mapper = new ControllerInputMapper(keyboard, joystick);
            mapper.OnControllerConnected();
            keyboard.Clear();
        }

        private static ControllerMessage State(bool left = false, bool lights = false, bool gas = false,
            int horn = 0, double steering = 0, params int[] actions)
        {
            return new ControllerMessage
            {
                LeftSignalClick = left,
                LightsClick = lights,
                GasPressed = gas,
                Horn = horn,
                Steering = steering,
                ActionCounters = actions.Length == 0 ? new int[8] : actions
            };
        }

        [Fact]
        public void FirstMessageTakesTogglesWithoutClicks()
        {
            mapper.Apply(State(left: true, actions: new[] { 3, 0, 0, 0, 0, 0, 0, 0 }));
            Assert.Empty(keyboard.Events);

            mapper.Apply(State(left: false, actions: new[] { 4, 0, 0, 0, 0, 0, 0, 0 }));
            Assert.Equal(new[] { "click Engine", "click LeftBlinker" }, keyboard.Events);
        }

        [Fact]
        public void PedalKeysAreHeldWhilePressed()
        {
            mapper.Apply(State());
            mapper.Apply(State(gas: true));
            mapper.Apply(State(gas: true));
            mapper.Apply(State(gas: false));

            Assert.Equal(new[] { "press Gas", "release Gas" }, keyboard.Events);
        }

        [Fact]
        public void SwitchingHornsReleasesThePreviousOne()
        {
            mapper.Apply(State(horn: 1));
            mapper.Apply(State(horn: 2));
            mapper.Apply(State(horn: 0));

            Assert.Equal(new[]
            {
                "release Horn", "release AirHorn", "press Horn",
                "release Horn", "release AirHorn", "press AirHorn",
                "release Horn", "release AirHorn"
            }, keyboard.Events);
        }

        [Theory]
        [InlineData(false, false, false, new[] { "click Lights" })]
        [InlineData(true, false, false, new[] { "click Lights" })]
        [InlineData(true, false, true, new[] { "click Lights", "click HighBeam" })]
        [InlineData(true, true, false, new[] { "click HighBeam" })]
        [InlineData(true, true, true, new[] { "click HighBeam", "click Lights" })]
        public void LightsButtonDependsOnTheCurrentLights(bool parking, bool low, bool high, string[] expected)
        {
            mapper.Apply(State());
            mapper.UpdateTelemetry(new TruckTelemetry { ParkingLights = parking, LowBeam = low, HighBeam = high });

            mapper.Apply(State(lights: true));

            Assert.Equal(expected, keyboard.Events);
        }

        [Fact]
        public void LightsAreNotSwitchedBeforeTelemetry()
        {
            mapper.Apply(State());
            mapper.Apply(State(lights: true));

            Assert.Empty(keyboard.Events);
        }

        [Fact]
        public void SteeringIsSmoothedAndLimited()
        {
            mapper.SteeringSensitivity = 50;
            mapper.Apply(State(steering: 5));
            //60% of the way to 16384 + 5 * 34.7 * 50
            Assert.Equal(16384 + (int)(0.6 * 5 * 34.7 * 50), joystick.Steering);

            for (int i = 0; i < 20; i++) mapper.Apply(State(steering: 100));
            Assert.Equal(32768, joystick.Steering);
            for (int i = 0; i < 20; i++) mapper.Apply(State(steering: -100));
            Assert.Equal(0, joystick.Steering);
        }

        [Fact]
        public void PedalLevelsAreAxes()
        {
            mapper.Apply(new ControllerMessage { HasPedalLevels = true, GasLevel = 0.5, BrakeLevel = 2, ActionCounters = new int[8] });

            Assert.Equal(16383, joystick.Gas);
            Assert.Equal(JoystickAxis.Max, joystick.Brake);
        }

        [Fact]
        public void ReleasingControlsLetsKeysGoAndCentersTheWheel()
        {
            mapper.Apply(State(gas: true, horn: 1, steering: 9));
            keyboard.Clear();

            mapper.ReleaseControls();

            Assert.Contains("release Gas", keyboard.Events);
            Assert.Contains("release Horn", keyboard.Events);
            Assert.Equal(JoystickAxis.Center, joystick.Steering);
            Assert.Equal(0, joystick.Gas);
        }

        [Fact]
        public void JoystickIsInitializedWhenControllerConnects()
        {
            joystick.IsAvailable = false;
            mapper.OnControllerConnected();

            Assert.Equal(1, joystick.Initializations);
        }
    }
}
