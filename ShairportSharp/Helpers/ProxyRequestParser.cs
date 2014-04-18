using ShairportSharp.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace ShairportSharp.Helpers
{
    class ProxyRequestParser : HttpParser
    {
        HlsProxy handler;

        public ProxyRequestParser(Socket socket, HlsProxy handler)
            : base(socket)
        {
            this.handler = handler;
        }

        public string UserAgent { get; set; }

        protected override HttpResponse HandleRequest(HttpRequest request)
        {
            string url = handler.GetOriginalUrl(request.Uri);
            Logger.Debug("ProxyRequestParser: Request '{0}'", url);
            if (url != null)
                proxyRequest(url, request.Headers);
            Close();
            return null;
        }

        void proxyRequest(string url, Dictionary<string, string> headers)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                if (!string.IsNullOrEmpty(UserAgent))
                    request.UserAgent = UserAgent;

                foreach (KeyValuePair<string, string> header in headers)
                {
                    if (!WebHeaderCollection.IsRestricted(header.Key))
                        request.Headers[header.Key] = header.Value;
                }

                long contentLength;
                using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = getResponseStream(response, out contentLength))
                {
                    HttpResponse proxyResponse = new HttpResponse("HTTP/" + response.ProtocolVersion.ToString());
                    proxyResponse.Status = (int)response.StatusCode + " " + response.StatusDescription;
                    proxyResponse["Content-Length"] = contentLength.ToString();
                    foreach (string header in response.Headers.Keys)
                    {
                        if (header != "Content-Length")
                            proxyResponse[header] = response.Headers[header];
                    }
                    copyToOutputStream(proxyResponse, responseStream);
                    Logger.Debug("ProxyRequestParser: Completed");
                }
            }
            catch(Exception ex) 
            {
                Logger.Error("ProxyRequestParser: Error proxying stream '{0}' - {1}\r\n{2}", url, ex.Message, ex.StackTrace);
            }
        }

        void copyToOutputStream(HttpResponse response, Stream source)
        {
            byte[] buffer = response.GetBytes();
            outputStream.Write(buffer, 0, buffer.Length);
            buffer = new byte[16384];
            int read;
            try
            {
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                    outputStream.Write(buffer, 0, read);
            }
            catch (IOException)
            {
                //Client/Server has probably disconnected
                Logger.Debug("ProxyRequestParser: IO Exception when copying streams");
            }
        }

        Stream getResponseStream(HttpWebResponse response, out long length)
        {
            Logger.Debug("ProxyRequestParser: Got response, ContentType '{0}'", response.ContentType);
            Stream responseStream = response.GetResponseStream();
            if (!HlsParser.IsHlsContentType(response.ContentType))
            {
                length = response.ContentLength;
                return responseStream;
            }

            using (responseStream)
            {
                Logger.Debug("ProxyRequestParser: Creating proxy playlist}");
                Stream playlistStream = replaceURLs(responseStream, response.ResponseUri);
                length = playlistStream.Length;
                return playlistStream;
            }
        }

        Stream replaceURLs(Stream playlistStream, Uri originalUrl)
        {
            StringBuilder playlist = new StringBuilder();
            using (StreamReader reader = new StreamReader(playlistStream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line != string.Empty && !line.StartsWith("#"))
                    {
                        Uri playlistUrl;
                        if (!Uri.TryCreate(line, UriKind.RelativeOrAbsolute, out playlistUrl) || !playlistUrl.IsAbsoluteUri)
                            playlistUrl = new Uri(originalUrl, line);
                        line = handler.GetProxyUrl(playlistUrl.ToString());
                    }
                    playlist.Append(line + "\n");
                }
            }
            Logger.Debug("ProxyRequestParser: Created proxy playlist\r\n{0}", playlist.ToString());
            return new MemoryStream(Encoding.UTF8.GetBytes(playlist.ToString()));
        }
    }
}
