using ShairportSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Mirroring
{
    static class NaluParser
    {
        public static byte[] ParseNalus(byte[] nalus, int lengthSize, int startCodeLength)
        {
            List<int> lengths = new List<int>();
            int totalLength = 0;
            int nalusLength = nalus.Length;
            int offset = 0;
            while (offset < nalusLength)
            {
                if (!checkSize(nalusLength, offset, lengthSize))
                    break;
                int length = nalus.IntFromBigEndian(offset, lengthSize);
                offset += lengthSize;

                if (!checkSize(nalusLength, offset, length))
                    break;
                offset += length;
                totalLength += length;
                lengths.Add(length);
            }

            byte[] nalusWithStartCodes = new byte[startCodeLength * lengths.Count + totalLength];
            offset = lengthSize;
            int dstOffset = 0;
            foreach (int size in lengths)
            {
                AddStartCodes(nalus, offset, nalusWithStartCodes, dstOffset, size, startCodeLength);
                offset += size + lengthSize;
                dstOffset += startCodeLength + size;
            }
            return nalusWithStartCodes;
        }

        public static void AddStartCodes(byte[] nalu, int srcOffset, byte[] dst, int dstOffset, int count, int startCodeLength)
        {
            for (int i = 0; i < startCodeLength - 1; i++)
                dst[dstOffset + i] = 0x00;

            int actualOffset = dstOffset + startCodeLength;
            dst[actualOffset - 1] = 0x01;
            Buffer.BlockCopy(nalu, srcOffset, dst, actualOffset, count);
        }

        static bool checkSize(int length, int offset, int required)
        {
            if (length - offset < required)
            {
                Logger.Warn("NaluParser: Bad nalu size - required {0}, actual {1}", required, length - offset);
                return false;
            }
            return true;
        }
    }
}