using ShairportSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer.Common.H264
{
    static class NaluParser
    {
        public const int START_CODE_LENGTH = 3;
        public static byte[] ParseNalus(byte[] nalus, int lengthSize)
        {
            List<int> lengths = new List<int>();
            int totalLength = 0;
            int nalusLength = nalus.Length;
            int offset = 0;
            while (offset < nalusLength)
            {
                if (!checkSize(nalusLength, offset, lengthSize))
                    break;
                int length = intFromBigEndian(nalus, offset, lengthSize);
                offset += lengthSize;

                if (!checkSize(nalusLength, offset, length))
                    break;
                //if (length > 0 && (nalus[offset] & 0x1F) == 5)
                //    isIdr = true;
                offset += length;
                totalLength += length;
                lengths.Add(length);
            }

            byte[] nalusWithStartCodes = new byte[START_CODE_LENGTH * lengths.Count + totalLength];
            offset = lengthSize;
            int dstOffset = 0;
            foreach (int size in lengths)
            {
                AddStartCodes(nalus, offset, nalusWithStartCodes, dstOffset, size);
                offset += size + lengthSize;
                dstOffset += START_CODE_LENGTH + size;
            }
            return nalusWithStartCodes;
        }

        public static byte[] CreateParameterSet(byte[] sps, byte[] pps)
        {
            int spsLength = sps != null ? sps.Length : 0;
            int ppsLength = pps != null ? pps.Length : 0;
            int length = 0;
            int ppsOffset = 0;
            if (spsLength != 0)
            {
                length = START_CODE_LENGTH + spsLength;
                ppsOffset = length;
            }
            if (ppsLength != 0)
            {
                length += START_CODE_LENGTH + ppsLength;
            }

            byte[] nalu = new byte[length];
            if (spsLength != 0)
                AddStartCodes(sps, 0, nalu, 0, spsLength);
            if (ppsLength != 0)
                AddStartCodes(pps, 0, nalu, ppsOffset, ppsLength);
            return nalu;
        }

        public static void AddStartCodes(byte[] nalu, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            for (int i = 0; i < START_CODE_LENGTH - 1; i++)
                dst[dstOffset + i] = 0x00;

            int actualOffset = dstOffset + START_CODE_LENGTH;
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

        static int intFromBigEndian(byte[] buffer, int offset, int count)
        {
            if (count > 4)
                count = 4;
            int result = 0;
            for (int x = 0; x < count; x++)
            {
                int shift = 8 * (count - 1 - x);
                result = result | (int)buffer[x + offset] << shift;
            }
            return result;
        }
    }
}