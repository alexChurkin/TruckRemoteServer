using System;
using System.Net;
using System.Windows.Forms;

namespace TruckRemoteServer
{
    public partial class MainForm : Form
    {
        private UDPServer server;

        public MainForm()
        {
            InitializeComponent();
            int sensitivity = Properties.Settings.Default.Sensitivity;
            decimal port = Properties.Settings.Default.Port;
            sensitivityTrackBar.Value = sensitivity;
            labelSensitivity.Text = sensitivity.ToString();
            PCController.SteeringSensitivity = sensitivity;
            numericUpPort.Value = port;
            //Notify Icon
            notifyIconTray.Visible = false;
            notifyIconTray.ContextMenuStrip = contextMenuStripNotifyIconTray;

            ShowIpInLabel();

            server = new UDPServer(labelStatus);
            server.port = (int) port;
            server.Start();
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
            //buttonStop.Enabled = false;
            //buttonStart.Enabled = true;
            buttonStartStopServer.Text = "Start";
            buttonStartStopServer.Click -= ButtonStop_Click;
            buttonStartStopServer.Click += ButtonStart_Click;

            StartStopServerToolStripMenuItem.Text = "Start Server";
            StartStopServerToolStripMenuItem.Click -= ButtonStop_Click;
            StartStopServerToolStripMenuItem.Click += ButtonStart_Click;
        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            server.port = (int) numericUpPort.Value;
            server.Start();

            buttonStartStopServer.Text = "Stop";
            buttonStartStopServer.Click -= ButtonStart_Click;
            buttonStartStopServer.Click += ButtonStop_Click;

            StartStopServerToolStripMenuItem.Text = "Stop Server";
            StartStopServerToolStripMenuItem.Click -= ButtonStart_Click;
            StartStopServerToolStripMenuItem.Click += ButtonStop_Click;
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
            server.Stop();
            InputEmulator.ReleaseJoy();
        }

        private void BackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {

        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIconTray.Visible = true;
                //this.ShowInTaskbar = false;
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
                //this.ShowInTaskbar = true;
            }
        }
    }
}