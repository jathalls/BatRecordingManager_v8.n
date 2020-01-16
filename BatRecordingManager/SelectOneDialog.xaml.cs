using Microsoft.VisualStudio.Language.Intellisense;
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
    /// Interaction logic for SelectOneDialog.xaml
    /// </summary>
    public partial class SelectOneDialog : Window
    {

        public BulkObservableCollection<String> itemList { get; set; } = new BulkObservableCollection<string>();

        protected string selectedItem { get; set; } = "";
        public SelectOneDialog()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public void SetItems(List<String> items)
        {
            if (itemList == null) itemList = new BulkObservableCollection<string>();
            itemList.Clear();
            itemList.AddRange(items);
        }

        private void SelectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectButton != null)
            {
                SelectButton.IsEnabled = true;
                SelectButton.IsDefault = true;
            }
            if (SelectionListBox.SelectedItem == null)
            {
                selectedItem = "";
                if (SelectButton != null)
                {
                    SelectButton.IsEnabled = false;
                }
            }
            else
            {
                selectedItem = SelectionListBox.SelectedItem as string;
                if (SelectButton != null)
                {
                    SelectButton.IsEnabled = true;
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        internal string GetSelectedItem()
        {
            return (selectedItem);
        }
    }
}
