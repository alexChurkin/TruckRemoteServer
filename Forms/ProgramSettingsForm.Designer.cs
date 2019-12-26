namespace TruckRemoteServer
{
    partial class ProgramSettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxStartMinimized = new System.Windows.Forms.CheckBox();
            this.labelStartServerOnStartup = new System.Windows.Forms.Label();
            this.labelStartMinimized = new System.Windows.Forms.Label();
            this.labelSteeringSensitivity = new System.Windows.Forms.Label();
            this.checkBoxStartServerOnStartup = new System.Windows.Forms.CheckBox();
            this.sensitivityTrackBar = new System.Windows.Forms.TrackBar();
            this.labelSensitivity = new System.Windows.Forms.Label();
            this.buttonSaveSettings = new System.Windows.Forms.Button();
            this.labelMinimizeToTray = new System.Windows.Forms.Label();
            this.checkBoxMinimizeToTray = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.checkBoxStartMinimized, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelStartServerOnStartup, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelStartMinimized, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxStartServerOnStartup, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonSaveSettings, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.labelSensitivity, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.sensitivityTrackBar, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelSteeringSensitivity, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelMinimizeToTray, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxMinimizeToTray, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(315, 264);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // checkBoxStartMinimized
            // 
            this.checkBoxStartMinimized.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxStartMinimized.AutoSize = true;
            this.checkBoxStartMinimized.Location = new System.Drawing.Point(133, 30);
            this.checkBoxStartMinimized.Name = "checkBoxStartMinimized";
            this.checkBoxStartMinimized.Size = new System.Drawing.Size(15, 14);
            this.checkBoxStartMinimized.TabIndex = 6;
            this.checkBoxStartMinimized.UseVisualStyleBackColor = true;
            // 
            // labelStartServerOnStartup
            // 
            this.labelStartServerOnStartup.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelStartServerOnStartup.AutoSize = true;
            this.labelStartServerOnStartup.Location = new System.Drawing.Point(3, 6);
            this.labelStartServerOnStartup.Name = "labelStartServerOnStartup";
            this.labelStartServerOnStartup.Size = new System.Drawing.Size(115, 13);
            this.labelStartServerOnStartup.TabIndex = 0;
            this.labelStartServerOnStartup.Text = "Start Server on Startup";
            // 
            // labelStartMinimized
            // 
            this.labelStartMinimized.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelStartMinimized.AutoSize = true;
            this.labelStartMinimized.Location = new System.Drawing.Point(3, 31);
            this.labelStartMinimized.Name = "labelStartMinimized";
            this.labelStartMinimized.Size = new System.Drawing.Size(77, 13);
            this.labelStartMinimized.TabIndex = 1;
            this.labelStartMinimized.Text = "Start minimized";
            // 
            // labelSteeringSensitivity
            // 
            this.labelSteeringSensitivity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelSteeringSensitivity.AutoSize = true;
            this.labelSteeringSensitivity.Location = new System.Drawing.Point(3, 81);
            this.labelSteeringSensitivity.Name = "labelSteeringSensitivity";
            this.labelSteeringSensitivity.Size = new System.Drawing.Size(96, 13);
            this.labelSteeringSensitivity.TabIndex = 2;
            this.labelSteeringSensitivity.Text = "Steering Sensitivity";
            // 
            // checkBoxStartServerOnStartup
            // 
            this.checkBoxStartServerOnStartup.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxStartServerOnStartup.AutoSize = true;
            this.checkBoxStartServerOnStartup.Location = new System.Drawing.Point(133, 5);
            this.checkBoxStartServerOnStartup.Name = "checkBoxStartServerOnStartup";
            this.checkBoxStartServerOnStartup.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.checkBoxStartServerOnStartup.Size = new System.Drawing.Size(15, 14);
            this.checkBoxStartServerOnStartup.TabIndex = 5;
            this.checkBoxStartServerOnStartup.UseVisualStyleBackColor = true;
            // 
            // sensitivityTrackBar
            // 
            this.sensitivityTrackBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensitivityTrackBar.Location = new System.Drawing.Point(133, 78);
            this.sensitivityTrackBar.Maximum = 100;
            this.sensitivityTrackBar.Minimum = 1;
            this.sensitivityTrackBar.Name = "sensitivityTrackBar";
            this.sensitivityTrackBar.Size = new System.Drawing.Size(179, 19);
            this.sensitivityTrackBar.TabIndex = 7;
            this.sensitivityTrackBar.Value = 50;
            this.sensitivityTrackBar.Scroll += new System.EventHandler(this.sensitivityTrackBar_Scroll);
            // 
            // labelSensitivity
            // 
            this.labelSensitivity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSensitivity.AutoSize = true;
            this.labelSensitivity.Location = new System.Drawing.Point(133, 100);
            this.labelSensitivity.Name = "labelSensitivity";
            this.labelSensitivity.Size = new System.Drawing.Size(179, 13);
            this.labelSensitivity.TabIndex = 8;
            this.labelSensitivity.Text = "50";
            this.labelSensitivity.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // buttonSaveSettings
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.buttonSaveSettings, 2);
            this.buttonSaveSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonSaveSettings.Location = new System.Drawing.Point(3, 217);
            this.buttonSaveSettings.Name = "buttonSaveSettings";
            this.buttonSaveSettings.Size = new System.Drawing.Size(309, 44);
            this.buttonSaveSettings.TabIndex = 9;
            this.buttonSaveSettings.Text = "Save";
            this.buttonSaveSettings.UseVisualStyleBackColor = true;
            this.buttonSaveSettings.Click += new System.EventHandler(this.buttonSaveSettings_Click);
            // 
            // labelMinimizeToTray
            // 
            this.labelMinimizeToTray.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelMinimizeToTray.AutoSize = true;
            this.labelMinimizeToTray.Location = new System.Drawing.Point(3, 56);
            this.labelMinimizeToTray.Name = "labelMinimizeToTray";
            this.labelMinimizeToTray.Size = new System.Drawing.Size(83, 13);
            this.labelMinimizeToTray.TabIndex = 10;
            this.labelMinimizeToTray.Text = "Minimize to Tray";
            // 
            // checkBoxMinimizeToTray
            // 
            this.checkBoxMinimizeToTray.AutoSize = true;
            this.checkBoxMinimizeToTray.Location = new System.Drawing.Point(133, 53);
            this.checkBoxMinimizeToTray.Name = "checkBoxMinimizeToTray";
            this.checkBoxMinimizeToTray.Size = new System.Drawing.Size(15, 14);
            this.checkBoxMinimizeToTray.TabIndex = 11;
            this.checkBoxMinimizeToTray.UseVisualStyleBackColor = true;
            // 
            // ProgramSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(315, 264);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgramSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ProgramSettingsForm";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityTrackBar)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox checkBoxStartMinimized;
        private System.Windows.Forms.Label labelStartServerOnStartup;
        private System.Windows.Forms.Label labelStartMinimized;
        private System.Windows.Forms.Label labelSteeringSensitivity;
        private System.Windows.Forms.CheckBox checkBoxStartServerOnStartup;
        private System.Windows.Forms.TrackBar sensitivityTrackBar;
        private System.Windows.Forms.Label labelSensitivity;
        private System.Windows.Forms.Button buttonSaveSettings;
        private System.Windows.Forms.Label labelMinimizeToTray;
        private System.Windows.Forms.CheckBox checkBoxMinimizeToTray;
    }
}