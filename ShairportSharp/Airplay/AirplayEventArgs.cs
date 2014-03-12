using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    public enum PhotoAction
    {
        CacheOnly,
        DisplayCached,
        None
    }

    public enum SlideshowState
    {
        Playing,
        Stopped
    }

    public class AirplayEventArgs : EventArgs
    {
        public AirplayEventArgs(string sessionId)
        {
            SessionId = sessionId;
        }

        public string SessionId { get; protected set; }
    }

    public class PhotoEventArgs : AirplayEventArgs
    {
        public PhotoEventArgs(string assetKey, string transition, byte[] photo, string sessionId)
            : base(sessionId)
        {
            AssetKey = assetKey;
            Transition = transition;
            Photo = photo;
        }

        public string AssetKey { get; protected set; }
        public string Transition { get; protected set; }
        public byte[] Photo { get; protected set; }
    }

    public class PhotoReceivedEventArgs : PhotoEventArgs
    {
        public PhotoReceivedEventArgs(string assetKey, string transition, byte[] photo, string assetAction, string sessionId)
            : base(assetKey, transition, photo, sessionId)
        {
            switch (assetAction)
            {
                case "cacheOnly":
                    AssetAction = PhotoAction.CacheOnly;
                    break;
                case "displayCached":
                    AssetAction = PhotoAction.DisplayCached;
                    break;
                default:
                    AssetAction = PhotoAction.None;
                    break;
            }
        }

        public PhotoAction AssetAction { get; protected set; }
        public bool NotInCache { get; set; }
    }

    public class SlideshowFeaturesEventArgs : AirplayEventArgs
    {
        public SlideshowFeaturesEventArgs(string sessionId)
            : base(sessionId)
        {
            Features = new SlideshowFeatures();
        }

        public SlideshowFeatures Features { get; protected set; }
    }

    public class SlideshowSettingsEventArgs : AirplayEventArgs
    {
        public SlideshowSettingsEventArgs(string state, int slideDuration, string theme, string sessionId)
            : base(sessionId)
        {
            SlideDuration = slideDuration;
            Theme = theme;
            if (state == "playing")
                State = SlideshowState.Playing;
            else
                State = SlideshowState.Stopped;
        }

        public SlideshowState State { get; protected set; }
        public int SlideDuration { get; protected set; }
        public string Theme { get; protected set; }
    }

    public class VideoEventArgs : AirplayEventArgs
    {
        public VideoEventArgs(string contentLocation, double startPosition, string sessionId)
            : base(sessionId)
        {
            ContentLocation = contentLocation;
            StartPosition = startPosition;
        }

        public string ContentLocation { get; protected set; }
        public double StartPosition { get; protected set; }
    }

    public class PlaybackInfoEventArgs : AirplayEventArgs
    {
        public PlaybackInfoEventArgs(string sessionId)
            : base(sessionId)
        {
            PlaybackInfo = new PlaybackInfo()
            {
                PlaybackBufferEmpty = true
            };
        }

        public PlaybackInfo PlaybackInfo { get; protected set; }
    }

    public class PlaybackRateEventArgs : AirplayEventArgs
    {
        public PlaybackRateEventArgs(double rate, string sessionId)
            : base(sessionId)
        {
            Rate = rate;
        }

        public double Rate { get; protected set; }
    }

    public class PlaybackPositionEventArgs : AirplayEventArgs
    {
        public PlaybackPositionEventArgs(double position, string sessionId)
            : base(sessionId)
        {
            Position = position;
        }

        public double Position { get; protected set; }
    }

    public class GetPlaybackPositionEventArgs : AirplayEventArgs
    {
        public GetPlaybackPositionEventArgs(string sessionId)
            : base(sessionId)
        { }

        public double Duration { get; set; }
        public double Position { get; set; }
    }
}
