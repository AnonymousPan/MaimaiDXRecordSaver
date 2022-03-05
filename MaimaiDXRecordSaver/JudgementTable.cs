using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class JudgementTable
    {
        // [NoteType, JudgementType]
        private int[,] table = new int[5,5];

        public int[,] Table { get { return table; } set { table = value; } }

        public JudgementTable() { }

        public JudgementTable(int[,] t)
        {
            if(t.Rank == 2 && t.GetUpperBound(0) == 4 && t.GetUpperBound(1) == 4)
            {
                table = t;
            }
            else
            {
                throw new ArgumentException("Invalid table.");
            }
        }

        public int[] ByNoteType(NoteType type)
        {
            int[] arr = new int[5];
            for(int i = 0; i < 5; i++ )
            {
                arr[i] = table[(int)type, i];
            }
            return arr;
        }

        public int[] ByJudgementType(JudgementType type)
        {
            int[] arr = new int[5];
            for (int i = 0; i < 5; i++)
            {
                arr[i] = table[i, (int)type];
            }
            return arr;
        }

        public override string ToString()
        {
            string str = "\tC.Pfct\tPerfect\tGreat\tGood\tMiss\n";
            for(int i = 0; i < 5; i++ )
            {
                string s = ((NoteType)i).GetName();
                for(int j = 0; j < 5; j++ )
                {
                    s += "\t" + table[i, j].ToString();
                }
                str += s + "\n";
            }
            return str;
        }
    }
}
