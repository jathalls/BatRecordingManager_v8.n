/*
 *  Copyright 2016 Justin A T Halls

        Licensed under the Apache License, Version 2.0 (the "License");
        you may not use this file except in compliance with the License.
        You may obtain a copy of the License at

            http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS,
        WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        See the License for the specific language governing permissions and
        limitations under the License.

 */

using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for FolderSelectionDialog.xaml
    /// </summary>
    public partial class FolderSelectionDialog : Window
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FolderSelectionDialog" /> class.
        /// </summary>
        public FolderSelectionDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var browser = new FileBrowser();
            browser.SelectHeaderTextFile();
            if (browser.WorkingFolder != null && !string.IsNullOrWhiteSpace(browser.WorkingFolder))
                if (Directory.Exists(browser.WorkingFolder))
                    FolderList.Add(browser.WorkingFolder);
        }

        private void AddFolderTreeButton_Click(object sender, RoutedEventArgs e)
        {
            var browser = new FileBrowser();
            browser.SelectRootFolder();
            if (!browser.WavFileFolders.IsNullOrEmpty())
            {
                var combinedList = FolderList.Concat(browser.WavFileFolders).Distinct();
                FolderList = (BulkObservableCollection<string>) combinedList;
            }
        }

        private void ButtonDeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            var thisButton = sender as Button;
            var itemToDelete = ((thisButton.Parent as Grid).Children[1] as TextBox).Text;
            FolderList.Remove(itemToDelete);

            var view = CollectionViewSource.GetDefaultView(FolderListView.ItemsSource);
            view.Refresh();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            var lvi = sender as ListViewItem;
            lvi.IsSelected = true;
        }

        private void OnTextBoxFocused(object sender, RoutedEventArgs e)
        {
            var segmentTextBox = sender as TextBox;
            var myDelButton = (segmentTextBox.Parent as Grid).Children[0] as Button;
            myDelButton.Visibility = Visibility.Visible;
        }

        #region FolderList

        /// <summary>
        ///     FolderList Dependency Property
        /// </summary>
        public static readonly DependencyProperty FolderListProperty =
            DependencyProperty.Register("FolderList", typeof(BulkObservableCollection<string>),
                typeof(FolderSelectionDialog),
                new FrameworkPropertyMetadata(new BulkObservableCollection<string>()));

        /// <summary>
        ///     Gets or sets the FolderList property. This dependency property indicates ....
        /// </summary>
        public BulkObservableCollection<string> FolderList
        {
            get => (BulkObservableCollection<string>) GetValue(FolderListProperty);
            set
            {
                if (!value.IsNullOrEmpty())
                    for (var i = 0; i < value.Count; i++)
                    {
                        var folder = value[i];
                        if (!folder.Contains("###") && DBAccess.FolderExists(folder)) value[i] = folder + " ###";
                    }

                SetValue(FolderListProperty, value);
            }
        }

        #endregion FolderList
    }
}