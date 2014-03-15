using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ZeroconfService;

namespace ShairportSharp.Remote
{
    class RemoteHandler
    {
        object serverInfoLock = new object();
        Dictionary<string, NetService> serverInfos;
        NetServiceBrowser browser = null;

        public void Start()
        {
            if (browser == null)
            {
                browser = new NetServiceBrowser();
                browser.AllowMultithreadedCallbacks = true;
                browser.DidFindService += browser_DidFindService;
                browser.DidRemoveService += browser_DidRemoveService;
                browser.SearchForService("_dacp._tcp.", "");

                lock (serverInfoLock)
                    serverInfos = new Dictionary<string, NetService>();
            }
        }

        public void Stop()
        {
            if (browser != null)
            {
                browser.Stop();
                browser = null;

                lock (serverInfoLock)
                    serverInfos = null;
            }
        }

        public void SendCommand(RemoteServerInfo serverInfo, RemoteCommand remoteCommand)
        {
            string hostName;
            int port;
            if(!tryGetServiceInfo(serverInfo.DacpId, out hostName, out port))
                return;

            string command = remoteCommand.ToString().ToLowerInvariant();
            Logger.Debug("Sending remote command to server: {0}", command);
            string url = string.Format("http://{0}:{1}/ctrl-int/1/{2}", hostName, port, command);

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("Active-Remote", serverInfo.ActiveRemote);
                request.BeginGetResponse(ar =>
                    {
                        try
                        {
                            using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar))
                            {
                                if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
                                    Logger.Warn("Client returned unexpected response to remote control request: {0} - {1}", response.StatusCode, response.StatusDescription);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(string.Format("Exception receiving remote control response from {0} -", url), ex);
                        }
                    }, null);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Exception sending remote control request to {0} -", url), ex);
            }
        }

        bool tryGetServiceInfo(string dacpId, out string hostName, out int port)
        {
            lock (serverInfoLock)
            {
                if (serverInfos != null)
                {
                    foreach (string key in serverInfos.Keys)
                    {
                        if (key.EndsWith(dacpId))
                        {
                            NetService service = serverInfos[key];
                            hostName = service.HostName;
                            port = service.Port;
                            return true;
                        }
                    }
                }
            }

            hostName = null;
            port = -1;
            return false;
        }

        void browser_DidFindService(NetServiceBrowser browser, NetService service, bool moreComing)
        {
            Logger.Debug("RemoteHandler: Found service - {0}", service.Name);
            service.DidResolveService += service_DidResolveService;
            service.ResolveWithTimeout(10);
        }

        void service_DidResolveService(NetService service)
        {
            lock (serverInfoLock)
            {
                if (serverInfos != null)
                {
                    serverInfos[service.Name] = service;
                    Logger.Debug("RemoteHandler: Resolved service - {0}, {1}:{2}", service.Name, service.HostName, service.Port);
                }
            }
        }

        void browser_DidRemoveService(NetServiceBrowser browser, NetService service, bool moreComing)
        {
            lock (serverInfoLock)
            {
                if (serverInfos != null)
                {
                    serverInfos.Remove(service.Name);
                    Logger.Debug("RemoteHandler: Removed service - {0}", service.Name);
                }
            }
        }
    }
}
