using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using MaimaiDXRecordSaver.PageParser;
using System.Threading;

namespace MaimaiDXRecordSaver
{
    public static class Program
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
                Logger.Info("========Starting Up========");
                new ConfigManager("AppConfig.xml").Initialize();
                if (!MusicList.Init()) return;
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
                        Logger.Info("Offline Mode enabled because -offline appeared in arguments.");
                        break;
                    }
                }
                if(!OfflineMode && IsServerClosedNow())
                {
                    OfflineMode = true;
                    Logger.Info("Offline Mode enabled because the server is closed.");
                }
                
                
                string sessionID, _t;
                if (LoadCredential(out sessionID, out _t))
                {
                    Logger.Info("Load credential OK.");
                    Logger.Info("userId = " + sessionID);
                    Logger.Info("_t = " + _t);
                }
                else
                {
                    Logger.Info("Saved credential not found.");
                    Console.WriteLine("Please enter your login credential.");
                    Console.Write("userId: ");
                    sessionID = Console.ReadLine();
                    Console.Write("_t: ");
                    _t = Console.ReadLine();
                    SaveCredential(sessionID, _t);
                }
                Requester = new CredentialWebRequester(sessionID, _t);
                Requester.Start();

                // TEST
                // *************************
                /*
                WebPageProxy = new WebPageProxy(ConfigManager.Instance.WebPageProxyIPBind.Value,
                        ConfigManager.Instance.WebPageProxyPort.Value);
                WebPageProxy.UpdateCredential(Requester.UserID, Requester.TValue);
                WebPageProxy.OnCredentialChange += OnCredentialsChange;
                WebPageProxy.Start();
                Console.ReadKey();
                */

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

                if(!OfflineMode)
                {
                    CheckAndEnterCredential();
                }


                if (!OfflineMode && ConfigManager.Instance.WebPageProxyEnabled.Value)
                {
                    WebPageProxy = new WebPageProxy(ConfigManager.Instance.WebPageProxyIPBind.Value,
                        ConfigManager.Instance.WebPageProxyPort.Value);
                    WebPageProxy.UpdateCredential(Requester.UserID, Requester.TValue);
                    WebPageProxy.OnCredentialChange += OnCredentialsChange;
                    WebPageProxy.Start();
                }

                while (true)
                {
                    Console.Write("> ");
                    string line = Console.ReadLine();
                    bool exit = false;
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] arr = line.Split(' ');
                        string command = arr[0].ToUpper();
                        switch (command)
                        {
                            case "HELP":
                                Console.WriteLine(help);
                                break;
                            case "EXIT":
                                exit = true;
                                break;
                            case "RECID":
                                if (arr.Length >= 2)
                                {
                                    int index = 0;
                                    if (int.TryParse(arr[1], out index))
                                    {
                                        Logger.Info("Command: RecID, index=" + index);
                                        PrintMusicRecord(index);
                                        SaveCredential();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid number.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Missing arguments.");
                                }
                                break;
                            case "RECLIST":
                                PrintMusicRecordList();
                                SaveCredential();
                                break;
                            case "PLAYERINFO":
                                Logger.Info("Command: PlayerInfo");
                                PrintPlayerInfo();
                                SaveCredential();
                                break;
                            case "SAVEALL":
                                Logger.Info("Command: SaveAll");
                                AutoSaveRecords();
                                SaveCredential();
                                break;
                            case "SAVEID":
                                if (arr.Length >= 2)
                                {
                                    int index = 0;
                                    if (int.TryParse(arr[1], out index))
                                    {
                                        Logger.Info("Command: SaveID");
                                        SaveRecordID(index);
                                        SaveCredential();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid number.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Missing arguments.");
                                }
                                break;
                            case "LOCALID":
                                if (arr.Length >= 2)
                                {
                                    int index = 0;
                                    if (int.TryParse(arr[1], out index))
                                    {
                                        Logger.Info("Command: LocalID");
                                        if (DataRecorder.IsRecordExists(index))
                                        {
                                            MusicRecord rec = DataRecorder.GetMusicRecord(index);
                                            Console.WriteLine(rec.ToString());
                                        }
                                        else
                                        {
                                            Console.WriteLine("Record not found.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid number.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Missing arguments.");
                                }
                                break;
                            case "LOCALRECENT":
                                if (arr.Length >= 2)
                                {
                                    int num = 0;
                                    if (int.TryParse(arr[1], out num))
                                    {
                                        Logger.Info("Command: LocalRecent");
                                        int latestID = DataRecorder.GetLastRecordID();
                                        int j = Math.Max(latestID - num + 1, 0);
                                        for(int i = latestID; i >= j; i-- )
                                        {
                                            Console.WriteLine("Local ID " + i.ToString());
                                            if(DataRecorder.IsRecordExists(i))
                                            {
                                                Console.Write(DataRecorder.GetMusicRecordSummary(i).ToString());
                                            }
                                            else
                                            {
                                                Console.WriteLine("Record not found!");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid number.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Missing arguments.");
                                }
                                break;
                            default:
                                Console.WriteLine("Unknown command! Type \"help\" for help.");
                                break;
                        }
                    }
                    if (exit) break;
                }
            }
            catch(Exception err)
            {
                Logger.Fatal("Unhandled Exception!");
                Logger.Fatal(err.ToString());
                Console.WriteLine("Application crashed, press any key to continue :(");
                Console.ReadKey();
            }
            if(WebPageProxy != null)
            {
                WebPageProxy.Stop();
            }
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
            Console.WriteLine("Welcome to use MaimaiDX Record Saver");
            Console.WriteLine("Made by AnonymousPan");
            Console.WriteLine("Version: " + Version);
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
                    Logger.Warn("Can not read login credential.\n" + err.ToString());
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
            if (WebPageProxy != null)
            {
                lock(WebPageProxy)
                {
                    WebPageProxy.UpdateCredential(Requester.UserID, Requester.TValue);
                }
            }
        }

        public static void SaveCredential(string sessionID, string _t)
        {
            string str = sessionID + "\n" + _t;
            File.WriteAllText("LoginCredential.txt", str);
            Logger.Info("Credential saved.");
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
                    Console.WriteLine("Invalid credential! Please enter your credential.");
                    EnterCredential();
                }
            }
            while (credentialInvalid);
        }

        public static void EnterCredential()
        {
            Console.WriteLine("Please enter your login credential.");
            Console.Write("userId: ");
            Requester.UserID = Console.ReadLine();
            Console.Write("_t: ");
            Requester.TValue = Console.ReadLine();
            SaveCredential();
        }

        private static void PrintPlayerInfo()
        {
            PlayerInfoPageParser parser = new PlayerInfoPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/playerData/"));
            parser.Parse();
            PlayerInfo obj = parser.GetResult();
            Console.WriteLine(obj.ToString());
        }

        private static void PrintMusicRecord(int index)
        {
            MusicRecordPageParser parser = new MusicRecordPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/playlogDetail/?idx=" + index.ToString()));
            parser.Parse();
            MusicRecord obj = parser.GetResult();
            Console.WriteLine(obj.ToString());
        }

        private static void PrintMusicRecordList()
        {
            MusicRecordListPageParser parser = new MusicRecordListPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/"));
            parser.Parse();
            List<MusicRecordSummary> list = parser.GetResult();
            for(int i = 0; i < list.Count; i++ )
            {
                Console.Write(i.ToString() + ". " + list[i].ToString());
            }
        }

        private static void AutoSaveRecords()
        {
            MusicRecordListPageParser parser1 = new MusicRecordListPageParser();
            parser1.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/"));
            parser1.Parse();
            List<MusicRecordSummary> list = parser1.GetResult();
            int[] indices = DataRecorder.GetRecordIndicesNeedToSave(list);
            MusicRecordPageParser parser2 = new MusicRecordPageParser();
            for(int i = indices.Length - 1; i >= 0; i-- )
            {
                int index = indices[i];
                Logger.Info("AutoSaveRecords: Saving record, idx=" + index.ToString());
                parser2.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/playlogDetail/?idx=" + index.ToString()));
                parser2.Parse();
                MusicRecord rec = parser2.GetResult();
                if(DataRecorder.SaveMusicRecord(rec) == -1)
                {
                    Logger.Warn("AutoSaveRecords: Failed to save music record, index=" + index.ToString());
                }
                Thread.Sleep(500);
            }
        }

        private static bool IsServerClosedNow()
        {
            DateTime now = DateTime.Now;
            return now.Hour >= 4 && now.Hour <= 7;
        }

        private static void SaveRecordID(int index)
        {
            Logger.Info("SaveRecordID: Saving record, idx=" + index.ToString());
            MusicRecordPageParser parser = new MusicRecordPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/playlogDetail/?idx=" + index.ToString()));
            parser.Parse();
            MusicRecord rec = parser.GetResult();
            if (DataRecorder.SaveMusicRecord(rec) == -1)
            {
                Logger.Warn("SaveRecordID: Failed to save music record, index=" + index.ToString());
            }
        }

        private static void MoveToDatabase()
        {
            Logger.Info("Started moving records to database.");
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
                Console.WriteLine(string.Format("Moving record {0} / {1}.", i, idMax));
                MusicRecord rec = recFile.GetMusicRecord(i);
                recDB.SaveMusicRecord(rec);
            }

            Logger.Info("Records moved to database.");
        }

        private static string help =
            "Available commands:\n" +
            "help - Show available commands.\n" +
            "exit - Save login credential and exit.\n" +
            "recid <ID> - Get the specified music record online.\n" +
            "reclist - Get music record summaries online.\n" +
            "playerinfo - Get the player info online.\n" +
            "saveall - Save the new music records automatically.\n" +
            "saveid <ID> - Save music record with specified index.\n" +
            "localid <ID> - Show the local music record with specified LocalID.\n" +
            "localrecent <amount> - Show the latest local music record.";
    }
}
