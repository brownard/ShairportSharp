using ShairportSharp.Audio;
using ShairportSharp.Remote;
using ShairportSharp.Raop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using ShairportSharp.Http;
using ShairportSharp.Bonjour;
using ShairportSharp.Helpers;

namespace ShairportSharp.Raop
{
    /// <summary>
    /// The main class used for broadcasting and listening for new connections
    /// </summary>
    public class RaopServer
    {
        #region Consts

        const int DEFAULT_PORT = 5000;

        #endregion

        #region Variables

        BonjourEmitter bonjour;
        object listenerLock = new object();
        HttpConnectionHandler listener = null;
        object sessionLock = new object();
        RaopSession currentSession = null;
        RemoteHandler remoteHandler;

        #endregion

        #region Ctor

        /// <summary>
        /// Create a server using the machine name and no password
        /// </summary>
        public RaopServer() { }

        /// <summary>
        /// Create a server with the specified name and optional password
        /// </summary>
        /// <param name="name">The broadcasted name of the server</param>
        /// <param name="password">The password to use (use null or empty for no password)</param>
        public RaopServer(string name, string password = null)
        {
            this.name = name;
            this.password = password;
        }

        #endregion

        #region Properties

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

        byte[] macAddress = null;
        /// <summary>
        /// The MAC address used to identify this server. If null or empty the actual MAC address of this computer will be used.
        /// Set to an alternative value to allow multiple servers on the same computer.
        /// </summary>
        public byte[] MacAddress
        {
            get { return macAddress; }
            set { macAddress = value; }
        }

        int port = DEFAULT_PORT;
        /// <summary>
        /// The port to listen on for RTSP packets. Defaults to 5000.
        /// </summary>
        public int Port
        {
            get { return port; }
            set { port = value.CheckValidPortNumber(DEFAULT_PORT); }
        }

        string modelName = Constants.DEFAULT_MODEL_NAME;
        /// <summary>
        /// The model of the server, should be in the format '[NAME], [VERSION]'
        /// In most cases can be left as the default 'ShairportSharp, 1'
        /// </summary>
        public string ModelName
        {
            get { return modelName; }
            set { modelName = value; }
        }

        /// <summary>
        /// The port to listen on for audio packets. Defaults to 6000.
        /// </summary>
        public int AudioPort
        {
            get;
            set;
        }

