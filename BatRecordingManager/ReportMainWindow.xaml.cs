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
            var periods = (int) Math.Floor(1440.0m / aggregationPeriod);// periods per daya
            this.bat = bat;
            OccurrencesPerPeriod = occurrencesPerPeriod ?? new BulkObservableCollection<int>();// set internal array to that provided or an empty one
            while (OccurrencesPerPeriod.Count < periods) OccurrencesPerPeriod.Add(0); // pad the internal array to the correct size if necessary
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
        /// returns a new, empty example of Frequency data configured for the named bat
        /// </summary>
        /// <param name="aggregationPeriodInMinutes"></param>
        /// <param name="bat"></param>
        /// <returns></returns>
        internal static FrequencyData CreateEmpty(int aggregationPeriodInMinutes, Bat bat)
        {
            FrequencyData fd=new FrequencyData(aggregationPeriodInMinutes,bat,null);
            for (int i = 0; i < fd.OccurrencesPerPeriod.Count; i++)
            {
                fd.OccurrencesPerPeriod[i] = 0;
            }

            return (fd);
        }

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
        /// 
        /// </summary>
        public BulkObservableCollection<ReportData> reportSummaryList { get; set; }=new BulkObservableCollection<ReportData>();


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
            }

            if (reportRecordingList != null)
            {
                ReportRecordingList = new BulkObservableCollection<Recording>();
                ReportRecordingList.AddRange(reportRecordingList.Where(rrl => rrl != null).Distinct());
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


            SortSessionHeaders();

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
/// <summary>
/// the bat to be reported
/// </summary>
        public Bat bat { get; set; } = new Bat();

/// <summary>
/// the session to be reported
/// </summary>
        public RecordingSession session { get; set; } = new RecordingSession();

/// <summary>
/// the stats for the bat to be reported
/// </summary>
        public BatStats sessionStats { get; set; } = new BatStats();

/// <summary>
/// the recording to be reported
/// </summary>
        public Recording recording { get; set; } = new Recording();

/// <summary>
/// the stats for the bat and recording to be reported
/// </summary>
        public BatStats recordingStats { get; set; } = new BatStats();

/// <summary>
/// the header for the report
/// </summary>
        public string sessionHeader { get; set; } = "";

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

    public string passesAndSegments
    {
        get
        {
            if (recordingStats != null)
            {
                return (recordingStats.passes.ToString() + "/" + recordingStats.segments.ToString());
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
                if (recording != null && !string.IsNullOrWhiteSpace(recording.RecordingGPSLatitude) &&
                    !string.IsNullOrWhiteSpace(recording.RecordingGPSLongitude))
                {
                    recording.GetGpSasDouble(out var latitude, out var longitude);
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