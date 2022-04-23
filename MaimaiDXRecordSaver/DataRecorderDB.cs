using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using log4net;

namespace MaimaiDXRecordSaver
{
    public class DataRecorderDB : DataRecorderBase
    {
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public bool UseWindowsAuth { get; set; }

        private ILog logger = LogManager.GetLogger("DataRecorderDB");
        private SqlConnection connection;
        private SqlCommandHelper cmdHelper;

        public override bool Init()
        {
            try
            {
                connection = new SqlConnection(GetConnectString());
                connection.Open();
                cmdHelper = new SqlCommandHelper(connection, File.ReadAllText("SqlCommands.txt"));
                if(!cmdHelper.IsTableExists("MusicRecords"))
                {
                    logger.Warn("Table MusicRecords not exists, creating.");
                    cmdHelper.ExecuteNonQuery("CreateTable_MusicRecords");
                }
                if (!cmdHelper.IsTableExists("Judgements"))
                {
                    logger.Warn("Table Judgements not exists, creating.");
                    cmdHelper.ExecuteNonQuery("CreateTable_Judgements");
                }
                if (!cmdHelper.IsTableExists("Characters"))
                {
                    logger.Warn("Table Characters not exists, creating.");
                    cmdHelper.ExecuteNonQuery("CreateTable_Characters");
                }
                if (!cmdHelper.IsTableExists("Friends"))
                {
                    logger.Warn("Table Friends not exists, creating.");
                    cmdHelper.ExecuteNonQuery("CreateTable_Friends");
                }
                if (!cmdHelper.IsTableExists("MatchingResults"))
                {
                    logger.Warn("Table MatchingResults not exists, creating.");
                    cmdHelper.ExecuteNonQuery("CreateTable_MatchingResults");
                }

                return true;
            }
            catch(Exception err)
            {
                logger.Error("Can not initialize Data Recorder (Database)");
                logger.Error(err.ToString());
                return false;
            }
        }

        public override int GetLastRecordID()
        {
            object result = cmdHelper.ExecuteScalar("GetLastRecordID");
            return result is DBNull ? -1 : (int)result;
        }

        public override MusicRecord GetMusicRecord(int id)
        {
            if(id >= 0)
            {
                MusicRecord rec = new MusicRecord();
                Dictionary<string, Difficulty> friendsInfo = ReadFriendsInfo(id);
                friendsInfo.Keys.CopyTo(rec.FriendNames, 0);
                friendsInfo.Values.CopyTo(rec.FriendDifficulties, 0);
                ReadBasicInfo(id, rec, friendsInfo.Values.ToArray());
                rec.Judgements = ReadJudgementsInfo(id);
                rec.Characters = ReadCharactersInfo(id);
                rec.MatchingInfo = ReadMatchingInfo(id);
                return rec;
            }
            else
            {
                return null;
            }
        }

        public override bool IsRecordExists(int id)
        {
            object result = cmdHelper.ExecuteScalarT("IsRecordExists", new (string, object)[] { ("RecordID", id) });
            return result != null;
        }

        public override int SaveMusicRecord(MusicRecord rec)
        {
            try
            {
                int id = GetLastRecordID() + 1;
                WriteBasicInfo(id, rec);
                WriteJudgementsInfo(id, rec.Judgements);
                WriteCharactersInfo(id, rec.Characters);
                WriteFriendsInfo(id, rec.FriendNames, rec.FriendDifficulties);
                WriteMatchingInfo(id, rec.MatchingInfo);
                return id;
            }
            catch(Exception err)
            {
                logger.Warn("Can not save MusicRecord");
                logger.Warn(err.ToString());
                return -1;
            }
        }

        private string GetConnectString()
        {
            if(UseWindowsAuth)
            {
                return string.Format("Server={0};Database={1};Integrated Security=True",
                    Server, Database);
            }
            else
            {
                return string.Format("Server={0};Database={1};User Id={2};Password={3}",
                    Server, Database, Username, Password);
            }
        }

        private int GetMaxSync(int id, int selfDifficulty, Difficulty[] friendDifficulties)
        {
            if(friendDifficulties.Length == 0)
            {
                return -1;
            }
            else if(friendDifficulties[0] == Difficulty.Unknown)
            {
                return -1;
            }
            MusicList.MusicListEntry musicObj = MusicList.Instance.GetMusicEntry(id);
            int result = musicObj.NoteCounts[selfDifficulty];
            foreach(int i in friendDifficulties)
            {
                result += musicObj.NoteCounts[i];
            }
            return result;
        }

        #region SQL read and write functions

        private int ReadInt(SqlDataReader reader, int i)
        {
            object obj = reader[i];
            Type typ = obj.GetType();
            return obj is DBNull ? -1 : Convert.ToInt32(obj);
        }

