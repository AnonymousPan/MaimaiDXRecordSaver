using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using log4net;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace MaimaiDXRecordSaver
{
    public class WebPageProxy
    {
        private Thread webPageProxyThread;
        private bool running = false;
        private ILog logger = LogManager.GetLogger("WebPageProxy");
        private string ipBind = "";
        private int port = 0;

        private HttpClient httpClient;

        private MyHttpServer httpServer;

        private bool serverHeaderEnabled = false;
        private string serverHeaderString;

        private Dictionary<string, byte[]> webResources;
        private Dictionary<string, string> webResourcesMime;

        public WebPageProxy(string ip, int _port, string serverString)
        {
            ipBind = ip;
            port = _port;
            webPageProxyThread = new Thread(WebPageProxyThreadProc);
            webPageProxyThread.IsBackground = true;

            httpClient = new HttpClient();

            httpServer = new MyHttpServer(new IPAddress(0L), port);

            if(!string.IsNullOrEmpty(serverString))
            {
                serverHeaderEnabled = true;
                serverHeaderString = serverString.Replace("{VERSION}", Program.Version);
            }

            webResources = new Dictionary<string, byte[]>();
            webResourcesMime = new Dictionary<string, string>();
        }

        public void Start()
        {
            running = true;
            webPageProxyThread.Start();
        }

        public void Stop()
        {
            try
            {
                running = false;
                webPageProxyThread.Abort();
                logger.Info("Web page proxy stopped.");
            }
            catch(Exception err)
            {
                logger.Warn("Can not stop Web page proxy.");
                logger.Warn(err.ToString());
            }
        }

        private void WebPageProxyThreadProc()
        {
            LoadResources();
            logger.Info(string.Format("Web page proxy started at {0}:{1}", ipBind, port));
            httpServer.Start();
            while(running)
            {
                string reqUrl = "N/A";
                try
                {
                    httpServer.WaitForRequest();
                    
                    if (httpServer.CurrentRequestValid)
                    {
                        string url = httpServer.CurrentRequest.URL;
                        reqUrl = url;
                        string lowerUrl = url.ToLower();

                        if(!ConfigManager.Instance.WebPageProxyAllowLogout.Value
                            && lowerUrl.Contains("logout")
                            && lowerUrl.Contains("maimai-mobile"))
                        {
                            // Logout not allowed
                            ProcessReq_TempRedir("/res/no_logout.html");
                        }
                        else if(!ConfigManager.Instance.WebPageProxyAllowChangeName.Value
                            && lowerUrl.Contains("maimai-mobile/home/useroption/updateusername/update"))
                        {
                            // Change name not allowed
                            ProcessReq_TempRedir("/res/no_changename.html");
                        }
                        else if(url.StartsWith("/maimai-mobile"))
                        {
                            // MaimaiDX pages
                            if(IsCredentialNeeded(url))
                            {
                                ProcessReq_MaimaiDXPage_Credential(url);
                            }
                            else
                            {
                                ProcessReq_MaimaiDXPage_NoCredential(url);
                            }
                        }
                        else if(url.StartsWith("/res/"))
                        {
                            // Local resources
                            ProcessReq_LocalResources(url);
                        }
                        else if(url == "/")
                        {
                            // Index
                            ProcessReq_Index();
                        }
                        else
                        {
                            // Not found
                            ProcessReq_NotFound();
                        }
                    }
                    else
                    {
                        // Bad request
                        ProcessReq_BadRequest();
                    }
                }
                catch(Exception err)
                {
                    string reqIp = httpServer.CurrentRequestEndPoint == null ? "N/A"
                        : httpServer.CurrentRequestEndPoint.ToString();
                    logger.WarnFormat("Failed to process request [IP={0} URL={1}]\n{2}", reqIp, reqUrl, err.ToString());
                }
            }
            logger.Info("Web page proxy thread exited");
        }

        private void AddServerHeader(HttpResponse resp)
        {
            if(serverHeaderEnabled)
            {
                resp.Headers.Add("Server", serverHeaderString);
            }
        }

        private bool IsCredentialNeeded(string url)
        {
            int index = url.IndexOf("/maimai-mobile/");
            if(index > -1)
            {
                index += 15; // Length of "/maimai-mobile/"
                string str = url.Substring(index);
                if (str.StartsWith("img/Icon/")) return true;
                if (str.StartsWith("img/photo/")) return true;
                if (str.StartsWith("apple-touch-icon.png")) return false;
                return !str.StartsWith("img/") && !str.StartsWith("js/") && !str.StartsWith("css/");
            }
            else
            {
                return false;
            }
        }

        private void LoadResources()
        {
            logger.Info("Loading web resources");
            if(Directory.Exists("WebResources"))
            {
                string rootPath = Path.GetFullPath("WebResources") + "\\";
                LoadResDirectory(rootPath, rootPath);
                // Generate MIME type strings
                foreach(string relPath in webResources.Keys)
                {
                    string mime = MIMEHelper.Instance.GetMIMEType(relPath);
                    if(string.IsNullOrEmpty(mime))
                    {
                        logger.WarnFormat("MIME type of file {0} is unknown", relPath);
                        webResourcesMime.Add(relPath, null);
                    }
                    else
                    {
                        webResourcesMime.Add(relPath, mime);
                    }
                }
                ProcessHtml();
                logger.InfoFormat("Loaded {0} web resources", webResources.Count);
            }
            else
            {
                logger.Warn("WebResources directory not found");
            }
        }

        private void LoadResFile(string rootPath, string fullPath)
        {
            string relativePath = fullPath.Substring(rootPath.Length);
            webResources.Add(relativePath.Replace('\\', '/'), File.ReadAllBytes(fullPath));
        }

        private void LoadResDirectory(string rootPath, string fullPath)
        {
            string[] files = Directory.GetFiles(fullPath);
            foreach(string file in files)
            {
                LoadResFile(rootPath, file);
            }
            string[] directories = Directory.GetDirectories(fullPath);
            foreach(string dir in directories)
            {
                LoadResDirectory(rootPath, dir);
            }
        }

        private void ProcessHtml()
        {
            foreach(KeyValuePair<string, string> kvPair in webResourcesMime)
            {
                if(kvPair.Value == "text/html")
                {
                    byte[] bytes = webResources[kvPair.Key];
                    string content = Encoding.UTF8.GetString(bytes);
                    content = content.Replace("{VERSION}", Program.Version);
                    webResources[kvPair.Key] = Encoding.UTF8.GetBytes(content);
                }
            }
        }

        private string RewriteURL(string content)
        {
            string replacement = string.Format("http://{0}:{1}", ipBind, port);
            string[] arr = content.Split(new string[] { "https://maimai.wahlap.com" }
                , StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            sb.Append(arr[0]);
            for(int i = 1; i < arr.Length; i++ )
            {
                string str = arr[i].Length > 64 ? arr[i].Substring(0, 64) : arr[i];
                if(IsCredentialNeeded(str))
                {
                    sb.Append(replacement);
                }
                else
                {
                    sb.Append("https://maimai.wahlap.com");
                }
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }

        private void ProcessReq_MaimaiDXPage_Credential(string url)
        {
            CredentialWebRequest req;
            byte[] cb = httpServer.CurrentRequest.ContentBytes;
            if (cb != null && cb.Length > 0)
            {
                string ct;
                if (!httpServer.CurrentRequest.Headers.TryGetValue("Content-Type", out ct))
                {
                    ct = "";
                }
                req = new CredentialWebRequest("https://maimai.wahlap.com" + url, ct, cb);
            }
            else
            {
                req = new CredentialWebRequest("https://maimai.wahlap.com" + url);
            }

            req.IsPost = httpServer.CurrentRequest.Method == "POST";

            CredentialWebResponse resp = Program.Requester.Request(req);
            if (resp.Failed)
            {
                logger.InfoFormat("Requesting URL {0} (failed) from {1}", url, httpServer.CurrentRequestEndPoint.ToString());
                HttpResponse httpResp = HttpResponse.CreateDefaultResponse(500);
                AddServerHeader(httpResp);
                httpResp.ContentBytes = Encoding.UTF8.GetBytes(
                    "Failed to request, please check logs for more information.");
                httpServer.SendResponse(httpResp);
            }
            else
            {
                HttpResponse httpResp = HttpResponse.CreateDefaultResponse(resp.StatusCode);
                AddServerHeader(httpResp);
                if (!string.IsNullOrEmpty(resp.Location))
                {
                    httpResp.Headers.Add("Location", RewriteURL(resp.Location));
                }
                httpResp.ContentType = resp.ContentType;
                if (resp.ContentType.StartsWith("text/html"))
                {
                    logger.InfoFormat("Requesting URL {0} from {1}", url, httpServer.CurrentRequestEndPoint.ToString());
                    string content = Encoding.UTF8.GetString(resp.ContentBytes);
                    content = RewriteURL(content);
                    httpResp.ContentBytes = Encoding.UTF8.GetBytes(content);
                }
                else
                {
                    httpResp.ContentBytes = resp.ContentBytes;
                }
                httpServer.SendResponse(httpResp);
            }
        }

        private void ProcessReq_MaimaiDXPage_NoCredential(string url)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("https://maimai.wahlap.com" + url);
            req.Method = httpServer.CurrentRequest.Method;
            byte[] cb = httpServer.CurrentRequest.ContentBytes;
            if (cb != null && cb.Length > 0)
            {
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(cb, 0, cb.Length);
                }
            }
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            HttpResponse httpResp = HttpResponse.CreateDefaultResponse(200);
            httpResp.ContentType = resp.ContentType;
            if (resp.ContentType.StartsWith("text/html"))
            {
                logger.InfoFormat("Requesting URL {0} (no credential) from {1}", url, httpServer.CurrentRequestEndPoint.ToString());
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    string content = reader.ReadToEnd();
                    content = RewriteURL(content);
                    httpResp.ContentBytes = Encoding.UTF8.GetBytes(content);
                }
            }
            else
            {
                using (Stream respStream = resp.GetResponseStream())
                {
                    using (MemoryStream memStream = new MemoryStream())
                    {
                        byte[] buf = new byte[4096];
                        int readCount;
                        while ((readCount = respStream.Read(buf, 0, 4096)) > 0)
                        {
                            memStream.Write(buf, 0, readCount);
                        }
                        httpResp.ContentBytes = memStream.ToArray();
                    }
                }
            }
            AddServerHeader(httpResp);
            httpServer.SendResponse(httpResp);
            resp.Dispose();
        }

        private void ProcessReq_LocalResources(string url)
        {
            string relPath = url.Substring(5);
            if (!string.IsNullOrEmpty(relPath) && webResources.TryGetValue(relPath, out byte[] contentBytes))
            {
                HttpResponse resp = HttpResponse.CreateDefaultResponse(200);
                AddServerHeader(resp);
                if (webResourcesMime.TryGetValue(relPath, out string mime))
                {
                    if (!string.IsNullOrEmpty(mime)) resp.ContentType = mime;
                }
                resp.ContentBytes = contentBytes;
                httpServer.SendResponse(resp);
            }
            else
            {
                // Not found
                ProcessReq_NotFound();
            }
        }

        private void ProcessReq_Index()
        {
            logger.InfoFormat("Requesting index.html from {0}", httpServer.CurrentRequestEndPoint.ToString());
            HttpResponse resp = HttpResponse.CreateDefaultResponse(200);
            AddServerHeader(resp);
            resp.ContentType = "text/html";
            if (webResources.TryGetValue("index.html", out byte[] contentBytes))
            {
                resp.ContentBytes = contentBytes;
            }
            else
            {
                resp.ContentBytes = Encoding.UTF8.GetBytes("index.html not found.");
            }
            httpServer.SendResponse(resp);
        }

        private void ProcessReq_NotFound()
        {
            logger.InfoFormat("Requesting a non-exist URL {0} from {1}",
                httpServer.CurrentRequest.URL, httpServer.CurrentRequestEndPoint.ToString());
            HttpResponse resp = HttpResponse.CreateDefaultResponse(404);
            AddServerHeader(resp);
            httpServer.SendResponse(resp);
        }

        private void ProcessReq_BadRequest()
        {
            logger.InfoFormat("Bad request from {0}", httpServer.CurrentRequestEndPoint.ToString());
            HttpResponse resp = HttpResponse.CreateDefaultResponse(400);
            AddServerHeader(resp);
            httpServer.SendResponse(resp);
        }

        private void ProcessReq_TempRedir(string location)
        {
            HttpResponse resp = HttpResponse.CreateDefaultResponse(302);
            AddServerHeader(resp);
            resp.Headers.Add("Location", location);
            httpServer.SendResponse(resp);
        }
    }
}
