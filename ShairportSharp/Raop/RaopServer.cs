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
using ShairportSharp.Base;
using ShairportSharp.Helpers;

namespace ShairportSharp.Raop
{
    /// <summary>
    /// The main class used for broadcasting and listening for new connections
    /// </summary>
    public class RaopServer : NamedServer<RaopSession>
    {
        #region Consts

        const int DEFAULT_PORT = 5000;

        #endregion

        #region Variables

        object sessionLock = new object();
        RaopSession currentSession;
        RemoteHandler remoteHandler;

        #endregion

        #region Ctor

        /// <summary>
        /// Create a server using the machine name and no password
        /// </summary>
        public RaopServer() 
        {
            Port = DEFAULT_PORT;
        }

        /// <summary>
        /// Create a server with the specified name and optional password
        /// </summary>
        /// <param name="name">The broadcasted name of the server</param>
        /// <param name="password">The password to use (use null or empty for no password)</param>
        public RaopServer(string name, string password = null)
            : this()
        {
            Name = name;
            Password = password;
        }

        #endregion

        #region Properties

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

        protected override void OnStarting()
        {
            if (MacAddress == null || MacAddress.Length == 0)
            {
                MacAddress = Utils.GetMacAddress();
                if (MacAddress == null)
                    return;
            }

            Emitter = new RaopEmitter(Name.ComputerNameIfNullOrEmpty(), MacAddress.HexStringFromBytes(), Port, ModelName, !string.IsNullOrEmpty(Password));
            remoteHandler = new RemoteHandler();
            remoteHandler.Start();
        }

        protected override void OnStopping()
        {
            if (remoteHandler != null)
            {
                remoteHandler.Stop();
                remoteHandler = null;
            }
            base.OnStopping();
        }

        protected override RaopSession OnSocketAccepted(Socket socket)
        {
            CloseConnections();
            RaopSession raop = new RaopSession(MacAddress, socket, Password);
            raop.UDPPort = AudioPort;
            raop.BufferSize = AudioBufferSize;
            raop.StreamStarting += streamStarting;
            raop.Closed += streamStopped;
            raop.StreamReady += raop_StreamReady;
            raop.ProgressChanged += raop_ProgressChanged;
            raop.MetaDataChanged += raop_MetaDataChanged;
            raop.ArtworkChanged += raop_ArtworkChanged;
            raop.VolumeChanged += raop_VolumeChanged;
            lock (sessionLock)
                currentSession = raop;
            return raop;
        }

        /// <summary>
        /// Stops the current audio stream.
        /// </summary>
        public void StopCurrentSession()
        {
            CloseConnections();
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
                lock (SyncRoot)
                {
                    if (remoteHandler != null)
                        remoteHandler.SendCommand(serverInfo, command);
                }
            }
        }

        #endregion

        #region Private Methods

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