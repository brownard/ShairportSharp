using AlacDecoder;
using ShairportSharp;
using ShairportSharp.Audio;
using ShairportSharp.Raop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShairportSharp_Test
{
    public partial class ShairportForm : Form
    {
        #region Variables

        ShairportServer server;
        PlayerForm playerForm = null;
        bool closed = false;

        #endregion

        #region Constructor

        public ShairportForm()
        {
            InitializeComponent();
            Log log = new Log();
            log.NewLog += logToTextBox;
            ShairportServer.SetLogger(log);
            nameTextBox.Text = SystemInformation.ComputerName;
        }

        #endregion

        #region Form Events

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closed = true;
            closePlayerForm();
            if (server != null)
                server.Stop();
        }

        void playerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            playerForm.Dispose();
            playerForm = null;

            if (server != null)
                server.StopCurrentSession();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (server == null)
            {
                server = new ShairportServer(nameTextBox.Text, passwordTextBox.Text)
                {
                    Port = (int)rtspPortUpDown.Value,
                    AudioPort = (int)udpPortUpDown.Value,
                    AudioBufferSize = (int)(bufferSizeUpDown.Value * 1000)
                };

                server.StreamStopped += server_StreamStopped;
                server.StreamStarting += server_StreamStarting;
                server.StreamReady += server_StreamReady;
                server.PlaybackProgressChanged += server_PlaybackProgressChanged;
                server.MetaDataChanged += server_MetaDataChanged;
                server.ArtworkChanged += server_ArtworkChanged;
                if (allowVolumeCheckBox.Checked)
                    server.VolumeChanged += server_VolumeChange;
                server.AudioBufferChanged += server_AudioBufferChanged;
                server.Start();
                panelSettings.Enabled = false;
                buttonStart.Text = "Stop";
            }
            else
            {
                closePlayerForm();
                server.Stop();
                server = null;
                panelSettings.Enabled = true;
                buttonStart.Text = "Start";
            }
        }

        #endregion

        #region Server Events

        void server_StreamStarting(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_StreamStarting(sender, e); });
                return;
            }

            closePlayerForm();
            playerForm = new PlayerForm(server, sendCommandCheckBox.Checked);
            playerForm.Location = new Point(this.Location.X + this.Width + 10, this.Location.Y);
            playerForm.FormClosed += playerForm_FormClosed;
        }

        void server_StreamStopped(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_StreamStopped(sender, e); });
                return;
            }
            closePlayerForm();
        }

        void server_StreamReady(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_StreamReady(sender, e); });
                return;
            }

            if (server != null && playerForm != null)
                playerForm.Show();
        }

        void server_PlaybackProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_PlaybackProgressChanged(sender, e); });
                return;
            }

            if (playerForm != null)
                playerForm.SetPlaybackProgress(e);
        }

        void server_MetaDataChanged(object sender, MetaDataChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_MetaDataChanged(sender, e); });
                return;
            }

            if (playerForm != null)
                playerForm.SetMetaData(e);
        }

        void server_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_ArtworkChanged(sender, e); });
                return;
            }

            if (!closed && playerForm != null)
                playerForm.SetArtwork(e);
        }

        void server_VolumeChange(object sender, VolumeChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_VolumeChange(sender, e); });
                return;
            }

            if (playerForm != null)
                playerForm.SetVolume(e);
        }
        
        void server_AudioBufferChanged(object sender, BufferChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_AudioBufferChanged(sender, e); });
                return;
            }

            if (playerForm != null)
                playerForm.SetBufferProgress(e);
        }

        #endregion

        #region Utils

        void closePlayerForm()
        {
            if (playerForm != null)
            {
                playerForm.Close();
            }
        }

        void logToTextBox(object sender, LogEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)(() => logToTextBox(sender, e)));
                return;
            }
            if (!closed)
                logBox.AppendText(string.Format("[{0} - {1}] {2}\r\n", e.Time, e.Level, e.Message));
        }

        #endregion
    }
}
