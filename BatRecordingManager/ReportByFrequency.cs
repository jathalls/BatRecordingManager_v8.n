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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
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

        private BulkObservableCollection<Bat> batList { get; set; } = new BulkObservableCollection<Bat>();

        private int _aggregationPeriod = 10;

        public int AggregationPeriodInMinutes
        {
            get { return (_aggregationPeriod); }
            set
            {
                if (value <= 0 || value > 1440)
                {
                    _aggregationPeriod = 10;
                }
                else
                {
                    while (1440 % value > 0)
                    {
                        value++;
                    }

                    _aggregationPeriod = value;
                }


            }
        }

        /// <summary>
        /// boolean to indicate if time of day should be raltive to sunset with table start at sunset -6 hours.
        /// </summary>
        public bool reSunset { get; set; } = false;

        private int _tableStartTimeInMinutesFromMidnight = 720;

        public int TableStartTimeInMinutesFromMidnight
        {
            get { return (_tableStartTimeInMinutesFromMidnight); }
            set
            {

                if (value < 0 || value >= 1440)
                {
                    _tableStartTimeInMinutesFromMidnight = 720;
                }
                else
                {
                    int t = AggregationPeriodInMinutes;
                    while (t < value)
                    {
                        t += AggregationPeriodInMinutes;

                    }

                    t -= AggregationPeriodInMinutes;
                    if (t <= 0 || t > 1440) t = 720;
                    _tableStartTimeInMinutesFromMidnight = t;

                }


            }
        }

        private int _numberOfPeriods = 144;

        private int NumberOfPeriods
        {
            get { return (1440 / AggregationPeriodInMinutes); }
        }

        private BulkObservableCollection<FrequencyData> OccurrencesPerPeriod =
            new BulkObservableCollection<FrequencyData>();

        /// <summary>
        /// public enum defining the possible values for the block sizes in the frequency table
        /// </summary>
        public enum BlockSizeInMinutes
        {
            FIVE = 5,
            TEN = 10,
            FIFTEEN = 15,
            TWENTY = 20
        };


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
            try
            {
                ReportDataGrid.DataContext = this;
                if (reportDataList == null) reportDataList = new BulkObservableCollection<FrequencyData>();
                reportDataList.Clear();
                var binding = new Binding("reportDataList");
                binding.Source = new FrequencyData(10, new Bat(), new BulkObservableCollection<int>());
                ReportDataGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in SetData(Frequency) creating data binding " + ex.Message);
            }


            try
            {
                var bats = (from b in reportBatStatsList
                    select b.bat).Distinct();
                batList.AddRange(bats);

                //CreateFrequencyTable();
                // aggregation period and start time for table and number of periods have been set or defaulted globally
                foreach (var bat in batList)
                {

                    FrequencyData fd = FrequencyData.CreateEmpty(AggregationPeriodInMinutes, bat);

                    foreach (var recordingSession in reportSessionList)
                    {
                        bool sessionHasBat = (
                            from sessbatLnk in recordingSession.BatSessionLinks
                            where sessbatLnk.BatID == bat.Id
                            select sessbatLnk).Any();
                        if (sessionHasBat)
                        {
                            var sunset = recordingSession.Sunset;
                            if (sunset == null || sunset.Value.Ticks<=0L)
                            {
                                sunset = recordingSession.Recordings.FirstOrDefault().sunset;
                            }

                            if (sunset == null)
                            {
                                sunset = SessionManager.CalculateSunset(recordingSession.SessionDate.Date, 51.9178783m,
                                    -1.1448518m);
                            }
                            if (reSunset && recordingSession.Sunset != null)
                            {
                                if (recordingSession.Sunset.Value.TotalMinutes > 720.0d)
                                {
                                    TableStartTimeInMinutesFromMidnight =
                                        (int) recordingSession.Sunset.Value.TotalMinutes - 360;
                                }
                            }

                            FrequencyData fdForSession = GetFrequencyData(bat, recordingSession, reportRecordingList);
                            fd.Add(fdForSession);
                        }
                    }
                    reportDataList.Add(fd);


                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in SetData(Frequency) - processing each bat " + ex.Message);
            }

            try
            {


                CreateFrequencyTable();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in SetData(Frequency) Creating Frequency Table " + ex.Message);
            }

            try
            {
                HeaderTextBox.Text = "";
                foreach (var session in reportSessionList)
                {
                    var data = new FrequencyData(1, null, null) {sessionHeader = SetHeaderText(session)};
                    reportDataList.Add(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in SetData(Frequency) Processing sessions in list for header " + ex.Message);
            }
            finally
            {
                ReportDataGrid.ItemsSource = reportDataList;
            }


            /* original method
                 var aggregationPeriod = 10;
            reportDataList = SetFrequencyData(aggregationPeriod, reportSessionList);
            CreateFrequencyTable();
            ReportDataGrid.ItemsSource = reportDataList;*/
        }

        private FrequencyData GetFrequencyData(Bat bat, RecordingSession recordingSession, BulkObservableCollection<Recording> reportRecordingList)
        {
            FrequencyData fd=FrequencyData.CreateEmpty(AggregationPeriodInMinutes, bat);

            //List<int> OccurrencesPerPeriodForBat = new List<int>();
            //for (int i = 0; i < NumberOfPeriods; i++) OccurrencesPerPeriodForBat.Add(0);

            var segmentsForThisBat = from seg in (reportRecordingList.SelectMany(rec => rec.LabelledSegments))
                                     where seg.Recording.RecordingSession.Id==recordingSession.Id &&
                                           seg.StartOffset!=seg.EndOffset
                                     from lnk in seg.BatSegmentLinks
                                     where lnk.BatID == bat.Id
                                     select seg;


            int firstBlock = GetFirstBlock(segmentsForThisBat);
            int lastBlock = GetLastBlock(segmentsForThisBat);
            for (int i = firstBlock; i <= lastBlock; i++)
            {
                fd.OccurrencesPerPeriod[i] = GetOcccurrencesForBlock(i, segmentsForThisBat);
            }
            

            return (fd);
        }

        /// <summary>
        /// Examines the provided segments to see how many minutes in this block overlap segments in the set
        /// </summary>
        /// <param name="i"></param>
        /// <param name="segmentsForThisBat"></param>
        /// <returns></returns>
        private int GetOcccurrencesForBlock(int blockIndex, IEnumerable<LabelledSegment> segmentsForThisBat)
        {
            int result = 0;
            TimeSpan startTimeForBlock = TimeSpan.FromMinutes( blockIndex * AggregationPeriodInMinutes);
            
            for (int minute = (int)startTimeForBlock.TotalMinutes;
                minute < (int)startTimeForBlock.TotalMinutes + AggregationPeriodInMinutes;
                minute++)
            {
                foreach (var seg in segmentsForThisBat)
                {
                    Debug.WriteLine(
                        $"seg start at {seg.StartTime(TableStartTimeInMinutesFromMidnight)}({seg.StartTime(TableStartTimeInMinutesFromMidnight).TotalMinutes})" +
                        $" end at {seg.EndTime(TableStartTimeInMinutesFromMidnight)}({seg.EndTime(TableStartTimeInMinutesFromMidnight).TotalMinutes})" +
                        $"in minute {minute} ");
                }

                var inseg = (from seg in segmentsForThisBat
                    where (int) (seg.StartTime(TableStartTimeInMinutesFromMidnight).TotalMinutes) <= minute &&
                          (int) (seg.EndTime(TableStartTimeInMinutesFromMidnight).TotalMinutes) >= minute
                    select seg).Any();

                if (inseg)
                {
                    Debug.WriteLine("start<=minute && end>=minute");
                    result++;
                }

            }

            return (result);
        }

        /// <summary>
        /// Finds the earliest segment and locates the first block that includes the start of that segment
        /// </summary>
        /// <param name="segmentsForThisBat"></param>
        /// <returns></returns>
        private int GetFirstBlock(IEnumerable<LabelledSegment> segmentsForThisBat)
        {
            var firstSegmentStart = (from seg in segmentsForThisBat
                orderby seg.StartTime(TableStartTimeInMinutesFromMidnight)
                select seg.StartTime(TableStartTimeInMinutesFromMidnight)).First();
            if (firstSegmentStart.Ticks <= 0)
            {
                return (0);
            }

            var block = (int)firstSegmentStart.TotalMinutes / AggregationPeriodInMinutes;
            return (block);
        }

        private int GetLastBlock(IEnumerable<LabelledSegment> segmentsForThisBat)
        {
            var lastSegmentEnd = (from seg in segmentsForThisBat
                orderby seg.EndTime(TableStartTimeInMinutesFromMidnight) descending
                select seg.EndTime(TableStartTimeInMinutesFromMidnight)).First();
            if (lastSegmentEnd.Ticks <= 0)
            {
                return (0);
            }

            var block = (int)lastSegmentEnd.TotalMinutes / AggregationPeriodInMinutes;
            return (block);
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
            var aggregationPeriod = AggregationPeriodInMinutes;
            var day = new TimeSpan(24, 0, 0);
            var minutesInDay = (int) day.TotalMinutes;

            ReportDataGrid.Columns.Add(CreateBatColumn());

            TimeSpan TableStartTime=TimeSpan.FromMinutes(TableStartTimeInMinutesFromMidnight);
            if (reSunset)
            {
                TableStartTime=TimeSpan.FromMinutes(-360); // if times are re sunset start at sunset -6hours
            }

            TimeSpan columnTime = TableStartTime;
            for (var i = 0; i < NumberOfPeriods; i++)
            {
                ReportDataGrid.Columns.Add(CreateOccurrencesColumn(columnTime, i));
                
                columnTime += TimeSpan.FromMinutes(AggregationPeriodInMinutes);
            }
        }

        /// <summary>
        ///     Generates each column in the occurrences array list
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private DataGridColumn CreateOccurrencesColumn(TimeSpan columnTime, int i)
        {
            //var time = new TimeSpan(12, aggregationperiod * i, 0);
            var strTime = columnTime.ToHMString();
            //Debug.WriteLine(strTime);
            var valueColumn = new DataGridTextColumn
            {
                Header = "'"+strTime, Binding = new Binding("OccurrencesPerPeriod[" + i + "]")
            };


            return valueColumn;
        }

        

        

        

        
    }
}