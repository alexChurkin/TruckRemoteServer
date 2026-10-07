using System;
using System.Collections.Generic;
using System.Linq;
using TruckRemoteServer.Protocol;
using TruckRemoteServer.Telemetry;

namespace TruckRemoteServer.Input
{
    //Turns controller messages into game input: steering and analog pedals are joystick axes,
    //the rest are keys. Toggles of the controller are flipped on every click, so a click is made on every change
    public class ControllerInputMapper
    {
        //Text protocol: part of the new steering value applied at once, smooths the jitter of the phone sensor
        private const double SteeringSmoothing = 0.6;
        //Text protocol: steering value (m/s²) times sensitivity gives the axis offset.
        //The sensitivity was a setting of the server, now it's set on the phone
        private const double SteeringScale = 34.7;
        private const int LegacySensitivity = 50;
        //Protocol 2: the phone applies its steering settings, gravity (m/s²) is the full lock
        private const double FullLockSteering = 9.80665;

        private readonly IKeyboard keyboard;
        private readonly IVirtualJoystick joystick;
        private readonly ActionCounters actionCounters = new ActionCounters();
        //Keys of held actions that are pressed now
        private readonly HashSet<GameKey> heldKeys = new HashSet<GameKey>();
        private readonly object inputLock = new object();

        private volatile TruckTelemetry truck = TruckTelemetry.Unknown;
        private volatile ControlsSnapshot controls = ControlsSnapshot.Released;

        private int steeringAxis = JoystickAxis.Center;
        private bool brakePressed, gasPressed;
        private int horn;
        private bool leftBlinker, rightBlinker, hazardLights, parkingBrake, lights, cruise;

        //Toggle states of a new controller are taken as they are, without clicks
        private bool syncToggles = true;

        public ControllerInputMapper(IKeyboard keyboard, IVirtualJoystick joystick)
        {
            this.keyboard = keyboard;
            this.joystick = joystick;
        }

        //The steering and the pedals as they were applied last (read by the window on its thread)
        public ControlsSnapshot Controls => controls;

        //The ids of the panel's actions (see ControllerActions) the player has no key for in the game, in order
        public IReadOnlyList<int> UnboundActions()
        {
            return ControllerActions.Clicks.Concat(ControllerActions.Holds)
                .Where(action => !keyboard.HasKey(action.Value))
                .Select(action => action.Key)
                .OrderBy(id => id)
                .ToList();
        }

        //The lights button needs to know the current lights of the truck
        public void UpdateTelemetry(TruckTelemetry telemetry)
        {
            truck = telemetry ?? TruckTelemetry.Unknown;
        }

        public void OnControllerConnected()
        {
            lock (inputLock)
            {
                if (!joystick.IsAvailable) joystick.Initialize();
                ReleaseControlsLocked();
                syncToggles = true;
            }
        }

        //Releases all held keys and centers the steering
        public void ReleaseControls()
        {
            lock (inputLock)
            {
                ReleaseControlsLocked();
            }
        }

        public void Apply(ControllerMessage state)
        {
            lock (inputLock)
            {
                ApplySteering(state.Steering, state.SteeringIsFinal);
                SetKey(GameKey.Brake, state.BrakePressed, ref brakePressed);
                SetKey(GameKey.Gas, state.GasPressed, ref gasPressed);
                SetHorn(state.Horn);
                if (state.HasPedalLevels) joystick.SetPedals(ToPedalAxis(state.GasLevel), ToPedalAxis(state.BrakeLevel));
                controls = new ControlsSnapshot(
                    (double)(steeringAxis - JoystickAxis.Center) / JoystickAxis.Center,
                    state.HasPedalLevels ? Level(state.GasLevel) : (state.GasPressed ? 1 : 0),
                    state.HasPedalLevels ? Level(state.BrakeLevel) : (state.BrakePressed ? 1 : 0));

                //Toggle values are only synchronized on the first message after (re)connect,
                //otherwise their difference with the previous session would cause false clicks
                if (syncToggles)
                {
                    syncToggles = false;
                    leftBlinker = state.LeftSignalClick;
                    rightBlinker = state.RightSignalClick;
                    hazardLights = state.EmergencyClick;
                    parkingBrake = state.ParkingBrakeClick;
                    lights = state.LightsClick;
                    cruise = state.CruiseClick;
                    actionCounters.Sync(state.Actions);
                    return;
                }

                ApplyActions(state.Actions);
                Toggle(GameKey.LeftBlinker, state.LeftSignalClick, ref leftBlinker);
                Toggle(GameKey.RightBlinker, state.RightSignalClick, ref rightBlinker);
                Toggle(GameKey.HazardLights, state.EmergencyClick, ref hazardLights);
                Toggle(GameKey.ParkingBrake, state.ParkingBrakeClick, ref parkingBrake);
                if (state.LightsClick != lights)
                {
                    lights = state.LightsClick;
                    SwitchLights();
                }
                Toggle(GameKey.CruiseControl, state.CruiseClick, ref cruise);
            }
        }

