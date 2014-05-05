using AirPlayer.Common.Hls;
using AirPlayer.Common.Proxy;
using AirPlayer.MediaPortal2.Configuration;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Threading;
using MediaPortal.UI.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using ShairportSharp.Airplay;
using ShairportSharp.Audio;
using ShairportSharp.Helpers;
using ShairportSharp.Raop;
using ShairportSharp.Remote;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AirPlayer.MediaPortal2
{
    public class AirPlayer : IPluginStateTracker
    {
        #region Variables

        string pluginIconPath;

        AsynchronousMessageQueue _messageQueue;

        PluginSettings settings;
        RaopServer airtunesServer;
        AirplayServer airplayServer;
        HlsProxy proxy;

        Dictionary<string, string> photoCache = new Dictionary<string, string>();
        string photoSessionId;

        //VideoPlayer currentVideoPlayer;
        //VideoPlayer bufferingPlayer;
        Thread videoBufferThread;
        object bufferLock = new object();
        HlsParser hlsParser;
        string currentVideoSessionId;
        string currentVideoUrl;
        string lastVideoSessionId;
        string lastVideoUrl;
        bool lastUseMPUrlSourceFilter;
        DateTime videoReceiveTime = DateTime.MinValue;

        //AudioPlayer currentAudioPlayer;
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

        #region Message handling

        protected void SubscribeToMessages()
        {
            _messageQueue = new AsynchronousMessageQueue(this, new[] { PlayerManagerMessaging.CHANNEL });
            _messageQueue.MessageReceived += OnMessageReceived;
            _messageQueue.Start();
        }

        protected virtual void UnsubscribeFromMessages()
        {
            if (_messageQueue == null)
                return;
            _messageQueue.Shutdown();
            _messageQueue = null;
        }

        void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
        {
            if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
            {
                HandlePlayerMessage((PlayerManagerMessaging.MessageType)message.MessageType);
            }
        }

        private void HandlePlayerMessage(PlayerManagerMessaging.MessageType message)
        {
            switch (message)
            {
                case PlayerManagerMessaging.MessageType.PlayerEnded:
                case PlayerManagerMessaging.MessageType.PlayerStopped:
                case PlayerManagerMessaging.MessageType.PlayerError:
                    isVideoPlaying = false;
                    if (currentVideoSessionId != null)
                        airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, ShairportSharp.Airplay.PlaybackState.Stopped);
                    break;
            }
        }

        #endregion

        public void Activated(PluginRuntime pluginRuntime)
        {
            //pluginIconPath = MediaPortal.Configuration.Config.GetFile(MediaPortal.Configuration.Config.Dir.Thumbs, "AirPlayer", "airplay-icon.png");
            //GUIWindow window = new PhotoWindow();
            //window.Init();
            //GUIWindowManager.Add(ref window);
            //photoWindow = (PhotoWindow)window;

            ShairportSharp.Logger.SetLogger(Logger.Instance);
            if (settings == null)
                settings = PluginSettings.Load();

            allowVolumeControl = settings.AllowVolume;
            sendCommands = settings.SendAudioCommands;
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

            SubscribeToMessages();
        }

        public void Continue()
        {
            
        }

        public bool RequestEnd()
        {
            return true;
        }

        public void Shutdown()
        {
            
        }

        public void Stop()
        {
            if (airtunesServer != null)
                airtunesServer.Stop();
            if (airplayServer != null)
                airplayServer.Stop();
            UnsubscribeFromMessages();
        }


        #region Airtunes Event Handlers

        void airtunesServer_StreamStarting(object sender, EventArgs e)
        {
            invoke(delegate()
            {
                stopCurrentItem();
                cleanupPlayback();
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
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
            //if (!isAudioBuffering)
            //{
            //    //airtunesServer.StopCurrentSession();
            //    airtunesServer.SendCommand(RemoteCommand.Pause);
            //    return;
            //}
            //isAudioBuffering = false;
            //ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
            //stopCurrentItem();
            //IPlayerFactory savedFactory = g_Player.Factory;
            //currentAudioPlayer = new AudioPlayer(new PlayerSettings(stream));
            //g_Player.Factory = new PlayerFactory(currentAudioPlayer);
            //g_Player.Play(AudioPlayer.AIRPLAY_DUMMY_FILE, g_Player.MediaType.Music);
            //g_Player.Factory = savedFactory;
            //isAudioPlaying = true;

            //Mediaportal sets the metadata skin properties internally, we overwrite them after a small delay
            //ThreadPool.QueueUserWorkItem((o) =>
            //{
            //    Thread.Sleep(SKIN_PROPERTIES_UPDATE_DELAY);
            //    invoke(delegate()
            //    {
            //        setMetaData(currentMeta);
            //        setCover(currentCover);
            //        setDuration();
            //    }, false);
            //});
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
            //if (isAudioPlaying && currentAudioPlayer != null)
            //    currentAudioPlayer.UpdateDurationInfo(currentStartStamp, currentStopStamp);
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
            //if (isAudioPlaying && metaData != null)
            //{
            //    GUIPropertyManager.SetProperty("#Play.Current.Title", metaData.Track);
            //    GUIPropertyManager.SetProperty("#Play.Current.Album", metaData.Album);
            //    GUIPropertyManager.SetProperty("#Play.Current.Artist", metaData.Artist);
            //    GUIPropertyManager.SetProperty("#Play.Current.Genre", metaData.Genre);
            //}
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
            //if (isAudioPlaying && !string.IsNullOrEmpty(cover))
            //    GUIPropertyManager.SetProperty("#Play.Current.Thumb", cover);
        }

        void airtunesServer_VolumeChanged(object sender, ShairportSharp.Raop.VolumeChangedEventArgs e)
        {
            //invoke(delegate()
            //{
            //    if (isAudioPlaying)
            //    {
            //        VolumeHandler volumeHandler = VolumeHandler.Instance;
            //        if (savedVolume == null)
            //            savedVolume = volumeHandler.Volume;

            //        if (e.Volume < -30)
            //        {
            //            volumeHandler.IsMuted = true;
            //        }
            //        else
            //        {
            //            double percent = (e.Volume + 30) / 30;
            //            volumeHandler.Volume = (int)(volumeHandler.Maximum * percent);
            //        }
            //    }
            //}, false);
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

            //invoke(delegate()
            //{
            //    //When playing a video from the camera roll the client sends a thumbnail before the video.
            //    //Occasionally we receive it after due to threading so we should ignore it if we have just started playing a video.
            //    if (!isVideoPlaying || photoReceiveTime.Subtract(videoReceiveTime).TotalSeconds > 2)
            //    {
            //        if (photoWindow != null)
            //        {
            //            photoSessionId = e.SessionId;
            //            photoWindow.SetPhoto(photoPath);
            //            if (GUIWindowManager.ActiveWindow != PhotoWindow.WINDOW_ID)
            //                GUIWindowManager.ActivateWindow(PhotoWindow.WINDOW_ID);
            //        }
            //    }
            //});
        }

        void airplayServer_VideoReceived(object sender, VideoEventArgs e)
        {
            airplayServer.SetPlaybackState(e.SessionId, PlaybackCategory.Video, ShairportSharp.Airplay.PlaybackState.Loading);
            invoke(delegate()
            {
                //YouTube sometimes sends play video twice?? Ignore but make sure we resend playing state if necessary
                if (e.SessionId == currentVideoSessionId && e.ContentLocation == currentVideoUrl)
                {
                    Logger.Instance.Debug("Airplayer: Ignoring duplicate playback request");
                    return;
                }

                videoReceiveTime = DateTime.Now;
                stopCurrentItem();
                cleanupPlayback();
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();

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
            ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
            isVideoPlaying = true;
            MediaPortal.UiComponents.Media.Models.PlayItemsModel.CheckQueryPlayAction(new VideoItem(url));
        }

        void airplayServer_PlaybackInfoRequested(object sender, PlaybackInfoEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    AirplayVideoPlayer currentVideoPlayer = getCurrentVideoPlayer();
                    if (currentVideoPlayer != null)
                    {
                        PlaybackInfo playbackInfo = e.PlaybackInfo;
                        playbackInfo.Duration = currentVideoPlayer.Duration.TotalSeconds;
                        playbackInfo.Position = currentVideoPlayer.CurrentTime.TotalSeconds;
                        playbackInfo.PlaybackLikelyToKeepUp = true;
                        playbackInfo.ReadyToPlay = true;
                        PlaybackTimeRange timeRange = new PlaybackTimeRange();
                        timeRange.Duration = playbackInfo.Duration;
                        playbackInfo.LoadedTimeRanges.Add(timeRange);
                        playbackInfo.SeekableTimeRanges.Add(timeRange);
                        if (currentVideoPlayer.IsPaused)
                        {
                            playbackInfo.Rate = 0;
                        }
                        else
                        {
                            playbackInfo.Rate = 1;
                        }
                    }
                }
            });
        }

        void airplayServer_GetPlaybackPosition(object sender, GetPlaybackPositionEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    AirplayVideoPlayer currentVideoPlayer = getCurrentVideoPlayer();
                    if (currentVideoPlayer != null)
                    {
                        e.Duration = currentVideoPlayer.Duration.TotalSeconds;
                        e.Position = currentVideoPlayer.CurrentTime.TotalSeconds;
                    }
                }
            });
        }

        void airplayServer_PlaybackPositionChanged(object sender, PlaybackPositionEventArgs e)
        {
            invoke(delegate()
            {
                if (isVideoPlaying)
                {
                    AirplayVideoPlayer currentVideoPlayer = getCurrentVideoPlayer();
                    if (currentVideoPlayer != null)
                    {
                        if (e.Position >= 0 && e.Position <= currentVideoPlayer.Duration.TotalSeconds)
                            currentVideoPlayer.CurrentTime = TimeSpan.FromSeconds(e.Position);
                    }
                }
                else if (currentVideoUrl == null && e.SessionId == lastVideoSessionId && lastVideoUrl != null)
                {
                    airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, ShairportSharp.Airplay.PlaybackState.Loading);
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
                    AirplayVideoPlayer currentVideoPlayer = getCurrentVideoPlayer();
                    if (currentVideoPlayer != null)
                    {
                        if (e.Rate > 0)
                        {
                            if (currentVideoPlayer.IsPaused)
                                currentVideoPlayer.Resume();
                        }
                        else if (e.Rate == 0)
                        {
                            if (!currentVideoPlayer.IsPaused)
                                currentVideoPlayer.Pause();
                        }
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
                    AirplayVideoPlayer currentVideoPlayer = getCurrentVideoPlayer();
                    if (currentVideoPlayer != null)
                    {
                        if (savedVolume == null)
                            savedVolume = currentVideoPlayer.Volume;

                        if (e.Volume == 0)
                        {
                            currentVideoPlayer.Volume = 0;
                        }
                        else if (e.Volume == 1)
                        {
                            currentVideoPlayer.Volume = 100;
                        }
                        else
                        {
                            double factor = 100 / 0.9;
                            currentVideoPlayer.Volume = (int)(factor - factor / Math.Pow(10, e.Volume));
                        }
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
                    //if (GUIWindowManager.ActiveWindow == PhotoWindow.WINDOW_ID)
                    //    GUIWindowManager.ShowPreviousWindow();
                }
            });
        }

        void airplayServer_SessionClosed(object sender, AirplayEventArgs e)
        {

        }

        IPlayerContext getCurrentVideoContext()
        {
            var videoContexts = ServiceRegistration.Get<IPlayerContextManager>().GetPlayerContextsByAVType(AVType.Video);
            return videoContexts.FirstOrDefault(vc => vc.CurrentPlayer is AirplayVideoPlayer);
            
        }

        AirplayVideoPlayer getCurrentVideoPlayer()
        {
            var airplayPlayerCtx = getCurrentVideoContext();
            if (airplayPlayerCtx != null)
                return (AirplayVideoPlayer)airplayPlayerCtx.CurrentPlayer;
            return null;
        }

        #endregion

        #region Utils

        void cleanupPlayback()
        {
            cleanupAudioPlayback();
            cleanupVideoPlayback();
        }

        void cleanupAudioPlayback()
        {
            if (isAudioBuffering)
            {
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                isAudioBuffering = false;
            }
            //restoreVolume();
            //currentAudioPlayer = null;
        }

        void cleanupVideoPlayback(bool sendStoppedState = true)
        {
            if (hlsParser != null)
            {
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                hlsParser = null;
            }
            if (proxy != null)
            {
                proxy.Stop();
                proxy = null;
            }
            if (sendStoppedState && currentVideoSessionId != null)
            {
                airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, ShairportSharp.Airplay.PlaybackState.Stopped);
            }

            //restoreVolume();
            currentVideoSessionId = null;
            //currentVideoPlayer = null;
            currentVideoUrl = null;
        }

        //void restoreVolume()
        //{
        //    if (savedVolume != null)
        //    {
        //        VolumeHandler.Instance.Volume = (int)savedVolume;
        //        savedVolume = null;
        //    }
        //}

        void invoke(System.Action action, bool wait = true)
        {
            action();
            //if (wait)
            //{
            //    GUIWindowManager.SendThreadCallbackAndWait((p1, p2, o) =>
            //    {
            //        action();
            //        return 0;
            //    }, 0, 0, null);
            //}
            //else
            //{
            //    GUIWindowManager.SendThreadCallback((p1, p2, o) =>
            //    {
            //        action();
            //        return 0;
            //    }, 0, 0, null);
            //}
        }

        void stopCurrentItem()
        {
            AirplayVideoPlayer currentVideoPlayer = getCurrentVideoPlayer();
            if (currentVideoPlayer != null)
                currentVideoPlayer.Stop();
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
