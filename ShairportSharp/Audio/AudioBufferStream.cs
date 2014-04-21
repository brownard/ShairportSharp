using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    public enum StreamType
    {
        Alac,
        Wave
    }

    public class AudioBufferStream : Stream
    {
        AudioBuffer audioBuffer;
        AudioSession audioSession;

        object timestampLock = new object();
        uint currentTimestamp;
        double packetDuration;
        double currentPosition;

        byte[] currentData = null;
        int currentDataLength = 0;
        int currentDataOffset = 0;

        internal AudioBufferStream(AudioBuffer audioBuffer, AudioSession audioSession)
        {
            this.audioBuffer = audioBuffer;
            this.audioSession = audioSession;
            packetDuration = audioSession.FrameSize / (double)audioSession.SampleRate;
        }

        public int SampleRate { get { return audioSession.SampleRate; } }
        public int SampleSize { get { return audioSession.SampleSize; } }
        public int Channels { get { return audioSession.Channels; } }

        public uint CurrentTimestamp
        {
            get { lock (timestampLock) return currentTimestamp; }
            private set { lock (timestampLock) currentTimestamp = value; }
        }

        public void GetPosition(out uint currentTimestamp, out double currentPosition)
        {
            lock (timestampLock)
            {
                currentTimestamp = this.currentTimestamp;
                currentPosition = this.currentPosition;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remainingBytes = count;
            int bytesRead = 0;
            if (currentData != null && currentDataOffset < currentDataLength)
            {
                remainingBytes = currentDataLength - currentDataOffset;
                bytesRead = remainingBytes > count ? count : remainingBytes;
                Buffer.BlockCopy(currentData, currentDataOffset, buffer, offset, bytesRead);
                currentDataOffset += bytesRead;
                if (currentDataOffset < currentDataLength)
                {
                    position += bytesRead;
                    return bytesRead;
                }
            }

            currentData = null;
            remainingBytes = count - bytesRead;
            if (remainingBytes > 0)
            {
                uint timestamp = 0;
                uint timestampTemp;
                int packetsRead = 0;
                while (true)
                {
                    bool? result = audioBuffer.GetNextFrame(out currentData, out timestampTemp);
                    if (result == null)
                        break;
                    else if (result == true)
                        timestamp = timestampTemp;

                    currentData = ProcessPacket(currentData, out currentDataLength);

                    if (currentData != null)
                    {
                        packetsRead++;
                        currentDataOffset = remainingBytes > currentDataLength ? currentDataLength : remainingBytes;
                        Buffer.BlockCopy(currentData, 0, buffer, offset + bytesRead, currentDataOffset);
                        bytesRead += currentDataOffset;
                        remainingBytes = count - bytesRead;
                        if (remainingBytes == 0)
                            break;
                    }
                }
                if (timestamp != 0)
                {
                    lock (timestampLock)
                    {
                        currentTimestamp = timestamp;
                        currentPosition += packetsRead * packetDuration;
                    }
                }
            }

            position += bytesRead;
            return bytesRead;
        }

        protected virtual byte[] ProcessPacket(byte[] packet, out int packetLength)
        {
            if (packet != null && packet.Length > AlacDecoder.Consts.EXTRA_BUFFER_SPACE)
                packetLength = packet.Length - AlacDecoder.Consts.EXTRA_BUFFER_SPACE;
            else
                packetLength = 0;
            return packet;
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        long position = 0;
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {

            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
