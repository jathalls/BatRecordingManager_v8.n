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