using ShairportSharp.Base;
using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public class MirroringServer : Server<MirroringSession>
    {
        const int PORT = 7100;
        object syncRoot = new object();

        public MirroringServer()
        {
            Port = PORT;
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

        public void StopCurrentSession()
        {
            CloseConnections();
        }

        public event EventHandler Authenticating;
        protected virtual void OnAuthenticating(EventArgs e)
        {
            if (Authenticating != null)
                Authenticating(this, e);
        }

        public event EventHandler<MirroringStartedEventArgs> Started;
        protected virtual void OnStarted(MirroringStartedEventArgs e)
        {
            if (Started != null)
                Started(this, e);
        }

        public event EventHandler SessionClosed;
        protected virtual void OnSessionClosed(EventArgs e)
        {
            if (SessionClosed != null)
                SessionClosed(this, e);
        }

        protected override MirroringSession OnSocketAccepted(Socket socket)
        {
            MirroringSession session = new MirroringSession(socket, Password);
            session.Authenticating += session_Authenticating;
            session.Started += session_Started;
            return session;
        }

        protected override void OnConnectionClosed(MirroringSession connection)
        {
            OnSessionClosed(EventArgs.Empty);
            base.OnConnectionClosed(connection);
        }

        void session_Authenticating(object sender, EventArgs e)
        {
            OnAuthenticating(e);
        }

        void session_Started(object sender, MirroringStartedEventArgs e)
        {
            OnStarted(e);
        }
    }
}