        /// <summary>
        /// The size of the audio buffer in milliseconds
        /// </summary>
        public int AudioBufferSize
        {
            get;
            set;
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when an audio stream has been started but not yet buffered fully.
        /// </summary>
        public event EventHandler<RaopEventArgs> StreamStarting;
        protected virtual void OnStreamStarting(RaopEventArgs e)
        {
            if (StreamStarting != null)
                StreamStarting(this, e);
        }

        /// <summary>
        /// Fired when an audio stream has been stopped.
        /// </summary>
        public event EventHandler StreamStopped;
        protected virtual void OnStreamStopped(EventArgs e)
        {
            if (StreamStopped != null)
                StreamStopped(this, e);
        }

        /// <summary>
        /// Fired when an audio stream has buffered fully and is ready to play.
        /// </summary>
        public event EventHandler<RaopEventArgs> StreamReady;
        protected virtual void OnStreamReady(RaopEventArgs e)
        {
            if (StreamReady != null)
                StreamReady(this, e);
        }

        /// <summary>
        /// Fired when new playback progress information has been received from the client.
        /// </summary>
        public event EventHandler<PlaybackProgressChangedEventArgs> PlaybackProgressChanged;
        protected virtual void OnPlaybackProgressChanged(PlaybackProgressChangedEventArgs e)
        {
            if (PlaybackProgressChanged != null)
                PlaybackProgressChanged(this, e);
        }

        /// <summary>
        /// Fired when new metadata has been received from the client.
        /// </summary>
        public event EventHandler<MetaDataChangedEventArgs> MetaDataChanged;
        protected virtual void OnMetaDataChanged(MetaDataChangedEventArgs e)
        {
            if (MetaDataChanged != null)
                MetaDataChanged(this, e);
        }

        /// <summary>
        /// Fired when new artwork has been received from the client.
        /// </summary>
        public event EventHandler<ArtwokChangedEventArgs> ArtworkChanged;
        protected virtual void OnArtworkChanged(ArtwokChangedEventArgs e)
        {
            if (ArtworkChanged != null)
                ArtworkChanged(this, e);
        }

        /// <summary>
        /// Fired when new volume information has been received from the client.
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        protected virtual void OnVolumeChanged(VolumeChangedEventArgs e)
        {
            if (VolumeChanged != null)
                VolumeChanged(this, e);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start broadcasting and listening for new connections
        /// </summary>
        public void Start()
        {
            //We need the mac address as part of the authentication response
            //and it also prefixes the name of the server in bonjour
            if (macAddress == null || macAddress.Length == 0)
                macAddress = Utils.GetMacAddress();
            if (macAddress == null)
                return;

            lock (listenerLock)
            {
                if (listener != null)
                    Stop();

                Logger.Info("RAOP Server: Starting - MAC address {0}, port {1}", macAddress.HexStringFromBytes(":"), port);
                //Start broadcasting the bonjour service
                publishBonjour();
                startListener();
                remoteHandler = new RemoteHandler();
                remoteHandler.Start();
            }
        }

        /// <summary>
        /// Stops broadcasting and listening for new connections
        /// </summary>
        public void Stop()
        {
            Logger.Info("RAOP Server: Stopping");
            lock (listenerLock)
            {
                if (bonjour != null)
                {
                    bonjour.Stop();
                    bonjour = null;
                }
                if (remoteHandler != null)
                {
                    remoteHandler.Stop();
                    remoteHandler = null;
                }

                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }
            }
            RaopSession session;
            lock (sessionLock)
            {
                session = currentSession;
                currentSession = null;
            }
            if (session != null)
                session.Close();

            Logger.Info("RAOP Server: Stopped");
        }

        /// <summary>
        /// Stops the current audio stream.
        /// </summary>
        public void StopCurrentSession()
        {
            RaopSession session = null;
            lock (sessionLock)
            {
                session = currentSession;
                currentSession = null;
            }

            if (session != null)
                session.Close();
        }

        /// <summary>
        /// Gets a System.IO.Stream wrapper for the audio stream.
        /// </summary>
        /// <param name="streamType"></param>
        /// <returns></returns>
        public AudioBufferStream GetStream(StreamType streamType)
        {
            lock (sessionLock)
                if (currentSession != null)
                    return currentSession.GetStream(streamType);
            return null;
        }

        public void GetBufferLevel(out int current, out int max)
        {
            lock (sessionLock)
                if (currentSession != null)
                {
                    currentSession.GetBufferLevel(out current, out max);
                    return;
                }

            current = 0;
            max = 0;
        }

        /// <summary>
        /// Sends a playback command to the client.
        /// </summary>
        /// <param name="command">The playback command to send.</param>
        public void SendCommand(RemoteCommand command)
        {
            RemoteServerInfo serverInfo = null;
            lock (sessionLock)
                if (currentSession != null)
                    serverInfo = currentSession.RemoteServerInfo;

            if (serverInfo != null)
            {
                lock (listenerLock)
                {
                    if (remoteHandler != null)
                        remoteHandler.SendCommand(serverInfo, command);
                }
            }
        }

        #endregion

        #region Private Methods

        void publishBonjour()
        {
            bonjour = new RaopEmitter(name.ComputerNameIfNullOrEmpty(), macAddress.HexStringFromBytes(), port, modelName, !string.IsNullOrEmpty(password));
            bonjour.Publish();
        }

        void startListener()
        {
            //Create a TCPListener on the specified port
            listener = new HttpConnectionHandler(IPAddress.Any, port);
            listener.SocketAccepted += listener_SocketAccepted;
            if (!listener.Start())
                Stop();
        }

        void listener_SocketAccepted(object sender, SocketAcceptedEventArgs e)
        {
            RaopSession oldSession;
            lock (sessionLock)
            {
                oldSession = currentSession;
                currentSession = null;
            }

            if (oldSession != null)
            {
                Logger.Info("RAOP Server: Stopping current connection, new connection requested");
                oldSession.Close();
            }

            lock (sessionLock)
            {
                if (currentSession == null)
                {
                    //Start a responder to handle the connection
                    e.Handled = true;
                    RaopSession raop = new RaopSession(macAddress, e.Socket, password);
                    raop.UDPPort = AudioPort;
                    raop.BufferSize = AudioBufferSize;
                    raop.StreamStarting += streamStarting;
                    raop.Closed += streamStopped;
                    raop.StreamReady += raop_StreamReady;
                    raop.ProgressChanged += raop_ProgressChanged;
                    raop.MetaDataChanged += raop_MetaDataChanged;
                    raop.ArtworkChanged += raop_ArtworkChanged;
                    raop.VolumeChanged += raop_VolumeChanged;
                    raop.Start();
                    currentSession = raop;
                }
            }
        }

        void streamStarting(object sender, RaopEventArgs e)
        {
            OnStreamStarting(e);
        }

        void streamStopped(object sender, EventArgs e)
        {
            OnStreamStopped(e);
            lock (sessionLock)
                if (sender == currentSession)
                    currentSession = null;
        }

        void raop_StreamReady(object sender, RaopEventArgs e)
        {
            OnStreamReady(e);
        }

        void raop_ProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            OnPlaybackProgressChanged(e);
        }

        void raop_MetaDataChanged(object sender, MetaDataChangedEventArgs e)
        {
            OnMetaDataChanged(e);
        }

        void raop_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            OnArtworkChanged(e);
        }

        void raop_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            OnVolumeChanged(e);
        }

        #endregion
    }
}