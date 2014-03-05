using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShairportSharp.Http
{
    class HttpRequest
    {
        #region Static Members
        static readonly byte[] headerDelimiter = { 0x0D, 0x0A, 0x0D, 0x0A }; // \r\n\r\n
        static readonly Regex headerPattern = new Regex(@"^([\w-]+):\W(.+)\r\n", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex requestPattern = new Regex(@"^(\w+)\W(\S+)\W(\S+)/(\S+)\r", RegexOptions.Compiled);       

        /// <summary>
        /// Tries to parse the first complete HTTP packet from binary data. If successful parsedPacket will hold the completed packet.
        /// If the Length property of the parsed packet is less than the length of the binary data there may be more packets to process. 
        /// </summary>
        /// <param name="data">The binary data to process</param>
        /// <param name="parsedPacket">Will hold the parsed packet if successful</param>
        /// <returns>True if a complete packet was successfully parsed</returns>
        public static bool TryParse(byte[] data, out HttpRequest parsedPacket)
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
                       
            string method = "";
            string directory = "";
            string protocol = "";
            string version = ""; 
            //Once we know we have a complete packet, parse the request info
            //Logger.Debug(packet);
            Match m = requestPattern.Match(packet);
            if (m.Success)
            {
                method = m.Groups[1].Value;
                directory = m.Groups[2].Value;
                protocol = m.Groups[3].Value;
                version = m.Groups[4].Value;
            }

            parsedPacket = new HttpRequest(method, directory, protocol, version, headers, packetLength, content);
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
        
        string method;
        string directory;
        string protocol;
        string version;
        byte[] content;
        Dictionary<string, string> headers;
        int totalLength; //total length (including headers) in bytes

        private HttpRequest(string method, string directory, string protocol, string version, Dictionary<string, string> headers, int totalLength, byte[] content) 
        {
            this.method = method;
            this.directory = directory;
            this.protocol = protocol;
            this.version = version;
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

        public string this[string header]
        {
            get { return GetHeader(header); }
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

        public string Method
        {
            get
            {
                return method;
            }
        }

        public string Protocol
        {
            get
            {
                return protocol;
            }
        }

        public string Version
        {
            get
            {
                return version;
            }
        }

        public string Directory
        {
            get
            {
                return directory;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("{0} {1} {2}/{3}", method, directory, protocol, version));
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
