using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using log4net;

namespace MaimaiDXRecordSaver
{
    public class DataRecorderFile : DataRecorderBase
    {
        private string SavePath;
        private string LastIDPath;
        private ILog logger = LogManager.GetLogger("DataRecorderFile");

        public override bool Init()
        {
            SavePath = Path.Combine(Environment.CurrentDirectory, "MusicRecords/");
            LastIDPath = Path.Combine(SavePath, "LastRecordID");
            try
            {
                logger.Info("Initializing Data Recorder (File)");
                if(!Directory.Exists(SavePath))
                {
                    Directory.CreateDirectory(SavePath);
                }
                if(!File.Exists(LastIDPath))
                {
                    File.WriteAllText(LastIDPath, "-1");
                }
                return true;
            }
            catch(Exception err)
            {
                logger.Error("Can not initialize Data Recorder (File)");
                logger.Error(err.ToString());
                return false;
            }
        }

        public override int GetLastRecordID()
        {
            return int.Parse(File.ReadAllText(LastIDPath));
        }

        public override MusicRecord GetMusicRecord(int id)
        {
            if(id >= 0)
            {
                string path = Path.Combine(SavePath, id.ToString() + ".json");
                string json = File.ReadAllText(path);
                return JSONToMusicRecord(json);
            }
            else
            {
                return null;
            }
        }

        public override int SaveMusicRecord(MusicRecord rec)
        {
            try
            {
                int newID = GetLastRecordID() + 1;
                File.WriteAllText(LastIDPath, newID.ToString());
                File.WriteAllText(Path.Combine(SavePath, newID.ToString() + ".json"), MusicRecordToJSON(rec));
                return newID;
            }
            catch(Exception err)
            {
                logger.Warn("Can not save MusicRecord");
                logger.Warn(err.ToString());
                return -1;
            }
        }

        public override bool IsRecordExists(int id)
        {
            return File.Exists(Path.Combine(SavePath, id.ToString() + ".json"));
        }

        private string MusicRecordToJSON(MusicRecord rec)
        {
            return JsonConvert.SerializeObject(rec);
        }

        private MusicRecord JSONToMusicRecord(string json)
        {
            MusicRecord rec = JsonConvert.DeserializeObject<MusicRecord>(json);
            rec.MusicIsDXLevel = MusicList.Instance.IsDXLevel(rec.MusicID);
            return rec;
        }

        /*
        private class MusicRecordStorObj
        {
            public MusicRecord MusicRecord { get; set; }
            public int[,] JudgementTable { get; set; }

            public MusicRecordStorObj() { }
            public MusicRecordStorObj(MusicRecord rec)
            {
                MusicRecord = rec;
                MusicRecord.Judgements = null;
                JudgementTable = rec.Judgements.Table;
            }

            public MusicRecord ToMusicRecord()
            {
                MusicRecord rec = MusicRecord;
                rec.Judgements = new JudgementTable(JudgementTable);
                return rec;
            }
        }
        */
    }
}
