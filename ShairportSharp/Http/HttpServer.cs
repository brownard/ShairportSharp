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
                    Logger.Debug("RtspServer: Listening for new RTSP packets");
                    inputStream.BeginRead(buffer, 0, buffer.Length, onInputReadComplete, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("RtspServer: Failed to start -", ex);
                    inputStream.Close();
                    outputStream.Close();
                    socket.Close();
                    socket = null;
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
                    //get the response
                    HttpResponse response = HandleRequest(parsedPacket);
                    lock (socketLock)
                    {
                        if (socket != null)
                        {
                            //send it
                            byte[] txtBytes = encoding.GetBytes(response.ToString());
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
                Logger.Debug("RtspServer: IO Exception, stream probably closed");
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("RtspServer: Error receiving RTSP packets -", ex);
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
                    Logger.Debug("RtspServer: Closed socket");
                }
            }
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
