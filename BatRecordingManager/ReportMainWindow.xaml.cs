/*
 *  Copyright 2016 Justin A T Halls

        Licensed under the Apache License, Version 2.0 (the "License");
        you may not use this file except in compliance with the License.
        You may obtain a copy of the License at

            http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS,
        WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        See the License for the specific language governing permissions and
        limitations under the License.

 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.VisualStudio.Language.Intellisense;
using Mm.ExportableDataGrid;

namespace BatRecordingManager
{
    public class RecordingReportData : ReportData

    {
    }

    /// <summary>
    ///     Class to hold rate of incidence data for all encountered species of bats.  Each Item
    ///     contains an instance of a bat species, and an array of values corresponding to incrmental
    ///     time periods throughout the session.  The values range from 0 to the number of minutes in
    ///     the aggregation period.
    /// </summary>
    public class FrequencyData
    {
        /// <summary>
        /// </summary>
        /// <param name="aggregationPeriod"></param>
        /// <param name="bat"></param>
        /// <param name="occurrencesPerPeriod"></param>
        public FrequencyData(int aggregationPeriod, Bat bat, BulkObservableCollection<int> occurrencesPerPeriod)
        {
            AggregationPeriod = aggregationPeriod;
            var periods = (int) Math.Floor(1440.0m / aggregationPeriod);
            this.bat = bat;
            if (occurrencesPerPeriod != null)
                OccurrencesPerPeriod = occurrencesPerPeriod;
            else
                OccurrencesPerPeriod = new BulkObservableCollection<int>();
            while (OccurrencesPerPeriod.Count < periods) OccurrencesPerPeriod.Add(0);
        }

        /// <summary>
        ///     The size of the periods into which the session will be divided in minutes
        /// </summary>
        public int AggregationPeriod { get; set; }

        /// <summary>
        ///     The species of bat to which this instance of Frequency daya relates
        /// </summary>
        public Bat bat { get; set; }

        /// <summary>
        ///     A list of all the aggregation periods in the recording session, each containing
        ///     the number of minutes that contained the specified type of bat
        /// </summary>
        public BulkObservableCollection<int> OccurrencesPerPeriod { get; set; } = new BulkObservableCollection<int>();

        public string sessionHeader { get; set; } = "";

        /// <summary>
        ///     given a period in the form of two dateTimes, adjusts those values to start and end at exact
        ///     multiples of the Aggregation period in seconds, starting at midday;
        /// </summary>
        /// <param name="aggregationPeriod"></param>
        /// <param name="recordingPeriod"></param>
        internal static Tuple<DateTime, DateTime> NormalizePeriod(int aggregationPeriod, Tuple<DateTime, DateTime> rp)
        {
            var result = rp;
            var normalizedItem1 = false;
            var normalizedItem2 = false;
            for (var dt = new DateTime();
                dt < new DateTime() + new TimeSpan(24, 0, 0);
                dt = dt + new TimeSpan(0, aggregationPeriod, 0))
            {
                //from midnight for 24 hours insteps of AggregationPeriod
                if (!normalizedItem1 && rp.Item1.TimeOfDay < dt.TimeOfDay)
                {
                    // have gone past start time
                    result = new Tuple<DateTime, DateTime>(
                        rp.Item1.Date + (dt.TimeOfDay - new TimeSpan(0, aggregationPeriod, 0)), result.Item2);
                    normalizedItem1 = true;
                }

                if (!normalizedItem2 && rp.Item2.TimeOfDay < dt.TimeOfDay)
                {
                    result = new Tuple<DateTime, DateTime>(result.Item1, rp.Item2.Date + dt.TimeOfDay);
                    normalizedItem2 = true;
                }

                if (normalizedItem1 && normalizedItem2) return result;
            }

            return result;
        }
    }

    /************************************************************************************************************************************/
    /**************************************************END FREQUENCY DATA CLASS**********************************************************/
    /************************************************************************************************************************************/

    /// <summary>
    ///     Interaction logic for ReportMainWindow.xaml
    /// </summary>
    public partial class ReportMainWindow : Window
    {
        /// <summary>
        ///     Base window for a displayable dialog/control to organise and select data to be exported to a report form
        ///     in the form of an excel compatible .csv file.  The selected data is passed from a parent form in which
        ///     bats, sessions and recordings have been identified and selected to be included in the report.  This dialog
        ///     allows the selection of the precise data to be included and the order of the columns.  Various default schemes
        ///     are presented in a tabbed main form but these can be modified by removing columns or by rearranging the order
        ///     of the columns.
        ///     External access is always by SetReportData(...); ShowDialog();
        /// </summary>
        public ReportMainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private BulkObservableCollection<BatStatistics> ReportBatStatsList { get; set; } =
            new BulkObservableCollection<BatStatistics>();

        private BulkObservableCollection<RecordingSession> ReportSessionList { get; set; } =
            new BulkObservableCollection<RecordingSession>();

        private BulkObservableCollection<Recording> ReportRecordingList { get; set; } =
            new BulkObservableCollection<Recording>();
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public BulkObservableCollection<ReportData> reportDataByBatList { get; set; } =
            new BulkObservableCollection<ReportData>();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public BulkObservableCollection<ReportData> reportDataBySessionList { get; set; } =
            new BulkObservableCollection<ReportData>();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public BulkObservableCollection<RecordingReportData> reportDataByRecordingList { get; set; } =
            new BulkObservableCollection<RecordingReportData>();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// </summary>
        public BulkObservableCollection<FrequencyData> reportDataByFrequencyList { get; set; } =
            new BulkObservableCollection<FrequencyData>();


        /// <summary>
        ///     Define the data for the report to be generated and populate the purpose defined class instances so that
        ///     it will be displayed in the DataGrid in the format to be in the csv file when export is clicked.
        ///     The user may change the order and contents in the display.
        /// </summary>
        /// <param name="reportBatList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public void SetReportData(List<BatStatistics> reportBatList, List<RecordingSession> reportSessionList,
            List<Recording> reportRecordingList)
        {
            using (new WaitCursor("Set report data"))
            {
                Debug.WriteLine(reportRecordingList.ToString());

                // Generic operations to set up the data
                ReportBatStatsList.Clear();
                ReportSessionList.Clear();
                ReportRecordingList.Clear();

                if (reportBatList != null)
                {
                    ReportBatStatsList = new BulkObservableCollection<BatStatistics>();
                    ReportBatStatsList.AddRange(reportBatList.Distinct());
                }

                if (reportSessionList != null)
                {
                    ReportSessionList = new BulkObservableCollection<RecordingSession>();
                    ReportSessionList.AddRange(reportSessionList.Distinct());
                }

                if (reportRecordingList != null)
                {
                    ReportRecordingList = new BulkObservableCollection<Recording>();
                    ReportRecordingList.AddRange(reportRecordingList.Distinct());
                }

                // Set data for the Test Frequency Tab
                foreach (var tabitem in MainWindowTabControl.Items)
                    if ((tabitem as TabItem).Content is ReportMaster)
                    {
                        var tabReportMaster = (tabitem as TabItem).Content as ReportMaster;
                        tabReportMaster.SetData(ReportBatStatsList, ReportSessionList, ReportRecordingList);
                        (tabitem as TabItem).Header = tabReportMaster.tabHeader;
                    }


                SortSessionHeaders();
            }
        }


        private void SortSessionHeaders()
        {
        }

        /// <summary>
        ///     Exports the data in the currently selected mode to a file in csv format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportTabButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                SortSessionHeaders();

                dialog.Filter = "csv file|*.csv|all files|*.*";
                dialog.Title = "Export to .csv File";
                dialog.ShowDialog();
                if (!string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    if (File.Exists(dialog.FileName))
                    {
                        if (File.Exists(dialog.FileName + ".bak")) File.Delete(dialog.FileName + ".bak");
                        File.Move(dialog.FileName, dialog.FileName + ".bak");
                    }

                    var filename = dialog.FileName;
                    if (string.IsNullOrWhiteSpace(filename)) return;
                    filename = StripExtension(filename);

                    using (new WaitCursor("Exporting report data"))
                    {
                        if (sender != null)
                        {
                            var selectedTab = MainWindowTabControl.SelectedItem as TabItem;
                            ExportTabItem(selectedTab, filename + ".csv");
                        }
                        else
                        {
                            foreach (var tab in MainWindowTabControl.Items)
                                if (!filename.EndsWith(".csv"))
                                {
                                    var tabItem = tab as TabItem;
                                    ExportTabItem(tabItem, filename + tabItem.Header + ".csv");
                                }
                                else
                                {
                                    filename = StripExtension(filename);
                                    var tabItem = tab as TabItem;
                                    ExportTabItem(tabItem, filename + tabItem.Header + ".csv");
                                }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     removes the extension if any from the string, assuming the string to be a fully qualified
        ///     file name
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string StripExtension(string filename)
        {
            if (filename.Contains(".")) filename = filename.Substring(0, filename.LastIndexOf("."));
            return filename;
        }

        private void ExportTabItem(TabItem tab, string filename)
        {
            var exporter = new CsvExporter(',');
            (tab.Content as ReportMaster)?.Export(exporter, filename);
        }

        private void ByRecordingTab_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void ReportDataGridByRecording_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            SortSessionHeaders();
        }

        private void ByFrequencyTab_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void ReportDataGridByFrequency_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
        }

        internal void ExportAll()
        {
            ExportTabButton_Click(null, new RoutedEventArgs());
        }
    }

    /// <summary>
    ///     Dedicated class to hold the specific data for a report in which many fields may be
    ///     duplicated
    /// </summary>
    public class ReportData
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Bat bat { get; set; } = new Bat();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public RecordingSession session { get; set; } = new RecordingSession();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public BatStats sessionStats { get; set; } = new BatStats();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Recording recording { get; set; } = new Recording();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public BatStats recordingStats { get; set; } = new BatStats();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        public string sessionHeader { get; set; } = "";

        public string GridRef
        {
            get
            {
                var gridRef = "";
                if (recording != null && !string.IsNullOrWhiteSpace(recording.RecordingGPSLatitude) &&
                    !string.IsNullOrWhiteSpace(recording.RecordingGPSLongitude))
                {
                    var latitude = 200.0d;
                    var longitude = 200.0d;
                    recording.GetGpSasDouble(out latitude, out longitude);
                    gridRef = GPSLocation.ConvertGPStoGridRef(latitude, longitude);
                }

                if (string.IsNullOrWhiteSpace(gridRef))
                {
                    gridRef = GPSLocation.ConvertGPStoGridRef((double) session.LocationGPSLatitude,
                        (double) session.LocationGPSLongitude);
                    if (string.IsNullOrWhiteSpace(gridRef))
                        Debug.WriteLine("No grid ref found for session " + session.SessionTag);
                    else
                        gridRef = gridRef + "*";
                }

                return gridRef;
            }
        }

        public string status { get; set; } = "Bat Detector/Recorder";
    }
}