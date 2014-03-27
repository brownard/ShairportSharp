using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    public class PlaybackInfo : IPlistResponse
    {
        public PlaybackInfo()
        {
            ReadyToPlay = true;
            PlaybackBufferEmpty = true;
            Rate = 1;
            LoadedTimeRanges = new List<PlaybackTimeRange>();
            SeekableTimeRanges = new List<PlaybackTimeRange>();
        }

        public double Duration { get; set; }
        public double Position { get; set; }
        public double Rate { get; set; }
        public bool ReadyToPlay { get; set; }
        public bool PlaybackBufferEmpty { get; set; }
        public bool PlaybackBufferFull { get; set; }
        public bool PlaybackLikelyToKeepUp { get; set; }
        public List<PlaybackTimeRange> LoadedTimeRanges { get; protected set; }
        public List<PlaybackTimeRange> SeekableTimeRanges { get; protected set; }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();

            List<object> loadedList = new List<object>();
            foreach (PlaybackTimeRange range in LoadedTimeRanges)
                loadedList.Add(range.GetPlist());

            List<object> seekableList = new List<object>();
            foreach (PlaybackTimeRange range in SeekableTimeRanges)
                seekableList.Add(range.GetPlist());

            plist["duration"] = Duration;
            plist["loadedTimeRanges"] = loadedList;
            plist["playbackBufferEmpty"] = PlaybackBufferEmpty;
            plist["playbackBufferFull"] = PlaybackBufferFull;
            plist["playbackLikelyToKeepUp"] = PlaybackLikelyToKeepUp;
            plist["position"] = Position;
            plist["rate"] = Rate;
            plist["readyToPlay"] = ReadyToPlay;
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
            plist["duration"] = Duration;
            plist["start"] = Start;
            return plist;
        }
    }
}
