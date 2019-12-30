using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using TruckRemoteServer.Setup;

namespace TruckRemoteServer
{
    public partial class MainForm : Form, UDPServer.IStatusListener
    {
        private readonly UDPServer server;

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
            PluginInstaller installer = new PluginInstaller();
            if (installer.Status == SetupStatus.Uninstalled)
            {
                OnStatusUpdate(false, false, false, false, false);
                installer.Install(this);
                server.Start();
            } else
            {
                server.Start();
            }
        }

        public void ShowIpInLabel()
        {
            IPAddress ip = NetworkUtil.GetLocalIp();
            if (ip != null)
            {
                labelIp.Text = ip.ToString();
            }
            else
            {
                labelIp.Text = "Not exist!";
            }
        }

        public void OnStatusUpdate(bool isEnabled,
            bool controllerConnected, bool panelConnected,
            bool controllerPaused, bool panelPaused)
        {
            if (isEnabled)
            {
                SetButtonsIsListening(true);
                //All devices connected
                if (controllerConnected && panelConnected)
                {
                    if (controllerPaused && panelPaused)
                    {
                        ShowStatus("All devices paused", Color.ForestGreen);
                    }
                    else if (controllerPaused)
                    {
                        ShowStatus("Controller: paused\nPanel: active", Color.ForestGreen);
                    }
                    else if (panelPaused)
                    {
                        ShowStatus("Controller: active\nPanel: paused", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("All devices active", Color.ForestGreen);
                    }
                }
                //Connected only controller
                else if (controllerConnected)
                {
                    if (controllerPaused)
                    {
                        ShowStatus("Controller paused", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("Controller active", Color.ForestGreen);
                    }
                }
                //Connected only panel
                else if (panelConnected)
                {
                    if (panelPaused)
                    {
                        ShowStatus("Panel paused", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("Panel active", Color.ForestGreen);
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
            if (numericUpPort.Text.Trim() == "" || numericUpPort.Text.Trim() == "0")
            {
                numericUpPort.ResetText();
                Properties.Settings.Default.Port = 18250;
            }
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
            server.Start();
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
            server.Shutdown();
            InputEmulator.ReleaseJoy();
        }
    }
}