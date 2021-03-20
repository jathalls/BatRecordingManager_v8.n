using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UniversalToolkit;

namespace BatCallAnalysisControlSet
{
    public class AxisData
    {
        public AxisData(Color foregroundColor, string Title)
        {
            this.ForegroundColor = foregroundColor;
            this.Title = Title;
        }

        public Color ForegroundColor { get; set; }
        public String Title { get; set; }
    }

    /// <summary>
    /// Interaction logic for SfEfChart.xaml
    /// </summary>
    public partial class SfEfChart : UserControl
    {
        public ColorsCollection colorsCollection = new ColorsCollection();

        public SfEfChart()
        {
            colorsCollection = new ColorsCollection();
            colorsCollection.Add(Colors.Blue);
            colorsCollection.Add(Colors.LightGreen);
            colorsCollection.Add(Colors.Cyan);
            colorsCollection.Add(Colors.LightCoral);

            InitializeComponent();
            BaseValues = new ObservableCollection<ChartValues<double>>();
            TopValues = new ObservableCollection<ChartValues<double>>();
            callCursors = new ObservableCollection<CallCursor>();
            DataContext = this;
        }

        public ObservableCollection<ChartValues<double>> BaseValues { get; set; }
        public ObservableCollection<CallCursor> callCursors { get; set; }

        /// <summary>
        /// String formatter for the Y-Axis labels.
        /// </summary>
        public Func<double, string> Formatter { get; set; }

        /// <summary>
        /// View holder for the bar labels, i.e. the species names to be displayed.  Bound to the chart X-axis
        /// </summary>
        public string[] Labels { get; set; }

        public ObservableCollection<ChartValues<double>> TopValues { get; set; }

        internal void selectTheme(string v)
        {
        }

        /// <summary>
        /// Takes a set of ParameterValues and displays them as floating columns in a graph.
        /// If CommonAxis is tru then there will be a single y-axis for all the data, otherwise
        /// separate axes will be used for each parameter;
        /// </summary>
        /// <param name="parameterList"></param>
        /// <param name="CommonAxis"></param>
        internal List<ParameterGroup> SetReferenceData(List<ParameterGroup> parameterList,
            List<SeriesMetadata> seriesMetadataList, List<AxisData> axisData, bool sort)
        {
            int numberOfParameters = seriesMetadataList?.Count ?? 0;
            int numberOfColumns = numberOfParameters + 1;

            for (int c = 0; c < numberOfColumns; c++)
            {
                BaseValues.Add(new ChartValues<double>());
                TopValues.Add(new ChartValues<double>());
                if (c < numberOfParameters)
                {
                    callCursors.Add(seriesMetadataList[c].cursors);
                }
            }

            List<ParameterGroup> sortedList = new List<ParameterGroup>();
            Labels = new string[parameterList.Count * (numberOfParameters + 1)];
            int i = 0;
            if (sort)
            {
                sortedList = (from pg in parameterList
                              where pg.batLabel[0] != "Cursor" && isInRange(callCursors, pg) < numberOfParameters

                              select pg).OrderBy(p => isInRange(callCursors, p)).ThenBy(p => variance(p, parameterList[0].value_mean)).ToList(); ;
            }
            else
            {
                sortedList = parameterList;
            }

            sortedList = sortedList.Take(15).ToList();

            foreach (var call in sortedList)
            {
                if (call.batLabel[0] == "Cursor") continue;
                for (int pNumber = 0; pNumber < numberOfParameters; pNumber++)
                {
                    for (int col = 0; col < numberOfColumns; col++)
                    {
                        var baseVal = col == pNumber ? call.value_min[pNumber] : 0.0d;
                        var topVal = col == pNumber ? call.value_max[pNumber] : 0.0d;
                        BaseValues[col].Add(baseVal);
                        TopValues[col].Add(topVal - baseVal);
                    }
                    Labels[i++] = call.batLabel[pNumber];
                }
                for (int col = 0; col < numberOfColumns; col++)
                {
                    BaseValues[col].Add(0.0);
                    TopValues[col].Add(0.0);
                }
                Labels[i++] = "";
                Debug.WriteLine($"{call.batCommonName}");
            }

            //Labels = new[] { "one", "two", "three" };

            //Labels = labelList.ToArray<string>();

            Formatter = value => value + " ";

            createGraphs(parameterList, seriesMetadataList, axisData);

            createSections(parameterList, seriesMetadataList, axisData);

            DataContext = this;

            return (sortedList.ToList());
        }

