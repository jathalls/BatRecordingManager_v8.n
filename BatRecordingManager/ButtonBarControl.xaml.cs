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
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ButtonBarControl.xaml
    /// </summary>
    public partial class ButtonBarControl : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ButtonBarControl" /> class.
        /// </summary>
        public ButtonBarControl()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Adds the custom button.
        /// </summary>
        /// <param name="label">
        ///     The label.
        /// </param>
        /// <param name="index">
        ///     The index.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        /// </returns>
        public Button AddCustomButton(string label, int index, string name)
        {
            var newButton = new Button { Name = name, Style = (Style)FindResource("SimpleButton"), Content = label };
            if (index < 0) index = 0;
            if (index > ButtonPanel.Children.Count) index = ButtonPanel.Children.Count;
            ButtonPanel.Children.Insert(index, newButton);
            //ButtonPanel.Children.Add(newButton);
            return newButton;
        }
    }
}