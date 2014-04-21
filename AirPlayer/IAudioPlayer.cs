using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer
{
    interface IAudioPlayer
    {
        void UpdateDurationInfo(uint startStamp, uint stopStamp);
    }
}
