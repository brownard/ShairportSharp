using ShairportSharp.Airplay;
using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Mirroring
{
    public class MirroringMessageEventArgs : EventArgs
    {
        public MirroringMessageEventArgs(MirroringMessage message)
        {
            Message = message;
        }

        public MirroringMessage Message { get; private set; }
    }

    class MirroringMessageBuffer : HttpMessageBuffer
    {
        const int HEADER_LENGTH = 128;
        int currentHeaderIndex;
        MirroringMessage currentMessage;

        public event EventHandler<MirroringMessageEventArgs> MirroringMessageReceived;
        protected virtual void OnMirroringMessageReceived(MirroringMessageEventArgs e)
        {
            if (MirroringMessageReceived != null)
                MirroringMessageReceived(this, e);
        }

        public bool IsDataMode { get; set; }

        protected override bool IsEndOfHeader(byte b)
        {
            if (!IsDataMode)
                return base.IsEndOfHeader(b);
            
            currentHeaderIndex++;
            bool result = currentHeaderIndex == HEADER_LENGTH;
            if (result)
                currentHeaderIndex = 0;
            return result;
        }

        protected override int OnMessage(byte[] messageData)
        {
            if (!IsDataMode)
                return base.OnMessage(messageData);

            currentMessage = new MirroringMessage(messageData);
            int size = (int)currentMessage.PayloadSize;
            if (size == 0)
                OnMirroringMessageReceived(new MirroringMessageEventArgs(currentMessage));
            return size;
        }

        protected override void OnContent(byte[] content)
        {
            if (!IsDataMode)
                base.OnContent(content);

            if (currentMessage != null)
            {
                currentMessage.Payload = content;
                OnMirroringMessageReceived(new MirroringMessageEventArgs(currentMessage));
                currentMessage = null;
            }
        }
    }
}
