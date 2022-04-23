using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public abstract class DataRecorderBase
    {
        public abstract bool Init();
        public abstract int SaveMusicRecord(MusicRecord rec);
        public abstract MusicRecord GetMusicRecord(int id);
        public abstract int GetLastRecordID();
        public abstract bool IsRecordExists(int id);

        public MusicRecord GetLastRecord()
        {
            return GetMusicRecord(GetLastRecordID());
        }

        public MusicRecordSummary GetMusicRecordSummary(int id)
        {
            return new MusicRecordSummary(GetMusicRecord(id));
        }

        public int[] GetRecordIndicesNeedToSave(List<MusicRecordSummary> list)
        {
            List<int> indices = new List<int>();
            MusicRecord latest = GetLastRecord();
            foreach (MusicRecordSummary rec in list)
            {
                if (rec.Equals(latest)) break;
                indices.Add(rec.Index);
            }
            return indices.ToArray();
        }
    }
}
