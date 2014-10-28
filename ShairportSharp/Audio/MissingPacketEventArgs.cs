using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    class MissingPacketEventArgs : EventArgs
    {
        public MissingPacketEventArgs(int first, int last)
        {
            First = first;
            Last = last;
        }

        public int First { get; private set; }
        public int Last { get; private set; }
    }
}
