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

namespace AirPlayer.MediaPortal2
{
    /// <summary>
    /// Player builder for Airplay audio streams.
    /// </summary>
    public class AudioPlayerBuilder : IPlayerBuilder
    {
        #region IPlayerBuilder implementation

        public IPlayer GetPlayer(MediaItem mediaItem)
        {
            AudioItem audioItem = mediaItem as AudioItem;
            if (audioItem == null)
                return null;

            AirplayAudioPlayer player = new AirplayAudioPlayer(audioItem.PlayerSettings);
            try
            {
                player.SetMediaItem(audioItem.GetResourceLocator(), null);
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

        #endregion
    }
}
