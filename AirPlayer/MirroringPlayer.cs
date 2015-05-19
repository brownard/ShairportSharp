using AirPlayer.Common.DirectShow;
using DirectShowLib;
using DShowNET.Helper;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;
using ShairportSharp.Mirroring;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace AirPlayer
{
    class MirroringPlayer : VideoPlayerVMR9
    {
        public const string DUMMY_URL = "http://localhost/AirPlayerMirroring.mp4";
        const string LAV_VIDEO_GUID = "{EE30215D-164F-4A92-A4EB-9D4C13390F9F}";

        DirectShow.IGraphBuilder managedGraphBuilder;
        MirroringStream stream;

        public MirroringPlayer(MirroringStream stream)
        {
            this.stream = stream;
        }

        public override string CurrentFile // hack to get around the MP 1.3 Alpha bug with non http URLs
        {
            get { return DUMMY_URL; }
        }

        protected override bool GetInterfaces()
        {
            try
            {
                managedGraphBuilder = (DirectShow.IGraphBuilder)new DirectShow.FilterGraph();

                graphBuilder = (IGraphBuilder)managedGraphBuilder;
                _rotEntry = new DsROTEntry((IFilterGraph)graphBuilder);

                Vmr9 = new VMR9Util();
                Vmr9.AddVMR9(graphBuilder);
                Vmr9.Enable(false);

                // set VMR9 back to NOT Active -> otherwise GUI is not refreshed while graph is building
                GUIGraphicsContext.Vmr9Active = false;

                // set fields for playback
                mediaCtrl = (IMediaControl)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                mediaSeek = (IMediaSeeking)graphBuilder;
                mediaPos = (IMediaPosition)graphBuilder;
                basicAudio = (IBasicAudio)graphBuilder;
                videoWin = (IVideoWindow)graphBuilder;

                int hr;
                var mirroringFilter = new MirroringSourceFilter(stream);
                using (var sourceFilter = new DirectShow.Helper.DSFilter(mirroringFilter))
                {
                    hr = managedGraphBuilder.AddFilter(sourceFilter.Value, sourceFilter.Name);
                    new DirectShow.Helper.HRESULT(hr).Throw();

                    AddVideoFilter();

                    hr = managedGraphBuilder.Render(sourceFilter.OutputPin.Value);
                    new DirectShow.Helper.HRESULT(hr).Throw();

                    //The client stops sending data when the screen content isn't changing to save bandwidth/processing
                    //However this causes the renderer to generate quality control messages leading to some filters dropping frames
                    //The stream only contains one I-Frame at the start so the video cannot recover from dropped frames
                    //We override the quality management to prevent the filter receiving the messages
                    mirroringFilter.SetQualityControl(managedGraphBuilder);
                }

                DirectShowUtil.EnableDeInterlace(graphBuilder);

                if (Vmr9 == null || !Vmr9.IsVMR9Connected)
                {
                    Logger.Instance.Warn("AirPlayerMirroring: Failed to render file -> No video renderer connected");
                    mediaCtrl = null;
                    Cleanup();
                    return false;
                }

                this.Vmr9.SetDeinterlaceMode();

                // now set VMR9 to Active
                GUIGraphicsContext.Vmr9Active = true;

                // set fields for playback                
                m_iVideoWidth = Vmr9.VideoWidth;
                m_iVideoHeight = Vmr9.VideoHeight;

                Vmr9.SetDeinterlaceMode();
                return true;
            }
            catch (Exception ex)
            {
                Error.SetError("Unable to play movie", "Unable build graph for VMR9");
                Logger.Instance.Error("AirPlayerVideo:exception while creating DShow graph {0} {1}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public override bool Play(string strFile)
        {
            updateTimer = DateTime.Now;
            m_speedRate = 10000;
            m_bVisible = false;
            m_iVolume = 100;
            m_state = PlayState.Init;
            if (strFile != DUMMY_URL) m_strCurrentFile = strFile; // hack to get around the MP 1.3 Alpha bug with non http URLs
            m_bFullScreen = true;
            m_ar = GUIGraphicsContext.ARType;
            VideoRendererStatistics.VideoState = VideoRendererStatistics.State.VideoPresent;
            _updateNeeded = true;
            Logger.Instance.Info("AirPlayerMirroring: Play '{0}'", m_strCurrentFile);

            m_bStarted = false;
            if (!GetInterfaces())
            {
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }

            //AnalyseStreams();
            //SelectSubtitles();
            //SelectAudioLanguage();
            OnInitialized();

            int hr = mediaEvt.SetNotifyWindow(GUIGraphicsContext.ActiveForm, WM_GRAPHNOTIFY, IntPtr.Zero);
            if (hr < 0)
            {
                Error.SetError("Unable to play movie", "Can not set notifications");
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }
            if (videoWin != null)
            {
                videoWin.put_Owner(GUIGraphicsContext.ActiveForm);
                videoWin.put_WindowStyle(
                  (WindowStyle)((int)WindowStyle.Child + (int)WindowStyle.ClipChildren + (int)WindowStyle.ClipSiblings));
                videoWin.put_MessageDrain(GUIGraphicsContext.form.Handle);
            }
            if (basicVideo != null)
            {
                hr = basicVideo.GetVideoSize(out m_iVideoWidth, out m_iVideoHeight);
                if (hr < 0)
                {
                    Error.SetError("Unable to play movie", "Can not find movie width/height");
                    m_strCurrentFile = "";
                    CloseInterfaces();
                    return false;
                }
            }

            DirectShowUtil.SetARMode(graphBuilder, AspectRatioMode.Stretched);

            try
            {
                hr = mediaCtrl.Run();
                DsError.ThrowExceptionForHR(hr);
                if (hr == 1) // S_FALSE from IMediaControl::Run means: The graph is preparing to run, but some filters have not completed the transition to a running state.
                {
                    // wait max. 10 seconds for the graph to transition to the running state
                    DateTime startTime = DateTime.Now;
                    FilterState filterState;
                    do
                    {
                        Thread.Sleep(100);
                        hr = mediaCtrl.GetState(100, out filterState); // check with timeout max. 10 times a second if the state changed
                    }
                    while ((hr != 0) && ((DateTime.Now - startTime).TotalSeconds <= 10));
                    if (hr != 0) // S_OK
                    {
                        DsError.ThrowExceptionForHR(hr);
                        throw new Exception(string.Format("IMediaControl.GetState after 10 seconds: 0x{0} - '{1}'", hr.ToString("X8"), DsError.GetErrorText(hr)));
                    }
                }
            }
            catch (Exception error)
            {
                Logger.Instance.Warn("AirPlayerMirroring: Unable to play with reason: {0}", error.Message);
            }
            if (hr != 0) // S_OK
            {
                Error.SetError("Unable to play movie", "Unable to start movie");
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }

            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
            msg.Label = CurrentFile;
            GUIWindowManager.SendThreadMessage(msg);
            m_state = PlayState.Playing;
            m_iPositionX = GUIGraphicsContext.VideoWindow.X;
            m_iPositionY = GUIGraphicsContext.VideoWindow.Y;
            m_iWidth = GUIGraphicsContext.VideoWindow.Width;
            m_iHeight = GUIGraphicsContext.VideoWindow.Height;
            m_ar = GUIGraphicsContext.ARType;
            _updateNeeded = true;
            SetVideoWindow();
            mediaPos.get_Duration(out m_dDuration);
            Logger.Instance.Info("AirPlayerVideo: Duration {0} sec", m_dDuration.ToString("F"));
            return true;
        }

        public override void Stop()
        {
            Logger.Instance.Info("AirPlayerVideo: Stop");
            if (stream != null)
                stream.Stop();
            m_strCurrentFile = "";
            CloseInterfaces();
            m_state = PlayState.Init;
            GUIGraphicsContext.IsPlaying = false;
        }

        void AddVideoFilter()
        {
            using (Settings xmlreader = new MPSettings())
            {
                bool autodecodersettings = xmlreader.GetValueAsBool("movieplayer", "autodecodersettings", false);
                if (!autodecodersettings) // the user has not chosen automatic graph building by merits
                {
                    string filterName = xmlreader.GetValueAsString("movieplayer", "h264videocodec", "");
                    if (!string.IsNullOrEmpty(filterName))
                        Utils.AddFilterByName(managedGraphBuilder, DirectShow.FilterCategory.LegacyAmFilterCategory, filterName);
                }
            }
        }
    }
}