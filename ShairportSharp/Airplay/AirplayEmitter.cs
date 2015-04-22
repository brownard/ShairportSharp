using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ShairportSharp.Base;
using ZeroconfService;

namespace ShairportSharp.Airplay
{
    class AirplayEmitter : BonjourEmitter
    {
        const string TYPE = "_airplay._tcp";
        const int INITIAL_DELAY = 2000;
        const int INTERVAL = 10000;

        string name;
        string identifier;
        string model;
        string features;
        int port;
        bool pass;
        bool ios8Workaround;

        Dictionary<string, object> txtRecord;
        bool dummyVal = false;
        object timerSync = new object();
        Timer timer;

        public AirplayEmitter(string name, AirplayServerInfo serverInfo, int port = 7000, bool pass = false, bool ios8Workaround = false)
        {
            this.name = name;
            this.identifier = serverInfo.DeviceId;
            this.model = serverInfo.Model;
            this.features = "0x" + ((int)serverInfo.Features).ToString("X4");
            Logger.Debug("Features: {0} ({1})", serverInfo.Features, features);
            this.port = port;
            this.pass = pass;
            this.ios8Workaround = ios8Workaround;
        }

        protected override NetService GetNetService()
        {
            Logger.Debug("AirplayEmitter: {0}, {1}, {2}", name, identifier, port);
            txtRecord = new Dictionary<string, object>();
            txtRecord.Add("txtvers", "1");
            txtRecord.Add("model", "AppleTV3,2");//model);
            txtRecord.Add("deviceid", identifier);
            txtRecord.Add("srcvers", Constants.VERSION); //"130.14");
            txtRecord.Add("features", "0x100029ff"); //ios8Workaround ? "0x20F7" : features);
            if (pass) txtRecord.Add("pw", "1");

            NetService service = new NetService("", TYPE, name, port);
            service.TXTRecordData = NetService.DataFromTXTRecordDictionary(txtRecord);
            return service;
        }

        public override void Stop()
        {
            lock (timerSync)
            {
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
                base.Stop();
            }
        }

        protected override void OnDidPublishService(NetService service)
        {
            //lock (timerSync)
                //timer = new Timer(timerCallback, null, INITIAL_DELAY, INTERVAL);
            base.OnDidPublishService(service);
        }        

        //Hack for IOS 7 detection, periodically update the TXTRecord to force redetection
        void timerCallback(object o)
        {
            lock (timerSync)
            {
                if (timer != null && CurrentService != null)
                {
                    dummyVal = !dummyVal;
                    txtRecord["dummy"] = dummyVal.ToString();
                    CurrentService.TXTRecordData = NetService.DataFromTXTRecordDictionary(txtRecord);
                }
            }
        }
    }
}
