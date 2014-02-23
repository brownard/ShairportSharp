using AlacDecoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    class AudioSession
    {
        AlacFile alacFile;
        byte[] aesIV;
        byte[] aesKey;
        int controlPort;
        int timingPort;
        int frameSize;
        int sampleSize;
        int _7a;
        int rice_historymult;
        int rice_initialhistory;
        int rice_kmodifier;
        int channels;
        int _80;
        int _82;
        int _86;
        int sampleRate;
        BiquadFilter bFilter;
        int outputSize;
        int bufferSize;

        public AudioSession(byte[] aesiv, byte[] aeskey, int[] fmtp, int controlPort, int timingPort, int bufferSize)
        {
            // KEYS
            this.aesIV = aesiv;
            this.aesKey = aeskey;

            // PORTS
            this.controlPort = controlPort;
            this.timingPort = timingPort;

            this.bufferSize = bufferSize;

            // FMTP
            frameSize = fmtp[1];
            _7a = fmtp[2];
            sampleSize = fmtp[3];
            rice_historymult = fmtp[4];
            rice_initialhistory = fmtp[5];
            rice_kmodifier = fmtp[6];
            channels = fmtp[7];
            _80 = fmtp[8];
            _82 = fmtp[9];
            _86 = fmtp[10];
            sampleRate = fmtp[11];
            outputSize = 4 * (frameSize + 3);
            alacFile = createAlac();
        }

        AlacFile createAlac()
        {
            if (sampleSize != 16)
                return null;
            
            AlacFile alac = AlacDecodeUtils.create_alac(sampleSize, 2);            
            alac.setinfo_max_samples_per_frame = frameSize;
            alac.setinfo_7a = _7a;
            alac.setinfo_sample_size = sampleSize;
            alac.setinfo_rice_historymult = rice_historymult;
            alac.setinfo_rice_initialhistory = rice_initialhistory;
            alac.setinfo_rice_kmodifier = rice_kmodifier;
            alac.setinfo_7f = channels;
            alac.setinfo_80 = _80;
            alac.setinfo_82 = _82;
            alac.setinfo_86 = _86;
            alac.setinfo_8a_rate = sampleRate;
            return alac;
        }

        public void ResetFilter()
        {
            bFilter = new BiquadFilter(sampleSize, frameSize);
        }

        public void UpdateFilter(int size)
        {
            if (bFilter == null)
                ResetFilter();
            bFilter.Update(size);
        }

        public AlacFile AlacFile
        {
            get { return alacFile; }
        }

        public int OutputSize
        {
            get { return outputSize; }
        }

        public BiquadFilter Filter
        {
            get { return bFilter; }
        }

        public byte[] AesIV
        {
            get { return aesIV; }
        }

        public byte[] AesKey
        {
            get { return aesKey; }
        }

        public int ControlPort
        {
            get { return controlPort; }
        }

        public int TimingPort
        {
            get { return timingPort; }
        }

        public int FrameSize
        {
            get { return frameSize; }
        }

        public int SampleSize
        {
            get { return sampleSize; }
        }

        public int SampleRate
        {
            get { return sampleRate; }
        }

        public int Channels
        {
            get { return channels; }
        }

        public int BufferSize
        {
            get { return bufferSize; }
        }

        public int BufferSizeToFrames()
        {
            return (int)Math.Ceiling(((double)BufferSize * SampleRate) / (FrameSize * 1000)) * 2; //milliseconds to number of frames (doubled)
        }
    }
}
