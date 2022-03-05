using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class MusicRecordSummary
    {
        public string MusicTitle { get; set; }
        public Difficulty MusicDifficulty { get; set; }
        public bool MusicIsDXLevel { get; set; }
        public int Achievement { get; set; }
        public byte TrackNumber { get; set; }
        public DateTime PlayTime { get; set; }
        public int Index { get; set; }

        public MusicRecordSummary() { }

        public MusicRecordSummary(MusicRecord rec)
        {
            MusicTitle = rec.MusicTitle;
            MusicDifficulty = rec.MusicDifficulty;
            MusicIsDXLevel = rec.MusicIsDXLevel;
            Achievement = rec.Achievement;
            TrackNumber = rec.TrackNumber;
            PlayTime = rec.PlayTime;
            Index = -1;
        }

        public override bool Equals(object obj)
        {
            if(obj != null)
            {
                bool flag = true;
                if(obj is MusicRecordSummary)
                {
                    MusicRecordSummary target = (MusicRecordSummary)obj;
                    flag &= target.MusicTitle == MusicTitle;
                    flag &= target.MusicDifficulty == MusicDifficulty;
                    flag &= target.MusicIsDXLevel == MusicIsDXLevel;
                    flag &= target.Achievement == Achievement;
                    flag &= target.TrackNumber == TrackNumber;
                    flag &= target.PlayTime == PlayTime;
                }
                else if(obj is MusicRecord)
                {
                    MusicRecord target = (MusicRecord)obj;
                    flag &= target.MusicTitle == MusicTitle;
                    flag &= target.MusicDifficulty == MusicDifficulty;
                    flag &= target.MusicIsDXLevel == MusicIsDXLevel;
                    flag &= target.Achievement == Achievement;
                    flag &= target.TrackNumber == TrackNumber;
                    flag &= target.PlayTime == PlayTime;
                }
                else
                {
                    flag = false;
                }
                return flag;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            string str = string.Format("[Summary] {0} ({1},{2}Level) {3}% {4}\n",
                MusicTitle, MusicDifficulty.GetName(), MusicIsDXLevel ? "DX" : "SD",
                Achievement / 10000.0f, LevelRatingEnum.AchievementToRating(Achievement).GetName());
            str += string.Format("Track{0} {1} (Index={2})\n", TrackNumber, PlayTime, Index);
            return str;
        }
    }
}
