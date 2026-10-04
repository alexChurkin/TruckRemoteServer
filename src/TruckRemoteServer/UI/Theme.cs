using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TruckRemoteServer.UI
{
    //Colors and fonts of the window. Light or dark as the apps theme of Windows
    public sealed class Theme
    {
        public static readonly Theme Light = new Theme(
            dark: false,
            background: Color.FromArgb(243, 243, 243),
            card: Color.White,
            cardBorder: Color.FromArgb(229, 229, 229),
            text: Color.FromArgb(27, 27, 27),
            secondaryText: Color.FromArgb(96, 96, 96),
            accent: Color.FromArgb(22, 135, 62),
            onAccent: Color.White,
            control: Color.FromArgb(251, 251, 251),
            controlBorder: Color.FromArgb(214, 214, 214),
            warningBackground: Color.FromArgb(255, 244, 206),
            warningText: Color.FromArgb(87, 63, 0),
            success: Color.FromArgb(15, 123, 15),
            caution: Color.FromArgb(157, 93, 0),
            error: Color.FromArgb(196, 43, 28),
            neutral: Color.FromArgb(118, 118, 118));

        public static readonly Theme Dark = new Theme(
            dark: true,
            background: Color.FromArgb(32, 32, 32),
            card: Color.FromArgb(43, 43, 43),
            cardBorder: Color.FromArgb(58, 58, 58),
            text: Color.White,
            secondaryText: Color.FromArgb(200, 200, 200),
            accent: Color.FromArgb(44, 199, 95),
            onAccent: Color.FromArgb(0, 40, 14),
            control: Color.FromArgb(52, 52, 52),
            controlBorder: Color.FromArgb(80, 80, 80),
            warningBackground: Color.FromArgb(67, 53, 25),
            warningText: Color.FromArgb(252, 225, 0),
            success: Color.FromArgb(108, 203, 95),
            caution: Color.FromArgb(252, 225, 0),
            error: Color.FromArgb(255, 153, 164),
            neutral: Color.FromArgb(160, 160, 160));

        private Theme(bool dark, Color background, Color card, Color cardBorder, Color text, Color secondaryText,
            Color accent, Color onAccent, Color control, Color controlBorder, Color warningBackground,
            Color warningText, Color success, Color caution, Color error, Color neutral)
        {
            IsDark = dark;
            Background = background;
            Card = card;
            CardBorder = cardBorder;
            Text = text;
            SecondaryText = secondaryText;
            Accent = accent;
            OnAccent = onAccent;
            Control = control;
            ControlBorder = controlBorder;
            WarningBackground = warningBackground;
            WarningText = warningText;
            Success = success;
            Caution = caution;
            Error = error;
            Neutral = neutral;
        }

        public bool IsDark { get; }
        public Color Background { get; }
        public Color Card { get; }
        public Color CardBorder { get; }
        public Color Text { get; }
        public Color SecondaryText { get; }
        public Color Accent { get; }
        public Color OnAccent { get; }
        public Color Control { get; }
        public Color ControlBorder { get; }
        public Color WarningBackground { get; }
        public Color WarningText { get; }
        public Color Success { get; }
        public Color Caution { get; }
        public Color Error { get; }
        public Color Neutral { get; }

        public static Theme FromSystem()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    return key?.GetValue("AppsUseLightTheme") is int light && light == 0 ? Dark : Light;
                }
            }
            catch (Exception)
            {
                return Light;
            }
        }

        /* Fonts: Segoe UI Variable on Windows 11, Segoe UI on Windows 10 */

        private static readonly string FontFamilyName = FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Text")
            ? "Segoe UI Variable Text"
            : "Segoe UI";

        private static readonly string DisplayFontFamilyName = FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Display")
            ? "Segoe UI Variable Display"
            : "Segoe UI";

        public static readonly Font Body = new Font(FontFamilyName, 10f);
        public static readonly Font Caption = new Font(FontFamilyName, 9f);
        public static readonly Font BodyStrong = new Font(FontFamilyName, 10f, FontStyle.Bold);
        public static readonly Font Subtitle = new Font(DisplayFontFamilyName, 13f, FontStyle.Bold);
        public static readonly Font Title = new Font(DisplayFontFamilyName, 16f, FontStyle.Bold);
        public static readonly Font Display = new Font(DisplayFontFamilyName, 24f, FontStyle.Bold);

        /* Title bar */

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        //Dark title bar on Windows 10 1809+ and 11 (ignored on older versions)
        public void ApplyToTitleBar(IntPtr window)
        {
            int value = IsDark ? 1 : 0;
            try
            {
                if (DwmSetWindowAttribute(window, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(window, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, sizeof(int));
                }
            }
            catch (Exception)
            {
                //No DWM
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    }
}
