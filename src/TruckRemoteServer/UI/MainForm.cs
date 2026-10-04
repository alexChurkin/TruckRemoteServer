using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TruckRemoteServer.Presentation;

namespace TruckRemoteServer.UI
{
    //Passive view: shows what MainPresenter tells and reports user actions
    public partial class MainForm : Form, IMainView
    {
        private readonly ToolTip ipToolTip = new ToolTip();
        //Settings shown by the presenter mustn't be reported back as user changes
        private bool showingSettings;

        public MainForm()
        {
            InitializeComponent();
        }

        public new event EventHandler Shown;
        public new event EventHandler Closing;
        public event EventHandler StartRequested;
        public event EventHandler StopRequested;
        public event EventHandler<int> PortChanged;
        public event EventHandler<int> SensitivityChanged;
        public event EventHandler AllowFirewallRequested;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Shown?.Invoke(this, EventArgs.Empty);
        }

        public void ShowSettings(int port, int sensitivity)
        {
            showingSettings = true;
            numericUpPort.Value = port;
            sensitivityTrackBar.Value = sensitivity;
            labelSensitivity.Text = sensitivity.ToString();
            showingSettings = false;
        }

        //The most likely address is shown, all of them are in the tooltip (e.g. when a VPN or a virtual machine adapter exists)
        public void ShowAddresses(IList<string> addresses)
        {
            if (addresses.Count == 0)
            {
                labelIp.Text = "Doesn't exist!";
                ipToolTip.SetToolTip(labelIp, null);
                return;
            }
            labelIp.Text = addresses.Count == 1 ? addresses[0] : addresses[0] + " (+" + (addresses.Count - 1) + ")";
            ipToolTip.SetToolTip(labelIp, addresses.Count == 1 ? null :
                "Addresses of this PC (try the next one if the phone can't connect):\n" + string.Join("\n", addresses));
        }

        public void ShowStatus(string text, StatusKind kind)
        {
            labelStatus.Text = text;
            labelStatus.ForeColor = kind == StatusKind.Ok ? Color.ForestGreen
                : kind == StatusKind.Warning ? Color.DarkOrange : Color.OrangeRed;
        }

        public void ShowRunning(bool running)
        {
            buttonStop.Enabled = running;
            buttonStart.Enabled = !running;
        }

        public void ShowFirewallWarning(bool visible, bool busy)
        {
            linkFirewall.Visible = visible;
            linkFirewall.Enabled = !busy;
        }

        public bool AskAllowFirewall()
        {
            return MessageBox.Show(this,
                "Windows Firewall may block connections from the phone.\n\n" +
                "Allow Truck Remote Server in the firewall? Administrator rights are required.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void NumericUpPort_ValueChanged(object sender, EventArgs e)
        {
            if (!showingSettings) PortChanged?.Invoke(this, (int)numericUpPort.Value);
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            StartRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SensitivityTrackBar_Scroll(object sender, EventArgs e)
        {
            labelSensitivity.Text = sensitivityTrackBar.Value.ToString();
            if (!showingSettings) SensitivityChanged?.Invoke(this, sensitivityTrackBar.Value);
        }

        private void LinkFirewall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AllowFirewallRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }
    }
}
