using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ZeroconfService;

namespace ShairportSharp
{
    class BonjourEmitter
    {
        string name;
        string identifier;
        int port;
        bool pass;
        NetService currentService = null;

        public event NetService.ServicePublished DidPublishService;
        public event NetService.ServiceNotPublished DidNotPublishService;

        public BonjourEmitter(string name, string identifier, int port = 6000, bool pass = false)
        {
            this.name = name;
            this.identifier = identifier;
            this.port = port;
            this.pass = pass;
        }

        public void Publish()
        {
            Dictionary<string, object> txtRecord = new Dictionary<string, object>();
            txtRecord.Add("txtvers", "1");
            txtRecord.Add("pw", pass.ToString());
            txtRecord.Add("sr", "44100");
            txtRecord.Add("ss", "16");
            txtRecord.Add("ch", "2");
            txtRecord.Add("tp", "UDP");
            txtRecord.Add("sm", "false");
            txtRecord.Add("sv", "false");
            txtRecord.Add("ek", "1");
            txtRecord.Add("et", "0,1");
            txtRecord.Add("cn", "0,1");
            txtRecord.Add("vn", "3");
            txtRecord.Add("md", "0,1,2");
            currentService = new NetService("", "_raop._tcp.", identifier + "@" + name, port);
            currentService.AllowMultithreadedCallbacks = true;
            currentService.TXTRecordData = NetService.DataFromTXTRecordDictionary(txtRecord);
            currentService.DidPublishService += service_DidPublishService;
            currentService.DidNotPublishService += service_DidNotPublishService;
            currentService.Publish();
        }

        public void Stop()
        {
            if (currentService != null)
            {
                currentService.Stop();
                currentService = null;
            }
        }

        void service_DidPublishService(NetService service)
        {
            if (DidPublishService != null)
                DidPublishService(service);
        }

        void service_DidNotPublishService(NetService service, DNSServiceException exception)
        {
            if (DidNotPublishService != null)
                DidNotPublishService(service, exception);
        }
    }
}
