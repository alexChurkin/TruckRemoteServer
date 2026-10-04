using System;
using System.Drawing;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //Windows 11 style slider: thin rounded track, accent fill, round thumb. Mouse and keyboard
    public class Slider : Control
    {
        private int value = 50;
        private bool dragging;

        public Slider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            TabStop = true;
            AccessibleRole = AccessibleRole.Slider;
            Cursor = Cursors.Hand;
        }

        public event EventHandler ValueChanged;

        public int Minimum { get; set; } = 1;
        public int Maximum { get; set; } = 100;
        public Color TrackColor { get; set; } = Color.LightGray;
        public Color FillColor { get; set; } = Color.SeaGreen;
        public Color ThumbColor { get; set; } = Color.White;
        public Color ParentColor { get; set; } = Color.White;

        public int Value
        {
            get => value;
            set
            {
                int clamped = Math.Max(Minimum, Math.Min(Maximum, value));
                if (clamped == this.value) return;
                this.value = clamped;
                AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override AccessibleObject CreateAccessibilityInstance() => new SliderAccessibleObject(this);

        private float Scale => DeviceDpi / 96f;
        private float ThumbRadius => 10 * Scale;
        private float TrackLeft => ThumbRadius + 1;
        private float TrackRight => Width - ThumbRadius - 1;

        private float ValueToX(int v)
        {
            return TrackLeft + (TrackRight - TrackLeft) * (v - Minimum) / Math.Max(1, Maximum - Minimum);
        }

        private int XToValue(float x)
        {
            float part = (x - TrackLeft) / Math.Max(1, TrackRight - TrackLeft);
            return Minimum + (int)Math.Round(Math.Max(0, Math.Min(1, part)) * (Maximum - Minimum));
        }

        public override Size GetPreferredSize(Size proposedSize) => new Size(200, (int)(ThumbRadius * 2 + 4));

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            dragging = true;
            Value = XToValue(e.X);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging) Value = XToValue(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    Value -= 1;
                    break;
                case Keys.Right:
                case Keys.Up:
                    Value += 1;
                    break;
                case Keys.PageDown:
                    Value -= 10;
                    break;
                case Keys.PageUp:
                    Value += 10;
                    break;
                case Keys.Home:
                    Value = Minimum;
                    break;
                case Keys.End:
                    Value = Maximum;
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(ParentColor);
            Drawing.Prepare(e.Graphics);
            float centerY = Height / 2f;
            float trackHeight = 4 * Scale;
            float thumbX = ValueToX(value);

            var track = new RectangleF(TrackLeft, centerY - trackHeight / 2, TrackRight - TrackLeft, trackHeight);
            using (var path = Drawing.RoundedRectangle(track, trackHeight / 2))
            using (var brush = new SolidBrush(TrackColor))
            {
                e.Graphics.FillPath(brush, path);
            }
            var filled = new RectangleF(TrackLeft, track.Top, Math.Max(trackHeight, thumbX - TrackLeft), trackHeight);
            using (var path = Drawing.RoundedRectangle(filled, trackHeight / 2))
            using (var brush = new SolidBrush(FillColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            //Thumb: an outer circle with an accent dot, the dot grows when focused
            float outer = ThumbRadius;
            float inner = (Focused ? 6 : 5) * Scale;
            using (var brush = new SolidBrush(ThumbColor))
            using (var border = new Pen(TrackColor))
            {
                e.Graphics.FillEllipse(brush, thumbX - outer, centerY - outer, outer * 2, outer * 2);
                e.Graphics.DrawEllipse(border, thumbX - outer, centerY - outer, outer * 2, outer * 2);
            }
            using (var brush = new SolidBrush(FillColor))
            {
                e.Graphics.FillEllipse(brush, thumbX - inner, centerY - inner, inner * 2, inner * 2);
            }
        }

        private sealed class SliderAccessibleObject : ControlAccessibleObject
        {
            private readonly Slider slider;

            public SliderAccessibleObject(Slider slider) : base(slider)
            {
                this.slider = slider;
            }

            public override AccessibleRole Role => AccessibleRole.Slider;

            public override string Value
            {
                get => slider.Value.ToString();
                set
                {
                    if (int.TryParse(value, out int parsed)) slider.Value = parsed;
                }
            }
        }
    }
}
