using AlacDecoder;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ShairportSharp.Audio
{
    class AudioBuffer
    {
        const int DEFAULT_BUFFER_SIZE = 256;

        // The array that represents the buffer
        AudioData[] audioBuffer;

        // The lock for writing/reading concurrency
        readonly object syncRoot = new object();
        
        volatile bool bufferStopped = false;

        //Total buffer size (number of frame)
        int maxBufferFrames;

        //Wait 'til there are startFill frames in buffer
        int startFill; 
        
        // Can we read in buffer?
        bool synced = false;
        
        protected ushort actualBufferSize;
        protected ushort readIndex;
        protected ushort writeIndex;
        bool decoderStopped = false; //The decoder stops 'cause the isn't enough packet. Waits till buffer is ok
        bool bufferInit = false;

        DateTime lastBufferUpdate = DateTime.MinValue;
        public event EventHandler<BufferChangedEventArgs> BufferChanged;
        protected virtual void OnBufferChanged(BufferChangedEventArgs e, bool alwaysFire = false)
        {
            if (!alwaysFire)
            {
                DateTime now = DateTime.Now;
                if (now.Subtract(lastBufferUpdate).TotalMilliseconds < bufferUpdateInterval)
                    return;
                lastBufferUpdate = now;
            }

            if (BufferChanged != null)
                BufferChanged(this, e);
        }

        public event EventHandler<BufferChangedEventArgs> BufferReady;
        protected virtual void OnBufferReady(BufferChangedEventArgs e)
        {
            if (BufferReady != null)
                BufferReady(this, e);
        }

        public event EventHandler<MissingPacketEventArgs> MissingPackets;
        protected virtual void OnMissingPackets(MissingPacketEventArgs e)
        {
            if (MissingPackets != null)
                MissingPackets(this, e);
        }

        /// <summary>
        /// Ring buffer for audio data
        /// </summary>
        /// <param name="session"></param>
        public AudioBuffer(int bufferSize)
        {
            initBuffer(bufferSize);
        }

        public int MaxBufferSize
        {
            get { return maxBufferFrames; }
        }

        public int CurrentBufferSize
        {
            get
            {
                lock (syncRoot)
                    return actualBufferSize;
            }
        }

        int bufferUpdateInterval = 500;
        public int BufferUpdateInterval
        {
            get { return bufferUpdateInterval; }
            set { bufferUpdateInterval = value; }
        }

        /// <summary>
        /// Decrypts and adds the audio data to the buffer
        /// </summary>
        /// <param name="seqno">The sequence number of the packet</param>
        /// <param name="timestamp">The timestamp of the packet</param>
        /// <param name="data">The encrypted audio packet</param>
        public void PutPacketInBuffer(ushort seqno, uint timestamp, byte[] data)
        {
            if (bufferStopped)
                return;

            lock (syncRoot)
            {
                if (bufferStopped)
                    return;

                if (!synced)
                {
                    writeIndex = seqno;
                    readIndex = seqno;
                    synced = true;
                }

                AudioData buffer = audioBuffer[seqno % maxBufferFrames];
                if (seqno == writeIndex) 
                {
                    //Packet we expected
                    buffer.Data = data;
                    buffer.TimeStamp = timestamp;
                    buffer.Ready = true;
                    writeIndex++;
                }
                else if (isSequence(writeIndex, seqno)) 
                {
                    //Too early, missed packets between writeIndex and seqno
                    //Logger.Debug("Audio Buffer: Received packet early. Expected: {0}, Received: {1}", writeIndex, seqno);
                    OnMissingPackets(new MissingPacketEventArgs(writeIndex, seqno)); //ask to resend
                    buffer.Data = data;
                    buffer.TimeStamp = timestamp;
                    buffer.Ready = true;
                    //jump to new seq no.
                    writeIndex = (ushort)(seqno + 1); 
                }
                else if (isSequence(readIndex, seqno)) 
                {
                    //Less than write index but greater than read so not yet played, insert
                    //Logger.Debug("Audio Buffer: Received packet late but still in time to play. Expected: {0}, Received: {1}", writeIndex, seqno);
                    buffer.Data = data;
                    buffer.TimeStamp = timestamp;
                    buffer.Ready = true;
                }
                else
                {
                    //Already played 
                    Logger.Warn("Audio Buffer: Received packet late. Expected: {0}, Received: {1}", writeIndex, seqno);
                }

                // The number of packets in buffer
                actualBufferSize = (ushort)(writeIndex - readIndex);
                BufferChangedEventArgs eventArgs = new BufferChangedEventArgs(actualBufferSize, maxBufferFrames);
                OnBufferChanged(eventArgs);

                if (actualBufferSize > startFill)
                {
                    if (!bufferInit)
                    {
                        bufferInit = true;
                        OnBufferReady(eventArgs);
                        Monitor.PulseAll(syncRoot);
                    }
                    else if (decoderStopped)
                        Monitor.PulseAll(syncRoot);
                }
            }
        }

        /// <summary>
        /// Tries to retrieve the next available ALAC frame
        /// </summary>
        /// <param name="frame">Will be set to the retrieved frame if successful</param>
        /// <returns>True if successful, False if waiting for next frame and null if the audio buffer has been stopped</returns>
        public bool? GetNextFrame(out byte[] frame, out uint timeStamp)
        {
            frame = null;
            timeStamp = 0;

            if (bufferStopped)
                return null;

            lock (syncRoot)
            {
                if (bufferStopped)
                    return null;

                if (!bufferInit || actualBufferSize < 1 || !synced)
                {
                    if (synced && actualBufferSize < 1)
                    {
                        //Buffer underrun
                        Logger.Warn("Audio Buffer: Underrun");
                    }
                    // Signal we're waiting and wait for next packet
                    Logger.Debug("Audio Buffer: Waiting for new packet");
                    decoderStopped = true;
                    Monitor.Wait(syncRoot);
                    if (bufferStopped)
                        return null;
                    Logger.Debug("Audio Buffer: New packet received");
                    decoderStopped = false;
                    OnBufferRestart();
                    return false;
                }

                // Overrunning. Restart at a sane distance
                if (actualBufferSize >= maxBufferFrames)
                {
                    Logger.Debug("Buffer overrun");
                    readIndex = (ushort)(writeIndex - startFill);
                }

                AudioData buffer = audioBuffer[readIndex % maxBufferFrames];
                bool ready = buffer.Ready;
                if (ready)
                {
                    timeStamp = buffer.TimeStamp;
                    frame = ProcessNextPacket(buffer.Data);
                    buffer.Ready = false;
                }
                else
                {
                    //Logger.Warn("Audio Buffer: Missing packet {0}", readIndex);
                }

                readIndex++;
                actualBufferSize = (ushort)(writeIndex - readIndex);
                OnPacketTaken();
                OnBufferChanged(new BufferChangedEventArgs(actualBufferSize, maxBufferFrames));

                return ready;
            }
        }

        /// <summary>
        /// Empties the buffer
        /// </summary>
        public void Flush()
        {
            if (bufferStopped)
                return;

            Logger.Debug("Audio Buffer: Flushing");
            lock (syncRoot)
            {
                if (bufferStopped)
                    return;
                actualBufferSize = 0;
                synced = false;
                for (int i = 0; i < maxBufferFrames; i++)
                    audioBuffer[i].Ready = false;
                OnBufferChanged(new BufferChangedEventArgs(0, maxBufferFrames), true);
            }
        }

        /// <summary>
        /// Releases any blocked read requests
        /// </summary>
        public void Stop()
        {
            bufferStopped = true;
            lock (syncRoot)
            {
                actualBufferSize = 0;
                OnBufferChanged(new BufferChangedEventArgs(0, maxBufferFrames), true);
                Monitor.PulseAll(syncRoot);
            }
            Logger.Debug("Audio Buffer: Stopped");
        }

        protected virtual byte[] ProcessNextPacket(byte[] packet)
        {
            return packet;
        }

        protected virtual void OnPacketTaken()
        {

        }

        protected virtual void OnBufferRestart()
        {

        }

        bool isSequence(ushort a, ushort b)
        {
            short c = (short)(b - a);
            return c > 0;
        }

        void initBuffer(int bufferSize)
        {
            maxBufferFrames = bufferSize;
            if (maxBufferFrames < 1)
                maxBufferFrames = DEFAULT_BUFFER_SIZE;
            else if (maxBufferFrames < 2)
                maxBufferFrames = 2;
            else if (maxBufferFrames > ushort.MaxValue)
                maxBufferFrames = ushort.MaxValue + 1;
            //Buffer size must be a power of 2 to ensure alignment when seq no. rolls over to 0
            else if ((maxBufferFrames & (maxBufferFrames - 1)) != 0)
                maxBufferFrames = (int)Math.Pow(2, Math.Ceiling(Math.Log(maxBufferFrames, 2)));

            Logger.Debug("Audio Buffer: Buffer size: {0} frames ({1}s)", maxBufferFrames, Math.Round(((double)352 / 44100) * maxBufferFrames, 1));
            startFill = maxBufferFrames / 2;
            audioBuffer = new AudioData[maxBufferFrames];
            for (int i = 0; i < maxBufferFrames; i++)
                audioBuffer[i] = new AudioData();
        }
    }
}
