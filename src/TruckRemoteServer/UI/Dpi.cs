using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TruckRemoteServer.UI
{
    //The scale of the display a window is on, in DPI (96 is 100%). Every size of the UI is computed from it. The
    //program is per-monitor DPI aware (app.manifest), so Windows never stretches its windows, and a window follows
    //another display while the program runs: DeviceDpi of Windows Forms is used only where the scale of a window
    //cannot be asked for (Windows 10 before 1607), as Windows Forms of .NET Framework follows the scale of a window
    //of a running program only with settings in an exe.config, and the program is a single exe
    internal static class Dpi
    {
        //The window got the scale of another display (lParam: the bounds Windows suggests for it)
        public const int WmDpiChanged = 0x02E0;

        //The scale of a 96 DPI display
        public const int Default = 96;

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr window);

        //The scale of a window itself can be asked for since Windows 10 1607
        private static readonly bool CanAsk = Available();

        private static bool Available()
        {
            try
            {
                //The value (0 for no window) is not needed: the call itself tells whether the function exists
                _ = GetDpiForWindow(IntPtr.Zero);
                return true;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        public static int Of(Control control)
        {
            //A control that hasn't been shown yet has no window: it is on the display of its window, so the nearest
            //parent that has one is asked (otherwise a hidden control would be laid out for the scale of the system)
            Control window = control;
            while (window != null && !window.IsHandleCreated) window = window.Parent;
            if (CanAsk && window != null)
            {
                uint dpi = GetDpiForWindow(window.Handle);
                if (dpi > 0) return (int)dpi;
            }
            return control.DeviceDpi > 0 ? control.DeviceDpi : Default;
        }

        //Where Windows suggests to put a window that got the scale of another display (WmDpiChanged). The window
        //has to go there: the place is chosen so that the window stays on the new display, and a window that only
        //changes its size where it is may get back to the old display and change the scale again, endlessly
        public static Point SuggestedLocation(Message dpiChanged)
        {
            var bounds = Marshal.PtrToStructure<SuggestedBounds>(dpiChanged.LParam);
            return new Point(bounds.Left, bounds.Top);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SuggestedBounds
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
