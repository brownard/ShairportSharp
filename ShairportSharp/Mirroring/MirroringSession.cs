using ShairportSharp.Base;
using ShairportSharp.Http;
using ShairportSharp.Sap;
using ShairportSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using ShairportSharp.Plist;

namespace ShairportSharp.Mirroring
{
    public class MirroringInfoEventArgs : EventArgs
    {
        public MirroringInfoEventArgs()
        {
            MirroringInfo = new MirroringInfo();
        }

        public MirroringInfo MirroringInfo { get; private set; }
    }

    public class MirroringStartedEventArgs : EventArgs
    {
        public MirroringStartedEventArgs(MirroringStream stream)
        {
            Stream = stream;
        }

        public MirroringStream Stream { get; private set; }
    }

    public class MirroringSession : SapSession
    {
        const string DIGEST_REALM = "AirPlay";

        MirroringSetup mirroringSetup;
        MirroringStream mirroringStream;
        MirroringMessageBuffer mirroingMessageBuffer;

        public MirroringSession(Socket socket, string password = null)
            : base(socket, password, DIGEST_REALM)
        {
            mirroingMessageBuffer = new MirroringMessageBuffer();
            mirroingMessageBuffer.MirroringMessageReceived += messageBuffer_MirroringMessageReceived;
            messageBuffer = mirroingMessageBuffer;
        }

        public event EventHandler<MirroringInfoEventArgs> MirroringInfoRequested;
        protected virtual void OnMirroringInfoRequested(MirroringInfoEventArgs e)
        {
            if (MirroringInfoRequested != null)
                MirroringInfoRequested(this, e);
        }

        public event EventHandler<MirroringStartedEventArgs> Started;
        protected virtual void OnStarted(MirroringStartedEventArgs e)
        {
            if (Started != null)
                Started(this, e);
        }

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            if (!IsAuthorised(request))
            {
                HttpResponse authResponse = HttpUtils.GetEmptyResponse("401 Unauthorized");
                authResponse.SetHeader("WWW-Authenticate", string.Format("Digest realm=\"{0}\" nonce=\"{1}\"", DIGEST_REALM, nonce));
                return authResponse;
            }

            HttpResponse response = null;
            if (request.Method == "GET")
            {
                if (request.Uri == "/stream.xml")
                {
                    MirroringInfoEventArgs e = new MirroringInfoEventArgs();
                    OnMirroringInfoRequested(e);
                    response = HttpUtils.GetPlistResponse(e.MirroringInfo);
                }
            }
            else if (request.Method == "POST")
            {
                if (request.Uri == "/fp-setup")
                {
                    Logger.Debug("MirroringSession: Received fp-setup");
                    response = handleFpRequest(request);
                }
                else if (request.Uri == "/stream")
                {
                    handleStream(request);
                    return null;
                }
            }

            if (response == null)
                Logger.Warn("MirroringSession: Unhandled request {0}\r\n{1}", request.Uri, request);
            return response;
        }

        protected override void Close(bool manualClose)
        {
            if (mirroringStream != null)
                mirroringStream.Stop();
            base.Close(manualClose);
        }

        void messageBuffer_MirroringMessageReceived(object sender, MirroringMessageEventArgs e)
        {
            PayloadType payloadType = e.Message.PayloadType;
            if (payloadType == PayloadType.Video)
            {
                if (mirroringStream != null)
                    mirroringStream.AddPacket(e.Message);
            }
            else if (payloadType == PayloadType.Codec)
            {
                var codecData = new H264CodecData(e.Message.Payload);
                if (mirroringStream == null)
                {
                    mirroringStream = new MirroringStream(mirroringSetup, codecData);
                    OnStarted(new MirroringStartedEventArgs(mirroringStream));
                }
                mirroringStream.AddPacket(e.Message);
            }
            //else if (payloadType == PayloadType.Heartbeat)
            //{
            //    //Heartbeat
            //}
            //else
            //{
            //    //Unknown
            //}
        }

        void handleStream(HttpRequest request)
        {
            Dictionary<string, object> plist;
            if (!PlistParser.TryGetPlist(request.Content, out plist))
                return;

            mirroringSetup = new MirroringSetup(plist);
            if (mirroringSetup.FPKey != null)
            {
                mirroringSetup.AESKey = DecryptSapKey(mirroringSetup.FPKey);
                Logger.Debug("MirroringSession: Received AES key - {0}", mirroringSetup.AESKey.HexStringFromBytes());
            }
            mirroingMessageBuffer.IsDataMode = true;
        }

        HttpResponse handleFpRequest(HttpRequest request)
        {
            byte[] fpResponse = GetSapResponse(request.Content);
            HttpResponse response = HttpUtils.GetEmptyResponse();
            response.SetHeader("Content-Type", "application/octet-stream");
            response.SetContent(fpResponse);
            return response;
        }
    }
}
