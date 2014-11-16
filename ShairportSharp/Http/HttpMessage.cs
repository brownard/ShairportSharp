using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ShairportSharp.Helpers;

namespace ShairportSharp.Http
{
    public enum HttpMessageType
    {
        Request,
        Response
    }

    public abstract class HttpMessage
    {
        #region Static Members

        static readonly Encoding encoding = Encoding.ASCII;

        static readonly Regex headerPattern = new Regex(@"^([\w-]+):\W(.+)\r\n", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex requestPattern = new Regex(@"^(\w+)\W(\S+)\W(\S+)/(\S+)\r", RegexOptions.Compiled);
        static readonly Regex responsePattern = new Regex(@"^(\w+)/(\d+[.]\d+)\W(\d+)\W([\w\s]+)\r", RegexOptions.Compiled);
        
        public static HttpMessage FromBytes(byte[] buffer, int offset, int count, out int contentLength)
        {
            string packet = encoding.GetString(buffer, offset, count);
            Dictionary<string, string> headers = getHeaders(packet);
            contentLength = getContentLength(headers);

            Match m = requestPattern.Match(packet);
            if (m.Success)
            {
                string protocol = m.Groups[3].Value + "/" + m.Groups[4].Value;
                return new HttpRequest(m.Groups[1].Value, m.Groups[2].Value, protocol, headers);
            }
            else
            {
                m = responsePattern.Match(packet);
                if (m.Success)
                {
                    string protocol = m.Groups[1].Value + "/" + m.Groups[2].Value;
                    string status = m.Groups[3].Value + " " + m.Groups[4].Value;
                    return new HttpResponse(status, protocol, headers);
                }
                else
                {
                    Logger.Warn("Failed to parse HTTP Message start line");
                    return new HttpResponse("200 OK", "HTTP/1.1", headers);
                }
            }
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

        byte[] content;
        Dictionary<string, string> headers;

        protected HttpMessage(string protocol)
        {
            Protocol = protocol;
            headers = new Dictionary<string, string>();
        }

        protected HttpMessage(string protocol, Dictionary<string, string> headers) 
        {
            Protocol = protocol;
            this.headers = headers;
        }

        public abstract HttpMessageType MessageType { get; }
        public abstract string StartLine { get; }
        public string Protocol { get; set; }
        public Dictionary<string, string> Headers { get { return headers; } }
        
        public string GetHeader(string headerName)
        {
            string value;
            if (headers.TryGetValue(headerName, out value) && value != "")
                return value;
            return null;
        }

        public void SetHeader(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
                headers[key] = value;
        }

        public void RemoveHeader(string header)
        {
            if (!string.IsNullOrEmpty(header))
                headers.Remove(header);
        }

        public string this[string header]
        {
            get { return GetHeader(header); }
            set { SetHeader(header, value); }
        }

        public void SetContent(byte[] content)
        {
            int length;
            if (content == null)
                length = 0;
            else
                length = content.Length;

            SetHeader("Content-Length", length.ToString(CultureInfo.InvariantCulture));
            this.content = content;
        }

        public void SetContent(string content)
        {
            if (!string.IsNullOrEmpty(content))
                SetContent(encoding.GetBytes(content));
        }

        public byte[] Content
        {
            get
            {
                return content;
            }
            internal set
            {
                content = value;
            }
        }

        public int ContentLength
        {
            get
            {
                return content != null ? content.Length : 0;
            }
        }

        public string GetContentString()
        {
            if (content != null && content.Length > 0)
                return encoding.GetString(content);
            return "";
        }

        string getHeaderString()
        {
            StringBuilder sb = new StringBuilder(StartLine + "\r\n");
            foreach (KeyValuePair<string, string> keyVal in headers)
                sb.Append(string.Format("{0}: {1}\r\n", keyVal.Key, keyVal.Value));
            sb.Append("\r\n");
            return sb.ToString();
        }

        public byte[] GetBytes()
        {
            string header = getHeaderString().ToString();
            int byteCount = encoding.GetByteCount(header);
            int contentLength = ContentLength;
            byte[] buffer = new byte[byteCount + contentLength];
            encoding.GetBytes(header, 0, header.Length, buffer, 0);
            if (contentLength > 0)
                Buffer.BlockCopy(content, 0, buffer, byteCount, contentLength);
            return buffer;
        }

        public override string ToString()
        {
            string message = getHeaderString();
            if (content != null && content.Length > 0)
            {
                string contentType;
                if (headers.TryGetValue("Content-Type", out contentType) && contentType.ToLower().StartsWith("text"))
                    message += encoding.GetString(content);
                else
                    message += "BINARY DATA";
            }
            return message;
        }
    }
}