        private void ApplyActions(IReadOnlyDictionary<int, int> actions)
        {
            foreach (KeyValuePair<int, int> clicks in actionCounters.Update(actions))
            {
                if (!ControllerActions.Clicks.TryGetValue(clicks.Key, out GameKey key)) continue;
                for (int c = 0; c < clicks.Value; c++) keyboard.Click(key);
            }
            foreach (KeyValuePair<int, GameKey> hold in ControllerActions.Holds)
            {
                bool held = actions.TryGetValue(hold.Key, out int value) && value != 0;
                if (held == heldKeys.Contains(hold.Value)) continue;
                if (held)
                {
                    heldKeys.Add(hold.Value);
                    keyboard.Press(hold.Value);
                }
                else
                {
                    heldKeys.Remove(hold.Value);
                    keyboard.Release(hold.Value);
                }
            }
        }

        private void ReleaseControlsLocked()
        {
            foreach (GameKey key in heldKeys) keyboard.Release(key);
            heldKeys.Clear();
            SetKey(GameKey.Brake, false, ref brakePressed);
            SetKey(GameKey.Gas, false, ref gasPressed);
            SetHorn(0);
            steeringAxis = JoystickAxis.Center;
            joystick.SetSteering(JoystickAxis.Center);
            joystick.SetPedals(0, 0);
            controls = ControlsSnapshot.Released;
        }

        private void ApplySteering(double value, bool final)
        {
            double offset = final
                ? value / FullLockSteering * JoystickAxis.Center
                : value * SteeringScale * LegacySensitivity;
            int target = JoystickAxis.Center + (int)Math.Round(offset);
            //Protocol 2: the phone filters the steering itself (adaptively), smoothing here would only add a lag
            int smoothed = final ? target : (int)(steeringAxis + SteeringSmoothing * (target - steeringAxis));
            //Axis range is 0..32768, out of range values must not reach vJoy
            steeringAxis = Math.Max(0, Math.Min(2 * JoystickAxis.Center, smoothed));
            joystick.SetSteering(steeringAxis);
        }

        private static int ToPedalAxis(double level)
        {
            return (int)(Level(level) * JoystickAxis.Max);
        }

        private static double Level(double level)
        {
            return double.IsNaN(level) ? 0 : Math.Max(0, Math.Min(1, level));
        }

        private void SetKey(GameKey key, bool pressed, ref bool current)
        {
            if (pressed == current) return;
            current = pressed;
            if (pressed) keyboard.Press(key);
            else keyboard.Release(key);
        }

        private void Toggle(GameKey key, bool value, ref bool current)
        {
            if (value == current) return;
            current = value;
            keyboard.Click(key);
        }

        //0 - off, 1 - horn, 2 - air horn; the previous horn is released when switching between them
        private void SetHorn(int value)
        {
            if (value == horn) return;
            keyboard.Release(GameKey.Horn);
            keyboard.Release(GameKey.AirHorn);
            if (value == 1) keyboard.Press(GameKey.Horn);
            else if (value == 2) keyboard.Press(GameKey.AirHorn);
            horn = value;
        }

        //The lights button cycles off -> parking lights -> low beam -> high beam -> off.
        //In the game L cycles parking lights/low beam/off and K toggles high beam, so the keys depend on the current lights.
        //Before the telemetry is read for the first time the lights are unknown and nothing is clicked
        private void SwitchLights()
        {
            TruckTelemetry current = truck;
            if (current == TruckTelemetry.Unknown) return;

            if (!current.ParkingLights)
            {
                keyboard.Click(GameKey.Lights);
            }
            else if (!current.LowBeam)
            {
                keyboard.Click(GameKey.Lights);
                if (current.HighBeam) keyboard.Click(GameKey.HighBeam);
            }
            else if (!current.HighBeam)
            {
                keyboard.Click(GameKey.HighBeam);
            }
            else
            {
                keyboard.Click(GameKey.HighBeam);
                keyboard.Click(GameKey.Lights);
            }
        }
    }
}
