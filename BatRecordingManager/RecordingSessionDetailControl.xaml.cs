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
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Maps.MapControl.WPF;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingSessionDetailControl.xaml
    /// </summary>
    public partial class RecordingSessionDetailControl : UserControl
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public RecordingSessionDetailControl()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();
            DataContext = this;
        }

        private void GPSLatitudeTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GPSMapButton_Click(sender, new RoutedEventArgs());
        }

        private void GPSMapButton_Click(object sender, RoutedEventArgs e)
        {
            Location coordinates;
            if (!double.TryParse(GpsLatitudeTextBox.Text, out var lat)) return;
            if (!double.TryParse(GpsLongitudeTextBox.Text, out var longit)) return;
            if (Math.Abs(lat) > 90.0d || Math.Abs(longit) > 180.0d) return;
            coordinates = new Location(lat, longit);

            var mapWindow = new MapWindow(false) {MapControl = {coordinates = coordinates}};
            mapWindow.Show();
            if (recordingSession?.Recordings != null && recordingSession.Recordings.Count > 0)
            {
                var i = 0;
                foreach (var rec in recordingSession.Recordings)
                {
                    i++;
                    if (double.TryParse(rec.RecordingGPSLatitude, out var latitude))
                        if (double.TryParse(rec.RecordingGPSLongitude, out var longitude))
                            if (Math.Abs(latitude) <= 90.0d && Math.Abs(longitude) <= 180.0d)
                                mapWindow.MapControl.AddPushPin(new Location(latitude, longitude), i.ToString());
                }
            }
        }

        #region SelectedSession

        /// <summary>
        ///     recordingSession Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedSessionProperty =
            DependencyProperty.Register(nameof(recordingSession), typeof(RecordingSession),
                typeof(RecordingSessionDetailControl),
                new FrameworkPropertyMetadata(new RecordingSession()));

        private string _gridRef = "";

        public string GridRef
        {
            get => _gridRef;

            set
            {
                if (recordingSession != null)
                {
                    if (recordingSession.LocationGPSLatitude != null)
                    {
                        var lat = (double) recordingSession.LocationGPSLatitude;
                        var longit = (double) recordingSession.LocationGPSLongitude;
                        _gridRef = GPSLocation.ConvertGPStoGridRef(lat, longit);
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets the recordingSession property. This dependency property indicates ....
        /// </summary>
        public RecordingSession recordingSession
        {
            get => (RecordingSession) GetValue(SelectedSessionProperty);
            set
            {
                SetValue(SelectedSessionProperty, value);
                if (value != null)
                {
                    SessionTagTextBlock.Text = value.SessionTag ?? "";
                    //SessionDatePicker.Text = value.SessionDate.ToShortDateString();
                    //StartTimePicker.Text = (value.SessionStartTime ?? new TimeSpan()).ToString();
                    //EndTimePicker.Text = (value.SessionEndTime ?? new TimeSpan()).ToString();

                    SessionStartDateTime.Value = value.SessionDate.Date + (value.SessionStartTime ?? new TimeSpan());

                    if (value.SessionEndTime == null) value.SessionEndTime = value.SessionStartTime ?? new TimeSpan();
                    if (value.EndDate == null)
                        value.EndDate = value.SessionDate.Date +
                                        (value.SessionEndTime ?? (value.SessionStartTime ?? new TimeSpan()));
                    SessionEndDateTime.Value = value.EndDate;

                    SunsetTimePicker.Text = (value.Sunset ?? new TimeSpan()).ToString();
                    TemperatureIntegerUpDown.Text = value.Temp <= 0 ? "" : value.Temp + @"°C";
                    WeatherTextBox.Text = value.Weather ?? "";
                    EquipmentComboBox.Text = value.Equipment ?? "";
                    MicrophoneComboBox.Text = value.Microphone ?? "";
                    OperatorComboBox.Text = value.Operator ?? "";
                    LocationComboBox.Text = value.Location ?? "";
                    if (value.LocationGPSLatitude == null || value.LocationGPSLatitude.Value < -90.0m ||
                        value.LocationGPSLatitude.Value > 90.0m)
                    {
                        GpsLatitudeTextBox.Text = "";
                    }
                    else
                    {
                        GpsLatitudeTextBox.Text = value.LocationGPSLatitude.Value.ToString();

                        GridRefTextBox.Text = GPSLocation.ConvertGPStoGridRef(
                            (double) (value.LocationGPSLatitude ?? 200.0m),
                            (double) (value.LocationGPSLongitude ?? 200.0m));
                    }

                    if (value.LocationGPSLongitude == null || value.LocationGPSLongitude.Value < -180.0m ||
                        value.LocationGPSLongitude.Value > 180.0m)
                        GpsLongitudeTextBox.Text = "";
                    else
                        GpsLongitudeTextBox.Text = value.LocationGPSLongitude.Value.ToString();
                    SessionNotesRichtextBox.Text = value.SessionNotes ?? "";
                }
                else
                {
                    SessionTagTextBlock.Text = "";
                    SessionStartDateTime.Value = null;
                    SessionEndDateTime.Value = null;
                    //SessionDatePicker.Text = "";
                    //StartTimePicker.Text = "";
                    //EndTimePicker.Text = "";
                    TemperatureIntegerUpDown.Text = "";
                    EquipmentComboBox.Text = "";
                    MicrophoneComboBox.Text = "";
                    OperatorComboBox.Text = "";
                    LocationComboBox.Text = "";
                    GpsLatitudeTextBox.Text = "";
                    GpsLongitudeTextBox.Text = "";
                    GridRefTextBox.Text = "";
                    SessionNotesRichtextBox.Text = "";
                }
            }
        }

        #endregion SelectedSession
    }
}