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

using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for NewTagForm.xaml
    /// </summary>
    public partial class NewTagForm : Window
    {
        /// <summary>
        ///     The tag text
        /// </summary>
        public string TagText;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NewTagForm" /> class.
        /// </summary>
        public NewTagForm()
        {
            InitializeComponent();
            TagText = "";
            TagTextBox.Focus();
        }

        /// <summary>
        ///     Handles the Click event of the Button control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TagTextBox.Text))
            {
                TagText = TagTextBox.Text;
                var tag = DBAccess.GetTag(TagText);
                if (tag != null)
                {
                    MessageBox.Show("Tag Already defined for " + tag.Bat.Name, "Tag <" + TagText + "> In Use");
                    return;
                }

                DialogResult = true;
                Close();
            }
        }
    }
}