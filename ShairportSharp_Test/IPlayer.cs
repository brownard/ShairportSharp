using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp_Test
{
    interface IPlayer
    {
        void Start();
        void Stop();
        void Dispose();
        void UpdateDurationInfo(uint startStamp, uint stopStamp);
        void SetVolume(double volume);
        double Duration { get; }
        double CurrentPosition { get; }
    }
}
