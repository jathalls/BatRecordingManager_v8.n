using Acr.Settings;
using Invisionware.Settings;
using Invisionware.Settings.Sinks;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LinqStatistics;
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

using Path = System.IO.Path;
using System.Windows.Forms;
using UniversalToolkit;

namespace BatCallAnalysisControlSet
{
    /// <summary>
    /// A class to hold displayable data for the callDataGrid
    /// </summary>
    public class CallData
    {
        public CallData(double Start, double Fhi, double Fpk, double Flo, double Fk, double Fh, double dur, double TBC)
        {
            this.Start = Start;
            this.Fhi = Fhi;
            this.Fpk = Fpk;
            this.Flo = Flo;
            this.Fk = Fk;
            this.Fh = Fh;
            this.Dur = dur;
            this.TBC = TBC;
        }

        /// <summary>
        /// the duration of the call in ms
        /// </summary>
        public double Dur { get; set; }

        /// <summary>
        /// The frequency of the Heel (shallow to steep inflection) if any
        /// </summary>
        public double Fh { get; set; }

        /// <summary>
        /// The start or hight frequency of the call
        /// </summary>
        public double Fhi { get; set; }

        /// <summary>
        /// The frequency of the Knee (steep to shallow inflection) if any
        /// </summary>
        public double Fk { get; set; }

        /// <summary>
        /// The end or low frequency of the call
        /// </summary>
        public double Flo { get; set; }

        /// <summary>
        /// The peak or max energy frequency of the call
        /// </summary>
        public double Fpk { get; set; }

        /// <summary>
        /// start location of the call in the segment in ms
        /// </summary>
        public double Start { get; set; }

        /// <summary>
        /// The interval between calls - actually the time from the start of this to the start of the next in ms
        /// </summary>
        public double TBC { get; set; }
    }

    /// <summary>
    /// Interaction logic for CallDataForm.xaml
    /// </summary>
    public partial class CallDataForm : System.Windows.Controls.UserControl
    {
        public CallDataForm()
        {
            InitializeComponent();

            DataContext = this;

            /*
            settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\");
            settingsPath = Path.Combine(settingsPath, "customSettings.json");
            var settingsConfig = new SettingsConfiguration().WriteTo.JsonNet(settingsPath).ReadFrom.JsonNet(settingsPath);
            var SettingsMgr = settingsConfig.CreateSettingsMgr<ISettingsObjectMgr>();

            var settings = SettingsMgr.ReadSettings<CustomSettings>();
            if (settings != null)
            {
                call = settings.call;
            }
            else
            {
                call = new ReferenceCall();
            }*/
            call = ReferenceCall.getFromSettings();

            hasData = false;
            dataGridData.Clear();

            setSetters(call);
            SetCallParametersButton_Click(this, new RoutedEventArgs());
        }

        public event EventHandler<callEventArgs> callSet;

        public event EventHandler saveClicked;

        public enum setterMode { NORMAL, EDIT };

        public ReferenceCall call { get; set; } = null;

        public List<CallData> dataGridData { get; set; } = new List<CallData>();

        public Visibility dataGridVisibility { get; set; } = Visibility.Hidden;

        public decimal fStart_Max { get; set; }

        public decimal fStart_Mean { get; set; }

        public decimal fStart_Min { get; set; }

        public bool hasData
        {
            get
            {
                if (dataGridVisibility == Visibility.Visible) return (true);
                return (false);
            }

            set
            {
                if (value)
                {
                    dataGridVisibility = Visibility.Visible;
                }
                else
                {
                    dataGridVisibility = Visibility.Hidden;
                }
            }
        }

        public ReferenceCall getCallParametersfromSetters()
        {
            ReferenceCall call = new ReferenceCall();
            call.setStartFrequency(startFrequencySetter.Value_Set);
            call.setEndFrequency(endFrequencySetter.Value_Set);
            call.setPeakFrequency(peakFrequencySetter.Value_Set);

            call.setDuration(durationSetter.Value_Set);
            call.setInterval(intervalSetter.Value_Set);

            call.setBandwidth(bandwidthSetter.Value_Set);
            call.setKneeFrequency(KneeFrequencySetter.Value_Set);
            call.setHeelFrequency(heelFrequencySetter.Value_Set);

            return (call);
        }

        public bool setSetters(ReferenceCall call, setterMode mode = setterMode.NORMAL)
        {
            var result = setSetters(call);
            if (mode == setterMode.EDIT)
            {
                SetCallParametersButton.Visibility = Visibility.Hidden;
                pasteButton.Visibility = Visibility.Hidden;
            }
            return (result);
        }

        /// <summary>
        /// Sets discrete call data for display in the datagrid and makes the grid visible
        /// </summary>
        /// <param name="displayableData"></param>
        internal void SetDisplayableCallData(List<CallData> displayableData)
        {
            dataGridData.Clear();
            hasData = false;
            if (displayableData != null && displayableData.Any())
            {
                foreach (var datum in displayableData) dataGridData.Add(datum);
                hasData = true;
            }
        }

