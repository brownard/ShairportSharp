using ShairportSharp.Audio;
using ShairportSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirPlayer.BassProxy
{
    class AudioProxy : Proxy
    {
        WaveStream audioStream;

        public AudioProxy(WaveStream audioStream)
        {
            this.audioStream = audioStream;
        }

        public override ShairportSharp.Http.HttpParser ConnectionAccepted(System.Net.Sockets.Socket socket)
        {
            return new AudioRequestParser(audioStream, socket);
        }
    }
}
