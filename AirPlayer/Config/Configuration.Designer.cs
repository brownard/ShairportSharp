namespace AirPlayer.Config
{
    partial class Configuration
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
            this.components = new System.ComponentModel.Container();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.allowVolumeCheckBox = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.audioBufferUpDown = new System.Windows.Forms.NumericUpDown();
            this.sendCommandCheckBox = new System.Windows.Forms.CheckBox();
            this.udpPortUpDown = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.rtspPortUpDown = new System.Windows.Forms.NumericUpDown();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.allowHDCheckBox = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.videoBufferUpDown = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.httpPortUpDown = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.useDummyAddressCheckBox = new System.Windows.Forms.CheckBox();
            this.ios8WorkaroundCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.audioBufferUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rtspPortUpDown)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.videoBufferUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.httpPortUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(86, 24);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(244, 20);
            this.nameTextBox.TabIndex = 0;
            this.toolTip.SetToolTip(this.nameTextBox, "The display name of the server.");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(86, 60);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(244, 20);
            this.passwordTextBox.TabIndex = 3;
            this.toolTip.SetToolTip(this.passwordTextBox, "The password required to connect.\r\nLeave empty to not require a password.");
            this.passwordTextBox.UseSystemPasswordChar = true;
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(294, 293);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 13;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(375, 293);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 14;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.allowVolumeCheckBox);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.audioBufferUpDown);
            this.groupBox1.Controls.Add(this.sendCommandCheckBox);
            this.groupBox1.Controls.Add(this.udpPortUpDown);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.rtspPortUpDown);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(15, 100);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(208, 179);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Audio";
            // 
            // allowVolumeCheckBox
            // 
            this.allowVolumeCheckBox.AutoSize = true;
            this.allowVolumeCheckBox.Location = new System.Drawing.Point(9, 148);
            this.allowVolumeCheckBox.Name = "allowVolumeCheckBox";
            this.allowVolumeCheckBox.Size = new System.Drawing.Size(163, 17);
            this.allowVolumeCheckBox.TabIndex = 17;
            this.allowVolumeCheckBox.Text = "Allow client to control volume";
            this.toolTip.SetToolTip(this.allowVolumeCheckBox, "Whether to allow the client to control the playback volume.");
            this.allowVolumeCheckBox.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(142, 91);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 13);
            this.label6.TabIndex = 21;
            this.label6.Text = "seconds";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 91);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 13);
            this.label5.TabIndex = 20;
            this.label5.Text = "Audio buffer";
            // 
            // audioBufferUpDown
            // 
            this.audioBufferUpDown.DecimalPlaces = 1;
            this.audioBufferUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.audioBufferUpDown.Location = new System.Drawing.Point(80, 89);
            this.audioBufferUpDown.Name = "audioBufferUpDown";
            this.audioBufferUpDown.Size = new System.Drawing.Size(56, 20);
            this.audioBufferUpDown.TabIndex = 19;
            this.toolTip.SetToolTip(this.audioBufferUpDown, "The amount of audio to buffer before starting playback.");
            this.audioBufferUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // sendCommandCheckBox
            // 
            this.sendCommandCheckBox.AutoSize = true;
            this.sendCommandCheckBox.Location = new System.Drawing.Point(9, 125);
            this.sendCommandCheckBox.Name = "sendCommandCheckBox";
            this.sendCommandCheckBox.Size = new System.Drawing.Size(191, 17);
            this.sendCommandCheckBox.TabIndex = 18;
            this.sendCommandCheckBox.Text = "Send playback commands to client";
            this.toolTip.SetToolTip(this.sendCommandCheckBox, "Whether to send Play/Pause/Prev/Next commands to the client.");
            this.sendCommandCheckBox.UseVisualStyleBackColor = true;
            // 
            // udpPortUpDown
            // 
            this.udpPortUpDown.Location = new System.Drawing.Point(80, 56);
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
            this.udpPortUpDown.TabIndex = 16;
            this.toolTip.SetToolTip(this.udpPortUpDown, "The port used to receive audio data.\r\nRequires 3 ports, this and the next 2.");
            this.udpPortUpDown.Value = new decimal(new int[] {
            50510,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 58);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "UDP Port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "RTSP Port";
            // 
            // rtspPortUpDown
            // 
            this.rtspPortUpDown.Location = new System.Drawing.Point(80, 23);
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
            this.rtspPortUpDown.TabIndex = 13;
            this.toolTip.SetToolTip(this.rtspPortUpDown, "The port used to receive RTSP messages (TCP).");
            this.rtspPortUpDown.Value = new decimal(new int[] {
            50500,
            0,
            0,
            0});
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.allowHDCheckBox);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.videoBufferUpDown);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.httpPortUpDown);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Location = new System.Drawing.Point(242, 100);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(208, 119);
            this.groupBox2.TabIndex = 16;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Video/Photo";
            // 
            // allowHDCheckBox
            // 
            this.allowHDCheckBox.AutoSize = true;
            this.allowHDCheckBox.Location = new System.Drawing.Point(13, 90);
            this.allowHDCheckBox.Name = "allowHDCheckBox";
            this.allowHDCheckBox.Size = new System.Drawing.Size(167, 17);
            this.allowHDCheckBox.TabIndex = 5;
            this.allowHDCheckBox.Text = "Select HD streams if available";
            this.toolTip.SetToolTip(this.allowHDCheckBox, "Whether to select HD quality if multiple streams are available.\r\nIf unchecked the" +
        " best non HD stream will be selected.");
            this.allowHDCheckBox.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(142, 58);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(43, 13);
            this.label9.TabIndex = 4;
            this.label9.Text = "percent";
            // 
            // videoBufferUpDown
            // 
            this.videoBufferUpDown.Location = new System.Drawing.Point(80, 56);
            this.videoBufferUpDown.Name = "videoBufferUpDown";
            this.videoBufferUpDown.Size = new System.Drawing.Size(56, 20);
            this.videoBufferUpDown.TabIndex = 3;
            this.toolTip.SetToolTip(this.videoBufferUpDown, "The amount of video to buffer before starting playback.");
            this.videoBufferUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(10, 58);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 13);
            this.label8.TabIndex = 2;
            this.label8.Text = "Video buffer";
            // 
            // httpPortUpDown
            // 
            this.httpPortUpDown.Location = new System.Drawing.Point(80, 23);
            this.httpPortUpDown.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.httpPortUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.httpPortUpDown.Name = "httpPortUpDown";
            this.httpPortUpDown.Size = new System.Drawing.Size(56, 20);
            this.httpPortUpDown.TabIndex = 1;
            this.toolTip.SetToolTip(this.httpPortUpDown, "The port used to receive HTTP messages (TCP).");
            this.httpPortUpDown.Value = new decimal(new int[] {
            60500,
            0,
            0,
            0});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 25);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "HTTP Port";
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 10000;
            this.toolTip.InitialDelay = 500;
            this.toolTip.ReshowDelay = 100;
            // 
            // useDummyAddressCheckBox
            // 
            this.useDummyAddressCheckBox.AutoSize = true;
            this.useDummyAddressCheckBox.Location = new System.Drawing.Point(242, 234);
            this.useDummyAddressCheckBox.Name = "useDummyAddressCheckBox";
            this.useDummyAddressCheckBox.Size = new System.Drawing.Size(212, 17);
            this.useDummyAddressCheckBox.TabIndex = 17;
            this.useDummyAddressCheckBox.Text = "Use a dummy MAC address as identifier";
            this.toolTip.SetToolTip(this.useDummyAddressCheckBox, "Enable this if you are running other airplay servers on this computer\r\n(ensure th" +
        "at you are also using different ports).");
            this.useDummyAddressCheckBox.UseVisualStyleBackColor = true;
            // 
            // ios8WorkaroundCheckBox
            // 
            this.ios8WorkaroundCheckBox.AutoSize = true;
            this.ios8WorkaroundCheckBox.Location = new System.Drawing.Point(242, 258);
            this.ios8WorkaroundCheckBox.Name = "ios8WorkaroundCheckBox";
            this.ios8WorkaroundCheckBox.Size = new System.Drawing.Size(147, 17);
            this.ios8WorkaroundCheckBox.TabIndex = 18;
            this.ios8WorkaroundCheckBox.Text = "Enable iOS 8 workaround";
            this.ios8WorkaroundCheckBox.UseVisualStyleBackColor = true;
            // 
            // Configuration
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(467, 328);
            this.Controls.Add(this.ios8WorkaroundCheckBox);
            this.Controls.Add(this.useDummyAddressCheckBox);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nameTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Configuration";
            this.Text = "AirPlay Settings";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.audioBufferUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rtspPortUpDown)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.videoBufferUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.httpPortUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown audioBufferUpDown;
        private System.Windows.Forms.CheckBox sendCommandCheckBox;
        private System.Windows.Forms.CheckBox allowVolumeCheckBox;
        private System.Windows.Forms.NumericUpDown udpPortUpDown;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown rtspPortUpDown;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox allowHDCheckBox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown videoBufferUpDown;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown httpPortUpDown;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox useDummyAddressCheckBox;
        private System.Windows.Forms.CheckBox ios8WorkaroundCheckBox;
    }
}