using System;
using System.Collections.Generic;

namespace TruckRemoteServer.Input
{
    //Keys as controls.sii of the games names them ("keyboard.e", "keyboard.lshift", "keyboard.period"): the names
    //follow the DirectInput key codes, written in lower case; the variants of a name the games have used are all known
    public static class ScsKeyNames
    {
        private static readonly Dictionary<string, KeyStroke> Keys = Build();

        public static bool TryGet(string name, out KeyStroke key)
        {
            if (name == null)
            {
                key = null;
                return false;
            }
            return Keys.TryGetValue(name, out key);
        }

        private static Dictionary<string, KeyStroke> Build()
        {
            var keys = new Dictionary<string, KeyStroke>(StringComparer.OrdinalIgnoreCase);
            void Add(short scanCode, bool extended, params string[] names)
            {
                foreach (string name in names) keys[name] = new KeyStroke(scanCode, extended);
            }

            //Letters by the rows of the keyboard
            Add(0x10, false, "q");
            Add(0x11, false, "w");
            Add(0x12, false, "e");
            Add(0x13, false, "r");
            Add(0x14, false, "t");
            Add(0x15, false, "y");
            Add(0x16, false, "u");
            Add(0x17, false, "i");
            Add(0x18, false, "o");
            Add(0x19, false, "p");
            Add(0x1E, false, "a");
            Add(0x1F, false, "s");
            Add(0x20, false, "d");
            Add(0x21, false, "f");
            Add(0x22, false, "g");
            Add(0x23, false, "h");
            Add(0x24, false, "j");
            Add(0x25, false, "k");
            Add(0x26, false, "l");
            Add(0x2C, false, "z");
            Add(0x2D, false, "x");
            Add(0x2E, false, "c");
            Add(0x2F, false, "v");
            Add(0x30, false, "b");
            Add(0x31, false, "n");
            Add(0x32, false, "m");

            //The digits row: 1..9 are 0x02..0x0A, 0 is 0x0B
            for (int digit = 1; digit <= 9; digit++) Add((short)(digit + 1), false, digit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Add(0x0B, false, "0");

            //F1..F10 are 0x3B..0x44, F11 and F12 are apart
            for (int f = 1; f <= 10; f++) Add((short)(0x3A + f), false, "f" + f.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Add(0x57, false, "f11");
            Add(0x58, false, "f12");

            Add(0x01, false, "escape", "esc");
            Add(0x0C, false, "minus");
            Add(0x0D, false, "equal", "equals");
            Add(0x0E, false, "back", "backspace");
            Add(0x0F, false, "tab");
            Add(0x1A, false, "lbracket");
            Add(0x1B, false, "rbracket");
            Add(0x1C, false, "enter", "return");
            Add(0x1D, false, "lctrl", "lcontrol");
            Add(0x27, false, "semicolon");
            Add(0x28, false, "apostrophe");
            Add(0x29, false, "grave");
            Add(0x2A, false, "lshift");
            Add(0x2B, false, "backslash");
            Add(0x33, false, "comma");
            Add(0x34, false, "period");
            Add(0x35, false, "slash");
            Add(0x36, false, "rshift");
            Add(0x38, false, "lalt", "lmenu");
            Add(0x39, false, "space");
            Add(0x3A, false, "capslock", "capital");
            Add(0x46, false, "scroll", "scrolllock");

            //The numeric keypad
            Add(0x37, false, "multiply", "nummultiply", "numpadstar");
            Add(0x4A, false, "subtract", "numminus", "numpadminus");
            Add(0x4E, false, "add", "numplus", "numpadplus");
            Add(0x53, false, "decimal", "numdecimal", "numpadperiod");
            Add(0x52, false, "num0", "numpad0");
            Add(0x4F, false, "num1", "numpad1");
            Add(0x50, false, "num2", "numpad2");
            Add(0x51, false, "num3", "numpad3");
            Add(0x4B, false, "num4", "numpad4");
            Add(0x4C, false, "num5", "numpad5");
            Add(0x4D, false, "num6", "numpad6");
            Add(0x47, false, "num7", "numpad7");
            Add(0x48, false, "num8", "numpad8");
            Add(0x49, false, "num9", "numpad9");
            Add(0x45, false, "numlock");

            //The extended keys: without the flag their codes are keys of the numeric keypad
            Add(0x35, true, "divide", "numdivide", "numpadslash");
            Add(0x1C, true, "numenter", "numpadenter");
            Add(0x1D, true, "rctrl", "rcontrol");
            Add(0x38, true, "ralt", "rmenu");
            Add(0x48, true, "up");
            Add(0x50, true, "down");
            Add(0x4B, true, "left");
            Add(0x4D, true, "right");
            Add(0x47, true, "home");
            Add(0x4F, true, "end");
            Add(0x49, true, "pgup", "prior", "pageup");
            Add(0x51, true, "pgdn", "next", "pagedown");
            Add(0x52, true, "insert");
            Add(0x53, true, "delete");
            return keys;
        }
    }
}
