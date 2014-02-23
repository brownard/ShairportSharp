namespace ShairportSharp_Test
{
    partial class ShairportForm
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
            this.logBox = new System.Windows.Forms.TextBox();
            this.panelSettings = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.bufferSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.sendCommandCheckBox = new System.Windows.Forms.CheckBox();
            this.allowVolumeCheckBox = new System.Windows.Forms.CheckBox();
            this.udpPortUpDown = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.rtspPortUpDown = new System.Windows.Forms.NumericUpDown();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.panelSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bufferSizeUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rtspPortUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.Location = new System.Drawing.Point(12, 257);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logBox.Size = new System.Drawing.Size(358, 138);
            this.logBox.TabIndex = 1;
            // 
            // panelSettings
            // 
            this.panelSettings.Controls.Add(this.label6);
            this.panelSettings.Controls.Add(this.label5);
            this.panelSettings.Controls.Add(this.bufferSizeUpDown);
            this.panelSettings.Controls.Add(this.sendCommandCheckBox);
            this.panelSettings.Controls.Add(this.allowVolumeCheckBox);
            this.panelSettings.Controls.Add(this.udpPortUpDown);
            this.panelSettings.Controls.Add(this.label4);
            this.panelSettings.Controls.Add(this.label3);
            this.panelSettings.Controls.Add(this.rtspPortUpDown);
            this.panelSettings.Controls.Add(this.passwordTextBox);
            this.panelSettings.Controls.Add(this.label2);
            this.panelSettings.Controls.Add(this.label1);
            this.panelSettings.Controls.Add(this.nameTextBox);
            this.panelSettings.Location = new System.Drawing.Point(2, 12);
            this.panelSettings.Name = "panelSettings";
            this.panelSettings.Size = new System.Drawing.Size(284, 225);
            this.panelSettings.TabIndex = 2;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(149, 121);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 13);
            this.label6.TabIndex = 51;
            this.label6.Text = "seconds";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 121);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 13);
            this.label5.TabIndex = 50;
            this.label5.Text = "Initial buffer";
            // 
            // bufferSizeUpDown
            // 
            this.bufferSizeUpDown.DecimalPlaces = 1;
            this.bufferSizeUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.bufferSizeUpDown.Location = new System.Drawing.Point(81, 119);
            this.bufferSizeUpDown.Name = "bufferSizeUpDown";
            this.bufferSizeUpDown.Size = new System.Drawing.Size(62, 20);
            this.bufferSizeUpDown.TabIndex = 49;
            this.bufferSizeUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // sendCommandCheckBox
            // 
            this.sendCommandCheckBox.AutoSize = true;
            this.sendCommandCheckBox.Location = new System.Drawing.Point(10, 195);
            this.sendCommandCheckBox.Name = "sendCommandCheckBox";
            this.sendCommandCheckBox.Size = new System.Drawing.Size(191, 17);
            this.sendCommandCheckBox.TabIndex = 48;
            this.sendCommandCheckBox.Text = "Send playback commands to client";
            this.sendCommandCheckBox.UseVisualStyleBackColor = true;
            // 
            // allowVolumeCheckBox
            // 
            this.allowVolumeCheckBox.AutoSize = true;
            this.allowVolumeCheckBox.Location = new System.Drawing.Point(10, 172);
            this.allowVolumeCheckBox.Name = "allowVolumeCheckBox";
            this.allowVolumeCheckBox.Size = new System.Drawing.Size(163, 17);
            this.allowVolumeCheckBox.TabIndex = 47;
            this.allowVolumeCheckBox.Text = "Allow client to control volume";
            this.allowVolumeCheckBox.UseVisualStyleBackColor = true;
            // 
            // udpPortUpDown
            // 
            this.udpPortUpDown.Location = new System.Drawing.Point(219, 82);
            this.udpPortUpDown.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.udpPortUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udpPortUpDown.Name = "udpPortUpDown";
            this.udpPortUpDown.Size = new System.Drawing.Size(56, 20);
            this.udpPortUpDown.TabIndex = 46;
            this.udpPortUpDown.Value = new decimal(new int[] {
            6000,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(152, 84);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 13);
            this.label4.TabIndex = 45;
            this.label4.Text = "UDP Port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 44;
            this.label3.Text = "RTSP Port";
            // 
            // rtspPortUpDown
            // 
            this.rtspPortUpDown.Location = new System.Drawing.Point(81, 82);
            this.rtspPortUpDown.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.rtspPortUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.rtspPortUpDown.Name = "rtspPortUpDown";
            this.rtspPortUpDown.Size = new System.Drawing.Size(56, 20);
            this.rtspPortUpDown.TabIndex = 43;
            this.rtspPortUpDown.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(81, 45);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(194, 20);
            this.passwordTextBox.TabIndex = 42;
            this.passwordTextBox.UseSystemPasswordChar = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 41;
            this.label2.Text = "Password";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 40;
            this.label1.Text = "Name";
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(81, 8);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(194, 20);
            this.nameTextBox.TabIndex = 39;
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(295, 18);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 3;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // ShairportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 407);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.panelSettings);
            this.Controls.Add(this.logBox);
            this.Name = "ShairportForm";
            this.Text = "ShairportSharp";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.panelSettings.ResumeLayout(false);
            this.panelSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bufferSizeUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rtspPortUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.Panel panelSettings;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown bufferSizeUpDown;
        private System.Windows.Forms.CheckBox sendCommandCheckBox;
        private System.Windows.Forms.CheckBox allowVolumeCheckBox;
        private System.Windows.Forms.NumericUpDown udpPortUpDown;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown rtspPortUpDown;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Button buttonStart;
    }
}

