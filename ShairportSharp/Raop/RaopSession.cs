using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ShairportSharp.Audio;
using ShairportSharp.Http;
using ShairportSharp.Remote;
using System.Globalization;
using ShairportSharp.Helpers;

namespace ShairportSharp.Raop
{    
    public class RaopSession : HttpParser
    {        
        #region Private Variables
                
        const string DIGEST_REALM = "raop";
        byte[] hardwareAddress;
        byte[] localEndpoint;
        
        object remoteServerLock = new object();
        RemoteServerInfo remoteServerInfo = null;

        object audioServerLock = new object();
        AudioServer audioServer; // Audio listener
        int[] fmtp; //audio stream info
        byte[] aesKey; //audio data encryption key
        byte[] aesIV; //IV

        #endregion

        #region Events
        /// <summary>
        /// Fired when the client begins streaming audio
        /// </summary>
        public event EventHandler<RaopEventArgs> StreamStarting;
        protected virtual void OnStreamStarting(RaopEventArgs e)
        {
            Logger.Debug("RAOPSession: Stream starting");
            if (StreamStarting != null)
                StreamStarting(this, e);
        }      
        
        /// <summary>
        /// Fired when the audio buffer has enough data to play
        /// </summary>
        public event EventHandler<RaopEventArgs> StreamReady;
        protected virtual void OnStreamReady(RaopEventArgs e)
        {
            if (StreamReady != null)
                StreamReady(this, e);
        }

        /// <summary>
        /// Fired when new volume information is received from the client
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        protected virtual void OnVolumeChanged(VolumeChangedEventArgs e)
        {
            if (VolumeChanged != null)
                VolumeChanged(this, e);
        }

        /// <summary>
        /// Fired when new volume information is received from the client
        /// </summary>
        public event EventHandler<VolumeRequestedEventArgs> VolumeRequested;
        protected virtual void OnVolumeRequested(VolumeRequestedEventArgs e)
        {
            if (VolumeRequested != null)
                VolumeRequested(this, e);
        }

        /// <summary>
        /// Fired when new track metadata is received from the client
        /// </summary>
        public event EventHandler<MetaDataChangedEventArgs> MetaDataChanged;
        protected virtual void OnMetaDataChanged(MetaDataChangedEventArgs e)
        {
            if (MetaDataChanged != null)
                MetaDataChanged(this, e);
        }

        /// <summary>
        /// Fired when new artwork is received from the client
        /// </summary>
        public event EventHandler<ArtwokChangedEventArgs> ArtworkChanged;
        protected virtual void OnArtworkChanged(ArtwokChangedEventArgs e)
        {
            if (ArtworkChanged != null)
                ArtworkChanged(this, e);
        }

        /// <summary>
        /// Fired when new playback progress info is received from the client
        /// </summary>
        public event EventHandler<PlaybackProgressChangedEventArgs> ProgressChanged;
        protected virtual void OnProgressChanged(PlaybackProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
        }

        #endregion

        #region Constructor

        public RaopSession(byte[] hardwareAddress, Socket socket, string password = null)
            : base(socket, password, DIGEST_REALM)
        {
            this.hardwareAddress = hardwareAddress;
            localEndpoint = ((IPEndPoint)socket.LocalEndPoint).Address.GetAddressBytes();
        }

        #endregion

        #region Properties

        public int UDPPort
        {
            get;
            set;
        }

        public int BufferSize
        {
            get;
            set;
        }

