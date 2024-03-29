﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using log4net;
using MaimaiDXRecordSaver.PageParser;
using System.Threading;
using System.Text;
using System.Net.Sockets;

namespace MaimaiDXRecordSaver
{
    public static partial class Program
    {
        public static readonly string Version = "1.1.3";
        public static ILog Logger = LogManager.GetLogger("Default");
        public static CredentialWebRequester Requester = null;
        public static DataRecorderBase DataRecorder = null;
        public static WebPageProxy WebPageProxy = null;

        public static bool OfflineMode { get; private set; } = false;

        public static void Main(string[] args)
        {
            ShowWelcomeMessage();
            try
            {
                Logger.Info("========正在启动========");

                // Load config
                new ConfigManager("AppConfig.xml").Initialize();

                // Load music list
                if (!MusicList.Init()) return;

                // Check command line arguments
                foreach (string str in args)
                {
                    if(str.ToUpper() == "-MOVETODB")
                    {
                        MoveToDatabase();
                        return;
                    }
                    if(str.ToUpper() == "-OFFLINE")
                    {
                        OfflineMode = true;
                        Logger.Info("正在以离线模式启动(-offline参数)");
                        break;
                    }
                }
                
                // Load credential
                string userId, _t, friendCodeList;
                if (LoadCredential(out userId, out _t, out friendCodeList))
                {
                    Logger.Info("成功加载登录凭据");
                }
                else
                {
                    Logger.Warn("未找到已保存的登录凭据");
                    userId = "";
                    _t = "";
                    friendCodeList = "";
                }

                // Start credential web requester
                Requester = new CredentialWebRequester(userId, _t, friendCodeList);
                Requester.Start();

                // Initialize data recorder
                if (ConfigManager.Instance.SaveMethod.Value == RecordSaveMethod.File)
                {
                    DataRecorder = new DataRecorderFile();
                }
                else
                {
                    DataRecorderDB rec = new DataRecorderDB();
                    rec.Server = ConfigManager.Instance.DBServer.Value;
                    rec.Database = ConfigManager.Instance.DBName.Value;
                    rec.Username = ConfigManager.Instance.DBUsername.Value;
                    rec.Password = ConfigManager.Instance.DBPassword.Value;
                    rec.UseWindowsAuth = ConfigManager.Instance.DBUseWindowsAuth.Value;
                    DataRecorder = rec;
                }
                if (!DataRecorder.Init())
                {
                    return;
                }

                // Check login credential
                if(!OfflineMode)
                {
                    CheckAndEnterCredential();
                }

                // Start web page proxy
                if (!OfflineMode && ConfigManager.Instance.WebPageProxyEnabled.Value)
                {
                    MIMEHelper.Initialize("MIMEMapping.txt");
                    WebPageProxy = new WebPageProxy(
                        ConfigManager.Instance.WebPageProxyIPBind.Value,
                        ConfigManager.Instance.WebPageProxyPort.Value,
                        ConfigManager.Instance.WebPageProxyServerStr.Value);
                    WebPageProxy.Start();
                }

                // Main loop
                while (true)
                {
                    Console.Write("> ");
                    string line = Console.ReadLine();
                    if (line.ToUpper().StartsWith("EXIT")) break;
                    if (!string.IsNullOrEmpty(line))
                    {
                        DispatchCommand(line);
                    }
                }
            }
            catch(Exception err)
            {
                Logger.Fatal("未处理的异常！");
                Logger.Fatal(err.ToString());
                Console.WriteLine("程序意外崩溃，按任意键继续 :(");
                Console.ReadKey();
            }

            // Stop web page proxy
            if(WebPageProxy != null)
            {
                WebPageProxy.Stop();
            }

            // Save credential
            SaveCredential();
        }

        private static void ShowWelcomeMessage()
        {
            Console.WriteLine("欢迎使用 MaimaiDXRecordSaver - 一款用于存储maimaiDX游玩记录的工具");
            Console.WriteLine("由 潘某人-AnonymousPan 制作");
            Console.WriteLine("版本: " + Version);
        }

        public static bool LoadCredential(out string userId, out string _t, out string friendCodeList)
        {
            if(File.Exists("LoginCredential.txt"))
            {
                try
                {
                    string[] lines = File.ReadAllLines("LoginCredential.txt");
                    userId = lines[0];
                    _t = lines[1];
                    friendCodeList = lines[2];
                    return true;
                }
                catch(Exception err)
                {
                    Logger.Warn("无法读取已保存的登录凭据\n" + err.ToString());
                }
            }
            userId = "";
            _t = "";
            friendCodeList = "";
            return false;
        }

        public static void SaveCredential()
        {
            if(Requester != null)
            {
                SaveCredential(Requester.UserID, Requester.TValue, Requester.FriendCodeList);
            }
        }

        public static void SaveCredential(string sessionID, string _t, string friendCodeList)
        {
            string str = sessionID + "\n" + _t + "\n" + friendCodeList + "\n";
            File.WriteAllText("LoginCredential.txt", str);
            Logger.Info("登录凭据已保存");
        }

