namespace TruckRemoteServer
{
    partial class ControlMappingForm
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
            this.tabControlControlSections = new System.Windows.Forms.TabControl();
            this.tabPageTruck = new System.Windows.Forms.TabPage();
            this.tabPageHud = new System.Windows.Forms.TabPage();
            this.tabPageCamera = new System.Windows.Forms.TabPage();
            this.tabPageOther = new System.Windows.Forms.TabPage();
            this.tableLayoutPanelControlMapping = new System.Windows.Forms.TableLayoutPanel();
            this.buttonControlMappingSave = new System.Windows.Forms.Button();
            this.tabControlControlSections.SuspendLayout();
            this.tableLayoutPanelControlMapping.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlControlSections
            // 
            this.tabControlControlSections.Controls.Add(this.tabPageTruck);
            this.tabControlControlSections.Controls.Add(this.tabPageHud);
            this.tabControlControlSections.Controls.Add(this.tabPageCamera);
            this.tabControlControlSections.Controls.Add(this.tabPageOther);
            this.tabControlControlSections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlControlSections.Location = new System.Drawing.Point(3, 3);
            this.tabControlControlSections.Name = "tabControlControlSections";
            this.tabControlControlSections.SelectedIndex = 0;
            this.tabControlControlSections.Size = new System.Drawing.Size(378, 359);
            this.tabControlControlSections.TabIndex = 0;
            // 
            // tabPageTruck
            // 
            this.tabPageTruck.Location = new System.Drawing.Point(4, 22);
            this.tabPageTruck.Name = "tabPageTruck";
            this.tabPageTruck.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTruck.Size = new System.Drawing.Size(370, 333);
            this.tabPageTruck.TabIndex = 0;
            this.tabPageTruck.Text = "Truck Controls";
            this.tabPageTruck.UseVisualStyleBackColor = true;
            // 
            // tabPageHud
            // 
            this.tabPageHud.Location = new System.Drawing.Point(4, 22);
            this.tabPageHud.Name = "tabPageHud";
            this.tabPageHud.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHud.Size = new System.Drawing.Size(370, 333);
            this.tabPageHud.TabIndex = 1;
            this.tabPageHud.Text = "HUD Controls";
            this.tabPageHud.UseVisualStyleBackColor = true;
            // 
            // tabPageCamera
            // 
            this.tabPageCamera.Location = new System.Drawing.Point(4, 22);
            this.tabPageCamera.Name = "tabPageCamera";
            this.tabPageCamera.Size = new System.Drawing.Size(370, 333);
            this.tabPageCamera.TabIndex = 2;
            this.tabPageCamera.Text = "Camera Controls";
            this.tabPageCamera.UseVisualStyleBackColor = true;
            // 
            // tabPageOther
            // 
            this.tabPageOther.Location = new System.Drawing.Point(4, 22);
            this.tabPageOther.Name = "tabPageOther";
            this.tabPageOther.Size = new System.Drawing.Size(370, 333);
            this.tabPageOther.TabIndex = 3;
            this.tabPageOther.Text = "Other";
            this.tabPageOther.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelControlMapping
            // 
            this.tableLayoutPanelControlMapping.ColumnCount = 1;
            this.tableLayoutPanelControlMapping.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelControlMapping.Controls.Add(this.tabControlControlSections, 0, 0);
            this.tableLayoutPanelControlMapping.Controls.Add(this.buttonControlMappingSave, 0, 1);
            this.tableLayoutPanelControlMapping.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelControlMapping.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelControlMapping.Name = "tableLayoutPanelControlMapping";
            this.tableLayoutPanelControlMapping.RowCount = 2;
            this.tableLayoutPanelControlMapping.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelControlMapping.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanelControlMapping.Size = new System.Drawing.Size(384, 405);
            this.tableLayoutPanelControlMapping.TabIndex = 1;
            // 
            // buttonControlMappingSave
            // 
            this.buttonControlMappingSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonControlMappingSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonControlMappingSave.Location = new System.Drawing.Point(3, 368);
            this.buttonControlMappingSave.Name = "buttonControlMappingSave";
            this.buttonControlMappingSave.Size = new System.Drawing.Size(378, 34);
            this.buttonControlMappingSave.TabIndex = 1;
            this.buttonControlMappingSave.Text = "Save";
            this.buttonControlMappingSave.UseVisualStyleBackColor = true;
            this.buttonControlMappingSave.Click += new System.EventHandler(this.buttonControlMappingSave_Click);
            // 
            // ControlMappingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 405);
            this.Controls.Add(this.tableLayoutPanelControlMapping);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "ControlMappingForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ControlMapping";
            this.tabControlControlSections.ResumeLayout(false);
            this.tableLayoutPanelControlMapping.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlControlSections;
        private System.Windows.Forms.TabPage tabPageTruck;
        private System.Windows.Forms.TabPage tabPageHud;
        private System.Windows.Forms.TabPage tabPageCamera;
        private System.Windows.Forms.TabPage tabPageOther;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelControlMapping;
        private System.Windows.Forms.Button buttonControlMappingSave;
    }
}