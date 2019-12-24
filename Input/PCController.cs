using System;
using System.Collections.Generic;

namespace TruckRemoteServer
{
    class PCController
    {
        public Dictionary<string, short[]> ControlMapping = new Dictionary<string, short[]>();

        //Controller
        public static int SteeringSensitivity = 50;
        public int prevXAxisValue;
        private bool prevBreakClicked, prevGasClicked;
        private bool prevLeftSignal, prevRightSignal;
        private bool wasParkingBreakEnabled;
        private int prevLightsState;
        private int prevHornState;
        private bool prevCruise;

        private int lastConditionNumber = -1;
        
        //Panel
        private bool prevDiffBlock;
        private int prevWipersState;
        private bool prevLiftingAxle;
        private bool prevFlashingBeacon;

        private short[] DIK_V_SCAN = { 0, 0x2F };
        private short[] DIK_P_SCAN = { 0, 0x19 };
        private short[] DIK_U_SCAN = { 0, 0x16 };
        private short[] DIK_O_SCAN = { 0, 0x18 };


        /* Controller */
        public void InitializeJoyIfNeccessary()
        {
            if (!InputEmulator.IsJoyInitialized())
            {
                InputEmulator.InitJoy();
            }
        }

        public void initializeKeyMapping()
        {
            ControlMapping.Add("Throttle", new short[] {1, 0x48 });
            ControlMapping.Add("Brake", new short[] { 1, 0x50 });
            ControlMapping.Add("ParkingBrake", new short[] { 0, 0x39 });
            ControlMapping.Add("LeftIndicator", new short[] { 0, 0x1A });
            ControlMapping.Add("RightIndicator", new short[] { 0, 0x1B });
            ControlMapping.Add("HazardLight", new short[] { 0, 0x21 });
            ControlMapping.Add("LightModes", new short[] { 0, 0x26 });
            ControlMapping.Add("HighBeam", new short[] { 0, 0x25 });
            ControlMapping.Add("AirHorn", new short[] { 0, 0x31 });
            ControlMapping.Add("Horn", new short[] { 0, 0x23 });
            ControlMapping.Add("CruiseControl", new short[] { 0, 0x2E });
            //ControlMapping.Add("", );
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
            int roughValue = 16384 + (int)(accelerometerValue * 34 * SteeringSensitivity);
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
                    InputEmulator.KeyPress(ControlMapping["Brake"]);
                }
                else
                {
                    InputEmulator.KeyRelease(ControlMapping["Brake"]);
                }
                prevBreakClicked = breakClicked;
            }
            //Gas
            if (gasClicked != prevGasClicked)
            {
                if (gasClicked)
                {
                    InputEmulator.KeyPress(ControlMapping["Throttle"]);
                }
                else
                {
                    InputEmulator.KeyRelease(ControlMapping["Throttle"]);
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
            InputEmulator.KeyClick(ControlMapping["LeftIndicator"]);
        }

        private void ToggleRightTurnSignal()
        {
            InputEmulator.KeyClick(ControlMapping["RightIndicator"]);
        }

        private void ToggleEmergencySignal()
        {
            InputEmulator.KeyClick(ControlMapping["HazardLight"]);
        }

        public void UpdateParkingBrake(bool isParkingBrakeEnabled)
        {
            if (wasParkingBreakEnabled != isParkingBrakeEnabled)
            {
                wasParkingBreakEnabled = isParkingBrakeEnabled;
                InputEmulator.KeyClick(ControlMapping["ParkingBrake"]);
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
                        InputEmulator.KeyClick(ControlMapping["HighBeam"]);
                        InputEmulator.KeyClick(ControlMapping["LightModes"]);
                        break;
                    case 1:
                        InputEmulator.KeyClick(ControlMapping["LightModes"]);
                        break;
                    case 2:
                        InputEmulator.KeyClick(ControlMapping["LightModes"]);
                        break;
                    case 3:
                        InputEmulator.KeyClick(ControlMapping["HighBeam"]);
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
                        InputEmulator.KeyPress(ControlMapping["AirHorn"]); 
                        break;
                    case 1:
                        InputEmulator.KeyPress(ControlMapping["Horn"]); 
                        break;
                    default:
                        InputEmulator.KeyRelease(ControlMapping["Horn"]); 
                        InputEmulator.KeyRelease(ControlMapping["AirHorn"]); 
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
                InputEmulator.KeyClick(ControlMapping["CruiseControl"]);
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