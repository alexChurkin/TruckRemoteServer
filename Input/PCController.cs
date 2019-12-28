using System;

namespace TruckRemoteServer
{
    class PCController
    {
        //Controller
        public static int SteeringSensitivity = 50;
        public int prevXAxisValue;
        private bool prevBreakClicked, prevGasClicked;
        private bool prevLeftSignal, prevRightSignal;
        private bool wasParkingBreakEnabled;
        private int prevLightsState;
        private int prevHornState;
        private bool prevCruise;

        private IFfbListener ffbListener;


        private int lastConditionNumber = -1;

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

        public void SetControllerStartValues(bool leftSignal, bool rightSignal, bool isParking, int lightsState)
        {
            prevLeftSignal = leftSignal;
            prevRightSignal = rightSignal;
            wasParkingBreakEnabled = isParking;
            prevLightsState = lightsState;
        }

        public void UpdateAccelerometerValue(double accelerometerValue)
        {
            int roughValue = 16384 + (int)(accelerometerValue * 34.7 * SteeringSensitivity);
            int newXAxisValue = (int)(prevXAxisValue + 0.6 * (roughValue - prevXAxisValue));
            prevXAxisValue = newXAxisValue;
            InputEmulator.SetXAxis(newXAxisValue);
        }

        public void UpdateBreakGasState(bool breakClicked, bool gasClicked)
        {
            if (breakClicked != prevBreakClicked)
            {
                if (breakClicked)
                {
                    InputEmulator.KeyPress(DIK_DOWN_ARROW_SCAN);
                }
                else
                {
                    InputEmulator.KeyRelease(DIK_DOWN_ARROW_SCAN);
                }
                prevBreakClicked = breakClicked;
            }
            //Gas
            if (gasClicked != prevGasClicked)
            {
                if (gasClicked)
                {
                    InputEmulator.KeyPress(DIK_UP_ARROW_SCAN);
                }
                else
                {
                    InputEmulator.KeyRelease(DIK_UP_ARROW_SCAN);
                }
                prevGasClicked = gasClicked;
            }
        }

        public void UpdateTurnSignals(bool leftSignal, bool rightSignal)
        {
            //All was disabled
            if (!prevLeftSignal && !prevRightSignal)
            {
                //Enabling emergency signal
                if (leftSignal && rightSignal)
                {
                    if (lastConditionNumber != 0)
                    {
                        ToggleEmergencySignal();
                        lastConditionNumber = 0;
                    }
                }
                //Enabling left
                else if (leftSignal)
                {
                    if (lastConditionNumber != 1)
                    {
                        ToggleLeftTurnSignal();
                        lastConditionNumber = 1;
                    }
                }
                //Enabling right
                else if (rightSignal)
                {
                    if (lastConditionNumber != 2)
                    {
                        ToggleRightTurnSignal();
                        lastConditionNumber = 2;
                    }
                }

            }
            //All was enabled
            else if (prevLeftSignal && prevRightSignal)
            {
                //Disabling emergency signal
                if (!leftSignal && !rightSignal)
                {
                    if (lastConditionNumber != 3)
                    {
                        ToggleEmergencySignal();
                        lastConditionNumber = 3;
                    }
                }
            }
            //Left was enabled
            else if (prevLeftSignal)
            {
                //Enabling emergency signal with disabling turn signal
                if (leftSignal && rightSignal)
                {
                    if (lastConditionNumber != 4)
                    {
                        ToggleLeftTurnSignal();
                        ToggleEmergencySignal();
                        lastConditionNumber = 4;
                    }
                }
                //Enabling right
                else if (rightSignal)
                {
                    if (lastConditionNumber != 5)
                    {
                        ToggleRightTurnSignal();
                        lastConditionNumber = 5;
                    }
                }
                //Disabling left
                else if (!leftSignal)
                {
                    if (lastConditionNumber != 6)
                    {
                        ToggleLeftTurnSignal();
                        lastConditionNumber = 6;
                    }
                }
            }
            //Right was enabled
            else
            {
                if (leftSignal && rightSignal)
                {
                    if (lastConditionNumber != 7)
                    {
                        ToggleRightTurnSignal();
                        ToggleEmergencySignal();
                        lastConditionNumber = 7;
                    }
                }
                else if (leftSignal)
                {
                    if (lastConditionNumber != 8)
                    {
                        ToggleLeftTurnSignal();
                        lastConditionNumber = 8;
                    }
                }
                else if (!rightSignal)
                {
                    if (lastConditionNumber != 9)
                    {
                        ToggleRightTurnSignal();
                        lastConditionNumber = 9;
                    }
                }
            }

            prevLeftSignal = leftSignal;
            prevRightSignal = rightSignal;
        }

        private void ToggleLeftTurnSignal()
        {
            InputEmulator.KeyClick(DIK_OPEN_BRACKET_SCAN);
        }

        private void ToggleRightTurnSignal()
        {
            InputEmulator.KeyClick(DIK_CLOSE_BRACKET_SCAN);
        }

        private void ToggleEmergencySignal()
        {
            InputEmulator.KeyClick(DIK_F_SCAN);
        }

        public void UpdateParkingBrake(bool isParkingBrakeEnabled)
        {
            if (wasParkingBreakEnabled != isParkingBrakeEnabled)
            {
                wasParkingBreakEnabled = isParkingBrakeEnabled;
                InputEmulator.KeyClick(DIK_SPACE_SCAN);
            }
        }

        public void UpdateLights(int lightsState)
        {
            if(lightsState != prevLightsState)
            {
                prevLightsState = lightsState;

                switch(lightsState)
                {
                    case 0:
                        InputEmulator.KeyClick(DIK_K_SCAN);
                        InputEmulator.KeyClick(DIK_L_SCAN);
                        break;
                    case 1:
                        InputEmulator.KeyClick(DIK_L_SCAN);
                        break;
                    case 2:
                        InputEmulator.KeyClick(DIK_L_SCAN);
                        break;
                    case 3:
                        InputEmulator.KeyClick(DIK_K_SCAN);
                        break;
                }
            }
        }

        public void UpdateHorn(int hornState)
        {
            if(hornState != prevHornState)
            {
                switch(hornState)
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
            if (prevCruise != isCruise)
            {
                prevCruise = isCruise;
                InputEmulator.KeyClick(DIK_C_SCAN);
            }
        }

        /* Panel */
        public void SetPanelStartValues(bool diffBlock, int wipersState, bool liftingAxle, bool flashingBeacon)
        {
            prevDiffBlock = diffBlock;
            prevWipersState = wipersState;
            prevLiftingAxle = liftingAxle;
            prevFlashingBeacon = flashingBeacon;
        }

        public void UpdateDiffBlock(bool diffBlock)
        {
            if(prevDiffBlock != diffBlock)
            {
                prevDiffBlock = diffBlock;
                InputEmulator.KeyClick(DIK_V_SCAN);
            }
        }

        public void UpdateWipers(int wipersState)
        {
            if (prevWipersState != wipersState)
            {
                prevWipersState = wipersState;
                InputEmulator.KeyClick(DIK_P_SCAN);
            }
        }

        public void UpdateLiftingAxle(bool liftingAxle)
        {
            if (prevLiftingAxle != liftingAxle)
            {
                prevLiftingAxle = liftingAxle;
                InputEmulator.KeyClick(DIK_U_SCAN);
            }
        }

        public void UpdateFlashingBeacon(bool flashingBeacon)
        {
            if (prevFlashingBeacon != flashingBeacon)
            {
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