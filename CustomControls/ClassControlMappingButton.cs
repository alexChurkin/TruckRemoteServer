using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TruckRemoteServer
{
    public class ControlMappingButton : Button
    {
        public short[] ScanCodes { get; set; } = { 0x00, 0x00 };
        // The following Windows message value is defined in Winuser.h.
        private const int WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x105;

        //Import MapVirtualKey
        private const uint MAPVK_VSC_TO_VK = 1;

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, EntryPoint = "MapVirtualKey",
            SetLastError = true, ThrowOnUnmappableChar = false)]

        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);
        //
        [DllImport("user32.dll", EntryPoint = "GetKeyNameTextW", CharSet = CharSet.Unicode)]
        private static extern int GetKeyNameText(int lParam, [MarshalAs(UnmanagedType.LPWStr), Out] StringBuilder str, int size);
        //
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicodeEx(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 256)] StringBuilder receivingBuffer, int bufferSize, uint flags, int dwhkl);
        //
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetKeyboardState(byte[] lpKeyState);
        //
        public ControlMappingButton()
        {
            //this.Size = new Size(300, 23);
            //this.AutoSize = false;
        }

        // Detect during preprocessing
        public override bool PreProcessMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN || m.Msg == WM_KEYUP || m.Msg == WM_SYSKEYDOWN || m.Msg == WM_SYSKEYUP)
            {
                this.Text = "";
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);

                int lParam = m.LParam.ToInt32();

                int scanCode = (lParam >> 16) & 0x000000ff; // Get Scan code from bit 16-23
                int ext = (lParam >> 24) & 0x00000001; // bit 24
                if (ext == 1)
                {
                    this.ScanCodes[0] = 0x01;
                    this.Text = "Ex"; // Indicating
                }
                else
                    this.ScanCodes[0] = 0x00;

                uint virtualCode = MapVirtualKey(Convert.ToUInt32(scanCode), MAPVK_VSC_TO_VK); // Get Virtual code

                byte[] kbState = new byte[256];
                GetKeyboardState(kbState);

                StringBuilder buffer = new StringBuilder(new string(' ', 256));
                int ToUnicodeExFlag = ToUnicodeEx(virtualCode, (uint)scanCode, kbState, buffer, 256, 0, 0); // get layout char
                string keyName = buffer.ToString().ToUpper(); // Convert to string

                if (ToUnicodeExFlag == 0 || virtualCode == 32 || virtualCode == 9 || virtualCode == 8 | virtualCode == 13 || virtualCode == 27) // Not characters and Space, Mod keys ...
                {
                    var sb = new StringBuilder(256);
                    GetKeyNameText(lParam, sb, 256);
                    keyName = sb.ToString();
                }

                this.Text += "SC = 0x" + scanCode.ToString("X2") + " | Key " + keyName + " (0x" + virtualCode.ToString("X2") + ")"; // Output
                this.ScanCodes[1] = (short)scanCode;
            }

            // Send all other messages to the base method.
            return base.PreProcessMessage(ref m);
        }
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = true; // Action key like arrows not changing focus
            base.OnPreviewKeyDown(e);
        }
    }
}
