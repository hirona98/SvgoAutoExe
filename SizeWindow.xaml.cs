using System.Windows;
using System.Windows.Input;

namespace SvgoAutoExe
{
    /// <summary>
    /// SizeWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SizeWindow : Window
    {
        public SizeWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// テキストをセットする
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text)
        {
            Dispatcher.Invoke(() =>
            {
                TextBlockFileSize.Text = text;
            });
        }

        /// <summary>
        /// ファイル容量ウインドウのドラッグ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SizeWindowMove(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

    }
}
