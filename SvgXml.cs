using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace SvgoAutoExe
{
    class SvgXml
    {
        /// <summary>
        /// SVGの分割
        /// </summary>
        public bool SaveSplitSvg(string filePath)
        {
            // <svg～>を取得
            StreamReader reader = new StreamReader(filePath);
            string readText = reader.ReadToEnd();
            reader.Close();
            int readTextEndPoint = readText.IndexOf(">");
            string textSvgElm = readText.Remove(readTextEndPoint + 1);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            XmlNodeList xmlPathList = xmlDoc.SelectNodes("//*[local-name()='path']");

            string textPathElm = "";
            string textGradientElm = "";
            string textLastSplitSvg = "";
            int saveCount = 1;
            for (int i = 0; i < xmlPathList.Count; i++)
            {
                textPathElm += xmlPathList[i].OuterXml;
                textGradientElm += FindConnectionElement(xmlDoc, xmlPathList[i].OuterXml, "fill");
                textGradientElm += FindConnectionElement(xmlDoc, xmlPathList[i].OuterXml, "stroke");

                // 13KB超えたら前回ループのデータを保存
                string textSplitSvg = textSvgElm + "<defs>" + textGradientElm + "</defs>" + textPathElm + "</svg>";
                if (textSplitSvg.Length >= Svgo.SVG_MAX_BYTE)
                {
                    SaveTextFile(MakeSplitFilePath(filePath, saveCount), textLastSplitSvg);
                    saveCount++;
                    // 次回保存は今回分から
                    textPathElm = xmlPathList[i].OuterXml;
                    textGradientElm = FindConnectionElement(xmlDoc, xmlPathList[i].OuterXml, "fill");
                    textGradientElm += FindConnectionElement(xmlDoc, xmlPathList[i].OuterXml, "stroke");
                    // 最後だったら現在のデータも保存して抜ける
                    if (i == xmlPathList.Count - 1)
                    {
                        textSplitSvg = textSvgElm + "<defs>" + textGradientElm + "</defs>" + textPathElm + "</svg>";
                        SaveTextFile(MakeSplitFilePath(filePath, saveCount), textSplitSvg);
                        break;
                    }
                }
                // 最後だったら容量に関係なく終了
                if (i == xmlPathList.Count - 1)
                {
                    SaveTextFile(MakeSplitFilePath(filePath, saveCount), textSplitSvg);
                }
                textLastSplitSvg = textSplitSvg;
            }
            return true;
        }

        /// <summary>
        /// 関連した要素の検索（Pathのidから検索）
        /// </summary>
        private string FindConnectionElement(XmlDocument xmlDoc, string outerXml, string searchAttr)
        {
            string textElement = "";
            // urlがある場合は関連づいているlinearGradientを取得
            Match matchId = Regex.Match(outerXml, searchAttr + "=\"url\\(#([A-Za-z0-9]*)");
            if (matchId.Value != "")
            {
                string id = matchId.Value.Remove(0, searchAttr.Length + 7);
                XmlNode searchById = xmlDoc.SelectSingleNode("//*[local-name()='linearGradient'][@id='" + id + "']");
                textElement = searchById.OuterXml;

                // xlink:hrefがある場合は関連づいているlinearGradientを取得
                Match matchLink = Regex.Match(searchById.OuterXml, "xlink:href=\\\"#([A-Za-z0-9]*)");
                if (matchLink.Value != "")
                {
                    string link = matchLink.Value.Remove(0, 13);
                    XmlNode searchByLink = xmlDoc.SelectSingleNode("//*[local-name()='linearGradient'][@id='" + link + "']");
                    textElement += searchByLink.OuterXml;
                }
            }
            return textElement;
        }

        /// <summary>
        /// ファイル保存
        /// </summary>
        private void SaveTextFile(string filePath, string text)
        {
            using (StreamWriter sWriter = new StreamWriter(filePath, false))
            {
                sWriter.Write(text);
            }
        }

        /// <summary>
        /// 分割保存ファイルパス作成
        /// </summary>
        private string MakeSplitFilePath(string filePath, int cnt)
        {
            string dirPath = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            return dirPath + "\\" + cnt.ToString() + fileName;
        }
    }
}
