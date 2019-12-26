using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Globalization;

namespace TruckRemoteServer
{
    public partial class ControlMappingForm : Form
    {
        MainForm Main_Form = Application.OpenForms.OfType<MainForm>().Single(); //Acessing Main Form
        Dictionary<string, short[]> localControlMapping;

        public ControlMappingForm()
        {
            InitializeComponent();

            CreateFormControls(); //Create controls
            LoadControlMapping(); //Loading existing Controll Mapping
        }

        private void CreateFormControls()
        {
            localControlMapping = new Dictionary<string, short[]>(Main_Form.server.pcController.ControlMapping);
            List<string> keyList = new List<string>(localControlMapping.Keys);
            int row = 0;
            //Sorting controls between Pages and top-down
            Dictionary<string, string[]> ControlsGroups = new Dictionary<string, string[]>();
            ControlsGroups.Add("Truck", new string[] { "Throttle", "Brake", "ParkingBrake", "LeftIndicator", "RightIndicator", "HazardLight", "LightModes", "HighBeam", "AirHorn", "Horn", "CruiseControl" });
            ControlsGroups.Add("Hud", new string[] {  });
            ControlsGroups.Add("Camera", new string[] {  });
            ControlsGroups.Add("Other", new string[] {  });

            foreach (TabPage tempTab in tabControlControlSections.TabPages)
            {
                //Create Table layout!
                //Here
                //
                string PageName = tempTab.Name.Substring(7, tempTab.Name.Length - 7);
                if( ControlsGroups[PageName].Length > 0)
                {
                    foreach (string Key in ControlsGroups[PageName])
                    {
                        //Create label!

                        //Creating Button
                        ControlMappingButton tempButton = new ControlMappingButton();
                        tempButton.Text = "New";

                        //Style
                        tempButton.Name = "button" + Key;
                        tempButton.Text = row.ToString();
                        tempButton.Dock = DockStyle.Fill;
                        tempButton.Margin = new Padding(0);
                        //Actions
                        tempButton.Click += buttonGetKeyCode_Click;

                        tableLayoutPanelTruckControls.Controls.Add(tempButton, 1, row); //Add

                        row++;
                    }
                }
            }
        }

        private void LoadControlMapping() //
        {
            foreach (KeyValuePair<string, short[]> CurrentPair in localControlMapping)
            {
                Button CurrentButton = this.Controls.Find("button" + CurrentPair.Key, true)[0] as Button;
                if (CurrentPair.Value[0] == 0)
                    CurrentButton.Text = "";
                else
                    CurrentButton.Text = "Ex";


                int scanCode = CurrentPair.Value[1];  // Get Scan code
                uint virtualCode = MapVirtualKey(Convert.ToUInt32(scanCode), MAPVK_VSC_TO_VK); // Get Virtual code

                byte[] kbState = new byte[256]; 
                GetKeyboardState(kbState); //Keyboard state
                //Letters
                StringBuilder buffer = new StringBuilder(new string(' ', 256));
                int ToUnicodeExFlag = ToUnicodeEx(virtualCode, (uint)scanCode, kbState, buffer, 256, 0, 0); // get char in Current Layout/language
                string keyName = buffer.ToString().ToUpper(); // Convert to string
                //Other keys
                int lParam = 0;
                string slParam = "c" + CurrentPair.Value[0].ToString() + scanCode.ToString("x2") + "0001"; //lParam
                int.TryParse(slParam, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lParam); //converting to int

                if (ToUnicodeExFlag == 0 || virtualCode == 32 || virtualCode == 9 || virtualCode == 8 | virtualCode == 13 || virtualCode == 27) // Not characters and Space, Mod keys ...
                {
                    var sb = new StringBuilder(256);
                    GetKeyNameText(lParam, sb, 256);
                    keyName = sb.ToString();
                }

                CurrentButton.Text += "SC = 0x" + CurrentPair.Value[1].ToString("X2") + " | Key " + keyName + " (0x" + virtualCode.ToString("X2") + ")"; // Resulting text
            }
        }

        private void buttonGetKeyCode_Click(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            SenderButton.Text = "Waiting for Input";
            //ReAdd events
            SenderButton.KeyUp += button1_KeyUp;
            SenderButton.Click -= buttonGetKeyCode_Click;
        }

        private void button1_KeyUp(object sender, KeyEventArgs e)
        {
            ControlMappingButton SenderButton = sender as ControlMappingButton;

            label1.Focus(); //remove focus
            //ReAdd events
            SenderButton.KeyUp -= button1_KeyUp;
            SenderButton.Click += buttonGetKeyCode_Click;
            ProcessKeyDetection(SenderButton);
        }

        private void ProcessKeyDetection(ControlMappingButton _inputButton)//, uint _VKcode)
        {
            string buttonName = _inputButton.Name.Substring(6);
            SetKey(buttonName, _inputButton.ScanCodes );
        }

        private void SetKey(string _KeyName, short[] _keySacanCode) //Saving
        {
            localControlMapping[_KeyName] = _keySacanCode; //Saving Scan code
        }

        private void buttonControlMappingSave_Click(object sender, EventArgs e)
        {
            Main_Form.server.pcController.ControlMapping = localControlMapping;
        }


        //----------------------------------//
        //Import 
        //Flags
        private const uint MAPVK_VSC_TO_VK = 1;
        //
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "MapVirtualKey", SetLastError = true, ThrowOnUnmappableChar = false)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        //
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetKeyboardState(byte[] lpKeyState);
        //
        [DllImport("user32.dll", EntryPoint = "GetKeyNameTextW", CharSet = CharSet.Unicode)]
        private static extern int GetKeyNameText(int lParam, [MarshalAs(UnmanagedType.LPWStr), Out] StringBuilder str, int size);
        //
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicodeEx(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 256)] StringBuilder receivingBuffer, int bufferSize, uint flags, int dwhkl);
        //
        //----------------------------------//

    }
}
