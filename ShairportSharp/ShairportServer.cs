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

namespace ShairportSharp
{
    /// <summary>
    /// The main class used for broadcasting and listening for new connections
    /// </summary>
    public class ShairportServer
    {
        #region Consts

        const int DEFAULT_PORT = 5000;

        #endregion

        #region Variables

        BonjourEmitter bonjour;
        byte[] macAddress;
        object listenerLock = new object();
        HttpConnectionHandler listener = null;
        object sessionLock = new object();
        RaopSession currentSession = null;
        RemoteHandler remoteHandler;
        RemoteServerInfo currentRemote;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a server using the machine name and no password
        /// </summary>
        public ShairportServer() { }

        /// <summary>
        /// Create a server with the specified name and optional password
        /// </summary>
        /// <param name="name">The broadcasted name of the server</param>
        /// <param name="password">The password to use (use null or empty for no password)</param>
        public ShairportServer(string name, string password = null)
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

        int port = DEFAULT_PORT;
        /// <summary>
        /// The port to listen on for RTSP packets. Defaults to 5000.
        /// </summary>
        public int Port
        {
            get { return port; }
            set { port = value.GetValidPortNumber(DEFAULT_PORT); } 
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

        /// <summary>
        /// Whether to allow new connection requests to stop an existing connection.
        /// </summary>
        public bool AllowSubsequentConnections
        {
            get;
            set;
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when an audio stream has been started but not yet buffered fully.
        /// </summary>
        public event EventHandler StreamStarting;
        protected virtual void OnStreamStarting(EventArgs e)
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
        public event EventHandler<EventArgs> StreamReady;
        protected virtual void OnStreamReady(EventArgs e)
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

        /// <summary>
        /// Fired when the audio buffer level has changed.
        /// </summary>
        public event EventHandler<BufferChangedEventArgs> AudioBufferChanged;
        protected virtual void OnAudioBufferChanged(BufferChangedEventArgs e)
        {
            if (AudioBufferChanged != null)
                AudioBufferChanged(this, e);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Sets the interface to use for logging.
        /// </summary>
        /// <param name="logger">The ILog implementation</param>
        public static void SetLogger(ILog logger)
        {
            Logger.SetLogger(logger);
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
            macAddress = Utils.GetMacAddress();
            if (macAddress == null)
                return;

            Logger.Info("Server starting: MAC address {0}, port {1}", macAddress.StringFromAddressBytes(), port);
            lock (listenerLock)
            {
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
            Logger.Info("Server stopping");
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
            lock (sessionLock)
            {
                if (currentSession != null)
                {
                    currentSession.Close();
                    currentSession = null;
                }
            }
            Logger.Info("Server stopped");
        }

        /// <summary>
        /// Stops the current audio stream.
        /// </summary>
        public void StopCurrentSession()
        {
            lock (sessionLock)
                if (currentSession != null)
                {
                    currentSession.Close();
                    currentSession = null;
                }
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

        /// <summary>
        /// Sends a playback command to the client.
        /// </summary>
        /// <param name="command">The playback command to send.</param>
        public void SendCommand(RemoteCommand command)
        {
            RemoteServerInfo serverInfo;
            lock (sessionLock)
                serverInfo = currentRemote;

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
            Logger.Debug("Starting bonjour service");
            bonjour = new RaopEmitter(name.ComputerNameIfNullOrEmpty(), macAddress.StringFromAddressBytes(), port, !string.IsNullOrEmpty(password));
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
            lock (sessionLock)
            {
                if (currentSession != null)
                {
                    if (AllowSubsequentConnections)
                    {
                        Logger.Info("Stopping current connection, new connection requested");
                        currentSession.Close();
                        currentRemote = null;
                    }
                    else
                    {
                        Logger.Info("Rejecting new connection, existing connection exists");
                        return;
                    }
                }
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
                raop.BufferChanged += raop_BufferChanged;
                raop.RemoteFound += raop_RemoteFound;
                raop.Start();
                currentSession = raop;
            }
        }

        void streamStarting(object sender, EventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    OnStreamStarting(e);
            }
        }

        void streamStopped(object sender, EventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                {
                    OnStreamStopped(e);
                    currentSession = null;
                }
            }
        }

        void raop_StreamReady(object sender, EventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    OnStreamReady(e);
            }
        }

        void raop_ProgressChanged(object sender, PlaybackProgressChangedEventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    OnPlaybackProgressChanged(e);
            }
        }

        void raop_MetaDataChanged(object sender, MetaDataChangedEventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    OnMetaDataChanged(e);
            }
        }

        void raop_ArtworkChanged(object sender, ArtwokChangedEventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    OnArtworkChanged(e);
            }
        }

        void raop_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    OnVolumeChanged(e);
            }
        }
                
        void raop_BufferChanged(object sender, BufferChangedEventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    OnAudioBufferChanged(e);
            }
        }

        void raop_RemoteFound(object sender, RemoteInfoFoundEventArgs e)
        {
            lock (sessionLock)
            {
                if (sender == currentSession)
                    currentRemote = e.RemoteServer;
            }
        }

        #endregion
    }
}