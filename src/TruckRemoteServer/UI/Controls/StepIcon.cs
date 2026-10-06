using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //The state of a step of the setup: a check mark, an exclamation mark, a cross, or a ring while it's checked
    public class StepIcon : Control
    {
        public enum Kind
        {
            Waiting,
            Done,
            Attention,
            Failed
        }

        private Kind kind = Kind.Waiting;

        public StepIcon()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            AccessibleRole = AccessibleRole.Graphic;
        }

        public Color ParentColor { get; set; } = Color.White;
        public Color DoneColor { get; set; } = Color.Green;
        public Color AttentionColor { get; set; } = Color.Orange;
        public Color FailedColor { get; set; } = Color.Red;
        public Color WaitingColor { get; set; } = Color.Gray;
        public Color MarkColor { get; set; } = Color.White;

        public Kind State
        {
            get => kind;
            set
            {
                if (value == kind) return;
                kind = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(ParentColor);
            Drawing.Prepare(g);
            float scale = DeviceDpi / 96f;
            var circle = new RectangleF(1, 1, Width - 3, Height - 3);
            if (kind == Kind.Waiting)
            {
                using (var pen = new Pen(WaitingColor, 2 * scale))
                {
                    g.DrawEllipse(pen, circle);
                }
                return;
            }

            Color fill = kind == Kind.Done ? DoneColor : kind == Kind.Attention ? AttentionColor : FailedColor;
            using (var brush = new SolidBrush(fill))
            {
                g.FillEllipse(brush, circle);
            }
            float w = circle.Width;
            float x = circle.Left;
            float y = circle.Top;
            using (var pen = new Pen(MarkColor, 2.2f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                switch (kind)
                {
                    case Kind.Done:
                        g.DrawLines(pen, new[]
                        {
                            new PointF(x + w * 0.28f, y + w * 0.52f),
                            new PointF(x + w * 0.44f, y + w * 0.68f),
                            new PointF(x + w * 0.72f, y + w * 0.36f)
                        });
                        break;
                    case Kind.Attention:
                        g.DrawLine(pen, x + w / 2, y + w * 0.26f, x + w / 2, y + w * 0.56f);
                        g.DrawLine(pen, x + w / 2, y + w * 0.73f, x + w / 2, y + w * 0.74f);
                        break;
                    default:
                        g.DrawLine(pen, x + w * 0.33f, y + w * 0.33f, x + w * 0.67f, y + w * 0.67f);
                        g.DrawLine(pen, x + w * 0.67f, y + w * 0.33f, x + w * 0.33f, y + w * 0.67f);
                        break;
                }
            }
        }
    }
}
