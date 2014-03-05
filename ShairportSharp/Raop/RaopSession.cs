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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ShairportSharp.Audio;
using ShairportSharp.Http;
using ShairportSharp.Remote;

namespace ShairportSharp.Raop
{    
    class RaopSession : HttpServer
    {        
        #region Private Variables

        static readonly Regex authPattern = new Regex("Digest username=\"(.*)\", realm=\"(.*)\", nonce=\"(.*)\", uri=\"(.*)\", response=\"(.*)\"", RegexOptions.Compiled);

        const string DIGEST_REALM = "raop";
        string password;
        string nonce;        
        byte[] hardwareAddress;
        byte[] localEndpoint;

        object requestLock = new object();
        AudioServer audioServer; // Audio listener

        RemoteServerInfo remoteServerInfo = null;
        
        int[] fmtp; //audio stream info
        byte[] aesKey; //audio data encryption key
        byte[] aesIV; //IV

        #endregion

        #region Events
        /// <summary>
        /// Fired when the client begins streaming audio
        /// </summary>
        public event EventHandler StreamStarting;
        protected virtual void OnStreamStarting(EventArgs e)
        {
            if (StreamStarting != null)
                StreamStarting(this, e);
        }      
        
        /// <summary>
        /// Fired when the audio buffer has enough data to play
        /// </summary>
        public event EventHandler StreamReady;
        protected virtual void OnStreamReady(EventArgs e)
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

        public event EventHandler<BufferChangedEventArgs> BufferChanged;
        protected virtual void OnBufferChanged(BufferChangedEventArgs e)
        {
            if (BufferChanged != null)
                BufferChanged(this, e);
        }

        public event EventHandler<RemoteInfoFoundEventArgs> RemoteFound;
        protected virtual void OnRemoteFound(RemoteInfoFoundEventArgs e)
        {
            if (RemoteFound != null)
                RemoteFound(this, e);
        }

        #endregion

        #region Constructor

        public RaopSession(byte[] hardwareAddress, Socket socket, string password = null)
            : base(socket)
        {
            this.hardwareAddress = hardwareAddress;
            localEndpoint = ((IPEndPoint)socket.LocalEndPoint).Address.GetAddressBytes();

            if (!string.IsNullOrEmpty(password))
            {
                this.password = password;
                nonce = createRandomString();
            }
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
                lock (requestLock)
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
            lock (requestLock)
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
            HttpResponse response = new HttpResponse();
            //iTunes wants to know we are legit before it will even authenticate
            string challengeResponse = getChallengeResponse(request);
            if (challengeResponse != null)
                response.SetHeader("Apple-Response", challengeResponse);

            if (isAuthorised(request))
            {
                response.Status = "RTSP/1.0 200 OK";
                response.SetHeader("Audio-Jack-Status", "connected; type=analog");
                response.SetHeader("CSeq", request.GetHeader("CSeq"));
            }
            else
            {
                response.Status = "RTSP/1.0 401 UNAUTHORIZED";
                response.SetHeader("WWW-Authenticate", string.Format("Digest realm=\"{0}\" nonce=\"{1}\"", DIGEST_REALM, nonce));
                response.SetHeader("Method", "DENIED");
                return response;
            }

            string requestType = request.Method;
            lock (requestLock)
            {
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
                    if (setupAudioServer(request))
                    {
                        response.SetHeader("Transport", request.GetHeader("Transport") + ";server_port=" + audioServer.Port);
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
                    if (audioServer != null)
                    {
                        audioServer.Buffer.Flush();
                    }
                }
                else if (requestType == "TEARDOWN")
                {
                    Logger.Debug("RAOPSession: TEARDOWN received");
                    response.SetHeader("Connection", "close");
                }
                else if (requestType == "SET_PARAMETER")
                {
                    string contentType = request.GetHeader("Content-Type");
                    if (contentType == "application/x-dmap-tagged")
                    {
                        Logger.Debug("RAOPSession: Received metadata");
                        OnMetaDataChanged(new MetaDataChangedEventArgs(new DmapData(request.Content)));
                    }
                    else if (contentType.StartsWith("image/"))
                    {
                        Logger.Debug("RAOPSession: Received cover art");
                        OnArtworkChanged(new ArtwokChangedEventArgs(request.Content, contentType));
                    }
                    else
                    {
                        handleParameterString(request.GetContentString());
                    }
                }
                else
                {
                    //Unsupported
                    Logger.Debug("RAOPSession: Unsupported request type: {0}", requestType);
                }
            }
            return response;
        }

        #endregion

        #region Public Methods

        public AudioBufferStream GetStream(StreamType streamType)
        {
            lock (requestLock)
            {
                if (audioServer != null)
                    return audioServer.GetStream(streamType);
            }
            return null;
        }
        
        public int BufferedPercent()
        {
            lock (requestLock)
            {
                if (audioServer != null)
                {
                    return audioServer.Buffer.CurrentBufferSize * 100 / audioServer.Buffer.MaxBufferSize;
                }
            }
            return 0;
        }
        
        #endregion

        #region Private Methods
        
