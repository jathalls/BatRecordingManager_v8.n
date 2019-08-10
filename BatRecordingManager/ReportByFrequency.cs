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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Derived class tailoring the report as a report by frequency
    ///     while using the ReportMaster TextBox/DataGrid layout and support
    ///     functions.
    /// </summary>
    public class ReportByFrequency : ReportMaster
    {
        /// <summary>
        ///     BOC to hold the formatted Frequency data for display and export
        /// </summary>
        public BulkObservableCollection<FrequencyData> reportDataList { get; set; } =
            new BulkObservableCollection<FrequencyData>();

        private BulkObservableCollection<Bat> batList { get; set; }=new BulkObservableCollection<Bat>();

        /// <summary>
        ///     Read only string for the label in the tab to identify this report type
        /// </summary>
        public override string tabHeader { get; } = "Frequencies";

        /// <summary>
        ///     SetData is passed the full set of report data in the form of three lists and uses whatever is necessary
        ///     to format and populate this particular datagrid.  It overrides the abstract function in reportMaster.
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public override void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList,
            BulkObservableCollection<RecordingSession> reportSessionList,
            BulkObservableCollection<Recording> reportRecordingList)
        {
            ReportDataGrid.DataContext = this;
            //var binding = new Binding("reportDataList");

            //binding.Source = new FrequencyData(10,new Bat(),new BulkObservableCollection<int>());
            //ReportDataGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
            var bats = (from b in reportBatStatsList
                select b.bat).Distinct();
            batList.AddRange(bats);
                 var aggregationPeriod = 10;
            reportDataList = SetFrequencyData(aggregationPeriod, reportSessionList);
            CreateFrequencyTable();
            ReportDataGrid.ItemsSource = reportDataList;
        }

        /// <summary>
        ///     Creates a single column in the reportDataGridByFrequency Datagrid which will hold the list of bats
        ///     in the reportDataGridByFrequencyList to which the grid is already bound.
        ///     Plus an invisible first column which will hold the session tags and notes
        /// </summary>
        /// <returns></returns>
        private DataGridColumn CreateBatColumn()
        {
            var headerColumn = new DataGridTextColumn
            {
                Header = "Sessions", Binding = new Binding("sessionHeader"), Visibility = Visibility.Hidden
            };
            ReportDataGrid.Columns.Add(headerColumn);

            var batColumn = new DataGridTextColumn {Header = "Bat", Binding = new Binding("bat.Name")};
            //var a=reportDataByFrequencyList.First().OccurrencesPerPeriod
            return batColumn;
        }

        /// <summary>
        ///     creates the datagrid for the frequency of occurrence data stored in reportDataByFrequencyList.
        ///     The table has a column for bat species and a column for each aggregation period in 24 hours running
        ///     from noon to noon.  Cells are bound to the reportDataByFrequencyList.
        /// </summary>
        private void CreateFrequencyTable()
        {
            ReportDataGrid.Columns.Clear();
            if (reportDataList == null || reportDataList.Count <= 0) return;
            var aggregationPeriod = reportDataList.FirstOrDefault().AggregationPeriod;
            var day = new TimeSpan(24, 0, 0);
            var minutesInDay = (int) day.TotalMinutes;

            ReportDataGrid.Columns.Add(CreateBatColumn());

            for (var i = 0; i < reportDataList.FirstOrDefault().OccurrencesPerPeriod.Count; i++)
                ReportDataGrid.Columns.Add(CreateOccurrencesColumn(aggregationPeriod, i));
        }

        /// <summary>
        ///     Generates each column in the occurrences array list
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private DataGridColumn CreateOccurrencesColumn(int aggregationperiod, int i)
        {
            var time = new TimeSpan(12, aggregationperiod * i, 0);
            var strTime = $"{time.Hours}:{time.Minutes}";
            var valueColumn = new DataGridTextColumn
            {
                Header = strTime, Binding = new Binding("OccurrencesPerPeriod[" + i + "]")
            };


            return valueColumn;
        }

        /// <summary>
        ///     Assuming aggregation in Aggregationperiods from midday to midday, get the indeces into the array of aggreagtion
        ///     values for the start abd stop times in the recordingperiod.
        /// </summary>
        /// <param name="aggregationPeriod"></param>
        /// <param name="recordingPeriod"></param>
        /// <returns></returns>
        private Tuple<int, int> GetAggregationIndeces(int aggregationPeriod, Tuple<DateTime, DateTime> recordingPeriod)
        {
            var startIndex = 0;
            var periods = 1440 / aggregationPeriod;
            startIndex = GetAggregationIndex(aggregationPeriod, recordingPeriod.Item1);
            periods = (int) ((recordingPeriod.Item2 - recordingPeriod.Item1).TotalMinutes / aggregationPeriod);
            if (periods <= 0) periods = 1;
            return new Tuple<int, int>(startIndex, periods);
        }

        /// <summary>
        ///     Return the index into a 144 element array corresponding to the time given
        /// </summary>
        /// <param name="aggregationPeriod"></param>
        /// <param name="recordingTime"></param>
        /// <returns></returns>
        private int GetAggregationIndex(int aggregationPeriod, DateTime recordingTime)
        {
            //TimeSpan day = new TimeSpan(24, 0, 0);
            //int minutesInDay = (int)day.TotalMinutes;
            var recordingTimeInMinutes = (int) (recordingTime.TimeOfDay.TotalMinutes - 720);
            if (recordingTimeInMinutes < 0) recordingTimeInMinutes += 1440;
            var index = (int) Math.Floor(recordingTimeInMinutes / (decimal) aggregationPeriod);
            return index;
        }

        /// <summary>
        ///     Accumulates frequency data per bat from the speified session into the list of FrequencyData
        /// </summary>
        /// <param name="session"></param>
        /// <param name="result"></param>
        private BulkObservableCollection<FrequencyData> GetFrequencyDataForSession(RecordingSession session,
            BulkObservableCollection<FrequencyData> result)
        {
            // example data for LGL18-2am_20180620
            Debug.WriteLine("GetFrequencyData for " + session.SessionTag);
            if (result == null || !(result.Count > 0)) return result;
            Tuple<DateTime, DateTime> recordingPeriod;
            var aggregationPeriod = result.FirstOrDefault().AggregationPeriod;
            var aggregationTimeSpan = new TimeSpan(0, aggregationPeriod, 0);

            recordingPeriod = DBAccess.GetRecordingPeriod(session); //20/6/2018 22:04 - 21/6/2018 06:07:28
            if (recordingPeriod.Item2 < recordingPeriod.Item1)
            {
                var reversed = new Tuple<DateTime, DateTime>(recordingPeriod.Item2, recordingPeriod.Item1);
                recordingPeriod = reversed;
                // since the recording session has a negative or zero length
            }

            recordingPeriod =
                FrequencyData.NormalizePeriod(aggregationPeriod,
                    recordingPeriod); // 20/6/2018 22:00 - 21/6/2018 06:10:00
            if ((recordingPeriod.Item2 - recordingPeriod.Item1).TotalDays > 30) return (result);// quit if we have an excessively long period i.e. greater than one month
            var indexForRecordingStartAndNumberOfPeriods =
                GetAggregationIndeces(aggregationPeriod, recordingPeriod); //60,49
            DateTime sampleStart;
            var i = 0;
            var periodsPerDay = result[0].OccurrencesPerPeriod.Count;
            for (sampleStart = recordingPeriod.Item1;
                i <= indexForRecordingStartAndNumberOfPeriods.Item2;
                i++, sampleStart = sampleStart + aggregationTimeSpan) // 21:50-22:00, 22:00-22:10, 22:10-22:20
            {
                Debug.WriteLine("Period " + sampleStart.ToShortTimeString());
                //foreach(var batData in result)
                foreach (var t in result)
                {
                    Debug.WriteLine("Bat=" + t.bat.Name);
                    try
                    {
                        t.OccurrencesPerPeriod[
                                (i + indexForRecordingStartAndNumberOfPeriods.Item1) % periodsPerDay] +=
                            DBAccess.GetOccurrencesInWindow(session, t.bat, sampleStart, aggregationPeriod);
                        //0; 3,2,1 - CP
                        //1, 0,2,0 - P50
                        //2, 0,3,1 - SP
                        //3, 0,0,2 - DB
                    }
                    catch (IndexOutOfRangeException iorex)
                    {
                        Debug.WriteLine(iorex.Message);
                        Tools.ErrorLog("From GetFrequencyDataForSession - Index out of range error [" +
                                       (i + indexForRecordingStartAndNumberOfPeriods.Item1) % periodsPerDay +
                                       "] period-" + aggregationPeriod + " :-");
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Creates a frequency of occurrence data set for a number of specified recording sessions
        ///     FrequencyData is a row in the report, corresponding to one type of bat.  It has a bat, a
        ///     sessionHeader and an array of Occurrences/Period
        /// </summary>
        /// <param name="reportSessionList"></param>
        /// <returns></returns>
        private BulkObservableCollection<FrequencyData> SetFrequencyData(int aggregationPeriod,
            BulkObservableCollection<RecordingSession> reportSessionList)
        {
            var day = new TimeSpan(24, 0, 0);
            var minutesInDay = (int) day.TotalMinutes;
            var numberOfAggregationPeriods = (int) Math.Floor(minutesInDay / (decimal) aggregationPeriod);
            var result = new BulkObservableCollection<FrequencyData>();

            if (reportSessionList != null && !reportSessionList.IsNullOrEmpty())
            {
                
               // var batList = DBAccess.GetBatsForTheseSessions(reportSessionList);
                if (batList != null)
                    // First establishes a row for each bat and fills every Occurrences/Period across the row with 0
                    foreach (var bat in batList)
                    {
                        var data = new FrequencyData(aggregationPeriod, bat, null);
                        data.OccurrencesPerPeriod.Clear();
                        for (var i = 0; i < numberOfAggregationPeriods; i++) data.OccurrencesPerPeriod.Add(0);
                        result.Add(data);
                    }

                foreach (var session in reportSessionList)
                    // GetFrequencyDataForSession accumulates the number of occurrences into the pre-created result table
                    result = GetFrequencyDataForSession(session, result);
            }

            HeaderTextBox.Text = "";
            foreach (var session in reportSessionList)
            {
                var data = new FrequencyData(1, null, null) {sessionHeader = SetHeaderText(session)};
                result.Add(data);
            }

            return result;
        }
    }
}