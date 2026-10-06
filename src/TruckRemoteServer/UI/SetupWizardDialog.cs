using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using TruckRemoteServer.Input;
using TruckRemoteServer.Localization;
using TruckRemoteServer.Presentation;
using TruckRemoteServer.UI.Controls;
using T = TruckRemoteServer.Localization.TextKeys;

namespace TruckRemoteServer.UI
{
    //The setup wizard: a row for every step with its state and the button that fixes it, the phone step has
    //the QR code and the live controls of the phone (passive view of SetupWizard)
    public sealed class SetupWizardDialog : Form, ISetupWizardView
    {
        private const string QrScheme = "truckremote://";
        private const int TickInterval = 100;

        private readonly Theme theme;
        private readonly IWin32Window owner;
        private readonly Label titleLabel = new Label { AutoSize = true };
        private readonly Label subtitleLabel = new Label { AutoSize = false };
        private readonly Dictionary<SetupStep, StepRow> rows = new Dictionary<SetupStep, StepRow>();
        private readonly QrCodeView qrView = new QrCodeView();
        private readonly Label addressLabel = new Label { AutoSize = true };
        private readonly ControlsPreview preview = new ControlsPreview();
        private readonly RoundedButton doneButton = new RoundedButton();
        private readonly Timer timer = new Timer { Interval = TickInterval };

