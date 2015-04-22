using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public class MirroringSetup
    {
        public int DeviceId { get; set; }
        public int SessionId { get; set; }
        public string Version { get; set; }
        public byte[] FPKey { get; set; }
        public byte[] AESKey { get; set; }
        public byte[] IV { get; set; }
        public int Latency { get; set; }

        public MirroringSetup(IDictionary<string, object> plist)
        {
            if (plist.ContainsKey("deviceID")) 
                DeviceId = (int)plist["deviceID"];
            if (plist.ContainsKey("sessionID")) 
                SessionId = (int)plist["sessionID"];
            if (plist.ContainsKey("version")) 
                Version = (string)plist["version"];
            if (plist.ContainsKey("latency")) 
                Latency = (int)plist["latency"];
            if (plist.ContainsKey("param1"))
                FPKey = (byte[])plist["param1"];
            if (plist.ContainsKey("param2"))
                IV = (byte[])plist["param2"];
        }
    }
}
