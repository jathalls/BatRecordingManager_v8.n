using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    internal class ReportBySessions : ReportMaster
    {
        public BulkObservableCollection<ReportData> reportDataList { get; set; } =
            new BulkObservableCollection<ReportData>();

        /// <summary>
        ///     Label for the hosting TabItem
        /// </summary>
        public override string tabHeader { get; } = "Sessions";

        /// <summary>
        ///     Override of generic SetData in order to configure the data correctly for this specific
        ///     report type and insert it into the DataGrid and TextBox.
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public override void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList,
            BulkObservableCollection<RecordingSession> reportSessionList,
            BulkObservableCollection<Recording> reportRecordingList)
        {
            reportDataList.Clear();
            HeaderTextBox.Text = "";

            var recnum = 0;

            var sessionList = new List<int>();

            foreach (var session in reportSessionList)
            {
                var isHeaderWritten = false;
                var allStatsForSession = session.GetStats();
                if (!allStatsForSession.IsNullOrEmpty())
                    foreach (var batStats in reportBatStatsList)
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
                                    if (recording.RecordingSession.Id == session.Id)
                                    {
                                        var allSTatsForRecording = recording.GetStats();
                                        var thisBatStatsForRecording = from bs in allSTatsForRecording
                                            where bs.batCommonName == batStats.Name
                                            select bs;
                                        if (!thisBatStatsForRecording.IsNullOrEmpty())
                                            if (statsForAllSessions.passes > 0 &&
                                                thisBatStatsForRecording.First().passes > 0)
                                            {
                                                var reportData = new ReportData();
                                                var recordingReportData = new RecordingReportData();

                                                reportData.bat = batStats.bat;
                                                recordingReportData.bat = batStats.bat;
                                                reportData.session = session;
                                                reportData.sessionStats = statsForAllSessions;
                                                reportData.recording = recording;
                                                reportData.recordingStats = thisBatStatsForRecording.First();

                                                if (!isHeaderWritten)
                                                {
                                                    reportData.sessionHeader = SetHeaderText(session);
                                                    isHeaderWritten = true;
                                                }

                                                reportDataList.Add(reportData);
                                                recnum++;
                                            }
                                    }
                            }
                        }
            }

            CreateTable();

            ReportDataGrid.ItemsSource = reportDataList;
        }

        /// <summary>
        ///     Creates the requisite columns in the DataGrid
        /// </summary>
        private void CreateTable()
        {
            DataGridTextColumn column;
            column = CreateColumn("Session", "sessionHeader", Visibility.Hidden, "");
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
            column = CreateColumn("Bat", "bat.Name", Visibility.Visible, "");
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
            column = CreateColumn("Total length", "recordingStats.totalDuration", Visibility.Visible,
                "ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);
        }
    }
}