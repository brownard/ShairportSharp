namespace ShairportSharp_Test
{
    partial class VideoForm
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
            this.videoControl = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.videoControl)).BeginInit();
            this.SuspendLayout();
            // 
            // videoControl
            // 
            this.videoControl.BackColor = System.Drawing.Color.Black;
            this.videoControl.Location = new System.Drawing.Point(12, 12);
            this.videoControl.Name = "videoControl";
            this.videoControl.Size = new System.Drawing.Size(378, 266);
            this.videoControl.TabIndex = 0;
            this.videoControl.TabStop = false;
            // 
            // VideoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(402, 317);
            this.Controls.Add(this.videoControl);
            this.Name = "VideoForm";
            this.Text = "VideoForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VideoForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.videoControl)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox videoControl;
    }
}