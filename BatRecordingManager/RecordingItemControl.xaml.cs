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

using Microsoft.VisualStudio.Language.Intellisense;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingItemControl.xaml
    /// </summary>
    public partial class RecordingItemControl : UserControl
    {
        /// <summary>
        ///     The summary
        /// </summary>
        private BulkObservableCollection<BatStats> _summary;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingItemControl" /> class.
        /// </summary>
        public RecordingItemControl()
        {
            InitializeComponent();
            DataContext = RecordingItem;
            GpsLabel.MouseDoubleClick += GPSLabel_MouseDoubleClick;
        }

        /// <summary>
        ///     Deletes the recording.
        /// </summary>
        internal void DeleteRecording()
        {
            if (RecordingItem != null)
            {
                var err = DBAccess.DeleteRecording(RecordingItem);
                if (!string.IsNullOrWhiteSpace(err)) MessageBox.Show(err, "Delete Recording Failed");
            }
        }

        private void GPSLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(GpsLabel.Content as string);
        }

        #region recordingItem

        /// <summary>
        ///     recordingItem Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingItemProperty =
            DependencyProperty.Register(nameof(RecordingItem), typeof(Recording), typeof(RecordingItemControl),
                new FrameworkPropertyMetadata(new Recording()));

        /// <summary>
        ///     Gets or sets the recordingItem property. This dependency property indicates ....
        /// </summary>
        /// <value>
        ///     The recording item.
        /// </value>
        public Recording RecordingItem
        {
            get => (Recording)GetValue(recordingItemProperty);
            set
            {
                SetValue(recordingItemProperty, value);
                _summary = value.GetStats();
                var date = value.RecordingDate ?? value.RecordingSession.SessionDate;
                var strDate = "";
                if (date != null) strDate = date.ToShortDateString();

                RecordingNameLabel.Content = value.RecordingName + " " + Tools.GetRecordingDuration(value);
                if (!string.IsNullOrWhiteSpace(value.RecordingGPSLatitude) &&
                    !string.IsNullOrWhiteSpace(value.RecordingGPSLongitude))
                    GpsLabel.Content = value.RecordingGPSLatitude + ", " + value.RecordingGPSLongitude;
                else
                    GpsLabel.Content = strDate + value.RecordingStartTime + " - " + value.RecordingEndTime;
                RecordingNotesLabel.Content = !string.IsNullOrWhiteSpace(value.RecordingNotes) ? value.RecordingNotes : "";

                BatPassSummaryStackPanel.Children.Clear();
                if (_summary != null && _summary.Count > 0)
                    foreach (var batType in _summary)
                    {
                        var batPassControl = new BatPassSummaryControl { PassSummary = batType };
                        BatPassSummaryStackPanel.Children.Add(batPassControl);
                    }

                LabelledSegmentListView.Items.Clear();
                foreach (var segment in value.LabelledSegments)
                {
                    var labelledSegmentControl = new LabelledSegmentControl { labelledSegment = segment };
                    LabelledSegmentListView.Items.Add(labelledSegmentControl);
                }

                InvalidateArrange();
                UpdateLayout();
            }
        }

        #endregion recordingItem
    }
}