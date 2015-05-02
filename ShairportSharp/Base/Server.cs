using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Base
{
    public abstract class Server<T> where T : HttpParser
    {
        #region Variables

        protected object SyncRoot = new object();
        HttpConnectionHandler listener = null;

        object connectionSync = new object();
        List<T> connections = new List<T>();

        #endregion

        int port;
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        protected virtual void OnStarting() { }
        protected virtual void OnStarted() { }
        protected virtual void OnStopping() { }
        protected virtual void OnConnectionClosed(T connection) { }
        protected abstract T OnSocketAccepted(Socket socket);

        public void Start()
        {
            lock (SyncRoot)
            {
                if (listener != null)
                    Stop();

                OnStarting();
                listener = new HttpConnectionHandler(IPAddress.Any, port);
                listener.SocketAccepted += listener_SocketAccepted;
                listener.Start();
                OnStarted();
            }
        }

        private void listener_SocketAccepted(object sender, SocketAcceptedEventArgs e)
        {
            T connection = OnSocketAccepted(e.Socket);
            if (connection != null)
            {
                e.Handled = true;
                connection.Closed += connection_Closed;
                lock (connectionSync)
                {
                    connections.Add(connection);
                    connection.Start();
                }
            }
        }

        void connection_Closed(object sender, ClosedEventArgs e)
        {
            T connection = (T)sender;
            if (!e.ManualClose)
                lock (connectionSync)
                    connections.Remove(connection);
            OnConnectionClosed(connection);
        }

        public void Stop()
        {
            Logger.Info("Server: Stopping");
            lock (SyncRoot)
            {
                OnStopping();
                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }
            }
            CloseConnections();
            Logger.Info("Server: Stopped");
        }

        protected virtual void CloseConnections()
        {
            List<T> lConnections;
            lock (connectionSync)
            {
                lConnections = connections;
                connections = new List<T>();
            }

            foreach (T connection in lConnections)
                connection.Close();
        }
    }
}