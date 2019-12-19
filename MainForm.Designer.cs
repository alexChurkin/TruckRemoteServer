namespace TruckRemoteServer
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpPort = new System.Windows.Forms.NumericUpDown();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.sensitivityTrackBar = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.labelIp = new System.Windows.Forms.Label();
            this.labelSensitivity = new System.Windows.Forms.Label();
            this.menuStripMainMenu = new System.Windows.Forms.MenuStrip();
            this.programToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.controlMappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityTrackBar)).BeginInit();
            this.menuStripMainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(12, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Port:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(11, 94);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Server status:";
            // 
            // numericUpPort
            // 
            this.numericUpPort.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.numericUpPort.Location = new System.Drawing.Point(53, 165);
            this.numericUpPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numericUpPort.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpPort.Name = "numericUpPort";
            this.numericUpPort.Size = new System.Drawing.Size(83, 22);
            this.numericUpPort.TabIndex = 3;
            this.numericUpPort.Value = new decimal(new int[] {
            18250,
            0,
            0,
            0});
            this.numericUpPort.ValueChanged += new System.EventHandler(this.NumericUpPort_ValueChanged);
            // 
            // buttonStop
            // 
            this.buttonStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonStop.Location = new System.Drawing.Point(178, 222);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(125, 33);
            this.buttonStop.TabIndex = 5;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.ButtonStop_Click);
            // 
            // buttonStart
            // 
            this.buttonStart.Enabled = false;
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonStart.Location = new System.Drawing.Point(15, 222);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(125, 33);
            this.buttonStart.TabIndex = 6;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.ButtonStart_Click);
            // 
            // sensitivityTrackBar
            // 
            this.sensitivityTrackBar.Location = new System.Drawing.Point(159, 116);
            this.sensitivityTrackBar.Maximum = 100;
            this.sensitivityTrackBar.Minimum = 1;
            this.sensitivityTrackBar.Name = "sensitivityTrackBar";
            this.sensitivityTrackBar.Size = new System.Drawing.Size(141, 45);
            this.sensitivityTrackBar.TabIndex = 7;
            this.sensitivityTrackBar.TickFrequency = 5;
            this.sensitivityTrackBar.Value = 50;
            this.sensitivityTrackBar.Scroll += new System.EventHandler(this.SensitivityTrackBar_Scroll);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(165, 36);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "Steering:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(166, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 16);
            this.label4.TabIndex = 9;
            this.label4.Text = "Sensitivity:";
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.labelStatus.Location = new System.Drawing.Point(12, 121);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(59, 16);
            this.labelStatus.TabIndex = 10;
            this.labelStatus.Text = "Enabled";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label7.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label7.Location = new System.Drawing.Point(12, 36);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(78, 20);
            this.label7.TabIndex = 11;
            this.label7.Text = "Server IP:";
            // 
            // labelIp
            // 
            this.labelIp.AutoSize = true;
            this.labelIp.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelIp.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelIp.Location = new System.Drawing.Point(12, 62);
            this.labelIp.Name = "labelIp";
            this.labelIp.Size = new System.Drawing.Size(80, 16);
            this.labelIp.TabIndex = 12;
            this.labelIp.Text = "192.168.0.16";
            // 
            // labelSensitivity
            // 
            this.labelSensitivity.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelSensitivity.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelSensitivity.Location = new System.Drawing.Point(208, 145);
            this.labelSensitivity.Name = "labelSensitivity";
            this.labelSensitivity.Size = new System.Drawing.Size(40, 16);
            this.labelSensitivity.TabIndex = 13;
            this.labelSensitivity.Text = "50";
            this.labelSensitivity.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // menuStripMainMenu
            // 
            this.menuStripMainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.programToolStripMenuItem});
            this.menuStripMainMenu.Location = new System.Drawing.Point(0, 0);
            this.menuStripMainMenu.Name = "menuStripMainMenu";
            this.menuStripMainMenu.Size = new System.Drawing.Size(315, 24);
            this.menuStripMainMenu.TabIndex = 14;
            this.menuStripMainMenu.Text = "menuStrip1";
            // 
            // programToolStripMenuItem
            // 
            this.programToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem,
            this.controlMappingToolStripMenuItem});
            this.programToolStripMenuItem.Name = "programToolStripMenuItem";
            this.programToolStripMenuItem.Size = new System.Drawing.Size(65, 20);
            this.programToolStripMenuItem.Text = "Program";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // controlMappingToolStripMenuItem
            // 
            this.controlMappingToolStripMenuItem.Name = "controlMappingToolStripMenuItem";
            this.controlMappingToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.controlMappingToolStripMenuItem.Text = "Control mapping";
            this.controlMappingToolStripMenuItem.Click += new System.EventHandler(this.controlMappingToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(315, 264);
            this.Controls.Add(this.labelSensitivity);
            this.Controls.Add(this.labelIp);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.sensitivityTrackBar);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.numericUpPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.menuStripMainMenu);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStripMainMenu;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Truck Remote Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityTrackBar)).EndInit();
            this.menuStripMainMenu.ResumeLayout(false);
            this.menuStripMainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpPort;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.TrackBar sensitivityTrackBar;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelIp;
        private System.Windows.Forms.Label labelSensitivity;
        private System.Windows.Forms.MenuStrip menuStripMainMenu;
        private System.Windows.Forms.ToolStripMenuItem programToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem controlMappingToolStripMenuItem;
    }
}

