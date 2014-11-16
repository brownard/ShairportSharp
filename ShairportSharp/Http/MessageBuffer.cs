using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShairportSharp.Http
{
    abstract class MessageBuffer
    {
        byte[] backBuffer;
        int bufferOffset;

        byte[] messageDelimiter;
        int delimeterIndex;
        int contentLength;

        public MessageBuffer(byte[] messageDelimiter)
        {
            this.messageDelimiter = messageDelimiter;
            backBuffer = new byte[131072];
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            checkBufferSize(count);

            for (int i = 0; i < count; i++)
            {
                byte b = buffer[offset + i];
                backBuffer[bufferOffset++] = b;
                if (contentLength > 0)
                {
                    if (bufferOffset == contentLength)
                    {
                        OnContent(getAndResetBuffer());
                        contentLength = 0;
                    }
                }
                else if (b == messageDelimiter[delimeterIndex])
                {
                    if (delimeterIndex == messageDelimiter.Length - 1)
                    {
                        contentLength = OnMessage(getAndResetBuffer());
                        delimeterIndex = 0;
                    }
                    else
                    {
                        delimeterIndex++;
                    }
                }
                else
                {
                    delimeterIndex = 0;
                }
            }
        }

        void checkBufferSize(int count)
        {
            int sizeNeeded = count + bufferOffset;
            if (sizeNeeded - backBuffer.Length > 0)
            {
                int newSize = backBuffer.Length * 2;
                while (newSize < sizeNeeded)
                    newSize = newSize * 2;
                Logger.Debug("MessageBuffer: Resized to {0} bytes", newSize);
                byte[] oldBuffer = backBuffer;
                backBuffer = new byte[newSize];
                Buffer.BlockCopy(oldBuffer, 0, backBuffer, 0, bufferOffset);
            }
        }

        byte[] getAndResetBuffer()
        {
            byte[] buffer = new byte[bufferOffset];
            Buffer.BlockCopy(backBuffer, 0, buffer, 0, bufferOffset);
            bufferOffset = 0;
            return buffer;
        }

        protected abstract int OnMessage(byte[] messageData);
        protected abstract void OnContent(byte[] content);
    }
}
