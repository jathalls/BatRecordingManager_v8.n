using System.ComponentModel;
using System.Windows.Controls;
using DataVirtualizationLibrary;

namespace BatRecordingManager
{
    public partial class DatabaseTableControl : UserControl
    {
        public DatabaseTableControl()
        {
            InitializeComponent();
        }

        private void DatabaseTableDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var columnName = e.Column.Header as string;
            if (e.Column.SortDirection != null && e.Column.SortDirection.Value == ListSortDirection.Descending)
                columnName = columnName + " descending";
            SortByColumn(columnName);
        }

        public void SortByColumn(string columnName)
        {
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
        }

        public AsyncVirtualizingCollection<RecordingSession> VirtualizedCollectionOfRecordingSession { get; set; } =
            new AsyncVirtualizingCollection<RecordingSession>(new RecordingSessionProvider(), 25, 100);

        /// <summary>
        ///     string to be used in Linq sortby query
        /// </summary>
        public new void SortByColumn(string name)
        {
            VirtualizedCollectionOfRecordingSession.sortColumn = name;
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
            //Debug.WriteLine("Data Context for Recordings set");
            //VirtualizedCollectionOfRecording = new AsyncVirtualizingCollection<Recording>(recordingProvider, 100, 0);
            //Debug.WriteLine(VirtualizedCollectionOfRecording.Count + " elements in List of Recording after setting conext");
            //Debug.WriteLine(VirtualizedCollectionOfRecording[0].RecordingName);
        }

        public AsyncVirtualizingCollection<Recording> VirtualizedCollectionOfRecording { get; set; } =
            new AsyncVirtualizingCollection<Recording>(new RecordingProvider(), 100, 100);
    }
}