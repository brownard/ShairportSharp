using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using PlistCS;
using System.Globalization;

namespace ShairportSharp.Airplay
{
    class AirplaySession : HttpServer
    {
        AirplayServerInfo serverInfo;

        public AirplaySession(Socket socket, AirplayServerInfo serverInfo)
            : base(socket) 
        {
            this.serverInfo = serverInfo;
        }

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

        public event EventHandler<SlideshowSettingsEventArgs> SlideshowSettingsReceived;
        protected virtual void OnSlideshowSettingsReceived(SlideshowSettingsEventArgs e)
        {
            if (SlideshowSettingsReceived != null)
                SlideshowSettingsReceived(this, e);
        }

        public event EventHandler SlideshowStopped;
        protected virtual void OnSlideshowStopped(EventArgs e)
        {
            if (SlideshowStopped != null)
                SlideshowStopped(this, e);
        }

        public event EventHandler<VideoEventArgs> VideoReceived;
        protected virtual void OnVideoReceived(VideoEventArgs e)
        {
            if (VideoReceived != null)
                VideoReceived(this, e);
        }

        public event EventHandler<PlaybackInfoEventArgs> PlaybackInfoRequested;
        protected virtual void OnPlaybackInfoRequested(PlaybackInfoEventArgs e)
        {
            if (PlaybackInfoRequested != null)
                PlaybackInfoRequested(this, e);
        }

        public string SessionId { get; private set; }

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            SessionId = request["X-Apple-Session-ID"];
            HttpResponse response;

            #region Photos

            if (request.Directory == "/reverse")
            {
                Logger.Debug("Reverse: {0}", request.Directory);
                response = handleEventRequest();
            }

            else if (request.Directory == "/photo")
            {
                response = handlePhoto(request);
            }

            else if (request.Directory == "/slideshow-features")
            {
                response = getSlideshowFeatures();
            }

            else if (request.Directory == "/slideshows/1")
            {
                response = handleSlideshowSettings(request);
            }

            else if (request.Directory == "/stop")
            {
                OnSlideshowStopped(EventArgs.Empty);
                response = getEmptyResponse();
            }

            #endregion

            #region Videos

            else if (request.Directory == "/server-info")
            {
                response = handleServerInfo();
            }

            else if (request.Directory == "/play")
            {
                response = handleVideoPlay(request);
            }

            else if (request.Directory == "/playback-info")
            {
                response = getPlaybackInfo();
            }

            else if (request.Directory.StartsWith("/getProperty"))
            {
                response = getEmptyResponse("404 Not Found");
            }

            else if (request.Directory.StartsWith("/setProperty"))
            {
                response = getEmptyResponse("404 Not Found");
            }

            #endregion

            else
            {
                Logger.Warn("Unhandled Request\r\n{0}", request.ToString());
                response = getEmptyResponse();
            }
            return response;
        }

        HttpResponse handleEventRequest()
        {
            Logger.Debug("Airplay: Event connection received");
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
            PhotoReceivedEventArgs e = new PhotoReceivedEventArgs(request["X-Apple-AssetKey"], request["X-Apple-Transition"], request.Content, request["X-Apple-AssetAction"]);
            Logger.Debug("Airplay: Photo received: Transition '{0}', Action '{1}'", e.Transition, e.AssetAction);
            OnPhotoReceived(e);
            return getEmptyResponse(e.NotInCache ? "412 Precondition Failed" : "200 OK");
        }

        HttpResponse getSlideshowFeatures()
        {
            return getPlistResponse(new Dictionary<string, object>());
        }

        HttpResponse handleSlideshowSettings(HttpRequest request)
        {
            Dictionary<string, object> pList = (Dictionary<string, object>)Plist.readPlist(request.Content);
            string playState = (string)pList["state"];
            Dictionary<string, object> settings = (Dictionary<string, object>)pList["settings"];
            int slideDuration = (int)settings["slideDuration"];
            string theme = (string)settings["theme"];

            OnSlideshowSettingsReceived(new SlideshowSettingsEventArgs(playState, slideDuration, theme));
            return getPlistResponse(new Dictionary<string, object>());
        }

        HttpResponse handleServerInfo()
        {
            return getPlistResponse(serverInfo);
        }

        HttpResponse handleVideoPlay(HttpRequest request)
        {
            Logger.Debug("Airplay: Video Play");
            string contentLocation = null;
            double startPosition = 0;

            if (request["Content-Type"] == "application/x-apple-binary-plist")
            {
                Dictionary<string, object> pList = (Dictionary<string, object>)Plist.readPlist(request.Content);
                //Logger.Debug("Play plist - " + Plist.writeXml(pList));
                if (pList.ContainsKey("Content-Location"))
                    contentLocation = (string)pList["Content-Location"];
                if (pList.ContainsKey("Start-Position"))
                    startPosition = (double)pList["Start-Position"];
            }
            else
            {
                foreach (KeyValuePair<string, string> keyVal in Utils.ParseTextParameters(request.GetContentString()))
                {
                    if (keyVal.Key == "Content-Location")
                        contentLocation = keyVal.Value;
                    else if (keyVal.Key == "Start-Position")
                        startPosition = Convert.ToDouble(keyVal.Value, CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrEmpty(contentLocation))
            {
                Logger.Debug("Airplay: Video received: {0}, {1}", contentLocation, startPosition);
                OnVideoReceived(new VideoEventArgs(contentLocation, startPosition));
            }
            return getEmptyResponse();
        }

        HttpResponse getPlaybackInfo()
        {
            PlaybackInfoEventArgs e = new PlaybackInfoEventArgs();
            OnPlaybackInfoRequested(e);
            return getPlistResponse(e.PlaybackInfo);
        }

        static HttpResponse getEmptyResponse(string statusCode = "200 OK")
        {
            HttpResponse response = new HttpResponse();
            response.Status = "HTTP/1.1 " + statusCode;
            response["Date"] = rfcTimeNow();
            response["Content-Length"] = "0";
            return response;
        }

        static HttpResponse getPlistResponse(IPlistResponse plist)
        {
            return getPlistResponse(plist.GetPlist());
        }

        static HttpResponse getPlistResponse(Dictionary<string, object> plist)
        {
            HttpResponse response = new HttpResponse();
            response.Status = "HTTP/1.1 200 OK";
            response["Date"] = rfcTimeNow();
            response["Content-Type"] = "text/x-apple-plist+xml";
            string plistXml = Plist.writeXml(plist);
            //Logger.Debug("Created plist xml - '{0}'", plistXml);
            response.SetContent(plistXml);
            return response;
        }

        static string rfcTimeNow()
        {
            return string.Format("{0:R}", DateTime.Now);
        }
    }
}
