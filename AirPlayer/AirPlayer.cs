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

namespace AirPlayer
{
    [MediaPortal.Configuration.PluginIcons("AirPlayer.MPE.airplay-icon.png", "AirPlayer.MPE.airplay-icon-faded.png")]
    public class AirPlayer : IPlugin, ISetupForm
    {
        #region Consts

        const string PLUGIN_NAME = "AirPlayer";
        const string AIRPLAY_DUMMY_FILE = "AirPlayer_Audio_Stream.wav";
        const int SKIN_PROPERTIES_UPDATE_DELAY = 2000;

        #endregion

        #region Variables

        string pluginIconPath;

        RaopServer airtunesServer;
        AirplayServer airplayServer;

        PhotoWindow photoWindow;
        Dictionary<string, string> photoCache = new Dictionary<string, string>();
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

        AudioPlayer currentAudioPlayer;
        bool isAudioBuffering;
        DmapData currentMeta;
        string currentCover;
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
            return true;
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
            ShairportSharp.Logger.SetLogger(Logger.Instance);
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
            airplayServer.PhotoReceived += airplayServer_PhotoReceived;
            airplayServer.VideoReceived += airplayServer_VideoReceived;
            airplayServer.PlaybackInfoRequested += airplayServer_PlaybackInfoRequested;
            airplayServer.GetPlaybackPosition += airplayServer_GetPlaybackPosition;
            airplayServer.PlaybackPositionChanged += airplayServer_PlaybackPositionChanged;
            airplayServer.PlaybackRateChanged += airplayServer_PlaybackRateChanged;
            if (allowVolumeControl)
                airplayServer.VolumeChanged += airplayServer_VolumeChanged;
            airplayServer.SessionStopped += airplayServer_SessionStopped;
            airplayServer.SessionClosed += airplayServer_SessionClosed;
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
        
        void airtunesServer_StreamStarting(object sender, EventArgs e)
        {
            invoke(delegate()
            {
                stopCurrentItem();
                cleanupPendingPlayback();
                GUIWaitCursor.Init(); GUIWaitCursor.Show();
                isAudioBuffering = true;
            });

        }

        void airtunesServer_StreamReady(object sender, EventArgs e)
        {
            AudioBufferStream input = airtunesServer.GetStream(StreamType.Wave);
            if (input == null)
                return;

            invoke(delegate()
            {
                startPlayback(input);
            }, false);
        }

