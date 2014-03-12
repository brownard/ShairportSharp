using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    public enum PlaybackState
    {
        Loading,
        Playing,
        Paused,
        Stopped
    }

    class PlaybackStateInfo :IPlistResponse
    {
        public string Category { get { return "video"; } }
        public string SessionId { get; set; }
        public PlaybackState State { get; set; }
   
        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist["category"] = Category;
            plist["sesionID"] = SessionId;
            plist["state"] = State.ToString().ToLowerInvariant();
            return plist;
        }
    }
}
