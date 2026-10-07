using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TruckRemoteServer.Input;

namespace TruckRemoteServer.Infrastructure
{
    //Keys are sent as scan codes (the game reads them regardless of the keyboard layout). The keys of the extended
    //part of the keyboard are sent with the extended flag: the games read them so both through DirectInput
    //(di8.keyboard of older profiles) and through the system input (sys.keyboard of new ones).
    //The keys are the player's bindings (KeyBindings), a modifier (e.g. Shift) is held around its key
    public sealed class SendInputKeyboard : IKeyboard, IDisposable
    {
        private const int InputKeyboard = 1;
        private const int KeyeventfExtendedkey = 0x0001;
        private const int KeyeventfKeyup = 0x0002;
        private const int KeyeventfScancode = 0x0008;

        private readonly KeyBindings bindings;
        //The keys that are down: a key is released as it was pressed, even if the bindings have changed meanwhile
        private readonly Dictionary<GameKey, KeyStroke> pressed = new Dictionary<GameKey, KeyStroke>();
        //Clicks are queued: a key is held for a while, so the game doesn't miss it
        private readonly KeyClicker<GameKey> clicker;

        public SendInputKeyboard(KeyBindings bindings)
        {
            this.bindings = bindings;
            clicker = new KeyClicker<GameKey>(Press, Release);
        }

        public void Press(GameKey key)
        {
            KeyStroke stroke = bindings.For(key);
            if (stroke == null) return;
            lock (pressed)
            {
                pressed[key] = stroke;
            }
            if (stroke.Modifier != null) Send(stroke.Modifier, up: false);
            Send(stroke, up: false);
        }

        public void Release(GameKey key)
        {
            KeyStroke stroke;
            lock (pressed)
            {
                if (!pressed.TryGetValue(key, out stroke)) stroke = bindings.For(key);
                pressed.Remove(key);
            }
            if (stroke == null) return;
            Send(stroke, up: true);
            if (stroke.Modifier != null) Send(stroke.Modifier, up: true);
        }

        public void Click(GameKey key)
        {
            clicker.Click(key);
        }

        public bool HasKey(GameKey key) => bindings.For(key) != null;

        public void Dispose()
        {
            clicker.Dispose();
        }

        private static void Send(KeyStroke stroke, bool up)
        {
            Send(stroke.ScanCode, KeyeventfScancode | (up ? KeyeventfKeyup : 0) | (stroke.Extended ? KeyeventfExtendedkey : 0));
        }

        private static void Send(short scanCode, int flags)
        {
            var input = new INPUT { type = InputKeyboard };
            input.u.ki.wScan = scanCode;
            input.u.ki.dwFlags = flags;
            //0 if the input was blocked (e.g. by a window of a higher integrity level): nothing to do then
            _ = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        //The union is aligned by the pointer size: its offset is 4 in 32-bit and 8 in 64-bit processes,
        //so a sequential layout is used instead of fixed offsets
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public System.IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public System.IntPtr dwExtraInfo;
        }
    }
}
