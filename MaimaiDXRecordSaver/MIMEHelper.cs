using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MaimaiDXRecordSaver
{
    public class MIMEHelper
    {
        public static MIMEHelper Instance { get; private set; }

        private Dictionary<string, string> mimeMapping;

        private MIMEHelper()
        {
            mimeMapping = new Dictionary<string, string>();
        }

        private void Load(string path)
        {
            string content = File.ReadAllText(path);
            string[] lines = content.Split('\n');
            foreach(string str in lines)
            {
                // Extension-Name MIME-Type
                // png image/png
                string line = str.Trim();
                if(!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                {
                    string[] arr = line.Split(' ');
                    if(arr.Length >= 2)
                    {
                        mimeMapping.Add(arr[0], arr[1]);
                    }
                }
            }
        }

        public string GetMIMEType(string filename)
        {
            int lastIndex = filename.LastIndexOf('.');
            if (lastIndex == -1) return null;
            string extName = filename.Substring(lastIndex + 1);
            if(mimeMapping.TryGetValue(extName, out string mimeType))
            {
                return mimeType;
            }
            else
            {
                return null;
            }
        }

        public static void Initialize(string path)
        {
            Instance = new MIMEHelper();
            Instance.Load(path);
        }
    }
}
