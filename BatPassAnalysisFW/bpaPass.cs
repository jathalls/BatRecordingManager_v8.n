using Acr.Settings;
using DspSharp.Algorithms;
using DspSharp.Utilities.Collections;
using LinqStatistics;
using LinqStatistics.NaN;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UniversalToolkit;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// class representing a single pass within a segment with a duration of up to 7.5s
    /// </summary>
    public class bpaPass : INotifyPropertyChanged
    {
        public bpaPass(int recNumber, int segmentNumber, int passNumber, int startOfPassInSegment, DataAccessBlock dab, int sampleRate,
            string Comment, float segLengthSecs, TimeSpan startOfSegment)
        {
            OffsetInSegmentInSamples = startOfPassInSegment;
            passStart = TimeSpan.FromSeconds(startOfPassInSegment / (float)sampleRate);
            PassLengthInSamples = (int)(dab.Length);
            SampleRate = sampleRate;
            passDataAccessBlock = dab;
            segStart = startOfSegment;
            //segLength = (float)dab.segLength / (float)sampleRate;
            this.SegLengthSecs = segLengthSecs;
            shtFileName = Path.GetFileName(dab.FQfileName);

            this.passNumber = passNumber;
            this.segmentNumber = segmentNumber;
            this.recordingNumber = recNumber;
            alternationNumber = recNumber + segmentNumber - 1;
            this.Comment = Comment;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public enum peakState { NOTINPEAK, INPEAKLEADIN, INPEAK, INPEAKLEADOUT };

        public int alternationNumber { get; set; }
        public string Comment { get; set; }

        public (float Mean, float SD, float NoPulses) durationDetails
        {
            get
            {
                if (pulseList.Count > 0)
                {
                    float sd = 0.0f;
                    if (pulseList.Count >= 3)
                    {
                        sd = (float)pulseList.Select(p => p.getPeak().peakWidthMs).StandardDeviation();
                    }

                    return (Mean: (float)pulseList.Select(p => p.getPeak().peakWidthMs).Average(),
                        SD: sd,
                        NoPulses: pulseList.Count);
                }
                else
                {
                    return ((Mean: 0.0f, SD: 0.0f, NoPulses: 0.0f));
                }
            }
        }

        public string durationString
        {
            get
            {
                var dd = durationDetails;
                if (dd.NoPulses > 0)
                {
                    return ($"{dd.Mean:0.0#}+/-{dd.SD:0.0#}");
                }
                else
                {
                    return ("");
                }
            }
        }

        /// <summary>
        /// formatted string with average end frequency of all pulses +/- 1SD
        /// </summary>
        public string End_kHz
        {
            get
            {
                var pfEndList = (from pulse in pulseList
                                 where pulse.GetSpectrumDetails().pfEnd >= 15000
                                 select pulse.GetSpectrumDetails().pfEnd);
                if (pfEndList != null && pfEndList.Any())
                {
                    double avg = pfEndList.Average() / 1000.0f;
                    if (pfEndList.Count() >= 3)
                    {
                        endFrequencykHzSD = pfEndList.StandardDeviation() / 1000.0f;
                    }
                    else
                    {
                        endFrequencykHzSD = 0.0f;
                    }
                    _endFrequencykHz = (float)avg;
                    if (pfEndList.Count() >= 2)
                    {
                        return ($"{avg:G3}+/-{(pfEndList.StandardDeviation() / 1000.0f):G3}");
                    }
                    else
                    {
                        return ($"{_endFrequencykHz:G3}");
                    }
                }
                return ("");
            }
        }

        public (float Mean, float SD, float NoPulses) endDetails
        {
            get
            {
                return (Mean: _endFrequencykHz, SD: _endFrequencykHzSD, NoPulses: pulseList.Count);
            }
        }

        public BitmapImage Frequency
        {
            get
            {
                int width = SampleRate / 1000;
                Bitmap bmp = new Bitmap(width, 20);

                System.Drawing.Pen pen = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Red));
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    for (int f = 20; f < 70; f += 10)
                    {
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Gold, 2), f * 2, 0, f * 2, 20);
                    }
                    if (endFrequencykHz > 15)
                    {
                        System.Drawing.Pen faintPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightCoral));
                        g.FillRectangle(faintPen.Brush, (endFrequencykHz - endFrequencykHzSD) * 2, 0, endFrequencykHzSD * 4, 7);

                        int x = (int)endFrequencykHz * 2;
                        x = x > width ? width : x;
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Red, 2), x, 0, x, 20);
                    }

                    if (startFrequencykHz > 15)
                    {
                        System.Drawing.Pen faintPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightBlue));
                        g.FillRectangle(faintPen.Brush, (startFrequencykHz - startFrequencykHzSD) * 2, 14, startFrequencykHzSD * 4, 7);

                        int x = (int)startFrequencykHz * 2;
                        x = x > width ? width : x;
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Blue, 2), x, 0, x, 20);
                    }
                    if (peakFrequencykHz > 15)
                    {
                        System.Drawing.Pen faintPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightGreen));
                        g.FillRectangle(faintPen.Brush, (peakFrequencykHz - peakFrequencykHzSD) * 2, 7, peakFrequencykHzSD * 4, 7);

                        int x = (int)peakFrequencykHz * 2;
                        x = x > width ? width : x;
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Green, 2), x, 0, x, 20);
                    }
                }

                return (loadBitmap(bmp));
            }
        }

        public (float Mean, float SD, float NoPulses) intervalDetails
        {
            get
            {
                if (_intervalCount <= 0)
                {
                    CalculateMeanInterval();
                }
                return (Mean: meanIntervalSecs, SD: _intervalSD, NoPulses: _intervalCount);
            }
        }

        public string intervalString
        {
            get
            {
                var id = intervalDetails;
                if (id.NoPulses > 0)
                {
                    return ($"{id.Mean:#0.##}+/-{id.SD:#0.##}");
                }
                return ("");
            }
        }

        public float meanDurationMs
        {
            get
            {
                var durtn = (from p in pulseList
                             select p.getPeak().peakWidthMs).Average();
                return ((float)durtn);
            }
        }

        /// <summary>
        /// A value calculated from the pulse previousIntervals using a minimising function to
        /// approximate the typical interval between pulses making allowance for possible missing pulses
        /// and therefore intervals that may be multiples of the base interval.
        /// </summary>
        public float meanIntervalSecs { get; set; }

        /// <summary>
        /// Number of pulses in the pass
        /// </summary>
        public int Number_Of_Pulses
        {
            get
            {
                return (pulseList.Count());
            }
        }

        /// <summary>
        /// duration of the Pass
        /// </summary>
        public double Pass_Length_s
        {
            get
            {
                return (PassLengthInSamples / (double)SampleRate);
            }
        }

        /// <summary>
        /// number of the pass in the segment
        /// </summary>
        public int Pass_Number
        {
            get
            {
                return (passNumber);
            }
        }

        public DataAccessBlock passDataAccessBlock { get; set; }
        public TimeSpan passStart { get; set; }

        /// <summary>
        /// formatted string with average peak frequency of all pulses +/- 1SD
        /// Bound to column in datagrid
        /// </summary>
        public string Peak_kHz
        {
            get
            {
                var pfMeanList = (from pulse in pulseList
                                  where pulse.GetSpectrumDetails().pfMeanOfPeakFrequenciesInSpectralPeaksList >= 15000
                                  select pulse.GetSpectrumDetails().pfMeanOfPeakFrequenciesInSpectralPeaksList);
                if (pfMeanList != null && pfMeanList.Any())
                {
                    _peakFrequencykHz = pfMeanList.Average() / 1000.0f;
                    if (pfMeanList.Count() >= 3)
                    {
                        peakFrequencykHzSD = (float)pfMeanList.StandardDeviation() / 1000.0f;
                    }
                    else
                    {
                        peakFrequencykHzSD = 0.0f;
                    }
                    if (pfMeanList.Count() >= 2)
                    {
                        return ($"{_peakFrequencykHz:G3}+/-{(pfMeanList.StandardDeviation() / 1000.0f):G3}");
                    }
                    else
                    {
                        return ($"{_peakFrequencykHz:G3}");
                    }
                }
                return ("");
            }
        }

        public (float Mean, float SD, float NoPulses) peakDetails
        {
            get
            {
                return (Mean: _peakFrequencykHz, SD: _peakFrequencykHzSD, NoPulses: pulseList.Count);
            }
        }

        public int recordingNumber { get; set; }
        public float SegLengthSecs { get; set; }
        public int segmentNumber { get; set; }
        public TimeSpan segStart { get; set; }
        public string shtFileName { get; set; }
        public decimal spectrumfactor { get; set; }

        /// <summary>
        /// formatted string with average start frequency of all pulses +/- 1SD
        /// </summary>
        public string Start_kHz
        {
            get
            {
                var pfStartList = (from pulse in pulseList
                                   where pulse.GetSpectrumDetails().pfStart >= 15000
                                   select pulse.GetSpectrumDetails().pfStart);
                if (pfStartList != null && pfStartList.Any())
                {
                    _startFrequencykHz = pfStartList.Average() / 1000.0f;
                    if (pfStartList.Count() >= 3)
                    {
                        startFrequencykHzSD = (float)pfStartList.StandardDeviation() / 1000.0f;
                    }
                    else
                    {
                        startFrequencykHzSD = 0.0f;
                    }
                    if (pfStartList.Count() >= 2)
                    {
                        return ($"{_startFrequencykHz:G3}+/-{(pfStartList.StandardDeviation() / 1000.0f):G3}");
                    }
                    else
                    {
                        return ($"{_startFrequencykHz:G3}");
                    }
                }
                return ("");
            }
        }

        public (float Mean, float SD, float NoPulses) startDetails
        {
            get
            {
                return (Mean: _startFrequencykHz, SD: _startFrequencykHzSD, NoPulses: pulseList.Count);
            }
        }

        public decimal thresholdFactor { get; set; }

        public static List<double> GetEnvelope2(DataAccessBlock passDataAccessBlock, int sampleRate, int smooth)
        {
            float[] data = passDataAccessBlock.getData((int)passDataAccessBlock.BlockStartInFileInSamples, (int)passDataAccessBlock.Length);

            Debug.WriteLine($"GE2:- raw max={data.IndexOf(data.Max())}");
            HighPassFilter(ref data, 15000, sampleRate);
            Debug.WriteLine($"GE2:- HF max={data.IndexOf(data.Max())}");
            data = data.Select(val => Math.Abs(val)).ToArray();

            LoPassFilter(ref data, 3000, sampleRate);
            Debug.WriteLine($"GE2:- absLF max={data.IndexOf(data.Max())}");
            HighPassFilter(ref data, 100, sampleRate);
            Debug.WriteLine($"GE2:- HF2 max={data.IndexOf(data.Max())}");
            List<double> smoothedData = new List<double>();
            double total = 0.0d;
            for (int i = 1; i < data.Length; i++)
            {
                total += data[i];
                if (i % smooth == 0)
                {
                    double v = (total / smooth);

                    smoothedData.Add(v * v);
                    total = 0.0d;
                }
            }
            Debug.WriteLine($"GE2:- smooth{smooth} max={smoothedData.IndexOf(smoothedData.Max())}");
            double min = smoothedData.Min();
            double scale = 1 / min;
            //smoothedData = smoothedData.Select(val => val * scale).ToList();
            Tools.WriteArrayToFile(@"C:\BRMTestData\Envelope.csv", smoothedData.ToArray());

            return (smoothedData);
        }

        public static void HighPassFilter(ref float[] data, int frequency, int sampleRate)
        {
            var filter = BiQuadFilter.HighPassFilter(sampleRate, frequency, q: 1);
            int preData = Math.Max(data.Length / 20, 300);
            if (preData > data.Length)
            {
                for (int i = 0; i < preData; i++)
                {
                    _ = filter.Transform(data[i]);
                }
            }
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = filter.Transform(data[i]);
            }
        }

        public static BitmapImage loadBitmap(System.Drawing.Bitmap source)
        {
            BitmapImage bmpi = null;
            if (source != null)
            {
                try
                {
                    MemoryStream ms = new MemoryStream();
                    source.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    bmpi = new BitmapImage();
                    bmpi.BeginInit();
                    bmpi.StreamSource = ms;
                    bmpi.EndInit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in Load Bitmap:-" + ex.Message);
                }
            }

            return bmpi;
        }

        public static void LoPassFilter(ref float[] data, int frequency, int sampleRate)
        {
            var filter = BiQuadFilter.LowPassFilter(sampleRate, frequency, q: 1);
            int preData = Math.Max(data.Length / 20, 300);
            if (preData > data.Length)
            {
                for (int i = 0; i < preData; i++)
                {
                    _ = filter.Transform(data[i]);
                }
            }
            data = data.Select(v => filter.Transform(v)).ToArray();
        }

        public void AddPulse(Pulse pulse)
        {
            pulseList.Add(pulse);
        }

        /// <summary>
        /// tries to use a least squares minimisation to approximate the typical pulse interval for the pass
        /// </summary>
        /// <returns></returns>
        public float CalculateMeanInterval()
        {
            float currentGuess = 0.0f;
            if (pulseList.Any())
            {
                var pulses = (from pulse in pulseList
                              where pulse.Pulse_Interval_ms > 0
                              select (float)pulse.Pulse_Interval_ms);
                if (pulses?.Any() ?? false)
                {
                    currentGuess = pulses.Average();
                }
                else
                {
                    return (0.0f);
                }
                float previousGuess = 0.0f;

                var currentDeviation = calculateIntervalDeviation(ref currentGuess);

                for (int i = 0; i < 10; i++)
                {
                    var deviation = calculateIntervalDeviation(ref currentGuess);
                    //Debug.WriteLine($"loop {i}: previous={previousGuess}, current={currentGuess}, deviation={deviation}");
                    if (previousGuess == 0.0f)
                    {
                        previousGuess = currentGuess;
                        currentGuess = currentGuess + (float)deviation / 10.0f;
                    }
                    else
                    {
                        var temp = currentGuess;
                        currentGuess = currentGuess + (float)deviation / 10.0f;
                        previousGuess = temp;
                    }
                }
            }
            meanIntervalSecs = currentGuess;
            return (currentGuess);
        }

        public void CreatePass(decimal thresholdFactor, decimal spectrumFactor)
        {
            pulseList.Clear();

            if (passDataAccessBlock.Length <= 0) return;

            this.thresholdFactor = thresholdFactor;
            this.spectrumfactor = spectrumfactor;

            ObservableList<Peak> peakList = new ObservableList<Peak>();
            int quietStart = -1;

            //quiet = getQuietPortion(secs: 0.1f,factor:1.0f);
            int smooth = 20;
            List<double> fullPassSmoothedEnvelope = GetEnvelope2(passDataAccessBlock, SampleRate, smooth);
            float leadInms = CrossSettings.Current.Get<float>("EnvelopeLeadInMS");
            if (leadInms <= 0.0f)
            {
                leadInms = 0.2f;
                CrossSettings.Current.Set<float>("EnvelopeLeadInMS", leadInms);
            }
            int leadinLimit = (int)((SampleRate / 1000) * leadInms); //0.2ms minimum duration

            float leadOutms = CrossSettings.Current.Get<float>("EnvelopeLeadOutMS");
            if (leadOutms <= 0.0f)
            {
                leadOutms = 1.0f;
                CrossSettings.Current.Set<float>("EnvelopeLeadOutMS", 1.0f);
            }
            int leadoutLimit = (int)((SampleRate / 1000) * leadOutms); //1ms silence between peaks
            double threshold = 0.0d;
            //quietStart = getQuietStart(ref fullPassSmoothedEnvelope, (float)(fullPassSmoothedEnvelope.Average() * (float)thresholdFactor),out double threshold);
            quietStart = getQuietStart2(ref fullPassSmoothedEnvelope, thresholdFactor, out threshold);
            if (quietStart < 0)
            {
                //quietStart = getQuietStart(ref fullPassSmoothedEnvelope, (float)(fullPassSmoothedEnvelope.Average() * (float)thresholdFactor * 2));
                quietStart = getQuietStart2(ref fullPassSmoothedEnvelope, thresholdFactor, out threshold);
            }

            peakNumber = 1;
            GP4depth = 0;
            //peakList = getPeaks3(ref fullPassSmoothedEnvelope, leadinLimit, leadoutLimit, (float)thresholdFactor, smooth, threshold);
            peakList = getPeaks4(fullPassSmoothedEnvelope, leadinLimit, leadoutLimit, (float)thresholdFactor, smooth,
                double.NaN, new ObservableList<Peak>(), 0);

            peakList = setPeakIntervals(peakList);

            //envelope = new float[0];
            //float[] passData = passDataAccessBlock.getData();
            foreach (var peak in peakList)
            {
                pulseList.Add(new Pulse(passDataAccessBlock, OffsetInSegmentInSamples, peak, passNumber, quietStart, spectrumFactor));
            }
            //Debug.WriteLine($"Pass at Offset {OffsetInSegmentInSamples} of length {Pass_Length_s}s has {pulseList.Count} pulses");
            _endFrequencykHz = 0.0f;
            _peakFrequencykHz = 0.0f;
            _startFrequencykHz = 0.0f;
            _ = End_kHz;
            _ = Start_kHz;
            _ = Peak_kHz;

            meanIntervalSecs = CalculateMeanInterval();
        }

        public List<Pulse> DeletePulses(List<Pulse> toBeRemoved)
        {
            List<Pulse> removed = new List<Pulse>();
            foreach (var p in toBeRemoved)
            {
                p?.spectralDetails?.spectralPeakList?.Clear();
                p.spectralDetails = null;
                if (pulseList.Contains(p))
                {
                    pulseList.Remove(p);

                    _ = Start_kHz;
                    _ = Peak_kHz;
                    _ = End_kHz;
                    NotifyPropertyChanged(nameof(Frequency));
                    removed.Add(p);
                    Debug.WriteLine($"Removed Pulse {p.Pulse_Number} from Pass {passNumber}");
                }
            }
            foreach (var p in removed) toBeRemoved.Remove(p);
            meanIntervalSecs = CalculateMeanInterval();
            return (removed);
        }

        /// <summary>
        /// returns the pulse List
        /// </summary>
        /// <returns></returns>
        public ObservableList<Pulse> getPulseList()
        {
            return (pulseList);
        }

        internal float peakFrequencykHzSD
        {
            get
            {
                if (_peakFrequencykHzSD < 0.0f)
                {
                    _ = Peak_kHz;
                }
                return (_peakFrequencykHzSD);
            }
            set
            {
                _peakFrequencykHzSD = value;
            }
        }

        internal int SampleRate { get; set; }

        /*
        internal static float[] GetEnvelope(DataAccessBlock passDab, int sampleRate)
        {
            float[] data = passDab.getData();
            HighPassFilter(ref data, frequency: 15000, sampleRate);

            //float[] result2 = new float[data.Length];
            if (data != null && data.Length > 384)
            {
                //for (int s = 0; s < data.Length; s++)
                //{
                //    data[s] = Math.Abs(data[s]);
                //}

                var filter = BiQuadFilter.LowPassFilter(sampleRate, 10000, 1);
                for (int s = 0; s < data.Length; s++)
                {
                    data[s] = filter.Transform(Math.Abs(data[s]));
                }
                HighPassFilter(ref data, frequency: 50, sampleRate);
                for (int s = 2; s < data.Length - 2; s++)
                {
                    data[s] = (float)Math.Sqrt((data[s - 2] * data[s - 2] + data[s - 1] * data[s - 1] + data[s] * data[s] + data[s + 1] * data[s + 1] + data[s + 2] * data[s + 2]) / 5);
                }
            }
            return (data);
        }*/ //GetEnvelope

        /// <summary>
        /// Delete any pulses with start, end or peak frequencies more than 2SD from the mean
        /// </summary>
        internal void DeleteExtremePulses()
        {
            var pulses = getPulseList();
            List<Pulse> pulsesToremove = new List<Pulse>();
            if (pulses != null && pulses.Count > 0)
            {
                pulsesToremove = (
                        from pulse in pulses
                        where pulse.spectralDetails.pfEnd <= 15000 ||
                        Math.Abs(pulse.spectralDetails.pfStart / 1000 - (startFrequencykHz)) > (2 * startFrequencykHzSD) ||
                        Math.Abs(pulse.spectralDetails.pfEnd / 1000 - (endFrequencykHz)) > (2 * endFrequencykHzSD) || endFrequencykHz > 100 ||
                        Math.Abs(pulse.spectralDetails.pfMeanOfPeakFrequenciesInSpectralPeaksList / 1000 - (peakFrequencykHz)) > (2 * peakFrequencykHzSD)
                        select pulse)?.ToList<Pulse>();

                if (pulsesToremove != null)
                {
                    DeletePulses(pulsesToremove);
                }
                Debug.WriteLine($"Deleted {pulsesToremove.Count} pulses from Pass {passNumber} in segment {segmentNumber} of rec {recordingNumber}");
            }
        }

        /// <summary>
        /// Gets the envelope of the pass and draws it into a bitmap with the identified peaks numbered
        /// </summary>
        /// <returns></returns>
        internal BitmapImage GetEnvelopeBitmap()
        {
            int smooth = 20;
            float[] envelope = bpaPass.GetEnvelope2(passDataAccessBlock, SampleRate, smooth).Select(val => (float)val).ToArray();
            if (envelope != null && pulseList != null && envelope.Length > 5 && pulseList.Any())
            {
                ObservableList<Peak> peakList = new ObservableList<Peak>();
                foreach (var pulse in pulseList)
                {
                    peakList.Add(pulse.getPeak());
                }

                var bmp = PassAnalysis.GetGraph(ref envelope, ref peakList, (double)passDataAccessBlock.Length);
                return (loadBitmap(bmp));
            }

            return (null);
        }

        internal decimal GetEnvelopeThresholdFactor()
        {
            return (thresholdFactor);
        }

        internal int getOffsetInSegmentInSamples()
        {
            return (OffsetInSegmentInSamples);
        }

        internal int getPassLengthInSamples()
        {
            return (PassLengthInSamples);
        }

        internal double GetPeakFrequencykHzSD()
        {
            _ = Peak_kHz;
            return (peakFrequencykHzSD);
        }

        /// <summary>
        /// Generates a bitmap graph of individual segments within the pass as used in getPeaks3
        /// </summary>
        /// <returns></returns>
        internal BitmapImage GetSegmentBitmap()
        {
            var envelopeDbl = GetEnvelope2(passDataAccessBlock, SampleRate, 20);

            var shtEnvelopeDbl = envelopeDbl.Take(envelopeDbl.Count / 5).ToArray();
            var envelope = shtEnvelopeDbl.Select(e => (float)e).ToArray();
            if (envelope != null && pulseList != null && envelope.Length > 5 && pulseList.Any())
            {
                ObservableList<Peak> peakList = new ObservableList<Peak>();
                foreach (var pulse in pulseList)
                {
                    if (pulse.getPeak().getStartAsSampleInPass() < envelope.Length / 5)
                    {
                        peakList.Add(pulse.getPeak());
                    }
                }
                Debug.Write($"{peakList.Count} peaks in the segment:- ");
                foreach (var peak in peakList)
                {
                    Debug.Write($"{peak.peakWidthMs}, ");
                }
                Debug.WriteLine("");
                var bmp = PassAnalysis.GetGraph(ref envelope, ref peakList, envelope.Length / (double)passDataAccessBlock.Length);
                return (loadBitmap(bmp));
            }

            return (null);
        }

        internal decimal GetSpectrumThresholdFactor()
        {
            return (spectrumfactor);
        }

        /// <summary>
        /// Returns the time in the recording at which this pass starts in secs
        /// </summary>
        /// <returns></returns>
        internal float GetStartTimeInrecording()
        {
            long startInSamples = passDataAccessBlock.BlockStartInFileInSamples;
            return (startInSamples / (float)SampleRate);
        }

        internal double getVariance(Pulse pulse, List<Pulse> plist)
        {
            double variance = 0.0d;

            if (pulse != null)
            {
                var meanEnd = plist.Select(p => p.GetSpectrumDetails().pfEnd).Average();
                var meanStart = plist.Select(p => p.GetSpectrumDetails().pfStart).Average();
                var meanPeak = plist.Select(p => p.GetSpectrumDetails().pfMeanOfPeakFrequenciesInSpectralPeaksList).Average();

                variance = Math.Sqrt(
                            (Math.Pow(pulse.GetSpectrumDetails().pfEnd - meanEnd, 2) +
                            Math.Pow(pulse.GetSpectrumDetails().pfStart - meanStart, 2) +
                            Math.Pow(pulse.GetSpectrumDetails().pfMeanOfPeakFrequenciesInSpectralPeaksList - meanPeak, 2)) / 3.0d
                            );
            }
            else
            {
                var varEnd = plist.Select(p => p.GetSpectrumDetails().pfEnd).Variance();
                var varStart = plist.Select(p => p.GetSpectrumDetails().pfStart).Variance();
                var varPeak = plist.Select(p => p.GetSpectrumDetails().pfMeanOfPeakFrequenciesInSpectralPeaksList).Variance();

                variance = Math.Sqrt(
                            (Math.Pow(varEnd, 2) +
                            Math.Pow(varStart, 2) +
                            Math.Pow(varPeak, 2)) / 3.0d
                            );
            }

            return (Math.Abs(variance));
        }

        internal void NotifyPropertyChanged(string propertyName) =>
                                                                                                                                                                                                                                                                                                                                                                                                                                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// removes items from the PulseList in order to reduce the variation in frequency
        /// parameters
        /// </summary>
        internal List<Pulse> RemoveOutliers()
        {
            Debug.WriteLine($"RemoveOutliers for rec {recordingNumber}, seg {segmentNumber}, Pass {passNumber}");
            List<Pulse> toBeRemoved = new List<Pulse>();
            if (pulseList == null || pulseList.Count < 3)
            {
                Debug.WriteLine($"Only {pulseList.Count} pulses");
                return (new List<Pulse>()); // too few pulses to make removal practicable
            }
            List<Pulse> mutablePulseList = pulseList.ToList();

            for (int i = 0; i < mutablePulseList.Count(); i++)
            {
                var pulse = mutablePulseList[i];
                var details = pulse.GetSpectrumDetails();
                if (details.pfEnd > details.pfStart || details.pfEnd > startFrequencykHz * 1000 || details.pfEnd <= 15000)
                {
                    Debug.WriteLine($"Remove Pulse {pulse.Pulse_Number}: pfEnd={details.pfEnd}, pfStart={details.pfStart}, sf={startFrequencykHz * 1000}");
                    mutablePulseList.Remove(pulse);
                    toBeRemoved.Add(pulse);
                }/*else if(details.pfEnd>(endFrequency+endFrequencySD)*1000 || details.pfEnd < (endFrequency - endFrequencySD)*1000)
                {
                    Debug.WriteLine($"Remove Pulse {pulse.Pulse_Number}:- pfEnd={details.pfEnd}, ef+SD={(endFrequency + endFrequencySD)*1000}, ef-SD={(endFrequency - endFrequencySD)*1000}");
                    mutablePulseList.Remove(pulse);
                    toBeRemoved.Add(pulse);
                }*/
            }
            if (mutablePulseList.Any())
            {
                var orderedList = (from p in mutablePulseList
                                   orderby getVariance(p, mutablePulseList) descending
                                   select p).ToList();

                while (orderedList.Count() > 3)
                {
                    var endfSD = (from p in orderedList
                                  select p.GetSpectrumDetails().pfEnd).StandardDeviation();
                    var startSD = orderedList.Select(p => p.GetSpectrumDetails().pfStart).StandardDeviation();
                    var peakSD = orderedList.Select(p => p.GetSpectrumDetails().pfMeanOfPeakFrequenciesInSpectralPeaksList).StandardDeviation();

                    var shortList = orderedList.ToList();
                    shortList.RemoveAt(0);
                    var oldvar = getVariance(null, orderedList.ToList());
                    var newvar = getVariance(null, shortList);

                    var pc = Math.Abs((oldvar - newvar) / oldvar);

                    Debug.WriteLine($"p {orderedList[0].Pulse_Number}, OldVariance={oldvar} NewVariance={newvar} pc={pc}");

                    if (newvar < oldvar && pc < 0.5d) break;
                    else
                    {
                        Debug.WriteLine($"Removed Pulse {orderedList[0].Pulse_Number}: pc={pc}");
                        toBeRemoved.Add(orderedList[0]);
                        orderedList = shortList;
                    }
                }
            }
            DeletePulses(toBeRemoved);
            if (pulseList.Count < 3)
            {
                List<Pulse> surplus = new List<Pulse>();
                foreach (var pulse in pulseList)
                {
                    surplus.Add(pulse);
                    toBeRemoved.Add(pulse);
                }
                DeletePulses(surplus);
            }
            return (toBeRemoved);
        }

        private readonly ObservableList<Pulse> pulseList = new ObservableList<Pulse>();

        private float _endFrequencykHz = -1.0f;

        private float _endFrequencykHzSD = -1.0f;

        private int _intervalCount = 0;

        private float _intervalSD = 0.0f;

        private float _peakFrequencykHz = -1.0f;

        private float _peakFrequencykHzSD = -1.0f;

        private float _startFrequencykHz = -1.0f;

        private float _startFrequencykHzSD = -1.0f;

        private int GP4depth = 0;
        private int peakNumber = 0;

        private float endFrequencykHz
        {
            get
            {
                if (_endFrequencykHz < 0.0f)
                {
                    _ = End_kHz;
                }
                return (_endFrequencykHz);
            }
        }

        private float endFrequencykHzSD
        {
            get
            {
                if (_endFrequencykHzSD < 0.0f)
                {
                    _ = End_kHz;
                }
                return (_endFrequencykHzSD);
            }
            set { _endFrequencykHzSD = value; }
        }

        private int OffsetInSegmentInSamples { get; set; } = -1;

        private int PassLengthInSamples { get; set; } = -1;

        private int passNumber { get; set; }

        private float peakFrequencykHz
        {
            get
            {
                if (_peakFrequencykHz < 0.0f)
                {
                    _ = Peak_kHz;
                }
                return (_peakFrequencykHz);
            }
        }

        private float startFrequencykHz
        {
            get
            {
                if (_startFrequencykHz < 0.0f)
                {
                    _ = Start_kHz;
                }
                return (_startFrequencykHz);
            }
        }

        private float startFrequencykHzSD
        {
            get
            {
                if (_startFrequencykHzSD < 0.0f)
                {
                    _ = Start_kHz;
                }
                return (_startFrequencykHzSD);
            }
            set
            {
                _startFrequencykHzSD = value;
            }
        }

        private double calculateIntervalDeviation(ref float Guess)
        {
            double result = 0.0d;
            var usableList = from pulse in pulseList
                             where pulse.Pulse_Interval_ms > 0
                             select (float)pulse.Pulse_Interval_ms;
            if (usableList != null)
            {
                float mean = (float)usableList.Average();
                var list = usableList.ToList();
                for (int i = 0; i < usableList.Count(); i++)
                {
                    if (list[i] > mean * 3) list[i] = mean;
                    if (list[i] > (mean * 1.5))
                    {
                        list[i] = list[i] / 2;
                        list.Add(list[i] / 2);
                    }
                }
                double sumsq = 0.0d;
                int higher = 0;
                int lower = 0;
                foreach (var value in list)
                {
                    double diff = value - (double)Guess;
                    if (diff > 0) higher++;
                    if (diff < 0) lower++;
                    sumsq += (diff * diff);
                }
                if (list.Count >= 3)
                {
                    _intervalSD = list.StandardDeviation();
                }
                else
                {
                    _intervalSD = 0.0f;
                }
                _intervalCount = list.Count;
                sumsq /= list.Count();
                result = Math.Sqrt(sumsq);
                //if (higher > lower) { Guess += (float)(result / 2); }
                if (lower > higher) { result = -result; }
            }
            return (result);
        }

        /// <summary>
        /// given a section of data and a position and value of the maximum within that data set, extracts and creates a peak
        /// surrounding that maximum and returns it.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="maxVal"></param>
        /// <param name="smoothedBy"></param>
        /// <param name="threshold"></param>
        /// <param name="dataStartInPass"></param>
        /// <param name="peakStartInData"></param>
        /// <param name="peakEndInData"></param>
        /// <returns></returns>
        private Peak ExtractPeak(List<double> data, (double value, int position) maxVal, int smoothedBy, double threshold,
            int dataStartInPass, out int peakStartInData, out int peakEndInData)
        {
            (int startPosition, int endPositioon, double Area) peakPosition = getPeakLimits(data, maxVal, threshold);
            peakStartInData = peakPosition.startPosition;
            peakEndInData = peakPosition.endPositioon;

            Peak peak = Peak.Create(peakNumber++, (peakPosition.startPosition * smoothedBy) + dataStartInPass, (peakPosition.endPositioon - peakPosition.startPosition) * smoothedBy,
                peakPosition.Area, (float)maxVal.value, 0, SampleRate, OffsetInSegmentInSamples, recordingNumber, (float)threshold);

            return (peak);
        }

        private void FindPeak(List<double> fullPassSmoothedEnvelope, List<double> slope, int peakPosinSmoothedPass, double threshold, int smoothedLeadinLimit, int smoothedLeadoutLimit, out int startOfPeak, out int widthOfPeak, out double peakArea)
        {
            startOfPeak = getPeakStart(fullPassSmoothedEnvelope, slope, peakPosinSmoothedPass, (float)threshold, smoothedLeadinLimit);
            int end = getPeakEnd(fullPassSmoothedEnvelope, slope, peakPosinSmoothedPass, (float)threshold, smoothedLeadoutLimit);
            widthOfPeak = end - startOfPeak;
            peakArea = getPeakArea(fullPassSmoothedEnvelope, startOfPeak, end);
        }

        private int getLastSampleNumber(List<double> envelope, int peakPos, float absoluteThreshold, int leadoutLimit)
        {
            peakState currentState = peakState.INPEAK;
            absoluteThreshold = (float)Math.Max(absoluteThreshold, envelope[peakPos] / 3);
            int leadoutCount = 0;
            int i = 0;
            for (i = peakPos; i < envelope.Count; i++)
            {
                switch (currentState)
                {
                    case peakState.INPEAK:
                        if (envelope[i] < absoluteThreshold)
                        {
                            currentState = peakState.INPEAKLEADOUT;
                            leadoutCount = 1;
                        }

                        break;

                    case peakState.INPEAKLEADOUT:
                        if (envelope[i] >= absoluteThreshold)
                        {
                            currentState = peakState.INPEAK;
                            leadoutCount = 0;
                        }
                        else
                        {
                            leadoutCount++;
                            if (leadoutCount > leadoutLimit)
                            {
                                return (i - leadoutLimit);
                            }
                        }
                        break;

                    default: break;
                }
            }
            return (envelope.Count - 1);
        }

        /// <summary>
        /// Given a section of data in the form of a List of double, finds the maximum value and returns it
        /// and its position as a Tuple
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private (double value, int position) getMaximum(List<double> data)
        {
            (double value, int position) result;
            result.value = double.MinValue;
            result.position = -1;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] > result.value)
                {
                    result.value = data[i];
                    result.position = i;
                }
            }
            return (result);
        }

        private double getPeakArea(List<double> envelope, int startInEnvelope, int endInEnvelope)
        {
            double area = 0.0d;

            for (int i = startInEnvelope; i >= 0 && i < endInEnvelope; i++)
            {
                area += envelope[i];
            }
            return (area);
        }

        /// <summary>
        /// performs a detailed analysis of a detected peak within the pass in order to establish its size
        /// </summary>
        /// <param name="envelope">section of the smoothed envelope</param>
        /// <param name="slope">slope of envelope i.e. change per smoothed sample</param>
        /// <param name="leadinLimit"></param>
        /// <param name="leadoutLimit"></param>
        /// <param name="peakPos">location of the peak within the envelope</param>
        /// <param name="offset">location of the start of envelope within the smoothed pass</param>
        /// <param name="absoluteThreshold"></param>
        /// <param name="smoothingValue">number of original samples per smoothed sample in envelope</param>
        /// <param name="peakStartInPass">returns the start of the peak in real samples in the pass</param>
        /// <param name="peakWidthInSamples">returns the width of the peak in real samples</param>
        /// <param name="peakArea">returns the area of the peak in real values</param>
        private void getPeakDetails(List<double> envelope, List<double> slope, int leadinLimit, int leadoutLimit, int peakPos, int offset,
            float absoluteThreshold, int smoothingValue, out int peakStartInPass, out int peakWidthInSamples, out double peakArea)
        {
            int startInEnvelope = getPeakStart(envelope, slope, peakPos, absoluteThreshold, leadinLimit);
            if (startInEnvelope < 0) startInEnvelope = 0;
            if (startInEnvelope >= envelope.Count()) startInEnvelope = envelope.Count - 1;
            int endInEnvelope = getPeakEnd(envelope, slope, peakPos, absoluteThreshold, leadoutLimit);
            if (endInEnvelope < 0) endInEnvelope = 0;
            if (endInEnvelope >= envelope.Count()) endInEnvelope = envelope.Count - 1;
            double envelopeArea = getPeakArea(envelope, startInEnvelope, endInEnvelope);
            peakStartInPass = (startInEnvelope + offset) * smoothingValue;
            peakWidthInSamples = (endInEnvelope - startInEnvelope) * smoothingValue;
            peakArea = envelopeArea * smoothingValue;
        }

        /// <summary>
        /// Given an envelope and a detected peak within that envelope uses the slope of the envelope
        /// and the peak position to find the end of the peak within the envelope.
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="slope"></param>
        /// <param name="peakPos"></param>
        /// <param name="absoluteThreshold"></param>
        /// <param name="lastSampleNumber"></param>
        /// <returns></returns>
        private int getPeakEnd(List<double> envelope, List<double> slope, int peakPos, float absoluteThreshold, int leadoutLimit)
        {
            int result = -1;

            int lastSampleNumber = getLastSampleNumber(envelope, peakPos, absoluteThreshold, leadoutLimit);
            return (lastSampleNumber);
            if ((lastSampleNumber - peakPos) < (leadoutLimit / 2)) return (result);

            var slopeSection = slope.Skip(peakPos).Take(lastSampleNumber - peakPos).ToArray();
            int slopeMaxPos = 0; // max descending slope is slope.Min() and is negative in value
            while (slopeSection.Min() < -0.0d)
            {
                slopeMaxPos = slopeSection.MinIndex();
                if (slopeMaxPos < 1)
                {
                    slopeSection[0] = 0.0d;
                    continue;
                }
                if (slopeMaxPos > slopeSection.Count() - 2)//+1 will be  index out of range
                {
                    slopeSection[slopeSection.Count() - 1] = 0.0d;
                    continue;
                }
                if (slopeSection[slopeMaxPos - 1] >= 0.0d || slopeSection[slopeMaxPos + 1] >= 0.0d)
                {
                    slopeSection[slopeMaxPos] = 0.0d;
                }
                else
                {
                    break;
                }
            }
            if (slopeSection[slopeMaxPos] < 0.0d)
            {
                double htRatio = envelope[peakPos] / (envelope[peakPos] - slopeSection[slopeMaxPos]);
                int offset = (int)(htRatio * (slopeMaxPos));
                result = peakPos + offset;
            }

            return (result);
        }

        /// <summary>
        /// given a section of data as a List of double, and the location of the maximum in that list, finds the lateral limits of tha peak
        /// and the area of the peak
        /// </summary>
        /// <param name="data"></param>
        /// <param name="maxVal"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private (int startPosition, int endPositioon, double Area) getPeakLimits(List<double> data, (double value, int position) maxVal, double threshold)
        {
            (int startPosition, int endPosition, double Area) result;
            int stepsBelowThreshold = 0;
            result.startPosition = 0;
            for (int steps = maxVal.position; steps >= 0; steps--)
            {
                if (data[steps] <= threshold)
                {
                    stepsBelowThreshold++;
                    if (stepsBelowThreshold >= 3)
                    {
                        result.startPosition = steps + stepsBelowThreshold;
                        break;
                    }
                }
            }

            stepsBelowThreshold = 0;
            result.endPosition = data.Count - 1;
            for (int steps = maxVal.position; steps < data.Count; steps++)
            {
                if (data[steps] <= threshold)
                {
                    stepsBelowThreshold++;
                    if (stepsBelowThreshold >= 3)
                    {
                        result.endPosition = steps - stepsBelowThreshold;
                        break;
                    }
                }
            }

            result.Area = 0.0d;
            for (int i = result.startPosition; i <= result.endPosition; i++) result.Area += data[i];

            return (result);
        }

        /*
        /// <summary>
        /// Uses a differentiated squared envelope to detect peaks
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="leadinLimit"></param>
        /// <param name="leadoutLimit"></param>
        /// <param name="thresholdFactor"></param>
        /// <returns></returns>
        private ObservableList<Peak> getPeaks2(ref List<double> envelope, int leadinLimit, int leadoutLimit, float thresholdFactor, int smooth)
        {
            var result = new ObservableList<Peak>();
            if (envelope.Count < 100) return (result);

            int seglength = envelope.Count / 20;
            if (seglength < 100) seglength = 100;
            List<double> slope = new List<double>();

            for (int seg = 0; seg < envelope.Count - seglength; seg += seglength / 2)
            {
                var segData = envelope.Skip(seg).Take(seglength);
                var segMean = segData.Average();
                var segMax = segData.Max();
                var segSD = segData.StandardDeviation();
                var threshold = segMean + (segSD * thresholdFactor);

                peakState state = peakState.NOTINPEAK;
                int SampleNumber = seg;
                var sample = envelope[SampleNumber];
                var lastSample = envelope[seg > 0 ? seg - 1 : seg];

                leadinLimit = leadinLimit / smooth;
                leadoutLimit = leadoutLimit / smooth;
                if (leadoutLimit < 10) leadoutLimit = 10;
                if (leadinLimit < 3) leadinLimit = 3;

                int leadInCount = 0;
                int leadOutCount = 0;
                double peakHeight = 0.0d;
                int peakPos = 0;
                int peakStart = 0;

                while (SampleNumber < seg + seglength)
                {
                    slope.Add(sample - lastSample);
                    switch (state)
                    {
                        case peakState.NOTINPEAK:
                            if (sample > threshold)
                            {
                                leadInCount = 1;
                                peakHeight = sample;
                                state = peakState.INPEAKLEADIN;
                            }
                            else
                            {
                            }
                            break;

                        case peakState.INPEAKLEADIN:
                            if (sample > threshold)
                            {
                                leadInCount++;
                                if (sample > peakHeight)
                                {
                                    peakHeight = sample;
                                    peakPos = SampleNumber;
                                }
                                peakStart = SampleNumber;
                                if (leadInCount > leadinLimit)
                                {
                                    peakStart = SampleNumber - leadinLimit;
                                    state = peakState.INPEAK;
                                }
                            }
                            else
                            {
                                leadInCount = 0;
                                state = peakState.NOTINPEAK;
                            }
                            break;

                        case peakState.INPEAK:
                            if (sample > threshold)
                            {
                                if (sample > peakHeight)
                                {
                                    peakHeight = sample;
                                    peakPos = SampleNumber;
                                }
                            }
                            else
                            {
                                leadOutCount = 1;
                                state = peakState.INPEAKLEADOUT;
                            }
                            break;

                        case peakState.INPEAKLEADOUT:
                            if (sample > threshold)
                            {
                                if (sample > peakHeight)
                                {
                                    peakHeight = sample;
                                    peakPos = SampleNumber;
                                }
                                leadOutCount = 0;
                                state = peakState.INPEAK;
                            }
                            else
                            {
                                leadOutCount++;
                                if (leadOutCount > leadoutLimit)
                                {
                                    Peak peak = makePeak(envelope, slope, peakPos, 0, (float)threshold, result, smooth, leadinLimit, leadoutLimit);
                                    if (peak != null)
                                    {
                                        result.Add(peak);
                                    }
                                }
                            }
                            break;

                        default: break;
                    }
                    SampleNumber++;
                    if (SampleNumber < envelope.Count)
                        sample = envelope[SampleNumber];
                }
            }

            return (result);
        }*/ //GetPeaks2

        /*
        private ObservableList<Peak> getPeaks3(ref List<double> fullPassSmoothedEnvelope, int leadinLimit, int leadoutLimit, float thresholdFactor,
                            int smooth, double threshold = double.NaN)
        {
            int envelopeSize = fullPassSmoothedEnvelope.Count;
            var result = new ObservableList<Peak>();
            if (envelopeSize < 100) return (result); // dont bother looking if very short pass less than 0.26ms at 384ksps

            int seglength = SampleRate / smooth; // work on segments of 1s or less
            while (seglength < envelopeSize && (envelopeSize % seglength < seglength / 2.0d)) seglength++;
            if (seglength < 100) seglength = 100;

            List<double> slope = new List<double>();
            double last = fullPassSmoothedEnvelope[0];
            foreach (var val in fullPassSmoothedEnvelope)
            {
                slope.Add(val - last);
                last = val;
            }
            if (double.IsNaN(threshold))
            {
                threshold = GetThreshold(fullPassSmoothedEnvelope);
            }
            int startOfSegInSmoothedEnvelope = 0;
            while (startOfSegInSmoothedEnvelope < envelopeSize)
            {
                int dataLeft = envelopeSize - startOfSegInSmoothedEnvelope;
                int actualSegLength = Math.Min(dataLeft, seglength);
                var segData = fullPassSmoothedEnvelope.Skip(startOfSegInSmoothedEnvelope).Take(actualSegLength);
                //var segData = fullPassSmoothedEnvelope;
                var segMean = segData.Average();
                var segMax = segData.Max();
                var segSD = segData.StandardDeviation();
                //var threshold = segMean + (segSD * thresholdFactor);

                Debug.WriteLine($"\n\nSegment of {segData.Count()}:- mean={segMean}, max={segMax}, sd={segSD}\n");

                int smoothedLeadinLimit = leadinLimit / smooth;
                int smoothedLeadoutLimit = leadoutLimit / smooth;
                if (smoothedLeadoutLimit < 10) smoothedLeadoutLimit = 10;
                if (smoothedLeadinLimit < 3) smoothedLeadinLimit = 3;

                //var threshold = GetThreshold(segData);

                int depth = 0;

                getSegmentPeaks2(ref fullPassSmoothedEnvelope, ref slope, startOfSegInSmoothedEnvelope, actualSegLength, threshold,
                    ref result, smooth, smoothedLeadinLimit, smoothedLeadoutLimit, depth);

                startOfSegInSmoothedEnvelope += seglength;
            }

            result = new ObservableList<Peak>(result.OrderBy(pk => pk.getStartAsSampleInPass()).ToList());
            if (result.Count > 1)
            {
                int prevStart = result[0].getStartAsSampleInPass();
                for (int i = 1; i < result.Count; i++)
                {
                    result[i].SetPrevIntervalSamples(result[i].getStartAsSampleInPass() - prevStart);
                    prevStart = result[i].getStartAsSampleInPass();
                }
            }

            return (result);
        }*/ // GetPeaks3

        /// <summary>
        /// Another attempt at getPeaks.  Called recursively.
        /// Find the biggest vale in the supplied data segment, extracts the peak surrounding that point,
        /// then calls itself for the data to the left of the extracted peak, then calls itself for the data
        /// to the right of the extracted peak.  The extracted peak is added to the peaks list and the list is returned.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="leadInLimit"></param>
        /// <param name="leadoutLimit"></param>
        /// <param name="thresholdFactor"></param>
        /// <param name="smoothedBy"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private ObservableList<Peak> getPeaks4(List<double> data, int leadInLimit, int leadoutLimit, float thresholdFactor,
            int smoothedBy, double threshold = double.NaN, ObservableList<Peak> peaks = null, int dataStartInPass = 0)
        {
            for (int i = 0; i < GP4depth; i++) Debug.Write("\t");
            Debug.WriteLine($"GP4:- from {dataStartInPass / (double)SampleRate:0.000} to {(dataStartInPass + (data.Count * smoothedBy)) / (double)SampleRate:0.000} = {dataStartInPass}-{dataStartInPass + (data.Count * smoothedBy)}");
            if (peaks == null) peaks = new ObservableList<Peak>();
            if (data == null || !data.Any()) return (peaks);
            if (double.IsNaN(threshold)) threshold = data.Average() * thresholdFactor;
            int minLength = (leadInLimit + leadoutLimit) / smoothedBy;
            if (minLength < 10) minLength = 10;

            if (data.Count < minLength) return (peaks); // data too short to be searchable

            (double value, int position) maxVal = getMaximum(data);
            if (maxVal.position < minLength) return (peaks);
            if (maxVal.value < threshold) return (peaks); // no further big enough peaks in this data segment

            Peak peak = ExtractPeak(data, maxVal, smoothedBy, threshold, dataStartInPass, out int peakStartInData, out int peakEndInData);

            peaks.Add(peak);

            GP4depth++;
            Debug.Write("L");
            // search to the left
            peaks = getPeaks4(data.Take(peakStartInData).ToList(), leadInLimit, leadoutLimit, thresholdFactor, smoothedBy,
                double.NaN, peaks, dataStartInPass);

            Debug.Write("R");
            // search to the right
            peaks = getPeaks4(data.Skip(peakEndInData).ToList(), leadInLimit, leadoutLimit, thresholdFactor, smoothedBy,
                double.NaN, peaks, dataStartInPass + (peakEndInData * smoothedBy));
            GP4depth--;
            return (peaks);
        }

        /// <summary>
        /// Given an envelope and a detected peak within that envelope, uses the slope of the envelope and the
        /// peak position to find the start of the peak within the envelope
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="slope"></param>
        /// <param name="peakPosinSmoothedPass"></param>
        /// <param name="absoluteThreshold"></param>
        /// <returns></returns>
        private int getPeakStart(List<double> envelope, List<double> slope, int peakPosinSmoothedPass, float absoluteThreshold, int leadinLimit)
        {
            int result = -1;

            if (peakPosinSmoothedPass < leadinLimit) return (result);
            int peakstartOfLeadin = getpeakStartOfLeadin(envelope, peakPosinSmoothedPass, absoluteThreshold, leadinLimit);

            return (peakstartOfLeadin);

            //var section = envelope.Skip(peakstartOfLeadin).Take(peakPos - peakstartOfLeadin);
            var slopeSection = slope.Skip(peakstartOfLeadin).Take(peakPosinSmoothedPass - peakstartOfLeadin).ToArray();
            if (slopeSection.Count() < 10) return (-1);
            var meanSlope = slopeSection.Average();
            int peakStart = peakPosinSmoothedPass - (int)(envelope[peakPosinSmoothedPass] / meanSlope);
            Debug.WriteLine($" mean slope={meanSlope}, peak={envelope[peakPosinSmoothedPass]}, peakStart={peakStart}=ht/slope");
            peakStart = Math.Min(peakStart, peakstartOfLeadin);
            return (peakStart);
        }

        private int getpeakStartOfLeadin(List<double> envelope, int peakPos, float absoluteThreshold, int leadinLimit)
        {
            peakState currentState = peakState.INPEAK;
            leadinLimit = 20;
            int leadinCount = 0;
            int i = 0;
            for (i = peakPos; i >= 0; i--)
            {
                switch (currentState)
                {
                    case peakState.INPEAK:
                        if (envelope[i] < absoluteThreshold)
                        {
                            currentState = peakState.INPEAKLEADIN;
                            leadinCount = 1;
                        }

                        break;

                    case peakState.INPEAKLEADIN:
                        if (envelope[i] >= absoluteThreshold)
                        {
                            currentState = peakState.INPEAK;
                            leadinCount = 0;
                        }
                        else
                        {
                            leadinCount++;
                            if (leadinCount > leadinLimit)
                            {
                                return (i + leadinLimit);
                            }
                        }
                        break;

                    default: break;
                }
            }
            return (i + 1 + leadinCount);
        }

        private float[] getQuietPortion(float secs, float factor)
        {
            float[] result;
            float[] data = passDataAccessBlock.getData();
            int sizeInSamples = (int)(secs * SampleRate);
            result = new float[sizeInSamples];
            float avg = data.Average() * factor;
            for (int i = 0; i < data.Length - sizeInSamples; i++)
            {
                float[] sampleData = passDataAccessBlock.getData((int)(passDataAccessBlock.BlockStartInFileInSamples + i), sizeInSamples);
                if (sampleData.Max() < avg)
                {
                    result = sampleData;
                    return (result);
                }
            }
            // if we failed to find a portion of length secs that was below the average level in the pass
            // then we try again looking a for half that length below 1.5xthe average value of the pass
            // recursively with smaller samples size and hight factors until the sample size becomes less
            // than two fft's worth (5.3ms at 384000 sps)
            if (sizeInSamples > 2048)
            {
                result = getQuietPortion(secs / 2, 1.5f * factor);
            }

            return (null);
        }

        /// <summary>
        /// Searches the envelope for a section in which all the envelope is below the threshold for a duration of 5000 samples
        /// </summary>
        /// <returns></returns>
        private int getQuietStart(ref List<double> envelope, float threshold)
        {
            //peakState currentPeakState = peakState.NOTINPEAK;
            int counter = 0;
            for (int i = 0; i < envelope.Count; i++)
            {
                if (envelope[i] < threshold)
                {
                    counter++;
                    if (counter >= 5000)
                    {
                        return (i - 5000);
                    }
                }
                else
                {
                    counter = 0;
                }
            }
            return (-1);
        }

        //private float[] quiet;
        //private float[] envelope;
        /// <summary>
        /// elaborate process to find a quiet section and derive a threshold value from it
        /// </summary>
        /// <param name="fullPassSmoothedEnvelope"></param>
        /// <param name="thresholdFactor"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private int getQuietStart2(ref List<double> fullPassSmoothedEnvelope, decimal thresholdFactor, out double threshold)
        {
            // @384000 sps we get 19,200 sample/s (smoothed by 20), 19 samples/ms
            threshold = fullPassSmoothedEnvelope.Average() * (double)thresholdFactor;
            int passLength = fullPassSmoothedEnvelope.Count;
            if (passLength < 100) return (-1); // not a long enough section to work with
            int segLength = passLength;
            int numberOfSegs = 1;
            while (segLength > passLength / 5)
            {
                numberOfSegs++;
                segLength = passLength / numberOfSegs;
            }
            double minVariance = double.MaxValue;
            double bestMean = 0.0d;
            int bestStart = -1;

            for (int i = 0; i < numberOfSegs; i++)
            {
                int start = i * segLength;
                var data = fullPassSmoothedEnvelope.Skip(start).Take(segLength - 1);
                double variance = data.VariancePNaN();
                if (variance < minVariance)
                {
                    minVariance = variance;
                    bestMean = data.Average();
                    var sd = data.StandardDeviation();
                    threshold = (bestMean + (sd * 2.0d)) * (double)thresholdFactor;
                    if (data.Max() < threshold)
                    {
                        bestStart = start + (segLength / 3);
                    }
                }
            }

            return (bestStart);
        }

        /// <summary>
        /// Recursive function to get the biggest peak the specified raange and then call itself to add the biggest peak in the
        /// region to the left of the peak and again in the region to the right of the peak until the data section ahs no further
        /// maximum greater than the threshold
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="slope"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="threshold"></param>
        /// <param name="result"></param>
        /// <param name="smooth"></param>
        /// <param name="leadinLimit"></param>
        /// <param name="leadoutLimit"></param>
        private void getSegmentPeaks(ref List<double> envelope, ref List<double> slope, int start, int count, double threshold, ref ObservableList<Peak> result, int smooth, int leadinLimit, int leadoutLimit, int depth = 0)
        {
            for (int i = 0; i < depth; i++) Debug.Write(".");
            depth++;
            Debug.Write($"Get Peaks from\t{start}\tfor\t{count}");
            if (count <= leadinLimit)
            {
                Debug.WriteLine(" - Abandon - too short");
                return;
            }
            var dataSection = envelope.Skip(start).Take(count);
            var slopeSection = slope.Skip(start).Take(count);
            int peakPos = dataSection.MaxIndex() + start;
            if (peakPos < leadoutLimit)
            {
                var shtData = dataSection.Skip(leadoutLimit);
                peakPos = shtData.MaxIndex() + leadoutLimit + start;
            }

            if (envelope[peakPos] < threshold)
            {
                Debug.WriteLine("  Max less than threshold");
                return;
            }
            Debug.WriteLine($"  Max={dataSection.Max()} at {peakPos}={((peakPos + start) * smooth) * 1000 / SampleRate}ms");
            Peak peak = makePeak(dataSection.ToList(), slopeSection.ToList(), peakPos, start, (float)threshold, result, smooth, leadinLimit, leadoutLimit);
            if (peak != null)
            {
                if (peak.getStartAsSampleInPass() > smooth)
                {
                    result.Add(peak);
                    Debug.Write("\t\t");
                }
                else
                {
                    Debug.Write("Reject early start:-\t");
                }
            }
            if (peak != null)
            {
                Debug.WriteLine($"start={(peak.getStartAsSampleInPass() / (float)peak.GetSampleRatePerSecond()) * 1000.0d}ms end={peak.peakWidthMs}ms");
            }
            else
            {
                Debug.WriteLine($"No peak, retry either side of {peakPos}={((peakPos * smooth) / (double)SampleRate) * 1000.0d}");
            }
            // now repeat on the section leading up to the peak
            int newStart = start;
            int peakStart = peakPos - leadinLimit;
            //if (peak != null)
            //{
            //   peakStart = peak.getStartAsSampleInPass() / smooth;
            //}

            int newCount = peakStart - start;
            if (newCount > leadinLimit)
            {
                Debug.Write("L ");
                getSegmentPeaks(ref envelope, ref slope, newStart, newCount, threshold, ref result, smooth, leadinLimit, leadoutLimit, depth);
            }

            // now do the section after the peak
            newStart = peakPos + leadoutLimit;
            //if (peak != null)
            //{
            //    newStart = (peak.getStartAsSampleInPass() + peak.getPeakWidthSamples()) / smooth;
            //}
            newCount = (start + count) - newStart;
            if (newCount > leadinLimit)
            {
                Debug.Write("R ");
                getSegmentPeaks(ref envelope, ref slope, newStart, newCount, threshold, ref result, smooth, leadinLimit, leadoutLimit, depth);
            }
        }

        /*
        /// <summary>
        /// revised recursive function to find peaks in a segment of an envelope
        /// </summary>
        /// <param name="fullPassSmoothedEnvelope"></param>
        /// <param name="slope"></param>
        /// <param name="startOfSectionInSmoothedEnvelope"></param>
        /// <param name="sectionLength"></param>
        /// <param name="threshold"></param>
        /// <param name="result"></param>
        /// <param name="smooth"></param>
        /// <param name="smoothedLeadinLimit"></param>
        /// <param name="smoothedLeadoutLimit"></param>
        /// <param name="depth"></param>
        private void getSegmentPeaks2(ref List<double> fullPassSmoothedEnvelope, ref List<double> slope, int startOfSectionInSmoothedEnvelope, int sectionLength, double threshold,
            ref ObservableList<Peak> result, int smooth, int smoothedLeadinLimit, int smoothedLeadoutLimit, int depth)
        {
            for (int i = 0; i < depth; i++) Debug.Write(".");
            depth++;
            Debug.Write($" Get Peaks from\t{startOfSectionInSmoothedEnvelope}\tfor\t{sectionLength}");
            if (sectionLength <= smoothedLeadinLimit)
            {
                Debug.WriteLine(" - Abandon - too short");
                return;
            }

            var dataSection = fullPassSmoothedEnvelope.Skip(startOfSectionInSmoothedEnvelope).Take(sectionLength);
            var slopeSection = slope.Skip(startOfSectionInSmoothedEnvelope).Take(sectionLength);
            int peakPosinSmoothedPass = dataSection.MaxIndex() + startOfSectionInSmoothedEnvelope;
            //double threshold = GetThreshold(dataSection);
            if (peakPosinSmoothedPass < smoothedLeadoutLimit)
            {
                var shtData = dataSection.Skip(smoothedLeadoutLimit);
                peakPosinSmoothedPass = shtData.MaxIndex() + smoothedLeadoutLimit + startOfSectionInSmoothedEnvelope;
                //threshold = GetThreshold(shtData);
            }

            Debug.WriteLine($"  Max={fullPassSmoothedEnvelope[peakPosinSmoothedPass]} threshold={threshold}");
            if (fullPassSmoothedEnvelope[peakPosinSmoothedPass] < threshold)
            {
                Debug.WriteLine("  Max less than threshold");
                return;
            }

            FindPeak(fullPassSmoothedEnvelope, slope, peakPosinSmoothedPass, threshold, smoothedLeadinLimit, smoothedLeadoutLimit,
                out int startOfPeak, out int widthOfPeak, out double peakArea);

            Debug.WriteLine($" found start={startOfPeak} width={widthOfPeak}");

            if (startOfPeak > startOfSectionInSmoothedEnvelope && widthOfPeak > smoothedLeadinLimit && widthOfPeak > 20)
            {
                Peak peak = Peak.Create(result.Count + 1, startOfPeak * smooth, widthOfPeak * smooth, peakArea * smooth, (float)fullPassSmoothedEnvelope[peakPosinSmoothedPass],
                    0, SampleRate, OffsetInSegmentInSamples, recordingNumber, (float)threshold);
                result.Add(peak);
                Debug.WriteLine($"{result.Count()} - peak at {(peak.getStartAsSampleInPass() / (float)SampleRate) * 1000.0f:####.}ms width {peak.peakWidthMs:###.##}ms");
            }

            startOfPeak = startOfPeak - (SampleRate / (smooth * 100)); // enforce 10ms gap prior to detected peak
            if (startOfPeak > startOfSectionInSmoothedEnvelope)
            {
                Debug.Write("\nL ");
                getSegmentPeaks2(ref fullPassSmoothedEnvelope, ref slope, startOfSectionInSmoothedEnvelope,
                    startOfPeak - startOfSectionInSmoothedEnvelope, threshold,
                    ref result, smooth, smoothedLeadinLimit, smoothedLeadoutLimit, depth);
            }

            int endOfPeak = startOfPeak + widthOfPeak + 2 * (SampleRate / (smooth * 100)); // with 10ms post peak clearance, *2 to account for moving start of peak left

            int dataToTheRight = fullPassSmoothedEnvelope.Count - endOfPeak;
            int sizeOfBlock = (startOfSectionInSmoothedEnvelope + sectionLength) - endOfPeak;
            if (endOfPeak > startOfSectionInSmoothedEnvelope && dataToTheRight > smoothedLeadoutLimit)
            {
                Debug.Write("\nR ");
                getSegmentPeaks2(ref fullPassSmoothedEnvelope, ref slope, endOfPeak, sizeOfBlock, threshold,
                    ref result, smooth, smoothedLeadinLimit, smoothedLeadoutLimit, depth);
            }
        }*/ // getsegmentpeaks2

        /*
        private double GetThreshold(IEnumerable<double> dataSection)
        {
            double mean = dataSection.Average();
            double sd = dataSection.StandardDeviation();
            return ((mean + mean) * (double)thresholdFactor * 2.0d);
        }*/ // GetThreshold

        /// <summary>
        /// Having detected a qualifying peak in the envelope, creates a new peak in
        /// the peakList using the available data
        /// </summary>
        /// <param name="envelope">section of smoothed envelope </param>
        /// <param name="slope">slope of envelope</param>
        /// <param name="peakPos"></param>
        /// <param name="offset"> start of the smoothed envelope in the smoothed pass</param>
        /// <param name="AbsoluteThreshold"></param>
        /// <param name="peakList">list of peaks collectedd so far</param>
        /// <param name="smoothingValue">number of real samples for each envelope sample</param>
        /// <param name="leadInLimit"></param>
        /// <param name="leadOutLimit"></param>
        private Peak makePeak(List<double> envelope, List<double> slope, int peakPos, int offset,
            float AbsoluteThreshold, ObservableList<Peak> peakList, int smoothingValue, int leadInLimit, int leadOutLimit)
        {
            if (envelope.Count() <= 0) return (null);
            int peakNumber = peakList.Count() + 1;
            float maxHeight = (float)envelope.ElementAt(peakPos);
            int startOfPassInSegment = this.OffsetInSegmentInSamples;
            int RecordingNumber = this.recordingNumber;

            int nextInterval;
            int previousInterval;
            int peakStartInPass;
            int peakWidthInSamples;
            double peakArea;
            getPeakDetails(envelope, slope, leadInLimit, leadOutLimit, peakPos, offset, AbsoluteThreshold, smoothingValue,
                out peakStartInPass, out peakWidthInSamples, out peakArea);
            if (peakWidthInSamples < leadInLimit * smoothingValue)
            {
                return (null);
            }
            if (peakList.Count > 0)
            {
                previousInterval = peakStartInPass - peakList[peakList.Count - 1].getStartAsSampleInPass();
                nextInterval = envelope.Count - (peakStartInPass + peakWidthInSamples);
            }
            else
            {
                previousInterval = 0;
                nextInterval = 0;
            }

            if (previousInterval < 10000) return (null);
            if (nextInterval < 10000) return (null);

            Peak peak = Peak.Create(peakNumber, peakStartInPass, peakWidthInSamples, peakArea, maxHeight,
                previousInterval, SampleRate, startOfPassInSegment, RecordingNumber, AbsoluteThreshold);

            return (peak);
        }

        /// <summary>
        /// goes through a list of peaks in discovered order, sorts them by startInpass
        /// and sets the previous interval for each, returning the modified list"
        /// </summary>
        /// <param name="peakList"></param>
        /// <returns></returns>
        private ObservableList<Peak> setPeakIntervals(ObservableList<Peak> peakList)
        {
            ObservableList<Peak> result = new ObservableList<Peak>();

            var sortedList = (from pk in peakList
                              orderby pk.getStartAsSampleInPass()
                              select pk)?.ToList();

            int lastPos = 0;
            for (int i = 0; i < sortedList.Count(); i++)
            {
                var interval = sortedList[i].getStartAsSampleInPass() - lastPos;

                sortedList[i].SetPrevIntervalSamples(interval);

                if (sortedList[i].prevIntervalMs > 10.0f)
                {
                    result.Add(sortedList[i]);
                    lastPos = sortedList[i].getStartAsSampleInPass();
                }
            }

            return (result);
        }
    }

    #region BackgroundConverter (ValueConverter)

    public class BackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null || parameter == null) return (System.Windows.Media.Brushes.White);

                System.Windows.Media.Brush[] brushes = parameter as System.Windows.Media.Brush[];
                if (brushes == null) return (System.Windows.Media.Brushes.White);

                if (value is int)
                {
                    int val = (int)value;
                    if (val % 2 == 0)
                    {
                        return (SolidColorBrush)brushes[0];
                    }
                }
                return (SolidColorBrush)brushes[1];
            }
            catch
            {
                return System.Windows.Media.Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BackgroundConverter (ValueConverter)
}