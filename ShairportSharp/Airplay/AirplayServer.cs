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
        object syncRoot = new object();
        string name;
        string password;
        int port = 7000;
        AirplayEmitter dummyEmitter;
        AirplayEmitter emitter;
        ServerListener listener;
        AirplayServerInfo serverInfo;

        object connectionSync = new object();
        List<AirplaySession> connections = new List<AirplaySession>();
        AirplaySession twoWayConnection;

        object photoSync = new object();
        Dictionary<string, byte[]> photoCache = new Dictionary<string,byte[]>();
        List<string> cacheKeys = new List<string>();
        int maxCachedItems = 5;

        public AirplayServer(string name, string password = null)
        {
            this.name = name;
            this.password = password;
            serverInfo = new AirplayServerInfo()
            {
                Model = "MediaPortal,1",
                ProtocolVersion = "1.0",
                ServerVersion = "130.14",
                Features = 
                AirplayFeature.Photo|
                AirplayFeature.PhotoCaching |
                AirplayFeature.Slideshow |
                AirplayFeature.Video|
                AirplayFeature.Unknown
            };
            byte[] macAddress = Utils.GetMacAddress();
            if (macAddress != null)
                serverInfo.DeviceId = macAddress.StringFromAddressBytes(":");
        }

        public event EventHandler<PhotoEventArgs> PhotoReceived;
        protected virtual void OnPhotoReceived(PhotoEventArgs e)
        {
            if (PhotoReceived != null)
                PhotoReceived(this, e);
        }

        public void Start()
        {                        
            lock (syncRoot)
            {
                if (listener != null)
                    Stop();

                listener = new ServerListener(IPAddress.Any, port);
                listener.SocketAccepted += listener_SocketAccepted;
                listener.Start();

                dummyEmitter = new AirplayEmitter(name.ComputerNameIfNullOrEmpty() + "_init", serverInfo, port, !string.IsNullOrEmpty(password));
                dummyEmitter.DidPublishService += (s) =>
                {
                    System.Threading.Thread.Sleep(2000);
                    lock (syncRoot)
                    {
                        if (dummyEmitter != null)
                        {
                            emitter = new AirplayEmitter(name.ComputerNameIfNullOrEmpty(), serverInfo, port, !string.IsNullOrEmpty(password));
                            emitter.Publish();
                        }
                    }
                };
                dummyEmitter.Publish();
            }
        }

        public void Stop()
        {
            lock (syncRoot)
            {
                if (dummyEmitter != null)
                {
                    dummyEmitter.Stop();
                    dummyEmitter = null;
                }
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
                List<AirplaySession> lConnections = new List<AirplaySession>(connections);
                foreach (AirplaySession connection in lConnections)
                    connection.Close();
            }
        }

        void listener_SocketAccepted(object sender, SocketAcceptedEventArgs e)
        {
            e.Handled = true;
            AirplaySession session = new AirplaySession(e.Socket, serverInfo);
            session.EventConnection += session_EventConnection;
            session.PhotoReceived += session_PhotoReceived;
            session.Closed += session_Closed;
            lock (connectionSync)
                connections.Add(session);
            session.Start();
        }

        void session_EventConnection(object sender, EventArgs e)
        {
            lock (connectionSync)
                twoWayConnection = (AirplaySession)sender;
        }

        void session_PhotoReceived(object sender, PhotoReceivedEventArgs e)
        {
            byte[] photo;
            lock (photoSync)
            {
                if (e.AssetAction == PhotoAction.CacheOnly)
                {
                    cachePhoto(e.AssetKey, e.Photo);
                    return;
                }

                if (e.AssetAction == PhotoAction.DisplayCached)
                {
                    if (!photoCache.TryGetValue(e.AssetKey, out photo))
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

            OnPhotoReceived(new PhotoEventArgs(e.AssetKey, e.Transition, photo));
        }

        void cachePhoto(string key, byte[] value)
        {
            if (!cacheKeys.Contains(key))
            {
                photoCache[key] = value;
                cacheKeys.Add(key);
                if (maxCachedItems > 0 && cacheKeys.Count > maxCachedItems)
                {
                    photoCache.Remove(cacheKeys[0]);
                    cacheKeys.RemoveAt(0);
                }
            }
        }

        void session_Closed(object sender, EventArgs e)
        {
            lock (connectionSync)
            {
                AirplaySession session = (AirplaySession)sender;
                connections.Remove(session);
                if (twoWayConnection == session)
                    twoWayConnection = null;
            }
        }
    }
}
