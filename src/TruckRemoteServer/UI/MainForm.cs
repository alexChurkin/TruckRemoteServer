using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using TruckRemoteServer.Localization;
using TruckRemoteServer.Presentation;
using TruckRemoteServer.UI.Controls;
using T = TruckRemoteServer.Localization.TextKeys;

namespace TruckRemoteServer.UI
{
    //Passive view: shows what MainPresenter tells and reports user actions.
    //Built in code and laid out by LayoutContent(): texts of different languages have different lengths
    public sealed class MainForm : Form, IMainView
    {
        private const int CONTENT_WIDTH = 440;
        private const int PORT_MIN = 10000;
        private const int PORT_MAX = 65535;

        private static readonly (string Code, string Name)[] LANGUAGES =
        {
            ("", null),
            ("en", "English"),
            ("ru", "Русский"),
            ("be", "Беларуская"),
            ("uk", "Українська")
        };

        private readonly ToolTip toolTip = new ToolTip();
        private readonly Timer copiedTimer = new Timer { Interval = 1500 };

        private readonly Label titleLabel = new Label { AutoSize = true, Text = "Truck Remote Server" };
        private readonly StatusPill statusPill = new StatusPill();

        private readonly RoundedPanel addressCard = new RoundedPanel();
        private readonly Label addressCaption = new Label { AutoSize = true };
        private readonly Label addressLabel = new Label { AutoSize = true };
        private readonly RoundedButton copyButton = new RoundedButton();
        private readonly Label portInfoLabel = new Label { AutoSize = true };
        private readonly Label hintLabel = new Label { AutoSize = false };

        private readonly RoundedPanel firewallBanner = new RoundedPanel { Visible = false };
        private readonly Label firewallLabel = new Label { AutoSize = false };
        private readonly RoundedButton allowButton = new RoundedButton();

        private readonly RoundedPanel steeringCard = new RoundedPanel();
        private readonly Label steeringTitle = new Label { AutoSize = true };
        private readonly Label sensitivityLabel = new Label { AutoSize = true };
        private readonly Label sensitivityValue = new Label { AutoSize = true };
        private readonly Slider sensitivitySlider = new Slider { Minimum = 1, Maximum = 100 };

        private readonly RoundedPanel settingsCard = new RoundedPanel();
        private readonly Label settingsTitle = new Label { AutoSize = true };
        private readonly Label portCaption = new Label { AutoSize = true };
        private readonly TextBox portBox = new TextBox { BorderStyle = BorderStyle.FixedSingle, MaxLength = 5 };
        private readonly Label languageCaption = new Label { AutoSize = true };
        private readonly ComboBox languageBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed,
            FlatStyle = FlatStyle.Flat
        };

        private readonly RoundedButton startStopButton = new RoundedButton();
        private readonly Label versionLabel = new Label { AutoSize = true };

        private Theme theme = Theme.FromSystem();
        private IList<string> addresses = new List<string>();
        private int port;
        private ServerState state = ServerState.Stopped;
        private string language = "";
        //Values shown by the presenter mustn't be reported back as user changes
        private bool showingValues;

        public MainForm()
        {
            Text = "Truck Remote Server";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            Font = Theme.Body;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
                ShowIcon = false;
            }

            titleLabel.Font = Theme.Title;
            statusPill.Font = Theme.Caption;
            addressCaption.Font = Theme.Caption;
            addressLabel.Font = Theme.Display;
            portInfoLabel.Font = Theme.Caption;
            hintLabel.Font = Theme.Caption;
            steeringTitle.Font = Theme.Subtitle;
            settingsTitle.Font = Theme.Subtitle;
            portCaption.Font = Theme.Caption;
            languageCaption.Font = Theme.Caption;
            versionLabel.Font = Theme.Caption;
            sensitivityValue.Font = Theme.BodyStrong;

