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
                try
                {
                    _rot.Dispose();
                }
                catch { }
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
        bool hasStarted = false;
        bool hasFinished = false;

        public VideoForm(AirplayServer server, string sessionId)
        {
            InitializeComponent();
            this.server = server;
            m_Playback = new rotPlayer();
            m_Playback.VideoControl = this.videoControl;
            m_Playback.OnPlaybackStart += Playback_OnPlaybackStart;
            m_Playback.OnPlaybackStop += Playback_OnPlaybackStop;
            m_Playback.OnPlaybackReady += Playback_OnPlaybackReady;
            m_Playback.OnPlaybackPause += Playback_OnPlaybackPause;
        }

        public void LoadVideo(VideoEventArgs e)
        {
            hasStarted = false;
            hasFinished = false;
            sessionId = e.SessionId;
            server.SetPlaybackState(sessionId, PlaybackCategory.Video, PlaybackState.Loading);
            m_Playback.Stop();
            m_Playback.FileName = e.ContentLocation;
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
            if (hasStarted && m_Playback.Duration > 0)
            {
                playbackInfo.Duration = m_Playback.Duration / (double)COMHelper.UNITS;
                playbackInfo.Position = m_Playback.Position / (double)COMHelper.UNITS;
                playbackInfo.PlaybackLikelyToKeepUp = true;
                PlaybackTimeRange timeRange = new PlaybackTimeRange() { Duration = playbackInfo.Duration };
                playbackInfo.LoadedTimeRanges.Add(timeRange);
                playbackInfo.SeekableTimeRanges.Add(timeRange);
                playbackInfo.Rate = m_Playback.IsPaused || hasFinished ? 0 : 1;
            }
        }

        public void SetPlaybackRate(PlaybackRateEventArgs e)
        {
            if (hasStarted && m_Playback.Duration > 0)
            {
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
        }

        private void Playback_OnPlaybackStart(object sender, EventArgs e)
        {
            hasStarted = true;
            server.SetPlaybackState(sessionId, PlaybackCategory.Video, PlaybackState.Playing);
        }

        void Playback_OnPlaybackPause(object sender, EventArgs e)
        {
            server.SetPlaybackState(sessionId, PlaybackCategory.Video, PlaybackState.Paused);
        }

        private void Playback_OnPlaybackStop(object sender, EventArgs e)
        {
            if (hasStarted && !hasFinished)
            {
                server.SetPlaybackState(sessionId, PlaybackCategory.Video, PlaybackState.Stopped);
                hasFinished = true;
            }
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
