using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShairportSharp.Helpers
{
    public class HlsParser : IDisposable
    {
        #region Consts

        const string SEPERATOR = "#EXT-X-STREAM-INF:";
        static readonly Regex bandwidthReg = new Regex(@"BANDWIDTH=(\d+)", RegexOptions.IgnoreCase);
        static readonly Regex resolutionReg = new Regex(@"RESOLUTION=(\d+)x(\d+)", RegexOptions.IgnoreCase);
        static readonly Regex urlReg = new Regex(@"http[^\r\n]+");

        #endregion

        #region Variables

        string url;
        VideoInfo videoInfo;
        List<HlsStreamInfo> streamInfos = new List<HlsStreamInfo>();

        #endregion

        #region Public Properties

        public List<HlsStreamInfo> StreamInfos { get { return streamInfos; } }
        public string Url { get { return url; } }
        public bool IsHls { get; protected set; }
        public string ContentType { get; protected set; }
        public bool Success { get; protected set; }

        #endregion

        #region Events

        public event EventHandler Completed;
        protected virtual void OnCompleted()
        {
            if (Completed != null)
                Completed(this, EventArgs.Empty);
        }

        #endregion

        #region Ctor

        public HlsParser(string url)
        {
            this.url = url;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            videoInfo = new VideoInfo(url);
            videoInfo.AnalyseComplete += videoInfo_AnalyseComplete;
            videoInfo.DownloadComplete += videoInfo_DownloadComplete;
            videoInfo.AnalyseFailed += videoInfo_Failed;
            videoInfo.DownloadFailed += videoInfo_Failed;
            videoInfo.BeginAnalyse();
        }

        #endregion

        #region Private Methods

        void populateStreamInfo(string hlsString)
        {
            string[] streamInfoStrings = hlsString.Split(new[] { SEPERATOR }, StringSplitOptions.None);
            if (streamInfoStrings.Length < 2)
            {
                Logger.Debug("HlsParser: No sub-streams found");
                return;
            }

            for (int x = 1; x < streamInfoStrings.Length; x++)
            {
                string streamInfo = streamInfoStrings[x];
                int bandwidth = 0, width = 0, height = 0;
                string url;
                Match m;

                if ((m = urlReg.Match(streamInfo)).Success)
                    url = m.Value;
                else
                    continue;

                if ((m = bandwidthReg.Match(streamInfo)).Success)
                    bandwidth = int.Parse(m.Groups[1].Value);
                if ((m = resolutionReg.Match(streamInfo)).Success)
                {
                    width = int.Parse(m.Groups[1].Value);
                    height = int.Parse(m.Groups[2].Value);
                }

                Logger.Debug("HlsParser: Found stream info: Bandwidth '{0}', Resolution '{1}x{2}', Url '{3}'", bandwidth, width, height, url);
                streamInfos.Add(new HlsStreamInfo(bandwidth, width, height, url));
            }
            streamInfos.Sort((x, y) => x.Bandwidth.CompareTo(y.Bandwidth));
        }

        void videoInfo_AnalyseComplete(object sender, EventArgs e)
        {
            ContentType = videoInfo.ContentType;
            if (string.Compare(ContentType, "application/x-mpegurl", true) == 0 || string.Compare(ContentType, "application/vnd.apple.mpegurl", true) == 0)
            {
                //HLS stream, download the playlist and see if there are multiple qualities available
                IsHls = true;
                Logger.Debug("HlsParser: HLS stream detected, selecting sub-stream");
                videoInfo.BeginDownload();
            }
            else
            {
                Success = true;
                OnCompleted();
            }
        }

        void videoInfo_DownloadComplete(object sender, EventArgs e)
        {
            Success = true;
            populateStreamInfo(videoInfo.Content);
            OnCompleted();
        }

        void videoInfo_Failed(object sender, EventArgs e)
        {
            OnCompleted();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (videoInfo != null)
            {
                videoInfo.Dispose();
                videoInfo = null;
            }
        }

        #endregion
    }

    public class HlsStreamInfo
    {
        public HlsStreamInfo(int bandwidth, int width, int height, string url)
        {
            Bandwidth = bandwidth;
            Width = width;
            Height = height;
            Url = url;
        }

        public int Bandwidth { get; protected set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public string Url { get; protected set; }
    }
}
