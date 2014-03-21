using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using PlistCS;
using System.Globalization;
using ShairportSharp.Helpers;

namespace ShairportSharp.Airplay
{
    class AirplaySession : HttpParser
    {
        #region Variables

        const string DIGEST_REALM = "AirPlay";
        AirplayServerInfo serverInfo;

        #endregion

        #region Public Properties

        public string SessionId { get; private set; }

        #endregion

        #region Ctor

        public AirplaySession(Socket socket, AirplayServerInfo serverInfo, string password = null)
            : base(socket, password, DIGEST_REALM) 
        {
            this.serverInfo = serverInfo;
        }

        #endregion

        #region Generic Events

        public event EventHandler<AirplayEventArgs> EventConnection;
        protected virtual void OnEventConnection(AirplayEventArgs e)
        {
            if (EventConnection != null)
                EventConnection(this, e);
        }

        public event EventHandler<AirplayEventArgs> Stopped;
        protected virtual void OnStopped(AirplayEventArgs e)
        {
            if (Stopped != null)
                Stopped(this, e);
        }

        #endregion

        #region Photo Events

        public event EventHandler<PhotoReceivedEventArgs> PhotoReceived;
        protected virtual void OnPhotoReceived(PhotoReceivedEventArgs e)
        {
            if (PhotoReceived != null)
                PhotoReceived(this, e);
        }

        public event EventHandler<SlideshowFeaturesEventArgs> SlideshowFeaturesRequested;
        protected virtual void OnSlideshowFeaturesRequested(SlideshowFeaturesEventArgs e)
        {
            if (SlideshowFeaturesRequested != null)
                SlideshowFeaturesRequested(this, e);
        }

        public event EventHandler<SlideshowSettingsEventArgs> SlideshowSettingsReceived;
        protected virtual void OnSlideshowSettingsReceived(SlideshowSettingsEventArgs e)
        {
            if (SlideshowSettingsReceived != null)
                SlideshowSettingsReceived(this, e);
        }

        #endregion

        #region Video Events

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

        public event EventHandler<PlaybackRateEventArgs> PlaybackRateChanged;
        protected virtual void OnPlaybackRateChanged(PlaybackRateEventArgs e)
        {
            if (PlaybackRateChanged != null)
                PlaybackRateChanged(this, e);
        }

        public event EventHandler<PlaybackPositionEventArgs> PlaybackPositionChanged;
        protected virtual void OnPlaybackPositionChanged(PlaybackPositionEventArgs e)
        {
            if (PlaybackPositionChanged != null)
                PlaybackPositionChanged(this, e);
        }

        public event EventHandler<GetPlaybackPositionEventArgs> GetPlaybackPosition;
        protected virtual void OnGetPlaybackPosition(GetPlaybackPositionEventArgs e)
        {
            if (GetPlaybackPosition != null)
                GetPlaybackPosition(this, e);
        }

        #endregion

        #region Overrides

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            if (!IsAuthorised(request))
            {
                HttpResponse authResponse = getEmptyResponse("401 Unauthorized");
                authResponse.SetHeader("WWW-Authenticate", string.Format("Digest realm=\"{0}\" nonce=\"{1}\"", DIGEST_REALM, nonce));
                return authResponse;
            }

