﻿// *  Copyright 2016 Justin A T Halls
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

using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Core;

namespace BatRecordingManager
{
    
    /// <summary>
    ///     Interaction logic for MapControl.xaml
    /// </summary>
    public partial class MapControl : UserControl
    {
        private Location _coordinates;

        private MapMode LabelMode { get; set; } = null;
        private MapMode AerialMode { get; set; } = null;
        private MapMode RoadMode { get; set; } = null;

        public string BingMapsApiKey { get; } = APIKeys.BingMapsLicenseKey;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapControl" /> class.
        /// </summary>
        public MapControl()
        {
            InitializeComponent();
            DataContext = this;
            ThisMap.CredentialsProvider=new ApplicationIdCredentialsProvider(APIKeys.BingMapsLicenseKey);
            ThisMap.Focus();
            lastInsertedPinLocation = null;
           LabelMode = ThisMap.Mode;
           
           
        }

        /// <summary>
        ///     Gets or sets the coordinates.
        /// </summary>
        /// <value>
        ///     The coordinates.
        /// </value>
        public Location coordinates
        {
            get => _coordinates;
            set
            {
                _coordinates = value;
                ThisMap.Center = value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Location lastInsertedPinLocation { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Adds the push pin.
        /// </summary>
        /// <param name="pinCoordinates">
        ///     The pin coordinates.
        /// </param>
        /// <param name="text">
        ///     The text.
        /// </param>
        public void AddPushPin(Location pinCoordinates, string text)
        {
            var pin = new Pushpin {Location = pinCoordinates, Content = text,PositionOrigin = PositionOrigin.BottomCenter};
            ThisMap.Children.Add(pin);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void AddPushPin(Location location)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            //var pin = new Pushpin {Location = location,PositionOrigin = PositionOrigin.BottomCenter};
            var pin=new Pushpin();
            pin.PositionOrigin = PositionOrigin.BottomCenter;
            pin.Location = location;
            
            
            ThisMap.Children.Add(pin);
        }

        private void mapControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ThisMap.Children.Clear();
            var mousePosition = e.GetPosition(ThisMap);
            var pinLocation = ThisMap.ViewportPointToLocation(mousePosition);

            var pin = new Pushpin {PositionOrigin = PositionOrigin.BottomCenter};
            pin.Location = pinLocation;
            lastInsertedPinLocation = pinLocation;
            ThisMap.Children.Add(pin);
        }

        private void AerialButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            
            ThisMap.Mode = new AerialMode();
        }

        private void AerialLabelButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            
            ThisMap.Mode = new AerialMode(true);
        }

        private void RoadButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            
            ThisMap.Mode = new RoadMode();
        }
    }
}