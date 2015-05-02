using AirPlayer.Common.Hls;
using AirPlayer.Common.Player;
using AirPlayer.Common.Proxy;
using AirPlayer.MediaPortal2.Configuration;
using AirPlayer.MediaPortal2.MediaItems;
using AirPlayer.MediaPortal2.Players;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Services.Players;
using MediaPortal.UiComponents.Media.Models;
using ShairportSharp.Airplay;
using ShairportSharp.Audio;
using ShairportSharp.Helpers;
using ShairportSharp.Mirroring;
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

        AsynchronousMessageQueue _messageQueue;
        readonly SettingsChangeWatcher<PluginSettings> settingsWatcher = new SettingsChangeWatcher<PluginSettings>();

        RaopServer airtunesServer;
        AirplayServer airplayServer;

        bool allowVolumeControl;
        bool sendCommands;
        bool allowHDStreams;
        int videoBuffer;

        object videoInfoSync = new object();
        HlsProxy proxy;
        HlsParser hlsParser;
        string currentVideoSessionId;
        string currentVideoUrl;
        string lastVideoSessionId;
        string lastVideoUrl;
        bool lastUseMPUrlSourceFilter;
        DateTime videoReceiveTime = DateTime.MinValue;

        object photoInfoSync = new object();
        string photoSessionId;

        object audioInfoSync = new object();
        string currentAudioSessionId;
        bool isAudioBuffering;
        DmapData currentMeta;
        byte[] currentCover;
        uint currentStartStamp;
        uint currentStopStamp;
        bool isAudioPlaying;

        object mirroringInfoSync = new object();
        bool isMirroringBuffering;
        bool isMirroringPlaying;

        object volumeSync = new object();
        int? savedVolume;

        #endregion

        #region Ctor

        public AirPlayer()
        {
            Common.Logger.SetLogger(Logger.Instance);
            ShairportSharp.Logger.SetLogger(Logger.Instance);
            settingsWatcher.SettingsChanged += settingsChanged;
        }

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
            if (message.ChannelName == PlayerManagerMessaging.CHANNEL && message.MessageData.ContainsKey(PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER))
            {
                HandlePlayerMessage((PlayerManagerMessaging.MessageType)message.MessageType, (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER]);
            }
        }

        private void HandlePlayerMessage(PlayerManagerMessaging.MessageType message, IPlayerSlotController psc)
        {
            switch (message)
            {
                case PlayerManagerMessaging.MessageType.PlayerEnded:
                case PlayerManagerMessaging.MessageType.PlayerStopped:
                case PlayerManagerMessaging.MessageType.PlayerError:
                    IPlayerContext pc = PlayerContext.GetPlayerContext(psc);
                    if (pc != null)
                        onPlayerStopped(pc.CurrentMediaItem);
                    break;
            }
        }

        void onPlayerStopped(MediaItem mediaItem)
        {
            if (mediaItem is VideoItem)
            {
                lock (videoInfoSync)
                {
                    if (currentVideoSessionId != null)
                    {
                        airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, ShairportSharp.Airplay.PlaybackState.Stopped);
                        cleanupVideoPlayback();
                    }
                }
            }
            else if (mediaItem is ImageItem)
            {
                lock (photoInfoSync)
                    photoSessionId = null;
            }
            else if (mediaItem is AudioItem)
            {
                lock (audioInfoSync)
                {
                    if (isAudioPlaying)
                    {
                        airtunesServer.SendCommand(RemoteCommand.Stop);
                        cleanupAudioPlayback();
                    }
                }
            }
            else if (mediaItem is MirroringItem)
            {
                lock (mirroringInfoSync)
                {
                    airplayServer.MirroringServer.StopCurrentSession();
                    cleanupMirroringPlayback();
                }
            }
        }

        #endregion

        #region IPluginStateTracker Members

        public void Activated(PluginRuntime pluginRuntime)
        {
            SubscribeToMessages();
            
            airtunesServer = new RaopServer();
            airtunesServer.StreamStarting += airtunesServer_StreamStarting;
            airtunesServer.StreamReady += airtunesServer_StreamReady;
            airtunesServer.StreamStopped += airtunesServer_StreamStopped;
            airtunesServer.PlaybackProgressChanged += airtunesServer_PlaybackProgressChanged;
            airtunesServer.MetaDataChanged += airtunesServer_MetaDataChanged;
            airtunesServer.ArtworkChanged += airtunesServer_ArtworkChanged;
            airtunesServer.VolumeChanged += airtunesServer_VolumeChanged;

            airplayServer = new AirplayServer();
            airplayServer.ShowPhoto += airplayServer_ShowPhoto;
            airplayServer.VideoReceived += airplayServer_VideoReceived;
            airplayServer.PlaybackInfoRequested += airplayServer_PlaybackInfoRequested;
            airplayServer.GetPlaybackPosition += airplayServer_GetPlaybackPosition;
            airplayServer.PlaybackPositionChanged += airplayServer_PlaybackPositionChanged;
            airplayServer.PlaybackRateChanged += airplayServer_PlaybackRateChanged;
            airplayServer.VolumeChanged += airplayServer_VolumeChanged;
            airplayServer.SessionStopped += airplayServer_SessionStopped;

            airplayServer.MirroringServer.Authenticating += MirroringServer_Authenticating;
            airplayServer.MirroringServer.Started += MirroringServer_Started;
            airplayServer.MirroringServer.SessionClosed += MirroringServer_SessionClosed;

            init();
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
            UnsubscribeFromMessages();
            deInit();
        }

        #endregion

        #region Init / Deinit

        void init()
        {
            PluginSettings settings = settingsWatcher.Settings;
            allowVolumeControl = settings.AllowVolume;
            sendCommands = settings.SendAudioCommands;
            allowHDStreams = settings.AllowHDStreams;
            videoBuffer = settings.VideoBuffer;

            if (airtunesServer != null)
            {
                airtunesServer.Name = settings.ServerName;
                airtunesServer.Password = settings.Password;
                airtunesServer.MacAddress = settings.CustomAddress;
                airtunesServer.Port = settings.RtspPort;
                airtunesServer.AudioPort = settings.UdpPort;
                airtunesServer.AudioBufferSize = (int)(settings.AudioBuffer * 1000);
                airtunesServer.Start();
            }

            if (airplayServer != null)
            {
                airplayServer.Name = settings.ServerName;
                airplayServer.Password = settings.Password;
                airplayServer.MacAddress = settings.CustomAddress;
                airplayServer.iOS8Workaround = settings.iOS8Workaround;
                airplayServer.Port = settings.AirplayPort;
                airplayServer.Start();
            }
        }

        void deInit()
        {
            if (airtunesServer != null)
                airtunesServer.Stop();
            if (airplayServer != null)
                airplayServer.Stop();
        }

        void settingsChanged(object sender, EventArgs e)
        {
            deInit();
            init();
        }

        #endregion

        #region Airtunes Event Handlers

        void airtunesServer_StreamStarting(object sender, RaopEventArgs e)
        {
            lock (audioInfoSync)
            {
                stopPlayer<AirplayAudioPlayer>();
                cleanupAudioPlayback();
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                currentAudioSessionId = e.SessionId;
                isAudioBuffering = true;
            }
        }

        void airtunesServer_StreamReady(object sender, RaopEventArgs e)
        {
            lock (audioInfoSync)
            {
                if (!isAudioBuffering)
                {
                    airtunesServer.SendCommand(RemoteCommand.Stop);
                    return;
                }
                isAudioBuffering = false;
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                startPlayback();
            }
        }

        void startPlayback()
        {
            AudioBufferStream stream = airtunesServer.GetStream(StreamType.Wave);
            if (stream != null)
            {
                AudioItem item = new AudioItem(new PlayerSettings(stream));
                setMetaData(item);
                PlayItemsModel.CheckQueryPlayAction(item);
                setDuration();
                isAudioPlaying = true;
            }
        }

        void airtunesServer_PlaybackProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            lock (audioInfoSync)
            {
                //When stopping playback on the client by stopping MP's player we get zeroed timestamps, 
                //if the client wants to restart playback and the connection is still open it just sends new timestamps 
                bool restart = !isAudioBuffering && !isAudioPlaying && currentAudioSessionId == e.SessionId && currentStartStamp == 0 && currentStopStamp == 0;
                currentStartStamp = e.Start;
                currentStopStamp = e.Stop;
                if (restart)
                    startPlayback();
                else
                    setDuration();
            }
        }

        void setDuration()
        {
            AirplayAudioPlayer player = getPlayer<AirplayAudioPlayer>();
            if (player != null)
                player.UpdateDurationInfo(currentStartStamp, currentStopStamp);
        }

        void airtunesServer_MetaDataChanged(object sender, MetaDataChangedEventArgs e)
        {
            lock (audioInfoSync)
            {
                currentMeta = e.MetaData;
                updateCurrentItem();
            }
        }

        void airtunesServer_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            lock (audioInfoSync)
            {
                currentCover = e.ImageData;
                updateCurrentItem();
            }
        }

        void setMetaData(AudioItem item)
        {
            if (currentMeta != null)
                item.SetMetaData(currentMeta);
            if (currentCover != null)
                item.SetCover(currentCover);
        }

        void updateCurrentItem()
        {
            var context = getPlayerContext<AirplayAudioPlayer>();
            if (context != null)
            {
                AudioItem newItem = new AudioItem(null);
                setMetaData(newItem);
                context.DoPlay(newItem);
            }
        }

        void airtunesServer_VolumeChanged(object sender, ShairportSharp.Raop.VolumeChangedEventArgs e)
        {
            if (!allowVolumeControl)
                return;

            var context = getPlayerContext<AirplayAudioPlayer>();
            if (context != null)
            {
                var pm = ServiceRegistration.Get<IPlayerManager>();

                lock (volumeSync)
                {
                    if (savedVolume == null)
                        savedVolume = pm.Volume;
                }

                if (e.Volume < -30)
                    pm.Muted = true;
                else
                    pm.Volume = (int)((e.Volume + 30) / 0.3);
            }
        }

        void airtunesServer_StreamStopped(object sender, EventArgs e)
        {
            lock (audioInfoSync)
            {
                cleanupAudioPlayback();
                stopPlayer<AirplayAudioPlayer>();
            }
        }

        #endregion

        #region AirPlay Event Handlers

        void airplayServer_ShowPhoto(object sender, PhotoEventArgs e)
        {
            //When playing a video from the camera roll the client sends a thumbnail before the video.
            //Occasionally we receive it after due to threading so we should ignore it if we have just started playing a video.
            lock (videoInfoSync)
                if (currentVideoSessionId != null && DateTime.Now.Subtract(videoReceiveTime).TotalSeconds < 2)
                    return;

            lock (photoInfoSync)
                photoSessionId = e.SessionId;
            ImageItem item = new ImageItem(e.AssetKey, e.Photo);
            var ic = getPlayerContext<AirplayImagePlayer>();
            if (ic != null)
                ic.DoPlay(item);
            else
                PlayItemsModel.CheckQueryPlayAction(item);
        }

        void airplayServer_VideoReceived(object sender, VideoEventArgs e)
        {
            airplayServer.SetPlaybackState(e.SessionId, PlaybackCategory.Video, ShairportSharp.Airplay.PlaybackState.Loading);
            lock (videoInfoSync)
            {
                //YouTube sometimes sends play video twice?? Ignore second
                if (e.SessionId == currentVideoSessionId && e.ContentLocation == currentVideoUrl)
                {
                    Logger.Instance.Debug("Airplayer: Ignoring duplicate playback request");
                    return;
                }

                videoReceiveTime = DateTime.Now;
                stopPlayer<AirplayVideoPlayer>();
                cleanupVideoPlayback();
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();

                currentVideoSessionId = e.SessionId;
                currentVideoUrl = e.ContentLocation;
                //See if we are loading a HLS stream. 
                //If so, manually select the best quality as LAVSplitter just selects the first/default.
                //If not, allow allow MPUrlSourceFilter as it has better seeking support but doesn't seem to like HLS streams :(
                hlsParser = new HlsParser(currentVideoUrl);
                hlsParser.Completed += hlsParser_Completed;
                hlsParser.Start();
            }
        }

        void hlsParser_Completed(object sender, EventArgs e)
        {
            lock (videoInfoSync)
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
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                startVideoLoading(finalUrl, useMPUrlSourceFilter);
            }
        }

        void startVideoLoading(string url, bool useMPSourceFilter = false)
        {
            PlayItemsModel.CheckQueryPlayAction(new VideoItem(url));
            lastVideoUrl = url;
            lastVideoSessionId = currentVideoSessionId;
            lastUseMPUrlSourceFilter = useMPSourceFilter;
        }

        void airplayServer_PlaybackInfoRequested(object sender, PlaybackInfoEventArgs e)
        {
            AirplayVideoPlayer currentVideoPlayer = getPlayer<AirplayVideoPlayer>();
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

        void airplayServer_GetPlaybackPosition(object sender, GetPlaybackPositionEventArgs e)
        {
            AirplayVideoPlayer currentVideoPlayer = getPlayer<AirplayVideoPlayer>();
            if (currentVideoPlayer != null)
            {
                e.Duration = currentVideoPlayer.Duration.TotalSeconds;
                e.Position = currentVideoPlayer.CurrentTime.TotalSeconds;
            }
        }

        void airplayServer_PlaybackPositionChanged(object sender, PlaybackPositionEventArgs e)
        {
            AirplayVideoPlayer currentVideoPlayer = getPlayer<AirplayVideoPlayer>();
            if (currentVideoPlayer != null)
            {
                if (e.Position >= 0 && e.Position <= currentVideoPlayer.Duration.TotalSeconds)
                    currentVideoPlayer.CurrentTime = TimeSpan.FromSeconds(e.Position);
            }
            else
            {
                lock (videoInfoSync)
                {
                    if (currentVideoUrl == null && e.SessionId == lastVideoSessionId && lastVideoUrl != null)
                    {
                        airplayServer.SetPlaybackState(currentVideoSessionId, PlaybackCategory.Video, ShairportSharp.Airplay.PlaybackState.Loading);
                        currentVideoSessionId = lastVideoSessionId;
                        currentVideoUrl = lastVideoUrl;
                        startVideoLoading(currentVideoUrl, lastUseMPUrlSourceFilter);
                    }
                }
            }
        }

        void airplayServer_PlaybackRateChanged(object sender, PlaybackRateEventArgs e)
        {
            AirplayVideoPlayer currentVideoPlayer = getPlayer<AirplayVideoPlayer>();
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

        void airplayServer_VolumeChanged(object sender, ShairportSharp.Airplay.VolumeChangedEventArgs e)
        {
            if (!allowVolumeControl)
                return;

            var context = getPlayerContext<AirplayVideoPlayer>();
            if (context != null)
            {
                var pm = ServiceRegistration.Get<IPlayerManager>();
                lock (volumeSync)
                {
                    if (savedVolume == null)
                        savedVolume = pm.Volume;

                    if (e.Volume == 0)
                    {
                        pm.Muted = true;
                    }
                    else if (e.Volume == 1)
                    {
                        pm.Volume = 100;
                    }
                    else
                    {
                        double factor = 100 / 0.9;
                        pm.Volume = (int)(factor - factor / Math.Pow(10, e.Volume));
                    }
                }
            }
        }

        void airplayServer_SessionStopped(object sender, AirplayEventArgs e)
        {
            if (e.SessionId == currentVideoSessionId)
            {
                lock (videoInfoSync)
                {
                    cleanupVideoPlayback();
                    stopPlayer<AirplayVideoPlayer>();
                }
            }
            else if (e.SessionId == photoSessionId)
            {
                lock (photoInfoSync)
                {
                    photoSessionId = null;
                    stopPlayer<AirplayImagePlayer>();
                }
            }
        }

        #endregion

        #region Mirroring Event Handlers
        
        void MirroringServer_Authenticating(object sender, EventArgs e)
        {
            lock (mirroringInfoSync)
            {
                stopPlayer<AirplayMirroringPlayer>();
                cleanupMirroringPlayback();
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                isMirroringBuffering = true;
            }
        }

        void MirroringServer_Started(object sender, MirroringStartedEventArgs e)
        {
            lock (mirroringInfoSync)
            {
                if (!isMirroringBuffering)
                    return;
                isMirroringBuffering = false;
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();

                MirroringItem item = new MirroringItem(e.Stream);
                PlayItemsModel.CheckQueryPlayAction(item);
                isMirroringPlaying = true;
            }
        }

        void MirroringServer_SessionClosed(object sender, EventArgs e)
        {
            lock (mirroringInfoSync)
                cleanupMirroringPlayback();
        }

        #endregion

        #region Utils

        T getPlayer<T>()
        {
            var context = getPlayerContext<T>();
            if (context != null)
                return (T)context.CurrentPlayer;
            return default(T);
        }

        IPlayerContext getPlayerContext<T>()
        {
            var contexts = ServiceRegistration.Get<IPlayerContextManager>().PlayerContexts;
            return contexts.FirstOrDefault(vc => vc.CurrentPlayer is T);
        }

        void stopPlayer<T>()
        {
            IPlayerContext context = getPlayerContext<T>();
            if (context != null)
                context.Stop();
        }

        void cleanupAudioPlayback()
        {
            if (isAudioBuffering)
            {
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                isAudioBuffering = false;
            }
            restoreVolume();
            isAudioPlaying = false;
        }

        void cleanupVideoPlayback()
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

            restoreVolume();
            currentVideoSessionId = null;
            currentVideoUrl = null;
        }

        void cleanupMirroringPlayback()
        {
            if (isMirroringBuffering)
            {
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                isMirroringBuffering = false;
            }
            isMirroringPlaying = false;
        }

        void restoreVolume()
        {
            lock (volumeSync)
            {
                if (savedVolume != null)
                {
                    ServiceRegistration.Get<IPlayerManager>().Volume = (int)savedVolume;
                    savedVolume = null;
                }
            }
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
