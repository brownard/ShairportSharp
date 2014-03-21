using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    public class AirplayServerInfo : IPlistResponse
    {
        public string DeviceId { get; set; }
        public string Model { get; set; }
        public string ProtocolVersion { get; set; }
        public string ServerVersion { get; set; }
        public AirplayFeature Features { get; set; }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist["deviceid"] = DeviceId;
            plist["features"] = (int)Features;
            plist["model"] = Model;
            plist["protovers"] = ProtocolVersion;
            plist["srcvers"] = ServerVersion;
            return plist;
        }
    }

    [Flags]
    public enum AirplayFeature
    {
        Video = 1,
        Photo = 2,
        VideoFairPlay = 4,
        VideoVolumeControl = 8,
        VideoHTTPLiveStreams = 16,
        Slideshow = 32,
        Unknown = 64,
        Screen = 128,
        ScreenRotate = 256,
        Audio = 512,
        AudioRedundant = 2048,
        FPSAPv2pt5_AES_GCM = 4096,
        PhotoCaching = 8192
    }
}