        protected virtual void OnCallSet(callEventArgs e) => callSet?.Invoke(this, e);

        protected virtual void OnSaveButtonClicked(callEventArgs e) => saveClicked?.Invoke(this, e);

        private string settingsPath { get; set; }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            call = new ReferenceCall();
            try
            {
                startFrequencySetter.SetAll(call.getStartFrequencySet);
                endFrequencySetter.SetAll(call.getEndFrequencySet);
                peakFrequencySetter.SetAll(call.getPeakFrequencySet);

                durationSetter.SetAll(call.getDurationSet);
                intervalSetter.SetAll(call.getIntervalSet);

                bandwidthSetter.SetAll(call.getBandwidthSet);
                KneeFrequencySetter.SetAll(call.getKneeFrequencySet);
                heelFrequencySetter.SetAll(call.getHeelFrequencySet);

                SetCallParametersButton_Click(sender, e);

                hasData = false;
                dataGridData.Clear();
            }
            catch (Exception)
            {
                return;
            }
        }

        private (double min, double mean, double max) getLimits(List<double> valList)
        {
            double mean = 0.0d;
            double sd = 0.0d;
            List<double> set = new List<double>();
            var nonzeros = valList.Where(v => v > 0.0d);
            if (nonzeros.Any()) mean = nonzeros.Average();
            if (nonzeros.Count() >= 2) sd = nonzeros.StandardDeviation();

            set = nonzeros.Where(v => v >= mean - sd && v <= mean + sd).ToList();
            if (set.Count >= 2)
            {
                return ((set.Average() + set.StandardDeviation(), set.Average(), set.Average() - set.StandardDeviation()));
            }
            else
            {
                if (set.Any())
                {
                    return ((set.Average(), set.Average(), set.Average()));
                }
            }
            return ((0.0d, 0.0d, 0.0d));
        }

