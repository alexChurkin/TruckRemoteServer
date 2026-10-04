using System.Drawing;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //State of the server: a colored dot and text in a rounded pill
    public class StatusPill : Control
    {
        public StatusPill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            AccessibleRole = AccessibleRole.StatusBar;
        }

        public Color DotColor { get; set; } = Color.Gray;
        public Color FillColor { get; set; } = Color.WhiteSmoke;
        public Color TextColor { get; set; } = Color.Black;
        public Color ParentColor { get; set; } = Color.White;

        private float Scale => DeviceDpi / 96f;

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = TextRenderer.MeasureText(Text, Font);
            return new Size((int)(text.Width + 34 * Scale), (int)(text.Height + 10 * Scale));
        }

        protected override void OnTextChanged(System.EventArgs e)
        {
            AccessibleName = Text;
            Size = GetPreferredSize(Size.Empty);
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(ParentColor);
            Drawing.Prepare(e.Graphics);
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Drawing.RoundedRectangle(bounds, bounds.Height / 2))
            using (var brush = new SolidBrush(FillColor))
            {
                e.Graphics.FillPath(brush, path);
            }
            float dot = 8 * Scale;
            using (var brush = new SolidBrush(DotColor))
            {
                e.Graphics.FillEllipse(brush, 12 * Scale, (Height - dot) / 2, dot, dot);
            }
            var textBounds = new Rectangle((int)(26 * Scale), 0, Width - (int)(26 * Scale), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, TextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
        }
    }
}
