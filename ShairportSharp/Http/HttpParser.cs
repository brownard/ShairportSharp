using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Math;

namespace ShairportSharp.Http
{
    abstract class HttpParser : IDisposable
    {
        #region Variables

        static readonly Regex authPattern = new Regex("Digest username=\"(.*)\", realm=\"(.*)\", nonce=\"(.*)\", uri=\"(.*)\", response=\"(.*)\"", RegexOptions.Compiled);
        static readonly Encoding encoding = Encoding.ASCII;
        object socketLock = new object();
        Socket socket;
        BufferedStream inputStream;
        NetworkStream outputStream;
        List<byte> byteBuffer;
        byte[] buffer;

        string password = null;
        string digestRealm = null;
        protected string nonce = null;
        
        #endregion

        #region Constructor

        public HttpParser(Socket socket)
        {
            this.socket = socket;
            inputStream = new BufferedStream(new NetworkStream(socket));
            outputStream = new NetworkStream(socket);
        }

        public HttpParser(Socket socket, string password, string digestRealm)
            : this(socket)
        {
            if (!string.IsNullOrEmpty(password))
            {
                this.password = password;
                this.digestRealm = digestRealm;
                nonce = createRandomString();
            }
        }

        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the client has disconnected
        /// </summary>
        public event EventHandler Closed;
        protected virtual void OnClosed(EventArgs e)
        {
            if (Closed != null)
                Closed(this, e);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts listening and handling requests from the client
        /// </summary>
        public void Start()
        {
            byteBuffer = new List<byte>();
            buffer = new byte[65536];

            lock (socketLock)
            {
                if (socket == null)
                    return;
                try
                {
                    //Logger.Debug("HttpServer: Listening for new requests");
                    inputStream.BeginRead(buffer, 0, buffer.Length, onInputReadComplete, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("HttpServer: Failed to start -", ex);
                    inputStream.Close();
                    outputStream.Close();
                    socket.Close();
                    socket = null;
                }
            }
        }

        public void Send(HttpMessage message)
        {
            lock (socketLock)
            {
                if (socket != null)
                {
                    try
                    {
                        byte[] txtBytes = message.GetBytes();
                        outputStream.Write(txtBytes, 0, txtBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("HttpServer: Error sending message -", ex);
                    }
                }
            }
        }

        void onInputReadComplete(IAsyncResult result)
        {
            try
            {
                int read;
                lock (socketLock)
                {
                    if (socket != null)
                        read = inputStream.EndRead(result);
                    else
                        return;
                }

                if (read < 1)
                    Close();

                for (int x = 0; x < read; x++)
                    byteBuffer.Add(buffer[x]);

                HttpMessage parsedMessage;
                int parsedLength;
                //Try and parse a complete packet from our data
                while (HttpMessage.TryParse(byteBuffer.ToArray(), out parsedMessage, out parsedLength))
                {
                    //Logger.Debug("RAOPSession:\r\n{0}", parsedPacket.ToString());
                    //remove packet from our buffer
                    byteBuffer.RemoveRange(0, parsedLength);
                    HttpResponse response = null;
                    try
                    {
                        if (parsedMessage.MessageType == HttpMessageType.Request)
                            response = HandleRequest((HttpRequest)parsedMessage);
                        else
                            HandleResponse((HttpResponse)parsedMessage);

                    }
                    catch (Exception ex)
                    {
                        Logger.Error("HttpServer: Exception handling message -", ex);
                    }

                    if (response != null)
                    {
                        lock (socketLock)
                        {
                            if (socket != null)
                            {
                                //send it
                                byte[] txtBytes = response.GetBytes();
                                outputStream.Write(txtBytes, 0, txtBytes.Length);
                            }
                            else
                            {
                                return;
                            }
                        }

                        if (response["Connection"] == "close")
                        {
                            Close();
                        }
                    }
                }

                lock (socketLock)
                {
                    if (socket != null)
                    {
                        inputStream.BeginRead(buffer, 0, buffer.Length, onInputReadComplete, null);
                    }
                }
            }
            catch (IOException)
            {
                Logger.Debug("HttpServer: IO Exception, socket probably closed");
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("HttpServer: Error receiving requests -", ex);
                Close();
            }
        }

        #endregion

        #region Virtual Methods

        protected abstract HttpResponse HandleRequest(HttpRequest request);
        protected virtual void HandleResponse(HttpResponse response) { }

        protected virtual bool IsAuthorised(HttpRequest request)
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

                    if (nonce == this.nonce && realm == this.digestRealm)
                    {
                        string hash1 = md5Hash(username + ":" + realm + ":" + password).ToUpper();
                        string hash2 = md5Hash(method + ":" + uri).ToUpper();
                        string hash = md5Hash(hash1 + ":" + nonce + ":" + hash2).ToUpper();

                        if (hash == resp)
                            return true;
                    }
                }
            }
            //Logger.Info("HTTPServer: Client authorisation falied");
            return false;
        }

        /// <summary>
        /// Stops listening for new packets and closes the underlying socket.
        /// </summary>
        public virtual void Close()
        {
            lock (socketLock)
            {
                if (socket != null)
                {
                    inputStream.Close();
                    outputStream.Close();
                    socket.Close();
                    socket = null;
                    Logger.Debug("HttpServer: Closed socket");
                }
            }
            OnClosed(EventArgs.Empty);
        }

        #endregion

        #region Utils

        static string createRandomString()
        {
            byte[] buffer = new byte[16];
            new Random().NextBytes(buffer);
            return Convert.ToBase64String(buffer);
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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}
