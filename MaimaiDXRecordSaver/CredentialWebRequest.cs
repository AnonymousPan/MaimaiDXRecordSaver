using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class CredentialWebRequest
    {
        public bool IsPost { get; set; } = false;
        public string URL { get; private set; }
        public string ContentType { get; private set; }
        public byte[] ContentBytes { get; private set; }
        public CredentialWebResponse Response { get; set; }

        public CredentialWebRequest(string url, string contentType, byte[] contentBytes)
        {
            URL = url;
            ContentType = contentType;
            ContentBytes = contentBytes;
        }

        public CredentialWebRequest(string url) : this(url, null, null) { }

        public HttpWebRequest CreateHttpWebRequest(string userId, string tValue, string uaString)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(URL);
            req.AllowAutoRedirect = false;
            req.Method = IsPost ? "POST" : "GET";
            CookieCollection cookies = new CookieCollection();
            cookies.Add(new Cookie("userId", userId, "/", "maimai.wahlap.com"));
            cookies.Add(new Cookie("_t", tValue, "/", "maimai.wahlap.com"));
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(cookies);
            req.UserAgent = uaString;
            if (ContentBytes != null && ContentBytes.Length != 0)
            {
                req.ContentType = ContentType;
                req.GetRequestStream().Write(ContentBytes, 0, ContentBytes.Length);
            }
            return req;
        }
    }
}
