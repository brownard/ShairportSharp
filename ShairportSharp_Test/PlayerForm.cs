using ShairportSharp;
using ShairportSharp.Audio;
using ShairportSharp.Raop;
using ShairportSharp.Remote;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShairportSharp_Test
{    
    public partial class PlayerForm : Form
    {
        ShairportServer server;
        IPlayer player;
        Timer progressTimer;
        PlaybackProgressChangedEventArgs lastProgressEventArgs;

        public PlayerForm(ShairportServer server, bool sendPlaybackCommands)
        {
            InitializeComponent();
            this.server = server;

            if (server != null && sendPlaybackCommands)
            {
                buttonPrev.Click += buttonPrev_Click;
                buttonPlayPause.Click += buttonPlayPause_Click;
                buttonStop.Click += buttonStop_Click;
                buttonNext.Click += buttonNext_Click;
            }
        }

        void buttonPrev_Click(object sender, EventArgs e)
        {
            server.SendCommand(RemoteCommand.PrevItem);
        }

        void buttonPlayPause_Click(object sender, EventArgs e)
        {
            server.SendCommand(RemoteCommand.PlayPause);
        }

        void buttonStop_Click(object sender, EventArgs e)
        {
            server.SendCommand(RemoteCommand.Stop);
            Close();
        }

        void buttonNext_Click(object sender, EventArgs e)
        {
            server.SendCommand(RemoteCommand.NextItem);
        }

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            if (server != null)
            {
                WaveStream stream = (WaveStream)server.GetStream(StreamType.Wave);
                if (stream != null)
                {
                    player = new WaveStreamPlayer(stream);
                    player.Start();
                    if (lastProgressEventArgs != null)
                        player.UpdateDurationInfo(lastProgressEventArgs.Start, lastProgressEventArgs.Stop);

                    progressTimer = new Timer();
                    progressTimer.Interval = 500;
                    progressTimer.Tick += progressTimer_Tick;
                    progressTimer.Start();
                }
            }
        }

        void progressTimer_Tick(object sender, EventArgs e)
        {
            if (player != null)
            {
                TimeSpan current = TimeSpan.FromSeconds(player.CurrentPosition);
                TimeSpan duration = TimeSpan.FromSeconds(player.Duration);
                labelProgress.Text = string.Format("{0}.{1} / {2}.{3}", current.Minutes, current.Seconds.ToString("00"), duration.Minutes, duration.Seconds.ToString("00"));
            }
        }

        private void PlayerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (progressTimer != null)
            {
                progressTimer.Dispose();
                progressTimer = null;
            }

            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
        }

        public void SetArtwork(ArtwokChangedEventArgs e)
        {
            if (panelArtwork.BackgroundImage != null)
                panelArtwork.BackgroundImage.Dispose();
            Image image;
            try { image = Image.FromStream(new MemoryStream(e.ImageData)); }
            catch { image = null; }
            panelArtwork.BackgroundImage = image;
        }

        public void SetMetaData(MetaDataChangedEventArgs e)
        {
            labelTrack.Text = e.MetaData.Track;
            labelAlbum.Text = e.MetaData.Album;
            labelArtist.Text = e.MetaData.Artist;
            labelGenre.Text = e.MetaData.Genre;
        }

        public void SetPlaybackProgress(PlaybackProgressChangedEventArgs e)
        {
            if (player != null)
                player.UpdateDurationInfo(e.Start, e.Stop);
            else
                lastProgressEventArgs = e;
        }

        public void SetVolume(VolumeChangedEventArgs e)
        {
            if (player != null)
                player.SetVolume(e.Volume);
        }

        public void SetBufferProgress(BufferChangedEventArgs e)
        {
            int percent = (e.CurrentSize * 100) / e.MaxSize;
            if (percent < 0)
                percent = 0;
            else if (percent > 100)
                percent = 100;
            bufferFill.Value = percent;
        }

    }
}
