using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MaimaiDXRecordSaver
{
    public class MyHttpServer
    {
        public HttpRequest CurrentRequest { get; private set; }
        public bool CurrentRequestValid { get; private set; }
        public bool HaveRequest { get; private set; }
        public EndPoint CurrentRequestEndPoint { get; private set; }

        private TcpListener tcpListener;
        private TcpClient currentClient;

        public MyHttpServer(IPAddress ipBind, int port)
        {
            tcpListener = new TcpListener(ipBind, port);
        }

        public void Start()
        {
            tcpListener.Start();
        }

        public void Stop()
        {
            tcpListener.Stop();
        }

        public void CloseCurrentConnection()
        {
            if (currentClient != null)
            {
                currentClient.Close();
                currentClient = null;
            }
        }

        public void WaitForRequest()
        {
            RestartWaiting:
            CloseCurrentConnection();

            currentClient = tcpListener.AcceptTcpClient();
            NetworkStream connStream = currentClient.GetStream();
            
            HttpRequest req = new HttpRequest();
            connStream.ReadTimeout = 10000;
            connStream.WriteTimeout = 10000;
            if(!connStream.DataAvailable)
            {
                // Wait for incoming data
                for(int i = 0; i < 20; i++ )
                {
                    Thread.Sleep(5);
                    if(connStream.DataAvailable)
                    {
                        goto Continue;
                    }
                }
                connStream.Close();
                goto RestartWaiting;
            }

            Continue:
            bool reqValid = req.ParseRequestFromStream(connStream);
            CurrentRequestEndPoint = currentClient.Client.RemoteEndPoint;
            CurrentRequestValid = reqValid;
            CurrentRequest = reqValid ? req : null;
        }

        public void SendResponse(HttpResponse resp)
        {
            byte[] respBytes = resp.ToBytes();
            NetworkStream connStream = currentClient.GetStream();
            connStream.Write(respBytes, 0, respBytes.Length);

            CloseCurrentConnection();
        }

        /// <summary>
        /// Take over the current TCP connection
        /// </summary>
        /// <returns>TcpClient object of current connection</returns>
        public TcpClient HandleConnection()
        {
            TcpClient cli = currentClient;
            currentClient = null;
            return cli;
        }
    }
}
