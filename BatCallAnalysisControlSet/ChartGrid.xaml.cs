using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Invisionware.Settings;
using Invisionware.Settings.Sinks;
using Invisionware.Settings.Overrides;

using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Path = System.IO.Path;
using System.ComponentModel;
using UniversalToolkit;

namespace BatCallAnalysisControlSet
{
    /// <summary>
    /// Interaction logic for ChartGrid.xaml
    /// </summary>
    public partial class ChartGrid : UserControl
    {
        public ChartGrid()
        {
            InitializeComponent();

            chartBase = new ChartBase();

            callForm.callSet += CallForm_callSet;

            ReferenceCall call = callForm.getCallParametersfromSetters();
            showCharts(call);
        }

        public SfEfChart bwChart { get; set; }

        public SfEfChart sfEfChart { get; set; }

        public SfEfChart timesChart { get; set; }

        /// <summary>
        /// set the displayable detailed call data in the display datagrid
        /// </summary>
        /// <param name="displayableData"></param>
        public void SetDisplayableCallData(List<CallData> displayableData)
        {
            callForm.SetDisplayableCallData(displayableData);
        }

        public void showCharts(ReferenceCall cursorCall)
        {
            var sortedList = ShowFrequencyChart(cursorCall);

            ShowTimesChart(sortedList, cursorCall);

            ShowBWChart(sortedList, cursorCall);
        }

        private ChartBase chartBase;

        private void CallForm_callSet(object sender, callEventArgs e)
        {
            var call = e.call;
            showCharts(call);
        }

        private void chartControl_MouseEnter(object sender, MouseEventArgs e)
        {
            topRow.Height = new GridLength(100.0d, GridUnitType.Star);
            BottomRow.Height = new GridLength(100.0d, GridUnitType.Star);
            leftColumn.Width = new GridLength(100.0d, GridUnitType.Star);
            rightColumn.Width = new GridLength(100.0d, GridUnitType.Star);
        }

        private void helpButton_Click(object sender, RoutedEventArgs e)
        {
            var helpfile = @"Bat Call Analyser.chm";
            if (File.Exists(helpfile)) System.Windows.Forms.Help.ShowHelp(null, helpfile);
        }

        private void ShowBWChart(List<ParameterGroup> sortedList, ReferenceCall cursorCall)
        {
            List<SeriesMetadata> seriesMetadataList = new List<SeriesMetadata>();
            List<AxisData> axisData = new List<AxisData>();

            axisData.Clear();
            axisData.Add(new AxisData(Colors.Black, "Frequency khz"));

            int color = 0;
            seriesMetadataList.Add(new SeriesMetadata("Bandwidth", chartBase.colorsCollection[color++],
                cursorCall.bandwidth_Min, cursorCall.bandwidth_Max));
            seriesMetadataList.Add(new SeriesMetadata("Knee Frequency", chartBase.colorsCollection[color++],
                cursorCall.fKnee_Min, cursorCall.fKnee_Max));
            seriesMetadataList.Add(new SeriesMetadata("Heel Frequency", chartBase.colorsCollection[color],
                cursorCall.fHeel_Min, cursorCall.fHeel_Max));

            bwChart = new SfEfChart();
            bwChart.SetReferenceData(chartBase.getBWData(sortedList), seriesMetadataList, axisData, false);
            Label label3 = new Label();
            label3.Content = "Bandwidth, Knee and Heel Frequencies";
            label3.HorizontalAlignment = HorizontalAlignment.Center;
            DockPanel.SetDock(label3, Dock.Top);
            BottomLeftPanel.Children.Clear();
            BottomLeftPanel.Children.Add(label3);
            BottomLeftPanel.Children.Add(bwChart);
        }

        private List<ParameterGroup> ShowFrequencyChart(ReferenceCall cursorCall)
        {
            List<SeriesMetadata> seriesMetadataList = new List<SeriesMetadata>();
            int color = 0;
            seriesMetadataList.Add(new SeriesMetadata("Start Frequency", chartBase.colorsCollection[color++],
                cursorCall.fStart_Min, cursorCall.fStart_Max));
            seriesMetadataList.Add(new SeriesMetadata("Peak Frequency", chartBase.colorsCollection[color++],
                cursorCall.fPeak_Min, cursorCall.fPeak_Max));
            seriesMetadataList.Add(new SeriesMetadata("End Frequency", chartBase.colorsCollection[color],
                cursorCall.fEnd_Min, cursorCall.fEnd_Max));
            sfEfChart = new SfEfChart();
            List<AxisData> axisData = new List<AxisData>();
            AxisData datum = new AxisData(Colors.Black, "Frequency kHz");
            axisData.Add(datum);
            var sortedList = sfEfChart.SetReferenceData(chartBase.getFrequencyData(), seriesMetadataList, axisData, true);

            Label label = new Label();
            label.Content = "Start, Peak, End Frequencies";
            label.HorizontalAlignment = HorizontalAlignment.Center;
            DockPanel.SetDock(label, Dock.Top);
            TopRightPanel.Children.Clear();
            TopRightPanel.Children.Add(label);

            TopRightPanel.Children.Add(sfEfChart);

            return (sortedList);
        }

        private void ShowTimesChart(List<ParameterGroup> sortedList, ReferenceCall cursorCall)
        {
            timesChart = new SfEfChart();
            List<SeriesMetadata> seriesMetadataList = new List<SeriesMetadata>();
            List<AxisData> axisData = new List<AxisData>();

            int color = 0;
            seriesMetadataList.Add(new SeriesMetadata("Duration", chartBase.colorsCollection[color++],
                cursorCall.duration_Min, cursorCall.duration_Max));
            seriesMetadataList.Add(new SeriesMetadata("Interval", chartBase.colorsCollection[color++],
                cursorCall.interval_Min, cursorCall.interval_Max));

            axisData.Clear();
            var datum = new AxisData(chartBase.colorsCollection[0], "Duration ms");
            axisData.Add(datum);
            axisData.Add(new AxisData(Colors.DarkGreen, "Interval ms"));
            timesChart.SetReferenceData(chartBase.getTemporalData(sortedList), seriesMetadataList, axisData, false);

            Label label2 = new Label();
            label2.Content = "Durations and Intervals";
            label2.HorizontalAlignment = HorizontalAlignment.Center;
            DockPanel.SetDock(label2, Dock.Top);
            BottomRightPanel.Children.Clear();
            BottomRightPanel.Children.Add(label2);

            BottomRightPanel.Children.Add(timesChart);
        }

        private void TopLeftPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            topRow.Height = new GridLength(100.0d, GridUnitType.Star);
            BottomRow.Height = new GridLength(100.0d, GridUnitType.Star);
            leftColumn.Width = new GridLength(100.0d, GridUnitType.Star);
            rightColumn.Width = new GridLength(100.0d, GridUnitType.Star);
        }

        private void TopRightPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            topRow.Height = new GridLength(1000.0d, GridUnitType.Star);
            BottomRow.Height = new GridLength(10.0d, GridUnitType.Star);
            leftColumn.Width = new GridLength(10.0d, GridUnitType.Star);
            rightColumn.Width = new GridLength(1000.0d, GridUnitType.Star);
        }
    }
}