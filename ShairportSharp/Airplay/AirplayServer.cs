using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ShairportSharp.Http;
using ShairportSharp.Helpers;

namespace ShairportSharp.Airplay
{
    public class AirplayServer
    {
        #region Consts

        const int DEFAULT_PORT = 7000;

        #endregion

        #region Variables

        object syncRoot = new object();
        AirplayEmitter emitter;
        HttpConnectionHandler listener;
        AirplayServerInfo serverInfo;

        object connectionSync = new object();
        List<AirplaySession> connections = new List<AirplaySession>();
        Dictionary<string, AirplaySession> eventConnections = new Dictionary<string, AirplaySession>();

        object photoSync = new object();
        Dictionary<string, Dictionary<string, byte[]>> photoCache = new Dictionary<string, Dictionary<string, byte[]>>();

        #endregion

        #region Ctor

        public AirplayServer()
        {
            serverInfo = new AirplayServerInfo()
            {
                Model = "MediaPortal,1",
                ProtocolVersion = "1.0",
                ServerVersion = "130.14",
                Features = AirplayFeature.Photo |
                AirplayFeature.PhotoCaching |
                AirplayFeature.Slideshow |
                AirplayFeature.Video |
                AirplayFeature.VideoHTTPLiveStreams |
                AirplayFeature.VideoVolumeControl
            };
        }

        public AirplayServer(string name, string password = null)
            : this()
        {
            this.name = name;
            this.password = password;
        }

        #endregion

        #region Public Properties

        string name;
        /// <summary>
        // The broadcasted name of the Airplay server. Defaults to the machine name if null or empty.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string password;
        /// <summary>
        /// The password needed to connect. Set to null or empty to not require a password.
        /// </summary>
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        /// <summary>
        /// The MAC address used to identify this server. If null or empty the actual MAC address of this computer will be used.
        /// Set to an alternative value to allow multiple servers on the same computer.
        /// </summary>
        byte[] macAddress = null;
        public byte[] MacAddress
        {
            get { return macAddress; }
            set { macAddress = value; }
        }

        int port = DEFAULT_PORT;
        public int Port
        {
            get { return port; }
            set { port = value.CheckValidPortNumber(DEFAULT_PORT); }
        }

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

        public void Start()
        {
            if (macAddress == null || macAddress.Length == 0)
                macAddress = Utils.GetMacAddress();
            if (macAddress == null)
                return;

            lock (syncRoot)
            {
                if (listener != null)
                    Stop();

                serverInfo.DeviceId = macAddress.HexStringFromBytes(":");
                Logger.Info("Airplay Server: Starting - MAC address {0}, port {1}", serverInfo.DeviceId, port);
                listener = new HttpConnectionHandler(IPAddress.Any, port);
                listener.SocketAccepted += listener_SocketAccepted;
                listener.Start();
                emitter = new AirplayEmitter(name.ComputerNameIfNullOrEmpty(), serverInfo, port, !string.IsNullOrEmpty(password), ios8Workaround);
                emitter.Publish();
            }
        }

        public void Stop()
        {
            Logger.Info("Airplay Server: Stopping");
            lock (syncRoot)
            {
                if (emitter != null)
                {
                    emitter.Stop();
                    emitter = null;
                }
                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }
            }
            lock (connectionSync)
            {
                while(connections.Count > 0)
                    connections[0].Close();
            }
            Logger.Info("Airplay Server: Stopped");
        }

        public void SetPlaybackState(string sessionId, PlaybackCategory category, PlaybackState state)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                Logger.Warn("AirplayServer: SetPlaybackState: Empty sessionId");
                return;
            }

            lock (connectionSync)
            {
                AirplaySession eventConnection;
                if (eventConnections.TryGetValue(sessionId, out eventConnection))
                {
                    eventConnection.SendPlaybackState(category, state);
                }
            }
        }

        public void CloseSession(string sessionId)
        {
            AirplaySession[] connectionsToClose;
            lock (connectionSync)
                connectionsToClose = connections.Where(s => s.SessionId == sessionId).ToArray();
            foreach (AirplaySession session in connectionsToClose)
                session.Close();
        }

        #endregion

        #region Event Handlers

        void listener_SocketAccepted(object sender, SocketAcceptedEventArgs e)
        {
            e.Handled = true;
            AirplaySession session = new AirplaySession(e.Socket, serverInfo, password);
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
            session.Closed += session_Closed;
            lock (connectionSync)
                connections.Add(session);
            session.Start();
        }

        void session_EventConnection(object sender, AirplayEventArgs e)
        {
            Logger.Debug("Airplay Server: Event connection received, '{0}'", e.SessionId);
            lock (connectionSync)
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

        void session_Closed(object sender, EventArgs e)
        {
            lock (connectionSync)
            {
                AirplaySession session = (AirplaySession)sender;
                connections.Remove(session);
                if (eventConnections.ContainsValue(session))
                {
                    Logger.Debug("Airplay Server: Event connection closed, '{0}'", session.SessionId);
                    eventConnections.Remove(session.SessionId);
                    lock (photoSync)
                        photoCache.Remove(session.SessionId);
                    OnSessionClosed(new AirplayEventArgs(session.SessionId));
                }
            }
        }

        #endregion
    }
}
