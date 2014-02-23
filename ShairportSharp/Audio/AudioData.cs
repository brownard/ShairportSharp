using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    class AudioData
    {
        public bool Ready { get; set; }
        public byte[] Data { get; set; }
        public uint TimeStamp { get; set; }
    }
}
