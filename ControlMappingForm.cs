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

        private void SetKey(string _KeyName, short[] _keySacanCode) //Saving
        {
            localControlMapping[_KeyName] = _keySacanCode; //Saving Scan code
        }

        private void CreateFormControls()
        {
            localControlMapping = new Dictionary<string, short[]>(Main_Form.server.pcController.ControlMapping);
            List<string> keyList = new List<string>(localControlMapping.Keys);
            int row = 0;

            foreach (string Key in keyList)
            {
                Button tempButton = new Button(); // new button
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

        private void LoadControlMapping() //
        {
            foreach (KeyValuePair<string, short[]> CurrentPair in localControlMapping)
            {
                Button CurrentButton = this.Controls.Find("button" + CurrentPair.Key, true)[0] as Button;
                //local
                StringBuilder locale = new StringBuilder(new string(' ', 256));
                string keyboardLanguage = InputLanguage.CurrentInputLanguage.LayoutName;
                GetKeyboardLayoutName(locale);
                keyboardLanguage = locale.ToString().Substring(0, locale.Length);

                IntPtr KeyboardLayout = LoadKeyboardLayout(keyboardLanguage, KLF_ACTIVATE);
                uint tempVK = MapVirtualKeyEx(Convert.ToUInt32(CurrentPair.Value[1]), MAPVK_VSC_TO_VK_EX, KeyboardLayout);

                Key tempKeyVK = KeyInterop.KeyFromVirtualKey((int)tempVK);

                CurrentButton.Text = "Key " + tempKeyVK.ToString() + " (" + CurrentPair.Value[1].ToString() + ")";
            }
        }
        //----------------------------------// Replace with Custom Button ↓↓↓
        private void buttonGetKeyCode_Click(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            SenderButton.Text = "Waiting for Input";
            //Adding actions
            SenderButton.KeyDown += buttonGetKeyCode_KeyDown;
            SenderButton.KeyUp += button1_KeyUp;
            SenderButton.PreviewKeyDown += button1_PreviewKeyDown;
            //Preventing from looping and extra clicks
            SenderButton.Click -= buttonGetKeyCode_Click;
        }

        private void buttonGetKeyCode_KeyDown(object sender, KeyEventArgs e)
        {
            Button SenderButton = sender as Button;
            uint VKcode = (uint)e.KeyCode;

            ProcessKeyDetection(SenderButton, VKcode);

            SenderButton.KeyDown -= buttonGetKeyCode_KeyDown;
            SenderButton.PreviewKeyDown -= button1_PreviewKeyDown;
        }

        private void button1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Button SenderButton = sender as Button;
            uint VKcode = (uint)e.KeyCode;

            ProcessKeyDetection(SenderButton, VKcode);

            SenderButton.PreviewKeyDown -= button1_PreviewKeyDown;
            SenderButton.KeyDown -= buttonGetKeyCode_KeyDown;
        }

        private void ProcessKeyDetection(Button _inputButton, uint _VKcode)
        {            
            uint scanCode = MapVirtualKey(_VKcode, MAPVK_VK_TO_VSC); //Scan code            
            Key tempKey = KeyInterop.KeyFromVirtualKey((int)_VKcode); //Key

            _inputButton.Text = "Key " + tempKey.ToString() + " (" + scanCode.ToString() + ")";

            string buttonName = _inputButton.Name.Substring(6);

            short extended = 0;
            if (scanCode == 45 || scanCode == 46 || scanCode == 144 || (33 <= scanCode && scanCode <= 40))
                extended = 1;

            SetKey(buttonName, new short[] { extended, (short)scanCode});
        }

        private void button1_KeyUp(object sender, KeyEventArgs e)
        {
            Button SenderButton = sender as Button;

            label1.Focus(); //remove focus
            //ReAdd events
            SenderButton.KeyUp -= button1_KeyUp;
            SenderButton.Click += buttonGetKeyCode_Click;
        }
        //----------------------------------// Replace with Custom Button ↑↑↑

        private void buttonControlMappingSave_Click(object sender, EventArgs e)
        {
            Main_Form.server.pcController.ControlMapping = localControlMapping;
        }


        //----------------------------------//
        //Import 
        //Flags
        private const uint MAPVK_VK_TO_VSC = 0;
        private const uint MAPVK_VSC_TO_VK_EX = 3;
        //
        private const uint KLF_ACTIVATE = 0x00000001;

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, EntryPoint = "MapVirtualKey",
            SetLastError = true, ThrowOnUnmappableChar = false)]

        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        //
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall,
        CharSet = CharSet.Unicode, EntryPoint = "MapVirtualKeyEx",
        SetLastError = true, ThrowOnUnmappableChar = false)]

        internal static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
        //
        [DllImport("user32.dll")]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);
        //
        [DllImport("user32.dll")]
        static extern bool GetKeyboardLayoutName([Out] StringBuilder pwszKLID);
        //
        //----------------------------------//

    }
}
