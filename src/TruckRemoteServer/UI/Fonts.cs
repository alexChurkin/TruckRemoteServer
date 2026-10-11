using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TruckRemoteServer.UI
{
    //The fonts of a window, built for the scale of the display it is on. A font of a point size keeps the scale the
    //display had when it was made, so the same font doesn't follow the window to a display of another scale: the
    //sizes here are points of a 96 DPI display, and they become pixels of the scale of this window
    internal sealed class Fonts
    {
        //Segoe UI Variable on Windows 11, Segoe UI on Windows 10
        private static readonly string TextFamily = FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Text")
            ? "Segoe UI Variable Text"
            : "Segoe UI";

        private static readonly string DisplayFamily =
            FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Display")
                ? "Segoe UI Variable Display"
                : "Segoe UI";

        //A window follows another display rarely and there are only a few scales: the fonts of a scale are shared
        private static readonly Dictionary<int, Fonts> Cache = new Dictionary<int, Fonts>();

        private readonly int dpi;

        private Fonts(int dpi)
        {
            this.dpi = dpi;
            Body = Make(TextFamily, 10f, FontStyle.Regular);
            Caption = Make(TextFamily, 9f, FontStyle.Regular);
            BodyStrong = Make(TextFamily, 10f, FontStyle.Bold);
            Subtitle = Make(DisplayFamily, 13f, FontStyle.Bold);
            Title = Make(DisplayFamily, 16f, FontStyle.Bold);
            Display = Make(DisplayFamily, 24f, FontStyle.Bold);
        }

        public static Fonts For(int dpi)
        {
            if (!Cache.TryGetValue(dpi, out Fonts fonts))
            {
                fonts = new Fonts(dpi);
                Cache[dpi] = fonts;
            }
            return fonts;
        }

        public Font Body { get; }
        public Font Caption { get; }
        public Font BodyStrong { get; }
        public Font Subtitle { get; }
        public Font Title { get; }
        public Font Display { get; }

        private Font Make(string family, float points, FontStyle style) =>
            new Font(family, points * dpi / 72f, style, GraphicsUnit.Pixel);
    }
}