        private byte ReadByte(SqlDataReader reader, int i)
        {
            object obj = reader[i];
            return obj is DBNull ? (byte)0 : Convert.ToByte(obj);
        }

        private bool ReadBoolean(SqlDataReader reader, int i)
        {
            object obj = reader[i];
            return obj is DBNull ? false : Convert.ToBoolean(obj);
        }

        private string ReadString(SqlDataReader reader, int i)
        {
            object obj = reader[i];
            return obj is DBNull ? null : Convert.ToString(obj);
        }

        private DateTime ReadDateTime(SqlDataReader reader, int i)
        {
            object obj = reader[i];
            return obj is DBNull ? DateTime.MinValue : Convert.ToDateTime(obj);
        }

        private object WriteInt(int value)
        {
            return value < 0 ? DBNull.Value : (object)value;
        }

        private object WriteString(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)value;
        }

        #endregion

        private void ReadBasicInfo(int id, MusicRecord rec, Difficulty[] friendDifficulties)
        {
            SqlDataReader reader = cmdHelper.ExecuteReaderT("ReadBasicInfo", new (string, object)[] { ("RecordID", id) });
            reader.Read();
            int i = 0;
            int musicID = ReadInt(reader, ++i);
            rec.MusicID = musicID;
            rec.MusicIsDXLevel = MusicList.Instance.IsDXLevel(musicID);
            int difficulty = ReadInt(reader, ++i);
            rec.MusicDifficulty = (Difficulty)difficulty;
            rec.TrackNumber = ReadByte(reader, ++i);
            rec.PlayTime = ReadDateTime(reader, ++i);
            rec.Ranking = ReadByte(reader, ++i);
            rec.Achievement = ReadInt(reader, ++i);
            rec.AchievementNewRecord = ReadBoolean(reader, ++i);
            rec.DXScore = ReadInt(reader, ++i);
            rec.DXScoreNewRecord = ReadBoolean(reader, ++i);
            rec.Combo = ReadInt(reader, ++i);
            rec.ComboIcon = (ComboIcon)ReadInt(reader, ++i);
            rec.MaxCombo = MusicList.Instance.GetMusicEntry(musicID).NoteCounts[difficulty];
            rec.Sync = ReadInt(reader, ++i);
            rec.SyncIcon = (SyncIcon)ReadInt(reader, ++i);
            rec.MaxSync = GetMaxSync(musicID, difficulty, friendDifficulties);
            rec.FastCount = ReadInt(reader, ++i);
            rec.LateCount = ReadInt(reader, ++i);
            rec.BaseRating = ReadInt(reader, ++i);
            rec.MatchLevelRating = ReadInt(reader, ++i);
            rec.MatchLevelRatingChange = ReadInt(reader, ++i);
            rec.NewMatchLevel = (MatchLevel)ReadInt(reader, ++i);
            rec.RatingChange = (RatingChange)ReadInt(reader, ++i);
            reader.Close();
        }

        private JudgementTable ReadJudgementsInfo(int id)
        {
            int[,] table = new int[5, 5];
            for(int i = 0; i < 25; i++ )
            {
                table[i / 5, i % 5] = -1;
            }
            SqlDataReader reader = cmdHelper.ExecuteReaderT("ReadJudgementsInfo", new (string, object)[] { ("RecordID", id) });
            while(reader.Read())
            {
                int noteType = ReadInt(reader, 1);
                for(int i = 0; i < 5; i++ )
                {
                    table[noteType, i] = ReadInt(reader, i + 2);
                }
            }
            reader.Close();
            return new JudgementTable(table);
        }

        private CharacterInfo[] ReadCharactersInfo(int id)
        {
            List<CharacterInfo> list = new List<CharacterInfo>();
            SqlDataReader reader = cmdHelper.ExecuteReaderT("ReadCharactersInfo", new (string, object)[] { ("RecordID", id) });
            while(reader.Read())
            {
                CharacterInfo info = new CharacterInfo();
                info.CharacterID = ReadString(reader, 2);
                info.Stars = ReadInt(reader, 3);
                info.Level = ReadInt(reader, 4);
                list.Add(info);
            }
            reader.Close();
            for(int i = list.Count; i < 5; i++ )
            {
                list.Add(null);
            }
            return list.ToArray();
        }

        private Dictionary<string, Difficulty> ReadFriendsInfo(int id)
        {
            Dictionary<string, Difficulty> dict = new Dictionary<string, Difficulty>();
            SqlDataReader reader = cmdHelper.ExecuteReaderT("ReadFriendsInfo", new (string, object)[] { ("RecordID", id) });
            while(reader.Read())
            {
                string name = ReadString(reader, 2);
                Difficulty diff = (Difficulty)ReadInt(reader, 3);
                dict.Add(name, diff);
            }
            reader.Close();
            return dict;
        }

