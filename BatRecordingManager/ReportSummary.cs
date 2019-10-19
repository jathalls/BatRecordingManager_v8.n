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
    /// Report Summary class builds a summary report int he standard format
    /// </summary>
    internal class ReportSummary : ReportMaster
    {
        /// <summary>
        /// report data tabulation for display in the datagrid
        /// </summary>
        public BulkObservableCollection<ReportData> reportDataList { get; set; } =
            new BulkObservableCollection<ReportData>();

        /// <summary>
        ///     Label for the hosting TabItem
        /// </summary>
        public override string tabHeader { get; } = "Summary";

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
                if (session == null) continue;
                var isHeaderWritten = false;
                var allStatsForSession = session.GetStats();
                var condensedStats = Tools.CondenseStatsList(allStatsForSession);
                 

                    if (!condensedStats.IsNullOrEmpty())
                    {
                        foreach (var bs in condensedStats)
                        {


                            var reportData = new ReportData();
                            reportData.session = session;
                        
                            reportData.recordingStats = bs;

                            reportDataList.Add(reportData);
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

            
            
            column = CreateColumn("Location", "session.Location", Visibility = Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Session", "session.SessionTag", Visibility = Visibility.Visible, "");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Position", "session", Visibility.Visible, "GPSConverter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("MapRef", "session", Visibility.Visible, "MapRefConverter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Start", "session", Visibility.Visible, "SessionStartDateTimeConverter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("End", "session", Visibility.Visible, "SessionEndDateTimeConverter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Bat", "recordingStats.batCommonName");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Passes/Segments", "recordingStats", Visibility.Visible, "BSPassesConverter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Min", "recordingStats.minDuration",Visibility.Visible,"ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Max", "recordingStats.maxDuration",Visibility.Visible,"ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Mean", "recordingStats.meanDuration",Visibility.Visible,"ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);

            column = CreateColumn("Total", "recordingStats.totalDuration",Visibility.Visible,"ShortTime_Converter");
            ReportDataGrid.Columns.Add(column);

            
        }
    }
}
