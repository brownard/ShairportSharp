using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AirPlayer.Common.Hls
{
    public class HlsParser : IDisposable
    {
        #region Consts

        const string STREAM_INFO_TAG = "#EXT-X-STREAM-INF:";
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

        string userAgent = Constants.APPLE_USER_AGENT;
        public string UserAgent
        {
            get { return userAgent; }
            set { userAgent = value; }
        }

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
            videoInfo = new VideoInfo(url) { UserAgent = userAgent };
            videoInfo.AnalyseComplete += videoInfo_AnalyseComplete;
            videoInfo.DownloadComplete += videoInfo_DownloadComplete;
            videoInfo.AnalyseFailed += videoInfo_AnalyzeFailed;
            videoInfo.DownloadFailed += videoInfo_DownloadFailed;
            videoInfo.BeginAnalyse();
        }

        public string SelectBestSubStream(bool allowHD)
        {
            if (streamInfos.Count > 0)
            {
                //HLS sub-streams, select best quality
                HlsStreamInfo streamInfo;
                if (!allowHD)
                {
                    streamInfo = streamInfos.LastOrDefault(si => si.Height < 720);
                    if (streamInfo == null)
                        streamInfo = streamInfos.First();
                }
                else
                {
                    streamInfo = streamInfos.Last();
                }

                Logger.Debug("HlsParser: Selected hls stream, Bandwidth: '{0}', Size: '{1}x{2}'", streamInfo.Bandwidth, streamInfo.Width, streamInfo.Height);
                return streamInfo.Url;
            }
            return url;
        }

        #endregion

        #region Private Methods

        void populateStreamInfo(string hlsString)
        {
            Uri baseUrl = new Uri(this.url);
            System.IO.StringReader reader = new System.IO.StringReader(hlsString.Trim());
            string line = reader.ReadLine();
            if (!line.StartsWith("#EXTM3U", StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Warn("HlsParser: Not a valid m3u8 file");
                return;
            }

            int bandwidth = 0;
            int width = 0;
            int height = 0;
            Match m;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(STREAM_INFO_TAG))
                {
                    if ((m = bandwidthReg.Match(line)).Success)
                    {
                        bandwidth = int.Parse(m.Groups[1].Value);
                    }
                    if ((m = resolutionReg.Match(line)).Success)
                    {
                        width = int.Parse(m.Groups[1].Value);
                        height = int.Parse(m.Groups[2].Value);
                    }
                }
                else if (line != string.Empty && !line.StartsWith("#"))
                {
                    Uri playlistUrl;
                    if (!Uri.TryCreate(line, UriKind.RelativeOrAbsolute, out playlistUrl) || !playlistUrl.IsAbsoluteUri)
                        playlistUrl = new Uri(baseUrl, line);
                    Logger.Debug("HlsParser: Found stream info: Bandwidth '{0}', Resolution '{1}x{2}', Url '{3}'", bandwidth, width, height, playlistUrl);
                    streamInfos.Add(new HlsStreamInfo(bandwidth, width, height, playlistUrl.ToString()));
                    bandwidth = 0;
                    width = 0;
                    height = 0;
                }
            }

            streamInfos.Sort((x, y) => x.Bandwidth.CompareTo(y.Bandwidth));
        }

        void videoInfo_AnalyseComplete(object sender, EventArgs e)
        {
            ContentType = videoInfo.ContentType;
            if (IsHlsContentType(ContentType))
            {
                //HLS stream, download the playlist and see if there are multiple qualities available
                IsHls = true;
                Logger.Debug("HlsParser: HLS stream detected, checking for sub-streams");
                videoInfo.BeginDownload();
            }
            else
            {
                Success = true;
                OnCompleted();
            }
        }

        void videoInfo_AnalyzeFailed(object sender, EventArgs e)
        {
            Uri uri = new Uri(url);
            if (uri.AbsolutePath.EndsWith(".m3u8", StringComparison.InvariantCultureIgnoreCase))
            {
                //Probably HLS stream, download the playlist and see if there are multiple qualities available
                IsHls = true;
                Logger.Debug("HlsParser: Guessed HLS stream from extension, checking for sub-streams");
                videoInfo.BeginDownload();
            }
            else
            {
                OnCompleted();
            }
        }

        void videoInfo_DownloadComplete(object sender, EventArgs e)
        {
            Success = true;
            populateStreamInfo(videoInfo.Content);
            OnCompleted();
        }

        void videoInfo_DownloadFailed(object sender, EventArgs e)
        {
            OnCompleted();
        }

        #endregion

        #region Public Static Methods

        public static bool IsHlsContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
            return contentType.EndsWith("/x-mpegurl", StringComparison.InvariantCultureIgnoreCase) || contentType.EndsWith("/vnd.apple.mpegurl", StringComparison.InvariantCultureIgnoreCase);
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
