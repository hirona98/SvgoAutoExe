using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace SvgoAutoExe
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string FILE_TYPE = "SVG files (*.svg)|*.svg|All files (*.*)|*.*";
        private const string SAVA_FILE_TITLE = "保存先のファイルを選択してください";
        private const string SVGO_EXE_PATH_CURRENT = "svgo\\svgo.exe";

        private readonly Svgo svgo;
        private readonly FileSystemWatcher fileWatcher = new FileSystemWatcher();
        private readonly SizeWindow sizeWindow;
        private readonly PreviewWindow previewWindow;

        /// <summary>
        /// メインウインドウ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ButtonStop.IsEnabled = false;
            sizeWindow = new SizeWindow();
            previewWindow = new PreviewWindow(this);
            svgo = new Svgo(sizeWindow, previewWindow);
        }

        /// <summary>
        /// 実行中UI制御
        /// </summary>
        private void UiDisabled()
        {
            TextBoxSrcFile.IsEnabled = false;
            ButtonSrcDialogOpen.IsEnabled = false;
            ButtonDstDialogOpen.IsEnabled = false;
            TextBoxDstFile.IsEnabled = false;
            ButtonStart.IsEnabled = false;
            ChkRemoveXMLNS.IsEnabled = false;
            SliderPrecision.IsEnabled = false;

            ButtonStop.IsEnabled = true;
        }

        /// <summary>
        /// アイドル中UI有効化
        /// </summary>
        private void UiEnabled()
        {
            TextBoxSrcFile.IsEnabled = true;
            ButtonSrcDialogOpen.IsEnabled = true;
            ButtonDstDialogOpen.IsEnabled = true;
            TextBoxDstFile.IsEnabled = true;
            ButtonStart.IsEnabled = true;
            ChkRemoveXMLNS.IsEnabled = true;
            SliderPrecision.IsEnabled = true;

            ButtonStop.IsEnabled = false;
        }

        /// <summary>
        /// 対象ファイル参照ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSrcOpenDialog_Click(object sender, EventArgs e)
        {
            OpenFileDialog srcDialogOpen = new OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = FILE_TYPE
            };

            if (srcDialogOpen.ShowDialog() == true)
            {
                TextBoxSrcFile.Text = srcDialogOpen.FileName;
                TextBoxDstFile.Text = Path.GetDirectoryName(TextBoxSrcFile.Text) + "\\S_" + Path.GetFileName(TextBoxSrcFile.Text);
            }
        }

        /// <summary>
        /// 保存先ファイル参照ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDstOpenDialog_Click(object sender, EventArgs e)
        {
            TextBoxDstFile.Text = SaveFileDialogOpen();

        }

        /// <summary>
        /// 保存先の取得
        /// </summary>
        private string SaveFileDialogOpen()
        {
            SaveFileDialog dstDialogOpen = new SaveFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (string.IsNullOrEmpty(TextBoxSrcFile.Text) == false)
            {
                if (Directory.Exists(Path.GetDirectoryName(TextBoxSrcFile.Text)) == true)
                {
                    dstDialogOpen.InitialDirectory = Path.GetDirectoryName(TextBoxSrcFile.Text);
                }
            }

            dstDialogOpen.Filter = FILE_TYPE;
            dstDialogOpen.Title = SAVA_FILE_TITLE;
            dstDialogOpen.RestoreDirectory = true;

            if (dstDialogOpen.ShowDialog() == true)
            {
                return dstDialogOpen.FileName;
            }

            return "";
        }

        /// <summary>
        /// スライダの値をセット
        /// </summary>
        private void PrecisionSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;

            // svgo = new Svgo(sizeWindow) をするために静的インスタンスに出来ないのでインスタンス化するまでは触らない
            if (svgo != null)
            {
                svgo.Precision = (Int32)slider.Value;
            }
        }

        /// <summary>
        /// 監視開始ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStart_Click(object sender, EventArgs e)
        {
            UiDisabled();
            DoEvents();

            svgo.ExePath = AppDomain.CurrentDomain.BaseDirectory + SVGO_EXE_PATH_CURRENT;
            svgo.InputFilePath = TextBoxSrcFile.Text;
            svgo.OutputFilePath = TextBoxDstFile.Text;

            if (StartWatching() == false)
            {
                UiEnabled();
                return;
            }
        }

        /// <summary>
        /// 監視ストップボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStop_Click(object sender, EventArgs e)
        {
            StopWatching();

            UiEnabled();
            DoEvents();
        }

        /// <summary>
        /// 監視開始
        /// </summary>
        private bool StartWatching()
        {
            if (File.Exists(TextBoxSrcFile.Text) == false)
            {
                MessageBox.Show("対象ファイルがありません");
                UiEnabled();
                return false;
            }
            if (Directory.Exists(Directory.GetParent(TextBoxDstFile.Text).ToString()) == false)
            {
                MessageBox.Show("保存フォルダがありません");
                UiEnabled();
                return false;
            }

            svgo.Precision = (Int32)SliderPrecision.Value;

            fileWatcher.Path = Path.GetDirectoryName(svgo.InputFilePath);
            fileWatcher.Filter = Path.GetFileName(svgo.InputFilePath);
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileWatcher.Changed += new FileSystemEventHandler(svgo.ExecSvgo);

            fileWatcher.EnableRaisingEvents = true; // スタート

            svgo.ExecSvgo(null, null);

            return true;
        }

        /// <summary>
        /// 監視停止
        /// </summary>
        private void StopWatching()
        {
            fileWatcher.EnableRaisingEvents = false; // ストップ
        }

        /// <summary>
        /// 描画更新用
        /// </summary>
        private void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(ExitFrames);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// 描画更新用
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private object ExitFrames(object obj)
        {
            ((DispatcherFrame)obj).Continue = false;
            return null;
        }

        /// <summary>
        /// サイズ表示ウインドウを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisibleSizeWindow(object sender, RoutedEventArgs e)
        {
            sizeWindow.Show();
        }

        /// <summary>
        /// サイズ表示ウインドウを閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnVisibleSizeWindow(object sender, RoutedEventArgs e)
        {
            sizeWindow.Hide();
        }

        /// <summary>
        /// プレビューウインドウ表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisiblePreviewWindow(object sender, RoutedEventArgs e)
        {
            if (File.Exists(TextBoxDstFile.Text) == true)
            {
                previewWindow.Browser.Navigate(new Uri(TextBoxDstFile.Text));
            }
            else
            {
                previewWindow.Browser.Navigate(new Uri("https://www.compileheart.com/"));
            }

            previewWindow.Show();
        }

        /// <summary>
        /// プレビューウインドウを閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnVisiblePreviewWindow(object sender, RoutedEventArgs e)
        {
            previewWindow.Hide();
        }

        /// <summary>
        /// 自動分割保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSplitOut_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBoxSrcFile.Text) == true)
            {
                MessageBox.Show("対象ファイルを設定してください");
                return;
            }
            if (string.IsNullOrEmpty(TextBoxSrcFile.Text) == true)
            {
                MessageBox.Show("保存ファイルを設定してください");
                return;
            }


            // 軽量化
            svgo.ExePath = AppDomain.CurrentDomain.BaseDirectory + SVGO_EXE_PATH_CURRENT;
            svgo.InputFilePath = TextBoxSrcFile.Text;
            svgo.OutputFilePath = TextBoxDstFile.Text;
            svgo.Precision = (Int32)SliderPrecision.Value;
            svgo.ExecSvgo(null, null);

            //軽量化したSVG（path）を分割する
            SplitSvg(TextBoxDstFile.Text);

            return;
        }

        /// <summary>
        /// SVGの分割
        /// </summary>
        /// <param name="filePath"></param>
        private bool SplitSvg(string filePath)
        {
            //TODO: 長すぎるので後で整理する
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
            string lastTextSplitSvg = "";
            int saveCount = 1;
            for (int i = 0; i < xmlPathList.Count; i++)
            {
                // urlがある場合は関連づいているlinearGradientを取得
                Match matchId = Regex.Match(xmlPathList[i].OuterXml, "url\\(#(.[A-Za-z0-9])");
                if (matchId.Value != "")
                {
                    string id = matchId.Value.Remove(0, 5);
                    XmlNode searchById = xmlDoc.SelectSingleNode("//*[local-name()='linearGradient'][@id='" + id + "']");
                    textGradientElm += searchById.OuterXml;
                    // xlink:hrefがある場合は関連づいているlinearGradientを取得
                    Match matchLink = Regex.Match(searchById.OuterXml, "xlink:href=\"#(.[A-Za-z0-9])");
                    if (matchLink.Value != "")
                    {
                        string link = matchLink.Value.Remove(0, 13);
                        XmlNode searchByLink = xmlDoc.SelectSingleNode("//*[local-name()='linearGradient'][@id='" + link + "']");
                        textGradientElm += searchByLink.OuterXml;
                    }
                }
                textPathElm += xmlPathList[i].OuterXml;

                // 保存
                string textSplitSvg = textSvgElm + "<defs>" + textGradientElm + "</defs>" + textPathElm + "</svg>";
                if (textSplitSvg.Length >= Svgo.SVG_MAX_BYTE - 1000) // 面倒なので少なめで…
                {
                    SaveTextFile(MakeSplitFilePath(filePath, saveCount), lastTextSplitSvg);
                    textGradientElm = "";
                    textPathElm = "";
                    saveCount++;
                    // 最後だったら現在のデータも保存して抜ける
                    if (i == xmlPathList.Count - 1)
                    {
                        SaveTextFile(MakeSplitFilePath(filePath, saveCount), textSplitSvg);
                        break;
                    }
                }
                // 最後だったら容量に関係なく終了
                if (i == xmlPathList.Count - 1)
                {
                    SaveTextFile(MakeSplitFilePath(filePath, saveCount), textSplitSvg);
                }
                lastTextSplitSvg = textSplitSvg;
            }
            return true;
        }

        /// <summary>
        /// ファイル保存
        /// </summary>
        /// <param name="filePath"></param>
        /// /// <param name="text"></param>
        private void SaveTextFile(string filePath, string text)
        {
            using (StreamWriter sWriter = new StreamWriter(filePath, false, Encoding.GetEncoding("UTF-8")))
            {
                sWriter.Write(text);
            }
        }

        /// <summary>
        /// 分割保存ファイルパス作成
        /// </summary>
        /// <param name="filePath"></param>
        /// /// <param name="cnt"></param>
        private string MakeSplitFilePath(string filePath, int cnt)
        {
            string dirPath = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            return dirPath + "\\" + cnt.ToString() + fileName;
        }
    }
}