        public SetupWizardDialog(Theme theme, IWin32Window owner)
        {
            this.theme = theme;
            this.owner = owner;
            Text = Texts.Get(T.WizardTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            Font = Theme.Body;

            titleLabel.Font = Theme.Subtitle;
            titleLabel.Text = Texts.Get(T.WizardTitle);
            subtitleLabel.Font = Theme.Caption;
            subtitleLabel.Text = Texts.Get(T.WizardSubtitle);
            addressLabel.Font = Theme.BodyStrong;
            preview.Font = Theme.Caption;
            preview.SteeringLabel = Texts.Get(T.WizardSteering);
            preview.GasLabel = Texts.Get(T.WizardGas);
            preview.BrakeLabel = Texts.Get(T.WizardBrake);
            qrView.AccessibleName = Texts.Get(T.QrCode);
            doneButton.Text = Texts.Get(T.WizardDone);
            doneButton.Click += (s, e) => Close();
            Controls.AddRange(new Control[] { titleLabel, subtitleLabel, qrView, addressLabel, preview, doneButton });

            foreach (SetupStep step in (SetupStep[])Enum.GetValues(typeof(SetupStep)))
            {
                var row = new StepRow(step);
                SetupStep fixedStep = step;
                row.Button.Click += (s, e) => FixRequested?.Invoke(this, fixedStep);
                rows[step] = row;
                Controls.AddRange(new Control[] { row.Icon, row.Title, row.Detail, row.Button });
                row.Title.Text = Texts.Get(TitleKey(step));
            }

            timer.Tick += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
            AcceptButton = doneButton;
            //Esc closes the wizard too: everything is checked again from the Settings menu
            CancelButton = doneButton;
            ApplyTheme();
            LayoutContent();
        }

        public event EventHandler<SetupStep> FixRequested;
        public event EventHandler Tick;

        /* ISetupWizardView */

        public void ShowStep(SetupStep setupStep, StepState state, string detail)
        {
            StepRow row = rows[setupStep];
            row.Icon.State = state == StepState.Done ? StepIcon.Kind.Done
                : state == StepState.Failed ? StepIcon.Kind.Failed
                : state == StepState.NeedsAction ? StepIcon.Kind.Attention
                : StepIcon.Kind.Waiting;
            row.Detail.Text = DetailText(setupStep, state, detail);
            string button = ButtonText(setupStep, state);
            row.Button.Visible = button != null;
            row.Button.Enabled = state != StepState.Working;
            if (button != null) row.Button.Text = button;
            LayoutContent();
        }

        public void ShowAddresses(IList<string> addresses, int port)
        {
            bool hasAddress = addresses.Count > 0;
            qrView.Content = hasAddress ? QrScheme + addresses[0] + ":" + port.ToString(CultureInfo.InvariantCulture) : null;
            qrView.Visible = hasAddress;
            addressLabel.Text = hasAddress ? addresses[0] + " : " + port.ToString(CultureInfo.InvariantCulture) : "";
            LayoutContent();
        }

        public void ShowControls(ControlsSnapshot controls)
        {
            if (controls == null) preview.SetValues(false, 0, 0, 0);
            else preview.SetValues(true, controls.Steering, controls.Gas, controls.Brake);
        }

        public void ShowModal()
        {
            timer.Start();
            try
            {
                ShowDialog(owner);
            }
            finally
            {
                timer.Stop();
                Dispose();
            }
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            theme.ApplyToTitleBar(Handle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        /* Texts */

        private static string TitleKey(SetupStep step)
        {
            switch (step)
            {
                case SetupStep.TelemetryPlugin: return T.WizardPlugin;
                case SetupStep.Joystick: return T.WizardJoystick;
                case SetupStep.Firewall: return T.WizardFirewall;
                case SetupStep.GameControls: return T.WizardControls;
                default: return T.WizardPhone;
            }
        }

        private static string DetailText(SetupStep step, StepState state, string detail)
        {
            if (state == StepState.Checking) return Texts.Get(T.WizardChecking);
            if (state == StepState.Working) return Texts.Get(step == SetupStep.TelemetryPlugin ? T.WizardWorking : T.WizardWorkingAdmin);
            switch (step)
            {
                case SetupStep.TelemetryPlugin:
                    if (state == StepState.Done) return Texts.Get(T.WizardPluginDone);
                    return state == StepState.Failed && !string.IsNullOrEmpty(detail)
                        ? Texts.Format(T.WizardPluginFailed, detail)
                        : Texts.Get(T.WizardPluginMissing);
                case SetupStep.Joystick:
                    if (state == StepState.Done) return Texts.Get(T.WizardJoystickDone);
                    return Texts.Get(state == StepState.Failed ? T.WizardJoystickFailed : T.WizardJoystickMissing);
                case SetupStep.Firewall:
                    if (state == StepState.Done) return Texts.Get(T.WizardFirewallDone);
                    return Texts.Get(state == StepState.Failed ? T.WizardFirewallFailed : T.WizardFirewallBlocked);
                case SetupStep.GameControls:
                    return Texts.Get(T.WizardControlsDone);
                default:
                    if (state == StepState.Done) return Texts.Get(T.WizardPhoneDone);
                    return Texts.Get(state == StepState.Failed ? T.WizardPhoneNoServer : T.WizardPhoneWaiting);
            }
        }

        //null: the step has no button in this state
        private static string ButtonText(SetupStep step, StepState state)
        {
            bool fixable = step == SetupStep.TelemetryPlugin || step == SetupStep.Joystick || step == SetupStep.Firewall;
            if (!fixable || state == StepState.Done || state == StepState.Checking) return null;
            if (state == StepState.Failed) return Texts.Get(T.WizardRetry);
            return Texts.Get(step == SetupStep.Firewall ? T.FirewallAllow : T.JoystickInstall);
        }

        /* Look */

        private void ApplyTheme()
        {
            BackColor = theme.Background;
            ForeColor = theme.Text;
            subtitleLabel.ForeColor = theme.SecondaryText;
            addressLabel.ForeColor = theme.Text;
            qrView.ParentColor = theme.Background;
            preview.ParentColor = theme.Background;
            preview.TrackColor = theme.ControlBorder;
            preview.SteeringColor = theme.Accent;
            preview.GasColor = theme.Success;
            preview.BrakeColor = theme.Error;
            preview.TextColor = theme.SecondaryText;
            doneButton.FillColor = theme.Accent;
            doneButton.TextColor = theme.OnAccent;
            doneButton.BorderColor = Color.Transparent;
            doneButton.ParentColor = theme.Background;
            foreach (StepRow row in rows.Values)
            {
                row.Icon.ParentColor = theme.Background;
                row.Icon.DoneColor = theme.Success;
                row.Icon.AttentionColor = theme.Caution;
                row.Icon.FailedColor = theme.Error;
                row.Icon.WaitingColor = theme.Neutral;
                row.Icon.MarkColor = theme.Background;
                row.Title.ForeColor = theme.Text;
                row.Detail.ForeColor = theme.SecondaryText;
                row.Button.FillColor = theme.Control;
                row.Button.TextColor = theme.Text;
                row.Button.BorderColor = theme.ControlBorder;
                row.Button.ParentColor = theme.Background;
            }
        }

        private int Px(float value) => (int)Math.Round(value * DeviceDpi / 96f);

        private static int TextHeight(Label label, int width)
        {
            return TextRenderer.MeasureText(label.Text, label.Font, new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
        }

        private void LayoutContent()
        {
            int pad = Px(24);
            int width = Px(560);
            int icon = Px(22);
            int gap = Px(12);
            int y = pad;

            titleLabel.Location = new Point(pad, y);
            y += titleLabel.PreferredHeight + Px(4);
            subtitleLabel.Size = new Size(width, TextHeight(subtitleLabel, width));
            subtitleLabel.Location = new Point(pad, y);
            y += subtitleLabel.Height + Px(18);

            foreach (SetupStep step in (SetupStep[])Enum.GetValues(typeof(SetupStep)))
            {
                StepRow row = rows[step];
                int textLeft = pad + icon + gap;
                int buttonWidth = 0;
                if (row.Button.Visible)
                {
                    row.Button.Size = row.Button.GetPreferredSize(Size.Empty);
                    buttonWidth = row.Button.Width + gap;
                }
                int textWidth = width - icon - gap - buttonWidth;
                row.Icon.Bounds = new Rectangle(pad, y, icon, icon);
                row.Title.Location = new Point(textLeft, y);
                int titleHeight = row.Title.PreferredHeight;
                row.Detail.Size = new Size(textWidth, TextHeight(row.Detail, textWidth));
                row.Detail.Location = new Point(textLeft, y + titleHeight + Px(2));
                int rowHeight = Math.Max(icon, titleHeight + Px(2) + row.Detail.Height);
                if (row.Button.Visible)
                {
                    row.Button.Location = new Point(pad + width - row.Button.Width, y + (rowHeight - row.Button.Height) / 2);
                    rowHeight = Math.Max(rowHeight, row.Button.Height);
                }
                y += rowHeight + Px(16);

                //The phone's QR code and its controls under the phone step
                if (step == SetupStep.Phone)
                {
                    int qr = qrView.Visible ? Px(132) : 0;
                    qrView.Bounds = new Rectangle(textLeft, y, qr, qr);
                    addressLabel.Location = new Point(textLeft, y + qr + Px(6));
                    int previewLeft = textLeft + (qr > 0 ? qr + Px(24) : 0);
                    preview.Size = new Size(pad + width - previewLeft, preview.GetPreferredSize(Size.Empty).Height);
                    preview.Location = new Point(previewLeft, y + Math.Max(0, (qr - preview.Height) / 2));
                    y += Math.Max(qr + Px(6) + addressLabel.PreferredHeight, preview.Height) + Px(16);
                }
            }

            doneButton.Size = doneButton.GetPreferredSize(Size.Empty);
            doneButton.Location = new Point(pad + width - doneButton.Width, y);
            y += doneButton.Height + pad;
            ClientSize = new Size(width + 2 * pad, y);
        }

        //The controls of a step
        private sealed class StepRow
        {
            public StepRow(SetupStep step)
            {
                Icon = new StepIcon { AccessibleName = step.ToString() };
                Title = new Label { AutoSize = true, Font = Theme.BodyStrong };
                Detail = new Label { AutoSize = false, Font = Theme.Caption };
                Button = new RoundedButton { Visible = false };
            }

            public StepIcon Icon { get; }
            public Label Title { get; }
            public Label Detail { get; }
            public RoundedButton Button { get; }
        }
    }
}
