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
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.rtspPortUpDown = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.udpPortUpDown = new System.Windows.Forms.NumericUpDown();
            this.allowVolumeCheckBox = new System.Windows.Forms.CheckBox();
            this.sendCommandCheckBox = new System.Windows.Forms.CheckBox();
            this.bufferSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.rtspPortUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bufferSizeUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(86, 24);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(194, 20);
            this.nameTextBox.TabIndex = 0;
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
            this.label2.Location = new System.Drawing.Point(12, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(86, 61);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(194, 20);
            this.passwordTextBox.TabIndex = 3;
            this.passwordTextBox.UseSystemPasswordChar = true;
            // 
            // rtspPortUpDown
            // 
            this.rtspPortUpDown.Location = new System.Drawing.Point(86, 98);
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
            this.rtspPortUpDown.TabIndex = 4;
            this.rtspPortUpDown.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 100);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "RTSP Port";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(157, 100);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "UDP Port";
            // 
            // udpPortUpDown
            // 
            this.udpPortUpDown.Location = new System.Drawing.Point(224, 98);
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
            this.udpPortUpDown.TabIndex = 7;
            this.udpPortUpDown.Value = new decimal(new int[] {
            6000,
            0,
            0,
            0});
            // 
            // allowVolumeCheckBox
            // 
            this.allowVolumeCheckBox.AutoSize = true;
            this.allowVolumeCheckBox.Location = new System.Drawing.Point(15, 188);
            this.allowVolumeCheckBox.Name = "allowVolumeCheckBox";
            this.allowVolumeCheckBox.Size = new System.Drawing.Size(163, 17);
            this.allowVolumeCheckBox.TabIndex = 8;
            this.allowVolumeCheckBox.Text = "Allow client to control volume";
            this.allowVolumeCheckBox.UseVisualStyleBackColor = true;
            // 
            // sendCommandCheckBox
            // 
            this.sendCommandCheckBox.AutoSize = true;
            this.sendCommandCheckBox.Location = new System.Drawing.Point(15, 211);
            this.sendCommandCheckBox.Name = "sendCommandCheckBox";
            this.sendCommandCheckBox.Size = new System.Drawing.Size(191, 17);
            this.sendCommandCheckBox.TabIndex = 9;
            this.sendCommandCheckBox.Text = "Send playback commands to client";
            this.sendCommandCheckBox.UseVisualStyleBackColor = true;
            // 
            // bufferSizeUpDown
            // 
            this.bufferSizeUpDown.DecimalPlaces = 1;
            this.bufferSizeUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.bufferSizeUpDown.Location = new System.Drawing.Point(86, 135);
            this.bufferSizeUpDown.Name = "bufferSizeUpDown";
            this.bufferSizeUpDown.Size = new System.Drawing.Size(62, 20);
            this.bufferSizeUpDown.TabIndex = 10;
            this.bufferSizeUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 137);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Initial buffer";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(154, 137);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "seconds";
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(160, 277);
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
            this.cancelButton.Location = new System.Drawing.Point(241, 277);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 14;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // Configuration
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(326, 312);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.bufferSizeUpDown);
            this.Controls.Add(this.sendCommandCheckBox);
            this.Controls.Add(this.allowVolumeCheckBox);
            this.Controls.Add(this.udpPortUpDown);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.rtspPortUpDown);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nameTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Configuration";
            this.Text = "AirPlay Settings";
            ((System.ComponentModel.ISupportInitialize)(this.rtspPortUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udpPortUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bufferSizeUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.NumericUpDown rtspPortUpDown;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown udpPortUpDown;
        private System.Windows.Forms.CheckBox allowVolumeCheckBox;
        private System.Windows.Forms.CheckBox sendCommandCheckBox;
        private System.Windows.Forms.NumericUpDown bufferSizeUpDown;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}