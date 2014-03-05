namespace ShairportSharp_Test
{
    partial class PhotoForm
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
            this.panelPhoto = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panelPhoto
            // 
            this.panelPhoto.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panelPhoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPhoto.Location = new System.Drawing.Point(0, 0);
            this.panelPhoto.Name = "panelPhoto";
            this.panelPhoto.Size = new System.Drawing.Size(284, 262);
            this.panelPhoto.TabIndex = 0;
            // 
            // PhotoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.panelPhoto);
            this.Name = "PhotoForm";
            this.Text = "Photo";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelPhoto;
    }
}