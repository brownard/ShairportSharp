using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ShairportSharp.Http;
using ShairportSharp.Helpers;
using ShairportSharp.Base;
using ShairportSharp.Mirroring;

namespace ShairportSharp.Airplay
{
    public class AirplayServer : NamedServer<AirplaySession>
    {
        #region Consts

        const int DEFAULT_PORT = 7000;

        #endregion

        #region Variables

        AirplayServerInfo serverInfo;

        object eventConnectionSync = new object();
        Dictionary<string, AirplaySession> eventConnections = new Dictionary<string, AirplaySession>();

        object photoSync = new object();
        Dictionary<string, Dictionary<string, byte[]>> photoCache = new Dictionary<string, Dictionary<string, byte[]>>();

        MirroringServer mirroringServer;

        #endregion

        #region Ctor

        public AirplayServer()
        {
            Port = DEFAULT_PORT;
            serverInfo = new AirplayServerInfo()
            {
                ProtocolVersion = "1.0",
                ServerVersion = Constants.VERSION,
                Features = AirplayFeature.Photo |
                AirplayFeature.PhotoCaching |
                AirplayFeature.Slideshow |
                AirplayFeature.Video |
                AirplayFeature.VideoHTTPLiveStreams |
                AirplayFeature.VideoVolumeControl
            };

            mirroringServer = new MirroringServer();
        }

        public AirplayServer(string name, string password = null)
            : this()
        {
            Name = name;
            Password = password;
        }

        #endregion

        #region Public Properties
        
        bool ios8Workaround;
        public bool iOS8Workaround
        {
            get { return ios8Workaround; }
            set { ios8Workaround = value; }
        }

        public AirplayServerInfo ServerInfo
        {
            get { return serverInfo; }
        }

        public MirroringServer MirroringServer
        {
            get { return mirroringServer; }
        }

        #endregion

        #region Generic Events

        public event EventHandler<AirplayEventArgs> SessionStopped;
        protected virtual void OnSessionStopped(AirplayEventArgs e)
        {
            if (SessionStopped != null)
                SessionStopped(this, e);
        }

        public event EventHandler<AirplayEventArgs> SessionClosed;
        protected virtual void OnSessionClosed(AirplayEventArgs e)
        {
            if (SessionClosed != null)
                SessionClosed(this, e);
        }

        #endregion

        #region Photo Events

        public event EventHandler<PhotoReceivedEventArgs> PhotoReceived;
        protected virtual void OnPhotoReceived(PhotoReceivedEventArgs e)
        {
            if (PhotoReceived != null)
                PhotoReceived(this, e);
        }

        public event EventHandler<PhotoEventArgs> ShowPhoto;
        protected virtual void OnShowPhoto(PhotoEventArgs e)
        {
            if (ShowPhoto != null)
                ShowPhoto(this, e);
        }

        public event EventHandler<SlideshowFeaturesEventArgs> SlideshowFeaturesRequested;
        protected virtual void OnSlideshowFeaturesRequested(SlideshowFeaturesEventArgs e)
        {
            if (SlideshowFeaturesRequested != null)
                SlideshowFeaturesRequested(this, e);
        }

        public event EventHandler<SlideshowSettingsEventArgs> SlideshowSettingsReceived;
        protected virtual void OnSlideshowSettingsReceived(SlideshowSettingsEventArgs e)
        {
            if (SlideshowSettingsReceived != null)
                SlideshowSettingsReceived(this, e);
        }

        #endregion

        #region Video Events

        public event EventHandler<VideoEventArgs> VideoReceived;
        protected virtual void OnVideoReceived(VideoEventArgs e)
        {
            if (VideoReceived != null)
                VideoReceived(this, e);
        }

        public event EventHandler<PlaybackInfoEventArgs> PlaybackInfoRequested;
        protected virtual void OnPlaybackInfoRequested(PlaybackInfoEventArgs e)
        {
            if (PlaybackInfoRequested != null)
                PlaybackInfoRequested(this, e);
        }

        public event EventHandler<PlaybackRateEventArgs> PlaybackRateChanged;
        protected virtual void OnPlaybackRateChanged(PlaybackRateEventArgs e)
        {
            if (PlaybackRateChanged != null)
                PlaybackRateChanged(this, e);
        }

        public event EventHandler<PlaybackPositionEventArgs> PlaybackPositionChanged;
        protected virtual void OnPlaybackPositionChanged(PlaybackPositionEventArgs e)
        {
            if (PlaybackPositionChanged != null)
                PlaybackPositionChanged(this, e);
        }

        public event EventHandler<GetPlaybackPositionEventArgs> GetPlaybackPosition;
        protected virtual void OnGetPlaybackPosition(GetPlaybackPositionEventArgs e)
        {
            if (GetPlaybackPosition != null)
                GetPlaybackPosition(this, e);
        }

        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        protected virtual void OnVolumeChanged(VolumeChangedEventArgs e)
        {
            if (VolumeChanged != null)
                VolumeChanged(this, e);
        }

        #endregion

        #region Public Methods

        public void SetPlaybackState(string sessionId, PlaybackCategory category, PlaybackState state)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                Logger.Warn("AirplayServer: SetPlaybackState: Empty sessionId");
                return;
            }

