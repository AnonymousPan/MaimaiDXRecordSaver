﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class MatchingResult
    {
        public string PlayerName { get; set; }
        public int Achievement { get; set; }
        public MatchLevel MatchLevel { get; set; }
        public int DXRating { get; set; }
        public bool Won { get; set; }

        public override string ToString()
        {
            string str = string.Format("[友人对战] 玩家名称: {0}\n", PlayerName);
            str += string.Format("DX Rating: {0} {1}\n", DXRating, MatchLevel.GetName());
            str += string.Format("达成率: {0}% {1}\n", Achievement / 10000.0f, Won ? "Won" : "Lost");
            return str;
        }
    }
}
