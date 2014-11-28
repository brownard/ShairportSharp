using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Base
{
    public abstract class Server<T> where T : ISocketHandler
    {
        #region Variables

        protected object SyncRoot = new object();
        protected BonjourEmitter Emitter;
        HttpConnectionHandler listener = null; 
        
        object connectionSync = new object();
        List<T> connections = new List<T>();

        #endregion

        string name;
        /// <summary>
        // The broadcasted name of the Airplay server. Defaults to the machine name if null or empty.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string password;
        /// <summary>
        /// The password needed to connect. Set to null or empty to not require a password.
        /// </summary>
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        byte[] macAddress = null;
        /// <summary>
        /// The MAC address used to identify this server. If null or empty the actual MAC address of this computer will be used.
        /// Set to an alternative value to allow multiple servers on the same computer.
        /// </summary>
        public byte[] MacAddress
        {
            get { return macAddress; }
            set { macAddress = value; }
        }

        int port;
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        string modelName = Constants.DEFAULT_MODEL_NAME;
        /// <summary>
        /// The model of the server, should be in the format '[NAME], [VERSION]'
        /// In most cases can be left as the default 'ShairportSharp, 1'
        /// </summary>
        public string ModelName
        {
            get { return modelName; }
            set { modelName = value; }
        }

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
                if (Emitter != null)
                    Emitter.Publish();
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
                if (Emitter != null)
                {
                    Emitter.Stop();
                    Emitter = null;
                }
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

        protected virtual void OnStarting() { }
        protected virtual void OnStopping() { }
        protected virtual void OnConnectionClosed(T connection) { }
        protected abstract T OnSocketAccepted(Socket socket);

    }
}
