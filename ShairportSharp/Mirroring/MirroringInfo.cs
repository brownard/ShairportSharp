using ShairportSharp.Airplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public class MirroringInfo : IPlistResponse
    {
        public MirroringInfo()
        {
            Width = 1280;
            Height = 720;
            Overscanned = true;
            RefreshRate = (double)1 / 60;
            Version = Constants.VERSION;
        }

        public int Height { get; set; }
        public int Width { get; set; }
        public bool Overscanned { get; set; }
        public double RefreshRate { get; set; }
        public string Version { get; set; }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist["height"] = Height;
            plist["overscanned"] = Overscanned;
            plist["refreshRate"] = RefreshRate;
            plist["version"] = Version;
            plist["width"] = Width;
            return plist;
        }
    }
}
