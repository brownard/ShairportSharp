using ShairportSharp.Helpers;
using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AirPlayer.Common.Proxy
{
    public abstract class Proxy
    {
        string proxyAddress;
        HttpConnectionHandler connectionHandler;
        object parserSync = new object();
        List<HttpParser> parsers = new List<HttpParser>();
        HashSet<string> allowedUrls = new HashSet<string>();

        public int Port { get; set; }

        IPAddress ipAddress = IPAddress.Loopback;
        IPAddress IPAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; }
        }

        public void Start()
        {
            if (connectionHandler == null)
            {
                connectionHandler = new HttpConnectionHandler(ipAddress, Port);
                connectionHandler.SocketAccepted += connectionHandler_SocketAccepted;
                connectionHandler.Start();
                Port = connectionHandler.Port;
                proxyAddress = string.Format("http://{0}:{1}", ipAddress, Port);
                Logger.Debug("Proxy: Started listener on '{0}'", proxyAddress);
            }
        }

        public void Stop()
        {
            if (connectionHandler != null)
            {
                connectionHandler.Stop();
                connectionHandler = null;
                lock (parserSync)
                    allowedUrls.Clear();
                Logger.Debug("Proxy: Stopped listener");
            }
        }

        public string GetProxyUrl(string originalUrl)
        {
            string allowedUrl = "/Proxy?url=" + System.Web.HttpUtility.UrlEncode(originalUrl);
            lock (parserSync)
                allowedUrls.Add(allowedUrl);
            return proxyAddress + allowedUrl;
        }

        public string GetOriginalUrl(string proxyUrl)
        {
            if (isAllowed(proxyUrl))
            {
                var queryString = proxyUrl.GetQueryStringParameters();
                string url;
                if (queryString.TryGetValue("url", out url))
                {
                    return System.Web.HttpUtility.UrlDecode(url);
                }
            }
            else
            {
                Logger.Warn("Proxy: Unsolicited request to proxy with url '{0}'", proxyUrl);
            }
            return null;
        }

        public abstract HttpParser ConnectionAccepted(Socket socket);

        bool isAllowed(string url)
        {
            lock (parserSync)
                return allowedUrls.Contains(url);
        }

        void connectionHandler_SocketAccepted(object sender, SocketAcceptedEventArgs e)
        {
            Logger.Debug("Proxy: Connection accepted");
            HttpParser parser = ConnectionAccepted(e.Socket);
            if (parser != null)
            {
                e.Handled = true;
                parser.Closed += parser_Closed;
                parser.Start();
                lock (parserSync)
                    parsers.Add(parser);
            }
        }

        void parser_Closed(object sender, EventArgs e)
        {
            lock (parserSync)
                parsers.Remove((HttpParser)sender);
            Logger.Debug("Proxy: Connection closed");
        }
    }
}
