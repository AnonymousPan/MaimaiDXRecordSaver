using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using log4net;

namespace MaimaiDXRecordSaver
{
    public class MusicList
    {
        private static ILog logger = LogManager.GetLogger("MusicList");
        public static MusicList Instance { get; private set; }

        public List<MusicListEntry> List { get; private set; }

        public MusicList()
        {
            List = new List<MusicListEntry>();
        }

        public static bool Init()
        {
            try
            {
                Instance = new MusicList();
                string cachePath = "MusicList.json";
                if (File.Exists(cachePath))
                {
                    logger.Info("Loading music list from local cache.");
                    string json = File.ReadAllText(cachePath);
                    Instance.LoadFromJson(json);
                }
                else
                {
                    logger.Info("Loading music list from DivingFish API.");
                    Instance.LoadFromDivingFishApi(true, cachePath);
                }
                logger.Info(string.Format("Loaded {0} music entries.", Instance.List.Count));
                return true;
            }
            catch(Exception err)
            {
                logger.Error("Can not load music list.");
                logger.Error(err.ToString());
                return false;
            }
        }

        public void Clear()
        {
            List.Clear();
        }

        public void LoadFromJson(string json)
        {
            JArray arr = JArray.Parse(json);
            foreach(JObject obj in arr.Children())
            {
                MusicListEntry entry = new MusicListEntry();
                entry.ID = int.Parse(obj["id"].ToString());
                entry.Title = obj["title"].ToString();
                entry.IsDXLevel = obj["type"].ToString() == "DX";
                List<JToken> innerLevels = obj["ds"].ToList();
                for(int i = 0; i < innerLevels.Count; i++ )
                {
                    entry.InnerLevels[i] = innerLevels[i].ToObject<float>();
                }
                entry.BPM = obj["basic_info"]["bpm"].ToObject<float>();
                entry.IsNewSong = obj["basic_info"]["is_new"].ToObject<bool>();
                List.Add(entry);
            }
        }

        public void LoadFromDivingFishApi(bool saveCache, string cachePath)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("https://www.diving-fish.com/api/maimaidxprober/music_data");
            req.Method = "GET";
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
            {
                string json = reader.ReadToEnd();
                LoadFromJson(json);
                if(saveCache)
                {
                    File.WriteAllText(cachePath, json);
                }
            }
        }

        public string GetTitleByID(int id)
        {
            foreach (MusicListEntry entry in List)
            {
                if (entry.ID == id) return entry.Title;
            }
            return "<Unknown Music>";
        }

        public int MatchMusic(string title, bool isDXLevel)
        {
            foreach (MusicListEntry entry in List)
            {
                if(entry.Title == title && entry.IsDXLevel == isDXLevel)
                {
                    return entry.ID;
                }
            }
            return -1;
        }

        public bool IsDXLevel(int id)
        {
            foreach (MusicListEntry entry in List)
            {
                if (entry.ID == id) return entry.IsDXLevel;
            }
            return false;
        }

        public class MusicListEntry
        {
            public string Title { get; set; }
            public int ID { get; set; }
            public bool IsDXLevel { get; set; }
            public float[] InnerLevels { get; set; } = new float[5];
            public float BPM { get; set; }
            public bool IsNewSong { get; set; }
        }
    }
}
