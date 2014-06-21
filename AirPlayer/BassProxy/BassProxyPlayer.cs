using MediaPortal.Player;
using ShairportSharp.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer.BassProxy
{
    class BassProxyPlayer : IPlayer, IAudioPlayer
    {
        AudioProxy proxy;
        WaveStream audioStream;
        object positionLock = new object();
        uint startStamp;
        uint stopStamp;
        double duration;

        public BassProxyPlayer(WaveStream audioStream)
        {
            this.audioStream = audioStream;
        }

        public override bool Play(string filePath)
        {
            if (BassMusicPlayer.BassFreed)
                BassMusicPlayer.Player.InitBass();
            proxy = new AudioProxy(audioStream);
            proxy.Start();
            string proxyUrl = proxy.GetProxyUrl(filePath);
            return BassMusicPlayer.Player.Play(proxyUrl);
        }

        public override void Stop()
        {
            BassMusicPlayer.Player.Stop();
        }

        public override void Dispose()
        {
            if (proxy != null)
            {
                proxy.Stop();
                proxy = null;
            }
            BassMusicPlayer.Player.Dispose();
        }

        public void UpdateDurationInfo(uint startStamp, uint stopStamp)
        {
            lock (positionLock)
            {
                this.startStamp = startStamp;
                this.stopStamp = stopStamp;
                duration = (stopStamp - startStamp) / (double)audioStream.Header.SampleRate;
            }
        }

        public override double Duration
        {
            get
            {
                lock (positionLock)
                    return duration;
            }
        }

        public override double CurrentPosition
        {
            get
            {
                uint currentTimeStamp;
                double currentPosition;
                audioStream.GetPosition(out currentTimeStamp, out currentPosition);
                double offset = currentPosition - base.CurrentPosition;
                
                double position;
                lock (positionLock)
                    position = ((currentTimeStamp - startStamp) / (double)audioStream.Header.SampleRate) - offset;
                if (position < 0)
                    position = 0;
                return position;
            }
        }

        public override bool CanSeek()
        {
            return false;
        }

        public override void SeekAbsolute(double dTime) { }
        public override void SeekRelative(double dTime) { }
        public override void SeekAsolutePercentage(int iPercentage) { }
        public override void SeekRelativePercentage(int iPercentage) { }
    }
}
