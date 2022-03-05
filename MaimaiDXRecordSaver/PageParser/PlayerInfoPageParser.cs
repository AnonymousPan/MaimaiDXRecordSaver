using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MaimaiDXRecordSaver.PageParser
{
    public class PlayerInfoPageParser : HtmlPageParserBase<PlayerInfo>
    {
        public override void Parse()
        {
            resultObj = new PlayerInfo();
            HtmlNode blockNode = doc.DocumentNode.SelectSingleNode("//div[@class='basic_block p_10 p_b_5 f_0']");
            resultObj.Name = blockNode.SelectSingleNode(".//div[@class='name_block f_l f_14']").InnerText;
            resultObj.Rating = int.Parse(blockNode.SelectSingleNode(".//div[@class='rating_block f_11']").InnerText);
            resultObj.MaxRating = int.Parse(blockNode.SelectSingleNode(".//div[@class='p_r_5 f_11']").InnerText.Substring(4));
            resultObj.Level = MatchLevelEnum.GetMatchLevelFromIconUrl(blockNode.SelectSingleNode(".//img[@class='h_25 f_l']").GetAttributeValue("src", ""));
            resultObj.Stars = int.Parse(blockNode.SelectSingleNode(".//div[@class='p_l_10 f_l f_14']").InnerText.Substring(1));

            resultObj.PlayCount = int.Parse(doc.DocumentNode.SelectSingleNode(".//div[@class='m_5 m_t_10 t_r f_12']").InnerText.Substring(5));

            blockNode = doc.DocumentNode.SelectSingleNode("//div[@class='see_through_block m_15 m_t_0 p_10 t_l f_0']");
            resultObj.SSSPlus = MusicCounterBlockGetValue(blockNode, 4);
            resultObj.SSS = MusicCounterBlockGetValue(blockNode, 7);
            resultObj.SSPlus = MusicCounterBlockGetValue(blockNode, 10);
            resultObj.SS = MusicCounterBlockGetValue(blockNode, 13);
            resultObj.SPlus = MusicCounterBlockGetValue(blockNode, 16);
            resultObj.S = MusicCounterBlockGetValue(blockNode, 19);
            resultObj.Clear = MusicCounterBlockGetValue(blockNode, 22);

            resultObj.AllPerfectPlus = MusicCounterBlockGetValue(blockNode, 5);
            resultObj.AllPerfect = MusicCounterBlockGetValue(blockNode, 8);
            resultObj.FullComboPlus = MusicCounterBlockGetValue(blockNode, 11);
            resultObj.FullCombo = MusicCounterBlockGetValue(blockNode, 14);
            resultObj.FullSyncDXPlus = MusicCounterBlockGetValue(blockNode, 17);
            resultObj.FullSyncDX = MusicCounterBlockGetValue(blockNode, 20);
            resultObj.FullSyncPlus = MusicCounterBlockGetValue(blockNode, 23);
            resultObj.FullSync = MusicCounterBlockGetValue(blockNode, 25);
        }

        private int MusicCounterBlockGetValue(HtmlNode parentNode, int index)
        {
            HtmlNode node = parentNode.SelectSingleNode(string.Format("./div[{0}]", index));
            string str = node.SelectSingleNode(".//div[@class='musiccount_counter_block f_13']").InnerText;
            return int.Parse(str.Split('/')[0]);
        }
    }
}
