using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    public class PlaybackInfo : IPlistResponse
    {
        public double Duration { get; set; }
        public double Position { get; set; }
        public double Rate { get; set; }
        public bool ReadyToPlay { get; set; }
        public bool PlaybackBufferEmpty { get; set; }
        public bool PlaybackBufferFull { get; set; }
        public bool PlaybackLikelyToKeepUp { get; set; }

        List<PlaybackTimeRange> loadedTimeRanges = new List<PlaybackTimeRange>();
        public List<PlaybackTimeRange> LoadedTimeRanges { get { return loadedTimeRanges; } }

        List<PlaybackTimeRange> seekableTimeRanges = new List<PlaybackTimeRange>();
        public List<PlaybackTimeRange> SeekableTimeRanges { get { return seekableTimeRanges; } }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist["duration"] = Duration;
            plist["position"] = Position;
            plist["rate"] = Rate;
            plist["readyToPlay"] = ReadyToPlay;
            plist["playbackBufferEmpty"] = PlaybackBufferEmpty;
            plist["playbackBufferFull"] = PlaybackBufferFull;
            plist["playbackLikelyToKeepUp"] = PlaybackLikelyToKeepUp;

            List<object> loadedList = new List<object>();
            foreach (PlaybackTimeRange range in loadedTimeRanges)
                loadedList.Add(range.GetPlist());
            plist["loadedTimeRanges"] = loadedList;

            List<object> seekableList = new List<object>();
            foreach (PlaybackTimeRange range in seekableTimeRanges)
                seekableList.Add(range.GetPlist());
            plist["seekableTimeRanges"] = seekableList;

            return plist;
        }
    }

    public class PlaybackTimeRange : IPlistResponse
    {
        public double Start { get; set; }
        public double Duration { get; set; }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist["start"] = Start;
            plist["duration"] = Duration;
            return plist;
        }
    }
}
