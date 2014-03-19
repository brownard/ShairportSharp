using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ShairportSharp.Http;

namespace ShairportSharp.Airplay
{
    public class AirplayServer
    {
        #region Variables

        object syncRoot = new object();
        string name;
        string password;
        int port = 7000;
        AirplayEmitter emitter;
        HttpConnectionHandler listener;
        AirplayServerInfo serverInfo;

        object connectionSync = new object();
        List<AirplaySession> connections = new List<AirplaySession>();
        Dictionary<string, AirplaySession> eventConnections = new Dictionary<string, AirplaySession>();

        object photoSync = new object();
        Dictionary<string, Dictionary<string, byte[]>> photoCache = new Dictionary<string, Dictionary<string, byte[]>>();

        #endregion

        #region Public Properties

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public AirplayServerInfo ServerInfo
        {
            get { return serverInfo; }
        }

        #endregion

        #region Ctor

        public AirplayServer(string name, string password = null)
        {
            this.name = name;
            this.password = password;
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
            byte[] macAddress = Utils.GetMacAddress();
            if (macAddress != null)
                serverInfo.DeviceId = macAddress.StringFromAddressBytes(":");
        }

        #endregion

        #region Generic Events

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

        #endregion

        #region Public Methods

        public void Start()
        {
            lock (syncRoot)
            {
                if (listener != null)
                    Stop();

                listener = new HttpConnectionHandler(IPAddress.Any, port);
                listener.SocketAccepted += listener_SocketAccepted;
                listener.Start();
                emitter = new AirplayEmitter(name.ComputerNameIfNullOrEmpty(), serverInfo, port, !string.IsNullOrEmpty(password));
                emitter.Publish();
            }
        }

        public void Stop()
        {
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
        }

        public void SetPlaybackState(string sessionId, PlaybackState state)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(o => 
            {
                lock (connectionSync)
                {
                    AirplaySession eventConnection;
                    if (eventConnections.TryGetValue(sessionId, out eventConnection))
                    {
                        PlaybackStateInfo info = new PlaybackStateInfo()
                        {
                            State = state,
                            SessionId = eventConnection.SessionId
                        };

                        HttpRequest request = new HttpRequest("POST", "/event", "HTTP/1.1");
                        request["Content-Type"] = "text/x-apple-plist";
                        request["X-Apple-Session-ID"] = eventConnection.SessionId;
                        string plistXml = PlistCS.Plist.writeXml(info.GetPlist());
                        //Logger.Debug("Created plist xml - '{0}'", plistXml);
                        request.SetContent(plistXml);
                        eventConnection.Send(request);
                    }
                }
            });
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

            session.Closed += session_Closed;
            lock (connectionSync)
                connections.Add(session);
            session.Start();
        }

        void session_EventConnection(object sender, AirplayEventArgs e)
        {
            if (string.IsNullOrEmpty(e.SessionId))
                Logger.Warn("Event connection received without session id");
            else
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

        void session_Closed(object sender, EventArgs e)
        {
            lock (connectionSync)
            {
                AirplaySession session = (AirplaySession)sender;
                connections.Remove(session);
                if (eventConnections.ContainsValue(session))
                {
                    Logger.Debug("Airplay Server: Event connection closed");
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
