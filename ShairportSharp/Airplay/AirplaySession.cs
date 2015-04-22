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
    public class AirplaySession : HttpParser
    {
        #region Consts

        const string DIGEST_REALM = "AirPlay";
        const string EMPTY_SESSION_ID = "00000000-0000-0000-0000-000000000000";

        #endregion

        #region Variables

        AirplayServerInfo serverInfo;
        string sessionId = EMPTY_SESSION_ID;
        PlaybackState? lastPlaybackState = null;

        #endregion

        #region Ctor

        public AirplaySession(Socket socket, AirplayServerInfo serverInfo, string password = null)
            : base(socket, password, DIGEST_REALM) 
        {
            this.serverInfo = serverInfo;
        }

        #endregion

        #region Public Properties

        public string SessionId
        {
            get { return sessionId; }
        }

        public PlaybackState? LastPlaybackState
        {
            get { return lastPlaybackState; }
        }

        #endregion

        #region Public Methods

        public void SendPlaybackState(PlaybackCategory category, PlaybackState state)
        {
            if (state == lastPlaybackState)
                return;
            lastPlaybackState = state;
            PlaybackStateInfo info = new PlaybackStateInfo()
            {
                Category = category,
                State = state
            };
            HttpRequest request = new HttpRequest("POST", "/event", "HTTP/1.1");
            request["Content-Type"] = "text/x-apple-plist+xml";
            request["X-Apple-Session-ID"] = sessionId;
            string plistXml = PlistCS.Plist.writeXml(info.GetPlist());
            //Logger.Debug("Created plist xml - '{0}'", plistXml);
            request.SetContent(plistXml);
            Logger.Debug("AirplaySession: Sending playback state '{0}' - '{1}'", category, state);
            Send(request, true);
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

        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        protected virtual void OnVolumeChanged(VolumeChangedEventArgs e)
        {
            if (VolumeChanged != null)
                VolumeChanged(this, e);
        }

        #endregion

        #region Overrides

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            if (!IsAuthorised(request))
            {
                HttpResponse authResponse = HttpUtils.GetEmptyResponse("401 Unauthorized");
                authResponse.SetHeader("WWW-Authenticate", string.Format("Digest realm=\"{0}\" nonce=\"{1}\"", DIGEST_REALM, nonce));
                return authResponse;
            }

            string requestSessionId = request["X-Apple-Session-ID"];
            if (!string.IsNullOrEmpty(requestSessionId))
            {
                if (sessionId != EMPTY_SESSION_ID && requestSessionId != SessionId)
                    Logger.Warn("Airplay Session: New SessionId received, old '{0}', new '{1}'", SessionId, requestSessionId);
                sessionId = requestSessionId;
            }

            HttpResponse response;

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

            else if (request.Uri.StartsWith("/volume"))
            {
                response = handleVolumeChange(request);
            }

            else if (request.Uri.StartsWith("/getProperty"))
            {
                //Logger.Debug("Airplay Session: getProperty\r\n{0}", request);
                if (request.Uri.EndsWith("?playbackAccessLog") || request.Uri.EndsWith("?playbackAccessLog"))
                {
                    response = HttpUtils.GetEmptyResponse();
                    response["Content-Type"] = "application/x-apple-binary-plist";
                }
                else
                {
                    response = HttpUtils.GetEmptyResponse("404 Not Found");
                }
            }

            else if (request.Uri.StartsWith("/setProperty"))
            {
                //Logger.Debug("Airplay Session: setProperty\r\n{0}", request);
                //if (request["Content-Type"] == "application/x-apple-binary-plist")
                //{
                //    Dictionary<string, object> pList = (Dictionary<string, object>)Plist.readPlist(request.Content);
                //    Logger.Debug("Request plist - " + Plist.writeXml(pList));
                //}
                response = HttpUtils.GetPlistResponse(new Dictionary<string, object>() { { "errorCode", 0 } }); //getEmptyResponse("404 Not Found");
            }

            #endregion

            else if (request.Uri == "/action")
            {
                Dictionary<string, object> plist;
                object action;
                if (HttpUtils.TryGetPlist(request, out plist) && plist.TryGetValue("type", out action) && action as string == "playlistRemove")
                {
                    OnStopped(new AirplayEventArgs(sessionId));
                }
                else
                {
                    Logger.Warn("Unhandled action request\r\n{0}", request);
                }
                response = HttpUtils.GetPlistResponse(new Dictionary<string, object>(), true);
            }

            else if (request.Uri == "/stop")
            {
                Logger.Debug("Airplay Session: Stop");
                OnStopped(new AirplayEventArgs(SessionId));
                response = HttpUtils.GetEmptyResponse();
            }

            else
            {
                Logger.Warn("Unhandled Request\r\n{0}", request);
                if (request.ContentLength > 0 && request["Content-Type"] == "application/x-apple-binary-plist")
                {
                    Dictionary<string, object> plist;
                    if (HttpUtils.TryGetPlist(request, out plist))
                        Logger.Debug("Request plist - " + Plist.writeXml(plist));
                }
                response = HttpUtils.GetEmptyResponse();
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
            response["Date"] = HttpUtils.RfcTimeNow();
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
                return HttpUtils.GetEmptyResponse("412 Precondition Failed");
            return HttpUtils.GetEmptyResponse();
        }

        HttpResponse getSlideshowFeatures()
        {
            Logger.Debug("Airplay Session: Slideshow features requested");
            SlideshowFeaturesEventArgs e = new SlideshowFeaturesEventArgs(SessionId);
            OnSlideshowFeaturesRequested(e);
            return HttpUtils.GetPlistResponse(e.Features);
        }

        HttpResponse handleSlideshowSettings(HttpRequest request)
        {
            Dictionary<string, object> plist;
            if (HttpUtils.TryGetPlist(request, out plist))
            {
                string playState = (string)plist["state"];
                Dictionary<string, object> settings = (Dictionary<string, object>)plist["settings"];
                int slideDuration = (int)settings["slideDuration"];
                string theme = (string)settings["theme"];
                Logger.Debug("AirplaySession: Slideshow settings received - state: '{0}', duration: '{1}', theme: '{2}'", playState, slideDuration, theme);
                OnSlideshowSettingsReceived(new SlideshowSettingsEventArgs(playState, slideDuration, theme, SessionId));
            }
            return HttpUtils.GetPlistResponse(new Dictionary<string, object>());
        }

        #endregion

        #region Video Methods

        HttpResponse handleServerInfo()
        {
            Logger.Debug("Airplay Session: Server info requested");
            return HttpUtils.GetPlistResponse(serverInfo);
        }

        HttpResponse handleVideoPlay(HttpRequest request)
        {
            Logger.Debug("Airplay: Video Play");
            string contentLocation = null;
            double startPosition = 0;

            if (request["Content-Type"] == "application/x-apple-binary-plist")
            {
                Dictionary<string, object> plist;
                if (HttpUtils.TryGetPlist(request, out plist))
                {
                    if (plist.ContainsKey("Content-Location"))
                        contentLocation = (string)plist["Content-Location"];
                    else if (plist.ContainsKey("host") && plist.ContainsKey("path"))
                        contentLocation = string.Format("http://{0}{1}", plist["host"], plist["path"]);

                    if (plist.ContainsKey("Start-Position"))
                        startPosition = (double)plist["Start-Position"];
                }
            }
            else
            {
                Dictionary<string, string> textParameters = request.GetContentString().AsTextParameters();
                if (textParameters.ContainsKey("Content-Location"))
                    contentLocation = textParameters["Content-Location"];
                if (textParameters.ContainsKey("Start-Position"))
                    startPosition = Convert.ToDouble(textParameters["Start-Position"], CultureInfo.InvariantCulture);
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
            return HttpUtils.GetEmptyResponse();
        }

        HttpResponse getPlaybackInfo()
        {
            Logger.Debug("Airplay Session: Playback info requested");
            PlaybackInfoEventArgs e = new PlaybackInfoEventArgs(SessionId);
            OnPlaybackInfoRequested(e);
            Logger.Debug("Airplay Session: Playback Info\r\n'{0}'", Plist.writeXml(e.PlaybackInfo.GetPlist()));
            return HttpUtils.GetPlistResponse(e.PlaybackInfo);
        }

        HttpResponse setPlaybackRate(HttpRequest request)
        {
            Dictionary<string, string> queryString = request.Uri.GetQueryStringParameters();
            string rateStr;
            double rate;
            if (queryString.TryGetValue("value", out rateStr) && tryParseDouble(rateStr, out rate))
            {
                Logger.Debug("Airplay Session: Received playback rate - '{0}'", rate);
                OnPlaybackRateChanged(new PlaybackRateEventArgs(rate, SessionId));
            }
            else
            {
                Logger.Debug("Airplay Session: Failed to determine playback rate\r\n{0}", request);
            }
            return HttpUtils.GetEmptyResponse();
        }

        HttpResponse setPlaybackPosition(HttpRequest request)
        {
            Dictionary<string, string> queryString = request.Uri.GetQueryStringParameters();
            string positionStr;
            double position;
            if (queryString.TryGetValue("position", out positionStr) && tryParseDouble(positionStr, out position))
            {
                Logger.Debug("Airplay Session: Received scrub position - '{0}'", position);
                OnPlaybackPositionChanged(new PlaybackPositionEventArgs(position, SessionId));
            }
            else
            {
                Logger.Debug("Airplay Session: Failed to determine scrub position\r\n{0}", request);
            }
            return HttpUtils.GetEmptyResponse();
        }

        HttpResponse getPlaybackPosition()
        {
            GetPlaybackPositionEventArgs e = new GetPlaybackPositionEventArgs(SessionId);
            OnGetPlaybackPosition(e);
            Logger.Debug("Airplay Session: Playback position requested: Position {0}, Duration {1}", e.Position, e.Duration);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("duration: " + e.Duration.ToString(CultureInfo.InvariantCulture));
            sb.Append("position: " + e.Position.ToString(CultureInfo.InvariantCulture));

            HttpResponse response = HttpUtils.GetEmptyResponse("200 OK");
            response["Content-Type"] = "text/parameters";
            response.SetContent(sb.ToString());
            return response;
        }

        HttpResponse handleVolumeChange(HttpRequest request)
        {
            Dictionary<string, string> queryString = request.Uri.GetQueryStringParameters();
            string volumeStr;
            double volume;
            if (queryString.TryGetValue("volume", out volumeStr) && tryParseDouble(volumeStr, out volume))
            {
                Logger.Debug("Airplay Session: Received volume - '{0}'", volume);
                OnVolumeChanged(new VolumeChangedEventArgs(volume, SessionId));
            }
            else
            {
                Logger.Debug("Airplay Session: Failed to determine volume\r\n{0}", request);
            }
            return HttpUtils.GetEmptyResponse();
        }

        #endregion

        #region Static Methods

        static bool tryParseDouble(string s, out double result)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        #endregion
    }
}
