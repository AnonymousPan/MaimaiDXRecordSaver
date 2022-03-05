using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using log4net;

namespace MaimaiDXRecordSaver
{
    public class ConfigManager
    {
        public static ConfigManager Instance { get; private set; }
        private static ILog logger = LogManager.GetLogger("ConfigManager");

        public int AutoSaveInterval { get; set; }
        public RecordSaveMethod RecordSaveMethod { get; set; }
        public string DBServer { get; set; }
        public string DBName { get; set; }
        public string DBUsername { get; set; }
        public string DBPassword { get; set; }
        public bool DBUseWindowsAuth { get; set; }
        public bool WebPageProxyEnabled { get; set; }
        public string WebPageProxyIPBind { get; set; }
        public int WebPageProxyPort { get; set; }

        public ConfigManager() { }

        public static bool Init()
        {
            Instance = new ConfigManager();
            logger.Info("Config Manager Initializing.");
            Instance.LoadConfig();
            return true;
        }

        public void LoadConfig()
        {
            AutoSaveInterval = int.Parse(ConfigurationManager.AppSettings["AutoSaveInterval"]);
            RecordSaveMethod = (RecordSaveMethod)int.Parse(ConfigurationManager.AppSettings["RecordSaveMethod"]);
            DBServer = ConfigurationManager.AppSettings["DBServer"];
            DBName = ConfigurationManager.AppSettings["DBName"];
            DBUsername = ConfigurationManager.AppSettings["DBUsername"];
            DBPassword = ConfigurationManager.AppSettings["DBPassword"];
            DBUseWindowsAuth = bool.Parse(ConfigurationManager.AppSettings["DBUseWindowsAuth"]);
            WebPageProxyEnabled = bool.Parse(ConfigurationManager.AppSettings["WebProxyEnabled"]);
            WebPageProxyIPBind = ConfigurationManager.AppSettings["WebPageProxyIPBind"];
            WebPageProxyPort = int.Parse(ConfigurationManager.AppSettings["WebPageProxyPort"]);
        }

        /*
        public void SaveConfig()
        {
            ConfigurationManager.AppSettings["AutoSaveInterval"] = AutoSaveInterval.ToString();
            ConfigurationManager.AppSettings["RecordSaveMethod"] = ((int)RecordSaveMethod).ToString();
            ConfigurationManager.AppSettings["DBServer"] = DBServer;
            ConfigurationManager.AppSettings["DBName"] = DBName;
            ConfigurationManager.AppSettings["DBUserame"] = DBUsername;
            ConfigurationManager.AppSettings["DBPassword"] = DBPassword;
            ConfigurationManager.AppSettings["DBUseWindowsAuth"] = DBUseWindowsAuth.ToString();
            ConfigurationManager.AppSettings["WebPageProxyEnabled"] = WebPageProxyEnabled.ToString();
            ConfigurationManager.AppSettings["WebPageProxyIPBind"] = WebPageProxyIPBind;
            ConfigurationManager.AppSettings["WebPageProxyPort"] = WebPageProxyPort.ToString();
            logger.Info("Config updated.");
        }

        public bool ConfigExists()
        {
            return ConfigurationManager.AppSettings.AllKeys.Contains("ConfigOK");
        }

        public void UpdateAutosaveSetting()
        {
            Console.WriteLine("[Autosave Setting]");
            AutoSaveInterval = EnterInt("Autosave Interval(in minutes, 0 means never)");
            SaveConfig();
        }

        public void UpdateRecordSaveSetting()
        {
            Console.WriteLine("[Record Save Settings]");
            Console.WriteLine("0 - Save records using file system.");
            Console.WriteLine("1 - Save records using SQL Server database.");
            RecordSaveMethod = (RecordSaveMethod)EnterIntWithRange("Record Save Method", 0, 1);
            switch(RecordSaveMethod)
            {
                case RecordSaveMethod.File:
                    break;
                case RecordSaveMethod.SQLServer:
                    DBServer = EnterString("Database Server");
                    DBName = EnterString("Database Name");
                    DBUseWindowsAuth = EnterBool("Use windows authentication?");
                    if(!DBUseWindowsAuth)
                    {
                        DBUsername = EnterString("Database Username");
                        DBPassword = EnterString("Database Password");
                    }
                    break;
            }
            SaveConfig();
        }

        public void UpdateWebPageProxySetting()
        {
            Console.WriteLine("[Webpage Proxy Settings]");
            WebPageProxyEnabled = EnterBool("Enable webpage proxy?");
            if(WebPageProxyEnabled)
            {
                WebPageProxyIPBind = EnterString("IP Bind");
                WebPageProxyPort = EnterIntWithRange("Port", 1, 65535);
            }
            SaveConfig();
        }

        private string EnterString(string tip)
        {
            Console.Write(tip + ": ");
            return Console.ReadLine();
        }

        private int EnterInt(string tip)
        {
            int val = 0;
            string input = "";
            do
            {
                Console.Write(tip + "(Integer): ");
                input = Console.ReadLine();
            }
            while (!int.TryParse(input, out val));
            return val;
        }

        private int EnterIntWithRange(string tip, int min, int max)
        {
            int val = 0;
            do
            {
                val = EnterInt(tip + string.Format("({0}..{1})", min, max));
            }
            while (val < min || val > max);
            return val;
        }

        private bool EnterBool(string tip)
        {
            bool val = false;
            string input = "";
            do
            {
                Console.Write(tip + "(true/false): ");
                input = Console.ReadLine();
            }
            while (!bool.TryParse(input, out val));
            return val;
        }
        */
    }
}
