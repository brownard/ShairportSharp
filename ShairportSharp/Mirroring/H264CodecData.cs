using ShairportSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public class H264CodecData
    {
        public H264CodecData(byte[] data)
        {
            int offset = 0;
            Version = data[offset++];
            Profile = data[offset++];
            Compatability = data[offset++];
            Level = data[offset++];
            NALSizeMinusOne = data[offset++] & 0x3;
            SPSCount = data[offset++] & 0x1F;
            SPSLength = data.IntFromBigEndian(offset, 2);
            offset += 2;
            SPS = copyBytes(data, offset, SPSLength);
            offset += SPSLength;
            PPSCount = data[offset++];
            PPSLength = data.IntFromBigEndian(offset, 2);
            offset += 2;
            PPS = copyBytes(data, offset, PPSLength);
        }

        public int Version { get; set; }
        public int Profile { get; set; }
        public int Compatability { get; set; }
        public int Level { get; set; }
        public int NALSizeMinusOne { get; set; }
        public int SPSCount { get; set; }
        public int SPSLength { get; set; }
        public byte[] SPS { get; set; }
        public int PPSCount { get; set; }
        public int PPSLength { get; set; }
        public byte[] PPS { get; set; }

        byte[] copyBytes(byte[] src, int offset, int count)
        {
            byte[] dst = new byte[count];
            Buffer.BlockCopy(src, offset, dst, 0, count);
            return dst;
        }
    }
}
