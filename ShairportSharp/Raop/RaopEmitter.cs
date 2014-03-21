using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShairportSharp.Bonjour;
using ZeroconfService;

namespace ShairportSharp.Raop
{
    class RaopEmitter : BonjourEmitter
    {
        const string TYPE = "_raop._tcp.";
        string name;
        string identifier;
        int port;
        bool pass;

        public RaopEmitter(string name, string identifier, int port = 6000, bool pass = false)
        {
            this.name = name;
            this.identifier = identifier;
            this.port = port;
            this.pass = pass;
        }

        protected override NetService GetNetService()
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

            NetService service = new NetService("", TYPE, identifier + "@" + name, port);
            service.TXTRecordData = NetService.DataFromTXTRecordDictionary(txtRecord);
            return service;
        }
    }
}
