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

        public int AggegrationPeriodInMinutes
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

        private int _tableStartTimeInMinutesFromMidnight = 720;

        public int TableStartTimeInMinutesFromMidnight
        {
            get { return (_tableStartTimeInMinutesFromMidnight); }
            set
            {
                if (value <= 0 || value >= 1440)
                {
                    _tableStartTimeInMinutesFromMidnight = 720;
                }
                else
                {
                    int t = AggegrationPeriodInMinutes;
                    while (t < value)
                    {
                        t += AggegrationPeriodInMinutes;

                    }

                    t -= AggegrationPeriodInMinutes;
                    if (t <= 0 || t > 1440) t = 720;
                    _tableStartTimeInMinutesFromMidnight = t;

                }


            }
        }

        private int _numberOfPeriods = 144;

        private int NumberOfPeriods
        {
            get { return (1440 / AggegrationPeriodInMinutes); }
        }

        private BulkObservableCollection<FrequencyData> OccurrencesPerPeriod =
            new BulkObservableCollection<FrequencyData>();



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
                    List<int> OccurrencesPerPeriodForBat = new List<int>();
                    for (int i = 0; i < NumberOfPeriods; i++) OccurrencesPerPeriodForBat.Add(0);
                    var segments = from seg in (reportRecordingList.SelectMany(rec => rec.LabelledSegments))
                        from lnk in seg.BatSegmentLinks
                        where lnk.BatID == bat.Id
                        select seg;
                    foreach (var segment in segments)
                    {
                        List<int> OccupiedPeriodsPerBlock = new List<int>();
                        if (segment.FrequencyContributions(out int FirstBlock, out List<int> occupiedMinutesPerBlock,
                            (double) TableStartTimeInMinutesFromMidnight, AggegrationPeriodInMinutes))
                        {
                            for (int i = FirstBlock, j = 0; i < FirstBlock + occupiedMinutesPerBlock.Count; i++, j++)
                            {
                                if (i >= OccurrencesPerPeriodForBat.Count || j >= occupiedMinutesPerBlock.Count)
                                {
                                    Debug.WriteLine($"Error OUT OF BOUNDS i={i} into OccurrencesPerPeriodForBat of length {OccurrencesPerPeriodForBat.Count},\n" +
                                                    $"j={j} into OccupiedMinutesPerBlock of length {OccupiedPeriodsPerBlock.Count}");
                                    break;
                                }
                                OccurrencesPerPeriodForBat[i] += occupiedMinutesPerBlock[j];
                            }
                        }

                    }

                    BulkObservableCollection<int> boc = new BulkObservableCollection<int>();
                    boc.AddRange(OccurrencesPerPeriodForBat);
                    FrequencyData fd = new FrequencyData(AggegrationPeriodInMinutes, bat, boc);
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
            var aggregationPeriod = AggegrationPeriodInMinutes;
            var day = new TimeSpan(24, 0, 0);
            var minutesInDay = (int) day.TotalMinutes;

            ReportDataGrid.Columns.Add(CreateBatColumn());

            for (var i = 0; i < NumberOfPeriods; i++)
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

        

        

        

        
    }
}