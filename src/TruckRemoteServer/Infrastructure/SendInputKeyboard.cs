using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TruckRemoteServer.Input;

namespace TruckRemoteServer.Infrastructure
{
    //Keys are sent as DirectInput scan codes (the game reads them regardless of the keyboard layout).
    //Default key bindings of ETS2/ATS are used
    public sealed class SendInputKeyboard : IKeyboard, IDisposable
    {
        private static readonly Dictionary<GameKey, short> ScanCodes = new Dictionary<GameKey, short>
        {
            { GameKey.Gas, 0xC8 },           //Up arrow
            { GameKey.Brake, 0xD0 },         //Down arrow
            { GameKey.LeftBlinker, 0x1A },   //[
            { GameKey.RightBlinker, 0x1B },  //]
            { GameKey.HazardLights, 0x21 },  //F
            { GameKey.ParkingBrake, 0x39 },  //Space
            { GameKey.Lights, 0x26 },        //L
            { GameKey.HighBeam, 0x25 },      //K
            { GameKey.Horn, 0x23 },          //H
            { GameKey.AirHorn, 0x31 },       //N
            { GameKey.CruiseControl, 0x2E }, //C
            { GameKey.Engine, 0x12 },        //E
            { GameKey.Trailer, 0x14 },       //T
            { GameKey.Activate, 0x1C },      //Enter
            { GameKey.Wipers, 0x19 },        //P
            { GameKey.DiffLock, 0x2F },      //V
            { GameKey.LiftAxle, 0x16 },      //U
            { GameKey.Beacon, 0x18 },        //O
            { GameKey.LightHorn, 0x24 }      //J
        };

        private const int InputKeyboard = 1;
        private const int KeyeventfKeyup = 0x0002;
        private const int KeyeventfScancode = 0x0008;

        //Clicks are queued: a key is held for a while, so the game doesn't miss it
        private readonly KeyClicker<GameKey> clicker;

        public SendInputKeyboard()
        {
            clicker = new KeyClicker<GameKey>(Press, Release);
        }

        public void Press(GameKey key)
        {
            Send(ScanCodes[key], KeyeventfScancode);
        }

        public void Release(GameKey key)
        {
            Send(ScanCodes[key], KeyeventfKeyup | KeyeventfScancode);
        }

        public void Click(GameKey key)
        {
            clicker.Click(key);
        }

        public void Dispose()
        {
            clicker.Dispose();
        }

        private static void Send(short scanCode, int flags)
        {
            var input = new INPUT { type = InputKeyboard };
            input.u.ki.wScan = scanCode;
            input.u.ki.dwFlags = flags;
            //0 if the input was blocked (e.g. by a window of a higher integrity level): nothing to do then
            _ = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
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
