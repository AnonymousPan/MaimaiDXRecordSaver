using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class HttpResponse
    {
        public string HttpVersion { get; set; } = "HTTP/1.1";
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public Dictionary<string, string> Headers { get; private set; }
        public byte[] ContentBytes { get; set; }

        public string ContentType
        {
            get
            {
                if(Headers.TryGetValue("Content-Type", out string ct))
                {
                    return ct;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if(Headers.ContainsKey("Content-Type"))
                {
                    Headers["Content-Type"] = value;
                }
                else
                {
                    Headers.Add("Content-Type", value);
                }
            }
        }

        public HttpResponse()
        {
            Headers = new Dictionary<string, string>();
            Headers.Add("Connection", "close");
        }

        public void UpdateContentLength()
        {
            string clStr = ContentBytes == null ? "0" : ContentBytes.Length.ToString();
            if(Headers.ContainsKey("Content-Length"))
            {
                Headers["Content-Length"] = clStr;
            }
            else
            {
                Headers.Add("Content-Length", clStr);
            }
        }

        public byte[] ToBytes()
        {
            UpdateContentLength();

            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0} {1} {2}\r\n", HttpVersion, StatusCode, StatusMessage));
            foreach(KeyValuePair<string, string> kvPair in Headers)
            {
                string headerLine = string.Format("{0}: {1}\r\n", kvPair.Key, kvPair.Value);
                sb.Append(headerLine);
            }
            sb.Append("\r\n");
            byte[] bytes = Encoding.ASCII.GetBytes(sb.ToString());

            int cl = 0;
            if(Headers.TryGetValue("Content-Length", out string clStr) && int.TryParse(clStr, out int _cl))
            {
                if(_cl > 0 && ContentBytes.Length == _cl)
                {
                    cl = _cl;
                }
            }

            byte[] result = new byte[bytes.Length + cl];
            bytes.CopyTo(result, 0);
            if(cl > 0)
            {
                ContentBytes.CopyTo(result, bytes.Length);
            }
            return result;
        }

        public static HttpResponse CreateDefaultResponse(int statusCode)
        {
            HttpResponse resp = new HttpResponse();
            resp.StatusCode = statusCode;
            resp.StatusMessage = GetDefaultStatusMessage(statusCode);
            resp.Headers.Add("Content-Length", "0");
            DateTime now = DateTime.Now;
            DateTimeOffset dateOffset = new DateTimeOffset(now, TimeZoneInfo.Local.GetUtcOffset(now));
            resp.Headers.Add("Date", dateOffset.ToString("r"));
            return resp;
        }

        public static string GetDefaultStatusMessage(int statusCode)
        {
            switch (statusCode)
            {
                // 1xx
                case 100:
                    return "Continue";

                // 2xx
                case 200:
                    return "OK";
                case 204:
                    return "No Content";
                case 206:
                    return "Partial Content";

                // 3xx
                case 300:
                    return "Multiple Choices";
                case 301:
                    return "Moved Permanently";
                case 302:
                    return "Found";

                // 4xx
                case 400:
                    return "Bad Request";
                case 401:
                    return "Unauthorized";
                case 403:
                    return "Forbidden";
                case 404:
                    return "Not Found";
                case 405:
                    return "Method Not Allowed";
                case 406:
                    return "Not Acceptable";

                // 5xx
                case 500:
                    return "Internal Server Error";
                case 501:
                    return "Not Implemented";
                case 502:
                    return "Bad Gateway";
                case 503:
                    return "Service Unavailable";
                case 504:
                    return "Gateway Time-out";
                case 505:
                    return "HTTP Version not supported";

                default:
                    return null;
            }
        }
    }
}
