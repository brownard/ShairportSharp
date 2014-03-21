using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ShairportSharp.Helpers;

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

        string password;
        string digestRealm;
        protected string nonce;
        bool isAuthorised = true;
        
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
                isAuthorised = false;
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
                        Logger.Error("HttpServer: Exception request\r\n{0}", parsedMessage);
                    }

                    if (response != null)
                    {
                        Send(response);
                        if (response["Connection"] == "close")
                        {
                            Close();
                            return;
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
            if (isAuthorised)
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
                        if (checkDigestHash(username, realm, password, method, uri, nonce, resp))
                        {
                            isAuthorised = true;
                            return true;
                        }
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

        static bool checkDigestHash(string username, string realm, string password, string method, string uri, string nonce, string hashToCheck)
        {
            //Airtunes hashes to uppercase, Airplay hashes to lowercase, try and detect which is used
            CaseType caseType = hashToCheck.GetCasing();
            bool result = hashToCheck == getDigestHash(username, realm, password, method, uri, nonce, caseType);
            //Edge case when hash is all digits so we cannot detect casing, try both
            if (!result && caseType == CaseType.Digit)
                result = hashToCheck == getDigestHash(username, realm, password, method, uri, nonce, CaseType.Lower);
            return result;
        }

        static string getDigestHash(string username, string realm, string password, string method, string uri, string nonce, CaseType caseType)
        {
            string hash1 = md5Hash(username + ":" + realm + ":" + password, caseType);
            string hash2 = md5Hash(method + ":" + uri, caseType);
            return md5Hash(hash1 + ":" + nonce + ":" + hash2, caseType);
        }

        /// <summary>
        /// Generates an MD5 hash from a string 
        /// </summary>
        /// <param name="plainText">The string to use to generate the hash</param>
        /// <returns>The MD5 hash</returns>
        static string md5Hash(string input, CaseType caseType)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            string format = caseType == CaseType.Lower ? "x2" : "X2";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString(format));
            return sb.ToString();
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
