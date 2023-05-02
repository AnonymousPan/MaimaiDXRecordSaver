using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using log4net;
using System.Threading.Tasks;

namespace MaimaiDXRecordSaver
{
    public class CredentialWebRequester
    {
        private object userIdLock = new object();
        private object tValueLock = new object();
        private string m_userId;
        private string m_tValue;
        public string UserID
        {
            get
            {
                lock(userIdLock)
                {
                    return m_userId;
                }
            }
            set
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
            set
            {
                lock(tValueLock)
                {
                    m_tValue = value;
                }
            }
        }
        public string UAString { get; private set; }

        private bool running = false;
        private Thread requesterThread;
        private ConcurrentQueue<Tuple<CredentialWebRequest, AutoResetEvent>> requestQueue;
        private AutoResetEvent requesterThreadWaitHandle;

        private ILog logger = LogManager.GetLogger("WebRequester");

        public CredentialWebRequester(string userID, string _t)
        {
            UserID = userID;
            TValue = _t;
            requestQueue = new ConcurrentQueue<Tuple<CredentialWebRequest, AutoResetEvent>>();
                requesterThread = new Thread(RequesterThreadProc);
            requesterThread.Name = "Credential Web Requester Thread";
            requesterThread.IsBackground = true;
            
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        public void Start()
        {
            running = true;
            requesterThreadWaitHandle = new AutoResetEvent(false);
            requesterThread.Start();
        }

        public void Stop()
        {
            running = false;
            if(requesterThreadWaitHandle != null)
            {
                requesterThreadWaitHandle.Dispose();
                requesterThreadWaitHandle = null;
            }
        }

        public void Terminate()
        {
            running = false;
            requesterThread.Abort();
            if (requesterThreadWaitHandle != null)
            {
                requesterThreadWaitHandle.Dispose();
                requesterThreadWaitHandle = null;
            }
        }

        public string RequestString(string url)
        {
            CredentialWebRequest req = new CredentialWebRequest(url);
            CredentialWebResponse resp = Request(req);
            if(resp.Failed)
            {
                throw new CredentialWebRequesterException("Exception while requesting", resp.Exception);
            }
            else
            {
                return resp.GetContentAsUTF8String();
            }
        }

        public CredentialWebResponse Request(CredentialWebRequest req)
        {
            AutoResetEvent waitHandle = new AutoResetEvent(false);
            requestQueue.Enqueue(new Tuple<CredentialWebRequest, AutoResetEvent>(req, waitHandle));
            // Resume the requester thread
            requesterThreadWaitHandle.Set();
            // Wait for the request to be completed
            waitHandle.WaitOne();

            waitHandle.Dispose();
            return req.Response;
        }

        public CredentialWebResponse Request(string url)
        {
            return Request(new CredentialWebRequest(url));
        }

        public async Task<string> RequestStringAsync(string url)
        {
            CredentialWebRequest req = new CredentialWebRequest(url);
            CredentialWebResponse resp = await RequestAsync(req);
            if (resp.Failed)
            {
                throw new CredentialWebRequesterException("Exception while requesting", resp.Exception);
            }
            else
            {
                return resp.GetContentAsUTF8String();
            }
        }

        public async Task<CredentialWebResponse> RequestAsync(CredentialWebRequest req)
        {
            AutoResetEvent waitHandle = new AutoResetEvent(false);
            requestQueue.Enqueue(new Tuple<CredentialWebRequest, AutoResetEvent>(req, waitHandle));
            // Resume the requester thread
            requesterThreadWaitHandle.Set();
            // Wait for the request to be completed
            await Task.Run(() => waitHandle.WaitOne());
            return req.Response;
        }

        public async Task<CredentialWebResponse> RequestAsync(string url)
        {
            return await RequestAsync(new CredentialWebRequest(url));
        }

        private void RequesterThreadProc()
        {
            logger.Info("凭据网络请求线程已启动");
            while(running)
            {
                while(requestQueue.Count > 0 && requestQueue.TryDequeue(out Tuple<CredentialWebRequest, AutoResetEvent> tuple))
                {
                    CredentialWebRequest req = tuple.Item1;
                    AutoResetEvent waitHandle = tuple.Item2;
                    string postFlag = req.IsPost ? "[POST] " : "";
                    logger.Debug("正在请求URL: " + postFlag + req.URL);

                    CredentialWebResponse resp = null;
                    HttpWebResponse httpResp = null;
                    try
                    {
                        HttpWebRequest httpReq = req.CreateHttpWebRequest(UserID, TValue, UAString);
                        httpResp = (HttpWebResponse)httpReq.GetResponse();
                        // Credential invalid
                        // Got a 302 and location header contains "error"
                        if (httpResp.StatusCode == HttpStatusCode.Found
                            && httpResp.Headers[HttpResponseHeader.Location].Contains("error"))
                        {
                            throw new CredentialInvalidException(req.URL, UserID, TValue);
                        }

                        // Read response content
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            using(Stream respStream = httpResp.GetResponseStream())
                            {
                                byte[] buf = new byte[4096];
                                int readCount;
                                while((readCount = respStream.Read(buf, 0, 4096)) > 0)
                                {
                                    memStream.Write(buf, 0, readCount);
                                }
                                resp = new CredentialWebResponse((int)httpResp.StatusCode, memStream.ToArray(), httpResp.ContentType);
                            }
                        }

                        // HTTP status code 301 / 302
                        if(httpResp.StatusCode == HttpStatusCode.Moved || httpResp.StatusCode == HttpStatusCode.Found)
                        {
                            resp.Location = httpResp.Headers["Location"];
                        }
                        
                        // Update credential cookies
                        Cookie userIdCookie = httpResp.Cookies["userId"];
                        if(userIdCookie != null)
                        {
                            UserID = userIdCookie.Value;
                        }
                        Cookie tValueCookie = httpResp.Cookies["_t"];
                        if(tValueCookie != null)
                        {
                            TValue = tValueCookie.Value;
                        }
                        if(userIdCookie != null || tValueCookie != null)
                        {
                            resp.SetCredentialInfo(UserID, TValue);
                        }
                    }
                    catch(Exception err)
                    {
                        if(err is CredentialInvalidException)
                        {
                            logger.Warn("无效的凭据");
                        }
                        else
                        {
                            logger.Warn("请求时发生异常\n" + err.ToString());
                        }
                        resp = new CredentialWebResponse(err);
                    }
                    finally
                    {
                        if (httpResp != null) httpResp.Dispose();
                        req.Response = resp;
                        waitHandle.Set();
                    }
                }
                // Suspend requester thread when requests are completed
                requesterThreadWaitHandle.WaitOne();
            }
        }
    }
}