            addressCard.Controls.AddRange(new Control[] { addressCaption, addressLabel, copyButton, portInfoLabel, hintLabel });
            firewallBanner.Controls.AddRange(new Control[] { firewallLabel, allowButton });
            steeringCard.Controls.AddRange(new Control[] { steeringTitle, sensitivityLabel, sensitivityValue, sensitivitySlider });
            settingsCard.Controls.AddRange(new Control[] { settingsTitle, portCaption, portBox, languageCaption, languageBox });
            Controls.AddRange(new Control[]
            {
                titleLabel, statusPill, addressCard, firewallBanner, steeringCard, settingsCard, startStopButton, versionLabel
            });

            foreach (var item in LANGUAGES) languageBox.Items.Add(item.Code);
            languageBox.DrawItem += DrawLanguageItem;
            languageBox.SelectedIndexChanged += OnLanguageSelected;
            copyButton.Click += (s, e) => CopyAddress();
            copiedTimer.Tick += (s, e) =>
            {
                copiedTimer.Stop();
                copyButton.Text = Texts.Get(T.Copy);
                LayoutContent();
            };
            allowButton.Click += (s, e) => AllowFirewallRequested?.Invoke(this, EventArgs.Empty);
            startStopButton.Click += (s, e) => (IsRunning ? StopRequested : StartRequested)?.Invoke(this, EventArgs.Empty);
            sensitivitySlider.ValueChanged += OnSensitivityChanged;
            portBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                CommitPort();
            };
            portBox.Leave += (s, e) => CommitPort();
            portBox.KeyPress += (s, e) => e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

            ApplyTexts();
            ApplyTheme();
        }

        public new event EventHandler Shown;
        public new event EventHandler Closing;
        public event EventHandler StartRequested;
        public event EventHandler StopRequested;
        public event EventHandler<int> PortChanged;
        public event EventHandler<int> SensitivityChanged;
        public event EventHandler<string> LanguageChanged;
        public event EventHandler AllowFirewallRequested;

        private bool IsRunning => state == ServerState.WaitingForController || state == ServerState.ControllerConnected
            || state == ServerState.ControllerConnectedWithoutJoystick || state == ServerState.ControllerPaused;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            theme.ApplyToTitleBar(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Shown?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Closing?.Invoke(this, EventArgs.Empty);
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                toolTip.Dispose();
                copiedTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        /* IMainView */

        public void ShowLanguage(string code)
        {
            language = code ?? "";
            Texts.SetLanguage(language);
            showingValues = true;
            languageBox.SelectedIndex = Math.Max(0, Array.FindIndex(LANGUAGES, item => item.Code == language));
            showingValues = false;
            ApplyTexts();
        }

        public void ShowSettings(int port, int sensitivity)
        {
            showingValues = true;
            this.port = port;
            portBox.Text = port.ToString();
            sensitivitySlider.Value = sensitivity;
            sensitivityValue.Text = sensitivity.ToString();
            showingValues = false;
            LayoutContent();
        }

        public void ShowAddresses(IList<string> addresses, int port)
        {
            this.addresses = addresses;
            this.port = port;
            ApplyAddressTexts();
            LayoutContent();
        }

        public void ShowState(ServerState state, int port)
        {
            this.state = state;
            this.port = port;
            ApplyStateTexts();
            ApplyStateTheme();
            LayoutContent();
        }

        public void ShowFirewallWarning(bool visible, bool busy)
        {
            firewallBanner.Visible = visible;
            allowButton.Enabled = !busy;
            LayoutContent();
        }

        public bool AskAllowFirewall()
        {
            return MessageBox.Show(this, Texts.Get(T.FirewallQuestion), Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        public void ShowWarning(Warning warning, string detail)
        {
            string text = warning == Warning.TelemetryPluginNotInstalled
                ? Texts.Format(T.PluginNotInstalled, detail)
                : Texts.Format(T.FirewallNotApplied, detail);
            MessageBox.Show(this, text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void RunOnUiThread(Action action)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke(action);
                else action();
            }
            catch (InvalidOperationException)
            {
                //The window is being closed
            }
        }

        /* User actions */

        private void CopyAddress()
        {
            if (addresses.Count == 0) return;
            try
            {
                Clipboard.SetText(addresses[0]);
                copyButton.Text = Texts.Get(T.Copied);
                LayoutContent();
                copiedTimer.Stop();
                copiedTimer.Start();
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                //The clipboard is used by another program
            }
        }

        private void OnSensitivityChanged(object sender, EventArgs e)
        {
            sensitivityValue.Text = sensitivitySlider.Value.ToString();
            LayoutContent();
            if (!showingValues) SensitivityChanged?.Invoke(this, sensitivitySlider.Value);
        }

        //A wrong port is replaced back by the current one
        private void CommitPort()
        {
            if (int.TryParse(portBox.Text, out int value) && value >= PORT_MIN && value <= PORT_MAX)
            {
                toolTip.Hide(portBox);
                if (value != port) PortChanged?.Invoke(this, value);
                return;
            }
            portBox.Text = port.ToString();
            toolTip.Show(Texts.Get(T.PortRange), portBox, 0, portBox.Height + 2, 2500);
        }

        private void OnLanguageSelected(object sender, EventArgs e)
        {
            if (showingValues || languageBox.SelectedIndex < 0) return;
            LanguageChanged?.Invoke(this, LANGUAGES[languageBox.SelectedIndex].Code);
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General) return;
            Theme system = Theme.FromSystem();
            if (system == theme) return;
            RunOnUiThread(() =>
            {
                theme = system;
                ApplyTheme();
            });
        }

        /* Texts */

        private void ApplyTexts()
        {
            addressCaption.Text = Texts.Get(T.ServerAddress);
            hintLabel.Text = Texts.Get(T.AddressHint);
            copyButton.Text = Texts.Get(T.Copy);
            firewallLabel.Text = Texts.Get(T.FirewallWarning);
            allowButton.Text = Texts.Get(T.FirewallAllow);
            steeringTitle.Text = Texts.Get(T.Steering);
            sensitivityLabel.Text = Texts.Get(T.Sensitivity);
            settingsTitle.Text = Texts.Get(T.SettingsTitle);
            portCaption.Text = Texts.Get(T.Port);
            languageCaption.Text = Texts.Get(T.Language);
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            versionLabel.Text = Texts.Format(T.Version, version.ToString(3));
            languageBox.Invalidate();
            ApplyAddressTexts();
            ApplyStateTexts();
            LayoutContent();
        }

        private void ApplyAddressTexts()
        {
            bool hasAddress = addresses.Count > 0;
            addressLabel.Text = hasAddress ? addresses[0] : Texts.Get(T.NoAddress);
            addressLabel.Font = hasAddress ? Theme.Display : Theme.Subtitle;
            copyButton.Visible = hasAddress;
            portInfoLabel.Text = addresses.Count > 1
                ? Texts.Format(T.PortAndMore, port, addresses.Count - 1)
                : Texts.Format(T.PortOnly, port);
            toolTip.SetToolTip(portInfoLabel, addresses.Count > 1
                ? Texts.Get(T.OtherAddresses) + "\n" + string.Join("\n", addresses)
                : null);
        }

        private void ApplyStateTexts()
        {
            switch (state)
            {
                case ServerState.Stopped:
                    statusPill.Text = Texts.Get(T.StateStopped);
                    break;
                case ServerState.PortBusy:
                    statusPill.Text = Texts.Format(T.StatePortBusy, port);
                    break;
                case ServerState.WaitingForController:
                    statusPill.Text = Texts.Get(T.StateWaiting);
                    break;
                case ServerState.ControllerConnected:
                    statusPill.Text = Texts.Get(T.StateConnected);
                    break;
                case ServerState.ControllerConnectedWithoutJoystick:
                    statusPill.Text = Texts.Get(T.StateNoJoystick);
                    break;
                case ServerState.ControllerPaused:
                    statusPill.Text = Texts.Get(T.StatePaused);
                    break;
            }
            startStopButton.Text = Texts.Get(IsRunning ? T.Stop : T.Start);
        }

        /* Theme */

        private void ApplyTheme()
        {
            BackColor = theme.Background;
            ForeColor = theme.Text;
            if (IsHandleCreated) theme.ApplyToTitleBar(Handle);

            titleLabel.ForeColor = theme.Text;
            versionLabel.ForeColor = theme.SecondaryText;
            foreach (RoundedPanel card in new[] { addressCard, steeringCard, settingsCard })
            {
                card.FillColor = theme.Card;
                card.BorderColor = theme.CardBorder;
                foreach (Control child in card.Controls)
                {
                    child.BackColor = theme.Card;
                    child.ForeColor = theme.Text;
                }
            }
            foreach (Label secondary in new[] { addressCaption, portInfoLabel, hintLabel, sensitivityLabel, portCaption, languageCaption })
            {
                secondary.ForeColor = theme.SecondaryText;
            }

            firewallBanner.FillColor = theme.WarningBackground;
            firewallBanner.BorderColor = theme.WarningBackground;
            firewallLabel.BackColor = theme.WarningBackground;
            firewallLabel.ForeColor = theme.WarningText;
            StyleSecondary(allowButton, theme.WarningBackground);
            StyleSecondary(copyButton, theme.Card);

            sensitivitySlider.ParentColor = theme.Card;
            sensitivitySlider.FillColor = theme.Accent;
            sensitivitySlider.TrackColor = theme.ControlBorder;
            sensitivitySlider.ThumbColor = theme.Control;

            portBox.BackColor = theme.Control;
            portBox.ForeColor = theme.Text;
            languageBox.BackColor = theme.Control;
            languageBox.ForeColor = theme.Text;

            statusPill.ParentColor = theme.Background;
            statusPill.FillColor = theme.Card;
            statusPill.TextColor = theme.Text;
            ApplyStateTheme();
            Invalidate(true);
        }

        //Start is the main action (accent), Stop is a secondary one
        private void ApplyStateTheme()
        {
            switch (state)
            {
                case ServerState.ControllerConnected:
                    statusPill.DotColor = theme.Success;
                    break;
                case ServerState.WaitingForController:
                case ServerState.ControllerConnectedWithoutJoystick:
                    statusPill.DotColor = theme.Caution;
                    break;
                case ServerState.PortBusy:
                    statusPill.DotColor = theme.Error;
                    break;
                default:
                    statusPill.DotColor = theme.Neutral;
                    break;
            }
            if (IsRunning)
            {
                StyleSecondary(startStopButton, theme.Background);
            }
            else
            {
                startStopButton.FillColor = theme.Accent;
                startStopButton.TextColor = theme.OnAccent;
                startStopButton.BorderColor = Color.Transparent;
                startStopButton.ParentColor = theme.Background;
            }
            statusPill.Invalidate();
            startStopButton.Invalidate();
        }

        private void StyleSecondary(RoundedButton button, Color parent)
        {
            button.FillColor = theme.Control;
            button.TextColor = theme.Text;
            button.BorderColor = theme.ControlBorder;
            button.ParentColor = parent;
            button.Invalidate();
        }

        private void DrawLanguageItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            Color back = selected ? theme.Accent : theme.Control;
            Color fore = selected ? theme.OnAccent : theme.Text;
            using (var brush = new SolidBrush(back)) e.Graphics.FillRectangle(brush, e.Bounds);
            string name = LANGUAGES[e.Index].Name ?? Texts.Get(T.LanguageSystem);
            TextRenderer.DrawText(e.Graphics, name, languageBox.Font, e.Bounds, fore,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
        }

        /* Layout */

        private int Px(float value) => (int)Math.Round(value * DeviceDpi / 96f);

        private void LayoutContent()
        {
            int pad = Px(24);
            int width = Px(CONTENT_WIDTH);
            int gap = Px(12);
            int cardPad = Px(16);
            int inner = width - 2 * cardPad;

            SuspendLayout();
            int y = pad;

            //Header: title and the server state
            statusPill.Size = statusPill.GetPreferredSize(Size.Empty);
            int headerHeight = Math.Max(titleLabel.PreferredHeight, statusPill.Height);
            titleLabel.Location = new Point(pad - Px(2), y + (headerHeight - titleLabel.PreferredHeight) / 2);
            statusPill.Location = new Point(pad + width - statusPill.Width, y + (headerHeight - statusPill.Height) / 2);
            y += headerHeight + Px(16);

            //Address of the server: the most important thing for the user
            int cy = cardPad;
            addressCaption.Location = new Point(cardPad, cy);
            cy += addressCaption.PreferredHeight + Px(2);
            copyButton.Size = copyButton.GetPreferredSize(Size.Empty);
            int addressHeight = Math.Max(addressLabel.PreferredHeight, copyButton.Height);
            addressLabel.Location = new Point(cardPad - Px(4), cy + (addressHeight - addressLabel.PreferredHeight) / 2);
            copyButton.Location = new Point(width - cardPad - copyButton.Width, cy + (addressHeight - copyButton.Height) / 2);
            cy += addressHeight + Px(2);
            portInfoLabel.Location = new Point(cardPad, cy);
            cy += portInfoLabel.PreferredHeight + Px(10);
            hintLabel.Size = new Size(inner, MeasureWrapped(hintLabel, inner));
            hintLabel.Location = new Point(cardPad, cy);
            cy += hintLabel.Height + cardPad;
            addressCard.Bounds = new Rectangle(pad, y, width, cy);
            y += cy + gap;

            //Firewall warning: only when the phone may be blocked
            if (firewallBanner.Visible)
            {
                allowButton.Size = allowButton.GetPreferredSize(Size.Empty);
                int textWidth = width - 2 * cardPad - allowButton.Width - gap;
                firewallLabel.Size = new Size(textWidth, MeasureWrapped(firewallLabel, textWidth));
                int bannerHeight = Math.Max(firewallLabel.Height, allowButton.Height) + 2 * Px(12);
                firewallLabel.Location = new Point(cardPad, (bannerHeight - firewallLabel.Height) / 2);
                allowButton.Location = new Point(width - cardPad - allowButton.Width, (bannerHeight - allowButton.Height) / 2);
                firewallBanner.Bounds = new Rectangle(pad, y, width, bannerHeight);
                y += bannerHeight + gap;
            }

            //Steering
            cy = cardPad;
            steeringTitle.Location = new Point(cardPad, cy);
            cy += steeringTitle.PreferredHeight + Px(8);
            sensitivityLabel.Location = new Point(cardPad, cy);
            sensitivityValue.Location = new Point(width - cardPad - sensitivityValue.PreferredWidth, cy);
            cy += Math.Max(sensitivityLabel.PreferredHeight, sensitivityValue.PreferredHeight) + Px(4);
            sensitivitySlider.Bounds = new Rectangle(cardPad - Px(2), cy, inner + Px(4), Px(24));
            cy += sensitivitySlider.Height + cardPad - Px(4);
            steeringCard.Bounds = new Rectangle(pad, y, width, cy);
            y += cy + gap;

            //Settings: port and language side by side
            cy = cardPad;
            settingsTitle.Location = new Point(cardPad, cy);
            cy += settingsTitle.PreferredHeight + Px(8);
            int column = (inner - gap) / 2;
            portCaption.Location = new Point(cardPad, cy);
            languageCaption.Location = new Point(cardPad + column + gap, cy);
            cy += Math.Max(portCaption.PreferredHeight, languageCaption.PreferredHeight) + Px(4);
            portBox.Width = Px(110);
            portBox.Location = new Point(cardPad, cy);
            languageBox.Width = column;
            languageBox.ItemHeight = portBox.PreferredHeight - Px(6);
            languageBox.Location = new Point(cardPad + column + gap, cy);
            cy += Math.Max(portBox.PreferredHeight, languageBox.Height) + cardPad;
            settingsCard.Bounds = new Rectangle(pad, y, width, cy);
            y += cy + Px(20);

            //Main action and the version
            startStopButton.Size = startStopButton.GetPreferredSize(Size.Empty);
            startStopButton.Location = new Point(pad, y);
            versionLabel.Location = new Point(pad + width - versionLabel.PreferredWidth,
                y + (startStopButton.Height - versionLabel.PreferredHeight) / 2);
            y += startStopButton.Height + pad;

            ClientSize = new Size(width + 2 * pad, y);
            ResumeLayout(false);
            Invalidate(true);
        }

        private static int MeasureWrapped(Label label, int width)
        {
            return TextRenderer.MeasureText(label.Text, label.Font, new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
        }
    }
}
