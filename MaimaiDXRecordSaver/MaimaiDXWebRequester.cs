using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using log4net;

namespace MaimaiDXRecordSaver
{
    public class MaimaiDXWebRequester
    {
        public string UserID { get; set; }
        public string TValue { get; set; }
        public string UAString { get; set; }
        public bool IsBusy { get; private set; }

        private ILog logger = LogManager.GetLogger("WebRequester");

        public MaimaiDXWebRequester(string userID, string _t)
        {
            UserID = userID;
            TValue = _t;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        public string Request(string url)
        {
            return Request(url, null, null);
        }

        public string Request(string url, byte[] payload, string contentType)
        {
            logger.Debug("Requesting url " + url);
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.AllowAutoRedirect = false;
            req.Method = "GET";
            CookieCollection cookies = new CookieCollection();
            cookies.Add(new Cookie("userId", UserID, "/", "maimai.wahlap.com"));
            cookies.Add(new Cookie("_t", TValue, "/", "maimai.wahlap.com"));
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(cookies);
            req.UserAgent = UAString;
            if(payload != null && payload.Length != 0)
            {
                req.ContentType = contentType;
                req.GetRequestStream().Write(payload, 0, payload.Length);
            }
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            if (resp.StatusCode == HttpStatusCode.Found
                && resp.Headers[HttpResponseHeader.Location].Contains("error"))
            {
                throw new CredentialInvalidException(url, UserID, TValue);
            }
            
            Cookie userIdCookie = resp.Cookies["userId"];
            bool userIdChanged = userIdCookie != null;
            if(userIdChanged)
            {
                UserID = resp.Cookies["userId"].Value;
            }
            
            TValue = resp.Cookies["_t"].Value;
            string str;
            using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
            {
                str = reader.ReadToEnd();
            }

            if(userIdChanged)
            {
                logger.Debug("Request completed. New userId: " + UserID);
            }
            else
            {
                logger.Debug("Request completed. userId is not changed.");
            }

            return str;
        }

        // TODO: Async Requesting
    }
}
