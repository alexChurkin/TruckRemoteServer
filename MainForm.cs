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
            string strHostName = string.Empty;

            strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostByName(strHostName);
            IPAddress[] addr = ipEntry.AddressList;

            labelIp.Text = addr[0].ToString();

            StartUDPServer();
        }

        public void StartUDPServer()
        {
            server = new UDPServer();
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
            labelSensitivity.Text = (sensitivityTrackBar.Value - 9).ToString();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            server.Stop();
        }
    }
}