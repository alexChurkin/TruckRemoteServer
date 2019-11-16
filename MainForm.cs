using System;
using System.Net;
using System.Windows.Forms;

namespace TruckRemoteControlServer
{
    public partial class MainForm : Form
    {
        private UDPServer server;

        public MainForm()
        {
            InitializeComponent();
            ShowIpInLabel();
            StartUDPServer();
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

        public void StartUDPServer()
        {
            server = new UDPServer(labelStatus, buttonStop, buttonStart);
            server.Start();
        }

        private void NumericUpPort_ValueChanged(object sender, EventArgs e)
        {
            if(numericUpPort.Text.Trim() == "" || numericUpPort.Text.Trim() == "0")
            {
                numericUpPort.ResetText();
            }
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

        private void SensitivityTrackBar_Scroll(object sender, EventArgs e)
        {
            PCController.Sensitivity = sensitivityTrackBar.Value;
            labelSensitivity.Text = (sensitivityTrackBar.Value).ToString();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            server.Stop();
        }

        private void BackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {

        }
    }
}