using System.IO;
using System.Windows;

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
