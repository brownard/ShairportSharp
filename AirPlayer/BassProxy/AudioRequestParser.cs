using ShairportSharp.Audio;
using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AirPlayer.BassProxy
{
    class AudioRequestParser : HttpParser
    {
        WaveStream audioStream;
        public AudioRequestParser(WaveStream audioStream, Socket socket)
            : base(socket)
        {
            this.audioStream = audioStream;
        }

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            HttpResponse response = new HttpResponse("HTTP/1.1");
            response["ContentType"] = "audio/x-wav";
            try
            {
                byte[] buffer = response.GetBytes();
                outputStream.Write(buffer, 0, buffer.Length);
                buffer = audioStream.Header.ToBytes();
                outputStream.Write(buffer, 0, buffer.Length);

                buffer = new byte[16384];
                int read;
                while ((read = audioStream.Read(buffer, 0, buffer.Length)) > 0)
                    outputStream.Write(buffer, 0, read);
            }
            catch { }
            Close();
            return null;
        }
    }
}
