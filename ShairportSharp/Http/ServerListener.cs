using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Http
{
    #region SocketAcceptedEventArgs

    public class SocketAcceptedEventArgs : EventArgs
    {
        public SocketAcceptedEventArgs(Socket socket)
        {
            Socket = socket;
        }

        public Socket Socket { get; private set; }
        public bool Handled { get; set; }
    }

    #endregion

    class ServerListener
    {
        TcpListener listener;
        IPAddress ipAddress;
        int port;
        object syncRoot = new object();

        public ServerListener(IPAddress ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        public event EventHandler<SocketAcceptedEventArgs> SocketAccepted;
        protected virtual void OnSocketAccepted(SocketAcceptedEventArgs e)
        {
            if (SocketAccepted != null)
                SocketAccepted(this, e);
        }

        public bool Start()
        {
            //Create a TCPListener on the specified port
            lock (syncRoot)
            {
                stopListener();
                try
                {
                    Logger.Debug("Starting TCP Listener");
                    listener = new TcpListener(ipAddress, port);
                    listener.Start(1000);
                    listener.BeginAcceptSocket(acceptSocket, null);
                }
                catch (Exception ex)
                {
                    //cleanup
                    Logger.Error("Error starting TCP Listener -", ex);
                    stopListener();
                    return false;
                }
            }
            return true;
        }

        public void Stop()
        {
            lock (syncRoot)
                stopListener();
        }

        void stopListener()
        {
            if (listener != null)
            {
                try
                {
                    listener.Server.Close();
                }
                finally
                {
                    listener.Stop();
                    listener = null;
                }
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
    }
}