        /// <summary>
        /// Presnts an open file dialog to select a text file exported from AnalookW
        /// </summary>
        /// <returns></returns>
        private string GetTextFile()
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dialog.Title = "Select AnalookW text file";
            dialog.DefaultExt = ".txt";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(dialog.FileName))
                {
                    return (dialog.FileName);
                }
            }

            return ("");
        }

        private void pasteButton_Click(object sender, RoutedEventArgs e)
        {
            clearButton_Click(sender, e);
            if (!System.Windows.Clipboard.ContainsText())
            {
                ReadAnabatFile();
                return;
            }
            var text = System.Windows.Clipboard.GetText();
            if (String.IsNullOrWhiteSpace(text))
            {
                ReadAnabatFile();
                return;
            }
            var lines = text.Split('\n');
            if (lines.Count() < 2)
            {
                ReadAnabatFile();
                return;
            }
            var values = lines[1].Split('\t');
            if (values.Count() < 16)
            {
                if (values.Count() < 8)
                {
                    ReadAnabatFile();
                    return;
                }
                // we have FS values pasted
                if (double.TryParse(values[5], out double fpMax))
                {
                    startFrequencySetter.SetAll((0.0d, fpMax, 0.0d));
                }

                if (double.TryParse(values[4], out double fpMin))
                {
                    endFrequencySetter.SetAll((0.0d, fpMin, 0.0d));
                }

                if (double.TryParse(values[7], out double fpPeak))
                {
                    peakFrequencySetter.SetAll((0.0d, fpPeak, 0.0d));
                }
                if (fpMax > 0.0d && fpMin > 0.0d)
                {
                    bandwidthSetter.SetAll((0.0d, fpMax - fpMin, 0.0d));
                }
            }
            else
            {
                // we have ZC values pasted
                if (double.TryParse(values[7], out double fMax))
                {
                    startFrequencySetter.SetAll((0.0d, fMax, 0.0d));
                }

                if (double.TryParse(values[8], out double fMin))
                {
                    endFrequencySetter.SetAll((0.0d, fMin, 0.0d));
                }

                if (double.TryParse(values[9], out double fPeak)) // Actually Fmean since no Fpeak given
                {
                    peakFrequencySetter.SetAll((0.0d, fPeak, 0.0d));
                }

                if (double.TryParse(values[13], out double fKnee))
                {
                    KneeFrequencySetter.SetAll((0.0d, fKnee, 0.0d));
                }

                if (double.TryParse(values[5], out double tDur))
                {
                    durationSetter.SetAll((0.0d, tDur, 0.0d));
                }

                if (double.TryParse(values[6], out double tInt))
                {
                    intervalSetter.SetAll((0.0d, tInt, 0.0d));
                }

                if (fMax > 0.0d && fMin > 0.0d)
                {
                    bandwidthSetter.SetAll((0.0d, fMax - fMin, 0.0d));
                }
            }

            SetCallParametersButton_Click(sender, e);
        }

        /// <summary>
        /// Asks for an Anabat data export file to open
        /// </summary>
        private void ReadAnabatFile()
        {
            string filename = GetTextFile();
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                return;
            }
            string text = File.ReadAllText(filename);
            string[] lines = text.Split('\n');
            if (!(lines[0].Contains("Filename") && lines[0].Contains("Fmax") && lines[0].Contains("Fmean")))
            {
                return;
            }
            List<double> durList = new List<double>();
            List<double> intList = new List<double>();
            List<double> fStartList = new List<double>();
            List<double> fEndList = new List<double>();
            List<double> fpeakList = new List<double>();
            List<double> bwList = new List<double>();
            List<double> fKneeList = new List<double>();

            for (int i = 1; i < lines.Count(); i++)
            {
                var values = lines[i].Split('\t');
                if (values.Count() >= 17)
                {
                    double val;
                    if (double.TryParse(values[2].Trim(), out val)) durList.Add(val); else durList.Add(0.0d);
                    if (double.TryParse(values[4].Trim(), out val)) intList.Add(val); else intList.Add(0.0d);
                    if (double.TryParse(values[5].Trim(), out val)) fStartList.Add(val); else fStartList.Add(0.0d);
                    if (double.TryParse(values[6].Trim(), out val)) fEndList.Add(val); else fEndList.Add(0.0d);
                    if (!double.TryParse(values[12].Trim(), out val))
                    {
                        val = 0.0d;
                    }
                    if (val == 0.0d) // failed to read Fc or Fc was 0.0d
                    {
                        double.TryParse(values[7].Trim(), out val); // if Fc is zero try reading Fmean
                        fpeakList.Add(val); // and add it whether a value or 0.0d
                    }
                    else
                    { // we got a non-zero value for Fc so use it
                        fpeakList.Add(val);
                    }

                    if (double.TryParse(values[9].Trim(), out val)) fKneeList.Add(val); else fKneeList.Add(0.0d);
                    val = fStartList.Last() - fEndList.Last();
                    bwList.Add(val);
                }
            }

            double mean;
            double sd;

            mean = durList.Where(v => v > 0.0d).Average();
            sd = durList.Where(v => v > 0.0d).StandardDeviation();
            var set = durList.Where(v => v > mean - sd && v < mean + sd);
            durationSetter.SetAll((set.Average() - set.StandardDeviation(), set.Average(), set.Average() + set.StandardDeviation()));

            durationSetter.SetAll(getLimits(durList));
            intervalSetter.SetAll(getLimits(intList));
            startFrequencySetter.SetAll(getLimits(fStartList));
            endFrequencySetter.SetAll(getLimits(fEndList));
            peakFrequencySetter.SetAll(getLimits(fpeakList));
            bandwidthSetter.SetAll(getLimits(bwList));
            KneeFrequencySetter.SetAll(getLimits(fKneeList));

            SetCallParametersButton_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Saves the current selection of values, either to a file or back to the original
        /// caller depending on the source of the data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ReferenceCall call = getCallParametersfromSetters();
            OnSaveButtonClicked(new callEventArgs(call));
        }

        private void SetCallParametersButton_Click(object sender, RoutedEventArgs e)
        {
            ReferenceCall call = new ReferenceCall();
            call = getCallParametersfromSetters();

            /*
            var settingsConfig = new SettingsConfiguration().WriteTo.JsonNet(settingsPath).ReadFrom.JsonNet(settingsPath);
            var SettingsMgr = settingsConfig.CreateSettingsMgr<ISettingsObjectMgr>();
            var settings = new CustomSettings();
            settings.call = call;
            SettingsMgr.WriteSettings<CustomSettings>(settings);*/
            call.setToSettings();

            OnCallSet(new callEventArgs(call));
        }

        private bool setSetters(ReferenceCall call)
        {
            try
            {
                startFrequencySetter.SetAll(call.getStartFrequencySet);
                endFrequencySetter.SetAll(call.getEndFrequencySet);
                peakFrequencySetter.SetAll(call.getPeakFrequencySet);

                durationSetter.SetAll(call.getDurationSet);
                intervalSetter.SetAll(call.getIntervalSet);

                bandwidthSetter.SetAll(call.getBandwidthSet);
                KneeFrequencySetter.SetAll(call.getKneeFrequencySet);
                heelFrequencySetter.SetAll(call.getHeelFrequencySet);
            }
            catch (Exception)
            {
                return (false);
            }
            return (true);
        }
    }

    public class callEventArgs : EventArgs
    {
        public callEventArgs(ReferenceCall call)
        {
            this.call = call;
        }

        public ReferenceCall call { get; set; } = new ReferenceCall();
    }
}