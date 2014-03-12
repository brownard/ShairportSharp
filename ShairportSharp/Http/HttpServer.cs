using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Http
{
    abstract class HttpServer : IDisposable
    {
        #region Variables

        static readonly Encoding encoding = Encoding.ASCII;
        object socketLock = new object();
        Socket socket;
        BufferedStream inputStream;
        NetworkStream outputStream;
        List<byte> byteBuffer;
        byte[] buffer;
        
        #endregion

        #region Constructor

        public HttpServer(Socket socket)
        {
            this.socket = socket;
            inputStream = new BufferedStream(new NetworkStream(socket));
            outputStream = new NetworkStream(socket);
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

        public void Send(HttpResponse response)
        {
            lock (socketLock)
            {
                if (socket != null)
                {
                    try
                    {
                        byte[] txtBytes = response.GetBytes();
                        outputStream.Write(txtBytes, 0, txtBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("HttpServer: Error sending response -", ex);
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

                HttpRequest parsedPacket;
                //Try and parse a complete packet from our data
                while (HttpRequest.TryParse(byteBuffer.ToArray(), out parsedPacket))
                {
                    //Logger.Debug("RAOPSession:\r\n{0}", parsedPacket.ToString());
                    //remove packet from our buffer
                    byteBuffer.RemoveRange(0, parsedPacket.Length);
                    HttpResponse response;
                    try
                    {
                        //get the response
                        response = HandleRequest(parsedPacket);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("HttpServer: Exception handling message -", ex);
                        response = null;
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

        #region IDisposable

        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}
