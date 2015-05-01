using MediaPortal.GUI.Library;
using MediaPortal.Player;
using ShairportSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ShairportSharp.Audio;
using AirPlayer.Config;
using ShairportSharp.Remote;
using ShairportSharp.Raop;
using ShairportSharp.Airplay;
using MediaPortal.Dialogs;
using ShairportSharp.Helpers;
using AirPlayer.Common.Player;
using AirPlayer.Common.Proxy;
using AirPlayer.Common.Hls;

namespace AirPlayer
{
    [MediaPortal.Configuration.PluginIcons("AirPlayer.MPE.airplay-icon.png", "AirPlayer.MPE.airplay-icon-faded.png")]
    public class AirPlayer : IPlugin, ISetupForm
    {
        #region Consts

        const string PLUGIN_NAME = "AirPlayer";
        const int SKIN_PROPERTIES_UPDATE_DELAY = 2000;

        #endregion

        #region Variables

        string pluginIconPath;

        RaopServer airtunesServer;
        AirplayServer airplayServer;
        HlsProxy proxy;

        PhotoWindow photoWindow;
        string photoSessionId;

        VideoPlayer currentVideoPlayer;
        VideoPlayer bufferingPlayer;
        Thread videoBufferThread;
        object bufferLock = new object();
        HlsParser hlsParser;
        string currentVideoSessionId;
        string currentVideoUrl;
        string lastVideoSessionId;
        string lastVideoUrl;
        bool lastUseMPUrlSourceFilter;
        DateTime videoReceiveTime = DateTime.MinValue;

        IAudioPlayer currentAudioPlayer;
        string currentAudioSessionId;
        bool isAudioBuffering;
        DmapData currentMeta;
        string currentCover;
        string nextCover;
        uint currentStartStamp;
        uint currentStopStamp;
        
        int coverNumber = 0;
        object coverLock = new object();

        int? savedVolume;
        bool allowVolumeControl;
        bool sendCommands;
        bool allowHDStreams;
        int videoBuffer;

        bool isAudioPlaying;
        bool isVideoPlaying;

        MirroringPlayer currentMirroringPlayer;
        bool isMirroringStarting;
        
        #endregion

        #region Ctor

        public AirPlayer()
        {
            Common.Logger.SetLogger(Logger.Instance);
            ShairportSharp.Logger.SetLogger(Logger.Instance);
        }

        #endregion

        #region ISetupForm Members

        public string Author()
        {
            return "Brownard";
        }

        public bool CanEnable()
        {
            return true;
        }

        public bool DefaultEnabled()
        {
            return true;
        }

        public string Description()
        {
            return "An AirPlay server emulator";
        }

        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = null;
            strButtonImage = null;
            strButtonImageFocus = null;
            strPictureImage = null;
            return false;
        }

        public int GetWindowId()
        {
            return -1;
        }

        public bool HasSetup()
        {
            return true;
        }

        public string PluginName()
        {
            return PLUGIN_NAME;
        }

        public void ShowPlugin()
        {
            new Configuration().ShowDialog();
        }

        #endregion

        #region IPlugin Members

