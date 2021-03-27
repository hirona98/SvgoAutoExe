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
using Microsoft.Web.WebView2.Core;
using System.Globalization;

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
        private const long MAX_SPLIT_BYTE = 10 * 1024 * 1024;
        private const long MIN_SPLIT_BYTE = 3 * 1024;

        private readonly Svgo svgo;
        private readonly FileSystemWatcher fileWatcher = new FileSystemWatcher();
        private readonly SizeWindow sizeWindow;
        private readonly PreviewWindow previewWindow;
        private NepNepWindow nepNepWindow;

        /// <summary>
        /// メインウインドウ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            sizeWindow = new SizeWindow();
            previewWindow = new PreviewWindow(this);
            svgo = new Svgo(sizeWindow, previewWindow);
            TextBoxSplitSize.Text = String.Format("{0:#,0}", Svgo.SVG_MAX_BYTE);
            TextBoxSrcFile.AddHandler(TextBox.DragOverEvent, new DragEventHandler(TextBoxSrcFile_DragOver), true);
            TextBoxSrcFile.AddHandler(TextBox.DropEvent, new DragEventHandler(TextBoxSrcFile_Drop), true);
        }

        /// <summary>
        /// テキストボックスドラッグオーバー
        /// </summary>
        private void TextBoxSrcFile_DragOver(object sender, DragEventArgs e)
        {
            // マウスポインタを変更する。
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }

        /// <summary>
        /// テキストボックスドロップ
        /// </summary>
        private void TextBoxSrcFile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                TextBoxSrcFile.Text = string.Empty;
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                TextBoxSrcFile.Text = filenames[0];
                TextBoxDstFile.Text = Path.GetDirectoryName(TextBoxSrcFile.Text) + "\\S_" + Path.GetFileName(TextBoxSrcFile.Text);
            }
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
            ChkRemoveXMLNS.IsEnabled = false;
            ChkJoinGradient.IsEnabled = false;
            ChkPreferViewBox.IsEnabled = false;
            SliderPrecision.IsEnabled = false;
            RealtimeExec.IsChecked = true;
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
            ChkRemoveXMLNS.IsEnabled = true;
            ChkJoinGradient.IsEnabled = true;
            ChkPreferViewBox.IsEnabled = true;
            SliderPrecision.IsEnabled = true;
            RealtimeExec.IsChecked = false;
        }

        /// <summary>
        /// 対象ファイル参照ボタンクリック
        /// </summary>
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
        /// リアルタイム実行開始
        /// </summary>
        private void EnabledExec(object sender, EventArgs e)
        {
            UiDisabled();
            DoEvents();

            svgo.ExePath = AppDomain.CurrentDomain.BaseDirectory + SVGO_EXE_PATH_CURRENT;
            svgo.InputFilePath = TextBoxSrcFile.Text;
            svgo.OutputFilePath = TextBoxDstFile.Text;
            svgo.JoinGradient = (bool)ChkJoinGradient.IsChecked;
            svgo.RemoveXmlns = (bool)ChkRemoveXMLNS.IsChecked;
            svgo.PreferViewBox = (bool)ChkPreferViewBox.IsChecked;

            if (StartWatching() == false)
            {
                UiEnabled();
                return;
            }
        }

        /// <summary>
        /// リアルタイム実行解除
        /// </summary>
        private void DisabledExec(object sender, EventArgs e)
        {
            StopWatching();

            UiEnabled();
            RealtimeExec.IsChecked = false;
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
        private object ExitFrames(object obj)
        {
            ((DispatcherFrame)obj).Continue = false;
            return null;
        }

        /// <summary>
        /// サイズ表示ウインドウを開く
        /// </summary>
        private void VisibleSizeWindow(object sender, RoutedEventArgs e)
        {
            sizeWindow.Show();
        }

        /// <summary>
        /// サイズ表示ウインドウを閉じる
        /// </summary>
        private void UnVisibleSizeWindow(object sender, RoutedEventArgs e)
        {
            sizeWindow.Hide();
        }

        /// <summary>
        /// プレビューウインドウ表示
        /// </summary>
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
        private void UnVisiblePreviewWindow(object sender, RoutedEventArgs e)
        {
            previewWindow.Hide();
        }

        /// <summary>
        /// 分割サイズテキストの変更
        /// </summary>
        private void TextSplitByte_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                long splitByte = long.Parse(TextBoxSplitSize.Text, NumberStyles.AllowThousands);
                TextBoxSplitSize.Text = String.Format("{0:#,0}", splitByte);
            }
            catch
            {
                // 何もしない（エラーチェックはFostFocusで行う）
            }
        }

        /// <summary>
        /// 分割サイズテキストボックスから抜ける
        /// </summary>
        private void TextSplitByte_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                long splitByte = long.Parse(TextBoxSplitSize.Text, NumberStyles.AllowThousands);
                if (splitByte > MAX_SPLIT_BYTE)
                {
                    splitByte = MAX_SPLIT_BYTE;
                }
                else if (splitByte < MIN_SPLIT_BYTE)
                {
                    splitByte = MIN_SPLIT_BYTE;
                }

                TextBoxSplitSize.Text = String.Format("{0:#,0}", splitByte);
            }
            catch
            {
                TextBoxSplitSize.Text = String.Format("{0:#,0}", Svgo.SVG_MAX_BYTE);
            }
        }


        /// <summary>
        /// 自動分割保存
        /// </summary>
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
            SvgXml svgXml = new SvgXml();
            svgXml.SaveSplitSvg(TextBoxDstFile.Text, long.Parse(TextBoxSplitSize.Text, NumberStyles.AllowThousands));

            return;
        }

        /// <summary>
        /// ねぷねぷ有効
        /// 停止機能は未実装
        /// </summary>
        private void EnabledNepNep(object sender, RoutedEventArgs e)
        {

            if (MessageBox.Show("本当にねぷねぷして良いですか？", "Information", MessageBoxButton.YesNo,
                 MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                nepNepWindow = new NepNepWindow();
                nepNepWindow.Show();
                ButtonNepNep.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// ねぷねぷ無効
        /// </summary>
        private void DisabledNepNep(object sender, RoutedEventArgs e)
        {
            /* ※ 機能保留 ※
            nepNepWindow.Hide();
            */
        }
    }
}