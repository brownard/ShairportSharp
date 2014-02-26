using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroconfService;

namespace ShairportSharp.Airplay
{
    class AirplayEmitter : BonjourEmitter
    {
        const string TYPE = "_airplay._tcp";
        string name;
        string identifier;
        int port;
        bool pass;

        public AirplayEmitter(string name, string identifier, int port = 7000, bool pass = false)
        {
            this.name = name;
            this.identifier = identifier;
            this.port = port;
            this.pass = pass;
        }

        protected override NetService GetNetService()
        {
            Dictionary<string, object> txtRecord = new Dictionary<string, object>();
            txtRecord.Add("model", "AppleTV2,1");
            txtRecord.Add("deviceid", identifier);
            txtRecord.Add("features", "0x0803");
            if (pass) txtRecord.Add("pw", "1");
            NetService service = new NetService("", TYPE, name, port);
            service.TXTRecordData = NetService.DataFromTXTRecordDictionary(txtRecord);
            return service;
        }
    }
}
