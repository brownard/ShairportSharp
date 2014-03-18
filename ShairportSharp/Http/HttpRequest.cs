using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Http
{
    class HttpRequest : HttpMessage
    {
        public HttpRequest(string method, string uri, string protocol)
            : base(protocol)
        {
            Method = method;
            Uri = uri;
        }

        public HttpRequest(string method, string uri, string protocol, Dictionary<string, string> headers, byte[] content)
            : base(protocol, headers, content)
        {
            Method = method;
            Uri = uri;
        }

        public override HttpMessageType MessageType
        {
            get { return HttpMessageType.Request; }
        }

        public override string StartLine
        {
            get { return string.Format("{0} {1} {2}", Method, Uri, Protocol); }
        }

        public string Method { get; set; }
        public string Uri { get; set; }
    }
}
