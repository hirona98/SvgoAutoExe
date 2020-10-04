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
    /// PreviewWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PreviewWindow : Window
    {
        private readonly MainWindow mainWindow;

        public PreviewWindow(MainWindow mw)
        {
            mainWindow = mw;
            InitializeComponent();
        }

        private void PreviewWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            mainWindow.ButtonPreview.IsChecked = false;
        }
    }
}
