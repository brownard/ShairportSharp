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
    public class MirroringStream
    {
        const int START_CODE_LENGTH = 3;
        object syncRoot = new object();
        bool isWaiting;
        bool isStopped;
        List<MirroringMessage> backBuffer;
        H264CodecData codecData;
        ICryptoTransform c;

        public event EventHandler CodecDataChanged;
        protected virtual void OnCodecDataChanged()
        {
            if (CodecDataChanged != null)
                CodecDataChanged(this, EventArgs.Empty);
        }

        public MirroringStream(MirroringSetup setup, H264CodecData codecData)
        {
            this.codecData = codecData;
            backBuffer = new List<MirroringMessage>();
            //some 3rd party apps don't encrypt the data so don't try and decrypt
            if (setup.AESKey != null)
                c = new Aes128CounterMode(setup.IV).CreateDecryptor(setup.AESKey, null);
        }

        public H264CodecData CodecData 
        {
            get { return codecData; }
            set
            {
                codecData = value;
                OnCodecDataChanged();
            }
        }
        
        public void AddPacket(MirroringMessage packet)
        {
            if (isStopped)
                return;

            if (packet.PayloadType == 0)
            {
                //encrypted video data
                byte[] decrypted = decryptPacket(packet.Content);
                packet.Content = NaluParser.ParseNalus(decrypted, codecData.NALSizeMinusOne + 1, START_CODE_LENGTH);
            }
            else if (packet.PayloadType == 1)
            {
                //unencrypted codec data
                codecData = new H264CodecData(packet.Content);
                //create SPS/PPS nalus
                byte[] nalu = new byte[2 * START_CODE_LENGTH + codecData.SPS.Length + codecData.PPS.Length];
                NaluParser.AddStartCodes(codecData.SPS, 0, nalu, 0, codecData.SPS.Length, START_CODE_LENGTH);
                NaluParser.AddStartCodes(codecData.PPS, 0, nalu, codecData.SPS.Length + START_CODE_LENGTH, codecData.PPS.Length, START_CODE_LENGTH);
                packet.Content = nalu;
            }
            
            lock (syncRoot)
            {
                backBuffer.Add(packet);
                if (isWaiting)
                {
                    isWaiting = false;
                    Monitor.PulseAll(syncRoot);
                }
            }
        }

        public MirroringMessage TakePacket()
        {
            if (isStopped)
                return null;

            lock (syncRoot)
            {
                if (!checkBuffer())
                    return null;

                MirroringMessage packet = backBuffer[0];
                backBuffer.RemoveAt(0);
                return packet;
            }
        }

        public MirroringMessage[] TakeAllPackets()
        {
            if (isStopped)
                return null;

            lock (syncRoot)
            {
                if (!checkBuffer())
                    return null;

                MirroringMessage[] packets = backBuffer.ToArray();
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
            if (c == null)
                return buffer;
            return c.TransformFinalBlock(buffer, 0, buffer.Length);
        }
    }
}