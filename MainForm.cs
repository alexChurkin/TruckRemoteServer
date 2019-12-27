using System;
using System.Net;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace TruckRemoteServer
{
    public partial class MainForm : Form
    {
        public UDPServer server;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitialSetup()
        {
            ShowIpInLabel();

            decimal port = Properties.Settings.Default.Port;
            numericUpPort.Value = port;
            //Notify Icon
            notifyIconTray.Visible = false;
            notifyIconTray.ContextMenuStrip = contextMenuStripNotifyIconTray;

            server = new UDPServer(labelStatus, buttonStartStopServer);
            ControlMappingRW ControlMapping = new ControlMappingRW();
            ControlMapping.LoadControlMapping();
            server.port = (int)port;

            if (Properties.Settings.Default.StartServerOnStartup)
            {
                buttonStartStopServer.Text = "Start";
                server.Start();
            }                

            if (Properties.Settings.Default.StartMinimized)
                this.WindowState = FormWindowState.Minimized;

            StartStopButtonProperties();
        }

        public void ShowIpInLabel()
        {
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList;

            string addr192String = string.Empty;

            foreach (IPAddress address in addr)
            {
                string addressString = address.ToString();
                if (addressString.StartsWith("192"))
                {
                    addr192String = addressString;
                    break;
                }
            }

            if (addr192String != string.Empty)
            {
                labelIp.Text = addr192String;
            }
            else
            {
                labelIp.Text = addr[0].ToString();
            }
        }

        private void NumericUpPort_ValueChanged(object sender, EventArgs e)
        {
            if(numericUpPort.Text.Trim() == "" || numericUpPort.Text.Trim() == "0")
            {
                numericUpPort.ResetText();
                Properties.Settings.Default.Port = 18250;
            }
            Properties.Settings.Default.Port = numericUpPort.Value;
            Properties.Settings.Default.Save();
        }

        internal void ButtonStop_Click(object sender, EventArgs e)
        {
            server.Stop();
        }
        internal void ButtonStart_Click(object sender, EventArgs e)
        {
            server.port = (int) numericUpPort.Value;
            server.Start();
        }

        internal void StartStopButtonProperties()
        {
            if (server.enabled && buttonStartStopServer.Text == "Start")
            {
                buttonStartStopServer.Text = "Stop";
                buttonStartStopServer.Click -= ButtonStart_Click;
                buttonStartStopServer.Click += ButtonStop_Click;

                StartStopServerToolStripMenuItem.Text = "Stop Server";
                StartStopServerToolStripMenuItem.Click -= ButtonStart_Click;
                StartStopServerToolStripMenuItem.Click += ButtonStop_Click;
            }
            else if (!server.enabled && buttonStartStopServer.Text == "Stop")
            {
                buttonStartStopServer.Text = "Start";
                buttonStartStopServer.Click -= ButtonStop_Click;
                buttonStartStopServer.Click += ButtonStart_Click;

                StartStopServerToolStripMenuItem.Text = "Start Server";
                StartStopServerToolStripMenuItem.Click -= ButtonStop_Click;
                StartStopServerToolStripMenuItem.Click += ButtonStart_Click;
            }
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            server.Stop();
            InputEmulator.ReleaseJoy();
        }

        private void controlMappingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ControlMappingForm controlMappingForm = new ControlMappingForm();
            controlMappingForm.ShowDialog();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProgramSettingsForm Form = new ProgramSettingsForm();
            Form.ShowDialog();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitialSetup();
            server.UpdateUiState();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized && Properties.Settings.Default.MinimizeToTray)
            {
                Hide();
                notifyIconTray.Visible = true;
                this.ShowInTaskbar = false;
            }
        }

        private void notifyIconTray_Click(object sender, EventArgs e)
        {

        }

        private void notifyIconTray_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Show();
                this.WindowState = FormWindowState.Normal;
                notifyIconTray.Visible = false;
                this.ShowInTaskbar = true;
            }
        }
    }
}