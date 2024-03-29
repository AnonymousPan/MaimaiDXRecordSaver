﻿using System;
using System.Collections.Generic;
using System.IO;
using AnonymousPan.XmlConfig;

namespace MaimaiDXRecordSaver
{
    public class ConfigManager : SimpleConfigManagerBase
    {
        public static ConfigManager Instance { get; private set; }

        // General
        public ConfigEntryInt32 AutoSaveInterval { get; private set; }
        public ConfigEntryEnum<RecordSaveMethod> SaveMethod { get; private set; }

        // Database
        public ConfigEntryString DBServer { get; private set; }
        public ConfigEntryString DBName { get; private set; }
        public ConfigEntryString DBUsername { get; private set; }
        public ConfigEntryString DBPassword { get; private set; }
        public ConfigEntryBool DBUseWindowsAuth { get; private set; }

        // WebPageProxy
        public ConfigEntryBool WebPageProxyEnabled { get; private set; }
        public ConfigEntryString WebPageProxyIPBind { get; private set; }
        public ConfigEntryInt32 WebPageProxyPort { get; private set; }
        public ConfigEntryString WebPageProxyServerStr { get; private set; }
        public ConfigEntryBool WebPageProxyAllowLogout { get; private set; }
        public ConfigEntryBool WebPageProxyAllowChangeName { get; private set; }

        // WechatLoginProxy
        public ConfigEntryBool WechatLoginProxyEnabled { get; private set; }
        public ConfigEntryInt32 WechatLoginProxyPort { get; private set; }
        public ConfigEntryBool WechatLoginProxyUrlWhitelist { get; private set; }

        public ConfigManager(string path)
        {
            FilePath = path;
            if(File.Exists(path))
            {
                ConfigFile = new XmlConfigFile(File.ReadAllText(path));
            }
            else
            {
                ConfigFile = new XmlConfigFile();
                ConfigFile.Save(path);
            }
        }

        public override void Initialize()
        {
            // General
            AutoSaveInterval = new ConfigEntryInt32(ConfigFile, "General", "AutoSaveInterval", -1, null);
            SaveMethod = new ConfigEntryEnum<RecordSaveMethod>(ConfigFile, "General", "RecordSaveMethod", RecordSaveMethod.File, null);
            
            // Database
            DBServer = new ConfigEntryString(ConfigFile, "Database", "Server", "", null);
            DBName = new ConfigEntryString(ConfigFile, "Database", "Name", "", null);
            DBUsername = new ConfigEntryString(ConfigFile, "Database", "Username", "", null);
            DBPassword = new ConfigEntryString(ConfigFile, "Database", "Password", "", null);
            DBUseWindowsAuth = new ConfigEntryBool(ConfigFile, "Database", "UseWindowsAuth", false, null);

            // WebPageProxy
            WebPageProxyEnabled = new ConfigEntryBool(ConfigFile, "WebPageProxy", "Enabled", true, null);
            WebPageProxyIPBind = new ConfigEntryString(ConfigFile, "WebPageProxy", "IPBind", "127.0.0.1", null);
            WebPageProxyPort = new ConfigEntryInt32(ConfigFile, "WebPageProxy", "Port", 9999, null);
            WebPageProxyServerStr = new ConfigEntryString(ConfigFile, "WebPageProxy", "ServerHeader", "", null);
            WebPageProxyAllowLogout = new ConfigEntryBool(ConfigFile, "WebPageProxy", "AllowLogout", false, null);
            WebPageProxyAllowChangeName = new ConfigEntryBool(ConfigFile, "WebPageProxy", "AllowChangeName", false, null);

            // WechatLoginProxy
            WechatLoginProxyEnabled = new ConfigEntryBool(ConfigFile, "WechatLoginProxy", "Enabled", true, null);
            WechatLoginProxyPort = new ConfigEntryInt32(ConfigFile, "WechatLoginProxy", "Port", 9998, null);
            WechatLoginProxyUrlWhitelist = new ConfigEntryBool(ConfigFile, "WechatLoginProxy", "UrlWhitelist", true, null);

            Instance = this;
        }
    }
}
