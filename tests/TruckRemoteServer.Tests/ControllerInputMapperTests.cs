using System.Linq;
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
            int horn = 0, double steering = 0, params (int Id, int Value)[] actions)
        {
            return new ControllerMessage
            {
                LeftSignalClick = left,
                LightsClick = lights,
                GasPressed = gas,
                Horn = horn,
                Steering = steering,
                Actions = actions.ToDictionary(action => action.Id, action => action.Value)
            };
        }

        [Fact]
        public void ActionsOfThePanelHaveTheirKeys()
        {
            //Every code is a click or a hold, never both; the codes are fixed (the controller sends them)
            Assert.Empty(System.Linq.Enumerable.Intersect(ControllerActions.Clicks.Keys, ControllerActions.Holds.Keys));
            Assert.All(ControllerActions.MainControls.Keys, id => Assert.InRange(id, 200, 255));
            for (int code = 1; code <= 41; code++)
            {
                Assert.True(ControllerActions.Clicks.ContainsKey(code) || ControllerActions.Holds.ContainsKey(code), "action " + code);
            }
            Assert.Equal(GameKey.Mirrors, ControllerActions.Clicks[24]);
            Assert.Equal(GameKey.LookLeft, ControllerActions.Holds[31]);
            Assert.Equal(GameKey.LookRight, ControllerActions.Holds[32]);
            Assert.Equal(GameKey.Menu, ControllerActions.Clicks[41]);
            //The light horn is clicked by the controllers that don't know it can be held
            Assert.Equal(GameKey.LightHorn, ControllerActions.Clicks[4]);
            Assert.Equal(GameKey.LightHorn, ControllerActions.Holds[42]);
            Assert.Equal(GameKey.Activate, ControllerActions.Clicks[3]);
            Assert.Equal(GameKey.Activate, ControllerActions.Holds[43]);
        }

        [Fact]
        public void ActionsWithoutKeysAreUnbound()
        {
            Assert.Empty(mapper.UnboundActions());

            keyboard.Unbound.Add(GameKey.Map);
            keyboard.Unbound.Add(GameKey.Activate);

            keyboard.Unbound.Add(GameKey.AirHorn);

            //"Activate" is clicked by 3 and held by 43; the controls of the main screen are 200+
            Assert.Equal(new[] { 3, 19, 43, 207 }, mapper.UnboundActions());
        }

        [Fact]
        public void FirstMessageTakesTogglesWithoutClicks()
        {
            mapper.Apply(State(left: true, actions: (1, 3)));
            Assert.Empty(keyboard.Events);

            mapper.Apply(State(left: false, actions: (1, 4)));
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
            //Text protocol: the former sensitivity 50 of the server
            mapper.Apply(State(steering: 5));
            //60% of the way to 16384 + 5 * 34.7 * 50
            Assert.Equal(16384 + (int)(0.6 * 5 * 34.7 * 50), joystick.Steering);

            for (int i = 0; i < 20; i++) mapper.Apply(State(steering: 100));
            Assert.Equal(32768, joystick.Steering);
            for (int i = 0; i < 20; i++) mapper.Apply(State(steering: -100));
            Assert.Equal(0, joystick.Steering);
        }

        [Fact]
        public void FinalSteeringOfProtocol2IsAppliedAtOnceWithFullLockAtGravity()
        {
            //The phone filters the steering, the server doesn't smooth it again
            mapper.Apply(new ControllerMessage { Steering = 9.80665 / 2, SteeringIsFinal = true });
            Assert.Equal(16384 + 8192, joystick.Steering);

            mapper.Apply(new ControllerMessage { Steering = -9.80665, SteeringIsFinal = true });
            Assert.Equal(0, joystick.Steering);
        }

        [Fact]
        public void ActionsAreFoundByIdWhateverTheirOrder()
        {
            mapper.Apply(State());
            mapper.Apply(State(actions: new[] { (19, 1), (9, 2), (200, 1) }));

            Assert.Equal(new[] { "click Map", "click RetarderUp", "click RetarderUp" }, keyboard.Events);
        }

        [Fact]
        public void HoldActionKeepsItsKeyPressed()
        {
            mapper.Apply(State());
            mapper.Apply(State(actions: (11, 1)));
            mapper.Apply(State(actions: (11, 1)));
            mapper.Apply(State());

            Assert.Equal(new[] { "press EngineBrake", "release EngineBrake" }, keyboard.Events);
        }

        [Fact]
        public void HeldActionIsReleasedWithControls()
        {
            mapper.Apply(State());
            mapper.Apply(State(actions: (11, 1)));
            keyboard.Clear();

            mapper.ReleaseControls();

            Assert.Contains("release EngineBrake", keyboard.Events);
        }

        [Fact]
        public void PedalLevelsAreAxes()
        {
            mapper.Apply(new ControllerMessage { HasPedalLevels = true, GasLevel = 0.5, BrakeLevel = 2 });

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
