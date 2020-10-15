using System;
using System.Collections.Generic;
using System.IO;
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

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for WebPageWindow.xaml
    /// </summary>
    public partial class WebPageWindow : Window
    {
        public WebPageWindow()
        {
            InitializeComponent();
        }

        public void Fill(string htmlFile)
        {
            if (File.Exists(htmlFile))
            {
                webBrowser.NavigateToString(htmlFile);
            }
        }
    }
}
