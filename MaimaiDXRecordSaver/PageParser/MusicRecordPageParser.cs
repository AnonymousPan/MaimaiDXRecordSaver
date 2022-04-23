using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MaimaiDXRecordSaver.PageParser
{
    public class MusicRecordPageParser : HtmlPageParserBase<MusicRecord>
    {
        public override void Parse()
        {
            bool is1P = false;
            resultObj = new MusicRecord();
            HtmlNode blockNode = doc.DocumentNode.SelectSingleNode("//div[@class='playlog_top_container']");
            resultObj.MusicDifficulty = DifficultyEnum.ImageToDifficulty(blockNode.SelectSingleNode(".//img[@class='playlog_diff v_b']").GetAttributeValue("src", ""));
            resultObj.TrackNumber = byte.Parse(blockNode.SelectSingleNode(".//span[@class='red f_b v_b']").InnerText.Substring(6));
            resultObj.PlayTime = DateTime.Parse(blockNode.SelectSingleNode(".//span[@class='v_b']").InnerText);

            blockNode = doc.DocumentNode.SelectSingleNode("//div[@class='p_10 t_l f_0 v_b']/div[2]");
            string title = blockNode.SelectSingleNode(".//div[@class='basic_block m_5 p_5 p_l_10 f_13 break']").InnerText;
            title = HtmlUnescape(title);
            resultObj.MusicIsDXLevel = ImageToIsDXLevel(blockNode.SelectSingleNode(".//img[@class='playlog_music_kind_icon']").GetAttributeValue("src", ""));
            HtmlNode achievementNode = blockNode.SelectSingleNode(".//div[@class='playlog_achievement_txt t_r']");
            resultObj.Achievement = int.Parse(achievementNode.InnerText.Replace("%", string.Empty).Replace(".", string.Empty));
            resultObj.AchievementNewRecord = blockNode.SelectSingleNode(".//img[@class='playlog_achievement_newrecord']") != null;
            resultObj.DXScore = int.Parse(blockNode.SelectSingleNode(".//div[@class='white p_r_5 f_15 f_r']").InnerText.Replace(",", string.Empty));
            resultObj.DXScoreNewRecord = blockNode.SelectSingleNode(".//img[@class='playlog_deluxscore_newrecord']") != null;
            HtmlNode rankingNode = blockNode.SelectSingleNode(".//img[@class='h_35 m_5 f_r']");
            if (rankingNode == null)
            {
                is1P = true;
                resultObj.Ranking = 0;
            }
            else
            {
                resultObj.Ranking = ImageToRanking(blockNode.SelectSingleNode(".//img[@class='h_35 m_5 f_r']").GetAttributeValue("src", ""));
            }
            HtmlNode fcfsIconNode = blockNode.SelectSingleNode(".//div[@class='playlog_result_innerblock basic_block p_5 f_13']");
            resultObj.ComboIcon = ImageToComboIcon(fcfsIconNode.SelectSingleNode("./img[1]").GetAttributeValue("src", ""));
            resultObj.SyncIcon = ImageToSyncIcon(fcfsIconNode.SelectSingleNode("./img[2]").GetAttributeValue("src", ""));

            resultObj.MusicID = MusicList.Instance.MatchMusic(title, resultObj.MusicIsDXLevel);

            blockNode = doc.DocumentNode.SelectSingleNode(".//div[@class='gray_block m_10 m_t_0 p_b_5 f_0']");
            for(int i = 2; i < 7; i++ )
            {
                HtmlNode charaNode = blockNode.SelectSingleNode(string.Format("./div[{0}]", i));
                CharacterInfo chara = new CharacterInfo();
                chara.CharacterID = ImageToCharaID(charaNode.SelectSingleNode(".//img[@class='chara_cycle_img']").GetAttributeValue("src", ""));
                chara.Stars = int.Parse(charaNode.SelectSingleNode("./div[@class='playlog_chara_star_block f_12']").InnerText.Substring(1));
                chara.Level = int.Parse(charaNode.SelectSingleNode("./div[@class='playlog_chara_lv_block f_13']").InnerText.Substring(2));
                resultObj.Characters[i - 2] = chara;
            }
            HtmlNode fastLateNode = blockNode.SelectSingleNode(".//div[@class='playlog_fl_block m_b_5 f_r f_12']");
            resultObj.FastCount = int.Parse(fastLateNode.SelectSingleNode("./div[1]/div[1]").InnerText);
            resultObj.LateCount = int.Parse(fastLateNode.SelectSingleNode("./div[2]/div[1]").InnerText);

            blockNode = blockNode.SelectSingleNode("./div[@class='p_5']");
            HtmlNode tableNode = blockNode.SelectSingleNode("./table[@class='playlog_notes_detail t_r f_l f_11 f_b']");
            int[,] table = new int[5, 5];
            for(int i = 2; i < 7; i++ )
            {
                HtmlNode rowNode = tableNode.SelectSingleNode(string.Format("./tr[{0}]", i));
                for(int j = 1; j < 6; j++ )
                {
                    int value = -1;
                    HtmlNode valueNode = rowNode.SelectSingleNode(string.Format("./td[{0}]", j));
                    if (valueNode != null && !string.IsNullOrEmpty(valueNode.InnerText) && !string.IsNullOrWhiteSpace(valueNode.InnerText))
                        value = int.Parse(valueNode.InnerText);
                    table[i - 2, j - 1] = value;
                }
            }
            resultObj.Judgements = new JudgementTable(table);

            blockNode = blockNode.SelectSingleNode("./div[@class='playlog_rating_detail_block f_r t_l']");
            HtmlNode node = blockNode.SelectSingleNode("./table[1]/tr[1]/td[2]");
            resultObj.MatchLevelRating = int.Parse(node.SelectSingleNode("./div[1]").InnerText);
            resultObj.MatchLevelRatingChange = int.Parse(node.SelectSingleNode("./span[1]").InnerText
                .Trim(new char[] { '(', ')'}).Replace('＋', '+').Replace('－', '-'));
            resultObj.BaseRating = int.Parse(blockNode.SelectSingleNode("./table[1]/tr[2]/td[2]/div[1]").InnerText);
            resultObj.RatingChange = ImageToRatingChange(blockNode.SelectSingleNode(".//img[@class='h_20 f_r']").GetAttributeValue("src", ""));
            resultObj.NewMatchLevel = MatchLevelEnum.GetMatchLevelFromIconUrl(blockNode.SelectSingleNode(".//img[@class='h_25 m_t_10 m_b_5']").GetAttributeValue("src", ""));

            blockNode = doc.DocumentNode.SelectSingleNode("//div[@class='gray_block m_10 m_t_0 p_b_5 f_0']");
            string[] arr = blockNode.SelectSingleNode(".//div[@class='col2 f_l t_l f_0']/div[1]/div[1]").InnerText.Split('/');
            resultObj.Combo = int.Parse(arr[0]);
            resultObj.MaxCombo = int.Parse(arr[1]);
            if (!is1P)
            {
                arr = blockNode.SelectSingleNode(".//div[@class='col2 p_l_5 f_l t_l f_0']/div[1]/div[1]").InnerText.Split('/');
                resultObj.Sync = int.Parse(arr[0]);
                resultObj.MaxSync = int.Parse(arr[1]);
            }
            else
            {
                resultObj.Sync = -1;
                resultObj.MaxSync = -1;
            }

            if(!is1P)
            {
                blockNode = doc.DocumentNode.SelectSingleNode("//div[@class='see_through_block m_10 p_5 t_l f_14']");
                for (int i = 1; i < 4; i++)
                {
                    node = blockNode.SelectSingleNode(string.Format("./span[{0}]", i));
                    HtmlNode diffNode = node.SelectSingleNode("./img[@class='h_16']");
                    if (diffNode != null)
                    {
                        resultObj.FriendDifficulties[i - 1] = DifficultyEnum.ImageToDifficulty(diffNode.GetAttributeValue("src", ""));
                        resultObj.FriendNames[i - 1] = node.SelectSingleNode("./div[@class='basic_block p_3 t_c f_11']").InnerText;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            blockNode = doc.DocumentNode.SelectSingleNode("//div[@class='see_through_block m_10 m_t_0  p_l_10 t_l f_0 break']");
            if(blockNode != null)
            {
                MatchingResult matching = new MatchingResult();
                matching.PlayerName = blockNode.SelectSingleNode(".//div[@class='p_l_5 p_r_5 f_l f_14 l_h_10']").GetDirectInnerText();
                matching.Achievement = int.Parse(blockNode.SelectSingleNode(".//span[@class='f_r f_11']").InnerText.Replace("%", string.Empty).Replace(".", string.Empty));
                matching.DXRating = int.Parse(blockNode.SelectSingleNode(".//span[@class='f_14']").InnerText);
                matching.MatchLevel = MatchLevelEnum.GetMatchLevelFromIconUrl(blockNode.SelectSingleNode(".//img[@class='h_25 m_3']").GetAttributeValue("src", ""));
                matching.Won = doc.DocumentNode.SelectSingleNode("//img[@class='playlog_vs_result v_b']").GetAttributeValue("src", "").Contains("win");
                resultObj.MatchingInfo = matching;
            }
        }

        private bool ImageToIsDXLevel(string imageUrl)
        {
            return imageUrl.Contains("music_dx");
        }

        private byte ImageToRanking(string imageUrl)
        {
            Regex regex = new Regex("[0-9][a-z]+.png");
            string num = regex.Match(imageUrl).Value.Substring(0, 1);
            return byte.Parse(num);
        }

        private string[] comboIconTable = { "/fc_dummy.", "/applus.", "/ap.", "/fcplus.", "/fc." };
        private ComboIcon ImageToComboIcon(string imageUrl)
        {
            for(int i = 0; i < comboIconTable.Length; i++ )
            {
                if(imageUrl.Contains(comboIconTable[i]))
                {
                    return (ComboIcon)i;
                }
            }
            return ComboIcon.Unknown;
        }

        private string[] syncIconTable = { "/fs_dummy.", "/fsdplus.", "/fsd.", "/fsplus.", "/fs." };
        private SyncIcon ImageToSyncIcon(string imageUrl)
        {
            for (int i = 0; i < syncIconTable.Length; i++)
            {
                if (imageUrl.Contains(syncIconTable[i]))
                {
                    return (SyncIcon)i;
                }
            }
            return SyncIcon.Unknown;
        }

        private string ImageToCharaID(string imageUrl)
        {
            Regex regex = new Regex("/[0-9a-f]+");
            return regex.Match(imageUrl).Value.Substring(1);
        }

        private string[] ratingChangeTable = { "rating_keep", "rating_up", "rating_down" };
        private RatingChange ImageToRatingChange(string imageUrl)
        {
            for (int i = 0; i < ratingChangeTable.Length; i++)
            {
                if (imageUrl.Contains(ratingChangeTable[i]))
                {
                    return (RatingChange)i;
                }
            }
            return RatingChange.Unknown;
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
