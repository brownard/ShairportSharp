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

    public class PhotoEventArgs : EventArgs
    {
        public PhotoEventArgs(string assetKey, string transition, byte[] photo)
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
        public PhotoReceivedEventArgs(string assetKey, string transition, byte[] photo, string assetAction)
            : base(assetKey, transition, photo)
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

    public class SlideshowSettingsEventArgs : EventArgs
    {
        public SlideshowSettingsEventArgs(string state, int slideDuration, string theme)
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

    public class VideoEventArgs : EventArgs
    {
        public VideoEventArgs(string contentLocation, double startPosition)
        {
            ContentLocation = contentLocation;
            StartPosition = startPosition;
        }

        public string ContentLocation { get; protected set; }
        public double StartPosition { get; protected set; }
    }

    public class PlaybackInfoEventArgs : EventArgs
    {
        public PlaybackInfoEventArgs()
        {
            PlaybackInfo = new PlaybackInfo()
            {
                PlaybackBufferEmpty = true
            };
        }

        public PlaybackInfo PlaybackInfo { get; protected set; }
    }
}
