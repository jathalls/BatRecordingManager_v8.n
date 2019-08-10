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
    ///     Class for specific Report By Bats using the ReportMaster
    /// </summary>
    internal class ReportByBats : ReportMaster
    {
        public BulkObservableCollection<ReportData> reportDataList { get; set; } =
            new BulkObservableCollection<ReportData>();

        /// <summary>
        ///     Specific TabItem label for this report type
        /// </summary>
        public override string tabHeader { get; } = "Bats";

        /// <summary>
        ///     Specific SetData for this report type which configures the data and establishes the DataGrid for this format
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public override void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList,
            BulkObservableCollection<RecordingSession> reportSessionList,
            BulkObservableCollection<Recording> reportRecordingList)
        {
            var headersWritten = new List<string>();
            var isHeaderWritten = false;
            reportDataList.Clear();
            foreach (var batStats in reportBatStatsList)
            {
                if (batStats == null) continue;
                foreach (var session in reportSessionList)
                {
                    if (session == null) continue;
                    isHeaderWritten = headersWritten.Contains(session.SessionTag);
                    var allStatsForSession = session.GetStats();
                    if (!allStatsForSession.IsNullOrEmpty())
                    {
                        if (batStats.bat != null)
                        {
                            var thisBatStatsForSession = from bs in allStatsForSession
                                where bs.batCommonName == batStats.bat.Name
                                select bs;
                            if (!thisBatStatsForSession.IsNullOrEmpty())
                            {
                                var statsForAllSessions = new BatStats();
                                foreach (var bs in thisBatStatsForSession) statsForAllSessions.Add(bs);

                                foreach (var recording in reportRecordingList.Distinct())
                                {
                                    if (recording == null)
                                    {
                                        continue;
                                    }

                                    if (recording.RecordingSession.Id == session.Id)
                                    {
                                        var allStatsForRecording = recording.GetStats();
                                        if (allStatsForRecording != null && allStatsForRecording.Count > 0)
                                        {
                                            var thisBatStatsForRecording = from bs in allStatsForRecording
                                                where bs.batCommonName == batStats.Name
                                                select bs;
                                            if (!thisBatStatsForRecording.IsNullOrEmpty())
                                            {
                                                if (statsForAllSessions.passes > 0 &&
                                                    thisBatStatsForRecording.First().passes > 0)
                                                {
                                                    var reportData = new ReportData();
                                                    if (!isHeaderWritten)
                                                    {
                                                        reportData.sessionHeader = SetHeaderText(session);
                                                        headersWritten.Add(session.SessionTag);
                                                        isHeaderWritten = true;
                                                    }

                                                    reportData.bat = batStats.bat;
                                                    reportData.session = session;
                                                    reportData.sessionStats = statsForAllSessions;
                                                    reportData.recording = recording;

                                                    reportData.recordingStats = thisBatStatsForRecording.First();
                                                    reportDataList.Add(reportData);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var tmp = new BulkObservableCollection<ReportData>();
            tmp.AddRange(reportDataList.Distinct());
            reportDataList = tmp;

            CreateTable();
            ReportDataGrid.ItemsSource = reportDataList;
        }

        private void CreateTable()
        {
            DataGridTextColumn column;
            column = CreateColumn("Session", "sessionHeader", Visibility.Hidden, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Bat", "bat.Name", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Session", "session.SessionTag", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Location", "session.Location", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Start Date", "session.SessionDate", Visibility.Visible, "ShortDate_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Start Time", "session.SessionStartTime", Visibility.Visible, "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("End Time", "session.SessionEndTime", Visibility.Visible, "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Passes in Session", "sessionStats.passes", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Mean Length", "sessionStats.meanDuration", Visibility.Visible,
                "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Total Length", "sessionStats.totalDuration", Visibility.Visible,
                "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Recording", "recording.RecordingName", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Start Time", "recording.RecordingStartTime", Visibility.Visible,
                "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("End Time", "recording.RecordingEndTime", Visibility.Visible, "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Passes", "recordingStats.passes", Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);
            column = CreateColumn("Total Length", "recordingStats.totalDuration", Visibility.Visible,
                "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
        }
    }
}