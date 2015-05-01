using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public enum PayloadType
    {
        Codec,
        Video,
        Heartbeat,
        Unknown
    }

    public class MirroringMessage
    {
        public MirroringMessage() { }

        public MirroringMessage(byte[] header)
        {
            PayloadSize = BitConverter.ToUInt32(header, 0);
            NTPTimestamp = BitConverter.ToUInt64(header, 8);

            uint payloadType = BitConverter.ToUInt16(header, 4);
            if (payloadType == 0)
                PayloadType = Mirroring.PayloadType.Video;
            else if (payloadType == 1)
                PayloadType = Mirroring.PayloadType.Codec;
            else if (payloadType == 2)
            {
                PayloadType = Mirroring.PayloadType.Heartbeat;
                //Logger.Debug("MirroringMessage: Heartbeat");
            }
            else
            {
                PayloadType = Mirroring.PayloadType.Unknown;
                //Logger.Debug("MirroringMessage: Unknown payload type '{0}', Size '{1}'", payloadType, PayloadSize);
            }
        }

        public uint PayloadSize { get; set; }
        public PayloadType PayloadType { get; set; }
        public ulong NTPTimestamp { get; set; }
        public byte[] Payload { get; set; }
    }
}
