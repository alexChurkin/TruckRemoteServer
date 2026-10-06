using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
    //Built in code and laid out by LayoutContent(): texts of different languages have different lengths.
    //Everything the phone needs is in the window (state, address, QR code), the rarely changed settings
    //(port, language, minimizing to the notification area) are in the Settings menu
    public sealed class MainForm : Form, IMainView
    {
        private const int ContentWidth = 440;
        private const int QrSize = 120;
        //Scanned by the app: the server address and port
        private const string QrScheme = "truckremote://";

        private static readonly (string Code, string Name)[] Languages =
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
        private readonly QrCodeView qrView = new QrCodeView();
        private readonly Label hintLabel = new Label { AutoSize = false };

        private readonly RoundedPanel firewallBanner = new RoundedPanel { Visible = false };
        private readonly Label firewallLabel = new Label { AutoSize = false };
        private readonly RoundedButton allowButton = new RoundedButton();

        private readonly RoundedPanel joystickBanner = new RoundedPanel { Visible = false };
        private readonly Label joystickLabel = new Label { AutoSize = false };
        private readonly RoundedButton installJoystickButton = new RoundedButton();
        private readonly RoundedPanel updateBanner = new RoundedPanel { Visible = false };
        private readonly Label updateLabel = new Label { AutoSize = false };
        private readonly RoundedButton updateButton = new RoundedButton();
        //The version offered by the banner
        private string updateVersion;

        private readonly RoundedButton startStopButton = new RoundedButton();
        private readonly RoundedButton settingsButton = new RoundedButton { ShowChevron = true };
        private readonly ContextMenuStrip settingsMenu = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = true };
        private readonly ToolStripMenuItem portItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem languageItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem minimizeToTrayItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem wizardItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem updatesItem = new ToolStripMenuItem();
        private readonly Label versionLabel = new Label { AutoSize = true };

        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private readonly ContextMenuStrip trayMenu = new ContextMenuStrip { ShowImageMargin = false };
        private readonly ToolStripMenuItem trayOpenItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem trayExitItem = new ToolStripMenuItem();
        //The hint about the notification area is shown once per start
        private bool trayHintShown;

        private Theme theme = Theme.FromSystem();
        private IList<string> addresses = new List<string>();
        private int port;
        private ServerState state = ServerState.Stopped;
        private string language = "";
        private bool minimizeToTray;

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
            versionLabel.Font = Theme.Caption;

            addressCard.Controls.AddRange(new Control[] { addressCaption, addressLabel, copyButton, portInfoLabel, qrView, hintLabel });
            firewallBanner.Controls.AddRange(new Control[] { firewallLabel, allowButton });
            joystickBanner.Controls.AddRange(new Control[] { joystickLabel, installJoystickButton });
            updateBanner.Controls.AddRange(new Control[] { updateLabel, updateButton });
            Controls.AddRange(new Control[]
            {
                titleLabel, statusPill, addressCard, updateBanner, joystickBanner, firewallBanner,
                startStopButton, settingsButton, versionLabel
            });

            foreach (var item in Languages)
            {
                string code = item.Code;
                var menuItem = new ToolStripMenuItem { Tag = code, Font = Theme.Body };
                menuItem.Click += (s, e) => OnLanguageSelected(code);
                languageItem.DropDownItems.Add(menuItem);
            }
            portItem.Click += (s, e) => ChangePort();
            minimizeToTrayItem.Click += (s, e) => MinimizeToTrayChanged?.Invoke(this, !minimizeToTray);
            wizardItem.Click += (s, e) => SetupWizardRequested?.Invoke(this, EventArgs.Empty);
            updatesItem.Click += (s, e) => CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);
            settingsMenu.Items.AddRange(new ToolStripItem[]
            {
                portItem, languageItem, minimizeToTrayItem, new ToolStripSeparator(), wizardItem, updatesItem
            });
            settingsMenu.Font = Theme.Body;
            settingsButton.Click += (s, e) => settingsMenu.Show(settingsButton, 0, settingsButton.Height + Px(2));

            trayOpenItem.Click += (s, e) => RestoreFromTray();
            trayExitItem.Click += (s, e) => Close();
            trayMenu.Items.AddRange(new ToolStripItem[] { trayOpenItem, trayExitItem });
            trayMenu.Font = Theme.Body;
            trayIcon.Icon = Icon;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();
            trayIcon.BalloonTipClicked += (s, e) => RestoreFromTray();

            copyButton.Click += (s, e) => CopyAddress();
            copiedTimer.Tick += (s, e) =>
            {
                copiedTimer.Stop();
                copyButton.Text = Texts.Get(T.Copy);
                LayoutContent();
            };
            allowButton.Click += (s, e) => AllowFirewallRequested?.Invoke(this, EventArgs.Empty);
            installJoystickButton.Click += (s, e) => InstallJoystickRequested?.Invoke(this, EventArgs.Empty);
            updateButton.Click += (s, e) => UpdateRequested?.Invoke(this, EventArgs.Empty);
            startStopButton.Click += (s, e) => (IsRunning ? StopRequested : StartRequested)?.Invoke(this, EventArgs.Empty);
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

            ApplyTexts();
            ApplyTheme();
        }

        public new event EventHandler Shown;
        public new event EventHandler Closing;
        public event EventHandler StartRequested;
        public event EventHandler StopRequested;
        public event EventHandler<int> PortChanged;
        public event EventHandler<string> LanguageChanged;
        public event EventHandler<bool> MinimizeToTrayChanged;
        public event EventHandler AllowFirewallRequested;
        public event EventHandler InstallJoystickRequested;
        public event EventHandler SetupWizardRequested;
        public event EventHandler UpdateRequested;
        public event EventHandler CheckForUpdatesRequested;

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

        //If the user chose so, the minimized window is hidden to the notification area (the server keeps working there)
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!minimizeToTray || WindowState != FormWindowState.Minimized || !Visible) return;
            trayIcon.Visible = true;
            Hide();
            if (trayHintShown) return;
            trayHintShown = true;
            trayIcon.ShowBalloonTip(3000, Text, Texts.Get(T.TrayHint), ToolTipIcon.Info);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Closing?.Invoke(this, EventArgs.Empty);
            trayIcon.Visible = false;
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                toolTip.Dispose();
                copiedTimer.Dispose();
                settingsMenu.Dispose();
                trayMenu.Dispose();
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }

        /* IMainView */

        public void ShowLanguage(string language)
        {
            this.language = language ?? "";
            Texts.SetLanguage(this.language);
            ApplyTexts();
        }

        public void ShowSettings(int port, bool minimizeToTray)
        {
            this.port = port;
            this.minimizeToTray = minimizeToTray;
            ApplyMenuTexts();
            LayoutContent();
        }

        public void ShowAddresses(IList<string> addresses, int port)
        {
            this.addresses = addresses;
            this.port = port;
            ApplyAddressTexts();
            ApplyMenuTexts();
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

        public void ShowUpdate(string version, bool busy)
        {
            updateVersion = version;
            updateBanner.Visible = version != null;
            updateButton.Enabled = !busy;
            ApplyUpdateTexts(busy);
            LayoutContent();
        }

        public void ShowUpToDate()
        {
            MessageBox.Show(this, Texts.Get(T.UpToDate), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void CloseForUpdate()
        {
            Close();
        }

        public void ShowJoystickWarning(bool visible, bool busy)
        {
            joystickBanner.Visible = visible;
            installJoystickButton.Enabled = !busy;
            LayoutContent();
        }

        public bool AskInstallJoystick()
        {
            return MessageBox.Show(this, Texts.Get(T.JoystickQuestion), Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        public bool AskAllowFirewall()
        {
            return MessageBox.Show(this, Texts.Get(T.FirewallQuestion), Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        public void ShowWarning(Warning warning, string detail)
        {
            string text;
            switch (warning)
            {
                case Warning.TelemetryPluginNotInstalled:
                    text = Texts.Format(T.PluginNotInstalled, detail);
                    break;
                case Warning.FirewallRuleNotApplied:
                    text = Texts.Format(T.FirewallNotApplied, detail);
                    break;
                default:
                    text = Texts.Get(T.JoystickSetupFailed);
                    break;
            }
            MessageBox.Show(this, text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public ISetupWizardView CreateSetupWizard()
        {
            //The window may be hidden in the notification area
            RestoreFromTray();
            return new SetupWizardDialog(theme, this);
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

        private void ChangePort()
        {
            using (var dialog = new PortDialog(theme, port))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Port != port) PortChanged?.Invoke(this, dialog.Port);
            }
        }

        private void OnLanguageSelected(string code)
        {
            if (code != language) LanguageChanged?.Invoke(this, code);
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
            Activate();
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
            joystickLabel.Text = Texts.Get(T.JoystickWarning);
            installJoystickButton.Text = Texts.Get(T.JoystickInstall);
            ApplyUpdateTexts(!updateButton.Enabled);
            settingsButton.Text = Texts.Get(T.SettingsTitle);
            trayOpenItem.Text = Texts.Get(T.TrayOpen);
            trayExitItem.Text = Texts.Get(T.TrayExit);
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            versionLabel.Text = Texts.Format(T.Version, version.ToString(3));
            ApplyMenuTexts();
            ApplyAddressTexts();
            ApplyStateTexts();
            LayoutContent();
        }

        private void ApplyMenuTexts()
        {
            portItem.Text = Texts.Format(T.PortItem, port);
            string systemName = Texts.Get(T.LanguageSystem);
            string languageName = Array.Find(Languages, l => l.Code == language).Name ?? systemName;
            languageItem.Text = Texts.Get(T.Language) + ": " + languageName;
            minimizeToTrayItem.Text = Texts.Get(T.MinimizeToTray);
            minimizeToTrayItem.Checked = minimizeToTray;
            wizardItem.Text = Texts.Get(T.WizardMenu);
            updatesItem.Text = Texts.Get(T.CheckForUpdates);
            foreach (ToolStripMenuItem item in languageItem.DropDownItems)
            {
                string code = (string)item.Tag;
                item.Text = Array.Find(Languages, l => l.Code == code).Name ?? systemName;
                item.Checked = code == language;
                item.AccessibleName = item.Text;
            }
        }

        private void ApplyAddressTexts()
        {
            bool hasAddress = addresses.Count > 0;
            addressLabel.Text = hasAddress ? addresses[0] : Texts.Get(T.NoAddress);
            addressLabel.Font = hasAddress ? Theme.Display : Theme.Subtitle;
            copyButton.Visible = hasAddress;
            qrView.Visible = hasAddress;
            qrView.Content = hasAddress ? QrScheme + addresses[0] + ":" + port.ToString(CultureInfo.InvariantCulture) : null;
            qrView.AccessibleName = Texts.Get(T.QrCode);
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
            //The tooltip of the notification area is limited to 63 characters
            string trayText = Text + " — " + statusPill.Text;
            trayIcon.Text = trayText.Length > 63 ? trayText.Substring(0, 63) : trayText;
        }

        /* Theme */

        private void ApplyTheme()
        {
            BackColor = theme.Background;
            ForeColor = theme.Text;
            if (IsHandleCreated) theme.ApplyToTitleBar(Handle);

            titleLabel.ForeColor = theme.Text;
            versionLabel.ForeColor = theme.SecondaryText;
            addressCard.FillColor = theme.Card;
            addressCard.BorderColor = theme.CardBorder;
            foreach (Control child in addressCard.Controls)
            {
                child.BackColor = theme.Card;
                child.ForeColor = theme.Text;
            }
            foreach (Label secondary in new[] { addressCaption, portInfoLabel, hintLabel })
            {
                secondary.ForeColor = theme.SecondaryText;
            }
            qrView.ParentColor = theme.Card;

            foreach (RoundedPanel banner in new[] { firewallBanner, joystickBanner })
            {
                banner.FillColor = theme.WarningBackground;
                banner.BorderColor = theme.WarningBackground;
            }
            foreach (Label bannerText in new[] { firewallLabel, joystickLabel })
            {
                bannerText.BackColor = theme.WarningBackground;
                bannerText.ForeColor = theme.WarningText;
            }
            StyleSecondary(allowButton, theme.WarningBackground);
            StyleSecondary(installJoystickButton, theme.WarningBackground);
            //The update is good news: a card with the accent button
            updateBanner.FillColor = theme.Card;
            updateBanner.BorderColor = theme.CardBorder;
            updateLabel.BackColor = theme.Card;
            updateLabel.ForeColor = theme.Text;
            updateButton.FillColor = theme.Accent;
            updateButton.TextColor = theme.OnAccent;
            updateButton.BorderColor = Color.Transparent;
            updateButton.ParentColor = theme.Card;
            StyleSecondary(copyButton, theme.Card);
            StyleSecondary(settingsButton, theme.Background);

            foreach (ContextMenuStrip menu in new[] { settingsMenu, trayMenu })
            {
                StyleMenu(menu);
            }
            StyleMenu(languageItem.DropDown);

            statusPill.ParentColor = theme.Background;
            statusPill.FillColor = theme.Card;
            statusPill.TextColor = theme.Text;
            ApplyStateTheme();
            Invalidate(true);
        }

        private void StyleMenu(ToolStripDropDown menu)
        {
            menu.Renderer = new ThemedMenuRenderer(theme.Card, theme.ControlBorder,
                theme.IsDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(234, 234, 234));
            menu.BackColor = theme.Card;
            foreach (ToolStripItem item in menu.Items) item.ForeColor = theme.Text;
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

        /* Layout */

        private int Px(float value) => (int)Math.Round(value * DeviceDpi / 96f);

        private void LayoutContent()
        {
            int pad = Px(24);
            int width = Px(ContentWidth);
            int gap = Px(12);
            int cardPad = Px(16);
            int inner = width - 2 * cardPad;

            SuspendLayout();
            int y = pad;

            //Header: title and the server state under it (long states don't fit beside the title)
            titleLabel.Location = new Point(pad - Px(2), y);
            y += titleLabel.PreferredHeight + Px(6);
            statusPill.Size = statusPill.GetPreferredSize(Size.Empty);
            statusPill.Location = new Point(pad, y);
            y += statusPill.Height + Px(16);

            //Address of the server: the most important thing for the user, with its QR code for the app
            int qr = qrView.Visible ? Px(QrSize) : 0;
            int cy = cardPad;
            addressCaption.Location = new Point(cardPad, cy);
            cy += addressCaption.PreferredHeight + Px(2);
            addressLabel.Location = new Point(cardPad - Px(4), cy);
            cy += addressLabel.PreferredHeight + Px(2);
            portInfoLabel.Location = new Point(cardPad, cy);
            cy += portInfoLabel.PreferredHeight + Px(10);
            if (copyButton.Visible)
            {
                copyButton.Size = copyButton.GetPreferredSize(Size.Empty);
                copyButton.Location = new Point(cardPad, cy);
                cy += copyButton.Height;
            }
            int textHeight = cy;
            if (qrView.Visible)
            {
                qrView.Bounds = new Rectangle(width - cardPad - qr, cardPad, qr, qr);
                cy = Math.Max(cy, cardPad + qr);
            }
            cy += Px(12);
            hintLabel.Size = new Size(inner, MeasureWrapped(hintLabel, inner));
            hintLabel.Location = new Point(cardPad, cy);
            cy += hintLabel.Height + cardPad;
            //The text column is centered beside the code
            if (qrView.Visible && textHeight < cardPad + qr)
            {
                int shift = (cardPad + qr - textHeight) / 2;
                foreach (Control control in new Control[] { addressCaption, addressLabel, portInfoLabel, copyButton })
                {
                    control.Top += shift;
                }
            }
            addressCard.Bounds = new Rectangle(pad, y, width, cy);
            y += cy + gap;

            //A newer version, then warnings: vJoy isn't ready (the phone can't steer), the firewall may block the phone
            y = LayoutBanner(updateBanner, updateLabel, updateButton, y);
            y = LayoutBanner(joystickBanner, joystickLabel, installJoystickButton, y);
            y = LayoutBanner(firewallBanner, firewallLabel, allowButton, y);
            y += Px(8);

            //Main action, settings and the version
            startStopButton.Size = startStopButton.GetPreferredSize(Size.Empty);
            startStopButton.Location = new Point(pad, y);
            settingsButton.Size = settingsButton.GetPreferredSize(Size.Empty);
            settingsButton.Location = new Point(startStopButton.Right + Px(8), y);
            versionLabel.Location = new Point(pad + width - versionLabel.PreferredWidth,
                y + (startStopButton.Height - versionLabel.PreferredHeight) / 2);
            y += startStopButton.Height + pad;

            ClientSize = new Size(width + 2 * pad, y);
            ResumeLayout(false);
            Invalidate(true);
        }

        private void ApplyUpdateTexts(bool busy)
        {
            updateLabel.Text = updateVersion == null ? "" : Texts.Format(T.UpdateAvailable, updateVersion);
            updateButton.Text = Texts.Get(busy ? T.UpdateInstalling : T.UpdateInstall);
        }

        //A warning banner (text and a button) at y, returns y under it
        private int LayoutBanner(RoundedPanel banner, Label text, RoundedButton button, int y)
        {
            if (!banner.Visible) return y;
            int pad = Px(24);
            int width = Px(ContentWidth);
            int cardPad = Px(16);
            int gap = Px(12);
            button.Size = button.GetPreferredSize(Size.Empty);
            int textWidth = width - 2 * cardPad - button.Width - gap;
            text.Size = new Size(textWidth, MeasureWrapped(text, textWidth));
            int bannerHeight = Math.Max(text.Height, button.Height) + 2 * Px(12);
            text.Location = new Point(cardPad, (bannerHeight - text.Height) / 2);
            button.Location = new Point(width - cardPad - button.Width, (bannerHeight - button.Height) / 2);
            banner.Bounds = new Rectangle(pad, y, width, bannerHeight);
            return y + bannerHeight + gap;
        }

        private static int MeasureWrapped(Label label, int width)
        {
            return TextRenderer.MeasureText(label.Text, label.Font, new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
        }
    }
}