        bool isAuthorised(HttpRequest request)
        {
            if (string.IsNullOrEmpty(password))
                return true;

            string authRaw = request.GetHeader("Authorization");
            if (authRaw != null)
            {
                Match authMatch = authPattern.Match(authRaw);
                if (authMatch.Success)
                {
                    string username = authMatch.Groups[1].Value;
                    string realm = authMatch.Groups[2].Value;
                    string nonce = authMatch.Groups[3].Value;
                    string uri = authMatch.Groups[4].Value;
                    string resp = authMatch.Groups[5].Value;
                    string method = request.Method;

                    string hash1 = md5Hash(username + ":" + realm + ":" + password).ToUpper();
                    string hash2 = md5Hash(method + ":" + uri).ToUpper();
                    string hash = md5Hash(hash1 + ":" + nonce + ":" + hash2).ToUpper();

                    // Check against password
                    if (hash == resp && nonce == this.nonce)
                        return true;
                }
            }
            //Logger.Info("RAOPSession: Client authorisation falied");
            return false;
        }

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
            foreach (Match m in Regex.Matches(request.GetContentString(), "^a=([^:]+):(.+)", RegexOptions.Multiline))
            {
                if (m.Groups[1].Value == "fmtp")
                {
                    // Parse FMTP as array
                    string[] temp = m.Groups[2].Value.Split(' ');
                    fmtp = new int[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        fmtp[i] = int.Parse(temp[i]);
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

        void initRemoteHandler(HttpRequest request)
        {
            string dacpId = request.GetHeader("DACP-ID");
            string activeRemote = request.GetHeader("Active-Remote");
            if (dacpId != null && activeRemote != null)
            {
                remoteServerInfo = new RemoteServerInfo(dacpId, activeRemote);
                Logger.Debug("RAOPSession: Received remote info - DacpId : '{0}', ActiveRemote : '{1}'", dacpId, activeRemote);
                OnRemoteFound(new RemoteInfoFoundEventArgs(remoteServerInfo));
            }
        }

        bool setupAudioServer(HttpRequest request)
        {
            if (fmtp == null)
            {
                Logger.Warn("RAOPSession: Unable to create Audio Server, received SETUP before ANNOUNCE");
                return false;
            }

            int controlPort = 0;
            int timingPort = 0;

            string value = request.GetHeader("Transport");
            // Control port
            Match m = Regex.Match(value, @";control_port=(\d+)");
            if (m.Success)
            {
                controlPort = int.Parse(m.Groups[1].Value);
                Logger.Debug("RAOPSession: Set client control port to {0}", controlPort);
            }
            // Timing port
            m = Regex.Match(value, @";timing_port=(\d+)");
            if (m.Success)
            {
                timingPort = int.Parse(m.Groups[1].Value);
                Logger.Debug("RAOPSession: Set client timing port to {0}", timingPort);
            }

            OnStreamStarting(EventArgs.Empty);

            AudioSession session = new AudioSession(aesIV, aesKey, fmtp, controlPort, timingPort, BufferSize);
            audioServer = new AudioServer(session, UDPPort);
            audioServer.Buffer.BufferReady += (o, e) => OnStreamReady(EventArgs.Empty);
            audioServer.Buffer.BufferChanged += (o, e) => OnBufferChanged(e);
            if (audioServer.Start())
            {
                Logger.Debug("RAOPSession: Started new Audio Server");
                return true;
            }
            else
            {
                Logger.Error("RAOPSession: Error starting Audio Server");
            }
            return false;
        }

        void handleParameterString(string parameterString)
        {
            Dictionary<string, string> paramaters = Utils.ParseTextParameters(parameterString);
            foreach (KeyValuePair<string, string> keyVal in paramaters)
            {
                string paramName = keyVal.Key;
                string paramVal = keyVal.Value;
                if (paramName == "volume")
                {
                    double volume = double.Parse(paramVal);
                    Logger.Debug("RAOPSession: Set Volume: {0}", volume);
                    OnVolumeChanged(new VolumeChangedEventArgs(volume));
                }
                else if (paramName == "progress")
                {
                    Match m = Regex.Match(paramVal, @"(\d+)/(\d+)/(\d+)");
                    if (m.Success)
                    {
                        uint start = uint.Parse(m.Groups[1].Value);
                        uint current = uint.Parse(m.Groups[2].Value);
                        uint stop = uint.Parse(m.Groups[3].Value);
                        Logger.Debug("RAOPSession: Set Progress: {0} / {1} / {2}", start, current, stop);
                        OnProgressChanged(new PlaybackProgressChangedEventArgs(start, stop, current));
                    }
                }
            }
        }

        /// <summary>
        /// Generates an MD5 hash from a string 
        /// </summary>
        /// <param name="plainText">The string to use to generate the hash</param>
        /// <returns>The MD5 hash</returns>
        static string md5Hash(string plainText)
        {
            String hashtext = "";
            try
            {
                HashAlgorithm md = new MD5CryptoServiceProvider();
                byte[] txtBytes = Encoding.ASCII.GetBytes(plainText);
                //md.TransformFinalBlock(txtBytes, 0, txtBytes.Length);
                byte[] digest = md.ComputeHash(txtBytes);

                BigInteger bigInt = new BigInteger(1, digest);
                hashtext = bigInt.ToString(16);

                // Now we need to zero pad it if you actually want the full 32 chars.
                while (hashtext.Length < 32)
                {
                    hashtext = "0" + hashtext;
                }
            }
            catch { }
            return hashtext;
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
                PemReader pemReader = new PemReader(new StringReader(Constants.RSA_KEY));
                AsymmetricCipherKeyPair pObj = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                IBufferedCipher cipher = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
                cipher.Init(true, pObj.Private);
                return cipher.DoFinal(array);

            }
            catch { }
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
                PemReader pemReader = new PemReader(new StringReader(Constants.RSA_KEY));
                AsymmetricCipherKeyPair pObj = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                IBufferedCipher cipher = CipherUtilities.GetCipher("RSA/NONE/OAEPPadding");
                cipher.Init(false, pObj.Private);
                return cipher.DoFinal(array);
            }
            catch { }
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

        static string createRandomString()
        {
            byte[] buffer = new byte[16];
            new Random().NextBytes(buffer);
            return Convert.ToBase64String(buffer);
        }
        #endregion
    }
}
