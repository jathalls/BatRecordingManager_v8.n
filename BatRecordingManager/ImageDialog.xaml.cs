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
    ///     Interaction logic for ImageDialog.xaml
    /// </summary>
    public partial class ImageDialog : Window
    {
        /// <summary>
        /// </summary>
        public new bool? DialogResult;

        /// <summary>
        ///     base level constructor, instantiates the imageDialogControl and sets up the OKbutton
        ///     event handler.
        /// </summary>
        public ImageDialog()
        {
            InitializeComponent();
            if (ImageDialogControl != null)
            {
                ImageDialogControl.SetImageDialogControl("", "");
                ImageDialogControl.e_OkButtonClicked += ImageDialogControl_OKButtonClicked;
            }
        }

        /// <summary>
        ///     Modified constructor which sets a caption and description for the image
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="description"></param>
        public ImageDialog(string caption, string description) : this()
        {
            ImageDialogControl?.SetImageDialogControl(caption, description);
        }

        internal StoredImage GetStoredImage()
        {
            return ImageDialogControl.storedImage;
        }

        /// <summary>
        ///     Dialog OK button handler responds to event generated in the imageDialoGControl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageDialogControl_OKButtonClicked(object sender, EventArgs e)
        {
            Visibility = Visibility.Visible;
            base.DialogResult = true;
            Close();
        }
    }
}