using AirPlayer.MediaPortal2.MediaItems;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Presentation.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirPlayer.MediaPortal2.Players
{
    /// <summary>
    /// Player builder for Airplay audio streams.
    /// </summary>
    public class AirplayPlayerBuilder : IPlayerBuilder
    {
        #region IPlayerBuilder implementation

        public IPlayer GetPlayer(MediaItem mediaItem)
        {
            AudioItem audioItem = mediaItem as AudioItem;
            if (audioItem != null)
                return getAudioPlayer(audioItem);

            ImageItem imageItem = mediaItem as ImageItem;
            if (imageItem != null)
                return getImagePlayer(imageItem);

            return null;
        }

        #endregion

        IPlayer getAudioPlayer(AudioItem mediaItem)
        {
            AirplayAudioPlayer player = new AirplayAudioPlayer(mediaItem.PlayerSettings);
            try
            {
                player.SetMediaItem(mediaItem.GetResourceLocator(), null);
            }
            catch (Exception e)
            {
                ServiceRegistration.Get<ILogger>().Warn("AirplayAudioPlayer: Unable to play audio stream", e);
                IDisposable disposablePlayer = player as IDisposable;
                if (disposablePlayer != null)
                    disposablePlayer.Dispose();
                throw;
            }
            return (IPlayer)player;
        }

        IPlayer getImagePlayer(ImageItem mediaItem)
        {
            AirplayImagePlayer player = new AirplayImagePlayer();
            player.NextItem(mediaItem, StartTime.AtOnce);
            return (IPlayer)player;
        }
    }
}
