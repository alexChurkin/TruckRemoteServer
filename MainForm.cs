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
            InitialSetup();
        }

        private void InitialSetup()
        {
            ShowIpInLabel();

            decimal port = Properties.Settings.Default.Port;
            numericUpPort.Value = port;

            server = new UDPServer(labelStatus, buttonStop, buttonStart);
            server.port = (int)port;

            if (Properties.Settings.Default.StartServerOnStartup)
            {
                server.Start();
            }

            if (Properties.Settings.Default.StartMinimized)
                this.WindowState = FormWindowState.Minimized;
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

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            server.Stop();
            buttonStop.Enabled = false;
            buttonStart.Enabled = true;
        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            server.port = (int) numericUpPort.Value;
            server.Start();
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
            server.UpdateUiState();
        }
    }
}