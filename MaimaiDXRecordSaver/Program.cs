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
                Logger.Info("========Starting Up========");

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
                        Logger.Info("Offline Mode enabled because -offline appeared in arguments.");
                        break;
                    }
                }
                
                // Load credential
                string sessionID, _t;
                if (LoadCredential(out sessionID, out _t))
                {
                    Logger.Info("Load credential OK.");
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
                Logger.Fatal("Unhandled Exception!");
                Logger.Fatal(err.ToString());
                Console.WriteLine("Application crashed, press any key to continue :(");
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