        void startPlayback(AudioBufferStream stream)
        {
            if (!isAudioBuffering)
            {
                //airtunesServer.StopCurrentSession();
                airtunesServer.SendCommand(RemoteCommand.Pause);
                return;
            }
            isAudioBuffering = false;
            GUIWaitCursor.Hide();
            stopCurrentItem();
            isAudioPlaying = true;
            IPlayerFactory savedFactory = g_Player.Factory;
            currentAudioPlayer = new AudioPlayer(new PlayerSettings(stream));
            g_Player.Factory = new PlayerFactory(currentAudioPlayer);
            g_Player.Play(AIRPLAY_DUMMY_FILE, g_Player.MediaType.Music);
            g_Player.Factory = savedFactory;

            //Mediaportal sets the metadata skin properties internally, we overwrite them after a small delay
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Thread.Sleep(SKIN_PROPERTIES_UPDATE_DELAY);
                invoke(delegate()
                {
                    setMetaData(currentMeta);
                    setCover(currentCover);
                    setDuration();
                }, false);
            });
        }

        void airtunesServer_PlaybackProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            invoke(delegate()
            {
                currentStartStamp = e.Start;
                currentStopStamp = e.Stop;
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
                setMetaData(currentMeta);
            }, false);
        }

        void setMetaData(DmapData metaData)
        {
            if (isAudioPlaying && metaData != null)
            {
                GUIPropertyManager.SetProperty("#Play.Current.Title", metaData.Track);
                GUIPropertyManager.SetProperty("#Play.Current.Album", metaData.Album);
                GUIPropertyManager.SetProperty("#Play.Current.Artist", metaData.Artist);
                GUIPropertyManager.SetProperty("#Play.Current.Genre", metaData.Genre);
            }
        }

        void airtunesServer_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            string newCover = saveCover(e.ImageData, e.ContentType);
            invoke(delegate()
            {
                currentCover = newCover;
                if (currentCover != null)
                    setCover(currentCover);
            }, false);
        }

        void setCover(string cover)
        {
            if (isAudioPlaying && !string.IsNullOrEmpty(cover))
                GUIPropertyManager.SetProperty("#Play.Current.Thumb", cover);
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
                cleanupPendingAudio();
                if (isAudioPlaying)
                    stopCurrentItem();
            }, false);
        }

        #endregion

        #region AirPlay Event Handlers
        
        void airplayServer_PhotoReceived(object sender, PhotoReceivedEventArgs e)
        {
            DateTime photoReceiveTime = DateTime.Now;
            string photoPath;
            if (e.AssetAction == PhotoAction.DisplayCached)
            {
                lock (photoCache)
                    if (!photoCache.TryGetValue(e.AssetKey, out photoPath))
                        return;
            }
            else
            {
                lock (photoCache)
                    if (!photoCache.TryGetValue(e.AssetKey, out photoPath))
                    {
                        photoPath = saveFileToTemp(e.AssetKey, ".jpg", e.Photo);
                        if (photoPath != null)
                            photoCache[e.AssetKey] = photoPath;
                    }
                if (photoPath == null || e.AssetAction == PhotoAction.CacheOnly)
                    return;
            }

            invoke(delegate()
            {
                //When playing a video from the camera roll the client sends a thumbnail before the video.
                //Occasionally we receive it after due to threading so we should ignore it if we have just started playing a video.
                if (!isVideoPlaying || photoReceiveTime.Subtract(videoReceiveTime).TotalSeconds > 2)
                {
                    if (photoWindow != null)
                    {
                        photoSessionId = e.SessionId;
                        photoWindow.SetPhoto(photoPath);
                        if (GUIWindowManager.ActiveWindow != PhotoWindow.WINDOW_ID)
                            GUIWindowManager.ActivateWindow(PhotoWindow.WINDOW_ID);
                    }
                }
            });
        }

        void airplayServer_VideoReceived(object sender, VideoEventArgs e)
        {
            airplayServer.SetPlaybackState(e.SessionId, PlaybackCategory.Video, PlaybackState.Loading);
            invoke(delegate()
            {
                //YouTube sometimes sends play video twice?? Ignore but make sure we resend playing state if necessary
                if (e.SessionId == currentVideoSessionId && e.ContentLocation == currentVideoUrl)
                {
                    if (isVideoPlaying)
                        airplayServer.SetPlaybackState(e.SessionId, PlaybackCategory.Video, PlaybackState.Playing);
                    Logger.Instance.Debug("Airplayer: Ignoring duplicate playback request");
                    return;
                }

                videoReceiveTime = DateTime.Now;
                stopCurrentItem();
                cleanupPendingPlayback();
                GUIWaitCursor.Init(); GUIWaitCursor.Show();

                currentVideoSessionId = e.SessionId;
                currentVideoUrl = e.ContentLocation;
                //See if we are loading a HLS stream. 
                //If so, manually select the best quality as LAVSplitter just selects the first/default.
                //If not, allow allow MPUrlSourceFilter as it has better seeking support but doesn't seem to like HLS streams :(
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

                string selectedUrl;
                bool useMPUrlSourceFilter;

                if (hlsParser.StreamInfos.Count > 0)
                {
                    //HLS sub-streams, select best quality
                    HlsStreamInfo streamInfo;
                    if (!allowHDStreams)
                    {
                        streamInfo = hlsParser.StreamInfos.LastOrDefault(si => si.Height < 720);
                        if (streamInfo == null)
                            streamInfo = hlsParser.StreamInfos.First();
                    }
                    else
                    {
                        streamInfo = hlsParser.StreamInfos.Last();
                    }

                    Logger.Instance.Debug("Airplayer: Selected hls stream '{0}x{1}'", streamInfo.Width, streamInfo.Height);
                    selectedUrl = streamInfo.Url;
                    useMPUrlSourceFilter = false;
                }
                else
                {
                    //Failed or non HLS stream or no sub-streams
                    selectedUrl = currentVideoUrl;
                    if (hlsParser.IsHls || selectedUrl.StartsWith("https", StringComparison.InvariantCultureIgnoreCase))
                    {
                        useMPUrlSourceFilter = false;
                    }
                    else
                    {
                        //see if we can determine type by extension
                        useMPUrlSourceFilter = hlsParser.Success || (hlsParser.Url.EndsWith(".mov", StringComparison.InvariantCultureIgnoreCase) || hlsParser.Url.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase));
                    }
                }
                hlsParser = null;

                lastVideoUrl = selectedUrl;
                lastVideoSessionId = currentVideoSessionId;
                lastUseMPUrlSourceFilter = useMPUrlSourceFilter;
                startVideoLoading(selectedUrl, currentVideoSessionId, useMPUrlSourceFilter);
            }, false);
        }

        void startVideoLoading(string url, string sessionId, bool useMPSourceFilter = false)
        {
            stopCurrentItem();           
            string sourceFilter = useMPSourceFilter ? VideoPlayer.MPURL_SOURCE_FILTER : VideoPlayer.DEFAULT_SOURCE_FILTER;
            currentVideoPlayer = new VideoPlayer(url, sessionId, sourceFilter) { BufferPercent = videoBuffer };
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
                        invoke(delegate()
                        {
                            startVideoPlayback(player, result, error);
                        }, false);
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
                    currentVideoPlayer = null;
                    isVideoPlaying = false;
                    if (showMessage)
                    {
                        GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                        if (dlg != null)
                        {
                            dlg.Reset();
                            dlg.SetImage(pluginIconPath);
                            dlg.SetHeading("Airplay Error");
                            dlg.SetText("Unable to play video" + (string.IsNullOrEmpty(error) ? "" : " " + error));
                            dlg.DoModal(GUIWindowManager.ActiveWindow);
                        }
                    }
                    return;
                }

                isVideoPlaying = true;
                //airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, PlaybackState.Playing);
                IPlayerFactory savedFactory = g_Player.Factory;
                g_Player.Factory = new PlayerFactory(currentVideoPlayer);
                g_Player.Play(VideoPlayer.DUMMY_URL, g_Player.MediaType.Video);
                g_Player.Factory = savedFactory;
            }
        }

        void airplayServer_PlaybackInfoRequested(object sender, PlaybackInfoEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    PlaybackInfo playbackInfo = e.PlaybackInfo;
                    playbackInfo.Duration = currentVideoPlayer.FloatDuration;
                    playbackInfo.Position = currentVideoPlayer.FloatPosition;
                    playbackInfo.Rate = currentVideoPlayer.Paused ? 0 : 1;
                    playbackInfo.PlaybackLikelyToKeepUp = true;
                    playbackInfo.ReadyToPlay = true;
                    PlaybackTimeRange timeRange = new PlaybackTimeRange();
                    timeRange.Duration = playbackInfo.Duration;
                    playbackInfo.LoadedTimeRanges.Add(timeRange);
                    playbackInfo.SeekableTimeRanges.Add(timeRange);
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
                    currentVideoSessionId = lastVideoSessionId;
                    currentVideoUrl = lastVideoUrl;
                    airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, PlaybackState.Loading);
                    startVideoLoading(lastVideoUrl, currentVideoSessionId, lastUseMPUrlSourceFilter);
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
                    cleanupPendingVideo();
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

        void airplayServer_SessionClosed(object sender, AirplayEventArgs e)
        {
            
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
                    //else if (sendCommands && isAudioPlaying)
                    //    airtunesServer.SendCommand(RemoteCommand.Pause);
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
                airtunesServer.SendCommand(RemoteCommand.Pause);
                cleanupPendingAudio();
                //airtunesServer.StopCurrentSession();
            }
            if (currentVideoPlayer != null)
            {
                airplayServer.SetPlaybackState(currentVideoPlayer.SessionId, PlaybackCategory.Video, PlaybackState.Stopped);
                cleanupPendingVideo();
            }
        }

        #endregion

        #region Utils
        
        void cleanupPendingPlayback()
        {
            cleanupPendingAudio();
            cleanupPendingVideo();
        }

        void cleanupPendingAudio()
        {
            if (isAudioBuffering)
            {
                GUIWaitCursor.Hide();
                isAudioBuffering = false;
            }
            restoreVolume();
            currentAudioPlayer = null;
        }

        void cleanupPendingVideo()
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
            else if (hlsParser != null)
            {
                GUIWaitCursor.Hide();
                hlsParser = null;
            }
            restoreVolume();
            currentVideoPlayer = null;
            currentVideoSessionId = null;
            currentVideoUrl = null;
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

        string saveCover(byte[] buffer, string contentType)
        {
            lock (coverLock)
            {
                coverNumber++;
                coverNumber = coverNumber % 3;
                string filename = "AirPlay_Thumb_" + coverNumber;
                string extension = "." + contentType.Replace("image/", "");
                if (extension == ".jpeg")
                    extension = ".jpg";

                return saveFileToTemp(filename, extension, buffer);
            }
        }

        static string saveFileToTemp(string filename, string extension, byte[] buffer)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), string.Format("{0}{1}", filename, extension));
                Logger.Instance.Debug("Saving file to '{0}'", path);
                using (FileStream fs = File.Create(path))
                    fs.Write(buffer, 0, buffer.Length);
                return path;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Failed to save file - {0}", ex.Message);
            }
            return null;
        }

        #endregion
    }
}
