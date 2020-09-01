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

using Microsoft.Maps.MapControl.WPF;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MapWindow.xaml
    /// </summary>
    public partial class MapWindow : Window
    {
        private readonly bool _isDialog;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapWindow" /> class. The parameter is
        ///     set true if the window is to be displayed using ShowDialog rather than Show so that
        ///     the DialogResult can be set before closing.
        /// </summary>
        /// <param name="isDialog">
        ///     if set to <c>true</c> [is dialog].
        /// </param>
        public MapWindow(bool isDialog)
        {
            _isDialog = isDialog;
            InitializeComponent();
            MapControl.OkButton.Click += OKButton_Click;
        }

        /// <summary>
        ///     Gets or sets the coordinates of the centre of the map window
        /// </summary>
        /// <value>
        ///     The coordinates.
        /// </value>
        public Location Coordinates
        {
            get => MapControl.coordinates;
            set => MapControl.coordinates = value;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public Location lastSelectedLocation
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return MapControl.lastInsertedPinLocation; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDialog) DialogResult = true;
            Close();
        }
    }
}