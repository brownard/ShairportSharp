using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShairportSharp.Airplay;
using DirectShow.Helper;

namespace ShairportSharp_Test
{
    public class rotPlayer : DSFilePlayback
    {
        DirectShow.DsROTEntry _rot = null;
        protected override HRESULT OnInitInterfaces()
        {
            _rot = new DirectShow.DsROTEntry(m_GraphBuilder);
            return base.OnInitInterfaces();
        }

        public override void Dispose()
        {
            if (_rot != null)
            {
                _rot.Dispose();
                _rot = null;
            }
            base.Dispose();
        }
    }

    public partial class VideoForm : Form
    {
        AirplayServer server;
        string sessionId;
        rotPlayer m_Playback = null;
        public VideoForm(AirplayServer server, string sessionId)
        {
            InitializeComponent();
            this.server = server;
            this.sessionId = sessionId;
            m_Playback = new rotPlayer();
            m_Playback.VideoControl = this.videoControl;
            m_Playback.OnPlaybackStart += Playback_OnPlaybackStart;
            m_Playback.OnPlaybackStop += Playback_OnPlaybackStop;
            m_Playback.OnPlaybackReady += Playback_OnPlaybackReady;
            m_Playback.OnPlaybackPause += Playback_OnPlaybackPause;
        }

        void Playback_OnPlaybackPause(object sender, EventArgs e)
        {
            
        }

        public void LoadVideo(VideoEventArgs e)
        {
            m_Playback.Stop();
            m_Playback.FileName = e.ContentLocation;
            server.SetPlaybackState(sessionId, PlaybackCategory.Video, PlaybackState.Loading);
        }

        public void GetProgress(GetPlaybackPositionEventArgs e)
        {
            e.Duration = m_Playback.Duration / (double)COMHelper.UNITS;
            e.Position = m_Playback.Position / (double)COMHelper.UNITS;
        }

        public void SetPosition(PlaybackPositionEventArgs e)
        {
            long position = (long)e.Position * COMHelper.UNITS;
            if (position <= m_Playback.Duration)
            {
                m_Playback.Position = position;
            }
        }

        public void GetPlaybackInfo(PlaybackInfo playbackInfo)
        {
            playbackInfo.ReadyToPlay = true;
            playbackInfo.Duration = m_Playback.Duration / (double)COMHelper.UNITS;
            playbackInfo.Position = m_Playback.Position / (double)COMHelper.UNITS;
            playbackInfo.PlaybackBufferEmpty = false;
            playbackInfo.PlaybackBufferFull = true;
            playbackInfo.PlaybackLikelyToKeepUp = true;

            PlaybackTimeRange timeRange = new PlaybackTimeRange() { Duration = playbackInfo.Duration };
            playbackInfo.LoadedTimeRanges.Add(timeRange);
            playbackInfo.SeekableTimeRanges.Add(timeRange);
            playbackInfo.Rate = m_Playback.IsPaused ? 0 : 1;
        }

        bool ignoreRate = true;
        public void SetPlaybackRate(PlaybackRateEventArgs e)
        {
            if (ignoreRate)
            {
                ignoreRate = false;
                return;
            }

            if (e.Rate == 0)
            {
                if (!m_Playback.IsPaused)
                {
                    m_Playback.Pause();
                }
            }
            else if (e.Rate == 1)
            {
                if (m_Playback.IsPaused)
                {
                    m_Playback.Pause();
                }
            }
        }

        private void Playback_OnPlaybackStart(object sender, EventArgs e)
        {
            
        }

        private void Playback_OnPlaybackStop(object sender, EventArgs e)
        {
            
        }

        private void Playback_OnPlaybackReady(object sender, EventArgs e)
        {
            
        }

        private void VideoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_Playback.Dispose();
            m_Playback = null;
        }
    }
}
