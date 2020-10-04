using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SvgoAutoExe4
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
            TextBlockFileSize.Text = text;
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
