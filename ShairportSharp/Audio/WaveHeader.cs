using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    public class WaveHeader
    {
        public WaveHeader(int sampleRate, int bitsPerSample, int channels)
        {
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
            Channels = channels;
        }

        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; }

        public byte[] ToBytes()
        {
            Encoding encoding = Encoding.ASCII;
            using (MemoryStream ms = new MemoryStream())
            {
                int sampleSize = BitsPerSample * Channels / 8; //sample size in bytes

                ms.Write(encoding.GetBytes("RIFF"), 0, 4); //RIFF header
                ms.Write(BitConverter.GetBytes(4), 0, 4); //Riff size
                ms.Write(encoding.GetBytes("WAVE"), 0, 4); //Wave header
                ms.Write(encoding.GetBytes("fmt "), 0, 4); //fmt header
                ms.Write(BitConverter.GetBytes(16), 0, 4); //fmt size
                ms.Write(BitConverter.GetBytes((short)1), 0, 2); //format (1 is PCM)
                ms.Write(BitConverter.GetBytes((short)Channels), 0, 2); //Channels
                ms.Write(BitConverter.GetBytes(SampleRate), 0, 4); //Sample rate
                ms.Write(BitConverter.GetBytes((int)(SampleRate * sampleSize)), 0, 4); //bytes per sec
                ms.Write(BitConverter.GetBytes((short)sampleSize), 0, 2); //total size of each sample
                ms.Write(BitConverter.GetBytes((short)BitsPerSample), 0, 2); //bits per sample
                ms.Write(encoding.GetBytes("data"), 0, 4); //data header
                ms.Write(BitConverter.GetBytes(0), 0, 4); //length of date (unknown so we specify 0)
                return ms.ToArray();
            }
        }
    }
}
