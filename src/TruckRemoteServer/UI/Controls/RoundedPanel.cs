using System.Drawing;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //Card with rounded corners and a thin border (Windows 11 style)
    public class RoundedPanel : Panel
    {
        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        public Color FillColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Gainsboro;
        public int Radius { get; set; } = 8;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Drawing.Prepare(e.Graphics);
            float radius = Radius * DeviceDpi / 96f;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Drawing.RoundedRectangle(bounds, radius))
            using (var fill = new SolidBrush(FillColor))
            using (var border = new Pen(BorderColor))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }
    }
}
