using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace MaimaiDXRecordSaver.PageParser
{
    public class MusicRecordListPageParser : HtmlPageParserBase<List<MusicRecordSummary>>
    {
        public override void Parse()
        {
            resultObj = new List<MusicRecordSummary>();

            HtmlNode listNode = doc.DocumentNode.SelectSingleNode("//div[@class='wrapper main_wrapper t_c']");
            HtmlNodeCollection records = listNode.SelectNodes("./div[@class='p_10 t_l f_0 v_b']");
            for(int i = 0; i < records.Count; i++ )
            {
                MusicRecordSummary rec = new MusicRecordSummary();
                HtmlNode blockNode = records[i];
                rec.MusicTitle = HtmlUnescape(blockNode.SelectSingleNode(".//div[@class='basic_block m_5 p_5 p_l_10 f_13 break']").InnerText);
                rec.MusicDifficulty = DifficultyEnum.ImageToDifficulty(blockNode.SelectSingleNode(".//img[@class='playlog_diff v_b']").GetAttributeValue("src", ""));
                rec.MusicIsDXLevel = blockNode.SelectSingleNode(".//img[@class='playlog_music_kind_icon']").GetAttributeValue("src", "").Contains("music_dx");
                rec.Achievement = int.Parse(blockNode.SelectSingleNode(".//div[@class='playlog_achievement_txt t_r']").InnerText.Replace("%", string.Empty).Replace(".", string.Empty));
                rec.TrackNumber = byte.Parse(blockNode.SelectSingleNode(".//span[@class='red f_b v_b']").InnerText.Substring(6));
                rec.PlayTime = DateTime.Parse(blockNode.SelectSingleNode(".//span[@class='v_b']").InnerText);
                rec.Index = int.Parse(blockNode.SelectSingleNode(".//input[@name='idx']").GetAttributeValue("value", "-1"));
                resultObj.Add(rec);
            }
        }

        private string HtmlUnescape(string str)
        {
            string result = str.Replace("&quot;", "\"");
            result = result.Replace("&amp;", "&");
            result.Replace("&lt;", "<");
            result.Replace("&gt;", ">");
            return result;
        }
    }
}
