using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace AirPlayer.Common.Hls
{
    class VideoInfo : IDisposable
    {
        #region Variables

        string videoUrl;
        Downloader analyseDownloader;
        Downloader downloader;
        bool analyseComplete;
        Timer timeoutTimer;

        #endregion

        #region Ctor

        public VideoInfo(string videoUrl, int analyseTimeout = 5000)
        {
            this.videoUrl = videoUrl;
            timeoutTimer = new Timer(analyseTimeout);
            timeoutTimer.AutoReset = false;
            timeoutTimer.Elapsed += timeoutTimer_Elapsed;
        }

        #endregion

        #region Public Properties

        public string UserAgent { get; set; }
        public string ContentType { get; protected set; }
        public string Content { get; protected set; }

        #endregion

        #region Events

        public event EventHandler AnalyseComplete;
        protected virtual void OnAnalyseComplete()
        {
            Logger.Debug("VideoInfo: Analyse completed at {0}", DateTime.Now);
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

        #endregion

        #region Public Methods

        public void BeginAnalyse()
        {
            analyseDownloader = new Downloader(videoUrl, "HEAD") { UserAgent = this.UserAgent };
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
                Logger.Debug("VideoInfo: Content-Type - '{0}'", ContentType);
            };

            Logger.Debug("VideoInfo: Analyse started at {0}", DateTime.Now);
            lock (timeoutTimer)
            {
                analyseComplete = false;
                timeoutTimer.Start();
            }
            analyseDownloader.BeginDownload();
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

        #endregion

        #region Private Methods

        void timeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (shouldRaiseAnalyseEvent())
            {
                Logger.Warn("VideoInfo: Timeout analysing video");
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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            timeoutTimer.Dispose();
        }

        #endregion
    }    
}
