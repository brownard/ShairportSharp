using AlacDecoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ShairportSharp.Audio;

namespace ShairportSharp.Audio
{
    public class WaveStream : AudioBufferStream
    {
        const int RANDOM_MAX = 0x7fff;
        long fixVolume = 0x10000;
        short randomA, randomB;
        Random random = new Random();
        bool manageBuffer;

        AudioSession session;

        public WaveHeader Header { get; private set; }

        internal WaveStream(AudioBuffer audioBuffer, AudioSession session, bool manageBuffer)
            : base(audioBuffer)
        {
            this.session = session;
            this.manageBuffer = manageBuffer;
            Header = new WaveHeader(session.SampleRate, session.SampleSize, session.Channels);
        }

        protected override byte[] ProcessPacket(byte[] packet, out int packetLength)
        {
            packet = processPacket(packet);
            packetLength = packet.Length;
            return packet;
        }

        byte[] processPacket(byte[] alacFrame)
        {
            if (alacFrame == null)
                return new byte[session.FrameSize * 4];

            int[] outBuffer = new int[(session.FrameSize + 3) * 2];
            int outputsize = AlacDecodeUtils.DecodeFrame(session.AlacFile, alacFrame, outBuffer);
            if (outputsize != session.FrameSize * 4)
                Logger.Warn("Alac Decoder: Unexpected audio frame size. Expected: {0}, Actual: {1}", session.FrameSize * 4, outputsize);
            
            int actualDataLength;
            if (manageBuffer)
            {
                int[] output = new int[outBuffer.Length];
                actualDataLength = stuffBuffer(session.Filter.PlaybackRate, outBuffer, output) * 2;
                outBuffer = output;
            }
            else
            {
                actualDataLength = session.FrameSize * 2;
            }

            byte[] pcmData = new byte[actualDataLength * 2];
            int j = 0;
            for (int i = 0; i < actualDataLength; i++)
            {
                //PCM data is received in big endian - we need little
                pcmData[j++] = (byte)(outBuffer[i]);
                pcmData[j++] = (byte)(outBuffer[i] >> 8);
            }
            return pcmData;
        }

        private int stuffBuffer(double playbackRate, int[] input, int[] output)
        {
            int stuffSamples = session.FrameSize;
            int stuff = 0;
            double pStuff = 1.0 - Math.Pow(1.0 - Math.Abs(playbackRate - 1.0), stuffSamples);

            if (random.Next(RANDOM_MAX) < pStuff * RANDOM_MAX)
            {
                stuff = playbackRate > 1.0 ? -1 : 1;
                stuffSamples = (int)(random.Next(RANDOM_MAX) % (session.FrameSize - 1));
            }
            Logger.Debug("pStuff: {0}, stuff: {1}, stuffSamp: {2}", pStuff, stuff, stuffSamples);

            int j = 0;
            int l = 0;
            for (int i = 0; i < stuffSamples; i++)
            {   // the whole frame, if no stuffing
                output[j++] = ditheredVolume(input[l++]);
                output[j++] = ditheredVolume(input[l++]);
            }

            if (stuff != 0)
            {
                if (l < 2)
                    l = 2;
                if (stuff == 1)
                {
                    // interpolate one sample
                    output[j++] = ditheredVolume(((int)input[l - 2] + (int)input[l]) >> 1);
                    output[j++] = ditheredVolume(((int)input[l - 1] + (int)input[l + 1]) >> 1);
                }
                else if (stuff == -1)
                {
                    l -= 2;
                }

                for (int i = stuffSamples; i < session.FrameSize + stuff; i++)
                {
                    output[j++] = ditheredVolume(input[l++]);
                    output[j++] = ditheredVolume(input[l++]);
                }
            }
            Logger.Debug("Stuff size: {0}", session.FrameSize + stuff);
            return session.FrameSize + stuff;
        }

        private short ditheredVolume(int sample)
        {
            long output;
            randomB = randomA;
            randomA = (short)(random.NextDouble() * 65535);

            output = (long)sample * fixVolume;
            if (fixVolume < 0x10000)
            {
                output += randomA;
                output -= randomB;
            }
            return (short)(output >> 16);
        }
    }
}
