using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Http
{
    #region SocketAcceptedEventArgs

    class SocketAcceptedEventArgs : EventArgs
    {
        public SocketAcceptedEventArgs(Socket socket)
        {
            Socket = socket;
        }

        public Socket Socket { get; private set; }
        public bool Handled { get; set; }
    }

    #endregion

    class HttpConnectionHandler
    {
        #region Variables

        TcpListener listener;
        IPAddress ipAddress;
        int port;
        object syncRoot = new object();

        #endregion

        #region Ctor

        public HttpConnectionHandler(IPAddress ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        #endregion

        #region Events

        public event EventHandler<SocketAcceptedEventArgs> SocketAccepted;
        protected virtual void OnSocketAccepted(SocketAcceptedEventArgs e)
        {
            if (SocketAccepted != null)
                SocketAccepted(this, e);
        }

        #endregion

        #region Public Methods

        public bool Start()
        {
            //Create a TCPListener on the specified port
            bool result = false;
            lock (syncRoot)
            {
                stopListener();
                Logger.Debug("Starting TCP Listener");
                int tries = 0;
                while (tries < 10 && port < ushort.MaxValue)
                {
                    try
                    {
                        listener = new TcpListener(ipAddress, port);
                        listener.Start(1000);
                    }
                    catch (SocketException)
                    {
                        Logger.Warn("TCP Listener: Failed to start on port {0}, trying next port");
                        stopListener();
                        port++;
                        tries++;
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TCP Listener: Error starting -", ex);
                        stopListener();
                        return false;
                    }
                    result = true;
                    break;
                }
                if (result)
                {
                    try
                    {
                        listener.BeginAcceptSocket(acceptSocket, null);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TCP Listener: Error starting -", ex);
                        stopListener();
                        return false;
                    }
                }
            }
            return result;
        }

        public void Stop()
        {
            lock (syncRoot)
                stopListener();
        }

        #endregion

        #region Private Methods

        void stopListener()
        {
            if (listener != null)
            {
                try
                {
                    listener.Server.Close();
                }
                catch { }
                try
                {
                    listener.Stop();
                    listener = null;
                }
                catch { }
            }
        }

        void acceptSocket(IAsyncResult result)
        {
            Socket socket = null;
            try
            {
                //New connection
                lock (syncRoot)
                {
                    if (listener == null)
                    {
                        //Server stopped
                        Logger.Debug("TCP Listener: Stopped");
                        return;
                    }

                    socket = listener.EndAcceptSocket(result);
                    if (socket != null)
                    {
                        //Logger.Debug("TCP Listener: New connection");
                        SocketAcceptedEventArgs e = new SocketAcceptedEventArgs(socket);
                        OnSocketAccepted(e);
                        if (!e.Handled)
                            socket.Close();
                    }
                    listener.BeginAcceptSocket(acceptSocket, null);
                }
            }
            catch (SocketException)
            {
                Logger.Debug("TCP Listener: SocketException, stream probably closed");
                if (socket != null)
                    socket.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("TCP Listener: Error accepting new connection -", ex);
                if (socket != null)
                    socket.Close();
            }
        }

        #endregion
    }
}
