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
using Mm.ExportableDataGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace BatRecordingManager
{
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
            AggregationPeriod = aggregationPeriod == 0 ? 10 : aggregationPeriod;
            var periods = (int)Math.Floor(1440.0m / AggregationPeriod);// periods per daya
            this.bat = bat;
            OccurrencesPerPeriod = occurrencesPerPeriod ?? new BulkObservableCollection<int>();// set internal array to that provided or an empty one

            while (OccurrencesPerPeriod.Count < periods) OccurrencesPerPeriod.Add(0); // pad the internal array to the correct size if necessary


        }

        /// <summary>
        ///     The size of the periods into which the session will be divided in minutes
        /// </summary>
        public int AggregationPeriod { get; set; }

        /// <summary>
        /// The time of sunset for this data set.  Sunset occurs in block 36 of 144
        /// </summary>
        public TimeSpan sunset { get; set; }

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
        /// adds the contents of the provided frequency data to the current one assuming that the bat is the same and the
        /// size of the OccurrencePerPeriod is the same
        /// </summary>
        /// <param name="fdForSession"></param>
        public void Add(FrequencyData fdForSession)
        {
            if (bat.Id == fdForSession.bat.Id && OccurrencesPerPeriod.Count == fdForSession.OccurrencesPerPeriod.Count)
            {
                for (int i = 0; i < OccurrencesPerPeriod.Count; i++)
                {
                    OccurrencesPerPeriod[i] += fdForSession.OccurrencesPerPeriod[i];
                }
            }
        }

        /// <summary>
        /// returns a new, empty example of Frequency data configured for the named bat
        /// </summary>
        /// <param name="aggregationPeriodInMinutes"></param>
        /// <param name="bat"></param>
        /// <returns></returns>
        internal static FrequencyData CreateEmpty(int aggregationPeriodInMinutes, Bat bat)
        {
            FrequencyData fd = new FrequencyData(aggregationPeriodInMinutes, bat, null);
            for (int i = 0; i < fd.OccurrencesPerPeriod.Count; i++)
            {
                fd.OccurrencesPerPeriod[i] = 0;
            }
            fd.sunset = new TimeSpan(0, 18, 0, 0);
            return (fd);
        }
    }

    public class RecordingReportData : ReportData

    {
    }

    /************************************************************************************************************************************/
    /**************************************************END FREQUENCY DATA CLASS**********************************************************/
    /************************************************************************************************************************************/

    /// <summary>
    ///     Dedicated class to hold the specific data for a report in which many fields may be
    ///     duplicated
    /// </summary>
    public class ReportData
    {
        /// <summary>
        /// the bat to be reported
        /// </summary>
        public Bat bat { get; set; } = new Bat();

        /// <summary>
        /// returns the combined GPS location from the session data
        /// </summary>
        public string combinedGPS
        {
            get
            {
                if (session != null)
                {
                    string result = session.LocationGPSLatitude?.ToString() + ", " + session.LocationGPSLongitude?.ToString();
                    if (!string.IsNullOrWhiteSpace(result)) return (result);
                    return (" - ");
                }

                return (" - ");
            }
        }

        /// <summary>
        /// the OS grid ref for the location
        /// </summary>
        public string GridRef
        {
            get
            {
                var gridRef = "";
                if (recording != null &&
                    !string.IsNullOrWhiteSpace(recording.RecordingGPSLatitude) &&
                    !string.IsNullOrWhiteSpace(recording.RecordingGPSLongitude))
                {
                    recording.GetGpSasDouble(out var latitude, out var longitude);
                    if (latitude > 90.0d || latitude < -90.0d || longitude > 180.0d || longitude < -180.0d || (latitude == 0.0d && longitude == 0.0d))
                    {
                        gridRef = GPSLocation.ConvertGPStoGridRef((double)(session.LocationGPSLatitude ?? 0.0m),
                        (double)(session.LocationGPSLongitude ?? 0.0m));
                        if (string.IsNullOrWhiteSpace(gridRef))
                            Debug.WriteLine("No grid ref found for session " + session.SessionTag);
                        else
                            gridRef = gridRef + "*";
                    }
                    else
                    {
                        gridRef = GPSLocation.ConvertGPStoGridRef(latitude, longitude);
                    }
                }

                if (string.IsNullOrWhiteSpace(gridRef))
                {
                    gridRef = GPSLocation.ConvertGPStoGridRef((double)(session.LocationGPSLatitude ?? 0.0m),
                        (double)(session.LocationGPSLongitude ?? 0.0m));
                    if (string.IsNullOrWhiteSpace(gridRef))
                        Debug.WriteLine("No grid ref found for session " + session.SessionTag);
                    else
                        gridRef = gridRef + "*";
                }

                return gridRef;
            }
        }

        public string passesAndSegments
        {
            get
            {
                if (recordingStats != null)
                {
                    return ("'" + recordingStats.passes.ToString() + "/" + recordingStats.segments.ToString());
                }

                return (" - ");
            }
        }

        /// <summary>
        /// the recording to be reported
        /// </summary>
        public Recording recording { get; set; } = new Recording();

        /// <summary>
        /// the stats for the bat and recording to be reported
        /// </summary>
        public BatStats recordingStats { get; set; } = new BatStats();

        /// <summary>
        /// the session to be reported
        /// </summary>
        public RecordingSession session { get; set; } = new RecordingSession();

        public string sessionEndDateTime
        {
            get
            {
                if (session != null)
                {
                    string result = ((session.EndDate ?? session.SessionDate).Date +
                                     (session.SessionEndTime ?? new TimeSpan(23, 59, 0))).ToString();
                    return (result);
                }

                return (" - ");
            }
        }

        /// <summary>
        /// the header for the report
        /// </summary>
        public string sessionHeader { get; set; } = "";

        /// <summary>
        /// returns a combined date and time string for the start of the session
        /// </summary>
        public string sessionStartDateTime
        {
            get
            {
                if (session != null)
                {
                    string result = (session.SessionDate.Date +
                                 (session.SessionStartTime ?? new TimeSpan(18, 0, 0))).ToString();
                    return (result);
                }

                return (" - ");
            }
        }

        /// <summary>
        /// the stats for the bat to be reported
        /// </summary>
        public BatStats sessionStats { get; set; } = new BatStats();

        public string status { get; set; } = "Bat Detector/Recorder";
    }

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



        public BulkObservableCollection<ReportData> reportDataByBatList { get; set; } =
            new BulkObservableCollection<ReportData>();

        /// <summary>
        /// </summary>
        public BulkObservableCollection<FrequencyData> reportDataByFrequencyList { get; set; } =
            new BulkObservableCollection<FrequencyData>();

        public BulkObservableCollection<RecordingReportData> reportDataByRecordingList { get; set; } =
            new BulkObservableCollection<RecordingReportData>();

        public BulkObservableCollection<ReportData> reportDataBySessionList { get; set; } =
            new BulkObservableCollection<ReportData>();

        /// <summary>
        ///
        /// </summary>
        public BulkObservableCollection<ReportData> reportSummaryList { get; set; } = new BulkObservableCollection<ReportData>();

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
            Debug.WriteLine(reportRecordingList.ToString());
            string sessiontag = "";

            // Generic operations to set up the data
            ReportBatStatsList.Clear();
            ReportSessionList.Clear();
            ReportRecordingList.Clear();

            if (reportBatList != null)
            {
                ReportBatStatsList = new BulkObservableCollection<BatStatistics>();
                ReportBatStatsList.AddRange(reportBatList.Where(rbl => rbl != null).Distinct());

            }

            if (reportSessionList != null)
            {
                ReportSessionList = new BulkObservableCollection<RecordingSession>();
                ReportSessionList.AddRange(reportSessionList.Where(rsl => rsl != null).Distinct());
                var reportSession = ReportSessionList.First();
                if (reportSession != null && string.IsNullOrWhiteSpace(sessiontag))
                {
                    sessiontag = reportSession.SessionTag;
                }
            }

            if (reportRecordingList != null)
            {
                ReportRecordingList = new BulkObservableCollection<Recording>();
                ReportRecordingList.AddRange(reportRecordingList.Where(rrl => rrl != null).Distinct());
                var reportRecording = ReportRecordingList.First();
                if (reportRecording != null && string.IsNullOrWhiteSpace(sessiontag))
                {
                    sessiontag = reportRecording.RecordingSession.SessionTag;
                }
            }

            // Set data for the Test Frequency Tab
            foreach (var tabitem in MainWindowTabControl.Items)
                if ((tabitem as TabItem).Content is ReportMaster)
                {
                    var tabReportMaster = (tabitem as TabItem).Content as ReportMaster;
                    if (tabReportMaster is ReportByFrequency)
                    {
                        ReportByFrequency frequencyReport = tabReportMaster as ReportByFrequency;
                        frequencyReport.AggregationPeriodInMinutes = 10;
                        frequencyReport.TableStartTimeInMinutesFromMidnight =
                            720; // indicates to use sunset -6 hours for each session
                        frequencyReport.reSunset = true;
                    }

                    tabReportMaster.SetData(ReportBatStatsList, ReportSessionList, ReportRecordingList);
                    (tabitem as TabItem).Header = tabReportMaster.tabHeader;
                }

            this.sessionTag = sessionTag;

            SortSessionHeaders();
        }

        private string sessionTag { get; set; } = "";

        internal void ExportAll()
        {
            ExportTabButton_Click(null, new RoutedEventArgs());
        }

        private BulkObservableCollection<BatStatistics> ReportBatStatsList { get; set; } =
                                                                    new BulkObservableCollection<BatStatistics>();

        private BulkObservableCollection<Recording> ReportRecordingList { get; set; } =
            new BulkObservableCollection<Recording>();

        private BulkObservableCollection<RecordingSession> ReportSessionList { get; set; } =
                    new BulkObservableCollection<RecordingSession>();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void ByFrequencyTab_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void ByRecordingTab_GotFocus(object sender, RoutedEventArgs e)
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
                SortSessionHeaders();// does nothing
                string tag = "";

                if (!ReportSessionList.IsNullOrEmpty())
                {
                    var sess = ReportSessionList.First();
                    tag = (sess?.SessionTag) ?? "";

                }

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    dialog.FileName = tag;
                }

                dialog.Filter = "csv file|*.csv|all files|*.*";
                dialog.Title = "Export to .csv File";
                dialog.ShowDialog();
                if (!string.IsNullOrWhiteSpace(dialog.FileName))
                {


                    var filename = dialog.FileName;
                    if (string.IsNullOrWhiteSpace(filename)) return;
                    filename = StripExtension(filename);

                    using (new WaitCursor("Exporting report data"))
                    {
                        if (sender != null)
                        {

                            var selectedTab = MainWindowTabControl.SelectedItem as TabItem;

                            filename = filename + "=" + selectedTab.Header;
                            if (!filename.EndsWith(".csv")) filename = filename + ".csv";
                            if (File.Exists(filename))
                            {
                                string backup = Path.ChangeExtension(filename, ".bak");
                                if (File.Exists(backup)) File.Delete(backup);
                                File.Move(filename, backup);
                            }


                            ExportTabItem(selectedTab, filename + ".csv");
                        }
                        else
                        {
                            string basename = filename + "-";
                            foreach (var tab in MainWindowTabControl.Items)
                            {


                                var tabItem = tab as TabItem;
                                if (!e.Handled && tabItem.Header.ToString().Contains("Freq")) continue;

                                filename = basename + tabItem.Header + ".csv";
                                if (File.Exists(filename))
                                {
                                    string backup = Path.ChangeExtension(filename, ".bak");
                                    if (File.Exists(backup)) File.Delete(backup);
                                    File.Move(filename, backup);
                                }

                                ExportTabItem(tabItem, filename);
                            }

                        }
                    }
                }
            }
            e.Handled = true;
        }

        private void ExportTabItem(TabItem tab, string filename)
        {
            var exporter = new CsvExporter(',');
            (tab.Content as ReportMaster)?.Export(exporter, filename);
        }

        private void ReportDataGridByFrequency_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
        }

        private void ReportDataGridByRecording_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            SortSessionHeaders();
        }

        private void SortSessionHeaders()
        {
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

        private void miExportTab_Click(object sender, RoutedEventArgs e)
        {
            ExportTabButton_Click(sender, e);
            e.Handled = true;
        }

        private void miExportAllButFreq_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = false;
            ExportTabButton_Click(null, e);
            e.Handled = true;
        }

        private void miExportAll_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ExportTabButton_Click(null, e);
        }
    }
}