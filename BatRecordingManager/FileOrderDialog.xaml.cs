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

using System.Windows;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for FileOrderDialog.xaml
    /// </summary>
    public partial class FileOrderDialog : Window
    {
        /// <summary>
        ///     Default Constructor
        /// </summary>
        public FileOrderDialog()
        {
            InitializeComponent();
            DataContext = this;
            fileList = new BulkObservableCollection<string>();
            FileListBox.ItemsSource = new BulkObservableCollection<string>();
        }

        /// <summary>
        ///     Returns the list of strings displayed in the dialog
        /// </summary>
        /// <returns>
        /// </returns>
        internal BulkObservableCollection<string> GetFileList()
        {
            return fileList;
        }

        /// <summary>
        ///     Populates the list box with the supplied list of strings
        /// </summary>
        /// <param name="list">
        /// </param>
        internal void Populate(BulkObservableCollection<string> list)
        {
            fileList = list;
        }

        /// <summary>
        ///     Allows the user to select additional files to add to the existing file list
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void ADDButton_Click(object sender, RoutedEventArgs e)
        {
            var additionalFileBrowser = new FileBrowser();
            additionalFileBrowser.SelectLogFiles();
            if (!additionalFileBrowser.TextFileNames.IsNullOrEmpty())
                // some additional names have been chosen
                foreach (var file in additionalFileBrowser.TextFileNames)
                    if (!fileList.Contains(file))
                        fileList.Add(file);
        }

        /// <summary>
        ///     Causes the selected file name to be deleted from the file list
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void DELButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
                if (fileList.Contains((string) FileListBox.SelectedItem))
                    fileList.Remove((string) FileListBox.SelectedItem);
            FileListBox.Items.Refresh();
        }

        /// <summary>
        ///     Moves the selected item down one place in the list
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void DOWNButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
                if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedIndex < FileListBox.Items.Count - 1)
                {
                    var selectedIndex = FileListBox.SelectedIndex;
                    var temp = fileList[selectedIndex];
                    fileList[selectedIndex] = fileList[selectedIndex + 1];
                    fileList[selectedIndex + 1] = temp;
                    FileListBox.Items.Refresh();
                    FileListBox.SelectedIndex = selectedIndex + 1;
                }
        }

        /// <summary>
        ///     Responds to the OK button by closing the dialog and returning true
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        ///     Moves the selected item one place up in the list
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void UPButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
                if (FileListBox.SelectedIndex > 0 && FileListBox.SelectedIndex < fileList.Count)
                {
                    var selectedIndex = FileListBox.SelectedIndex;
                    var temp = fileList[selectedIndex];
                    fileList[selectedIndex] = fileList[selectedIndex - 1];
                    fileList[selectedIndex - 1] = temp;
                    FileListBox.Items.Refresh();
                    FileListBox.SelectedIndex = selectedIndex - 1;
                }
        }
        //private BulkObservableCollection<String> fileList;

        #region fileList

        /// <summary>
        ///     fileList Dependency Property
        /// </summary>
        public static readonly DependencyProperty fileListProperty =
            DependencyProperty.Register("fileList", typeof(BulkObservableCollection<string>), typeof(FileOrderDialog),
                new FrameworkPropertyMetadata(new BulkObservableCollection<string>()));

        /// <summary>
        ///     Gets or sets the fileList property. This dependency property indicates ....
        /// </summary>
        public BulkObservableCollection<string> fileList
        {
            get => (BulkObservableCollection<string>) GetValue(fileListProperty);
            set => SetValue(fileListProperty, value);
        }

        #endregion fileList
    }
}