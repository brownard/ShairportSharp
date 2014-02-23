using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShairportSharp.Rtsp
{
    class RtspRequest
    {
        #region Static Members
        static readonly byte[] headerDelimiter = { 0x0D, 0x0A, 0x0D, 0x0A }; // \r\n\r\n
        static readonly Regex headerPattern = new Regex("^([\\w-]+):\\W(.+)\r\n", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex requestPattern = new Regex("^(\\w+)\\W(.+)\\WRTSP/(.+)\r\n", RegexOptions.Compiled);       

        /// <summary>
        /// Tries to parse the first complete RTSP packet from binary data. If successful parsedPacket will hold the completed packet.
        /// If the Length property of the parsed packet is less than the length of the binary data there may be more packets to process. 
        /// </summary>
        /// <param name="data">The binary data to process</param>
        /// <param name="parsedPacket">Will hold the parsed packet if successful</param>
        /// <returns>True if a complete packet was successfully parsed</returns>
        public static bool TryParse(byte[] data, out RtspRequest parsedPacket)
        {
            parsedPacket = null;
            int packetLength;
            if (!tryGetPacketLength(data, out packetLength))
                return false; //incomplete header

            string packet = Encoding.ASCII.GetString(data, 0, packetLength);
            Dictionary<string, string> headers = getHeaders(packet);

            int contentLength = getContentLength(headers);
            if (data.Length - packetLength < contentLength)
                return false; //not enough bytes after header

            byte[] content = new byte[contentLength];
            if (contentLength > 0)
            {
                Array.Copy(data, packetLength, content, 0, contentLength);
                packetLength += contentLength;
            }
                       
            string request = "";
            string directory = "";
            string rtspVersion = ""; 
            //Once we know we have a complete packet, parse the request info
            Match m = requestPattern.Match(packet);
            if (m.Success)
            {
                request = m.Groups[1].Value;
                directory = m.Groups[2].Value;
                rtspVersion = m.Groups[3].Value;
            }

            parsedPacket = new RtspRequest(request, directory, rtspVersion, headers, packetLength, content);
            return true;
        }

        static bool tryGetPacketLength(byte[] data, out int packetLength)
        {
            packetLength = data.IndexOf(headerDelimiter);
            if (packetLength > 0)
            {
                packetLength += headerDelimiter.Length; //include the delimiter in the length
                return true;
            }
            return false;
        }

        static Dictionary<string, string> getHeaders(string packet)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            foreach(Match m in headerPattern.Matches(packet))
                headers[m.Groups[1].Value] = m.Groups[2].Value;
            return headers;
        }

        static int getContentLength(Dictionary<string, string> headers)
        {
            string contentLengthStr;
            int contentLength;
            if (!headers.TryGetValue("Content-Length", out contentLengthStr) || !int.TryParse(contentLengthStr, out contentLength))
                contentLength = 0;
            return contentLength;
        }
        #endregion
        
        string request;
        string directory;
        string rtspVersion;
        byte[] content;
        Dictionary<string, string> headers;
        int totalLength; //total length (including headers) in bytes

        private RtspRequest(string request, string directory, string rtspVersion, Dictionary<string, string> headers, int totalLength, byte[] content) 
        {
            this.request = request;
            this.directory = directory;
            this.rtspVersion = rtspVersion;
            this.headers = headers;
            this.content = content;
            this.totalLength = totalLength;
        }
        
        public string GetHeader(string headerName)
        {
            string value;
            if (headers.TryGetValue(headerName, out value) && value != "")
                return value;
            return null;
        }

        public string GetContentString()
        {
            return Encoding.ASCII.GetString(content);
        }

        public int Length
        {
            get
            {
                return totalLength;
            }
        }

        public byte[] Content
        {
            get
            {
                return content;
            }
        }

        public string Request
        {
            get
            {
                return request;
            }
        }

        public string Version
        {
            get
            {
                return rtspVersion;
            }
        }

        public string Directory
        {
            get
            {
                return directory;
            }
        }

        public int Code
        {
            get
            {
                return 200;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("{0} {1} RTSP/{2}", request, directory, rtspVersion));
            sb.AppendLine();
            foreach (string header in headers.Keys)
                sb.AppendLine(string.Format("{0}: {1}", header, headers[header]));
            sb.AppendLine();
            if (content.Length > 0)
            {
                string contentType;
                if (headers.TryGetValue("Content-Type", out contentType) && contentType.ToLower().StartsWith("text"))
                    sb.AppendLine(Encoding.ASCII.GetString(content));
                else
                    sb.AppendLine("BINARY DATA");
            }
            sb.AppendLine(string.Format("Total length: {0}, Content length: {1}", totalLength, content.Length));
            return sb.ToString();
        }
    }
}