        public void Start()
        {
            pluginIconPath = MediaPortal.Configuration.Config.GetFile(MediaPortal.Configuration.Config.Dir.Thumbs, "AirPlayer", "airplay-icon.png");
            GUIWindow window = new PhotoWindow();
            window.Init();
            GUIWindowManager.Add(ref window);
            photoWindow = (PhotoWindow)window;

            PluginSettings settings = new PluginSettings();
            allowVolumeControl = settings.AllowVolume;
            sendCommands = settings.SendCommands;
            allowHDStreams = settings.AllowHDStreams;
            videoBuffer = settings.VideoBuffer;

            airtunesServer = new RaopServer(settings.ServerName, settings.Password);
            airtunesServer.MacAddress = settings.CustomAddress;
            airtunesServer.Port = settings.RtspPort;
            airtunesServer.AudioPort = settings.UdpPort;
            airtunesServer.AudioBufferSize = (int)(settings.AudioBuffer * 1000);
            airtunesServer.StreamStarting += airtunesServer_StreamStarting;
            airtunesServer.StreamReady += airtunesServer_StreamReady;
            airtunesServer.StreamStopped += airtunesServer_StreamStopped;
            airtunesServer.PlaybackProgressChanged += airtunesServer_PlaybackProgressChanged;
            airtunesServer.MetaDataChanged += airtunesServer_MetaDataChanged;
            airtunesServer.ArtworkChanged += airtunesServer_ArtworkChanged;
            if (allowVolumeControl)
                airtunesServer.VolumeChanged += airtunesServer_VolumeChanged;
            airtunesServer.Start();

            airplayServer = new AirplayServer(settings.ServerName, settings.Password);
            airplayServer.MacAddress = settings.CustomAddress;
            airplayServer.Port = settings.AirplayPort;
            airplayServer.iOS8Workaround = settings.iOS8Workaround;
            airplayServer.ShowPhoto += airplayServer_ShowPhoto;
            airplayServer.VideoReceived += airplayServer_VideoReceived;
            airplayServer.PlaybackInfoRequested += airplayServer_PlaybackInfoRequested;
            airplayServer.GetPlaybackPosition += airplayServer_GetPlaybackPosition;
            airplayServer.PlaybackPositionChanged += airplayServer_PlaybackPositionChanged;
            airplayServer.PlaybackRateChanged += airplayServer_PlaybackRateChanged;
            if (allowVolumeControl)
                airplayServer.VolumeChanged += airplayServer_VolumeChanged;
            airplayServer.SessionStopped += airplayServer_SessionStopped;

            airplayServer.MirroringServer.Authenticating += MirroringServer_Authenticating;
            airplayServer.MirroringServer.Started += MirroringServer_Started;

            airplayServer.Start();

            g_Player.PlayBackChanged += g_Player_PlayBackChanged;
            g_Player.PlayBackStopped += g_Player_PlayBackStopped;
            g_Player.PlayBackEnded += g_Player_PlayBackEnded;
            GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;
        }
        
        public void Stop()
        {
            if (airtunesServer != null)
                airtunesServer.Stop();
            if (airplayServer != null)
                airplayServer.Stop();
        }

        #endregion

        #region Airtunes Event Handlers
        
        void airtunesServer_StreamStarting(object sender, RaopEventArgs e)
        {
            invoke(delegate()
            {
                stopCurrentItem();
                cleanupPlayback();
                GUIWaitCursor.Init(); GUIWaitCursor.Show();
                currentAudioSessionId = e.SessionId;
                isAudioBuffering = true;
            });

        }

        void airtunesServer_StreamReady(object sender, RaopEventArgs e)
        {
            AudioBufferStream input = airtunesServer.GetStream(StreamType.Wave);
            if (input == null)
                return;

            invoke(delegate()
            {
                if (!isAudioBuffering)
                {
                    airtunesServer.SendCommand(RemoteCommand.Stop);
                    return;
                }
                isAudioBuffering = false;
                GUIWaitCursor.Hide();
                startPlayback(input);
            }, false);
        }

