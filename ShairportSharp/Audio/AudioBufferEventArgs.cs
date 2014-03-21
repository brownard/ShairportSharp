using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    public class BufferChangedEventArgs : EventArgs
    {
        public BufferChangedEventArgs(int currentSize, int maxSize)
        {
            CurrentSize = currentSize;
            MaxSize = maxSize;
        }

        public int CurrentSize { get; private set; }
        public int MaxSize { get; private set; }
    }

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