        public RemoteServerInfo RemoteServerInfo
        {
            get
            {
                lock (remoteServerLock)
                    return remoteServerInfo;
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Stops listening for new packets and closes the underlying socket and audio server.
        /// </summary>
        public override void Close()
        {
            lock (audioServerLock)
            {
                if (audioServer != null)
                {
                    audioServer.Stop();
                    audioServer = null;
                    Logger.Debug("RAOPSession: Stopped audio server");
                }
            }
            base.Close();
        }

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            HttpResponse response = new HttpResponse("RTSP/1.0");
            //iTunes wants to know we are legit before it will even authenticate
            string challengeResponse = getChallengeResponse(request);
            if (challengeResponse != null)
                response.SetHeader("Apple-Response", challengeResponse);

            if (IsAuthorised(request))
            {
                response.Status = "200 OK";
                response.SetHeader("Audio-Jack-Status", "connected; type=analog");
                response.SetHeader("CSeq", request.GetHeader("CSeq"));
            }
            else
            {
                response.Status = "401 UNAUTHORIZED";
                response.SetHeader("WWW-Authenticate", string.Format("Digest realm=\"{0}\" nonce=\"{1}\"", DIGEST_REALM, nonce));
                response.SetHeader("Method", "DENIED");
                return response;
            }

            string requestType = request.Method;
            if (requestType == "OPTIONS")
            {
                response.SetHeader("Public", "ANNOUNCE, SETUP, RECORD, PAUSE, FLUSH, TEARDOWN, OPTIONS, GET_PARAMETER, SET_PARAMETER");
            }
            else if (requestType == "ANNOUNCE")
            {
                initRemoteHandler(request);
                getSessionParams(request);
            }
            else if (requestType == "SETUP")
            {
                int port;
                if (setupAudioServer(request, out port))
                {
                    response.SetHeader("Transport", request.GetHeader("Transport") + ";server_port=" + port);
                    response.SetHeader("Session", "DEADBEEF");
                }
            }
            else if (requestType == "RECORD")
            {
                //        	Headers	
                //        	Range: ntp=0-
                //        	RTP-Info: seq={Note 1};rtptime={Note 2}
                //        	Note 1: Initial value for the RTP Sequence Number, random 16 bit value
                //        	Note 2: Initial value for the RTP Timestamps, random 32 bit value

                response.SetHeader("Audio-Latency", "44100");
            }
            else if (requestType == "FLUSH")
            {
                lock (audioServerLock)
                    if (audioServer != null)
                        audioServer.Buffer.Flush();
            }
            else if (requestType == "TEARDOWN")
            {
                Logger.Debug("RAOPSession: TEARDOWN received");
                response.SetHeader("Connection", "close");
            }
            else if (requestType == "SET_PARAMETER")
            {
                string contentType = request.GetHeader("Content-Type");
                if (contentType == null)
                    Logger.Debug("RAOPSession: Empty Content-Type\r\n{0}", request.ToString());

                if (contentType == "application/x-dmap-tagged")
                {
                    Logger.Debug("RAOPSession: Received metadata");
                    OnMetaDataChanged(new MetaDataChangedEventArgs(new DmapData(request.Content), request.Uri));
                }
                else if (contentType != null && contentType.StartsWith("image/"))
                {
                    Logger.Debug("RAOPSession: Received cover art");
                    OnArtworkChanged(new ArtwokChangedEventArgs(request.Content, contentType, request.Uri));
                }
                else
                {
                    handleParameterString(request);
                }
            }
            else if (requestType == "GET_PARAMETER")
            {
                bool handled = false;
                if (request["Content-Type"] == "text/parameters")
                {
                    if (request.GetContentString().StartsWith("volume"))
                    {
                        handled = true;
                        Logger.Debug("RAOPSession: Client requested volume");
                        VolumeRequestedEventArgs e = new VolumeRequestedEventArgs(request.Uri);
                        OnVolumeRequested(e);
                        response["Content-Type"] = "text/parameters";
                        response.SetContent(string.Format(CultureInfo.InvariantCulture, "volume: {0}", e.Volume));
                    }
                }
                if (!handled)
                    Logger.Debug("Unhandled GET_PARAMETER request\r\n{0}", request.ToString());
            }
            else
            {
                //Unsupported
                Logger.Debug("RAOPSession: Unsupported request type: {0}", requestType);
                Logger.Debug(request.ToString());
            }
            return response;
        }

        #endregion

        #region Public Methods

        public AudioBufferStream GetStream(StreamType streamType)
        {
            lock (audioServerLock)
                if (audioServer != null)
                    return audioServer.GetStream(streamType);
            return null;
        }

        public void GetBufferLevel(out int current, out int max)
        {
            lock (audioServerLock)
                if (audioServer != null)
                {
                    current = audioServer.Buffer.CurrentBufferSize;
                    max = audioServer.Buffer.MaxBufferSize;
                    return;
                }

            current = 0;
            max = 0;
        }
        
        #endregion

        #region Private Methods
        
        string getChallengeResponse(HttpRequest request)
        {
            string challenge = request.GetHeader("Apple-Challenge");
            if (challenge != null)
            {
                Logger.Debug("RAOPSession: Received Apple-Challenge: {0}", challenge);
                List<byte> output = new List<byte>();
                // Challenge
                output.AddRange(decodeBase64(challenge));
                // IP-Address
                output.AddRange(localEndpoint);
                // HW-Addr
                output.AddRange(hardwareAddress);

                // Pad to 32 Bytes
                int padLen = 32 - output.Count;
                for (int i = 0; i < padLen; ++i)
                {
                    output.Add(0x00);
                }

                // RSA
                byte[] crypted = encryptRSA(output.ToArray());
                // Encode64
                string response = Convert.ToBase64String(crypted);
                // Unpad
                response = response.Replace("=", "").Replace("\r", "").Replace("\n", "");
                Logger.Debug("RAOPSession: Created Apple-Response: {0}", response);
                return response;
            }
            return null;
        }

        void getSessionParams(HttpRequest request)
        {
            MatchCollection matches = Regex.Matches(request.GetContentString(), "^a=([^:]+):(.+)", RegexOptions.Multiline);
            lock (audioServerLock)
            {
                foreach (Match m in matches)
                {
                    if (m.Groups[1].Value == "fmtp")
                    {
                        // Parse FMTP as array
                        string[] temp = m.Groups[2].Value.Split(' ');
                        fmtp = new int[temp.Length];
                        for (int i = 0; i < temp.Length; i++)
                            fmtp[i] = int.Parse(temp[i], CultureInfo.InvariantCulture);
                        Logger.Debug("RAOPSession: Received audio info - '{0}'", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value == "rsaaeskey")
                    {
                        aesKey = decryptRSA(decodeBase64(m.Groups[2].Value));
                        Logger.Debug("RAOPSession: Received AES Key - '{0}'", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value == "aesiv")
                    {
                        aesIV = decodeBase64(m.Groups[2].Value);
                        Logger.Debug("RAOPSession: Received AES IV - '{0}'", m.Groups[2].Value);
                    }
                }
            }
        }

        void initRemoteHandler(HttpRequest request)
        {
            string dacpId = request.GetHeader("DACP-ID");
            string activeRemote = request.GetHeader("Active-Remote");
            if (dacpId != null && activeRemote != null)
            {
                Logger.Debug("RAOPSession: Received remote info - DacpId : '{0}', ActiveRemote : '{1}'", dacpId, activeRemote);
                lock (remoteServerLock)
                    remoteServerInfo = new RemoteServerInfo(dacpId, activeRemote);
            }
        }

        bool setupAudioServer(HttpRequest request, out int port)
        {
            int controlPort = 0;
            int timingPort = 0;
            port = 0;

            string value = request.GetHeader("Transport");
            // Control port
            Match m = Regex.Match(value, @";control_port=(\d+)");
            if (m.Success)
            {
                controlPort = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                Logger.Debug("RAOPSession: Set client control port to {0}", controlPort);
            }
            // Timing port
            m = Regex.Match(value, @";timing_port=(\d+)");
            if (m.Success)
            {
                timingPort = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                Logger.Debug("RAOPSession: Set client timing port to {0}", timingPort);
            }

            lock (audioServerLock)
            {
                if (fmtp == null)
                {
                    Logger.Warn("RAOPSession: Unable to create Audio Server, received SETUP before ANNOUNCE");
                    return false;
                }

                string sessionId = request.Uri;
                AudioSession session = new AudioSession(aesIV, aesKey, fmtp, controlPort, timingPort, BufferSize);
                audioServer = new AudioServer(session, UDPPort);
                audioServer.Buffer.BufferStarted += (o, e) => OnStreamStarting(new RaopEventArgs(sessionId));
                audioServer.Buffer.BufferReady += (o, e) => OnStreamReady(new RaopEventArgs(sessionId));

                if (audioServer.Start())
                {
                    Logger.Debug("RAOPSession: Started new Audio Server");
                    port = audioServer.Port;
                    return true;
                }
                else
                {
                    Logger.Error("RAOPSession: Error starting Audio Server");
                }
            }
            return false;
        }

        void handleParameterString(HttpRequest request)
        {
            Dictionary<string, string> textParamaters = request.GetContentString().AsTextParameters();
            foreach (KeyValuePair<string, string> keyVal in textParamaters)
            {
                string paramName = keyVal.Key;
                string paramVal = keyVal.Value;
                if (paramName == "volume")
                {
                    double volume = double.Parse(paramVal, CultureInfo.InvariantCulture);
                    Logger.Debug("RAOPSession: Set Volume: {0}", volume);
                    OnVolumeChanged(new VolumeChangedEventArgs(volume, request.Uri));
                }
                else if (paramName == "progress")
                {
                    Match m = Regex.Match(paramVal, @"(\d+)/(\d+)/(\d+)");
                    if (m.Success)
                    {
                        uint start = uint.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                        uint current = uint.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                        uint stop = uint.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                        Logger.Debug("RAOPSession: Set Progress: {0} / {1} / {2}", start, current, stop);
                        OnProgressChanged(new PlaybackProgressChangedEventArgs(start, stop, current, request.Uri));
                    }
                }
            }
        }

        /// <summary>
        /// Encrypts the specified bytes using the private key
        /// </summary>
        /// <param name="array">The bytes to encrypt</param>
        /// <returns>The encrypted bytes</returns>
        static byte[] encryptRSA(byte[] array)
        {
            try
            {
                PemReader pemReader = new PemReader(new StringReader(RSAKey.RSA_KEY));
                AsymmetricCipherKeyPair pObj = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                IBufferedCipher cipher = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
                cipher.Init(true, pObj.Private);
                return cipher.DoFinal(array);

            }
            catch(Exception ex) 
            {
                Logger.Error("RAOPSession: Exception encrypting RSA -", ex);
            }
            return null;
        }

        /// <summary>
        /// Decrypts the specified bytes using the private key
        /// </summary>
        /// <param name="array">The bytes to decrypt</param>
        /// <returns>The unencrypted bytes</returns>
        static byte[] decryptRSA(byte[] array)
        {
            try
            {                
                PemReader pemReader = new PemReader(new StringReader(RSAKey.RSA_KEY));
                AsymmetricCipherKeyPair pObj = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                IBufferedCipher cipher = CipherUtilities.GetCipher("RSA/NONE/OAEPPadding");
                cipher.Init(false, pObj.Private);
                return cipher.DoFinal(array);
            }
            catch (Exception ex)
            {
                Logger.Error("RAOPSession: Exception decrypting RSA -", ex);
            }
            return null;
        }

        static byte[] decodeBase64(string encoded)
        {
            //iTunes likes to add line endings to the strings, remove them
            encoded = encoded.Replace("\r", "").Replace("\n", "");
            //re-pad if length not a multiple of 4
            while (encoded.Length % 4 > 0)
                encoded += "=";
            //decode
            return Convert.FromBase64String(encoded);
        }

        #endregion
    }
}
