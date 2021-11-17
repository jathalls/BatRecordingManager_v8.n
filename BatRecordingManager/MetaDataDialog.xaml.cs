using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for MetaDataDialog.xaml
    /// </summary>
    public partial class MetaDataDialog : Window
    {
        public MetaDataDialog()
        {
            InitializeComponent();
            DataContext = recording;
        }

        public Recording recording
        {
            get
            {
                return (_recording);
            }
            set
            {
                _recording = value;
                this.DataContext = recording;
            }
        }

        private Recording _recording = new Recording();

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}