        /// <summary>
        /// Generates the stackedColumn series holding the graph data for display and adds them to the underlying
        /// chart
        /// </summary>
        /// <param name="parameterList"></param>
        private void createGraphs(List<ParameterGroup> parameterList, List<SeriesMetadata> seriesMetadataList, List<AxisData> axisData)
        {
            sfEfStackedBarChart.Series.Clear();

            SetAxisData(axisData);

            for (int i = 0; i < seriesMetadataList.Count; i++)
            {
                StackedColumnSeries series = new StackedColumnSeries();

                Binding binding = new Binding();
                binding.Path = new PropertyPath($"BaseValues[{i.ToString()}]");
                series.SetBinding(StackedColumnSeries.ValuesProperty, binding);
                series.Stroke = new SolidColorBrush(Colors.Transparent);
                series.Fill = new SolidColorBrush(Colors.Transparent);
                series.ColumnPadding = -1;
                series.Title = "";

                sfEfStackedBarChart.Series.Add(series);

                StackedColumnSeries series2 = new StackedColumnSeries();
                Binding binding2 = new Binding();
                binding2.Path = new PropertyPath($"TopValues[{i.ToString()}]");
                series2.SetBinding(StackedColumnSeries.ValuesProperty, binding2);

                var seriesColor = new LinearGradientBrush();
                seriesColor.StartPoint = new Point(0.5, 0.0);
                seriesColor.EndPoint = new Point(0.5, 1.0);

                seriesColor.GradientStops.Add(
                    new GradientStop(Colors.White, 0.0));

                seriesColor.GradientStops.Add(
                    new GradientStop(seriesMetadataList[i].SeriesColor, 0.5));

                seriesColor.GradientStops.Add(
                    new GradientStop(Colors.White, 1.0));

                series2.Stroke = new SolidColorBrush(seriesMetadataList[i].SeriesColor);
                series2.Fill = seriesColor;
                series2.ColumnPadding = -1;
                series2.Title = seriesMetadataList[i].Title;

                sfEfStackedBarChart.Series.Add(series2);

                if (axisData.Count > i)
                {
                    series.ScalesYAt = i;
                    series2.ScalesYAt = i;
                }
            }
        }

        private void createSections(List<ParameterGroup> parameterList, List<SeriesMetadata> seriesMetadataList, List<AxisData> axisData)
        {
            yAxis.Sections.Clear();
            for (int i = 0; i < seriesMetadataList.Count; i++)
            {
                Debug.WriteLine($"{axisData[0].Title}={callCursors[i].cursor_min},{callCursors[i].cursor_mean},{callCursors[i].cursor_max}");
                AxisSection mean_section = new AxisSection();
                Binding binding = new Binding();
                binding.Path = new PropertyPath($"callCursors[{i.ToString()}].cursor_mean");
                binding.FallbackValue = 20.0d;
                mean_section.SetBinding(AxisSection.ValueProperty, binding);
                mean_section.Stroke = new SolidColorBrush(seriesMetadataList[i].SeriesColor);
                mean_section.StrokeThickness = 2.0d;
                mean_section.Fill = new SolidColorBrush(seriesMetadataList[i].SeriesColor);

                AxisSection range_section = new AxisSection();
                Binding binding2 = new Binding();
                binding2.Path = new PropertyPath($"callCursors[{i.ToString()}].cursor_min");
                binding2.FallbackValue = 20.0d;
                range_section.SetBinding(AxisSection.ValueProperty, binding2);
                Binding binding4 = new Binding();
                binding4.Path = new PropertyPath($"callCursors[{i.ToString()}].cursor_range");
                binding4.FallbackValue = 10.0d;
                range_section.SetBinding(AxisSection.SectionWidthProperty, binding4);
                SolidColorBrush brush = new SolidColorBrush(seriesMetadataList[i].SeriesColor);
                brush.Opacity = 0.4;
                range_section.Fill = brush;

                if (axisData.Count > i)
                {
                    sfEfStackedBarChart.AxisY[i].Sections.Add(range_section);
                    sfEfStackedBarChart.AxisY[i].Sections.Add(mean_section);
                }
                else
                {
                    sfEfStackedBarChart.AxisY[0].Sections.Add(range_section);
                    sfEfStackedBarChart.AxisY[0].Sections.Add(mean_section);
                }
            }
        }

        /// <summary>
        /// Checks to see if all three cursors are in the rnage of the bat values
        /// </summary>
        /// <param name="callCursor"></param>
        /// <param name="pg"></param>
        /// <returns></returns>
        private int isInRange(ObservableCollection<CallCursor> callCursor, ParameterGroup pg)
        {
            int fails = 0;
            for (int i = 0; i < pg.value_max.Length; i++)
            {
                if (callCursor[i].cursor_mean != 0.0d)
                {
                    if (callCursor[i].cursor_mean < pg.value_min[i] || callCursor[i].cursor_mean > pg.value_max[i]) fails++;
                }
            }
            return (fails);
        }

        /// <summary>
        /// Sets the parameters for the various yAxes listed in axisData
        /// </summary>
        /// <param name="axisData"></param>
        private void SetAxisData(List<AxisData> axisData)
        {
            sfEfStackedBarChart.AxisY.Clear();
            bool first = true;
            foreach (var data in axisData)
            {
                Axis axis = new Axis();
                axis.Foreground = new SolidColorBrush(data.ForegroundColor);
                axis.Title = data.Title;
                if (!first)
                {
                    axis.Position = AxisPosition.RightTop;
                }
                first = false;
                sfEfStackedBarChart.AxisY.Add(axis);
            }
        }

        private double variance(ParameterGroup pg, double[] cursorValues)
        {
            double sumDiffs = 0.0d;

            for (int i = 0; i < cursorValues.Length; i++)
            {
                sumDiffs += Math.Pow(pg.value_mean[i] - cursorValues[i], 2);
                sumDiffs += Math.Pow(pg.value_max[i] - cursorValues[i], 2);
                sumDiffs += Math.Pow(pg.value_min[i] - cursorValues[i], 2);
                Debug.Write($"\t{pg.value_mean[i]:####.#}");
            }
            var variance = Math.Sqrt(sumDiffs);

            Debug.WriteLine($"\t=\t{variance:##.#}");

            return (variance);
        }
    }
}