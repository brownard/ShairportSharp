using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AirPlayer
{
    class HlsParser
    {
        const string SEPERATOR = "#EXT-X-STREAM-INF:";
        static readonly Regex bandwidthReg = new Regex(@"BANDWIDTH=(\d+)", RegexOptions.IgnoreCase);
        static readonly Regex resolutionReg = new Regex(@"RESOLUTION=(\d+)x(\d+)", RegexOptions.IgnoreCase);
        static readonly Regex urlReg = new Regex(@"http[^\r\n]+");

        public HlsParser(string hlsString)
        {
            if (hlsString.IndexOf(SEPERATOR) < 0)
            {
                Logger.Instance.Debug("HlsParser: No stream info found");
                return;
            }

            string[] streamInfoStrings = hlsString.Split(new[] { SEPERATOR }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string streamInfo in streamInfoStrings)
            {
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

                Logger.Instance.Debug("HlsParser: Found stream info: Bandwidth '{0}', Resolution '{1}x{2}', Url '{3}'", bandwidth, width, height, url);
                streamInfos.Add(new HlsStreamInfo(bandwidth, width, height, url));
            }
            streamInfos.Sort((x, y) => x.Width.CompareTo(y.Width));
        }

        List<HlsStreamInfo> streamInfos = new List<HlsStreamInfo>();
        public List<HlsStreamInfo> StreamInfos { get { return streamInfos; } }
    }

    class HlsStreamInfo
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
