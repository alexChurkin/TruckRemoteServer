using System.Drawing;
using System.Windows.Forms;
using QRCoder;

namespace TruckRemoteServer.UI.Controls
{
    //QR code of a text: dark modules on a light rounded card (scanners need dark on light, also in the dark theme)
    public class QrCodeView : Control
    {
        private const int QuietModules = 2;

        private bool[,] modules;
        private string content;

        public QrCodeView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            AccessibleRole = AccessibleRole.Graphic;
        }

        public Color ParentColor { get; set; } = Color.White;

        //null or empty hides the code
        public string Content
        {
            get => content;
            set
            {
                if (value == content) return;
                content = value;
                modules = string.IsNullOrEmpty(value) ? null : Encode(value);
                Invalidate();
            }
        }

        private static bool[,] Encode(string text)
        {
            using (var generator = new QRCodeGenerator())
            using (QRCodeData data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M))
            {
                //The matrix already has a quiet zone of 4 modules: it's trimmed to QuietModules
                int full = data.ModuleMatrix.Count;
                int trim = 4 - QuietModules;
                int size = full - 2 * trim;
                var result = new bool[size, size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++) result[x, y] = data.ModuleMatrix[y + trim][x + trim];
                }
                return result;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(ParentColor);
            if (modules == null) return;
            Drawing.Prepare(e.Graphics);
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Drawing.RoundedRectangle(bounds, 8 * DeviceDpi / 96f))
            {
                e.Graphics.FillPath(Brushes.White, path);
            }

            //Whole pixels per module keep the code sharp
            int count = modules.GetLength(0);
            int module = System.Math.Max(1, System.Math.Min(Width, Height) / count);
            int offsetX = (Width - module * count) / 2;
            int offsetY = (Height - module * count) / 2;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            using (var brush = new SolidBrush(Color.FromArgb(24, 24, 24)))
            {
                for (int y = 0; y < count; y++)
                {
                    for (int x = 0; x < count; x++)
                    {
                        if (modules[x, y]) e.Graphics.FillRectangle(brush, offsetX + x * module, offsetY + y * module, module, module);
                    }
                }
            }
        }
    }
}
