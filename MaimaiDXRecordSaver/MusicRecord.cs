using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MaimaiDXRecordSaver
{
    public class MusicRecord
    {
        public int MusicID { get; set; }
        [JsonIgnore]
        public bool MusicIsDXLevel { get; set; }
        [JsonIgnore]
        public string MusicTitle { get { return MusicList.Instance.GetTitleByID(MusicID); } }
        public Difficulty MusicDifficulty { get; set; }
        public DateTime PlayTime { get; set; }
        public byte TrackNumber { get; set; }
        public ComboIcon ComboIcon { get; set; }
        public SyncIcon SyncIcon { get; set; }
        public byte Ranking { get; set; }
        public MatchingResult MatchingInfo { get; set; }
        public int Achievement { get; set; }
        public bool AchievementNewRecord { get; set; }
        [JsonIgnore]
        public bool Clear { get { return Achievement >= 800000; } }
        [JsonIgnore]
        public LevelRating Rating { get { return LevelRatingEnum.AchievementToRating(Achievement); } }
        public int DXScore { get; set; }
        public bool DXScoreNewRecord { get; set; }
        public CharacterInfo[] Characters { get; set; } = new CharacterInfo[5];
        public int FastCount { get; set; }
        public int LateCount { get; set; }
        public JudgementTable Judgements { get; set; }
        public int MatchLevelRating { get; set; } // 段位分
        public int BaseRating { get; set; } // 底分
        [JsonIgnore]
        public int NewRating { get { return MatchLevelRating + BaseRating; } }
        public int MatchLevelRatingChange { get; set; }
        public MatchLevel NewMatchLevel { get; set; }
        public RatingChange RatingChange { get; set; }
        public int Combo { get; set; }
        public int MaxCombo { get; set; }
        public int Sync { get; set; }
        public int MaxSync { get; set; }
        public string[] FriendNames { get; set; } = new string[3];
        public Difficulty[] FriendDifficulties { get; set; } = new Difficulty[3];

        public override string ToString()
        {
            string str = "========乐曲记录========\n";
            str += string.Format("{0}. {1}({2},{3}谱面)\n", MusicID, MusicTitle, MusicDifficulty.GetName(), MusicIsDXLevel ? "DX" : "标准");
            str += string.Format("Track{0} {1} Ranking:{2}\n", TrackNumber, PlayTime, Ranking);
            str += string.Format("Achievement: {0}% {1} {2}\n", Achievement / 10000.0d, Rating.GetName(), AchievementNewRecord ? "(新纪录)" : "");
            str += string.Format("DX分数: {0} {1}\n", DXScore, DXScoreNewRecord ? "(新纪录)" : "");
            str += string.Format("Combo: {0}/{1}({2}) Sync:{3}/{4}({5})\n", Combo, MaxCombo, ComboIcon.GetName(), Sync, MaxSync, SyncIcon.GetName());
            str += "判定信息:\n" + Judgements.ToString();
            str += string.Format("Fast: {0} Late: {1}\n", FastCount, LateCount);
            str += string.Format("DX Rating: {0}(底分) + {1}(段位分) = {2}({3})\n", BaseRating, MatchLevelRating, NewRating, RatingChange.GetName());
            str += string.Format("段位分变化: {0} 当前段位: {1}\n", MatchLevelRatingChange, NewMatchLevel.GetName());
            str += "旅行伙伴:\n";
            for(int i = 0; i < 5; i++ )
            {
                if(Characters[i] == null)
                {
                    str += string.Format("[{0}] --\n", i + 1);
                }
                else
                {
                    CharacterInfo info = Characters[i];
                    str += string.Format("[{0}] ID:{1} ☆x{2} Level.{3}\n", i + 1, info.CharacterID, info.Stars, info.Level);
                }
            }
            str += "拼机玩家:\n";
            for(int i = 0; i < 3; i++ )
            {
                if(string.IsNullOrEmpty(FriendNames[i]))
                {
                    str += string.Format("[{0}] --\n", i + 1);
                }
                else
                {
                    str += string.Format("[{0}] {1}({2})\n", i + 1, FriendNames[i], FriendDifficulties[i].GetName());
                }
            }
            if(MatchingInfo == null)
            {
                str += "友人对战结果: N/A\n";
            }
            else
            {
                str += "友人对战结果:\n";
                str += MatchingInfo.ToString();
            }
            str += "============================\n";
            return str;
        }
    }
}
