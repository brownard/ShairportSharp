using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Http
{
    public class HttpResponse : HttpMessage
    {
        public HttpResponse(string protocol)
            : base(protocol)
        {
            
        }

        public HttpResponse(string status, string protocol, Dictionary<string, string> headers)
            : base(protocol, headers)
        {
            Status = status;
        }

        public override HttpMessageType MessageType
        {
            get { return HttpMessageType.Response; }
        }

        public override string StartLine
        {
            get { return string.Format("{0} {1}", Protocol, Status); }
        }

        public string Status { get; set; }
    }
}
