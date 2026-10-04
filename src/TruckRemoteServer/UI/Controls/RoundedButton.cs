using System.Drawing;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //Flat button with rounded corners: filled accent (primary) or outlined (secondary)
    public class RoundedButton : Button
    {
        private bool hovered;
        private bool pressed;

        public RoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            FlatStyle = FlatStyle.Flat;
            Cursor = Cursors.Hand;
            Padding = new Padding(16, 6, 16, 6);
        }

        public Color FillColor { get; set; } = Color.SeaGreen;
        public Color TextColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Transparent;
        public Color ParentColor { get; set; } = Color.White;

        //Drop-down button: the text is on the left and a chevron on the right
        public bool ShowChevron { get; set; }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = TextRenderer.MeasureText(Text, Font);
            int minHeight = (int)(32 * DeviceDpi / 96f);
            return new Size(text.Width + Padding.Horizontal, System.Math.Max(minHeight, text.Height + Padding.Vertical));
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(ParentColor);
            Drawing.Prepare(e.Graphics);
            float radius = 4 * DeviceDpi / 96f;
            Color fill = !Enabled ? Blend(FillColor, ParentColor, 0.5f)
                : pressed ? Blend(FillColor, ParentColor, 0.25f)
                : hovered ? Blend(FillColor, ParentColor, 0.12f)
                : FillColor;

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Drawing.RoundedRectangle(bounds, radius))
            {
                using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
                if (BorderColor.A > 0)
                {
                    using (var pen = new Pen(BorderColor)) e.Graphics.DrawPath(pen, path);
                }
            }
            if (Focused && ShowFocusCues)
            {
                var ring = new RectangleF(2, 2, Width - 5, Height - 5);
                using (var path = Drawing.RoundedRectangle(ring, radius))
                using (var pen = new Pen(TextColor, 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
            Color text = Enabled ? TextColor : Blend(TextColor, fill, 0.5f);
            if (!ShowChevron)
            {
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                return;
            }
            float scale = DeviceDpi / 96f;
            var textBounds = new Rectangle((int)(10 * scale), 0, Width - (int)(34 * scale), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            float cx = Width - 16 * scale;
            float cy = Height / 2f;
            float size = 4 * scale;
            using (var pen = new Pen(text, 1.5f * scale))
            {
                e.Graphics.DrawLines(pen, new[]
                {
                    new PointF(cx - size, cy - size / 2), new PointF(cx, cy + size / 2), new PointF(cx + size, cy - size / 2)
                });
            }
        }

        private static Color Blend(Color color, Color with, float amount)
        {
            return Color.FromArgb(
                (int)(color.R + (with.R - color.R) * amount),
                (int)(color.G + (with.G - color.G) * amount),
                (int)(color.B + (with.B - color.B) * amount));
        }
    }
}
