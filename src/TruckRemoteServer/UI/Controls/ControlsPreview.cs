using System;
using System.Drawing;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //The phone's controls as the game gets them: the steering bar from the middle to a side, the gas and the brake
    //bars filling up. Empty (dim tracks) while the phone isn't connected
    public class ControlsPreview : Control
    {
        private double steering;
        private double gas;
        private double brake;
        private bool active;

        public ControlsPreview()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            AccessibleRole = AccessibleRole.Graphic;
        }

        public Color ParentColor { get; set; } = Color.White;
        public Color TrackColor { get; set; } = Color.Gainsboro;
        public Color SteeringColor { get; set; } = Color.SteelBlue;
        public Color GasColor { get; set; } = Color.Green;
        public Color BrakeColor { get; set; } = Color.Red;
        public Color TextColor { get; set; } = Color.Gray;

        public string SteeringLabel { get; set; } = "";
        public string GasLabel { get; set; } = "";
        public string BrakeLabel { get; set; } = "";

        //Steering -1..1, pedals 0..1; inactive: the phone isn't connected
        public void SetValues(bool isActive, double steeringValue, double gasValue, double brakeValue)
        {
            if (isActive == active && steeringValue == steering && gasValue == gas && brakeValue == brake) return;
            active = isActive;
            steering = Math.Max(-1, Math.Min(1, steeringValue));
            gas = Math.Max(0, Math.Min(1, gasValue));
            brake = Math.Max(0, Math.Min(1, brakeValue));
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int line = TextRenderer.MeasureText("Ag", Font).Height;
            return new Size(Px(260), 3 * (line + Px(16)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(ParentColor);
            Drawing.Prepare(g);
            int line = TextRenderer.MeasureText("Ag", Font).Height;
            int row = line + Px(16);
            DrawBar(g, 0, line, SteeringLabel, steering, SteeringColor, fromMiddle: true);
            DrawBar(g, row, line, GasLabel, gas, GasColor, fromMiddle: false);
            DrawBar(g, 2 * row, line, BrakeLabel, brake, BrakeColor, fromMiddle: false);
        }

        private void DrawBar(Graphics g, int top, int line, string label, double value, Color color, bool fromMiddle)
        {
            TextRenderer.DrawText(g, label, Font, new Point(0, top), TextColor);
            float height = Px(8);
            var track = new RectangleF(0, top + line + Px(3), Width - 1, height);
            using (var path = Drawing.RoundedRectangle(track, height / 2))
            using (var brush = new SolidBrush(TrackColor))
            {
                g.FillPath(brush, path);
            }
            if (!active) return;

            float middle = track.Left + track.Width / 2;
            RectangleF fill = fromMiddle
                ? RectangleF.FromLTRB(
                    Math.Min(middle, middle + (float)(value * track.Width / 2)), track.Top,
                    Math.Max(middle, middle + (float)(value * track.Width / 2)), track.Bottom)
                : new RectangleF(track.Left, track.Top, (float)(value * track.Width), height);
            if (fromMiddle)
            {
                //The middle mark: straight wheel
                using (var pen = new Pen(TextColor, 1))
                {
                    g.DrawLine(pen, middle, track.Top - Px(2), middle, track.Bottom + Px(2));
                }
            }
            if (fill.Width < 1) return;
            using (var path = Drawing.RoundedRectangle(fill, Math.Min(height / 2, fill.Width / 2)))
            using (var brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
        }

        private int Px(float value) => (int)Math.Round(value * DeviceDpi / 96f);
    }
}
