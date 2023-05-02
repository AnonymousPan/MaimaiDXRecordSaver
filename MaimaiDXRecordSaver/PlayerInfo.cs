using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class PlayerInfo
    {
        public string Name { get; set; }
        public int Rating { get; set; }
        public int MaxRating { get; set; }
        public MatchLevel Level { get; set; }
        public int Stars { get; set; }
        public int PlayCount { get; set; }
        public int SSSPlus { get; set; }
        public int SSS { get; set; }
        public int SSPlus { get; set; }
        public int SS { get; set; }
        public int SPlus { get; set; }
        public int S { get; set; }
        public int Clear { get; set; }
        public int AllPerfectPlus { get; set; }
        public int AllPerfect { get; set; }
        public int FullComboPlus { get; set; }
        public int FullCombo { get; set; }
        public int FullSyncDXPlus { get; set; }
        public int FullSyncDX { get; set; }
        public int FullSyncPlus { get; set; }
        public int FullSync { get; set; }

        public override string ToString()
        {
            string str = "========玩家信息========\n";
            str += string.Format("Name: {0}\tDX Rating: {1} {2}\n", Name, Rating, Level.GetName());
            str += string.Format("Max Rating: {0}\t☆x{1}\tPlay Count: {2}\n", MaxRating, Stars, PlayCount);
            str += string.Format("SSS+ {0}\tSSS {1}\tSS+ {2}\tSS {3}\n", new object[] { SSSPlus, SSS, SSPlus, SS });
            str += string.Format("S+ {0}\tS {1}\tClear {2}\n", SPlus, S, Clear);
            str += string.Format("AP+ {0}\tAP {1}\tFC+ {2}\tFC {3}\n", new object[] { AllPerfectPlus, AllPerfect, FullComboPlus, FullCombo });
            str += string.Format("FDX+ {0}\tFDX {1}\tFS+ {2}\tFS {3}\n", new object[] { FullSyncDXPlus, FullSyncDX, FullSyncPlus, FullSync });
            str += "================\n";
            return str;
        }
    }
}
