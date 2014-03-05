using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Http
{
    class HttpResponse
    {
        Dictionary<string, string> headers;

        public HttpResponse()
        {
            headers = new Dictionary<string, string>();
        }

        public string Status
        {
            get;
            set;
        }

        public void SetHeader(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
                headers[key] = value;
        }

        public string GetHeader(string header)
        {
            string value;
            if (string.IsNullOrEmpty(header) || !headers.TryGetValue(header, out value))
                value = null;
            return value;
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

        byte[] content;
        public byte[] Content
        {
            get { return content; }
        }

        public void SetContent(byte[] content)
        {
            SetHeader("Content-Length", content.Length.ToString());
            this.content = content;
        }

        public void SetContent(string content)
        {
            SetContent(Encoding.ASCII.GetBytes(content));
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>(Encoding.ASCII.GetBytes(HeaderToString()));
            if (content != null && content.Length > 0)
                bytes.AddRange(content);
            return bytes.ToArray();
        }

        public string HeaderToString()
        {
            StringBuilder sb = new StringBuilder(Status + "\r\n");
            foreach (KeyValuePair<string, string> keyVal in headers)
                sb.Append(string.Format("{0}: {1}\r\n", keyVal.Key, keyVal.Value));
            sb.Append("\r\n");
            return sb.ToString();
        }
    }
}
