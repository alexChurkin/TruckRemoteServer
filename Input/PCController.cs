using System;
using TruckRemoteServer.Data;

namespace TruckRemoteServer
{
    class PCController
    {
        public static int SteeringSensitivity = 50;

        private const int X_AXIS_CENTER = 16384;
        private const int PEDAL_AXIS_MAX = 32767;

        //Keys of additional actions in the order the controller sends their counters:
        //engine (E), trailer (T), activate (Enter), wipers (P), differential lock (V),
        //lift axle (U), beacon (O), light horn (J)
        private static readonly short[] ACTION_SCANCODES = { 0x12, 0x14, 0x1C, 0x19, 0x2F, 0x16, 0x18, 0x24 };
        public static int ActionsCount => ACTION_SCANCODES.Length;
        //More clicks at once are considered as broken counter
        private const int MAX_ACTION_CLICKS = 5;
        private readonly int[] prevActionCounters = new int[ACTION_SCANCODES.Length];

        //Controller-dependent previous data
        public int prevXAxisValue;
        private bool prevBreakPressed, prevGasPressed;
        private int prevHornState;
        private bool prevParkingBreakState;
        private bool prevCruiseState;
        private bool prevLightsState;
        private bool prevLeftSignalState, prevRightSignalState;
        private bool prevEmergencyState;

        //Toggle states of new controller should be taken as they are, without clicks
        private bool syncToggles = true;

        private volatile IEts2TelemetryData telemetry;
        private readonly IFfbListener ffbListener;

        private byte DIK_UP_ARROW_SCAN = 0xC8;
        private const int DIK_DOWN_ARROW_SCAN = 0xD0;
        private byte DIK_OPEN_BRACKET_SCAN = 0x1A;
        private byte DIK_CLOSE_BRACKET_SCAN = 0x1B;
        private byte DIK_F_SCAN = 0x21;
        private byte DIK_SPACE_SCAN = 0x39;
        private byte DIK_L_SCAN = 0x26;
        private byte DIK_K_SCAN = 0x25;
        private byte DIK_H_SCAN = 0x23;
        private byte DIK_N_SCAN = 0x31;
        private byte DIK_C_SCAN = 0x2E;

        public PCController(IFfbListener ffbListener)
        {
            this.ffbListener = ffbListener;
        }

        /* Controller */
        public void OnRemoteControlConnected()
        {
            if (!InputEmulator.IsJoyInitialized())
            {
                InputEmulator.InitJoy(ffbListener);
            }
            ReleaseControls();
            syncToggles = true;
        }

        //Releases all held keys and centers the steering
        public void ReleaseControls()
        {
            UpdateBreakGasState(false, false);
            UpdateHorn(0);
            prevXAxisValue = X_AXIS_CENTER;
            InputEmulator.SetXAxis(X_AXIS_CENTER);
            InputEmulator.SetPedalAxes(0, 0);
        }

        //Returns true if toggles were synchronized (and no clicks should be made this time)
        public bool SyncTogglesIfNeeded(bool leftSignal, bool rightSignal, bool emergencySignal,
            bool parkingBrake, bool lights, bool cruise, int[] actionCounters)
        {
            if (!syncToggles) return false;
            syncToggles = false;

            prevLeftSignalState = leftSignal;
            prevRightSignalState = rightSignal;
            prevEmergencyState = emergencySignal;
            prevParkingBreakState = parkingBrake;
            prevLightsState = lights;
            prevCruiseState = cruise;
            Array.Copy(actionCounters, prevActionCounters,
                Math.Min(actionCounters.Length, prevActionCounters.Length));
            return true;
        }

        //Levels are from 0 to 1 (axes are used for analog pedals)
        public void UpdatePedalLevels(double gasLevel, double brakeLevel)
        {
            InputEmulator.SetPedalAxes(ToPedalAxis(gasLevel), ToPedalAxis(brakeLevel));
        }

        private static int ToPedalAxis(double level)
        {
            if (double.IsNaN(level)) return 0;
            return (int)(Math.Max(0, Math.Min(1, level)) * PEDAL_AXIS_MAX);
        }

        //Every action has a counter of clicks on controller's side
        public void UpdateActions(int[] actionCounters)
        {
            int count = Math.Min(actionCounters.Length, prevActionCounters.Length);
            for (int i = 0; i < count; i++)
            {
                int clicks = actionCounters[i] - prevActionCounters[i];
                prevActionCounters[i] = actionCounters[i];
                if (clicks <= 0 || clicks > MAX_ACTION_CLICKS) continue;

                for (int c = 0; c < clicks; c++)
                {
                    InputEmulator.KeyClick(ACTION_SCANCODES[i]);
                }
            }
        }

