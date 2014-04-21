﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShairportSharp.Http;

namespace ShairportSharp.Helpers
{
    public class HlsProxy : Proxy
    {
        string userAgent = Utils.APPLE_USER_AGENT;
        public string UserAgent 
        {
            get { return userAgent; }
            set { userAgent = value; }
        }

        public override HttpParser ConnectionAccepted(System.Net.Sockets.Socket socket)
        {
            HlsProxyRequestParser parser = new HlsProxyRequestParser(socket, this);
            parser.UserAgent = userAgent;
            return parser;
        }
    }
}
