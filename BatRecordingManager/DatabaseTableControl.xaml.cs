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
using System.ComponentModel;
using System.Windows.Controls;

namespace BatRecordingManager
{
    public partial class DatabaseTableControl : UserControl
    {
        public DatabaseTableControl()
        {
            InitializeComponent();
        }

        public void SortByColumn(string columnName)
        {
        }

        private void DatabaseTableDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var columnName = e.Column.Header as string;
            if (e.Column.SortDirection != null && e.Column.SortDirection.Value == ListSortDirection.Descending)
                columnName = columnName + " descending";
            SortByColumn(columnName);
        }
    }

    public class RecordingSessionTableControl : DatabaseTableControl
    {
        /// <summary>
        ///     default constructor
        /// </summary>
        public RecordingSessionTableControl()
        {
            DatabaseTableDataGrid.DataContext = VirtualizedCollectionOfRecordingSession;
            VirtualizedCollectionOfRecordingSession.AddRange(DBAccess.GetAllRecordingSessions());
        }

        public BulkObservableCollection<RecordingSession> VirtualizedCollectionOfRecordingSession { get; set; } =
            new BulkObservableCollection<RecordingSession>();

        /// <summary>
        ///     string to be used in Linq sortby query
        /// </summary>
        public new void SortByColumn(string name)
        {
            //VirtualizedCollectionOfRecordingSession.sortColumn = name;
        }
    }

    public class RecordingTableControl : DatabaseTableControl
    {
        public RecordingTableControl()
        {
            //RecordingProvider recordingProvider = new RecordingProvider(100, 0);
            //if (recordingProvider != null)
            //{
            //    VirtualizedCollectionOfRecording = new AsyncVirtualizingCollection<Recording>(recordingProvider, 100, 0);
            //}
            //Debug.WriteLine(VirtualizedCollectionOfRecording.Count+" elements in List of Recording");

            DataContext = VirtualizedCollectionOfRecording;
            VirtualizedCollectionOfRecording.AddRange(DBAccess.GetAllRecordings());
            //Debug.WriteLine("Data Context for Recordings set");
            //VirtualizedCollectionOfRecording = new AsyncVirtualizingCollection<Recording>(recordingProvider, 100, 0);
            //Debug.WriteLine(VirtualizedCollectionOfRecording.Count + " elements in List of Recording after setting conext");
            //Debug.WriteLine(VirtualizedCollectionOfRecording[0].RecordingName);
        }

        public BulkObservableCollection<Recording> VirtualizedCollectionOfRecording { get; set; } =
            new BulkObservableCollection<Recording>();
    }
}