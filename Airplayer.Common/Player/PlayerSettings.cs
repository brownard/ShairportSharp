using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectShow;
using ShairportSharp;
using ShairportSharp.Audio;
using ShairportSharp.Remote;

namespace AirPlayer.Common.Player
{
    public class PlayerSettings
    {
        AudioBufferStream source;
        public PlayerSettings(AudioBufferStream source)
        {
            this.source = source;
        }

        public AudioBufferStream Source { get { return source; } }

        public AMMediaType GetMediaType()
        {
            WaveStream stream = source as WaveStream;            
            if (stream != null)
            {
                return getWaveMediaType(stream);
            }
            return null;
        }

        public uint GetLastTimeStamp()
        {
            if (source != null)
                return source.CurrentTimestamp;
            return 0;
        }

        AMMediaType getWaveMediaType(WaveStream stream)
        {
            WaveFormatEx w = new WaveFormatEx();
            w.wBitsPerSample = (ushort)stream.Header.BitsPerSample;
            w.cbSize = 0;
            w.nChannels = (ushort)stream.Header.Channels;
            w.nSamplesPerSec = stream.Header.SampleRate;
            w.wFormatTag = 1;
            int bytesPerSample = stream.Header.Channels * (stream.Header.BitsPerSample / 8);
            w.nAvgBytesPerSec = w.nSamplesPerSec * bytesPerSample;
            w.nBlockAlign = (ushort)bytesPerSample;

            AMMediaType amt = new AMMediaType();
            amt.majorType = MediaType.Audio;
            amt.subType = MediaSubType.PCM;
            amt.formatType = FormatType.WaveEx;
            amt.SetFormat(w);
            amt.fixedSizeSamples = true;
            amt.sampleSize = 4;
            return amt;
        }

        //Can't get this to work!!
        AMMediaType getAlacMediaType()
        {
            byte[] extraInfo = new byte[] { 0x00, 0x00, 0x00, 0x24, 0x61, 0x6C, 0x61, 0x63, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x01, 0x60, 0x00, 0x10, 0x28, 0x0E, 0x0A, 0x02, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAC, 0x44 };

            WaveFormatEx w = new WaveFormatEx();
            w.wBitsPerSample = 16;
            w.cbSize = (ushort)extraInfo.Length;
            w.nChannels = 2;
            w.nSamplesPerSec = 44100;
            w.wFormatTag = 27745;
            w.nAvgBytesPerSec = 87765;
            w.nBlockAlign = 4;

            AMMediaType amt = new AMMediaType();
            amt.majorType = MediaType.Audio;
            amt.subType = new Guid("63616C61-0000-0010-8000-00AA00389B71"); //ALAC
            amt.formatType = FormatType.WaveEx;
            amt.SetFormat(w);
            amt.AddFormatExtraData(extraInfo);
            amt.fixedSizeSamples = true;
            amt.sampleSize = 4;
            return amt;
        }
    }
}