        void startPlayback(AudioBufferStream stream)
        {
            stopCurrentItem();
            IPlayer player = new AudioPlayer(new PlayerSettings(stream));
            currentAudioPlayer = player as IAudioPlayer;
            IPlayerFactory savedFactory = g_Player.Factory;
            g_Player.Factory = new PlayerFactory(player);
            isAudioPlaying = g_Player.Play(AudioPlayer.AIRPLAY_DUMMY_FILE, g_Player.MediaType.Music);
            g_Player.Factory = savedFactory;

            if (!isAudioPlaying)
            {
                currentAudioPlayer = null;
                return;
            }
            //Mediaportal sets the metadata skin properties internally, we overwrite them after a small delay
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Thread.Sleep(SKIN_PROPERTIES_UPDATE_DELAY);
                invoke(delegate()
                {
                    setMetaData();
                    setCover();
                    setDuration();
                }, false);
            });
        }

        void restartPlayback()
        {
            Logger.Instance.Debug("AirPlayer: Restarting playback");
            AudioBufferStream input = airtunesServer.GetStream(StreamType.Wave);
            Logger.Instance.Debug("AirPlayer: Got audio stream");
            if (input != null)
                startPlayback(input);
        }

        void airtunesServer_PlaybackProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            invoke(delegate()
            {
                //When stopping playback on the client by stopping MP's player we get zeroed timestamps, 
                //if the client wants to restart playback and the connection is still open it just sends new timestamps 
                bool restart = !isAudioBuffering && !isAudioPlaying && e.SessionId == currentAudioSessionId && currentStartStamp == 0 && currentStopStamp == 0;
                currentStartStamp = e.Start;
                currentStopStamp = e.Stop;
                if (restart)
                    restartPlayback();
                else
                    setDuration();
            }, false);
        }

        void setDuration()
        {
            if (isAudioPlaying && currentAudioPlayer != null)
                currentAudioPlayer.UpdateDurationInfo(currentStartStamp, currentStopStamp);
        }

        void airtunesServer_MetaDataChanged(object sender, MetaDataChangedEventArgs e)
        {
            invoke(delegate()
            {
                currentMeta = e.MetaData;
                setMetaData();
            }, false);
        }

        void setMetaData()
        {
            if (isAudioPlaying && currentMeta != null)
            {
                GUIPropertyManager.SetProperty("#Play.Current.Title", currentMeta.Track);
                GUIPropertyManager.SetProperty("#Play.Current.Album", currentMeta.Album);
                GUIPropertyManager.SetProperty("#Play.Current.Artist", currentMeta.Artist);
                GUIPropertyManager.SetProperty("#Play.Current.Genre", currentMeta.Genre);
            }
        }

        void airtunesServer_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            string newCover = saveCover(e.ImageData);
            invoke(delegate()
            {
                //we've previously loaded an image but never displayed it
                if (!string.IsNullOrEmpty(nextCover))
                    GUITextureManager.ReleaseTexture(nextCover);
                nextCover = newCover;
                setCover();
            }, false);
        }

        void setCover()
        {
            if (isAudioPlaying && nextCover != null)
            {
                GUIPropertyManager.SetProperty("#Play.Current.Thumb", nextCover);
                if (!string.IsNullOrEmpty(currentCover))
                    GUITextureManager.ReleaseTexture(currentCover);
                currentCover = nextCover;
                nextCover = null;
            }
        }

        void airtunesServer_VolumeChanged(object sender, ShairportSharp.Raop.VolumeChangedEventArgs e)
        {
            invoke(delegate()
            {
                if (isAudioPlaying)
                {
                    VolumeHandler volumeHandler = VolumeHandler.Instance;
                    if (savedVolume == null)
                        savedVolume = volumeHandler.Volume;

                    if (e.Volume < -30)
                    {
                        volumeHandler.IsMuted = true;
                    }
                    else
                    {
                        double percent = (e.Volume + 30) / 30;
                        volumeHandler.Volume = (int)(volumeHandler.Maximum * percent);
                    }
                }
            }, false);
        }

        void airtunesServer_StreamStopped(object sender, EventArgs e)
        {
            invoke(delegate()
            {
                cleanupAudioPlayback();
                if (isAudioPlaying)
                    stopCurrentItem();
            }, false);
        }

        #endregion

        #region AirPlay Event Handlers

        void airplayServer_ShowPhoto(object sender, PhotoEventArgs e)
        {
            invoke(delegate()
            {
                //When playing a video from the camera roll the client sends a thumbnail before the video.
                //Occasionally we receive it after due to threading so we should ignore it if we have just started playing a video.
                if (!isVideoPlaying || DateTime.Now.Subtract(videoReceiveTime).TotalSeconds > 2)
                {
                    if (photoWindow != null)
                    {
                        photoSessionId = e.SessionId;
                        photoWindow.SetPhoto(e.AssetKey, e.Photo);
                        GUIGraphicsContext.ResetLastActivity();
                        if (GUIWindowManager.ActiveWindow != PhotoWindow.WINDOW_ID)
                            GUIWindowManager.ActivateWindow(PhotoWindow.WINDOW_ID);
                    }
                }
            }, false);
        }

        void airplayServer_VideoReceived(object sender, VideoEventArgs e)
        {
            airplayServer.SetPlaybackState(e.SessionId, PlaybackCategory.Video, PlaybackState.Loading);
            invoke(delegate()
            {
                //YouTube sometimes sends play video twice?? Ignore the second request
                if (e.SessionId == currentVideoSessionId && e.ContentLocation == currentVideoUrl)
                {
                    Logger.Instance.Debug("Airplayer: Ignoring duplicate playback request");
                    return;
                }

                videoReceiveTime = DateTime.Now;
                stopCurrentItem();
                cleanupPlayback();

                GUIGraphicsContext.ResetLastActivity();
                GUIWaitCursor.Init(); GUIWaitCursor.Show();

                currentVideoSessionId = e.SessionId;
                currentVideoUrl = e.ContentLocation;
                //See if we are loading a HLS stream. 
                //If so, manually select the best quality as LAVSplitter just selects the first/default.
                //If not, allow MPUrlSourceFilter as it has better seeking support but doesn't seem to like HLS streams :(
                hlsParser = new HlsParser(currentVideoUrl);
                hlsParser.Completed += hlsParser_Completed;
                hlsParser.Start();
            });
        }

        void hlsParser_Completed(object sender, EventArgs e)
        {
            invoke(delegate()
            {
                if (sender != hlsParser)
                    return;

                //We shouldn't alter currentVideoUrl as this is how we check for duplicate requests
                string finalUrl;
                bool useMPUrlSourceFilter;
                if (hlsParser.IsHls)
                {
                    useMPUrlSourceFilter = false;
                    finalUrl = hlsParser.SelectBestSubStream(allowHDStreams);
                    //Secure HLS stream
                    if (isSecureUrl(finalUrl))
                    {
                        //Lav Splitter does not support SSL so it cannot download the HLS segments
                        //Use reverse proxy to workaround
                        Logger.Instance.Debug("Airplayer: Secure HLS Stream, setting up proxy");
                        proxy = new HlsProxy();
                        proxy.Start();
                        finalUrl = proxy.GetProxyUrl(finalUrl);
                    }
                }
                else
                {
                    finalUrl = currentVideoUrl;
                    //Again, MPUrlSource does not support SSL, FileSource is OK for non HLS streams
                    //Use MPUrlSource if we're not secure and definately not a HLS stream or we can guess filetype by extension
                    useMPUrlSourceFilter = !isSecureUrl(finalUrl) && (hlsParser.Success || isKnownExtension(finalUrl));
                }
                hlsParser = null;
                startVideoLoading(finalUrl, useMPUrlSourceFilter);
            }, false);
        }

        void startVideoLoading(string url, bool useMPSourceFilter = false)
        {
            stopCurrentItem();
            string sourceFilter = useMPSourceFilter ? VideoPlayer.MPURL_SOURCE_FILTER : VideoPlayer.DEFAULT_SOURCE_FILTER;
            Logger.Instance.Info("Airplayer: Starting playback, Url: '{0}', SourceFilter: '{1}'", url, sourceFilter);
            currentVideoPlayer = new VideoPlayer(url, currentVideoSessionId, sourceFilter) { BufferPercent = videoBuffer };
            bool? prepareResult;
            lock (bufferLock)
                prepareResult = currentVideoPlayer.PrepareGraph();
            switch (prepareResult)
            {
                case true:
                    startBuffering(currentVideoPlayer);
                    break;
                case false:
                    startVideoPlayback(currentVideoPlayer, true);
                    break;
                default:
                    startVideoPlayback(currentVideoPlayer, false);
                    break;
            }
        }

        void startBuffering(VideoPlayer player)
        {
            bufferingPlayer = player;
            videoBufferThread = new Thread(delegate()
            {
                lock (bufferLock)
                {
                    bool result = false;
                    string error = null;
                    try
                    {
                        result = player.BufferFile();
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                        result = false;
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        error = ex.Message;
                    }
                    finally
                    {
                        invoke(delegate() { startVideoPlayback(player, result, error); }, false);
                    }
                }
            }) { Name = "AirPlayerBufferThread", IsBackground = true };
            videoBufferThread.Start();
        }

        void startVideoPlayback(VideoPlayer player, bool result, string error = null)
        {
            if (player != currentVideoPlayer)
                return;
            
            GUIWaitCursor.Hide();
            bufferingPlayer = null;
            if (currentVideoPlayer != null)
            {
                if (!result)
                {
                    bool showMessage = !currentVideoPlayer.BufferingStopped;
                    currentVideoPlayer.Dispose();
                    cleanupVideoPlayback();
                    if (showMessage)
                        showDialog("Unable to play video" + (string.IsNullOrEmpty(error) ? "" : " " + error));
                }
                else
                {
                    IPlayerFactory savedFactory = g_Player.Factory;
                    g_Player.Factory = new PlayerFactory(currentVideoPlayer);
                    isVideoPlaying = g_Player.Play(VideoPlayer.DUMMY_URL, g_Player.MediaType.Video);
                    g_Player.Factory = savedFactory;

                    if (isVideoPlaying)
                    {
                        lastVideoUrl = currentVideoPlayer.Url;
                        lastVideoSessionId = currentVideoSessionId;
                        lastUseMPUrlSourceFilter = currentVideoPlayer.SourceFilterName == VideoPlayer.MPURL_SOURCE_FILTER;
                        g_Player.ShowFullScreenWindow();
                    }
                    else
                    {
                        cleanupVideoPlayback();
                    }
                }
            }
        }

        void airplayServer_PlaybackInfoRequested(object sender, PlaybackInfoEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    PlaybackInfo playbackInfo = e.PlaybackInfo;
                    playbackInfo.Duration = currentVideoPlayer.Duration;
                    playbackInfo.Position = currentVideoPlayer.CurrentPosition;
                    playbackInfo.PlaybackLikelyToKeepUp = true;
                    playbackInfo.ReadyToPlay = true;
                    
                    AvailableTimerange availableTimerange = currentVideoPlayer.GetAvailableTimerange();
                    PlaybackTimeRange timeRange = new PlaybackTimeRange();
                    if (availableTimerange != null)
                    {
                        timeRange.Start = availableTimerange.StartTime;
                        timeRange.Duration = availableTimerange.EndTime - availableTimerange.StartTime;
                    }
                    else
                    {
                        timeRange.Duration = currentVideoPlayer.Duration;
                    }
                    playbackInfo.LoadedTimeRanges.Add(timeRange);
                    playbackInfo.SeekableTimeRanges.Add(timeRange);

                    if (currentVideoPlayer.Paused)
                        playbackInfo.Rate = 0;
                    else
                        playbackInfo.Rate = 1;
                }
            });
        }

        void airplayServer_GetPlaybackPosition(object sender, GetPlaybackPositionEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    e.Duration = currentVideoPlayer.Duration;
                    e.Position = currentVideoPlayer.CurrentPosition;
                }
            });
        }

        void airplayServer_PlaybackPositionChanged(object sender, PlaybackPositionEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    if (e.Position >= 0 && e.Position <= currentVideoPlayer.Duration)
                        currentVideoPlayer.SeekAbsolute(e.Position);
                }
                else if (currentVideoUrl == null && e.SessionId == lastVideoSessionId && lastVideoUrl != null)
                {
                    airplayServer.SetPlaybackState(lastVideoSessionId, PlaybackCategory.Video, PlaybackState.Loading);
                    currentVideoSessionId = lastVideoSessionId;
                    currentVideoUrl = lastVideoUrl;
                    startVideoLoading(currentVideoUrl, lastUseMPUrlSourceFilter);
                }
            }, false);
        }

        void airplayServer_PlaybackRateChanged(object sender, PlaybackRateEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    if ((e.Rate > 0 && g_Player.Paused) || (e.Rate == 0 && !g_Player.Paused))
                    {
                        MediaPortal.GUI.Library.Action action = new MediaPortal.GUI.Library.Action();
                        action.wID = g_Player.Paused ? MediaPortal.GUI.Library.Action.ActionType.ACTION_PLAY : MediaPortal.GUI.Library.Action.ActionType.ACTION_PAUSE;
                        GUIGraphicsContext.OnAction(action);
                    }
                }
            }, false);
        }

        void airplayServer_VolumeChanged(object sender, ShairportSharp.Airplay.VolumeChangedEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    VolumeHandler volumeHandler = VolumeHandler.Instance;
                    if (savedVolume == null)
                        savedVolume = volumeHandler.Volume;

                    if (e.Volume == 0)
                    {
                        volumeHandler.Volume = volumeHandler.Minimum;
                    }
                    else if (e.Volume == 1)
                    {
                        volumeHandler.Volume = volumeHandler.Maximum;
                    }
                    else
                    {
                        double factor = volumeHandler.Maximum / 0.9;
                        volumeHandler.Volume = (int)(factor - factor / Math.Pow(10, e.Volume));
                    }
                }
            }, false);
        }

        void airplayServer_SessionStopped(object sender, AirplayEventArgs e)
        {
            invoke(delegate()
            {
                if (e.SessionId == currentVideoSessionId)
                {
                    cleanupVideoPlayback(false);
                    if (isVideoPlaying)
                        stopCurrentItem();
                }
                else if (e.SessionId == photoSessionId)
                {
                    photoSessionId = null;
                    if (GUIWindowManager.ActiveWindow == PhotoWindow.WINDOW_ID)
                        GUIWindowManager.ShowPreviousWindow();
                }
            });
        }

        #endregion

        #region Mirroring Event Handlers

        void MirroringServer_Authenticating(object sender, EventArgs e)
        {
            invoke(() =>
            {
                stopCurrentItem();
                cleanupPlayback();
                GUIWaitCursor.Init();
                GUIWaitCursor.Show();
                isMirroringStarting = true;
            });
        }

        void MirroringServer_Started(object sender, ShairportSharp.Mirroring.MirroringStartedEventArgs e)
        {
            invoke(() =>
            {
                if (!isMirroringStarting)
                    return;

                isMirroringStarting = false;
                GUIWaitCursor.Hide();
                stopCurrentItem();
                currentMirroringPlayer = new MirroringPlayer(e.Stream);
                IPlayerFactory savedFactory = g_Player.Factory;
                g_Player.Factory = new PlayerFactory(currentMirroringPlayer);
                bool isPlaying = g_Player.Play(MirroringPlayer.DUMMY_URL, g_Player.MediaType.Video);
                g_Player.Factory = savedFactory;
                if (isPlaying)
                    g_Player.ShowFullScreenWindow();
            }, false);
        }

        #endregion

        #region Mediaportal Event Handlers

        void GUIWindowManager_OnNewAction(MediaPortal.GUI.Library.Action action)
        {
            switch (action.wID)
            {
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_PLAY:
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_PLAY:
                    if (bufferingPlayer != null)
                        bufferingPlayer.SkipBuffering();
                    else if (isVideoPlaying && currentVideoPlayer != null)
                        airplayServer.SetPlaybackState(currentVideoPlayer.SessionId, PlaybackCategory.Video, PlaybackState.Playing);
                    else if (isAudioPlaying)
                        airtunesServer.SendCommand(RemoteCommand.Play);
                    break;
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_PAUSE:
                    if (isVideoPlaying && currentVideoPlayer != null)
                        airplayServer.SetPlaybackState(currentVideoPlayer.SessionId, PlaybackCategory.Video, PlaybackState.Paused);
                    if (isAudioPlaying)
                        airtunesServer.SendCommand(RemoteCommand.Pause);
                    break;
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_STOP:
                    if (bufferingPlayer != null)
                        bufferingPlayer.StopBuffering();
                    else if (currentMirroringPlayer != null)
                        airplayServer.MirroringServer.StopCurrentSession();
                    break;
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_CHAPTER:
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_ITEM:
                    if (sendCommands && isAudioPlaying)
                        airtunesServer.SendCommand(RemoteCommand.PrevItem);
                    break;
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_CHAPTER:
                case MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_ITEM:
                    if (sendCommands && isAudioPlaying)
                        airtunesServer.SendCommand(RemoteCommand.NextItem);
                    break;
            }
        }

        void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            Logger.Instance.Debug("Airplayer: PlaybackChanged");
            onPlaybackFinished();
        }

        void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
        {
            Logger.Instance.Debug("Airplayer: PlaybackStopped");
            onPlaybackFinished();
        }

        void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            Logger.Instance.Debug("Airplayer: PlaybackEnded");
            onPlaybackFinished();
        }

        void onPlaybackFinished()
        {
            isAudioPlaying = false;
            isVideoPlaying = false;
            if (currentAudioPlayer != null)
            {
                airtunesServer.SendCommand(RemoteCommand.Stop);
                cleanupAudioPlayback();
            }
            if (currentVideoPlayer != null)
            {
                cleanupVideoPlayback();
            }            
        }

        #endregion

        #region Utils

        void showDialog(string message)
        {
            GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
            if (dlg != null)
            {
                dlg.Reset();
                dlg.SetImage(pluginIconPath);
                dlg.SetHeading("Airplay Error");
                dlg.SetText(message);
                dlg.DoModal(GUIWindowManager.ActiveWindow);
            }
        }

        void cleanupPlayback()
        {
            cleanupAudioPlayback();
            cleanupVideoPlayback();
            cleanupMirroringPlayback();
        }

        void cleanupAudioPlayback()
        {
            if (isAudioBuffering)
            {
                GUIWaitCursor.Hide();
                isAudioBuffering = false;
            }
            restoreVolume();
            currentAudioPlayer = null;
        }

        void cleanupVideoPlayback(bool sendStoppedState = true)
        {
            if (videoBufferThread != null && videoBufferThread.IsAlive)
            {
                videoBufferThread.Abort();
                videoBufferThread = null;
            }
            if (bufferingPlayer != null)
            {
                GUIWaitCursor.Hide();
                bufferingPlayer.Dispose();
                bufferingPlayer = null;
            }
            if (hlsParser != null)
            {
                GUIWaitCursor.Hide();
                hlsParser = null;
            }
            if (proxy != null)
            {
                proxy.Stop();
                proxy = null;
            }
            if (sendStoppedState && currentVideoSessionId != null)
            {
                airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, PlaybackState.Stopped);
            }

            restoreVolume();
            currentVideoSessionId = null;
            currentVideoPlayer = null;
            currentVideoUrl = null;
        }

        void cleanupMirroringPlayback()
        {
            if (isMirroringStarting)
            {
                GUIWaitCursor.Hide();
                isMirroringStarting = false;
            }
            currentMirroringPlayer = null;
        }

        void restoreVolume()
        {
            if (savedVolume != null)
            {
                VolumeHandler.Instance.Volume = (int)savedVolume;
                savedVolume = null;
            }
        }

        void invoke(System.Action action, bool wait = true)
        {
            if (wait)
            {
                GUIWindowManager.SendThreadCallbackAndWait((p1, p2, o) =>
                {
                    action();
                    return 0;
                }, 0, 0, null);
            }
            else
            {
                GUIWindowManager.SendThreadCallback((p1, p2, o) =>
                {
                    action();
                    return 0;
                }, 0, 0, null);
            }
        }

        void stopCurrentItem()
        {
            if (g_Player.Playing)
            {
                if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVFULLSCREEN)
                    GUIWindowManager.ShowPreviousWindow();
                g_Player.Stop();
            }
        }

        string saveCover(byte[] buffer)
        {
            if (buffer != null && buffer.Length > 0)
            {
                lock (coverLock)
                {
                    coverNumber = ++coverNumber % 2;
                    string identifier = getImageIdentifier("AirPlay_Thumb_" + coverNumber);
                    if (loadImage(identifier, buffer))
                        return identifier;
                }
            }
            return "";
        }

        static bool loadImage(string identifier, byte[] imageData)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    if (GUITextureManager.LoadFromMemory(System.Drawing.Image.FromStream(ms), identifier, 0, 0, 0) > 0)
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Airplayer: Error loading image to TextureManager -", ex);
            }
            return false;
        }

        static string getImageIdentifier(string name)
        {
            return "[Airplayer:" + name.GetHashCode() + "]";
        }

        static bool isSecureUrl(string url)
        {
            return !string.IsNullOrEmpty(url) && url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase);
        }

        static bool isKnownExtension(string url)
        {
            return url.EndsWith(".mov", StringComparison.InvariantCultureIgnoreCase) || url.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion
    }
}
