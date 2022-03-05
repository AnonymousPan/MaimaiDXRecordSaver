using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Net;
using log4net;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class WebPageProxy
    {
        private HttpListener listener;
        private Thread reqProcThread;
        private bool running = false;
        private ILog logger = LogManager.GetLogger("WebPageProxy");
        private string ipBind = "";
        private int port = 0;

        private string sessionID = "";
        private string _t = "";

        public delegate void OnCredentialChangeEventHandler(string sessionID, string _t);
        public event OnCredentialChangeEventHandler OnCredentialChange;

        public WebPageProxy(string ip, int _port)
        {
            ipBind = ip;
            port = _port;
            listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://+:{0}/", port));
            reqProcThread = new Thread(ProcessRequest);
            reqProcThread.IsBackground = true;
        }

        public void Start()
        {
            running = true;
            listener.Start();
            reqProcThread.Start();
            logger.Info(string.Format("Web page proxy started at {0}:{1}", ipBind, port));
        }

        public void Stop()
        {
            try
            {
                running = false;
                listener.Stop();
                reqProcThread.Abort();
                logger.Info("Web page proxy stopped.");
            }
            catch(Exception err)
            {
                logger.Warn("Can not stop Web page proxy.");
                logger.Warn(err.ToString());
            }
        }

        public void UpdateCredential(string s, string t)
        {
            sessionID = s;
            _t = t;
        }

        private void ProcessRequest()
        {
            logger.Info("Request processing thread started.");
            while(running)
            {
                string url = "N/A";
                string ip = "N/A";
                try
                {
                    // Wait for connection
                    HttpListenerContext context = listener.GetContext();
                    
                    HttpListenerRequest req = context.Request;
                    url = req.RawUrl;
                    ip = req.RemoteEndPoint.ToString();
                    HttpListenerResponse resp = context.Response;
                    if (req.RawUrl == "/")
                    {
                        logger.Info("WebPageProxy: Connecting from " + req.RemoteEndPoint.ToString());
                        using (StreamWriter writter = new StreamWriter(resp.OutputStream))
                        {
                            string str = htmlText.Replace("HOST", ipBind + ":" + port.ToString());
                            //str = str.Replace("SESSIONID", sessionID);
                            //str = str.Replace("TVALUE", _t);
                            writter.Write(str);
                            resp.StatusCode = 200;
                        }
                    }
                    else if (req.RawUrl.Contains("/maimai-mobile/home/userOption/logout"))
                    {
                        logger.Info("WebPageProxy: Logout clicked, from " + req.RemoteEndPoint.ToString());
                        using (StreamWriter writter = new StreamWriter(resp.OutputStream))
                        {
                            string str = "You clicked logout. I'm sure that was a mistake!";
                            writter.Write(str);
                            resp.StatusCode = 200;
                        }
                    }
                    else if (req.RawUrl.Contains("maimai-mobile/"))
                    {
                        DoProxy(req, resp);
                    }
                    else
                    {
                        logger.Info(string.Format("WebPageProxy: Invalid URL {0} from {1}", req.RawUrl, req.RemoteEndPoint.ToString()));
                        using (StreamWriter writter = new StreamWriter(resp.OutputStream))
                        {
                            string str = "Invalid URL: " + req.RawUrl;
                            writter.Write(str);
                            resp.StatusCode = 400;
                        }
                    }

                    // Send response
                    resp.Close();
                }
                catch(Exception err)
                {
                    logger.Warn(string.Format("Error while processing request! URL = {0} from {1}", url, ip));
                    logger.Warn(err.ToString());
                }
            }
            logger.Info("Request processing thread terminated.");
        }

        private void DoProxy(HttpListenerRequest mRequest, HttpListenerResponse mResponse)
        {
            string url = "https://maimai.wahlap.com" + mRequest.RawUrl;
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.AllowAutoRedirect = false;
            req.Method = mRequest.HttpMethod;
            req.ContentType = mRequest.ContentType;
            CookieCollection cookies = new CookieCollection();
            cookies.Add(new Cookie("userId", sessionID, "/", "maimai.wahlap.com"));
            cookies.Add(new Cookie("_t", _t, "/", "maimai.wahlap.com"));
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(cookies);
            if(mRequest.ContentLength64 > 0)
            {
                req.ContentLength = mRequest.ContentLength64;
                mRequest.InputStream.CopyTo(req.GetRequestStream());
            }
            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch(WebException err)
            {
                resp = (HttpWebResponse)err.Response;
            }
            mResponse.StatusCode = (int)resp.StatusCode;
            if(resp.StatusCode == HttpStatusCode.MovedPermanently || resp.StatusCode == HttpStatusCode.Found)
            {
                string str = resp.Headers["Location"];
                str = str.Replace("https://maimai.wahlap.com", string.Format("http://{0}:{1}", ipBind, port.ToString()));
                mResponse.RedirectLocation = str;
            }
            mResponse.ContentType = resp.ContentType;
            // mResponse.ContentLength64 = resp.ContentLength;
            if(resp.Cookies["userId"] != null)
            {
                sessionID = resp.Cookies["userId"].Value;
                _t = resp.Cookies["_t"].Value;
                OnCredentialChange(sessionID, _t);
            }
            
            // Rewrite url
            if(resp.ContentType.Contains("text/html"))
            {
                logger.Info(string.Format("WebPageProxy: Requesting url {0} from {1}", url, mRequest.RemoteEndPoint.ToString()));
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    string str = reader.ReadToEnd();
                    str = str.Replace("https://maimai.wahlap.com", string.Format("http://{0}:{1}", ipBind, port.ToString()));
                    byte[] bytes = Encoding.UTF8.GetBytes(str);
                    mResponse.OutputStream.Write(bytes, 0, bytes.Length);
                }
            }
            else
            {
                resp.GetResponseStream().CopyTo(mResponse.OutputStream);
            }
        }

        private static string htmlText =
            "<!DOCTYPE html><html><head><title>MaimaiDXRecordSaver - WebPageProxy</title>" +
            "<body><b>Congratulations!</b>" +
            "<p>If you see this message, it means the WebPageProxy is working.</p>" +
            "<a href=\"http://HOST/maimai-mobile/home/\">Click here to view your game data.</a>" +
            "<p>userId = SESSIONID</p>" +
            "<p>_t = TVALUE</p>" +
            "</body></html>";
    }
}
