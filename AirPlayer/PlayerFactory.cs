using MediaPortal.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer
{
    class PlayerFactory : IPlayerFactory
    {
        Player player;
        public PlayerFactory(Player player)
        {
            this.player = player;
        }

        public IPlayer Create(string fileName, g_Player.MediaType type)
        {
            return Create(fileName);
        }

        public IPlayer Create(string fileName)
        {
            return player;
        }
    }
}
