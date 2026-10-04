using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using TruckRemoteServer.Firewall;
using TruckRemoteServer.Setup;

namespace TruckRemoteServer
{
    public partial class MainForm : Form, UDPServer.IStatusListener
    {
        private readonly UDPServer server;
        private readonly ToolTip ipToolTip = new ToolTip();

        public MainForm()
        {
            InitializeComponent();
            int sensitivity = Properties.Settings.Default.Sensitivity;
            int port = (int)Properties.Settings.Default.Port;
            sensitivityTrackBar.Value = sensitivity;
            labelSensitivity.Text = sensitivity.ToString();
            PCController.SteeringSensitivity = sensitivity;
            numericUpPort.Value = port;
            server = new UDPServer(this, port);
        }

        protected override void OnShown(EventArgs e)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            ShowIpInLabel();
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            PluginInstaller installer = new PluginInstaller();
            if (installer.Status == SetupStatus.Uninstalled)
            {
                OnStatusUpdate(false, false, false);
                try
                {
                    installer.Install(this);
                }
                catch (Exception ex)
                {
                    //Controls still work without telemetry, so the server is started anyway
                    MessageBox.Show(this, "Telemetry plugin wasn't installed: " + ex.Message,
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            StartServer();
            CheckFirewall(offerFix: true);
        }

        /* Windows Firewall blocks the phone's packets when there is no allowing rule */

        private void CheckFirewall(bool offerFix)
        {
            string programPath = Application.ExecutablePath;
            int port = server.port;
            Task.Run(() => WindowsFirewall.Check(programPath, port))
                .ContinueWith(task => OnFirewallChecked(task.Result, offerFix),
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnFirewallChecked(FirewallStatus status, bool offerFix)
        {
            bool blocked = status == FirewallStatus.NoRule || status == FirewallStatus.Blocked;
            linkFirewall.Visible = blocked;

            //The question is asked once, later only the link is shown
            if (!blocked || !offerFix || Properties.Settings.Default.FirewallPromptShown) return;
            Properties.Settings.Default.FirewallPromptShown = true;
            Properties.Settings.Default.Save();

            DialogResult answer = MessageBox.Show(this,
                "Windows Firewall may block connections from the phone.\n\n" +
                "Allow Truck Remote Server in the firewall? Administrator rights are required.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer == DialogResult.Yes) AllowInFirewall();
        }

        private void AllowInFirewall()
        {
            string programPath = Application.ExecutablePath;
            int port = server.port;
            linkFirewall.Enabled = false;
            //null: elevation was cancelled, nothing has changed
            Task.Run(() => WindowsFirewall.AllowProgram(programPath)
                    ? WindowsFirewall.Check(programPath, port)
                    : (FirewallStatus?)null)
                .ContinueWith(task =>
                {
                    linkFirewall.Enabled = true;
                    if (task.Result == null) return;
                    OnFirewallChecked(task.Result.Value, offerFix: false);
                    if (linkFirewall.Visible)
                    {
                        MessageBox.Show(this, "The firewall rule wasn't applied. Please allow Truck Remote Server " +
                            "(UDP port " + port + ") in your firewall manually.",
                            Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void LinkFirewall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AllowInFirewall();
        }

        private void StartServer()
        {
            if (!server.Start())
            {
                ShowStatus("Port " + server.port + " is busy", Color.OrangeRed);
            }
        }

        //The most likely address is shown, all of them are in the tooltip (e.g. when a VPN or a virtual machine adapter exists)
        public void ShowIpInLabel()
        {
            List<IPAddress> ips = NetworkUtil.GetLocalIps();
            if (ips.Count == 0)
            {
                labelIp.Text = "Doesn't exist!";
                ipToolTip.SetToolTip(labelIp, null);
                return;
            }

            labelIp.Text = ips.Count == 1 ? ips[0].ToString() : ips[0] + " (+" + (ips.Count - 1) + ")";
            ipToolTip.SetToolTip(labelIp, ips.Count == 1 ? null :
                "Addresses of this PC (try the next one if the phone can't connect):\n" + string.Join("\n", ips));
        }

        //Wi-Fi reconnection or a new DHCP lease may change the address
        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            try
            {
                BeginInvoke((MethodInvoker)ShowIpInLabel);
            }
            catch (InvalidOperationException)
            {
                //The form is closed
            }
        }

        public void OnStatusUpdate(bool isEnabled, bool controllerConnected, bool controllerPaused)
        {
            if (isEnabled)
            {
                SetButtonsIsListening(true);

                if (controllerConnected)
                {
                    if (controllerPaused)
                    {
                        ShowStatus("Controller paused", Color.ForestGreen);
                    }
                    else if (!InputEmulator.IsJoyInitialized())
                    {
                        ShowStatus("Controller active, vJoy error", Color.DarkOrange);
                    }
                    else
                    {
                        ShowStatus("Controller active", Color.ForestGreen);
                    }
                }
                else
                {
                    ShowStatus("Enabled", Color.ForestGreen);
                }
            }
            else
            {
                SetButtonsIsListening(false);
                ShowStatus("Disabled", Color.OrangeRed);
            }
        }

        private void ShowStatus(string labelText, Color color)
        {
            try
            {
                labelStatus.BeginInvoke((MethodInvoker)delegate ()
                {
                    labelStatus.Text = labelText;
                    labelStatus.ForeColor = color;
                });
            }
            catch (Exception) { }
        }

        private void SetButtonsIsListening(bool isConnected)
        {
            try
            {
                buttonStop.BeginInvoke((MethodInvoker)delegate ()
            {
                buttonStop.Enabled = isConnected;
            });
                buttonStart.BeginInvoke((MethodInvoker)delegate ()
                {
                    buttonStart.Enabled = !isConnected;
                });
            }
            catch (Exception) { }
        }

        private void NumericUpPort_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Port = numericUpPort.Value;
            Properties.Settings.Default.Save();
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            server.Shutdown();
            buttonStop.Enabled = false;
            buttonStart.Enabled = true;
        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            server.port = (int)numericUpPort.Value;
            StartServer();
            //Allowing rules may be limited to a port
            CheckFirewall(offerFix: false);
        }

        private void SensitivityTrackBar_Scroll(object sender, EventArgs e)
        {
            PCController.SteeringSensitivity = sensitivityTrackBar.Value;
            labelSensitivity.Text = (sensitivityTrackBar.Value).ToString();

            Properties.Settings.Default.Sensitivity = sensitivityTrackBar.Value;
            Properties.Settings.Default.Save();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
            server.Shutdown();
            InputEmulator.ReleaseJoy();
        }
    }
}