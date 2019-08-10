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
    ///     Interaction logic for Database.xaml
    /// </summary>
    public partial class Database : Window
    {
        public Database()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Scans through all RecordingSessions and Recordings and ensures that the
        /// end time is later than the start time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FixTimesButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                DBAccess.FixSessionAndRecordingTimes();
            }
        }

        /// <summary>
        /// Checks each recording that does not have location data and if so tries to find it in
        /// either metadata for the recording or a related GPX file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FixMDataButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                DBAccess.FixRecordingLocationData();
            }
        }
    }
}