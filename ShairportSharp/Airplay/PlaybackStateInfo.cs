using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    public enum PlaybackCategory
    {
        Photo,
        Slideshow,
        Video
    }

    public enum PlaybackState
    {
        Loading,
        Playing,
        Paused,
        Stopped
    }

    class PlaybackStateInfo : IPlistResponse
    {
        public string SessionId { get; set; }
        public PlaybackCategory Category { get; set; }
        public PlaybackState State { get; set; }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist["category"] = Category.ToString().ToLowerInvariant();
            plist["sesionID"] = SessionId;
            plist["state"] = State.ToString().ToLowerInvariant();
            return plist;
        }
    }
}
