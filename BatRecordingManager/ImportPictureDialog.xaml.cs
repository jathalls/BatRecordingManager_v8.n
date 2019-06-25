// *  Copyright 2016 Justin A T Halls
//  *
//  *  This file is part of the Bat Recording Manager Project
// 
//         Licensed under the Apache License, Version 2.0 (the "License");
//         you may not use this file except in compliance with the License.
//         You may obtain a copy of the License at
// 
//             http://www.apache.org/licenses/LICENSE-2.0
// 
//         Unless required by applicable law or agreed to in writing, software
//         distributed under the License is distributed on an "AS IS" BASIS,
//         WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//         See the License for the specific language governing permissions and
//         limitations under the License.

using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ImportPictureDialog.xaml
    /// </summary>
    public partial class ImportPictureDialog : Window
    {
        /// <summary>
        ///     Allows the import picture control to be used in a stand-alone dialog mode
        /// </summary>
        public ImportPictureDialog()
        {
            ImportPictureControl = Activator.CreateInstance<ImportPictureControl>();
            InitializeComponent();
        }

        /// <summary>
        /// </summary>
        /// <param name="fileName"></param>
        internal void SetCaption(string fileName)
        {
            ImportPictureControl.SetCaption(fileName);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            DBAccess.ResolveOrphanImages();
            /*
             * NB this code only required if the window is to stay open and the content needs to
             * be refreshed after the update to the database.
             *
            importPictureControl.imageEntryScroller.Clear();
            BulkObservableCollection<StoredImage> orphanImages = DBAccess.GetOrphanImages(null);
            if (!orphanImages.IsNullOrEmpty())
            {
                foreach (var image in orphanImages)
                {
                    importPictureControl.imageEntryScroller.AddImage(image);
                }
            }*/
            Close();
        }
    }
}