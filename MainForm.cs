using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TruckRemoteControlServer
{
    public partial class MainForm : Form
    {
        private UDPServer server;

        public MainForm()
        {
            InitializeComponent();
            StartUDPServer();
        }

        public void StartUDPServer()
        {
            server = new UDPServer();
            server.Start();
        }

        private void NumericUpPort_ValueChanged(object sender, EventArgs e)
        {
            //if(numericUpPort.text)
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
    }
}