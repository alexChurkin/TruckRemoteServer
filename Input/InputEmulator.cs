using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;



namespace TruckRemoteServer
{
    public static class InputEmulator
    {
        //~ OK
        const int ERROR_SUCCESS = 0;

        private static vJoy joyStick;
        private static uint joyId = 1;
        private static volatile bool joyAcquired;
        private static volatile bool pedalAxesExist;
        private static bool ffbRegistered;

        private static IFfbListener ffbListener;
        private static readonly KeyClicker clicker = new KeyClicker(KeyPress, KeyRelease);

        public static bool IsJoyInitialized()
        {
            return joyAcquired;
        }

        //Analog pedals need Y (gas) and Z (brake) axes enabled in vJoy configuration
        public static bool HasPedalAxes()
        {
            return joyAcquired && pedalAxesExist;
        }

        public static bool InitJoy(IFfbListener listener)
        {
            ffbListener = listener;
            try
            {
                if (joyStick == null) joyStick = new vJoy();

                if (!joyStick.vJoyEnabled())
                {
                    Console.WriteLine("vJoy driver is not enabled");
                    return false;
                }

                VjdStat status = joyStick.GetVJDStatus(joyId);
                bool acquired = status == VjdStat.VJD_STAT_OWN
                    || (status == VjdStat.VJD_STAT_FREE && joyStick.AcquireVJD(joyId));

                if (!acquired)
                {
                    Console.WriteLine("Failed to acquire vJoy device number {0} (status: {1}).\n", joyId, status);
                    return false;
                }

                Console.WriteLine("Acquired: vJoy device number {0}.\n", joyId);
                joyStick.ResetVJD(joyId);
                pedalAxesExist = joyStick.GetVJDAxisExist(joyId, HID_USAGES.HID_USAGE_Y)
                    && joyStick.GetVJDAxisExist(joyId, HID_USAGES.HID_USAGE_Z);
                if (!ffbRegistered)
                {
                    joyStick.FfbRegisterGenCB(OnFFBEvent, joyId);
                    ffbRegistered = true;
                }
                joyAcquired = true;
                return true;
            }
            catch (Exception e)
            {
                //vJoy is not installed
                Console.WriteLine("vJoy initialization failed: " + e.Message);
                return false;
            }
        }

        private static void OnFFBEvent(IntPtr data, object userData)
        {
            vJoy.FFB_EFF_CONSTANT effectInf = new vJoy.FFB_EFF_CONSTANT();
            if (joyStick.Ffb_h_Eff_Constant(data, ref effectInf) == ERROR_SUCCESS)
            {
                ffbListener?.OnFfbEffect((uint)Math.Abs(effectInf.Magnitude));
            }
        }

        public static void ReleaseJoy()
        {
            if (joyStick != null && joyAcquired)
            {
                joyAcquired = false;
                joyStick.RelinquishVJD(joyId);
            }
        }

        public static void SetXAxis(int xAxisValue)
        {
            //xAxisValue can be from 0 to 32768
            if (!joyAcquired) return;
            joyStick.SetAxis(xAxisValue, joyId, HID_USAGES.HID_USAGE_X);
        }

        public static void SetPedalAxes(int gasValue, int brakeValue)
        {
            //Values can be from 0 to 32767
            if (!HasPedalAxes()) return;
            joyStick.SetAxis(gasValue, joyId, HID_USAGES.HID_USAGE_Y);
            joyStick.SetAxis(brakeValue, joyId, HID_USAGES.HID_USAGE_Z);
        }

        //Clicks are queued: a key is held for a while, so the game doesn't miss it
        public static void KeyClick(short scanCode)
        {
            clicker.Click(scanCode);
        }

        public static void KeyPress(short scanCode)
        {
            INPUT input = new INPUT();
            input.type = (int)InputType.INPUT_KEYBOARD;
            input.u.ki.dwFlags = (int)KEYEVENTF.SCANCODE;
            input.u.ki.wScan = scanCode;

            INPUT[] pInputs = new INPUT[] { input };

            SendInput(1, pInputs, Marshal.SizeOf(input));
        }

        public static void KeyRelease(short scanCode)
        {
            INPUT input = new INPUT();
            input.type = (int)InputType.INPUT_KEYBOARD;
            input.u.ki.dwFlags = (int)KEYEVENTF.KEYUP | (int)KEYEVENTF.SCANCODE;
            input.u.ki.wScan = scanCode;

            INPUT[] pInputs = new INPUT[] { input };

            SendInput(1, pInputs, Marshal.SizeOf(input));
        }




        /*................................................................................................*/
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        //The union is aligned by the pointer size: its offset is 4 in 32-bit and 8 in 64-bit processes,
        //so a sequential layout is used instead of fixed offsets
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [Flags]
        public enum InputType
        {
            INPUT_MOUSE = 0,
            INPUT_KEYBOARD = 1,
            INPUT_HARDWARE = 2
        }

        [Flags]
        public enum MOUSEEVENTF
        {
            MOVE = 0x0001, /* mouse move */
            LEFTDOWN = 0x0002, /* left button down */
            LEFTUP = 0x0004, /* left button up */
            RIGHTDOWN = 0x0008, /* right button down */
            RIGHTUP = 0x0010, /* right button up */
            MIDDLEDOWN = 0x0020, /* middle button down */
            MIDDLEUP = 0x0040, /* middle button up */
            XDOWN = 0x0080, /* x button down */
            XUP = 0x0100, /* x button down */
            WHEEL = 0x0800, /* wheel button rolled */
            MOVE_NOCOALESCE = 0x2000, /* do not coalesce mouse moves */
            VIRTUALDESK = 0x4000, /* map to entire virtual desktop */
            ABSOLUTE = 0x8000 /* absolute move */
        }

        [Flags]
        public enum KEYEVENTF
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            UNICODE = 0x0004,
            SCANCODE = 0x0008,
        }
    }
}