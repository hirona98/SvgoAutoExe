using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Linq;
using System.Xml.Linq;

namespace SvgoAutoExe
{
    class Svgo
    {
        public const long SVG_MAX_BYTE = 1024 * 15;
        public Int32 Precision { get; set; }
        public string OutputFilePath { get; set; }
        public string InputFilePath { get; set; }

        public string ExePath { get; set; }
        public bool RemoveXmlns { get; set; }

        private readonly SizeWindow sizeWindow;
        private readonly PreviewWindow previewWindow;

        public Svgo(SizeWindow sWindow, PreviewWindow pWindow)
        {
            sizeWindow = sWindow;
            previewWindow = pWindow;
        }

        /// <summary>
        /// SVGOに渡す引数を作成
        /// </summary>
        public string GetArgument(string workFilePath)
        {
            string exeDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
            string optConfig = " --config=" + exeDir + "\\SvgoConfig.yml";
            string optOutput = " -o " + OutputFilePath;
            return workFilePath + optOutput + optConfig;
        }

        /// <summary>
        /// SVGO実行
        /// </summary>
        public void ExecSvgo(object source, FileSystemEventArgs e)
        {
            if (UpdateConfigFile() == false)
            {
                return;
            }

            string workFilePath = DeleteRasterImage(InputFilePath);

            // SVGO実行
            ProcessStartInfo psInfo = new ProcessStartInfo
            {
                FileName = ExePath,

                Arguments = GetArgument(workFilePath),
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process p = Process.Start(psInfo);
            p.WaitForExit();
            p.Dispose();

            File.Delete(workFilePath);

            UpdateSizeWindow();
            previewWindow.PreviewRefresh();

        }

        /// <summary>
        /// ラスター画像の削除
        /// SVGOだと1枚しか削除されないため本アプリで処理する
        /// コピーして編集したファイルのパスを戻す
        /// </summary>
        private string DeleteRasterImage(string inputFilePath)
        {
            string workFilePath = inputFilePath + ".tmp";
            File.Copy(inputFilePath, workFilePath, true);

            // 改行付きでヒットさせる方法を知らないのでこの方法
            RegexReplaceFile(workFilePath, "\n ", ""); // 一度すべて1行にして
            RegexReplaceFile(workFilePath, "/>", "/>\n"); // 要素の最後で改行を入れて
            RegexReplaceFile(workFilePath, "<image .*/>\n", ""); // imageを削除

            return workFilePath;
        }

        private void UpdateSizeWindow()
        {
            FileInfo svgFileInfo = new FileInfo(OutputFilePath);
            long diffByte = svgFileInfo.Length - SVG_MAX_BYTE;
            string sign = "";
            if (diffByte >= 0)
            {
                sign = "+";
            }
            sizeWindow.SetText(String.Format("Size: {0:#,0}Byte ({1}{2:#,0})", svgFileInfo.Length, sign, diffByte));
        }

        /// <summary>
        /// 設定ファイル書き換え（良いYAMLライブラリがなかったので手抜き）
        /// </summary>
        private bool UpdateConfigFile()
        {
            try
            {
            string cfgPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString() + "\\SvgoConfig.yml";
            if (RemoveXmlns == true)
            {
                    RegexReplaceFile(cfgPath, "noSpaceAfterFlags: false", "noSpaceAfterFlags: true");
                }
            else
            {
                    RegexReplaceFile(cfgPath, "noSpaceAfterFlags: true", "noSpaceAfterFlags: false");
                }

                RegexReplaceFile(cfgPath, "floatPrecision: .", "floatPrecision: " + Precision);
                RegexReplaceFile(cfgPath, "floatPrecision: 0 # このプラグインは最低値1", "floatPrecision: 1 # このプラグインは最低値1");
            }
            catch
            {
                MessageBox.Show("設定ファイルの書き換えができません。ファイル有無やアクセス権を確認してください。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ファイル書き換え（正規表現に対応しておく）
        /// </summary>
        private void RegexReplaceFile(string filePath, string beforeRegexString, string afterRegexString)
            {
                string allText;
                using (StreamReader sReader = new StreamReader(filePath))
                {
                    allText = sReader.ReadToEnd();
                    allText = Regex.Replace(allText, beforeRegexString, afterRegexString, RegexOptions.Multiline);
                }

                using (StreamWriter sWriter = new StreamWriter(filePath, false))
                {
                    sWriter.Write(allText);
                }
            }
    }
}
