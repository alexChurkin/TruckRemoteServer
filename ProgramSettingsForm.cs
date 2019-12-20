using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TruckRemoteServer
{
    public partial class ProgramSettingsForm : Form
    {
        public ProgramSettingsForm()
        {
            InitializeComponent();
            InitialSetup();
        }

        private void InitialSetup()
        {
            //Start on Startup
            checkBoxStartServerOnStartup.Checked = Properties.Settings.Default.StartServerOnStartup;
            //Start Minimized
            checkBoxStartMinimized.Checked = Properties.Settings.Default.StartMinimized;
            //Sensitivity
            int sensitivity = Properties.Settings.Default.Sensitivity;
            sensitivityTrackBar.Value = sensitivity;
            labelSensitivity.Text = sensitivity.ToString();
        }

        private void buttonSaveSettings_Click(object sender, EventArgs e)
        {
            //Start on Startup
            Properties.Settings.Default.StartServerOnStartup = checkBoxStartServerOnStartup.Checked;
            //Start Minimized
            Properties.Settings.Default.StartMinimized = checkBoxStartMinimized.Checked;

            //Sensitivity
            PCController.SteeringSensitivity = sensitivityTrackBar.Value;
            Properties.Settings.Default.Sensitivity = sensitivityTrackBar.Value;

            //Save
            Properties.Settings.Default.Save();
        }

        private void sensitivityTrackBar_Scroll(object sender, EventArgs e)
        {
            labelSensitivity.Text = (sensitivityTrackBar.Value).ToString();
        }
    }
}
