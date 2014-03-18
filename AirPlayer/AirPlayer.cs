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

        ShairportServer server = null;
        AirplayServer airplayServer = null;
        object streamLock = new object();

        VideoPlayer currentVideoPlayer;
        AudioPlayer currentAudioPlayer;
        DmapData currentMeta = null;
        string currentCover = null;
        uint currentStartStamp;
        uint currentStopStamp;
        
        int coverNumber = 0;
        object coverLock = new object();

        bool allowVolumeControl;
        bool sendCommands;

        bool isAudioPlaying = false;
        bool isVideoPlaying = false;

        Dictionary<string, string> photoCache = new Dictionary<string, string>();

        #endregion

        #region IPlugin Members

        public void Start()
        {
            GUIWindow window = new PhotoWindow();
            window.Init();
            GUIWindowManager.Add(ref window);
            ShairportServer.SetLogger(Logger.Instance);
            PluginSettings settings = new PluginSettings();
            allowVolumeControl = settings.AllowVolume;
            sendCommands = settings.SendCommands;

            server = new ShairportServer(settings.ServerName, settings.Password);
            server.Port = settings.RtspPort;
            server.AudioPort = settings.UdpPort;
            server.AudioBufferSize = (int)(settings.BufferSize * 1000);
            server.StreamStopped += server_StreamStopped;
            server.StreamReady += server_StreamReady;
            server.PlaybackProgressChanged += server_PlaybackProgressChanged;
            server.MetaDataChanged += server_MetaDataChanged;
            server.ArtworkChanged += server_ArtworkChanged;
            if (allowVolumeControl)
                server.VolumeChanged += server_VolumeChanged;
            server.Start();

            airplayServer = new AirplayServer(settings.ServerName);
            airplayServer.PhotoReceived += airplayServer_PhotoReceived;
            airplayServer.VideoReceived += airplayServer_VideoReceived;
            airplayServer.PlaybackInfoRequested += airplayServer_PlaybackInfoRequested;
            airplayServer.GetPlaybackPosition += airplayServer_GetPlaybackPosition;
            airplayServer.PlaybackPositionChanged += airplayServer_PlaybackPositionChanged;
            airplayServer.PlaybackRateChanged += airplayServer_PlaybackRateChanged;
            airplayServer.SessionClosed += airplayServer_SessionClosed;
            airplayServer.Start();

            g_Player.PlayBackChanged += g_Player_PlayBackChanged;
            g_Player.PlayBackStopped += g_Player_PlayBackChanged;
            GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;
        }
        
        public void Stop()
        {
            if (server != null)
                server.Stop();
            if (airplayServer != null)
                airplayServer.Stop();
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

        #region Shairplay Event Handlers

        void server_StreamStopped(object sender, EventArgs e)
        {
            invoke(delegate()
            {
                lock (streamLock)
                {
                    if (isAudioPlaying)
                    {
                        isAudioPlaying = false;
                        currentAudioPlayer = null;
                        stopCurrentItem();
                    }
                }
            }, false);
        }

        void server_StreamReady(object sender, EventArgs e)
        {
            AudioBufferStream input = server.GetStream(StreamType.Wave);
            if (input == null)
                return;

            invoke(delegate() { startPlayback(input); }, false);
        }

        void startPlayback(AudioBufferStream stream)
        {
            lock (streamLock)
            {
                stopCurrentItem();
                IPlayerFactory savedFactory = g_Player.Factory;
                currentAudioPlayer = new AudioPlayer(new PlayerSettings(sendCommands ? server : null, stream));
                g_Player.Factory = new PlayerFactory(currentAudioPlayer);
                g_Player.Play(AIRPLAY_DUMMY_FILE, g_Player.MediaType.Music);
                g_Player.Factory = savedFactory;
                isAudioPlaying = true;
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                Thread.Sleep(SKIN_PROPERTIES_UPDATE_DELAY);
                lock (streamLock)
                {
                    setMetaData(currentMeta);
                    setCover(currentCover);
                    setDuration();
                }
            });
        }

        void server_PlaybackProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            lock (streamLock)
            {
                currentStartStamp = e.Start;
                currentStopStamp = e.Stop;
                setDuration();
            }
        }

        void setDuration()
        {
            if (isAudioPlaying && currentAudioPlayer != null)
                currentAudioPlayer.UpdateDurationInfo(currentStartStamp, currentStopStamp);
        }

        void server_MetaDataChanged(object sender, MetaDataChangedEventArgs e)
        {
            lock (streamLock)
            {
                currentMeta = e.MetaData;
                setMetaData(currentMeta);
            }
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

        void server_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            string newCover = saveCover(e.ImageData, e.ContentType);
            lock (streamLock)
            {
                currentCover = newCover;
                if (currentCover != null)
                    setCover(currentCover);
            }
        }

        void setCover(string cover)
        {
            if (isAudioPlaying && !string.IsNullOrEmpty(cover))
                GUIPropertyManager.SetProperty("#Play.Current.Thumb", cover);
        }

        void server_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            lock (streamLock)
            {
                if (isAudioPlaying)
                {
                    VolumeHandler volumeHandler = VolumeHandler.Instance;
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
            }
        }

        #endregion

        #region AirPlay Event Handlers
        
        void airplayServer_PhotoReceived(object sender, PhotoReceivedEventArgs e)
        {
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
                PhotoWindow photoWindow = GUIWindowManager.GetWindow(PhotoWindow.WINDOW_ID) as PhotoWindow;
                if (photoWindow != null)
                {
                    photoWindow.SetPhoto(photoPath);
                    if (GUIWindowManager.ActiveWindow != PhotoWindow.WINDOW_ID)
                        GUIWindowManager.ActivateWindow(PhotoWindow.WINDOW_ID);
                }
            }, false);
        }

        void airplayServer_VideoReceived(object sender, VideoEventArgs e)
        {
            airplayServer.SetPlaybackState(e.SessionId, PlaybackState.Loading);
            invoke(delegate()
            {
                startVideoLoading(e.ContentLocation, e.SessionId);
            }, false);
        }

        void startVideoLoading(string url, string sessionId)
        {
            GUIWaitCursor.Init(); GUIWaitCursor.Show();
            lock (streamLock)
            {
                stopCurrentItem();
                if (currentVideoPlayer != null)
                    currentVideoPlayer.Dispose();
                currentVideoPlayer = new VideoPlayer(url, sessionId);
                bool? prepareResult = currentVideoPlayer.PrepareGraph();
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
        }

        Thread videoLoadingThread = null;
        object bufferLock = new object();
        void startBuffering(VideoPlayer player)
        {
            if (videoLoadingThread != null && videoLoadingThread.IsAlive)
                videoLoadingThread.Abort();

            videoLoadingThread = new Thread(delegate()
            {
                lock (bufferLock)
                {
                    bool result = false;
                    try
                    {
                        result = player.BufferFile();
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                        result = false;
                    }
                    catch (Exception)
                    {
                        result = false;
                    }
                    finally
                    {
                        invoke(delegate() { lock (streamLock)startVideoPlayback(player, result); }, false);
                    }
                }
            }) { Name = "AirPlayerBufferThread", IsBackground = true };
            videoLoadingThread.Start();
        }

        void startVideoPlayback(VideoPlayer player, bool result)
        {
            GUIWaitCursor.Hide();
            if (player != currentVideoPlayer)
                return;

            if (currentVideoPlayer != null)
            {
                if (!result)
                {
                    currentVideoPlayer.Dispose();
                    currentVideoPlayer = null;
                    isVideoPlaying = false;
                    return;
                }

                IPlayerFactory savedFactory = g_Player.Factory;
                g_Player.Factory = new PlayerFactory(currentVideoPlayer);
                g_Player.Play(VideoPlayer.DUMMY_URL, g_Player.MediaType.Video);
                g_Player.Factory = savedFactory;
                isVideoPlaying = true;
            }
        }

        void airplayServer_PlaybackInfoRequested(object sender, PlaybackInfoEventArgs e)
        {
            invoke(delegate()
            {
                lock (streamLock)
                {
                    if (isVideoPlaying && currentVideoPlayer.Duration > 0)
                    {
                        PlaybackInfo playbackInfo = e.PlaybackInfo;
                        playbackInfo.ReadyToPlay = true;
                        playbackInfo.Duration = currentVideoPlayer.Duration;
                        playbackInfo.Position = currentVideoPlayer.CurrentPosition;
                        playbackInfo.PlaybackBufferEmpty = false;
                        playbackInfo.PlaybackBufferFull = true;
                        playbackInfo.PlaybackLikelyToKeepUp = true;

                        PlaybackTimeRange timeRange = new PlaybackTimeRange() { Duration = playbackInfo.Duration };
                        playbackInfo.LoadedTimeRanges.Add(timeRange);
                        playbackInfo.SeekableTimeRanges.Add(timeRange);
                        playbackInfo.Rate = currentVideoPlayer.Paused ? 0 : 1;
                    }
                }
            });
        }

        void airplayServer_GetPlaybackPosition(object sender, GetPlaybackPositionEventArgs e)
        {
            invoke(delegate()
            {
                lock (streamLock)
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
                lock (streamLock)
                {
                    if (isVideoPlaying && e.Position >= 0 && e.Position <= currentVideoPlayer.Duration)
                    {
                        currentVideoPlayer.SeekAbsolute(e.Position);
                    }
                }
            });
        }

        void airplayServer_PlaybackRateChanged(object sender, PlaybackRateEventArgs e)
        {
            invoke(delegate()
            {
                lock (streamLock)
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
                }
            });
        }

        void airplayServer_SessionClosed(object sender, AirplayEventArgs e)
        {
            invoke(delegate()
            {
                lock (streamLock)
                {
                    isVideoPlaying = false;
                    currentVideoPlayer = null;
                    stopCurrentItem();
                }
            }, false);
        }

        #endregion

        #region Mediaportal Event Handlers

        void GUIWindowManager_OnNewAction(MediaPortal.GUI.Library.Action action)
        {
            if (sendCommands && isAudioPlaying)
            {
                switch (action.wID)
                {
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_PLAY:
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_PLAY:
                        lock (streamLock)
                            if (isAudioPlaying)
                                server.SendCommand(RemoteCommand.Play);
                        break;
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_PAUSE:
                        lock(streamLock)
                            if(isAudioPlaying)
                                server.SendCommand(RemoteCommand.Pause);
                        break;
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_STOP:
                        lock (streamLock)
                            if (isAudioPlaying)
                                server.SendCommand(RemoteCommand.Stop);
                        break;
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_CHAPTER:
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_ITEM:
                        lock (streamLock)
                            if (isAudioPlaying)
                                server.SendCommand(RemoteCommand.PrevItem);
                        break;
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_CHAPTER:
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_ITEM:
                        lock (streamLock)
                            if (isAudioPlaying)
                                server.SendCommand(RemoteCommand.NextItem);
                        break;
                }
            }
        }

        void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            lock (streamLock)
            {
                if (currentAudioPlayer != null)
                {
                    isAudioPlaying = false;
                    currentAudioPlayer = null;
                    server.StopCurrentSession();
                }
                if (currentVideoPlayer != null)
                {
                    airplayServer.SetPlaybackState(currentVideoPlayer.SessionId, PlaybackState.Stopped);
                    isVideoPlaying = false;
                    currentVideoPlayer = null;
                }
            }
        }

        #endregion

        #region Utils

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
                g_Player.Stop();
                GUIGraphicsContext.ResetLastActivity();
            }
        }

        string saveCover(byte[] buffer, string contentType)
        {
            lock (coverLock)
            {
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
