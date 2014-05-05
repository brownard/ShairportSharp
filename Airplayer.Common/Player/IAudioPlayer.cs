using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer.Common.Player
{
    public interface IAudioPlayer
    {
        void UpdateDurationInfo(uint startStamp, uint stopStamp);
    }
}
