using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ShairportSharp.Helpers;
using ShairportSharp.Base;

namespace ShairportSharp.Http
{
    public class ClosedEventArgs : EventArgs
    {
        public bool ManualClose { get; set; }
    }

    public abstract class HttpParser : ISocketHandler, IDisposable
    {
        #region Variables

        static readonly Regex authPattern = new Regex("Digest username=\"(.*)\", realm=\"(.*)\", nonce=\"(.*)\", uri=\"(.*)\", response=\"(.*)\"", RegexOptions.Compiled);
        static readonly Encoding encoding = Encoding.ASCII;
        object socketLock = new object();
        Socket socket;
        BufferedStream inputStream;
        protected HttpMessageBuffer messageBuffer;
        protected NetworkStream outputStream;

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
        public event EventHandler<ClosedEventArgs> Closed;
        protected virtual void OnClosed(ClosedEventArgs e)
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
            messageBuffer = messageBuffer ?? new HttpMessageBuffer();
            messageBuffer.MessageReceived += messageBuffer_MessageReceived;
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
                    Logger.Error("HttpParser: Failed to start -", ex);
                    inputStream.Close();
                    outputStream.Close();
                    socket.Close();
                    socket = null;
                }
            }
        }

        public void Send(HttpMessage message, bool async = false)
        {
            lock (socketLock)
            {
                if (socket != null)
                {
                    try
                    {
                        byte[] txtBytes = message.GetBytes();
                        if (async)
                        {
                            outputStream.BeginWrite(txtBytes, 0, txtBytes.Length, onSendComplete, null);
                        }
                        else
                        {
                            outputStream.Write(txtBytes, 0, txtBytes.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("HttpParser: Error sending message -", ex);
                    }
                }
            }
        }

        void onSendComplete(IAsyncResult result)
        {
            try
            {
                lock (socketLock)
                {
                    if (socket != null)
                    {
                        outputStream.EndWrite(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("HttpParser: Error ending send message -", ex);
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
                {
                    Close(false);
                    return;
                }

                messageBuffer.Write(buffer, 0, read);
                
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
                Logger.Debug("HttpParser: IO Exception, socket probably closed");
                Close(false);
            }
            catch (Exception ex)
            {
                Logger.Error("HttpParser: Error receiving requests -", ex);
                Close(false);
            }
        }

        void messageBuffer_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            HttpResponse response = null;
            try
            {
                if (e.Message.MessageType == HttpMessageType.Request)
                    response = HandleRequest((HttpRequest)e.Message);
                else
                    HandleResponse((HttpResponse)e.Message);
            }
            catch (Exception ex)
            {
                Logger.Error("HttpParser: Exception handling message -", ex);
                Logger.Error("HttpParser: Exception request\r\n{0}", e.Message);
            }

            if (response != null)
            {
                Send(response);
                if (response["Connection"] == "close")
                {
                    Close(false);
                    return;
                }
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
            Close(true);
        }

        protected virtual void Close(bool manualClose)
        {
            bool closed = false;
            lock (socketLock)
            {
                if (socket != null)
                {
                    inputStream.Close();
                    outputStream.Close();
                    socket.Close();
                    socket = null;
                    closed = true;
                    Logger.Debug("HttpParser: Closed socket", Environment.StackTrace);
                }
            }
            if (closed)
                OnClosed(new ClosedEventArgs() { ManualClose = manualClose });
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
            byte[] inputBytes = encoding.GetBytes(input);
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
            Close(false);
        }

        #endregion
    }
}
