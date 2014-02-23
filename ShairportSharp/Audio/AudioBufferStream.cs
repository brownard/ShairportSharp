using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    public enum StreamType
    {
        Wave,
        Raw
    }

    public class AudioBufferStream : Stream
    {
        AudioBuffer audioBuffer;
        int currentOffset = 0;
        byte[] currentData = null;
        int currentDataLength = 0;
        object timeStampLock = new object();
        uint currentTimeStamp;

        internal AudioBufferStream(AudioBuffer audioBuffer)
        {
            this.audioBuffer = audioBuffer;
        }

        public uint CurrentTimeStamp
        {
            get { lock (timeStampLock) return currentTimeStamp; }
            private set { lock (timeStampLock) currentTimeStamp = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remainingBytes = count;
            int bytesRead = 0;
            if (currentData != null && currentOffset < currentDataLength)
            {
                remainingBytes = currentDataLength - currentOffset;
                bytesRead = remainingBytes > count ? count : remainingBytes;
                Buffer.BlockCopy(currentData, currentOffset, buffer, offset, bytesRead);
                currentOffset += bytesRead;
                if (currentOffset < currentDataLength)
                {
                    position += bytesRead;
                    return bytesRead;
                }
            }

            currentData = null;
            remainingBytes = count - bytesRead;
            if (remainingBytes > 0)
            {
                uint timeStamp = 0, timeStampTemp;
                while (true)
                {
                    bool? result = audioBuffer.GetNextFrame(out currentData, out timeStampTemp);//GetNextPacket(out currentData, out currentDataLength, out timeStampTemp);
                    if (result == null)
                        break;

                    if (timeStampTemp != 0)
                        timeStamp = timeStampTemp;

                    currentData = ProcessPacket(currentData, out currentDataLength);

                    if (currentData != null)
                    {
                        currentOffset = remainingBytes > currentDataLength ? currentDataLength : remainingBytes;
                        Buffer.BlockCopy(currentData, 0, buffer, offset + bytesRead, currentOffset);
                        bytesRead += currentOffset;
                        remainingBytes = count - bytesRead;
                        if (remainingBytes == 0)
                            break;
                    }
                }
                if (timeStamp != 0)
                    CurrentTimeStamp = timeStamp;
            }

            position += bytesRead;
            return bytesRead;
        }

        protected virtual byte[] ProcessPacket(byte[] packet, out int packetLength)
        {
            if (packet != null)
                packetLength = packet.Length;
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
            get { return long.MaxValue; }
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
