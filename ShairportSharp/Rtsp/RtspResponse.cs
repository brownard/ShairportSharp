using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Rtsp
{
    class RtspResponse
    {
        Dictionary<string, string> headers;

        public RtspResponse()
        {
            Status = "RTSP/1.0 200 OK";
            headers = new Dictionary<string, string>();
        }

        public RtspResponse(string status)
        {
            Status = status;
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Status + "\r\n");
            foreach(KeyValuePair<string, string> keyVal in headers)
                sb.Append(string.Format("{0}: {1}\r\n", keyVal.Key, keyVal.Value));
            sb.Append("\r\n");
            return sb.ToString();
        }
    }
}
