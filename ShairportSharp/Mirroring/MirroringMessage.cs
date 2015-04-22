using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public class MirroringMessage
    {
        public MirroringMessage() { }

        public MirroringMessage(byte[] header)
        {
            PayloadSize = BitConverter.ToUInt32(header, 0);
            PayloadType = BitConverter.ToUInt16(header, 4);
            NTPTimestamp = BitConverter.ToUInt64(header, 8);
        }

        public uint PayloadSize { get; set; }
        public uint PayloadType { get; set; }
        public ulong NTPTimestamp { get; set; }
        public byte[] Content { get; set; }
    }
}
