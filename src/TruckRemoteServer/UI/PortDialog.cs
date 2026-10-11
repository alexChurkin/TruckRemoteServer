using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using TruckRemoteServer.Localization;
using TruckRemoteServer.UI.Controls;
using T = TruckRemoteServer.Localization.TextKeys;

namespace TruckRemoteServer.UI
{
    //Asks for the server port (rarely changed, so it isn't in the main window)
    public sealed class PortDialog : Form
    {
        public const int PortMin = 10000;
        public const int PortMax = 65535;

        private readonly Label hintLabel = new Label { AutoSize = false };
        private readonly InputField portField = new InputField();
        private readonly Label errorLabel = new Label { AutoSize = true };
        private readonly RoundedButton okButton = new RoundedButton();
        private readonly RoundedButton cancelButton = new RoundedButton();
        private readonly Theme theme;
        //The fonts of the display the window is on: it follows another display while the program runs
        private Fonts fonts;

        public PortDialog(Theme theme, int port)
        {
            this.theme = theme;
            Port = port;
            Text = Texts.Get(T.PortTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            fonts = Fonts.For(Dpi.Of(this));

            hintLabel.Text = Texts.Get(T.PortHint);
            okButton.Text = Texts.Get(T.Ok);
            cancelButton.Text = Texts.Get(T.Cancel);
            TextBox box = portField.TextBox;
            box.MaxLength = 5;
            box.Text = port.ToString(CultureInfo.InvariantCulture);
            box.KeyPress += (s, e) => e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
            box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Confirm();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    Close();
                }
            };
            okButton.Click += (s, e) => Confirm();
            cancelButton.Click += (s, e) => Close();
            Controls.AddRange(new Control[] { hintLabel, portField, errorLabel, okButton, cancelButton });

            ApplyTheme();
            ApplyFonts();
            LayoutContent();
        }

        public int Port { get; private set; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            theme.ApplyToTitleBar(Handle);
            //The window knows the display it is on (and so its scale) only now
            ReloadFonts();
        }

        protected override void WndProc(ref Message m)
        {
            //The window is on a display of another scale now. The base class goes first: it resizes the window to the
            //size the system suggests, and the layout has to set its own size over it
            base.WndProc(ref m);
            if (m.Msg == Dpi.WmDpiChanged) ReloadFonts();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            portField.TextBox.Focus();
            portField.TextBox.SelectAll();
        }

        private void Confirm()
        {
            if (int.TryParse(portField.TextBox.Text, out int value) && value >= PortMin && value <= PortMax)
            {
                Port = value;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            errorLabel.Text = Texts.Get(T.PortRange);
            LayoutContent();
        }

        private void ApplyTheme()
        {
            BackColor = theme.Background;
            ForeColor = theme.Text;
            hintLabel.ForeColor = theme.SecondaryText;
            errorLabel.ForeColor = theme.Error;
            portField.FillColor = theme.Control;
            portField.BorderColor = theme.ControlBorder;
            portField.AccentColor = theme.Accent;
            portField.BackColor = theme.Background;
            portField.TextBox.BackColor = theme.Control;
            portField.TextBox.ForeColor = theme.Text;
            okButton.FillColor = theme.Accent;
            okButton.TextColor = theme.OnAccent;
            okButton.BorderColor = Color.Transparent;
            okButton.ParentColor = theme.Background;
            cancelButton.FillColor = theme.Control;
            cancelButton.TextColor = theme.Text;
            cancelButton.BorderColor = theme.ControlBorder;
            cancelButton.ParentColor = theme.Background;
        }

        //The display of the window, and with it the scale of the UI, changed: the fonts and the sizes follow it
        private void ReloadFonts()
        {
            fonts = Fonts.For(Dpi.Of(this));
            ApplyFonts();
            LayoutContent();
        }

        //The controls that don't inherit the font of the window
        private void ApplyFonts()
        {
            Font = fonts.Body;
            hintLabel.Font = fonts.Caption;
            errorLabel.Font = fonts.Caption;
            portField.TextBox.Font = fonts.Body;
        }

        private int Px(float value) => (int)Math.Round(value * Dpi.Of(this) / 96f);

        private void LayoutContent()
        {
            int pad = Px(20);
            int width = Px(300);
            int y = pad;
            hintLabel.Size = new Size(width, TextRenderer.MeasureText(hintLabel.Text, hintLabel.Font,
                new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height);
            hintLabel.Location = new Point(pad, y);
            y += hintLabel.Height + Px(10);
            portField.Bounds = new Rectangle(pad, y, Px(140), Px(34));
            y += portField.Height + Px(4);
            errorLabel.Location = new Point(pad, y);
            y += Math.Max(errorLabel.PreferredHeight, Px(16)) + Px(12);
            okButton.Size = okButton.GetPreferredSize(Size.Empty);
            cancelButton.Size = cancelButton.GetPreferredSize(Size.Empty);
            cancelButton.Location = new Point(pad + width - cancelButton.Width, y);
            okButton.Location = new Point(cancelButton.Left - Px(8) - okButton.Width, y);
            y += okButton.Height + pad;
            ClientSize = new Size(width + 2 * pad, y);
        }
    }
}
