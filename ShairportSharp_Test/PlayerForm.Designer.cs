namespace ShairportSharp_Test
{
    partial class PlayerForm
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
            this.mediaPanel = new System.Windows.Forms.Panel();
            this.labelGenre = new System.Windows.Forms.Label();
            this.labelProgress = new System.Windows.Forms.Label();
            this.labelArtist = new System.Windows.Forms.Label();
            this.labelAlbum = new System.Windows.Forms.Label();
            this.labelTrack = new System.Windows.Forms.Label();
            this.buttonPrev = new System.Windows.Forms.Button();
            this.buttonNext = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonPlayPause = new System.Windows.Forms.Button();
            this.panelArtwork = new System.Windows.Forms.Panel();
            this.bufferFill = new System.Windows.Forms.ProgressBar();
            this.mediaPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mediaPanel
            // 
            this.mediaPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mediaPanel.Controls.Add(this.labelGenre);
            this.mediaPanel.Controls.Add(this.labelProgress);
            this.mediaPanel.Controls.Add(this.labelArtist);
            this.mediaPanel.Controls.Add(this.labelAlbum);
            this.mediaPanel.Controls.Add(this.labelTrack);
            this.mediaPanel.Controls.Add(this.buttonPrev);
            this.mediaPanel.Controls.Add(this.buttonNext);
            this.mediaPanel.Controls.Add(this.buttonStop);
            this.mediaPanel.Controls.Add(this.buttonPlayPause);
            this.mediaPanel.Controls.Add(this.panelArtwork);
            this.mediaPanel.Location = new System.Drawing.Point(6, 12);
            this.mediaPanel.Name = "mediaPanel";
            this.mediaPanel.Size = new System.Drawing.Size(318, 193);
            this.mediaPanel.TabIndex = 1;
            // 
            // labelGenre
            // 
            this.labelGenre.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelGenre.AutoSize = true;
            this.labelGenre.Location = new System.Drawing.Point(178, 93);
            this.labelGenre.Name = "labelGenre";
            this.labelGenre.Size = new System.Drawing.Size(0, 13);
            this.labelGenre.TabIndex = 10;
            // 
            // labelProgress
            // 
            this.labelProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProgress.AutoSize = true;
            this.labelProgress.Location = new System.Drawing.Point(178, 118);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(0, 13);
            this.labelProgress.TabIndex = 9;
            // 
            // labelArtist
            // 
            this.labelArtist.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelArtist.AutoSize = true;
            this.labelArtist.Location = new System.Drawing.Point(178, 68);
            this.labelArtist.Name = "labelArtist";
            this.labelArtist.Size = new System.Drawing.Size(0, 13);
            this.labelArtist.TabIndex = 8;
            // 
            // labelAlbum
            // 
            this.labelAlbum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAlbum.AutoSize = true;
            this.labelAlbum.Location = new System.Drawing.Point(178, 43);
            this.labelAlbum.Name = "labelAlbum";
            this.labelAlbum.Size = new System.Drawing.Size(0, 13);
            this.labelAlbum.TabIndex = 7;
            // 
            // labelTrack
            // 
            this.labelTrack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTrack.AutoSize = true;
            this.labelTrack.Location = new System.Drawing.Point(178, 18);
            this.labelTrack.Name = "labelTrack";
            this.labelTrack.Size = new System.Drawing.Size(0, 13);
            this.labelTrack.TabIndex = 6;
            // 
            // buttonPrev
            // 
            this.buttonPrev.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonPrev.BackgroundImage = global::ShairportSharp_Test.Properties.Resources.Prev;
            this.buttonPrev.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.buttonPrev.Location = new System.Drawing.Point(45, 147);
            this.buttonPrev.Name = "buttonPrev";
            this.buttonPrev.Size = new System.Drawing.Size(58, 32);
            this.buttonPrev.TabIndex = 5;
            this.buttonPrev.UseVisualStyleBackColor = true;
            // 
            // buttonNext
            // 
            this.buttonNext.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonNext.BackgroundImage = global::ShairportSharp_Test.Properties.Resources.Next;
            this.buttonNext.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.buttonNext.Location = new System.Drawing.Point(225, 147);
            this.buttonNext.Name = "buttonNext";
            this.buttonNext.Size = new System.Drawing.Size(58, 32);
            this.buttonNext.TabIndex = 4;
            this.buttonNext.UseVisualStyleBackColor = true;
            // 
            // buttonStop
            // 
            this.buttonStop.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonStop.BackgroundImage = global::ShairportSharp_Test.Properties.Resources.Stop;
            this.buttonStop.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.buttonStop.Location = new System.Drawing.Point(165, 147);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(58, 32);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.UseVisualStyleBackColor = true;
            // 
            // buttonPlayPause
            // 
            this.buttonPlayPause.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonPlayPause.BackgroundImage = global::ShairportSharp_Test.Properties.Resources.Play;
            this.buttonPlayPause.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.buttonPlayPause.Location = new System.Drawing.Point(105, 147);
            this.buttonPlayPause.Name = "buttonPlayPause";
            this.buttonPlayPause.Size = new System.Drawing.Size(58, 32);
            this.buttonPlayPause.TabIndex = 2;
            this.buttonPlayPause.UseVisualStyleBackColor = true;
            // 
            // panelArtwork
            // 
            this.panelArtwork.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelArtwork.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panelArtwork.Location = new System.Drawing.Point(14, 18);
            this.panelArtwork.Name = "panelArtwork";
            this.panelArtwork.Size = new System.Drawing.Size(149, 113);
            this.panelArtwork.TabIndex = 0;
            // 
            // bufferFill
            // 
            this.bufferFill.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bufferFill.Location = new System.Drawing.Point(6, 224);
            this.bufferFill.Name = "bufferFill";
            this.bufferFill.Size = new System.Drawing.Size(318, 23);
            this.bufferFill.TabIndex = 3;
            // 
            // PlayerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 271);
            this.Controls.Add(this.bufferFill);
            this.Controls.Add(this.mediaPanel);
            this.Name = "PlayerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Player";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PlayerForm_FormClosing);
            this.Load += new System.EventHandler(this.PlayerForm_Load);
            this.mediaPanel.ResumeLayout(false);
            this.mediaPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel mediaPanel;
        private System.Windows.Forms.Button buttonPrev;
        private System.Windows.Forms.Button buttonNext;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonPlayPause;
        private System.Windows.Forms.Panel panelArtwork;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.Label labelArtist;
        private System.Windows.Forms.Label labelAlbum;
        private System.Windows.Forms.Label labelTrack;
        private System.Windows.Forms.ProgressBar bufferFill;
        private System.Windows.Forms.Label labelGenre;
    }
}