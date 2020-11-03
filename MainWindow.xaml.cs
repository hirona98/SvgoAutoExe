using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SvgoAutoExe
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string FILE_TYPE = "SVG files (*.svg)|*.svg|All files (*.*)|*.*";
        private const string SAVA_FILE_TITLE = "保存先のファイルを選択してください";
        private const string DEFAULT_SAVE_FILENAME = "\\Output.svg";
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
                TextBoxDstFile.Text = Path.GetDirectoryName(TextBoxSrcFile.Text) + DEFAULT_SAVE_FILENAME;
            }
        }

        /// <summary>
        /// 保存先ファイル参照ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDstOpenDialog_Click(object sender, EventArgs e)
        {

            SaveFileDialog dstDialogOpen = new SaveFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (String.IsNullOrEmpty(TextBoxSrcFile.Text) == false)
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
                TextBoxDstFile.Text = dstDialogOpen.FileName;
            }
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
                MessageBox.Show("出力フォルダがありません");
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
    }
}
