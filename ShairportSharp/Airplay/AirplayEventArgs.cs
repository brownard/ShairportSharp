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

    public class PhotoReceivedEventArgs : EventArgs
    {
        public PhotoReceivedEventArgs(string assetKey, string transition, string assetAction, byte[] photo)
        {
            AssetKey = assetKey;
            Transition = transition;
            Photo = photo;
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

        public string AssetKey { get; private set; }
        public string Transition { get; private set; }
        public PhotoAction AssetAction { get; private set; }
        public byte[] Photo { get; private set; }
    }
}
