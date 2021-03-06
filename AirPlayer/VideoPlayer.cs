﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DirectShowLib;
using DShowNET.Helper;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;

namespace AirPlayer
{
    class AvailableTimerange
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }

    class VideoPlayer : VideoPlayerVMR9
    {
        public const string DUMMY_URL = "http://localhost/AirPlayer.mp4";
        public const string DEFAULT_SOURCE_FILTER = "File Source (URL)";
        public const string MPURL_SOURCE_FILTER = "MediaPortal Url Source Splitter";

        string sourceFilterName;
        float percentageBuffered;
        DateTime lastProgressCheck = DateTime.MinValue;

        int bufferPercent = 2;
        public int BufferPercent
        {
            get { return bufferPercent; }
            set 
            {
                if (value < 0 || value > 100)
                    return;
                bufferPercent = value; 
            }
        }

        public bool BufferingStopped { get; protected set; }
        public void StopBuffering() { BufferingStopped = true; }

        protected bool skipBuffering = false;
        public void SkipBuffering() { skipBuffering = true; }

        public VideoPlayer(string url, string sessionId, string sourceFilterName = "File Source (URL)")
            : base(g_Player.MediaType.Video)
        {
            m_strCurrentFile = url;
            SessionId = sessionId;
            this.sourceFilterName = sourceFilterName;
        }

        public string Url { get { return m_strCurrentFile; } }
        public string SourceFilterName { get { return sourceFilterName; } }
        public string SessionId { get; protected set; }

        public override string CurrentFile // hack to get around the MP 1.3 Alpha bug with non http URLs
        {
            get { return DUMMY_URL; }
        }

        protected override bool GetInterfaces()
        {
            if (graphBuilder != null) // graph was already started and playback file buffered
                return FinishPreparedGraph();
            else
                return base.GetInterfaces();
        }

        /// <summary>
        /// If the url to be played can be buffered before starting playback, this function
        /// starts building a graph by adding the preferred video and audio render to it.
        /// This needs to be called on the MpMain Thread.
        /// </summary>
        /// <returns>true, if the url can be buffered (a graph was started), false if it can't be and null if an error occured building the graph</returns>
        public bool? PrepareGraph()
        {
            //string sourceFilterName = SOURCE_FILTER_NAME; //GetSourceFilterName(m_strCurrentFile);

            if (!string.IsNullOrEmpty(sourceFilterName))
            {
                graphBuilder = (IGraphBuilder)new FilterGraph();
                _rotEntry = new DsROTEntry((IFilterGraph)graphBuilder);

                Vmr9 = new VMR9Util();
                Vmr9.AddVMR9(graphBuilder);
                Vmr9.Enable(false);
                // set VMR9 back to NOT Active -> otherwise GUI is not refreshed while graph is building
                GUIGraphicsContext.Vmr9Active = false;

                // add the audio renderer
                using (Settings settings = new MPSettings())
                {
                    string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                    DirectShowUtil.AddAudioRendererToGraph(graphBuilder, audiorenderer, false);
                }

                // set fields for playback
                mediaCtrl = (IMediaControl)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                mediaSeek = (IMediaSeeking)graphBuilder;
                mediaPos = (IMediaPosition)graphBuilder;
                basicAudio = (IBasicAudio)graphBuilder;
                videoWin = (IVideoWindow)graphBuilder;

                // add the source filter
                return tryAddSourceFilter();
            }
            else
            {
                return false;
            }
        }

        bool? tryAddSourceFilter()
        {
            bool? result = null;
            IBaseFilter sourceFilter = null;
            try
            {
                sourceFilter = DirectShowUtil.AddFilterToGraph(graphBuilder, sourceFilterName);
                if (sourceFilter != null)
                {
                    //openProgress = sourceFilter as IAMOpenProgress;
                    result = true;
                    Logger.Instance.Debug("AirPlayerVideo: Using source filter '{0}'", sourceFilterName);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn("Error adding '{0}' filter to graph: {1}", sourceFilterName, ex.Message);
            }
            finally
            {
                if (sourceFilter != null) DirectShowUtil.ReleaseComObject(sourceFilter);
            }

            if (result == null && sourceFilterName != DEFAULT_SOURCE_FILTER)
            {
                Logger.Instance.Warn("Failed to add source filter '{0}', falling back to default source filter '{1}'", sourceFilterName, DEFAULT_SOURCE_FILTER);
                sourceFilterName = DEFAULT_SOURCE_FILTER;
                return tryAddSourceFilter();
            }

            return result;
        }

        /// <summary>
        /// This function can be called by a background thread. It finishes building the graph and
        /// waits until the buffer is filled to the configured percentage.
        /// If a filter in the graph requires the full file to be downloaded, the function will return only afterwards.
        /// </summary>
        /// <returns>true, when playback can be started</returns>
        public bool BufferFile()
        {
            Thread renderPinsThread = null;
            VideoRendererStatistics.VideoState = VideoRendererStatistics.State.VideoPresent; // prevents the BlackRectangle on first time playback
            bool PlaybackReady = false;
            IBaseFilter sourceFilter = null;
            try
            {
                int result = graphBuilder.FindFilterByName(sourceFilterName, out sourceFilter);
                if (result != 0)
                {
                    string errorText = DirectShowLib.DsError.GetErrorText(result);
                    if (errorText != null) errorText = errorText.Trim();
                    Logger.Instance.Warn("BufferFile : FindFilterByName returned '{0}'{1}", "0x" + result.ToString("X8"), !string.IsNullOrEmpty(errorText) ? " : (" + errorText + ")" : "");
                    return false;
                }

                Marshal.ThrowExceptionForHR(((IFileSourceFilter)sourceFilter).Load(m_strCurrentFile, null));
                OnlineVideos.MPUrlSourceFilter.IFilterState filterState = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterState;
                if (sourceFilter is IAMOpenProgress)
                {
                    // buffer before starting playback
                    bool filterConnected = false;
                    percentageBuffered = 0.0f;
                    long total = 0, current = 0, last = 0;
                    do
                    {
                        result = ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                        Marshal.ThrowExceptionForHR(result);

                        percentageBuffered = (float)current / (float)total * 100.0f;
                        // after configured percentage has been buffered, connect the graph
                        if (!filterConnected && (percentageBuffered >= bufferPercent || skipBuffering))
                        {
                            if (((filterState != null) && (filterState.IsFilterReadyToConnectPins())) ||
                                (filterState == null))
                            {
                                //cacheFile = filterState.GetCacheFileName();
                                if (skipBuffering) Logger.Instance.Debug("Buffering skipped at {0}%", percentageBuffered);
                                filterConnected = true;
                                renderPinsThread = new Thread(delegate()
                                {
                                    try
                                    {
                                        Logger.Instance.Debug("BufferFile : Rendering unconnected output pins of source filter ...");
                                        // add audio and video filter from MP Movie Codec setting section
                                        AddPreferredFilters(graphBuilder, sourceFilter);
                                        // connect the pin automatically -> will buffer the full file in cases of bad metadata in the file or request of the audio or video filter
                                        DirectShowUtil.RenderUnconnectedOutputPins(graphBuilder, sourceFilter);
                                        Logger.Instance.Debug("BufferFile : Playback Ready.");
                                        PlaybackReady = true;
                                    }
                                    catch (ThreadAbortException)
                                    {
                                        Thread.ResetAbort();
                                        Logger.Instance.Info("RenderUnconnectedOutputPins foribly aborted.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Instance.Warn(ex.Message);
                                        StopBuffering();
                                    }
                                }) { IsBackground = true, Name = "AirPlayerGraph" };
                                renderPinsThread.Start();
                            }
                        }
                        // log every percent
                        if (current > last && current - last >= (double)total * 0.01)
                        {
                            Logger.Instance.Debug("Buffering: {0}/{1} KB ({2}%)", current / 1024, total / 1024, (int)percentageBuffered);
                            last = current;
                        }
                        // set the percentage to a gui property, formatted according to percentage, so the user knows very early if anything is buffering                   
                        string formatString = "###";
                        if (percentageBuffered == 0f) formatString = "0.0";
                        else if (percentageBuffered < 1f) formatString = ".00";
                        else if (percentageBuffered < 10f) formatString = "0.0";
                        else if (percentageBuffered < 100f) formatString = "##";
                        GUIPropertyManager.SetProperty("#AirPlayerVideo.buffered", percentageBuffered.ToString(formatString, System.Globalization.CultureInfo.InvariantCulture));
                        Thread.Sleep(50); // no need to do this more often than 20 times per second
                    }
                    while (!PlaybackReady && graphBuilder != null && !BufferingStopped);
                }
                else
                {
                    if (filterState != null)
                    {
                        while (!filterState.IsFilterReadyToConnectPins())
                        {
                            Thread.Sleep(50);
                        }
                        //cacheFile = filterState.GetCacheFileName();
                    }
                    // add audio and video filter from MP Movie Codec setting section
                    AddPreferredFilters(graphBuilder, sourceFilter);
                    // connect the pin automatically -> will buffer the full file in cases of bad metadata in the file or request of the audio or video filter
                    DirectShowUtil.RenderUnconnectedOutputPins(graphBuilder, sourceFilter);
                    percentageBuffered = 100.0f; // no progress reporting possible
                    GUIPropertyManager.SetProperty("#TV.Record.percent3", percentageBuffered.ToString());
                    PlaybackReady = true;
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (COMException comEx)
            {
                Logger.Instance.Warn(comEx.ToString());

                if (sourceFilterName == MPURL_SOURCE_FILTER &&
                    Enum.IsDefined(typeof(OnlineVideos.MPUrlSourceFilter.MPUrlSourceSplitterError), comEx.ErrorCode))
                {
                    throw new Exception(((OnlineVideos.MPUrlSourceFilter.MPUrlSourceSplitterError)comEx.ErrorCode).ToString());
                }

                string errorText = DirectShowLib.DsError.GetErrorText(comEx.ErrorCode);
                if (errorText != null) errorText = errorText.Trim();
                if (!string.IsNullOrEmpty(errorText))
                {
                    throw new Exception(errorText);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn(ex.ToString());
            }
            finally
            {
                if (sourceFilter != null)
                {
                    // the render pin thread was already started and is still runnning
                    if (renderPinsThread != null && (renderPinsThread.ThreadState & ThreadState.Stopped) == 0)
                    {
                        // buffering was stopped by the user -> abort the thread
                        if (BufferingStopped) renderPinsThread.Abort();
                    }

                    // playback is not ready but the source filter is already downloading -> abort the operation
                    if (!PlaybackReady)
                    {
                        Logger.Instance.Info("Buffering was aborted.");
                        if (sourceFilter != null) ((IAMOpenProgress)sourceFilter).AbortOperation();
                        Thread.Sleep(100); // give it some time
                        int result = graphBuilder.RemoveFilter(sourceFilter); // remove the filter from the graph to prevent lockup later in Dispose
                    }

                    // release the COM pointer that we created
                    DirectShowUtil.ReleaseComObject(sourceFilter);
                    
                }
            }

            return PlaybackReady;
        }

        /// <summary>
        /// Third and last step of a graph build with the file source url filter used to monitor buffer.
        /// Needs to be called on the MpMain Thread.
        /// </summary>
        /// <returns></returns>
        bool FinishPreparedGraph()
        {
            try
            {
                DirectShowUtil.EnableDeInterlace(graphBuilder);

                if (Vmr9 == null || !Vmr9.IsVMR9Connected)
                {
                    Logger.Instance.Warn("AirPlayerVideo: Failed to render file -> No video renderer connected");
                    mediaCtrl = null;
                    Cleanup();
                    return false;
                }

                try
                {
                    // remove filter that are not used from the graph
                    DirectShowUtil.RemoveUnusedFiltersFromGraph(graphBuilder);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Warn("Error during RemoveUnusedFiltersFromGraph: {0}", ex.ToString());
                }

                //if (Log.Instance.LogLevel < log4net.Core.Level.Debug)
                //{
                //    string sourceFilterName = GetSourceFilterName(m_strCurrentFile);
                //    if (!string.IsNullOrEmpty(sourceFilterName))
                //    {
                //        IBaseFilter sourceFilter;
                //        if (graphBuilder.FindFilterByName(sourceFilterName, out sourceFilter) == 0 && sourceFilter != null)
                //        {
                //            LogOutputPinsConnectionRecursive(sourceFilter);
                //        }
                //        if (sourceFilter != null) DirectShowUtil.ReleaseComObject(sourceFilter);
                //    }
                //}

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
            Logger.Instance.Info("AirPlayerVideo: Play '{0}'", m_strCurrentFile);

            m_bStarted = false;
            if (!GetInterfaces())
            {
                m_strCurrentFile = "";
                CloseInterfaces();
                return false;
            }

            AnalyseStreams();
            SelectSubtitles();
            SelectAudioLanguage();
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
                    // wait max. 20 seconds for the graph to transition to the running state
                    DateTime startTime = DateTime.Now;
                    FilterState filterState;
                    do
                    {
                        Thread.Sleep(100);
                        hr = mediaCtrl.GetState(100, out filterState); // check with timeout max. 10 times a second if the state changed
                    }
                    while ((hr != 0) && ((DateTime.Now - startTime).TotalSeconds <= 20));
                    if (hr != 0) // S_OK
                    {
                        DsError.ThrowExceptionForHR(hr);
                        throw new Exception(string.Format("IMediaControl.GetState after 20 seconds: 0x{0} - '{1}'", hr.ToString("X8"), DsError.GetErrorText(hr)));
                    }
                }
            }
            catch (Exception error)
            {
                Logger.Instance.Warn("AirPlayerVideo: Unable to play with reason: {0}", error.Message);
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
            m_strCurrentFile = "";
            CloseInterfaces();
            m_state = PlayState.Init;
            GUIGraphicsContext.IsPlaying = false;
        }

        public AvailableTimerange GetAvailableTimerange()
        {
                if (mediaSeek != null)
                {
                    try
                    {
                        long start, end;
                        int hr = mediaSeek.GetAvailable(out start, out end);
                        Marshal.ThrowExceptionForHR(hr);
                        return new AvailableTimerange()
                        { 
                            StartTime = start / (double)DirectShow.Helper.COMHelper.UNITS,
                            EndTime = end / (double)DirectShow.Helper.COMHelper.UNITS
                        };
                    }
                    catch(Exception ex)
                    {
                        Logger.Instance.Warn("AirPlayerVideoPlayer: Exception getting buffered duration - {0}\r\n{1}", ex.Message, ex.StackTrace);
                    }
                }
                return null;
        }

        public override double CurrentPosition
        {
            get
            {
                if (mediaSeek != null)
                {
                    long currentPosition;
                    int hr = mediaSeek.GetCurrentPosition(out currentPosition);
                    if (hr == 0)
                        return currentPosition / (double)DirectShow.Helper.COMHelper.UNITS;
                }
                return -1;
            }
        }

        public override double Duration
        {
            get
            {
                if (mediaSeek != null)
                {
                    long duration;
                    int hr = mediaSeek.GetDuration(out duration);
                    if (hr == 0)
                        return duration / (double)DirectShow.Helper.COMHelper.UNITS;
                }
                return -1;
            }
        }

        protected override void CloseInterfaces()
        {
            base.CloseInterfaces();
        }

        public override void Dispose()
        {
            base.Dispose();
            GUIPropertyManager.SetProperty("#TV.Record.percent3", 0.0f.ToString());
        }

        public static readonly Guid MEDIASUBTYPE_AVC1 = new Guid("31435641-0000-0010-8000-00aa00389b71");
        public static void AddPreferredFilters(IGraphBuilder graphBuilder, IBaseFilter sourceFilter)
        {
            using (Settings xmlreader = new MPSettings())
            {
                bool autodecodersettings = xmlreader.GetValueAsBool("movieplayer", "autodecodersettings", false);

                if (!autodecodersettings) // the user has not chosen automatic graph building by merits
                {
                    // bool vc1ICodec,vc1Codec,xvidCodec = false; - will come later
                    bool aacCodec = false;
                    bool h264Codec = false;

                    // check the output pins of the splitter for known media types
                    IEnumPins pinEnum = null;
                    if (sourceFilter.EnumPins(out pinEnum) == 0)
                    {
                        int fetched = 0;
                        IPin[] pins = new IPin[1];
                        while (pinEnum.Next(1, pins, out fetched) == 0 && fetched > 0)
                        {
                            IPin pin = pins[0];
                            PinDirection pinDirection;
                            if (pin.QueryDirection(out pinDirection) == 0 && pinDirection == PinDirection.Output)
                            {
                                IEnumMediaTypes enumMediaTypesVideo = null;
                                if (pin.EnumMediaTypes(out enumMediaTypesVideo) == 0)
                                {
                                    AMMediaType[] mediaTypes = new AMMediaType[1];
                                    int typesFetched;
                                    while (enumMediaTypesVideo.Next(1, mediaTypes, out typesFetched) == 0 && typesFetched > 0)
                                    {
                                        if (mediaTypes[0].majorType == MediaType.Video &&
                                            (mediaTypes[0].subType == MediaSubType.H264 || mediaTypes[0].subType == MEDIASUBTYPE_AVC1))
                                        {
                                            Logger.Instance.Info("found H264 video on output pin");
                                            h264Codec = true;
                                        }
                                        else if (mediaTypes[0].majorType == MediaType.Audio && mediaTypes[0].subType == MediaSubType.LATMAAC)
                                        {
                                            Logger.Instance.Info("found AAC audio on output pin");
                                            aacCodec = true;
                                        }
                                    }
                                    DirectShowUtil.ReleaseComObject(enumMediaTypesVideo);
                                }
                            }
                            DirectShowUtil.ReleaseComObject(pin);
                        }
                        DirectShowUtil.ReleaseComObject(pinEnum);
                    }

                    // add filters for found media types to the graph as configured in MP
                    if (h264Codec)
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "h264videocodec", "")));
                    }
                    else
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "mpeg2videocodec", "")));
                    }
                    if (aacCodec)
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "aacaudiocodec", "")));
                    }
                    else
                    {
                        DirectShowUtil.ReleaseComObject(
                            DirectShowUtil.AddFilterToGraph(graphBuilder, xmlreader.GetValueAsString("movieplayer", "mpeg2audiocodec", "")));
                    }
                }
            }
        }
    }
}
