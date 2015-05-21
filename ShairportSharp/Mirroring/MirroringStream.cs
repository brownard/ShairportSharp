using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using ShairportSharp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ShairportSharp.Mirroring
{
    public class MirroringPacket
    {
        public ulong NTPTimeStamp { get; set; }
        public byte[] Nalus { get; set; }
        public H264CodecData CodecData { get; set; }
    }

    public class MirroringStream
    {
        object syncRoot = new object();
        bool isWaiting;
        bool isStopped;
        Queue<MirroringPacket> backBuffer;
        H264CodecData codecData;
        ICryptoTransform cipher;

        public MirroringStream(MirroringSetup setup, H264CodecData codecData)
        {
            this.codecData = codecData;
            backBuffer = new Queue<MirroringPacket>();
            //some 3rd party apps don't encrypt the data so don't try and decrypt
            if (setup.AESKey != null)
                cipher = new Aes128CounterMode(setup.IV).CreateDecryptor(setup.AESKey, null);
        }

        public H264CodecData CodecData 
        {
            get { return codecData; }
        }

        public void AddPacket(MirroringMessage message)
        {
            if (isStopped)
                return;
            
            MirroringPacket packet = new MirroringPacket();
            if (message.PayloadType == PayloadType.Video)
            {
                //encrypted video data
                byte[] decrypted = decryptPacket(message.Payload);
                packet.Nalus = decrypted;
            }
            else if (message.PayloadType == PayloadType.Codec)
            {
                //unencrypted codec data
                codecData = new H264CodecData(message.Payload);
                packet.CodecData = codecData;
            }
            else
            {
                Logger.Warn("MirroringStream: Tried to add incorrect PayloadType '{0}'", message.PayloadType);
                return;
            }

            lock (syncRoot)
            {
                if (isStopped)
                    return;

                backBuffer.Enqueue(packet);
                if (isWaiting)
                {
                    isWaiting = false;
                    Monitor.PulseAll(syncRoot);
                }
            }
        }

        public MirroringPacket TakePacket()
        {
            if (isStopped)
                return null;

            lock (syncRoot)
            {
                if (!checkBuffer())
                    return null;

                MirroringPacket packet = backBuffer.Dequeue();
                return packet;
            }
        }

        public MirroringPacket[] TakeAllPackets()
        {
            if (isStopped)
                return null;

            lock (syncRoot)
            {
                if (!checkBuffer())
                    return null;

                MirroringPacket[] packets = backBuffer.ToArray();
                backBuffer.Clear();
                return packets;
            }
        }

        public void Stop()
        {
            lock (syncRoot)
            {
                isStopped = true;
                Monitor.PulseAll(syncRoot);
            }
        }

        bool checkBuffer()
        {
            if (isStopped)
                return false;

            if (backBuffer.Count == 0)
            {
                isWaiting = true;
                Monitor.Wait(syncRoot);
                if (isStopped)
                    return false;
            }
            return true;
        }

        byte[] decryptPacket(byte[] buffer)
        {
            if (cipher == null)
                return buffer;
            return cipher.TransformFinalBlock(buffer, 0, buffer.Length);
        }
    }
}