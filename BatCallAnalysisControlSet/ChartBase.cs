using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using UniversalToolkit;

namespace BatCallAnalysisControlSet
{
    /// <summary>
    /// A Model for the stacked bar chart view that extracts sets of three numeric parameters from the reference call set
    /// and packages for transfer to a generic triple-stacked bar chart for display
    /// </summary>
    internal class ChartBase
    {
        public ColorsCollection colorsCollection = new ColorsCollection();

        public ChartBase()
        {
            ReferenceCallList = LoadReferenceBats();
            colorsCollection = new ColorsCollection();
            colorsCollection.Add(Colors.Blue);
            colorsCollection.Add(Colors.LightGreen);
            colorsCollection.Add(Colors.Cyan);
            colorsCollection.Add(Colors.LightCoral);
        }

        public List<ParameterGroup> getFrequencyData()
        {
            var result = new List<ParameterGroup>();

            foreach (var call in ReferenceCallList)
            {
                ParameterGroup group = new ParameterGroup(3);
                group.setValue(0, call.fStart_Min, call.fStart_Max);
                group.setValue(1, call.fPeak_Min, call.fPeak_Max);
                group.setValue(2, call.fEnd_Min, call.fEnd_Max);
                group.batLabel[0] = "";
                group.batLabel[1] = call.BatLabel;
                group.batLabel[2] = call.CallType;
                group.batCommonName = call.BatCommonName;
                group.batLatinName = call.BatLatinName;
                result.Add(group);
            }

            return (result);
        }

        /// <summary>
        /// Gets timing data for all bats
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name=""></param>
        /// <returns></returns>
        internal List<ParameterGroup> getBWData(List<ParameterGroup> batList)
        {
            var result = new List<ParameterGroup>();

            var data = from bat in batList
                       from call in ReferenceCallList
                       where bat.batLabel[1] == call.BatLabel && bat.batLabel[2] == call.CallType
                       select getBWParameters(call);

            if (data != null)
            {
                foreach (var datum in data) result.Add(datum);
            }

            return (result);
        }

        /// <summary>
        /// Gets timing data for all bats
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name=""></param>
        /// <returns></returns>
        internal List<ParameterGroup> getTemporalData(List<ParameterGroup> batList)
        {
            var result = new List<ParameterGroup>();

            var data = from bat in batList
                       from call in ReferenceCallList
                       where bat.batLabel[1] == call.BatLabel && bat.batLabel[2] == call.CallType
                       select getTemporalParameters(call);

            if (data != null)
            {
                foreach (var datum in data) result.Add(datum);
            }

            return (result);
        }

        private List<ReferenceCall> ReferenceCallList { get; set; }

        private ParameterGroup getBWParameters(ReferenceCall call)
        {
            ParameterGroup group = new ParameterGroup(3);
            group.setValue(0, call.bandwidth_Min, call.bandwidth_Max);
            group.setValue(1, call.fKnee_Min, call.fKnee_Max);
            group.setValue(2, call.fHeel_Min, call.fHeel_Max);

            group.batLabel[2] = "";
            group.batLabel[0] = call.BatLabel;
            group.batLabel[1] = call.CallType;

            group.batCommonName = call.BatCommonName;
            group.batLatinName = call.BatLatinName;

            return (group);
        }

        private double getMax(IEnumerable<XElement> element)
        {
            double result = 0.0d;
            if (element != null && element.Count() > 0)
            {
                string text = element.FirstOrDefault().Value;
                if (text.Contains(','))
                {
                    var items = text.Split(',');
                    if (items.Count() >= 1)
                    {
                        double first = 0.0d;
                        double.TryParse(items[0], out first);
                        double second = 0.0d;
                        if (items.Count() >= 2)
                        {
                            double.TryParse(items[1], out second);
                        }
                        else
                        {
                            second = first;
                        }
                        result = first + (second * 2.0d);
                    }
                }
                else
                {
                    var items = text.Split('-');
                    if (items.Count() >= 1)
                    {
                        double first = 0.0d;
                        double.TryParse(items[0], out first);
                        double second = 0.0d;
                        if (items.Count() >= 2)
                        {
                            double.TryParse(items[1], out second);
                        }
                        else
                        {
                            second = first;
                        }
                        result = Math.Max(first, second);
                    }
                }
            }

            return (result);
        }

