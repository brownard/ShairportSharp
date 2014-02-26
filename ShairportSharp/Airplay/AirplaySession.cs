using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ShairportSharp.Airplay
{
    class AirplaySession : HttpServer
    {
        public AirplaySession(Socket socket)
            : base(socket) { }

        public event EventHandler EventConnection;
        protected virtual void OnEventConnection()
        {
            if (EventConnection != null)
                EventConnection(this, EventArgs.Empty);
        }

        public event EventHandler<PhotoReceivedEventArgs> PhotoReceived;
        protected virtual void OnPhotoReceived(PhotoReceivedEventArgs e)
        {
            if (PhotoReceived != null)
                PhotoReceived(this, e);
        }

        public string SessionId { get; private set; }

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            SessionId = request["X-Apple-Session-ID"];
                        
            if (request.Directory == "/reverse")
            {
                return handleEventRequest();
            }

            if (request.Directory == "/photo")
            {
                return handlePhoto(request);
            }

            return getOKResponse();
        }

        HttpResponse handleEventRequest()
        {
            OnEventConnection();
            HttpResponse response = new HttpResponse();
            response.Status = "HTTP/1.1 101 Switching Protocols";
            response["Date"] = rfcTimeNow();
            response["Upgrade"] = "PTTH/1.0";
            response["Connection"] = "Upgrade";
            return response;
        }

        HttpResponse handlePhoto(HttpRequest request)
        {
            OnPhotoReceived(new PhotoReceivedEventArgs(request["X-Apple-AssetKey"], request["X-Apple-Transition"], request["X-Apple-AssetAction"], request.Content));
            return getOKResponse();
        }

        static HttpResponse getOKResponse()
        {
            HttpResponse response = new HttpResponse();
            response.Status = "HTTP/1.1 200 OK";
            response["Date"] = rfcTimeNow();
            response["Content-Length"] = "0";
            return response;
        }

        static string rfcTimeNow()
        {
            return string.Format("{0:R}", DateTime.Now);
        }
    }
}
