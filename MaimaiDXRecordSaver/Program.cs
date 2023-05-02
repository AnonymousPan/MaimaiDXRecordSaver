using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using MaimaiDXRecordSaver.PageParser;
using System.Threading;

namespace MaimaiDXRecordSaver
{
    public static partial class Program
    {
        public static readonly string Version = "1.1.2";
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
                string sessionID, _t;
                if (LoadCredential(out sessionID, out _t))
                {
                    Logger.Info("成功加载登录凭据");
                }
                else
                {
                    Logger.Info("未找到已保存的登录凭据");
                    Console.WriteLine("请输入你的登录凭据");
                    Console.Write("userId: ");
                    sessionID = Console.ReadLine();
                    Console.Write("_t: ");
                    _t = Console.ReadLine();
                    SaveCredential(sessionID, _t);
                }

                // Start credential web requester
                Requester = new CredentialWebRequester(sessionID, _t);
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

        private static void OnCredentialsChange(string s, string t)
        {
            Requester.UserID = s;
            Requester.TValue = t;
            Logger.Info(string.Format("OnCredentialChange sessionID={0} _t={1}", s, t));
        }

        private static void ShowWelcomeMessage()
        {
            Console.WriteLine("欢迎使用 MaimaiDXRecordSaver - 一款用于存储maimaiDX游玩记录的工具");
            Console.WriteLine("由 潘某人-AnonymousPan 制作");
            Console.WriteLine("版本: " + Version);
        }

        public static bool LoadCredential(out string sessionID, out string _t)
        {
            if(File.Exists("LoginCredential.txt"))
            {
                try
                {
                    string[] lines = File.ReadAllLines("LoginCredential.txt");
                    sessionID = lines[0];
                    _t = lines[1];
                    return true;
                }
                catch(Exception err)
                {
                    Logger.Warn("无法读取已保存的登录凭据\n" + err.ToString());
                    sessionID = "";
                    _t = "";
                    return false;
                }
            }
            else
            {
                sessionID = "";
                _t = "";
                return false;
            }
        }

        public static void SaveCredential()
        {
            if(Requester != null)
            {
                SaveCredential(Requester.UserID, Requester.TValue);
            }
        }

        public static void SaveCredential(string sessionID, string _t)
        {
            string str = sessionID + "\n" + _t;
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
