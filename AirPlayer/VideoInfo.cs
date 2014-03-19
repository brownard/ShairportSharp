using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;

namespace AirPlayer
{
    class VideoInfo : IDisposable
    {
        string videoUrl;
        Downloader analyseDownloader;
        Downloader downloader;
        bool analyseComplete;
        Timer timeoutTimer;

        public VideoInfo(string videoUrl, int analyseTimeout = 5000)
        {
            this.videoUrl = videoUrl;
            timeoutTimer = new Timer(analyseTimeout);
            timeoutTimer.AutoReset = false;
            timeoutTimer.Elapsed += timeoutTimer_Elapsed;
        }

        public string ContentType { get; protected set; }
        public string Content { get; protected set; }

        public event EventHandler AnalyseComplete;
        protected virtual void OnAnalyseComplete()
        {
            Logger.Instance.Debug("VideoInfo: Analyse completed at {0}", DateTime.Now);
            if (AnalyseComplete != null)
                AnalyseComplete(this, EventArgs.Empty);
        }

        public event EventHandler AnalyseFailed;
        protected virtual void OnAnalyseFailed()
        {
            if (AnalyseFailed != null)
                AnalyseFailed(this, EventArgs.Empty);
        }

        public event EventHandler DownloadComplete;
        protected virtual void OnDownloadComplete()
        {
            if (DownloadComplete != null)
                DownloadComplete(this, EventArgs.Empty);
        }

        public event EventHandler DownloadFailed;
        protected virtual void OnDownloadFailed()
        {
            if (DownloadFailed != null)
                DownloadFailed(this, EventArgs.Empty);
        }

        public void BeginAnalyse()
        {
            analyseDownloader = new Downloader(videoUrl, "HEAD");
            analyseDownloader.Complete += (s, e) => 
            {
                if (shouldRaiseAnalyseEvent())
                    OnAnalyseComplete();
            };
            analyseDownloader.Failed += (s, e) => 
            {
                if (shouldRaiseAnalyseEvent())
                    OnAnalyseFailed();
            };
            analyseDownloader.ResponseReady += (s, e) => 
            { 
                ContentType = e.Response.ContentType;
                Logger.Instance.Debug("VideoInfo: Content-Type - '{0}'", ContentType);
            };

            Logger.Instance.Debug("VideoInfo: Analyse started at {0}", DateTime.Now);
            analyseDownloader.BeginDownload();
            lock (timeoutTimer)
            {
                analyseComplete = false;
                timeoutTimer.Start();
            }
        }

        void timeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (shouldRaiseAnalyseEvent())
            {
                Logger.Instance.Warn("VideoInfo: Timeout analysing video");
                OnAnalyseFailed();
            }
        }

        bool shouldRaiseAnalyseEvent()
        {
            bool raise;
            lock (timeoutTimer)
            {
                if (!analyseComplete)
                {
                    analyseComplete = true;
                    raise = true;
                    timeoutTimer.Dispose();
                }
                else
                {
                    raise = false;
                }
            }
            return raise;
        }

        public void BeginDownload()
        {
            downloader = new Downloader(videoUrl, null);
            downloader.Complete += (s, e) => OnDownloadComplete();
            downloader.Failed += (s, e) => OnDownloadFailed();
            downloader.ResponseReady += (s, e) => 
            {
                using (StreamReader sr = new StreamReader(e.Response.GetResponseStream()))
                {
                    Content = sr.ReadToEnd();
                }
            };
            downloader.BeginDownload();
        }

        public void Dispose()
        {
            timeoutTimer.Dispose();
        }
    }

    class Downloader
    {
        public class ResponseReadyEventArgs : EventArgs
        {
            public ResponseReadyEventArgs(HttpWebResponse response)
            {
                Response = response;
            }

            public HttpWebResponse Response { get; protected set; }
        }

        string url;
        string method;

        public Downloader(string url, string method)
        {
            this.url = url;
            this.method = method;
        }

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

        public void BeginDownload()
        {
            bool failed = false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                if (!string.IsNullOrEmpty(method))
                    request.Method = method;

                request.BeginGetResponse(ar =>
                {
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar))
                        {
                            OnResponseReady(new ResponseReadyEventArgs(response));
                        }
                    }
                    catch (Exception ex)
                    {
                        failed = true;
                        Logger.Instance.Warn("VideoInfo: Could not get response - {0}", ex.Message);
                    }

                    if (failed)
                        OnFailed();
                    else
                        OnComplete();
                }
                , null);
            }
            catch (Exception ex)
            {
                failed = true;
                Logger.Instance.Warn("VideoInfo: Could not create request - {0}", ex.Message);
            }

            if (failed)
                OnFailed();
        }
    }
}
