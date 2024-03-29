﻿using MaimaiDXRecordSaver.PageParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaimaiDXRecordSaver
{
    public static partial class Program
    {
        private static void DispatchCommand(string line)
        {
            string[] arr = line.Split(' ');
            string command = arr[0].ToUpper();
            switch (command)
            {
                case "HELP":
                    Command_Help();
                    break;
                case "RECID":
                    Command_RecID(arr);
                    break;
                case "RECLIST":
                    Command_RecList();
                    break;
                case "PLAYERINFO":
                    Command_PlayerInfo();
                    break;
                case "SAVEALL":
                    Command_SaveAll();
                    break;
                case "SAVEID":
                    Command_SaveID(arr);
                    break;
                case "LOCALID":
                    Command_LocalID(arr);
                    break;
                case "LOCALRECENT":
                    Command_LocalRecent(arr);
                    break;
                default:
                    Console.WriteLine("未知的命令，输入\"help\"查看帮助");
                    break;
            }
            SaveCredential();
        }

        private static void Command_Help()
        {
            Console.WriteLine(help);
        }

        private static void Command_RecID(string[] arr)
        {
            if (arr.Length >= 2)
            {
                if (int.TryParse(arr[1], out int index))
                {
                    PrintMusicRecord(index);
                    SaveCredential();
                }
                else
                {
                    Console.WriteLine("无效的数字");
                }
            }
            else
            {
                Console.WriteLine("缺少参数");
            }
        }

        private static void Command_RecList()
        {
            MusicRecordListPageParser parser = new MusicRecordListPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/"));
            parser.Parse();
            List<MusicRecordSummary> list = parser.GetResult();
            for (int i = 0; i < list.Count; i++)
            {
                Console.Write(i.ToString() + ". " + list[i].ToString());
            }
        }

        private static void Command_PlayerInfo()
        {
            PlayerInfoPageParser parser = new PlayerInfoPageParser();
            parser.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/playerData/"));
            parser.Parse();
            PlayerInfo obj = parser.GetResult();
            Console.WriteLine(obj.ToString());
        }

        private static void Command_SaveAll()
        {
            MusicRecordListPageParser parser1 = new MusicRecordListPageParser();
            parser1.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/"));
            parser1.Parse();
            List<MusicRecordSummary> list = parser1.GetResult();
            int[] indices = DataRecorder.GetRecordIndicesNeedToSave(list);
            MusicRecordPageParser parser2 = new MusicRecordPageParser();
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                int index = indices[i];
                Logger.Info("SaveAll: 正在保存记录, idx=" + index.ToString());
                parser2.LoadPage(Requester.RequestString("https://maimai.wahlap.com/maimai-mobile/record/playlogDetail/?idx=" + index.ToString()));
                parser2.Parse();
                MusicRecord rec = parser2.GetResult();
                if (DataRecorder.SaveMusicRecord(rec) == -1)
                {
                    Logger.Warn("SaveAll: 无法保存记录, index=" + index.ToString());
                }
                Thread.Sleep(250);
            }
        }

        private static void Command_SaveID(string[] arr)
        {
            if (arr.Length >= 2)
            {
                if (int.TryParse(arr[1], out int index))
                {
                    SaveRecordID(index);
                    SaveCredential();
                }
                else
                {
                    Console.WriteLine("无效的数字");
                }
            }
            else
            {
                Console.WriteLine("缺少参数");
            }
        }

        private static void Command_LocalID(string[] arr)
        {
            if (arr.Length >= 2)
            {
                if (int.TryParse(arr[1], out int index))
                {
                    if (DataRecorder.IsRecordExists(index))
                    {
                        MusicRecord rec = DataRecorder.GetMusicRecord(index);
                        Console.WriteLine(rec.ToString());
                    }
                    else
                    {
                        Console.WriteLine("记录未找到");
                    }
                }
                else
                {
                    Console.WriteLine("无效的数字");
                }
            }
            else
            {
                Console.WriteLine("缺少参数");
            }
        }

        private static void Command_LocalRecent(string[] arr)
        {
            if (arr.Length >= 2)
            {
                int num = 0;
                if (int.TryParse(arr[1], out num))
                {
                    int latestID = DataRecorder.GetLastRecordID();
                    int j = Math.Max(latestID - num + 1, 0);
                    for (int i = latestID; i >= j; i--)
                    {
                        Console.WriteLine("Local ID " + i.ToString());
                        if (DataRecorder.IsRecordExists(i))
                        {
                            Console.Write(DataRecorder.GetMusicRecordSummary(i).ToString());
                        }
                        else
                        {
                            Console.WriteLine("记录未找到");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("无效的数字");
                }
            }
            else
            {
                Console.WriteLine("缺少参数");
            }
        }
    }
}
