using ShairportSharp.Base;
using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public class MirroringServer : Server<MirroringSession>
    {
        object syncRoot = new object();

        public MirroringServer()
        {
            Port = 7100;
        }

        public event EventHandler<MirroringStartedEventArgs> Started;
        protected virtual void OnStarted(MirroringStartedEventArgs e)
        {
            if (Started != null)
                Started(this, e);
        }

        protected override MirroringSession OnSocketAccepted(System.Net.Sockets.Socket socket)
        {
            MirroringSession session = new MirroringSession(socket, Password);
            session.Started += session_Started;
            return session;
        }

        void session_Started(object sender, MirroringStartedEventArgs e)
        {
            OnStarted(e);
        }
    }
}
