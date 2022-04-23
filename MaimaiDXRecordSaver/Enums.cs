using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MaimaiDXRecordSaver
{
    public enum Difficulty
    {
        Basic,
        Advanced,
        Expert,
        Master,
        ReMaster,
        Unknown = -1
    }

    public static class DifficultyEnum
    {
        private static string[] names = {
            "Basic", "Advanced", "Expert", "Master", "Re:Master"
        };

        public static string GetName(this Difficulty self)
        {
            return self != Difficulty.Unknown ? names[(int)self] : "Unknown";
        }

        private static string[] diffTable = { "diff_basic", "diff_advanced", "diff_expert", "diff_master", "diff_remaster" };
        public static Difficulty ImageToDifficulty(string imageUrl)
        {
            for (int i = 0; i < diffTable.Length; i++)
            {
                if (imageUrl.Contains(diffTable[i]))
                {
                    return (Difficulty)i;
                }
            }
            return Difficulty.Unknown;
        }
    }

    public enum NoteType
    {
        Tap,
        Hold,
        Slide,
        Touch,
        Break,
        Unknon = -1
    }

    public static class NoteTypeEnum
    {
        private static string[] names = {
            "Tap", "Hold", "Slide", "Touch", "Break"
        };

        public static string GetName(this NoteType self)
        {
            return self != NoteType.Unknon ? names[(int)self] : "Unknown";
        }
    }

    public enum JudgementType
    {
        CriticalPerfect,
        Perfect,
        Great,
        Good,
        Miss,
        Unknown = -1
    }

    public enum LevelRating
    {
        D,
        C,
        B,
        BB,
        BBB,
        A,
        AA,
        AAA,
        S,
        SPlus,
        SS,
        SSPlus,
        SSS,
        SSSPlus,
        Unknown = -1
    }

    public static class LevelRatingEnum
    {
        public static LevelRating AchievementToRating(int achievement)
        {
            if (achievement < 0) return LevelRating.Unknown;
            else if (achievement <= 400000) return LevelRating.D;
            else if (achievement <= 600000) return LevelRating.C;
            else if (achievement <= 700000) return LevelRating.B;
            else if (achievement <= 750000) return LevelRating.BB;
            else if (achievement <= 800000) return LevelRating.BBB;
            else if (achievement <= 900000) return LevelRating.A;
            else if (achievement <= 940000) return LevelRating.AA;
            else if (achievement <= 970000) return LevelRating.AAA;
            else if (achievement <= 980000) return LevelRating.S;
            else if (achievement <= 990000) return LevelRating.SPlus;
            else if (achievement <= 995000) return LevelRating.SS;
            else if (achievement <= 1000000) return LevelRating.SSPlus;
            else if (achievement <= 1005000) return LevelRating.SSS;
            else if (achievement <= 1010000) return LevelRating.SSSPlus;
            else return LevelRating.Unknown;
        }

        /*
        public static LevelRating AchievementToRating(float achievement)
        {
            return AchievementToRating(achievement * 1000);
        }
        */

        private static string[] names = { 
            "D", "C", "B", "BB", "BBB", "A", "AA", "AAA",
            "S", "S+", "SS", "SS+", "SSS", "SSS+"
        };
        public static string GetName(this LevelRating self)
        {
            return self != LevelRating.Unknown ? names[(int)self] : "Unknown";
        }
    }

    public enum MatchLevel
    {
        MatchLevel_0,
        MatchLevel_1,
        MatchLevel_2,
        MatchLevel_3,
        MatchLevel_4,
        MatchLevel_5,
        MatchLevel_6,
        MatchLevel_7,
        MatchLevel_8,
        MatchLevel_9,
        MatchLevel_10,
        MatchLevel_11,
        MatchLevel_12,
        MatchLevel_13,
        MatchLevel_14,
        MatchLevel_15,
        MatchLevel_16,
        MatchLevel_17,
        MatchLevel_18,
        MatchLevel_19,
        MatchLevel_20,
        MatchLevel_21,
        MatchLevel_22,
        MatchLevel_23,
        MatchLevel_24,
        Unknown = -1
    }

    public static class MatchLevelEnum
    {
        private static string[] names = {
            "初学者", "实习生", "初出茅庐", "修行中", "初段",
            "二段", "三段", "四段", "五段", "六段",
            "七段", "八段", "九段", "十段", "真传",
            "真传壹段", "真传贰段", "真传叁段", "真传肆段", "真传伍段",
            "真传陆段", "真传柒段", "真传扒段", "真传玖段", "真传拾段"
        };

        private static int[] scores = {
            0, 250, 500, 750, 1000,
            1200, 1400, 1500, 1600, 1700,
            1800, 1850, 1900, 1950, 2000,
            2010, 2020, 2030, 2040, 2050,
            2060, 2070, 2080, 2090, 2100
        };

        public static string GetName(this MatchLevel self)
        {
            return self != MatchLevel.Unknown ? names[(int)self] : "Unknown";
        }

        public static int GetScore(this MatchLevel self)
        {
            return  self != MatchLevel.Unknown ? scores[(int)self] : 0;
        }

        // TODO: Complete this table!
        // It is useless now, but I want to keep it (?
        private static string[] matchLevelTable = {
            "", "", "03w7JvyxjH", "", "",
            "", "", "08xS8aqrYG", "09sA8D6X7e", "",
            "", "", "", "", "",
            "", "", "", "", "",
            "", "", "", "", "",
        };

        public static MatchLevel GetMatchLevelFromIconUrl(string iconUrl)
        {
            Regex regex = new Regex("grade_[\\w]+");
            string str = regex.Match(iconUrl).Value.Substring(6, 2);
            int result = -1;
            if(!int.TryParse(str, out result))
            {
                return MatchLevel.Unknown;
            }
            else
            {
                result -= 1;
                return (MatchLevel)(result > 24 ? -1 : result);
            }
        }
    }

    public enum ComboIcon
    {
        None,
        AllPerfectPlus,
        AllPerfect,
        FullComboPlus,
        FullCombo,
        Unknown = -1
    }

    public static class ComboIconEnum
    {
        private static string[] names = {
            "-", "AP+", "AP", "FC+", "FC"
        };

        public static string GetName(this ComboIcon self)
        {
            return self != ComboIcon.Unknown ? names[(int)self] : "Unknown";
        }
    }

    public enum SyncIcon
    {
        None,
        FullSyncDXPlus,
        FullSyncDX,
        FullSyncPlus,
        FullSync,
        Unknown = -1
    }

    public static class SyncIconEnum
    {
        private static string[] names = {
            "-", "FDX+", "FDX", "FS+", "FS"
        };

        public static string GetName(this SyncIcon self)
        {
            return self != SyncIcon.Unknown ? names[(int)self] : "Unknown";
        }
    }

    public enum RatingChange
    {
        None,
        Increased,
        Decreased,
        Unknown = -1
    }

    public static class RatingChangeEnum
    {
        private static string[] names = {
            "=", "↑", "↓"
        };

        public static string GetName(this RatingChange self)
        {
            return self != RatingChange.Unknown ? names[(int)self] : "?";
        }
    }

    public enum RecordSaveMethod
    {
        File,
        SQLServer,
        Unknown = -1
    }
}
