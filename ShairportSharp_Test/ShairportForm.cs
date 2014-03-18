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
using ShairportSharp.Airplay;

namespace ShairportSharp_Test
{
    public partial class ShairportForm : Form
    {
        #region Variables

        AirplayServer airplay;
        ShairportServer server;
        PlayerForm playerForm = null;
        PhotoForm photoForm = null;
        VideoForm videoForm = null;
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
            closeForms();
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

        void photoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (photoForm != null)
            {
                photoForm.Dispose();
                photoForm = null;
            }
        }

        void videoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (videoForm != null)
            {
                videoForm.Dispose();
                videoForm = null;
            }
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

                airplay = new AirplayServer(nameTextBox.Text, passwordTextBox.Text);
                airplay.ShowPhoto += airplay_ShowPhoto;
                airplay.VideoReceived += airplay_VideoReceived;
                airplay.PlaybackInfoRequested += airplay_PlaybackInfoRequested;
                airplay.GetPlaybackPosition += airplay_GetPlaybackPosition;
                airplay.PlaybackPositionChanged += airplay_PlaybackPositionChanged;
                airplay.PlaybackRateChanged += airplay_PlaybackRateChanged;
                airplay.SessionClosed += airplay_SessionClosed;
                airplay.Start();

                panelSettings.Enabled = false;
                buttonStart.Text = "Stop";
            }
            else
            {
                closeForms();

                airplay.Stop();
                airplay = null;

                server.Stop();
                server = null;
                panelSettings.Enabled = true;
                buttonStart.Text = "Start";
            }
        }

        #endregion

        #region AirPlay Events
                
        void airplay_ShowPhoto(object sender, PhotoEventArgs e)
        {
            Image image;
            try
            {
                using (MemoryStream ms = new MemoryStream(e.Photo))
                    image = Image.FromStream(ms);
            }
            catch
            {
                image = null;
            }
            BeginInvoke((MethodInvoker)delegate() { showPhoto(image); });
        }

        void airplay_GetPlaybackPosition(object sender, GetPlaybackPositionEventArgs e)
        {
            Invoke((MethodInvoker)delegate() 
            {
                if (videoForm != null)
                    videoForm.GetProgress(e);
            });
        }

        void airplay_PlaybackPositionChanged(object sender, PlaybackPositionEventArgs e)
        {
            Invoke((MethodInvoker)delegate()
            {
                if (videoForm != null)
                    videoForm.SetPosition(e);
            });
        }

        void airplay_PlaybackInfoRequested(object sender, PlaybackInfoEventArgs e)
        {
            Invoke((MethodInvoker)delegate()
            {
                if (videoForm != null)
                    videoForm.GetPlaybackInfo(e.PlaybackInfo);
            });
        }

        void airplay_PlaybackRateChanged(object sender, PlaybackRateEventArgs e)
        {
            Invoke((MethodInvoker)delegate()
            {
                if (videoForm != null)
                    videoForm.SetPlaybackRate(e);
            });
        }

        void airplay_VideoReceived(object sender, VideoEventArgs e)
        {
            Invoke((MethodInvoker)delegate()
            {
                showVideo(e);
            });
        }

        void airplay_SessionClosed(object sender, AirplayEventArgs e)
        {
            Invoke((MethodInvoker)delegate()
            {
                closeAirplayForms();
            });
        }

        void showPhoto(Image photo)
        {
            if (photoForm == null)
            {
                photoForm = new PhotoForm();
                photoForm.FormClosed += photoForm_FormClosed;
                photoForm.Show();
            }
            photoForm.SetPhoto(photo);
        }

        void showVideo(VideoEventArgs e)
        {
            if (videoForm == null)
            {
                videoForm = new VideoForm(airplay, e.SessionId);
                videoForm.FormClosed += videoForm_FormClosed;
                videoForm.Show();
            }
            videoForm.LoadVideo(e);
        }

        #endregion

        #region AirTunes Events

        void server_StreamStarting(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate() { server_StreamStarting(sender, e); });
                return;
            }

            closeForms();
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
            closeAirtunesForms();
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

        void closeForms()
        {
            closeAirtunesForms();
            closeAirplayForms();
        }

        void closeAirtunesForms()
        {
            if (playerForm != null)
                playerForm.Close();
        }

        void closeAirplayForms()
        {
            if (photoForm != null)
                photoForm.Close();
            if (videoForm != null)
                videoForm.Close();
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