            lock (eventConnectionSync)
            {
                AirplaySession eventConnection;
                if (eventConnections.TryGetValue(sessionId, out eventConnection))
                {
                    eventConnection.SendPlaybackState(category, state);
                }
            }
        }

        #endregion

        #region Overrides

        protected override void OnStarting()
        {
            byte[] macAddress = MacAddress;
            if (macAddress == null || macAddress.Length == 0)
            {
                macAddress = Utils.GetMacAddress();
                if (macAddress == null)
                    return;
            }

            serverInfo.Model = ModelName;
            serverInfo.DeviceId = macAddress.HexStringFromBytes(":");
            Emitter = new AirplayEmitter(Name.ComputerNameIfNullOrEmpty(), serverInfo, Port, !string.IsNullOrEmpty(Password), ios8Workaround);

            mirroringServer.Password = Password;
            mirroringServer.Start();
        }

        protected override AirplaySession OnSocketAccepted(System.Net.Sockets.Socket socket)
        {
            AirplaySession session = new AirplaySession(socket, serverInfo, Password);
            session.EventConnection += session_EventConnection;

            session.PhotoReceived += session_PhotoReceived;
            session.SlideshowFeaturesRequested += session_SlideshowFeaturesRequested;
            session.SlideshowSettingsReceived += session_SlideshowSettingsReceived;

            session.VideoReceived += session_VideoReceived;
            session.PlaybackInfoRequested += session_PlaybackInfoRequested;
            session.PlaybackRateChanged += session_PlaybackRateChanged;
            session.PlaybackPositionChanged += session_PlaybackPositionChanged;
            session.GetPlaybackPosition += session_GetPlaybackPosition;
            session.VolumeChanged += session_VolumeChanged;
            session.Stopped += session_Stopped;

            return session;
        }

        protected override void OnConnectionClosed(AirplaySession connection)
        {
            bool closed = false;
            lock (eventConnectionSync)
            {
                if (eventConnections.ContainsValue(connection))
                {
                    Logger.Debug("Airplay Server: Event connection closed, '{0}'", connection.SessionId);
                    eventConnections.Remove(connection.SessionId);
                    lock (photoSync)
                        photoCache.Remove(connection.SessionId);
                    closed = true;
                }
            }
            if (closed)
                OnSessionClosed(new AirplayEventArgs(connection.SessionId));
        }

        #endregion

        #region Event Handlers

        void session_EventConnection(object sender, AirplayEventArgs e)
        {
            Logger.Debug("Airplay Server: Event connection received, '{0}'", e.SessionId);
            lock (eventConnectionSync)
                eventConnections[e.SessionId] = (AirplaySession)sender;
        }

        void session_PhotoReceived(object sender, PhotoReceivedEventArgs e)
        {
            OnPhotoReceived(e);

            byte[] photo;
            lock (photoSync)
            {
                if (e.AssetAction == PhotoAction.CacheOnly)
                {
                    cachePhoto(e.SessionId, e.AssetKey, e.Photo);
                    return;
                }

                if (e.AssetAction == PhotoAction.DisplayCached)
                {
                    Dictionary<string, byte[]> sessionCache;
                    if (!photoCache.TryGetValue(e.SessionId, out sessionCache) || !sessionCache.TryGetValue(e.AssetKey, out photo))
                    {
                        e.NotInCache = true;
                        return;
                    }
                }
                else
                {
                    photo = e.Photo;
                }
            }

            OnShowPhoto(new PhotoEventArgs(e.AssetKey, e.Transition, photo, e.SessionId));
        }

        void session_SlideshowFeaturesRequested(object sender, SlideshowFeaturesEventArgs e)
        {
            OnSlideshowFeaturesRequested(e);
        }

        void session_SlideshowSettingsReceived(object sender, SlideshowSettingsEventArgs e)
        {
            OnSlideshowSettingsReceived(e);
        }

        void session_VideoReceived(object sender, VideoEventArgs e)
        {
            OnVideoReceived(e);
        }

        void session_PlaybackInfoRequested(object sender, PlaybackInfoEventArgs e)
        {
            OnPlaybackInfoRequested(e);
        }

        void session_PlaybackRateChanged(object sender, PlaybackRateEventArgs e)
        {
            OnPlaybackRateChanged(e);
        }

        void session_PlaybackPositionChanged(object sender, PlaybackPositionEventArgs e)
        {
            OnPlaybackPositionChanged(e);
        }

        void session_GetPlaybackPosition(object sender, GetPlaybackPositionEventArgs e)
        {
            OnGetPlaybackPosition(e);
        }

        void session_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            OnVolumeChanged(e);
        }

        void cachePhoto(string sessionId, string key, byte[] value)
        {
            Dictionary<string, byte[]> sessionCache;
            if (!photoCache.TryGetValue(sessionId, out sessionCache))
            {
                sessionCache = new Dictionary<string, byte[]>();
                sessionCache[key] = value;
                photoCache[sessionId] = sessionCache;
            }
            else if (!sessionCache.ContainsKey(key))
            {
                sessionCache[key] = value;
            }
        }

        void session_Stopped(object sender, AirplayEventArgs e)
        {
            OnSessionStopped(e);
        }

        #endregion
    }
}
