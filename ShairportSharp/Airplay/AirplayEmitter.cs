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
        string model;
        string features;
        int port;
        bool pass;

        public AirplayEmitter(string name, AirplayServerInfo serverInfo, int port = 7000, bool pass = false)
        {
            this.name = name;
            this.identifier = serverInfo.DeviceId;
            this.model = serverInfo.Model;
            this.features = "0x" + ((int)serverInfo.Features).ToString("X4");
            Logger.Debug("Features: {0}", features);
            this.port = port;
            this.pass = pass;
        }

        protected override NetService GetNetService()
        {
            Logger.Debug("AirplayEmitter: {0}, {1}, {2}", name, identifier, port);
            Dictionary<string, object> txtRecord = new Dictionary<string, object>();
            txtRecord.Add("txtvers", "1");
            txtRecord.Add("model", model);
            txtRecord.Add("deviceid", identifier);
            txtRecord.Add("features", features);
            if (pass) txtRecord.Add("pw", "1");
            NetService service = new NetService("", TYPE, name, port);
            service.TXTRecordData = NetService.DataFromTXTRecordDictionary(txtRecord);
            return service;
        }
    }
}