            string sessionId = request["X-Apple-Session-ID"];
            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!string.IsNullOrEmpty(SessionId) && sessionId != SessionId)
                    Logger.Warn("Airplay Session: New SessionId received, old '{0}', new '{1}'", SessionId, sessionId);
                SessionId = sessionId;
            }

            HttpResponse response = null;

            if (request.Uri == "/reverse")
            {
                response = handleEventRequest();
            }

            #region Photos

            else if (request.Uri == "/photo")
            {
                response = handlePhoto(request);
            }

            else if (request.Uri == "/slideshow-features")
            {
                response = getSlideshowFeatures();
            }

            else if (request.Uri == "/slideshows/1")
            {
                response = handleSlideshowSettings(request);
            }

            #endregion

            #region Videos

            else if (request.Uri == "/server-info")
            {
                response = handleServerInfo();
            }

            else if (request.Uri == "/play")
            {
                response = handleVideoPlay(request);
            }

            else if (request.Uri == "/playback-info")
            {
                response = getPlaybackInfo();
            }

            else if (request.Uri.StartsWith("/rate"))
            {
                response = setPlaybackRate(request);
            }

            else if (request.Uri.StartsWith("/scrub"))
            {
                if (request.Method == "POST")
                    response = setPlaybackPosition(request);
                else
                    response = getPlaybackPosition();
            }

            else if (request.Uri.StartsWith("/getProperty"))
            {
                //Logger.Debug("Airplay Session: getProperty\r\n{0}", request);
                response = getEmptyResponse("404 Not Found");
            }

            else if (request.Uri.StartsWith("/setProperty"))
            {
                //Logger.Debug("Airplay Session: setProperty\r\n{0}", request);
                //if (request["Content-Type"] == "application/x-apple-binary-plist")
                //{
                //    Dictionary<string, object> pList = (Dictionary<string, object>)Plist.readPlist(request.Content);
                //    Logger.Debug("Request plist - " + Plist.writeXml(pList));
                //}
                response = getEmptyResponse("404 Not Found");
            }

            #endregion

            else if (request.Uri == "/stop")
            {
                Logger.Debug("Airplay Session: Stop");
                OnStopped(new AirplayEventArgs(SessionId));
                response = getEmptyResponse();
            }

            else
            {
                Logger.Warn("Unhandled Request\r\n{0}", request);
                if (request.ContentLength > 0 && request["Content-Type"] == "application/x-apple-binary-plist")
                {
                    Dictionary<string, object> plist;
                    if (tryGetPlist(request, out plist))
                        Logger.Debug("Request plist - " + Plist.writeXml(plist));
                }
            }
            return response;
        }

        protected override void HandleResponse(HttpResponse response)
        {
            Logger.Debug("Airplay Session: Received response, '{0}'", response.Status);
            base.HandleResponse(response);
        }

        #endregion

        #region Generic Methods

        HttpResponse handleEventRequest()
        {
            Logger.Debug("Airplay Session: Event connection received");
            OnEventConnection(new AirplayEventArgs(SessionId));
            HttpResponse response = new HttpResponse("HTTP/1.1");
            response.Status = "101 Switching Protocols";
            response["Date"] = rfcTimeNow();
            response["Upgrade"] = "PTTH/1.0";
            response["Connection"] = "Upgrade";
            return response;
        }

        #endregion

        #region Photo Methods

        HttpResponse handlePhoto(HttpRequest request)
        {
            Logger.Debug("Airplay: Photo received: Transition '{0}', Action '{1}'", request["X-Apple-Transition"], request["X-Apple-AssetAction"]);
            PhotoReceivedEventArgs e = new PhotoReceivedEventArgs(request["X-Apple-AssetKey"], request["X-Apple-Transition"], request.Content, request["X-Apple-AssetAction"], SessionId);            
            OnPhotoReceived(e);
            if (e.NotInCache)
                return getEmptyResponse("412 Precondition Failed");
            return getEmptyResponse();
        }

        HttpResponse getSlideshowFeatures()
        {
            Logger.Debug("Airplay Session: Slideshow features requested");
            SlideshowFeaturesEventArgs e = new SlideshowFeaturesEventArgs(SessionId);
            OnSlideshowFeaturesRequested(e);
            return getPlistResponse(e.Features);
        }

        HttpResponse handleSlideshowSettings(HttpRequest request)
        {
            Dictionary<string, object> plist;
            if (tryGetPlist(request, out plist))
            {
                string playState = (string)plist["state"];
                Dictionary<string, object> settings = (Dictionary<string, object>)plist["settings"];
                int slideDuration = (int)settings["slideDuration"];
                string theme = (string)settings["theme"];
                Logger.Debug("AirplaySession: Slideshow settings received - state: '{0}', duration: '{1}', theme: '{2}'", playState, slideDuration, theme);
                OnSlideshowSettingsReceived(new SlideshowSettingsEventArgs(playState, slideDuration, theme, SessionId));
            }
            return getPlistResponse(new Dictionary<string, object>());
        }

        #endregion

        #region Video Methods

        HttpResponse handleServerInfo()
        {
            Logger.Debug("Airplay Session: Server info requested");
            return getPlistResponse(serverInfo);
        }

        HttpResponse handleVideoPlay(HttpRequest request)
        {
            Logger.Debug("Airplay: Video Play");
            string contentLocation = null;
            double startPosition = 0;

            if (request["Content-Type"] == "application/x-apple-binary-plist")
            {
                Dictionary<string, object> plist;
                if (tryGetPlist(request, out plist))
                {
                    if (plist.ContainsKey("Content-Location"))
                        contentLocation = (string)plist["Content-Location"];
                    if (plist.ContainsKey("Start-Position"))
                        startPosition = (double)plist["Start-Position"];
                }
            }
            else
            {
                Dictionary<string, string> textParameters = request.GetContentString().AsTextParameters();
                foreach (KeyValuePair<string, string> keyVal in textParameters)
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
                OnVideoReceived(new VideoEventArgs(contentLocation, startPosition, SessionId));
            }
            else
            {
                Logger.Debug("Airplay Session: Failed to determine video url\r\n{0}", request);
            }
            return getEmptyResponse();
        }

        HttpResponse getPlaybackInfo()
        {
            Logger.Debug("Airplay Session: Playback info requested");
            PlaybackInfoEventArgs e = new PlaybackInfoEventArgs(SessionId);
            OnPlaybackInfoRequested(e);
            return getPlistResponse(e.PlaybackInfo);
        }

        HttpResponse setPlaybackRate(HttpRequest request)
        {
            bool parsed = false;
            Dictionary<string, string> queryString = request.Uri.GetQueryStringParameters();
            string rateStr;
            if (queryString.TryGetValue("value", out rateStr))
            {
                double rate;
                if (double.TryParse(rateStr, out rate))
                {
                    Logger.Debug("Airplay Session: Received playback rate - '{0}'", rate);
                    parsed = true;
                    OnPlaybackRateChanged(new PlaybackRateEventArgs(rate, SessionId));
                }
            }
            if (!parsed)
                Logger.Debug("Airplay Session: Failed to determine playback rate\r\n{0}", request);
            return getEmptyResponse();
        }

        HttpResponse setPlaybackPosition(HttpRequest request)
        {
            bool parsed = false;
            Dictionary<string, string> queryString = request.Uri.GetQueryStringParameters();
            string positionStr;
            if (queryString.TryGetValue("position", out positionStr))
            {
                double position;
                if (double.TryParse(positionStr, out position))
                {
                    Logger.Debug("Airplay Session: Received scrub position - '{0}'", position);
                    parsed = true;
                    OnPlaybackPositionChanged(new PlaybackPositionEventArgs(position, SessionId));
                }
            }
            if (!parsed)
                Logger.Debug("Airplay Session: Failed to determine scrub position\r\n{0}", request);
            return getEmptyResponse();
        }

        HttpResponse getPlaybackPosition()
        {
            GetPlaybackPositionEventArgs e = new GetPlaybackPositionEventArgs(SessionId);
            OnGetPlaybackPosition(e);
            Logger.Debug("Airplay Session: Playback position requested: Position {0}, Duration {1}", e.Position, e.Duration);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("duration: " + e.Duration.ToString(CultureInfo.InvariantCulture));
            sb.Append("position: " + e.Position.ToString(CultureInfo.InvariantCulture));

            HttpResponse response = getEmptyResponse("200 OK", false);
            response["Content-Type"] = "text/parameters";
            response.SetContent(sb.ToString());
            return response;
        }

        static HttpResponse getEmptyResponse(string status = "200 OK", bool noContent = true)
        {
            HttpResponse response = new HttpResponse("HTTP/1.1");
            response.Status = status;
            response["Date"] = rfcTimeNow();
            if (noContent)
                response["Content-Length"] = "0";
            return response;
        }

        #endregion

        #region Static Methods

        static bool tryGetPlist(HttpMessage message, out Dictionary<string, object> plist)
        {
            try
            {
                plist = (Dictionary<string, object>)Plist.readPlist(message.Content);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception parsing plist - {0}", ex);
                Logger.Error("Exception message\r\n{0}", message);
                plist = null;
            }
            return false;
        }

        static HttpResponse getPlistResponse(IPlistResponse plist)
        {
            return getPlistResponse(plist.GetPlist());
        }

        static HttpResponse getPlistResponse(Dictionary<string, object> plist)
        {
            HttpResponse response = new HttpResponse("HTTP/1.1");
            response.Status = "200 OK";
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

        #endregion
    }
}
