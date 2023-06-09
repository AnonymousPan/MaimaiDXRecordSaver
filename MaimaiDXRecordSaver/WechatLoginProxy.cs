using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MaimaiDXRecordSaver
{
    public class WechatLoginProxy
    {
        private Thread wechatLoginProxyThread;
        private bool running = false;
        private ILog logger = LogManager.GetLogger("WechatLoginProxy");
        private int port = 0;
        private bool whitelist;

        private MyHttpServer httpServer;
        private HttpClient httpClient;

        private bool m_credentialCaptured = false;
        private object credentialCapturedLock = new object();
        private string m_userId;
        private object userIdLock = new object();
        private string m_tValue;
        private object tValueLock = new object();
        private string m_friendCodeList;
        private object friendCodeListLock = new object();

        private static readonly byte[] urlNotWhitelistedMsg = Encoding.UTF8.GetBytes("MaimaiDXRecordSaver-WechatLoginProxy: This URL is not whitelisted.");
        private static readonly byte[] closeWindowMsg = Encoding.UTF8.GetBytes("MaimaiDXRecordSaver-WechatLoginProxy: Logged in successfully. You can close the window now.");
        private static readonly byte[] failedMsg = Encoding.UTF8.GetBytes("MaimaiDXRecordSaver-WechatLoginProxy: Failed, please check logs.");

        public bool CredentialCaptured
        {
            get
            {
                lock(credentialCapturedLock)
                {
                    return m_credentialCaptured;
                }
            }
            private set
            {
                lock(credentialCapturedLock)
                {
                    m_credentialCaptured = value;
                }
            }
        }

        public string UserID
        {
            get
            {
                lock(userIdLock)
                {
                    return m_userId;
                }
            }
            private set
            {
                lock(userIdLock)
                {
                    m_userId = value;
                }
            }
        }

        public string TValue
        {
            get
            {
                lock(tValueLock)
                {
                    return m_tValue;
                }
            }
            private set
            {
                lock(tValueLock)
                {
                    m_tValue = value;
                }
            }
        }

        public string FriendCodeList
        {
            get
            {
                lock(friendCodeListLock)
                {
                    return m_friendCodeList;
                }
            }
            private set
            {
                lock(friendCodeListLock)
                {
                    m_friendCodeList = value;
                }
            }
        }

        public WechatLoginProxy(int _port, bool _whitelist)
        {
            port = _port;
            whitelist = _whitelist;
            wechatLoginProxyThread = new Thread(WechatLoginProxyThreadProc);
            wechatLoginProxyThread.Name = "WechatLoginProxy Thread";
            wechatLoginProxyThread.IsBackground = true;

            httpServer = new MyHttpServer(new IPAddress(0L), port);

            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            httpClient = new HttpClient(handler);
        }

        public void Start()
        {
            running = true;
            wechatLoginProxyThread.Start();
        }

        public void Stop()
        {
            running = false;
            wechatLoginProxyThread.Abort();
            httpServer.Stop();
            logger.Info("微信登录代理已停止");
        }

        private void WechatLoginProxyThreadProc()
        {
            logger.InfoFormat("微信登录代理已在端口 {0} 上运行", port);
            httpServer.Start();
            while(running)
            {
                string reqUrl = "N/A";
                string reqIP = "N/A";
                try
                {
                    httpServer.WaitForRequest();
                    reqIP = httpServer.CurrentRequestEndPoint.ToString();
                    if (httpServer.CurrentRequestValid)
                    {
                        reqUrl = httpServer.CurrentRequest.URL;
                        string method = httpServer.CurrentRequest.Method;
                        if (IsUrlWhiteListed(reqUrl))
                        {
                            logger.InfoFormat("正常请求 {0} {1} IP={2}", method, reqUrl, reqIP);
                            if(reqUrl.Contains("MyAPI/GetAuthUrl"))
                            {
                                HttpResponse resp = HttpResponse.CreateDefaultResponse(200);
                                string str = GetAuthUrl();
                                resp.ContentBytes = Encoding.UTF8.GetBytes(str);
                                httpServer.SendResponse(resp);
                            }
                            else if (method.ToUpper() == "CONNECT")
                            {
                                string[] arr = reqUrl.Split(':');
                                if (arr.Length == 2 && int.TryParse(arr[1], out int serverPort))
                                {
                                    TcpClient client = httpServer.HandleConnection();
                                    Task.Run(async () => await ProcessHttpConnect(client, arr[0], serverPort));
                                }
                                else
                                {
                                    SendBadRequest();
                                }
                            }
                            else
                            {
                                TcpClient client = httpServer.HandleConnection();
                                Task.Run(async () => await ProcessNormalHttpRequest(httpServer.CurrentRequest, client));
                            }
                        }
                        else
                        {
                            logger.InfoFormat("不在白名单请求 {0} {1} IP={2}", method, reqUrl, reqIP);
                            HttpResponse resp = HttpResponse.CreateDefaultResponse(403);
                            resp.ContentBytes = urlNotWhitelistedMsg;
                            httpServer.SendResponse(resp);
                        }
                    }
                    else
                    {
                        SendBadRequest();
                    }
                }
                catch(Exception err)
                {
                    if(err is ThreadAbortException)
                    {
                        return;
                    }
                    logger.WarnFormat("处理请求时发生异常 [IP={0} URL={1}]\n{2}", reqIP, reqUrl, err.ToString());
                }
            }
        }

        private void SendBadRequest()
        {
            HttpResponse resp = HttpResponse.CreateDefaultResponse(400);
            httpServer.SendResponse(resp);
        }

        private void SendNotFound()
        {
            HttpResponse resp = HttpResponse.CreateDefaultResponse(404);
            httpServer.SendResponse(resp);
        }

        private async Task ProcessHttpConnect(TcpClient client, string domain, int port)
        {
            IPAddress[] addrList = await Dns.GetHostAddressesAsync(domain);
            if(addrList == null || addrList.Length == 0)
            {
                HttpResponse httpResp = HttpResponse.CreateDefaultResponse(404);
                byte[] bytes = httpResp.ToBytes();
                await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                return;
            }

            TcpClient server = new TcpClient();
            await server.ConnectAsync(addrList[0], port);

            NetworkStream clientStream = client.GetStream();
            NetworkStream serverStream = server.GetStream();
            HttpResponse resp = HttpResponse.CreateDefaultResponse(200);
            resp.StatusMessage = "Connection Established";
            byte[] okResp = resp.ToBytes();
            await clientStream.WriteAsync(okResp, 0, okResp.Length);

            int counter = 0;
            while(true)
            {
                if(clientStream.DataAvailable)
                {
                    int bytesRead;
                    byte[] buf = new byte[2048];
                    do
                    {
                        bytesRead = await clientStream.ReadAsync(buf, 0, 2048);
                        await serverStream.WriteAsync(buf, 0, bytesRead);
                    }
                    while (bytesRead >= 2048);
                }
                else if(serverStream.DataAvailable)
                {
                    int bytesRead;
                    byte[] buf = new byte[2048];
                    do
                    {
                        bytesRead = await serverStream.ReadAsync(buf, 0, 2048);
                        await clientStream.WriteAsync(buf, 0, bytesRead);
                    }
                    while (bytesRead >= 2048);
                }
                else
                {
                    Thread.Sleep(1);
                    counter++;
                }
                if (counter > 10000) break;
                // bool flag = server.Client.Poll(2000, SelectMode.SelectRead);
                // bool flag2 = server.Client.Poll(2000, SelectMode.SelectWrite);
            }
            client.Close();
            server.Close();
        }

        private async Task ProcessNormalHttpRequest(HttpRequest req, TcpClient client)
        {
            HttpRequestMessage reqMessage = null;
            HttpResponseMessage respMessage = null;
            NetworkStream clientStream = client.GetStream();
            try
            {
                if(await ProcessSpecialUrl(req, clientStream))
                {
                    return;
                }
                
                reqMessage = new HttpRequestMessage(new HttpMethod(req.Method), req.URL);
                foreach (KeyValuePair<string, string> header in req.Headers)
                {
                    reqMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                if (req.ContentBytes != null && req.ContentBytes.Length > 0)
                {
                    HttpContent content = new ByteArrayContent(req.ContentBytes);
                    reqMessage.Content = content;
                }
                respMessage = await httpClient.SendAsync(reqMessage);

                HttpResponse resp = new HttpResponse();
                resp.StatusCode = (int)respMessage.StatusCode;
                resp.StatusMessage = HttpResponse.GetDefaultStatusMessage(resp.StatusCode);
                foreach(KeyValuePair<string, IEnumerable<string>> header in respMessage.Headers)
                {
                    if(header.Key.ToLower() != "connection")
                    {
                        StringBuilder sb = new StringBuilder();
                        IEnumerator<string> values = header.Value.GetEnumerator();
                        bool hasNext = values.MoveNext();
                        while (hasNext)
                        {
                            sb.Append(values.Current);
                            hasNext = values.MoveNext();
                            if (hasNext) sb.Append(", ");
                        }
                        resp.Headers.Add(header.Key, sb.ToString());
                    }
                }
                if(respMessage.Content != null)
                {
                    byte[] contentBytes = await respMessage.Content.ReadAsByteArrayAsync();
                    resp.ContentBytes = contentBytes;
                }
                byte[] respBytes = resp.ToBytes();
                await clientStream.WriteAsync(respBytes, 0, respBytes.Length);
            }
            catch(Exception err)
            {
                logger.WarnFormat("处理一般请求时发生异常 [IP={0} URL={1}]\n{2}", client.Client.RemoteEndPoint.ToString(), req.URL, err.ToString());
                try
                {
                    SendResponse500(clientStream);
                }
                catch (Exception) { }
            }
            finally
            {
                reqMessage?.Dispose();
                respMessage?.Dispose();
                clientStream?.Dispose();
                client.Close();
            }
        }

        private async Task<bool> ProcessSpecialUrl(HttpRequest req, NetworkStream clientStream)
        {
            string url = req.URL;
            if(url.Contains("tgk-wcaime.wahlap.com/wc_auth/oauth/authorize/maimai-dx"))
            {
                // STAGE 1
                await SpecialProcStage1(req, clientStream);
                return true;
            }
            if (url.Contains("tgk-wcaime.wahlap.com/wc_auth/oauth/callback/maimai-dx?"))
            {
                // STAGE 2
                await SpecialProcStage2(req, clientStream);
                return true;
            }
            if (url.Contains("/maimai-mobile/?t="))
            {
                // STAGE 3
                // #### UNUSED ####
                await SpecialProcStage3(req, clientStream);
                return true;
            }
            return false;
        }

        // STAGE 1 - https://tgk-wcaime.wahlap.com/wc_auth/oauth/authorize/maimai-dx
        // Redirect to Wechat OAuth API
        // Rewrite parameter "redirect_uri" (https -> http)
        private async Task SpecialProcStage1(HttpRequest req, NetworkStream clientStream)
        {
            string url = req.URL;
            HttpResponseMessage respMsg = await httpClient.GetAsync(UrlHttpToHttps(url));
            if(respMsg.StatusCode == HttpStatusCode.Found)
            {
                Uri locationUri = respMsg.Headers.Location;
                if(locationUri == null)
                {
                    logger.Warn("阶段 1 异常: 响应没有Location标头");
                }
                else
                {
                    string location = locationUri.ToString();
                    int index = location.IndexOf("&redirect_uri=");
                    if(index > 0)
                    {
                        index += 14;
                        location = location.Substring(0, index) + "http" + location.Substring(index + 5);
                        logger.Info("阶段 1 正常");
                        SendResponse302(clientStream, location);
                        return;
                    }
                    logger.Warn("阶段 1 异常: 重定向URL不正确 " + location);
                }
            }
            else
            {
                logger.Warn("阶段 1 异常: HTTP状态码不是302");
            }
            SendResponse200(clientStream, failedMsg);
        }

        // STAGE 2 - https://tgk-wcaime.wahlap.com/wc_auth/oauth/callback/maimai-dx?r=...
        // Callback of Wechat OAuth API
        private async Task SpecialProcStage2(HttpRequest req, NetworkStream clientStream)
        {
            string url = req.URL;
            HttpResponseMessage respMsg = await httpClient.GetAsync(UrlHttpToHttps(url));
            if (respMsg.StatusCode == HttpStatusCode.Found)
            {
                Uri locationUri = respMsg.Headers.Location;
                if (locationUri == null)
                {
                    logger.Warn("阶段 2 异常: 响应没有Location标头");
                }
                else
                {
                    string location = locationUri.ToString();
                    if(location.Contains("/maimai-mobile/?t="))
                    {
                        int index = location.IndexOf("/?t=");
                        if(index > 0)
                        {
                            string tempToken = location.Substring(index + 4);
                            if (await ExchangeTempToken(tempToken))
                            {
                                logger.Info("阶段 2 正常");
                                SendResponse200(clientStream, closeWindowMsg);
                                return;
                            }
                            else
                            {
                                logger.Warn("阶段 2 异常: 临时token交换失败");
                            }
                        }
                        else
                        {
                            logger.Warn("阶段 2 异常: 重定向URL不正确");
                        }
                    }
                    else
                    {
                        logger.Warn("阶段 2 异常: 重定向URL不正确 " + location);
                    }
                }
            }
            else
            {
                logger.Warn("阶段 2 异常: HTTP状态码不是302");
            }
            SendResponse200(clientStream, failedMsg);
        }

        // STAGE 3 - https://maimai.wahlap.com/maimai-mobile/?t=...
        // Redirect to home page and set credential cookies
        // #### UNUSED ####
        private async Task SpecialProcStage3(HttpRequest req, NetworkStream clientStream)
        {
            string url = req.URL;
            HttpResponseMessage respMsg = await httpClient.GetAsync(UrlHttpToHttps(url));
            if (respMsg.StatusCode == HttpStatusCode.Found)
            {
                Uri locationUri = respMsg.Headers.Location;
                if (locationUri == null)
                {
                    logger.Warn("阶段 3 异常: 响应没有Location标头");
                }
                else
                {
                    string location = locationUri.ToString();
                    if (location.Contains("/maimai-mobile/home/"))
                    {
                        IEnumerable<string> cookieValues = respMsg.Headers.GetValues("Set-Cookies");

                        logger.Info("阶段 3 正常");
                        SendResponse302(clientStream, location);
                        return;
                    }
                    else
                    {
                        logger.Warn("阶段 3 异常: 重定向URL不正确 " + location);
                    }
                }
            }
            else
            {
                logger.Warn("阶段 3 异常: HTTP状态码不是302");
            }
            SendResponse200(clientStream, failedMsg);
        }

        private async Task<bool> ExchangeTempToken(string tempToken)
        {
            try
            {
                string url = "https://maimai.wahlap.com/maimai-mobile/?t=" + tempToken;
                HttpResponseMessage respMsg = await httpClient.GetAsync(url);

                string userId = null;
                string _t = null;
                string friendCodeList = null;
                Regex userIdRegex = new Regex("userId=[0-9a-z]+");
                Regex _tRegex = new Regex("_t=[0-9a-f]+");
                Regex friendCodeListRegex = new Regex("friendCodeList=([0-9]+(%2[Cc])?)+");

                IEnumerable<string> cookieHeaders = respMsg.Headers.GetValues("Set-Cookie");
                foreach(string setCookie in cookieHeaders)
                {
                    Match match;
                    if((match = userIdRegex.Match(setCookie)).Success)
                    {
                        string str = match.Value;
                        if (str.Length > 7) userId = str.Substring(7);
                    }
                    else if((match = _tRegex.Match(setCookie)).Success)
                    {
                        string str = match.Value;
                        if (str.Length > 3) _t = str.Substring(3);
                    }
                    else if((match = friendCodeListRegex.Match(setCookie)).Success)
                    {
                        string str = match.Value;
                        if (str.Length > 15) friendCodeList = str.Substring(15);
                    }
                }
                if(!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(_t))
                {
                    UserID = userId;
                    TValue = _t;
                    FriendCodeList = friendCodeList;
                    CredentialCaptured = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception err)
            {
                logger.Warn("交换临时token失败\n" + err.ToString());
                return false;
            }
        }

        private void SendResponse200(NetworkStream cliStream, byte[] content)
        {
            HttpResponse resp = HttpResponse.CreateDefaultResponse(200);
            resp.ContentBytes = content;
            SendResponse(cliStream, resp);
        }

        private void SendResponse302(NetworkStream cliStream, string location)
        {
            HttpResponse resp = HttpResponse.CreateDefaultResponse(302);
            resp.Headers.Add("Location", location);
            SendResponse(cliStream, resp);
        }

        private void SendResponse500(NetworkStream cliStream)
        {
            HttpResponse resp = HttpResponse.CreateDefaultResponse(500);
            SendResponse(cliStream, resp);
        }

        private void SendResponse(NetworkStream cliStream, HttpResponse resp)
        {
            byte[] bytes = resp.ToBytes();
            cliStream.Write(bytes, 0, bytes.Length);
        }

        private string UrlHttpToHttps(string url)
        {
            if(!url.StartsWith("https"))
            {
                return "https" + url.Substring(4);
            }
            else
            {
                return url;
            }
        }

        private bool IsUrlWhiteListed(string url)
        {
            if (!whitelist) return true;
            int index;
            string str;
            if(url.StartsWith("http"))
            {
                index = url.IndexOf('/');
                if (index + 2 >= url.Length) return false;
                str = url.Substring(index + 2);
            }
            else
            {
                str = url;
            }

            index = str.IndexOf('/');
            if(index == -1)
            {
                int i = str.IndexOf(':');
                if (i > 0) index = i;
            }
            string domain = str.Substring(0, index).ToLower();
            for(int i = 0; i < domainWhitelist.Length; i++ )
            {
                if (domainWhitelist[i] == domain) return true;
            }
            return false;
        }

        public static string GetAuthUrl()
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("https://tgk-wcaime.wahlap.com/wc_auth/oauth/authorize/maimai-dx");
            req.Method = "GET";
            req.UserAgent = "Mozilla/5.0 (Linux; Android 11; SM-E5260 Build/RP1A.200720.012; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/107.0.5304.141 Mobile Safari/537.36 XWEB/5075 MMWEBSDK/20220903 MMWEBID/1767 MicroMessenger/8.0.28.2240(0x28001C57) WeChat/arm64 Weixin NetType/WIFI Language/zh_CN ABI/arm64";
            req.AllowAutoRedirect = false;
            using(HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                if(resp.StatusCode == HttpStatusCode.Found)
                {
                    string location = resp.Headers["Location"];
                    if (string.IsNullOrEmpty(location)) return null;
                    int index = location.IndexOf("&redirect_uri=") + 14;
                    string redirUri = "http" + location.Substring(index + 5);
                    return location.Substring(0, index) + redirUri;
                }
                else
                {
                    return null;
                }
            }
        }

        private static readonly string[] domainWhitelist = new string[]
        {
            "127.0.0.1",
            "localhost",
            "tgk-wcaime.wahlap.com",
            "maimai.wahlap.com",
            "open.weixin.qq.com",
            "weixin110.qq.com",
            "res.wx.qq.com",
        };
    }
}
