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
        public ControlMappingForm()
        {
            InitializeComponent();
        }

        //Import MapVirtualKey
        private const uint MAPVK_VK_TO_VSC = 0;

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, EntryPoint = "MapVirtualKey",
            SetLastError = true, ThrowOnUnmappableChar = false)]

        private static extern uint MapVirtualKey(uint uCode,uint uMapType);
        //

        private void buttonGetKeyCode_Click(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            SenderButton.Text = "Waiting for Input";

            SenderButton.KeyDown += buttonGetKeyCode_KeyDown;
            SenderButton.KeyUp += button1_KeyUp;
            SenderButton.PreviewKeyDown += button1_PreviewKeyDown;

            SenderButton.Click -= buttonGetKeyCode_Click;
        }

        private void buttonGetKeyCode_KeyDown(object sender, KeyEventArgs e)
        {
            Button SenderButton = sender as Button;
            
            //Scan code
            uint uCode = (uint)e.KeyCode;
            uint scanCode = MapVirtualKey(uCode, MAPVK_VK_TO_VSC);
            //
            SenderButton.Text = "Key " + e.KeyCode.ToString() + " (" + scanCode.ToString() + ")";

            SenderButton.KeyDown -= buttonGetKeyCode_KeyDown;
        }

        private void button1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Button SenderButton = sender as Button;
            //Scan code
            uint uCode = (uint)e.KeyCode;
            uint scanCode = MapVirtualKey(uCode, MAPVK_VK_TO_VSC);
            //
            SenderButton.Text = "Key " + e.KeyCode.ToString() + " (" + scanCode.ToString() + ")";

            SenderButton.PreviewKeyDown -= button1_PreviewKeyDown;
        }

        private void button1_KeyUp(object sender, KeyEventArgs e)
        {
            Button SenderButton = sender as Button;

            SenderButton.KeyUp -= button1_KeyUp;
            SenderButton.Click += buttonGetKeyCode_Click;
        }
    }
}
