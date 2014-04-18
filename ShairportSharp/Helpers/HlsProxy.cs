using ShairportSharp.Http;
using ShairportSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ShairportSharp.Helpers
{
    public class HlsProxy
    {
        string proxyAddress;
        HttpConnectionHandler connectionHandler;
        object parserSync = new object();
        List<ProxyRequestParser> parsers = new List<ProxyRequestParser>();
        HashSet<string> allowedUrls = new HashSet<string>();

        public int Port { get; set; }

        IPAddress ipAddress = IPAddress.Loopback;
        IPAddress IPAddress 
        { 
            get { return ipAddress; } 
            set { ipAddress = value; }         
        }

        string userAgent = Utils.APPLE_USER_AGENT;
        public string UserAgent 
        {
            get { return userAgent; }
            set { userAgent = value; }
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
                Logger.Debug("HlsProxy: Started listener on '{0}'", proxyAddress);
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
                Logger.Debug("HlsProxy: Stopped listener");
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
                Logger.Warn("Unsolicited request to proxy with url '{0}'", proxyUrl);
            }
            return null;
        }

        bool isAllowed(string url)
        {
            lock (parserSync)
                return allowedUrls.Contains(url);
        }

        void connectionHandler_SocketAccepted(object sender, SocketAcceptedEventArgs e)
        {
            Logger.Debug("HlsProxy: Connection accepted");
            e.Handled = true;
            ProxyRequestParser parser = new ProxyRequestParser(e.Socket, this);
            parser.Closed += parser_Closed;
            parser.UserAgent = userAgent;
            parser.Start();
            lock (parserSync)
                parsers.Add(parser);
        }

        void parser_Closed(object sender, EventArgs e)
        {
            lock (parserSync)
                parsers.Remove((ProxyRequestParser)sender);
            Logger.Debug("HlsProxy: Connection closed");
        }

    }
}
