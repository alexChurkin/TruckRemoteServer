using TruckRemoteServer.Data;

namespace TruckRemoteServer
{
    class PCController
    {
        public static int SteeringSensitivity = 50;

        //Controller-dependent previous data
        public int prevXAxisValue;
        private bool prevBreakPressed, prevGasPressed;
        private int prevHornState;
        private bool prevParkingBreakState;
        private bool prevCruiseState;
        private bool prevLightsState;
        private bool prevLeftSignalState, prevRightSignalState;
        private bool prevEmergencyState;

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

        //Panel
        private bool prevDiffBlock;
        private int prevWipersState;
        private bool prevLiftingAxle;
        private bool prevFlashingBeacon;

        private const int DIK_V_SCAN = 0x2F;
        private const int DIK_P_SCAN = 0x19;
        private const int DIK_U_SCAN = 0x16;
        private const int DIK_O_SCAN = 0x18;

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
        }

        public void UpdateTelemetryData(IEts2TelemetryData telemetry)
        {
            this.telemetry = telemetry;
        }

        public void UpdateAccelerometerValue(double accelerometerValue)
        {
            int roughValue = 16384 + (int)(accelerometerValue * 34.7 * SteeringSensitivity);
            int newXAxisValue = (int)(prevXAxisValue + 0.6 * (roughValue - prevXAxisValue));
            prevXAxisValue = newXAxisValue;
            InputEmulator.SetXAxis(newXAxisValue);
        }

        public void UpdateBreakGasState(bool breakPressed, bool gasPressed)
        {
            if (breakPressed != prevBreakPressed)
            {
                if (breakPressed)
                {
                    Logger.Log($"Break pedal: press");
                    InputEmulator.KeyPress(DIK_DOWN_ARROW_SCAN);
                }
                else
                {
                    Logger.Log($"Break pedal: release");
                    InputEmulator.KeyRelease(DIK_DOWN_ARROW_SCAN);
                }
                prevBreakPressed = breakPressed;
            }
            //Gas
            if (gasPressed != prevGasPressed)
            {
                if (gasPressed)
                {
                    Logger.Log($"Gas pedal: press");
                    InputEmulator.KeyPress(DIK_UP_ARROW_SCAN);
                }
                else
                {
                    Logger.Log($"Gas pedal: release");
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
                Logger.Log($"Left blinker: click");
                ClickLeftTurnSignal();
            }

            //Right signal click
            if (rightSignal != prevRightSignalState)
            {
                prevRightSignalState = rightSignal;
                Logger.Log($"Right blinker: click");
                ClickRightTurnSignal();
            }

            //Emergency click
            if(emergencySignal != prevEmergencyState)
            {
                prevEmergencyState = emergencySignal;
                Logger.Log($"Emergency: click");
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
                Logger.Log($"Parking brake: click");
                InputEmulator.KeyClick(DIK_SPACE_SCAN);
            }
        }

        public void UpdateLights(bool lightsState)
        {
            if (telemetry == null) return;

            if (lightsState != prevLightsState)
            {
                Logger.Log($"Lights: clicked");
                prevLightsState = lightsState;

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
                Logger.Log($"Horn: clicked");
                switch (hornState)
                {
                    case 2:
                        InputEmulator.KeyPress(DIK_N_SCAN);
                        break;
                    case 1:
                        InputEmulator.KeyPress(DIK_H_SCAN);
                        break;
                    default:
                        InputEmulator.KeyRelease(DIK_H_SCAN);
                        InputEmulator.KeyRelease(DIK_N_SCAN);
                        break;
                }
                prevHornState = hornState;
            }
        }

        public void UpdateCruise(bool isCruise)
        {
            if (prevCruiseState != isCruise)
            {
                Logger.Log($"Cruise control: clicked");
                prevCruiseState = isCruise;
                InputEmulator.KeyClick(DIK_C_SCAN);
            }
        }

        public void UpdateDiffBlock(bool diffBlock)
        {
            if(prevDiffBlock != diffBlock)
            {
                Logger.Log($"Diff block: clicked");
                prevDiffBlock = diffBlock;
                InputEmulator.KeyClick(DIK_V_SCAN);
            }
        }

        public void UpdateWipers(int wipersState)
        {
            if (prevWipersState != wipersState)
            {
                Logger.Log($"Wipers: clicked");
                prevWipersState = wipersState;
                InputEmulator.KeyClick(DIK_P_SCAN);
            }
        }

        public void UpdateLiftingAxle(bool liftingAxle)
        {
            if (prevLiftingAxle != liftingAxle)
            {
                Logger.Log($"Lifting axle: clicked");
                prevLiftingAxle = liftingAxle;
                InputEmulator.KeyClick(DIK_U_SCAN);
            }
        }

        public void UpdateFlashingBeacon(bool flashingBeacon)
        {
            if (prevFlashingBeacon != flashingBeacon)
            {
                Logger.Log($"Flashing beacon: clicked");
                prevFlashingBeacon = flashingBeacon;
                InputEmulator.KeyClick(DIK_O_SCAN);
            }
        }

        public void Release()
        {
            InputEmulator.ReleaseJoy();
        }
    }
}