        public static void CheckAndEnterCredential()
        {
            CredentialWebResponse resp;
            bool credentialInvalid;
            do
            {
                resp = Requester.Request("https://maimai.wahlap.com/maimai-mobile/home/");
                credentialInvalid = resp.Failed && resp.Exception is CredentialInvalidException;
                if(credentialInvalid)
                {
                    Console.WriteLine("无效的登录凭据，请重新输入");
                    EnterCredential();
                }
            }
            while (credentialInvalid);
        }

        public static void EnterCredential()
        {
            if(ConfigManager.Instance.WechatLoginProxyEnabled.Value)
            {
                WechatLoginProxy wechatLoginProxy = new WechatLoginProxy(
                    ConfigManager.Instance.WechatLoginProxyPort.Value,
                    ConfigManager.Instance.WechatLoginProxyUrlWhitelist.Value
                );
                PrintLocalIPs();
                wechatLoginProxy.Start();
                Console.WriteLine("已启动微信登录代理，按任意键来手动输入登录凭据");
                Console.WriteLine("请设置代理并在微信中打开此URL: http://tgk-wcaime.wahlap.com/wc_auth/oauth/authorize/maimai-dx");
                while (!wechatLoginProxy.CredentialCaptured)
                {
                    Thread.Sleep(500);
                    if(Console.KeyAvailable)
                    {
                        Console.ReadKey();
                        break;
                    }
                }
                wechatLoginProxy.Stop();
                if(wechatLoginProxy.CredentialCaptured)
                {
                    Console.WriteLine("微信登录成功(不要忘记改回代理设置)");
                    Requester.UserID = wechatLoginProxy.UserID;
                    Requester.TValue = wechatLoginProxy.TValue;
                    Requester.FriendCodeList = wechatLoginProxy.FriendCodeList;
                    SaveCredential();
                    return;
                }
            }
            Console.WriteLine("请输入你的登录凭据");
            Console.Write("userId: ");
            Requester.UserID = Console.ReadLine();
            Console.Write("_t: ");
            Requester.TValue = Console.ReadLine();
            SaveCredential();
        }

        private static void PrintMusicRecord(int index)
        {
            MusicRecordPageParser parser = new MusicRecordPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/playlogDetail/?idx=" + index.ToString()));
            parser.Parse();
            MusicRecord obj = parser.GetResult();
            Console.WriteLine(obj.ToString());
        }

        private static void SaveRecordID(int index)
        {
            Logger.Info("SaveRecordID: 正在保存记录, idx=" + index.ToString());
            MusicRecordPageParser parser = new MusicRecordPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/playlogDetail/?idx=" + index.ToString()));
            parser.Parse();
            MusicRecord rec = parser.GetResult();
            if (DataRecorder.SaveMusicRecord(rec) == -1)
            {
                Logger.Warn("SaveRecordID: 无法保存记录, index=" + index.ToString());
            }
        }

        private static void MoveToDatabase()
        {
            Logger.Info("开始将记录移动至数据库");
            DataRecorderDB recDB = new DataRecorderDB();
            recDB.Server = ConfigManager.Instance.DBServer.Value;
            recDB.Database = ConfigManager.Instance.DBName.Value;
            recDB.Username = ConfigManager.Instance.DBUsername.Value;
            recDB.Password = ConfigManager.Instance.DBPassword.Value;
            recDB.UseWindowsAuth = ConfigManager.Instance.DBUseWindowsAuth.Value;
            recDB.Init();
            DataRecorderFile recFile = new DataRecorderFile();
            recFile.Init();

            int idMax = recFile.GetLastRecordID();
            for(int i = 0; i < idMax + 1; i++ )
            {
                Console.WriteLine(string.Format("正在移动记录 {0} / {1}.", i, idMax));
                MusicRecord rec = recFile.GetMusicRecord(i);
                recDB.SaveMusicRecord(rec);
            }

            Logger.Info("记录已移动至数据库");
        }

        private static void PrintLocalIPs()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine("[提示] 本机IP地址: ");
            foreach(NetworkInterface i in interfaces)
            {
                IPInterfaceProperties ip = i.GetIPProperties();
                UnicastIPAddressInformationCollection unicastIPInfoColl = ip.UnicastAddresses;
                foreach(UnicastIPAddressInformation unicastIPInfo in unicastIPInfoColl)
                {
                    AddressFamily addrFamily = unicastIPInfo.Address.AddressFamily;
                    if(addrFamily == AddressFamily.InterNetwork
                        || addrFamily == AddressFamily.InterNetworkV6)
                    {
                        Console.WriteLine(string.Format("{0} - {1}",
                            unicastIPInfo.Address.ToString(),
                            i.Name));
                    }
                }
            }
        }

        private static string help =
            "可用命令:\n" +
            "help - 显示可用命令\n" +
            "exit - 保存登录凭据并退出\n" +
            "recid <ID> - 线上获取指定的乐曲记录\n" +
            "reclist - 线上获取最近游玩的乐曲记录\n" +
            "playerinfo - 线上获取玩家信息\n" +
            "saveall - 自动保存最新的乐曲记录\n" +
            "saveid <ID> - 保存指定的乐曲记录\n" +
            "localid <ID> - 获取指定的本地乐曲记录\n" +
            "localrecent <数量> - 显示本地最后N条乐曲记录";
    }
}
