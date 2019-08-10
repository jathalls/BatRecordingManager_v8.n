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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Specific case of ReportMaster for displaying and exporting reports
    ///     organised on the basis of Recordings
    /// </summary>
    internal class ReportByRecordings : ReportMaster
    {
        /// <summary>
        /// BySession data list for cross reference
        /// </summary>
        //public BulkObservableCollection<ReportData> reportDataBySessionList { get; set; } = new BulkObservableCollection<ReportData>();

        /// <summary>
        ///     main report data list for this instance
        /// </summary>
        public BulkObservableCollection<RecordingReportData> reportDataList { get; set; } =
            new BulkObservableCollection<RecordingReportData>();

        /// <summary>
        ///     Specific header for the tab containing this report type
        /// </summary>
        public override string tabHeader { get; } = "Recordings";

        /// <summary>
        ///     Specific instance of data initialization and configuration for this report type
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public override void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList,
            BulkObservableCollection<RecordingSession> reportSessionList,
            BulkObservableCollection<Recording> reportRecordingList)
        {
            //reportDataBySessionList.Clear();
            HeaderTextBox.Text = "";
            reportDataList.Clear();
            DataContext = this;
            var recnum = 0;
            //string lastTag = null;
            var sessionList = new List<int>();
            foreach (var session in reportSessionList)
            {
                if (session == null) continue;
                var sessionHeaderAdded = false;
                var allStatsForSession = session.GetStats();
                if (!allStatsForSession.IsNullOrEmpty())
                {
                    foreach (var batStats in reportBatStatsList)
                    {
                        if (batStats == null) continue;
                        if (batStats.bat != null)
                        {
                            var thisBatStatsForSession = from bs in allStatsForSession
                                where bs.batCommonName == batStats.bat.Name
                                select bs;
                            if (!thisBatStatsForSession.IsNullOrEmpty())
                            {
                                var statsForAllSessions = new BatStats();
                                foreach (var bs in thisBatStatsForSession) statsForAllSessions.Add(bs);
                                sessionList.Add(recnum);
                                foreach (var recording in reportRecordingList)
                                {
                                    if (recording == null) continue;
                                    if (recording.RecordingSession.Id == session.Id)
                                    {
                                        var allSTatsForRecording = recording.GetStats();
                                        var thisBatStatsForRecording = from bs in allSTatsForRecording
                                            where bs.batCommonName == batStats.Name
                                            select bs;
                                        if (!thisBatStatsForRecording.IsNullOrEmpty())
                                        {
                                            if (statsForAllSessions.passes > 0 &&
                                                thisBatStatsForRecording.First().passes > 0)
                                            {
                                                //ReportData reportData = new ReportData();
                                                var recordingReportData = new RecordingReportData();

                                                //reportData.bat = batStats.bat;
                                                //recordingReportData.bat = batStats.bat;
                                                //reportData.session = session;
                                                //reportData.sessionStats = statsForAllSessions;
                                                //reportData.recording = recording;
                                                //reportData.recordingStats = thisBatStatsForRecording.First();
                                                //reportDataBySessionList.Add(reportData);
                                                if (!sessionHeaderAdded)
                                                {
                                                    recordingReportData.sessionHeader = SetHeaderText(session);
                                                    sessionHeaderAdded = true;
                                                }
                                                else
                                                {
                                                    recordingReportData.sessionHeader = "";
                                                }

                                                recordingReportData.recording = recording;
                                                recordingReportData.bat = batStats.bat;
                                                recordingReportData.session = session;
                                                recordingReportData.sessionStats = statsForAllSessions;

                                                recordingReportData.recordingStats = thisBatStatsForRecording.First();
                                                reportDataList.Add(recordingReportData);

                                                recnum++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (reportDataList != null)
            {
                var tmpList = new BulkObservableCollection<RecordingReportData>();
                tmpList.AddRange(reportDataList.OrderBy(recrepdata => recrepdata.recording.RecordingName));
                reportDataList = tmpList;
            }

            CreateTable();

            ReportDataGrid.ItemsSource = reportDataList;
        }

        /// <summary>
        ///     Creates the relevant columns in the DataGrid and assigns the relevant bindings
        /// </summary>
        private void CreateTable()
        {
            DataGridTextColumn column;
            column = CreateColumn("Session", "sessionHeader", Visibility.Hidden, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Recording", "recording.RecordingName", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Latitude", "recording.RecordingGPSLatitude", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Longitude", "recording.RecordingGPSLongitude", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Bat", "bat.Name", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Start Time", "recording.RecordingStartTime", Visibility.Visible,
                "ShortTime_Converter");

            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("End Time", "recording.RecordingEndTime", Visibility.Visible, "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Passes", "recordingStats.passes", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Total length", "recordingStats.totalDuration", Visibility.Visible,
                "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
        }
    }
}