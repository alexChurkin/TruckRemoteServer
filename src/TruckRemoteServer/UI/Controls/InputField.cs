using System.Drawing;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //Text box in a rounded frame (Windows 11 style): the bottom line becomes accent when focused
    public class InputField : RoundedPanel
    {
        public InputField()
        {
            TextBox = new TextBox { BorderStyle = BorderStyle.None };
            Controls.Add(TextBox);
            TextBox.GotFocus += (s, e) => Invalidate();
            TextBox.LostFocus += (s, e) => Invalidate();
            Radius = 4;
            Cursor = Cursors.IBeam;
            Click += (s, e) => TextBox.Focus();
        }

        public TextBox TextBox { get; }
        public Color AccentColor { get; set; } = Color.SeaGreen;

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int padding = (int)(10 * DeviceDpi / 96f);
            TextBox.BackColor = FillColor;
            TextBox.Bounds = new Rectangle(padding, (Height - TextBox.PreferredHeight) / 2,
                Width - 2 * padding, TextBox.PreferredHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!TextBox.Focused) return;
            float thickness = 2 * DeviceDpi / 96f;
            float radius = Radius * DeviceDpi / 96f;
            using (var pen = new Pen(AccentColor, thickness))
            {
                e.Graphics.DrawLine(pen, radius, Height - thickness / 2 - 1, Width - radius, Height - thickness / 2 - 1);
            }
        }
    }
}