        private MatchingResult ReadMatchingInfo(int id)
        {
            SqlDataReader reader = cmdHelper.ExecuteReaderT("ReadMatchingInfo", new (string, object)[] { ("RecordID", id) });
            MatchingResult result = null;
            if(reader.Read())
            {
                result = new MatchingResult();
                result.PlayerName = ReadString(reader, 1);
                result.DXRating = ReadInt(reader, 2);
                result.MatchLevel = (MatchLevel)ReadInt(reader, 3);
                result.Achievement = ReadInt(reader, 4);
                result.Won = ReadInt(reader, 5) != 0;
            }
            reader.Close();
            return result;
        }

        private void WriteBasicInfo(int id, MusicRecord rec)
        {
            Dictionary<string, object> param = new Dictionary<string, object>();
            param.Add("RecordID", id);
            param.Add("MusicID", WriteInt(rec.MusicID));
            param.Add("Difficulty", WriteInt((int)rec.MusicDifficulty));
            param.Add("TrackNo", rec.TrackNumber);
            param.Add("PlayTime", rec.PlayTime);
            param.Add("Ranking", rec.Ranking);
            param.Add("Achievement", WriteInt(rec.Achievement));
            param.Add("AchievementNewRecord", rec.AchievementNewRecord);
            param.Add("DXScore", WriteInt(rec.DXScore));
            param.Add("DXScoreNewRecord", rec.DXScoreNewRecord);
            param.Add("Combo", WriteInt(rec.Combo));
            param.Add("ComboIcon", WriteInt((int)rec.ComboIcon));
            param.Add("Sync", WriteInt(rec.Sync));
            param.Add("SyncIcon", WriteInt((int)rec.SyncIcon));
            param.Add("Fast", WriteInt(rec.FastCount));
            param.Add("Late", WriteInt(rec.LateCount));
            param.Add("BaseRating", WriteInt(rec.BaseRating));
            param.Add("MatchLevelRating", WriteInt(rec.MatchLevelRating));
            param.Add("MatchLevelChange", WriteInt(rec.MatchLevelRatingChange));
            param.Add("CurrentMatchLevel", WriteInt((int)rec.NewMatchLevel));
            param.Add("RatingChange", WriteInt((int)rec.RatingChange));
            cmdHelper.ExecuteNonQuery("WriteBasicInfo", param);
        }

        private void WriteJudgementsInfo(int id, JudgementTable jt)
        {
            int[,] table = jt.Table;
            for(int i = 0; i < 5; i++ )
            {
                if(table[i, 0] != -1)
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("RecordID", id);
                    param.Add("NoteType", i);
                    param.Add("Critical", WriteInt(table[i, 0]));
                    param.Add("Perfect", WriteInt(table[i, 1]));
                    param.Add("Great", WriteInt(table[i, 2]));
                    param.Add("Good", WriteInt(table[i, 3]));
                    param.Add("Miss", WriteInt(table[i, 4]));
                    cmdHelper.ExecuteNonQuery("WriteJudgementsInfo", param);
                }
            }
        }

        private void WriteCharactersInfo(int id, CharacterInfo[] charas)
        {
            for(int i = 0; i < 5; i++ )
            {
                CharacterInfo chara = charas[i];
                if(chara != null)
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("RecordID", id);
                    param.Add("Slot", i);
                    param.Add("CharacterID", WriteString(chara.CharacterID));
                    param.Add("Star", WriteInt(chara.Stars));
                    param.Add("Level", WriteInt(chara.Level));
                    cmdHelper.ExecuteNonQuery("WriteCharactersInfo", param);
                }
            }
        }

        private void WriteFriendsInfo(int id, string[] names, Difficulty[] diffs)
        {
            for(int i = 0; i < 3; i++ )
            {
                if(!string.IsNullOrEmpty(names[i]))
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("RecordID", id);
                    param.Add("Slot", i);
                    param.Add("Name", WriteString(names[i]));
                    param.Add("Difficulty", WriteInt((int)diffs[i]));
                    cmdHelper.ExecuteNonQuery("WriteFriendsInfo", param);
                }
            }
        }

        private void WriteMatchingInfo(int id, MatchingResult mr)
        {
            if(mr != null)
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add("RecordID", id);
                param.Add("Name", WriteString(mr.PlayerName));
                param.Add("DXRating", WriteInt(mr.DXRating));
                param.Add("MatchLevel", WriteInt((int)mr.MatchLevel));
                param.Add("Achievement", WriteInt(mr.Achievement));
                param.Add("Result", mr.Won ? 1 : 0);
                cmdHelper.ExecuteNonQuery("WriteMatchingInfo", param);
            }
        }
    }
}