        private double getMin(IEnumerable<XElement> element)
        {
            double result = 0.0d;
            if (element != null && element.Count() > 0)
            {
                string text = element.FirstOrDefault().Value;
                if (text.Contains(','))
                {
                    var items = text.Split(',');
                    if (items.Count() >= 1)
                    {
                        double first = 0.0d;
                        double.TryParse(items[0], out first);
                        double second = 0.0d;
                        if (items.Count() >= 2)
                        {
                            double.TryParse(items[1], out second);
                        }
                        else
                        {
                            second = first;
                        }
                        result = first - (second * 2.0d);
                        if (result < 0.0d) result = 0.1d;
                    }
                }
                else
                {
                    var items = text.Split('-');
                    if (items.Count() >= 1)
                    {
                        double first = 0.0d;
                        double.TryParse(items[0], out first);
                        double second = 0.0d;
                        if (items.Count() >= 2)
                        {
                            double.TryParse(items[1], out second);
                        }
                        else
                        {
                            second = first;
                        }
                        result = Math.Min(first, second);
                    }
                }
            }

            return (result);
        }

        private ParameterGroup getTemporalParameters(ReferenceCall call)
        {
            ParameterGroup group = new ParameterGroup(2);
            group.setValue(0, call.duration_Min, call.duration_Max);
            group.setValue(1, call.interval_Min, call.interval_Max);

            group.batLabel[0] = call.BatLabel;
            group.batLabel[1] = call.CallType;

            group.batCommonName = call.BatCommonName;
            group.batLatinName = call.BatLatinName;

            return (group);
        }

        private List<ReferenceCall> LoadReferenceBats()
        {
            List<ReferenceCall> result = new List<ReferenceCall>();

            if (File.Exists(@".\BatReferenceXMLFileEx.xml"))
            {
                XElement referenceFile = XElement.Load(new FileStream(@".\BatReferenceXMLFileEx.xml", FileMode.Open));

                var calls = referenceFile.Descendants("Call");
                if (calls != null)
                {
                    var referenceCalls = from call in calls
                                         select new ReferenceCall
                                         {
                                             BatLatinName = call.Parent.Descendants("BatGenus")?.FirstOrDefault()?.Value + " " +
                                                          call.Parent.Descendants("BatSpecies")?.FirstOrDefault()?.Value,
                                             BatLabel = call.Parent.Descendants("Label")?.FirstOrDefault()?.Value,
                                             BatCommonName = call.Parent.Descendants("BatCommonName")?.FirstOrDefault()?.Value,
                                             fStart_Max = getMax(call.Elements("fStart")),
                                             fStart_Min = getMin(call.Elements("fStart")),
                                             fEnd_Max = getMax(call.Elements("fEnd")),
                                             fEnd_Min = getMin(call.Elements("fEnd")),
                                             fPeak_Max = getMax(call.Elements("fPeak")),
                                             fPeak_Min = getMin(call.Elements("fPeak")),
                                             interval_Max = getMax(call.Elements("Interval")),
                                             interval_Min = getMin(call.Elements("Interval")),
                                             duration_Max = getMax(call.Elements("Duration")),
                                             duration_Min = getMin(call.Elements("Duration")),
                                             CallType = call.Elements("Type")?.FirstOrDefault()?.Value,
                                             fKnee_Max = getMax(call.Elements("fKnee")),
                                             fKnee_Min = getMin(call.Elements("fKnee")),
                                             fHeel_Max = getMax(call.Elements("fHeel")),
                                             fHeel_Min = getMin(call.Elements("fHeel")),
                                             bandwidth_Min = getMin(call.Elements("Bandwidth")),
                                             bandwidth_Max = getMax(call.Elements("Bandwidth"))
                                         };

                    if (referenceCalls != null)
                    {
                        result.AddRange(referenceCalls);
                    }
                }
            }

            return (result);
        }
    }
}