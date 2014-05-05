using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace AirPlayer.Common.Hls
{
    #region ResponseReadyEventArgs

    public class ResponseReadyEventArgs : EventArgs
    {
        public ResponseReadyEventArgs(HttpWebResponse response)
        {
            Response = response;
        }

        public HttpWebResponse Response { get; protected set; }
    }

    #endregion

    class Downloader
    {
        #region Variables

        string url;
        string method;

        #endregion

        #region Ctor

        public Downloader(string url, string method)
        {
            this.url = url;
            this.method = method;
        }

        #endregion

        #region Events

        public event EventHandler Complete;
        protected virtual void OnComplete()
        {
            if (Complete != null)
                Complete(this, EventArgs.Empty);
        }

        public event EventHandler Failed;
        protected virtual void OnFailed()
        {
            if (Failed != null)
                Failed(this, EventArgs.Empty);
        }

        public event EventHandler<ResponseReadyEventArgs> ResponseReady;
        protected virtual void OnResponseReady(ResponseReadyEventArgs e)
        {
            if (ResponseReady != null)
                ResponseReady(this, e);
        }

        #endregion

        #region Properties

        public string UserAgent { get; set; }

        #endregion

        #region Public Methods

        public void BeginDownload()
        {
            bool failed = false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                if (!string.IsNullOrEmpty(method))
                    request.Method = method;
                if (!string.IsNullOrEmpty(UserAgent))
                    request.UserAgent = UserAgent;

                request.BeginGetResponse(ar =>
                {
                    bool responseFailed = false;
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar))
                        {
                            OnResponseReady(new ResponseReadyEventArgs(response));
                        }
                    }
                    catch (Exception ex)
                    {
                        responseFailed = true;
                        Logger.Warn("VideoInfo: Could not get response - {0}", ex.Message);
                    }

                    if (responseFailed)
                        OnFailed();
                    else
                        OnComplete();
                }
                , null);
            }
            catch (Exception ex)
            {
                failed = true;
                Logger.Warn("VideoInfo: Could not create request - {0}", ex.Message);
            }

            if (failed)
                OnFailed();
        }

        #endregion
    }
}
