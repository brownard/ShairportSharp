using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirPlayer.MediaPortal2
{
    public class ImagePlayerBuilder : IPlayerBuilder
    {
        public IPlayer GetPlayer(MediaItem mediaItem)
        {
            if (!(mediaItem is ImageItem))
                return null;

            AirplayImagePlayer player = new AirplayImagePlayer();
            player.NextItem(mediaItem, StartTime.AtOnce);
            return (IPlayer)player;
        }
    }
}