        public void UpdateTelemetryData(IEts2TelemetryData telemetry)
        {
            this.telemetry = telemetry;
        }

        public void UpdateAccelerometerValue(double accelerometerValue)
        {
            int roughValue = X_AXIS_CENTER + (int)(accelerometerValue * 34.7 * SteeringSensitivity);
            int newXAxisValue = (int)(prevXAxisValue + 0.6 * (roughValue - prevXAxisValue));
            //Axis range is 0..32768, out of range values must not reach vJoy
            newXAxisValue = Math.Max(0, Math.Min(2 * X_AXIS_CENTER, newXAxisValue));
            prevXAxisValue = newXAxisValue;
            InputEmulator.SetXAxis(newXAxisValue);
        }

        public void UpdateBreakGasState(bool breakPressed, bool gasPressed)
        {
            if (breakPressed != prevBreakPressed)
            {
                if (breakPressed)
                {
                    InputEmulator.KeyPress(DIK_DOWN_ARROW_SCAN);
                }
                else
                {
                    InputEmulator.KeyRelease(DIK_DOWN_ARROW_SCAN);
                }
                prevBreakPressed = breakPressed;
            }
            //Gas
            if (gasPressed != prevGasPressed)
            {
                if (gasPressed)
                {
                    InputEmulator.KeyPress(DIK_UP_ARROW_SCAN);
                }
                else
                {
                    InputEmulator.KeyRelease(DIK_UP_ARROW_SCAN);
                }
                prevGasPressed = gasPressed;
            }
        }

        public void UpdateTurnSignals(bool leftSignal, bool rightSignal, bool emergencySignal)
        {
            //Left signal click
            if (leftSignal != prevLeftSignalState)
            {
                prevLeftSignalState = leftSignal;
                ClickLeftTurnSignal();
            }

            //Right signal click
            if (rightSignal != prevRightSignalState)
            {
                prevRightSignalState = rightSignal;
                ClickRightTurnSignal();
            }

            //Emergency click
            if(emergencySignal != prevEmergencyState)
            {
                prevEmergencyState = emergencySignal;
                ClickEmergencySignal();
            }
        }

        private void ClickLeftTurnSignal()
        {
            InputEmulator.KeyClick(DIK_OPEN_BRACKET_SCAN);
        }

        private void ClickRightTurnSignal()
        {
            InputEmulator.KeyClick(DIK_CLOSE_BRACKET_SCAN);
        }

        private void ClickEmergencySignal()
        {
            InputEmulator.KeyClick(DIK_F_SCAN);
        }

        public void UpdateParkingBrake(bool isParkingBrakeEnabled)
        {
            if (prevParkingBreakState != isParkingBrakeEnabled)
            {
                prevParkingBreakState = isParkingBrakeEnabled;
                InputEmulator.KeyClick(DIK_SPACE_SCAN);
            }
        }

        public void UpdateLights(bool lightsState)
        {
            if (lightsState != prevLightsState)
            {
                prevLightsState = lightsState;
                if (telemetry == null) return;

                var truck = telemetry.Truck;

                if(!truck.LightsParkingOn)
                {
                    InputEmulator.KeyClick(DIK_L_SCAN);
                }
                else if(!truck.LightsBeamLowOn)
                {
                    InputEmulator.KeyClick(DIK_L_SCAN);
                    if(truck.LightsBeamHighOn)
                    {
                        InputEmulator.KeyClick(DIK_K_SCAN);
                    }
                }
                else if(!truck.LightsBeamHighOn)
                {
                    InputEmulator.KeyClick(DIK_K_SCAN);
                }
                else
                {
                    InputEmulator.KeyClick(DIK_K_SCAN);
                    InputEmulator.KeyClick(DIK_L_SCAN);
                }
            }
        }

        public void UpdateHorn(int hornState)
        {
            if(hornState != prevHornState)
            {
                //Previous horn must be released when switching between horns
                InputEmulator.KeyRelease(DIK_H_SCAN);
                InputEmulator.KeyRelease(DIK_N_SCAN);
                switch(hornState)
                {
                    case 2:
                        InputEmulator.KeyPress(DIK_N_SCAN);
                        break;
                    case 1:
                        InputEmulator.KeyPress(DIK_H_SCAN);
                        break;
                }
                prevHornState = hornState;
            }
        }

        public void UpdateCruise(bool isCruise)
        {
            if (prevCruiseState != isCruise)
            {
                prevCruiseState = isCruise;
                InputEmulator.KeyClick(DIK_C_SCAN);
            }
        }

        public void Release()
        {
            InputEmulator.ReleaseJoy();
        }
    }
}