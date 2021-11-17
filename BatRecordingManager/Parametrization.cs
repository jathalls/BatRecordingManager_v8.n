using BatCallAnalysisControlSet;
using LinqStatistics;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using UniversalToolkit;

namespace BatRecordingManager
{
    public class Peak
    {
        public int endSample;
        public int lengthInSamples;
        public double peakHeight;
        public double peakInMs;
        public int startOverall;
        public int startSample;

        public Peak(int start, int end, int startOverall, int sampleRate, double peakHeight, int peakLoc)
        {
            startSample = start;
            endSample = end;
            this.startOverall = startOverall;
            this.sampleRate = sampleRate;
            lengthInSamples = end - start;
            this.peakHeight = peakHeight;
            peakInMs = (peakLoc * 1000.0d) / (double)sampleRate;
        }

        public FrequencyDataSet frequencyData { get; set; }

        public double lengthInms
        {
            get
            {
                return ((double)lengthInSamples / (double)(sampleRate / 1000));
            }
        }

        public double startInPass
        {
            get
            {
                return (((double)startOverall * 1000) / (double)sampleRate);
            }
        }

        internal static bool isValidPeak(Peak peak)
        {
            if (peak == null) return (false);
            if (peak.lengthInms < 0.5) return (false);
            if (peak.lengthInms > 100.0d) return (false);

            return (true);
        }

        /// <summary>
        /// Writes essential data to the named file
        /// </summary>
        /// <param name="fname"></param>
        internal void WriteData(string fname)
        {
            File.AppendAllText(fname, $"start={startOverall / sampleRate}s, Start={startSample}, length in samples={lengthInSamples}\n");
        }

        private int sampleRate;
    }

    /// <summary>
    /// A class to take a portion of waveform, identify calls and extract parameters therefrom
    /// AnalysisMode may be 0 (5s, default), 1 (single pulse) or 5 (5 loudest pulses)
    /// </summary>
    internal class Parametrization
    {
        public List<Peak> unfilteredPeaks;

        public Parametrization(List<float> alldata, int sampleRate, int AnalysisMode = 5)
        {
            Alldata = alldata;
            SampleRate = sampleRate;
            this.AnalysisMode = AnalysisMode;
            if (this.AnalysisMode != 0 && this.AnalysisMode != 1 && this.AnalysisMode != 5) this.AnalysisMode = 0;
        }

        public event EventHandler saveClicked;

        public List<float> Alldata { get; }

        //public List<FrequencyDataSet> AllFrequencyData { get; set; } = new List<FrequencyDataSet>();
        public List<Peak> AllPeaks { get; set; }

        public List<double> envelopeData { get; set; }
        public int SampleRate { get; set; }

        internal void AnalysePulse(System.Windows.Point pe)
        {
            if (unfilteredPeaks.Count > 0)
            {
                selectNearestPeak(pe);
                GetFrequencyParams(isRelaxed: true);
                ReferenceCall call = getCallFromAllData(); // uses data in AllPeaks and in AllFrequencyData
                call.setToSettings();

                var analysisWindow = new CallAnalysisWindow();
                analysisWindow.WindowState = System.Windows.WindowState.Maximized;
                analysisWindow.Show();
                analysisWindow.AnalysisChartGrid.saveClicked += AnalysisChartGrid_saveClicked;
            }
        }

        /// <summary>
        /// Analyses a pulse within a defined frequency/time region.  The supplied data (not envelope) has already
        /// been trimmed to the section required and filtered to remove lower and higher frequency components
        /// </summary>
        /// <param name="sectionData"></param>
        internal void AnalysePulse(List<float> sectionData, int startSample)
        {
            var sectionEnvelope = getEnvelope(sectionData);
            GetIsolatedPeak(sectionEnvelope, startSample); // finds a peak within the sectionData and makes it the only peak in AllPeaks
            GetIsolatedFrequencyParams(sectionData, startSample); // finds relaxed spectral data in the only peak in Allpeaks

            ReferenceCall call = getCallFromAllData(); // uses data in AllPeaks and in AllFrequencyData
            call.setToSettings();

            var analysisWindow = new CallAnalysisWindow();
            analysisWindow.WindowState = System.Windows.WindowState.Maximized;
            analysisWindow.Show();
            analysisWindow.AnalysisChartGrid.saveClicked += AnalysisChartGrid_saveClicked;
        }

        internal void CalculateParameters(List<Spectrum> spectra, int FFTSize, int FFTOverlapSamples)
        {
            this.spectra = spectra;
            this.FFTSize = FFTSize;
            this.FFTOverlapSamples = FFTOverlapSamples;
            getEnvelope();
            getCalls();
        }

        protected virtual void OnSaveClicked(callEventArgs e) => saveClicked?.Invoke(this, e);

        private int depth = 0;

        private int FFTOverlapSamples;

        private int FFTSize;

        private bool overFlow = false;

        private int samplesPerMs;

        private List<Spectrum> spectra;

        private double threshold;

        private int AnalysisMode { get; set; }

        private void AnalysisChartGrid_saveClicked(object sender, EventArgs e)
        {
            OnSaveClicked(e as callEventArgs);
        }

        /// <summary>
        /// Takes the data for multiple peaks and condenses them into a referenceCall structure
        /// </summary>
        /// <param name="allPeaks"></param>
        /// <returns></returns>
        private ReferenceCall getCallFromAllData()
        {
            ReferenceCall call = new ReferenceCall();
            if (AllPeaks.Count > 0)
            {
                double fsmean = AllPeaks.Select(v => (double)v.frequencyData.startFrequency / 1000.0d).Average();
                double femean = AllPeaks.Select(v => (double)v.frequencyData.endFrequency / 1000.0d).Average();
                double fpmean = AllPeaks.Select(v => (double)v.frequencyData.peakFrequency / 1000.0d).Average();
                double durmean = AllPeaks.Select(v => ((double)v.lengthInSamples * 1000.0d / SampleRate)).Average();

                double fssd = 0.0d;
                double fesd = 0.0d;
                double fpsd = 0.0d;
                double intervalsd = 0.0d;
                double intmean = 0.0d;
                double dursd = 0.0d;

                List<double> intervalList = new List<double>();
                if (AllPeaks.Count > 1)
                {
                    for (int i = 0; i < AllPeaks.Count - 1; i++)
                    {
                        intervalList.Add((AllPeaks[i + 1].startOverall - AllPeaks[i].startOverall) / ((double)SampleRate / 1000.0d));
                    }
                    //intmean = intervalList.Average();

                    // remove outliers more than 1sd from the mean
                    //if (intervalList.Count >= 2) intervalsd = intervalList.StandardDeviation();
                    intervalList = reduceIntervalList(intervalList);
                    intervalList = reduceIntervalList(intervalList);
                    //intervalList = intervalList.Where(v => v >= intmean - intervalsd && v <= intmean + intervalsd).ToList();
                    intmean = intervalList.Average();
                    if (intervalList.Count > 1) intervalsd = intervalList.StandardDeviation();
                    else intervalsd = 0.0d;

                    fssd = AllPeaks.Select(v => (double)v.frequencyData.startFrequency / 1000.0d).StandardDeviation();
                    fesd = AllPeaks.Select(v => (double)v.frequencyData.endFrequency / 1000.0d).StandardDeviation();
                    fpsd = AllPeaks.Select(v => (double)v.frequencyData.peakFrequency / 1000.0d).StandardDeviation();
                    dursd = AllPeaks.Select(v => ((double)v.lengthInSamples * 1000.0d / SampleRate)).StandardDeviation();
                }

                call.setStartFrequency((fsmean - fssd, fsmean, fsmean + fssd));
                call.setEndFrequency((femean - fesd, femean, femean + fesd));
                call.setPeakFrequency((fpmean - fpsd, fpmean, fpmean + fpsd));
                call.setInterval(((intmean - intervalsd) > 0.0d ? (intmean - intervalsd) : 0.0d, intmean, intmean + intervalsd));
                call.setDuration((durmean - dursd, durmean, durmean + dursd));

                double bwmean = fsmean - femean;
                double bwsd = Math.Sqrt(Math.Pow(fssd, 2) + Math.Pow(fesd, 2));
                call.setBandwidth((bwmean - bwsd, bwmean, bwmean + bwsd));
            }

            return (call);
        }

        private void getCalls()
        {
            if (envelopeData.Count > (SampleRate * 5))
            {
                // we have a sample length greater than 5s
                double max = envelopeData.Max();
                int maxpos = envelopeData.IndexOf(max);
                int start = maxpos - (int)(SampleRate * 2.5f);
                if (start < 0) start = 0;
                envelopeData = envelopeData.Skip(start).Take(SampleRate * 5).ToList();
                // limits envelope data to 5s centred on the biggest peak
            }

            threshold = envelopeData.OrderBy(v => v).Take(envelopeData.Count / 10).Average();
            AllPeaks = new List<Peak>();
            overFlow = false;
            depth = 0;
            samplesPerMs = SampleRate / 1000;

            getPeaks(envelopeData, 0);

            unfilteredPeaks = AllPeaks;

            AllPeaks = (from pk in AllPeaks
                        where (double)pk.lengthInSamples > (SampleRate / 2000.0d) && (double)pk.lengthInSamples < (3.0d * SampleRate / 100.0d)
                        orderby pk.startOverall
                        select pk).ToList();

            GetFrequencyParams();

            var byHeight = AllPeaks.OrderByDescending(pk => pk.peakHeight).Select(p => p.peakHeight).ToList();

            if (AnalysisMode == 1 && AllPeaks.Count > 0)
            {
                var maxPeak = AllPeaks.OrderBy(v => v.peakHeight).First();
                AllPeaks.Clear();
                AllPeaks.Add(maxPeak);
            }
            else if (AnalysisMode == 5 && AllPeaks.Count > 5)
            {
                AllPeaks = AllPeaks.OrderByDescending(v => v.peakHeight).Take(5).OrderBy(v2 => v2.startOverall).ToList();
            }

#if DEBUG

            Debug.WriteLine($"Found frequencies for {AllPeaks.Count} peaks");

            string filename = @"C:\BRMTestData\Params.txt";
            if (File.Exists(filename)) File.Delete(filename);
            List<String> lines = new List<string>();
            lines.Add(" Filename	  		    st	    Dur	     Prev	     Next	   Fmax	   Fmin	  Fmean	    Tk	     Fk	     Qk	    Tc	     Fc	      S1	      Sc	  Qual	    Pmc	");
            for (int i = 0; i < AllPeaks.Count; i++)
            {
                var peak = AllPeaks[i];
                double prevint = (i > 0 ? AllPeaks[i].startOverall - AllPeaks[i - 1].startOverall : 0.0d) / (double)(SampleRate / 1000);
                double nextInt = (i < AllPeaks.Count - 1 ? AllPeaks[i + 1].startOverall - AllPeaks[i].startOverall : 0.0d) / (double)(SampleRate / 1000);
                lines.Add($"filename\t{i}\t{((double)peak.lengthInSamples * 1000.0d / (double)SampleRate):#0.0}\t{prevint:##0.0}\t{nextInt:##0.0}\t" +
                    $"{(double)AllPeaks[i].frequencyData.startFrequency / 1000.0d}\t{(double)AllPeaks[i].frequencyData.endFrequency / 1000.0d}\t{(double)AllPeaks[i].frequencyData.peakFrequency / 1000.0d}\t" +
                    $"0.0\t0.0\t0.0\t0.0\t0.0\t0.0\t0.0\t0.0\t0.0");
            }
            File.WriteAllLines(filename, lines);
#endif

            ReferenceCall call = getCallFromAllData(); // uses data in AllPeaks and in AllFrequencyData
            call.setToSettings();

            var analysisWindow = new CallAnalysisWindow();
            analysisWindow.WindowState = System.Windows.WindowState.Maximized;
            analysisWindow.Show();

            List<CallData> displayableData = getDisplayableData(); // get the details of each analysed call
            analysisWindow.SetDisplayableCallData(displayableData); // and send them to the display window
            analysisWindow.AnalysisChartGrid.saveClicked += AnalysisChartGrid_saveClicked;
        }

        /// <summary>
        /// Using the data in AllPeaks, generates a list of displayable parameters for the Analysis Window dataGrid
        /// </summary>
        /// <returns></returns>
        private List<CallData> getDisplayableData()
        {
            List<CallData> data = new List<CallData>();
            if (AllPeaks != null && AllPeaks.Any())
            {
                for (int i = 0; i < AllPeaks.Count; i++)
                {
                    Peak peak = AllPeaks[i];
                    CallData datum = new CallData(
                        peak.peakInMs,
                        peak.frequencyData.startFrequency / 1000.0d,
                        peak.frequencyData.peakFrequency / 1000.0d,
                        peak.frequencyData.endFrequency / 1000.0d,
                        peak.frequencyData.kneeFrequency / 1000.0d,
                        peak.frequencyData.heelFrequency / 1000.0d,
                        peak.lengthInms,
                        i < AllPeaks.Count - 1 ? (double)(AllPeaks[i + 1].startOverall - peak.startOverall) / (double)samplesPerMs : 0.0d
                        );
                    data.Add(datum);
                }
            }
            return (data);
        }

        /// <summary>
        /// Extracts the envelope of the provided data by taking the square of the values and
        /// low pass filtering
        /// </summary>
        private void getEnvelope()
        {
            envelopeData = getEnvelope(Alldata);
        }

        /// <summary>
        /// Overload of getEnvelope() to work on a specified section of raw data, returning the envelope
        /// </summary>
        /// <param name="sectionData"></param>
        /// <returns></returns>
        private List<double> getEnvelope(List<float> sectionData)
        {
            var envelope = sectionData.Select(v => Math.Abs((double)v * (double)v)).ToList<double>();
            envelope = LowPassFilter(envelope, 4000);
            return (envelope);
        }

        /// <summary>
        /// for a single call, establishes and returns the frequency data
        /// </summary>
        /// <param name="peak"></param>
        /// <returns></returns>
        private FrequencyDataSet getFrequencydata(Peak peak, bool isFirstPeak = false, string fname = @"C:\BRMTestData\freq.csv")
        {
            //int FFTSize = 256;
            if (string.IsNullOrWhiteSpace(fname)) fname = @"C:\BRMTestData\freq-sampleSpectra.csv";
            FrequencyDataSet result = new FrequencyDataSet();
            int dataStart = peak.startOverall - FFTSize;
            if (dataStart < 0) dataStart = 0;
            int dataEnd = Math.Min(peak.startOverall + peak.lengthInSamples + FFTSize, Alldata.Count);
            while (((dataEnd - dataStart) % FFTSize) != 0)
            {
                dataEnd++;
                if (dataEnd > Alldata.Count)
                {
                    dataEnd = Alldata.Count;
                    dataStart--;
                    if (dataStart < 0)
                    {
                        dataStart = 0;
                        break;
                    }
                }
            }

            double[] fftTotal = new double[FFTSize / 2];
            List<Spectrum> SampleSpectra = new List<Spectrum>();

            double maxVal = double.MinValue;

            int firstSpectrum = (peak.startOverall / FFTOverlapSamples) - 1; // index of the first spectrum to fall in the call window
            if (firstSpectrum < 0) firstSpectrum = 0;

            int lastSpectrum = (int)((peak.startOverall + peak.lengthInSamples) / FFTOverlapSamples) + 1; // index of the last spectrum to fall in the call window
            if (lastSpectrum >= spectra.Count) lastSpectrum = spectra.Count - 1;

            SampleSpectra = spectra.Skip(firstSpectrum).Take(lastSpectrum - firstSpectrum).ToList(); // extract the set of spectra to examine for this call
            for (int s = 0; s < SampleSpectra.Count(); s++)
            {
                if (SampleSpectra[s].fft.Max() > maxVal) maxVal = SampleSpectra[s].fft.Max();
                for (int i = 0; i < FFTSize / 2; i++)
                {
                    fftTotal[i] += SampleSpectra[s].fft[i];
                }
            }

            List<(double startFrequency, double peakFrequency, double endFrequency, double maxVal)> paramsBySpectrum =
                new List<(double startFrequency, double peakFrequency, double endFrequency, double maxVal)>();

            foreach (var spect in SampleSpectra)
            {
                var parameters = getParamsFromSpectrum(spect.fft);
                paramsBySpectrum.Add(parameters);
            }

            // remove if no overlap with next spectrum
            if (paramsBySpectrum.Count > 2)
            {
                if (paramsBySpectrum[0].endFrequency > paramsBySpectrum[1].startFrequency)
                {
                    paramsBySpectrum.RemoveAt(0);
                }
                else if (paramsBySpectrum[0].startFrequency < paramsBySpectrum[1].endFrequency)
                {
                    paramsBySpectrum.RemoveAt(0);
                }
            }

            // remove last if no overlap with previous spectrum
            if (paramsBySpectrum.Count > 2)
            {
                if (paramsBySpectrum.Last().startFrequency < paramsBySpectrum[paramsBySpectrum.Count - 2].endFrequency)
                {
                    paramsBySpectrum.Remove(paramsBySpectrum.Last());
                }
                else if (paramsBySpectrum[paramsBySpectrum.Count - 1].endFrequency > paramsBySpectrum[paramsBySpectrum.Count - 2].startFrequency)
                {
                    paramsBySpectrum.Remove(paramsBySpectrum.Last());
                }
            }

            result.startFrequency = paramsBySpectrum.Select(tuple => tuple.startFrequency).Max();
            result.endFrequency = paramsBySpectrum.Select(tuple => tuple.endFrequency).Min();
            var spectrumWithPeak = paramsBySpectrum.Where(tuple => tuple.maxVal == paramsBySpectrum.Select(tup => tup.maxVal).Max()).First();
            result.peakFrequency = spectrumWithPeak.peakFrequency;
            Debug.WriteLine($"\npeak {peak.startOverall / (samplesPerMs):F4}ms - {result.startFrequency / 1000.0d:F4}, {result.peakFrequency / 1000.0d:F4}," +
                $"{result.endFrequency / 1000.0d:F4}");
            return (result);
        }

        /// <summary>
        /// Uses information in the AllPeaks list and the original data to establish start, end and peak frequencies
        /// of each call
        /// </summary>
        private void GetFrequencyParams(bool isRelaxed = false)
        {
            string fname = null;

            if (AllPeaks != null && AllPeaks.Count > 0)
            {
                bool isFirstPeak = true;
                foreach (var peak in AllPeaks)
                {
                    FrequencyDataSet fd = getFrequencydata(peak, isFirstPeak, fname);
                    if (fd != null)
                    {
                        peak.frequencyData = fd;
                        isFirstPeak = false;
                    }
                    else
                    {
                        peak.frequencyData = new FrequencyDataSet();
                        //File.AppendAllText(fname, "null data\n");
                    }
                }
            }

            // as long as we have matching arrays, remove outliers on peak and end frequency and duration
            if (!isRelaxed)
            {
                if (AllPeaks.Count() < 2) return;
                double pfMean = AllPeaks.Select(v => v.frequencyData.peakFrequency).Average();
                double durMean = AllPeaks.Select(v => v.lengthInSamples).Average();
                double efMean = AllPeaks.Select(v => v.frequencyData.endFrequency).Average();

                double pfSD = AllPeaks.Select(v => v.frequencyData.endFrequency).StandardDeviation();
                double durSD = AllPeaks.Select(v => v.lengthInSamples).StandardDeviation();
                double efSD = AllPeaks.Select(v => v.frequencyData.endFrequency).StandardDeviation();

                for (int i = AllPeaks.Count() - 1; i >= 0; i--)
                {
                    double sdFactor = 1.0d;
                    if (pfSD < pfMean / 10.0d) // be more tolerant if the peak range is quite small
                    {
                        sdFactor = 2.0d;
                    }
                    if (AllPeaks[i].frequencyData.peakFrequency > pfMean + (pfSD * sdFactor) || AllPeaks[i].frequencyData.peakFrequency < pfMean - (pfSD * sdFactor))
                    {
                        Debug.WriteLine($"Remove peak {i} variance of peak from mean+/-SD {AllPeaks[i].frequencyData.peakFrequency} - {pfMean}");
                        AllPeaks.RemoveAt(i);

                        continue;
                    }

                    if (AllPeaks[i].lengthInSamples > durMean + durSD || AllPeaks[i].lengthInSamples < durMean - durSD)
                    {
                        Debug.WriteLine($"Remove peak {i} variance of length from mean {AllPeaks[i].lengthInSamples} - {durMean}");
                        AllPeaks.RemoveAt(i);

                        continue;
                    }

                    if (efSD < efMean / 10.0d)
                    {
                        sdFactor = 2.0d;
                    }
                    else
                    {
                        sdFactor = 1.0d;
                    }
                    if (AllPeaks[i].frequencyData.endFrequency > efMean + (efSD * sdFactor) || AllPeaks[i].frequencyData.endFrequency < efMean - (efSD * sdFactor))
                    {
                        Debug.WriteLine($"Remove peak at {i} variance of end from mean {AllPeaks[i].frequencyData.endFrequency} - {efMean}");
                        AllPeaks.RemoveAt(i);

                        continue;
                    }
                }
            }
        }

        private void GetIsolatedFrequencyParams(List<float> sectionData, int startSample)
        {
            List<float> extendedData = new List<float>();
            for (int i = 0; i < FFTSize; i++) extendedData.Add(0.0f);
            for (int i = 0; i < sectionData.Count; i++)
            {
                extendedData.Add(sectionData[i]);
            }
            for (int i = 0; i < FFTSize; i++) extendedData.Add(0.0f);

            float scale = 0.9f / (Math.Abs(Math.Max(extendedData.Max(), Math.Abs(extendedData.Min()))));// scale all data to 90% of maximum value
            FFTOverlapSamples = (int)Math.Floor(FFTSize * 0.25);

            var advanceMS = ((double)FFTOverlapSamples / (double)SampleRate) * 1000.0d;

            float[] buffer = new float[FFTSize];
            int offset = 0;
            int FFTOrder = DeepAnalysis.GetFFTOrder(FFTSize);
            spectra.Clear();
            while (extendedData.Count - offset >= FFTSize)
            {
                buffer = extendedData.Skip(offset).Take(FFTSize).ToArray();
                Spectrum spect = new Spectrum(FFTOrder);
                spect.Create(buffer, SampleRate, scale);
                spectra.Add(spect);
                offset += FFTOverlapSamples;
            }
            string file = @"C:\BRMTestData\isoSpectra.csv";
            File.WriteAllText(file, $"For extended sample region start at {startSample} and length {extendedData.Count}\n");
            string fileData = "";
            for (int v = 0; v < spectra[0].fft.Length; v++)
            {
                for (int s = 0; s < spectra.Count; s++)
                {
                    fileData += $"{spectra[s].fft[v]}, ";
                }
                fileData += "\n";
            }
            File.AppendAllText(file, fileData);

            if (AllPeaks != null && AllPeaks.Any())
            {
                Peak peak = AllPeaks[0];
                peak.startOverall = peak.startSample + (FFTSize / 2); // to prevent overflow in getFrequencyData which assumes spectra are for all data from 0
                peak.lengthInSamples = peak.lengthInSamples + FFTSize;
                FrequencyDataSet fd = getFrequencydata(peak, true, null);
                if (fd != null)
                {
                    AllPeaks[0].frequencyData = fd;
                }
                else
                {
                    AllPeaks[0].frequencyData = new FrequencyDataSet();
                }
            }
        }

        private void GetIsolatedPeak(List<double> sectionEnvelope, int startSample)
        {
            Peak peak = getPeak(sectionEnvelope, startSample);
            AllPeaks.Clear();
            AllPeaks.Add(peak);
        }

        private (double startFrequency, double peakFrequency, double endFrequency, double maxVal) getParamsFromSpectrum(double[] fft)
        {
            var result = (startFrequency: 0.0d, peakFrequency: 0.0d, endFrequency: 0.0d, maxVal: 0.0d);
            var max = fft.Max();
            var maxloc = fft.ToList().IndexOf(max);
            int lowloc = 0; ;
            int hiloc = 0; ;
            int v1;
            int v2;
            double slope;

            for (int i = maxloc; i > 0; i--)
            {
                if (fft[i] < max / 2)
                {
                    v1 = i;
                    v2 = i + 1;
                    slope = Math.Abs(fft[v2] - fft[v1]);
                    lowloc = v2 - (int)Math.Ceiling((fft[v2] / slope));
                    break;
                }
            }

            double threshold = fft.Skip(fft.Count() - 6).Take(5).Average() * 2.0d; ;

            for (int v = maxloc > 3 ? maxloc : 3; v < fft.Count(); v++)
            {
                double smooth = fft[v - 2] + fft[v - 1] + fft[v];
                if (smooth / 3 <= threshold)
                {
                    hiloc = v - 1;
                    break;
                }
            }

            if (lowloc > 0 && hiloc > 0)
            {
                int HzPerBin = SampleRate / FFTSize;
                result.endFrequency = lowloc * HzPerBin;
                result.peakFrequency = maxloc * HzPerBin;
                result.startFrequency = hiloc * HzPerBin;
                result.maxVal = max;

                Debug.WriteLine($"s={result.startFrequency}, p={result.peakFrequency}, e={result.endFrequency}");
            }
            return (result);
        }

        /// <summary>
        /// Locates and measures the biggest peak in the data provided
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Peak getPeak(List<double> data, int startOverall)
        {
            if ((data?.Count ?? 0) < SampleRate / 1000.0) return (null); // not enough data for a single pulse
            Peak result = null;
            try
            {
                double maxVal = data.Max();
                int maxLoc = data.IndexOf(maxVal);
                int loc = maxLoc;
                for (loc = maxLoc; loc >= 0 && data[loc] > maxVal / 2.0; loc--) { }

                var slopeData = data.Skip(loc - ((maxLoc - loc) / 2)).Take(maxLoc - loc).ToArray();
                double slopeTotal = 0.0d;
                for (int i = 1; i < slopeData.Count(); i++) { slopeTotal += slopeData[i] - slopeData[i - 1]; }// should all be positive values
                double slope = slopeTotal / (slopeData.Count() - 1);
                int startLoc = maxLoc - (int)Math.Ceiling(maxVal / slope);

                if (startLoc < 0) return (null);

                loc = maxLoc;
                for (loc = maxLoc; loc < data.Count() && data[loc] > maxVal / 2.0; loc++) { }
                slopeData = data.Skip(loc - ((loc - maxLoc) / 2)).Take(loc - maxLoc).ToArray();
                slopeTotal = 0.0d;
                for (int i = 1; i < slopeData.Count(); i++) { slopeTotal += slopeData[i - 1] - slopeData[i]; }// should all be positive
                slope = slopeTotal / (slopeData.Count() - 1);
                int endLoc = maxLoc + (int)Math.Ceiling(maxVal / slope);

                if (endLoc < 0 || endLoc > data.Count()) return (null);
                result = new Peak(startLoc, endLoc, startLoc + startOverall, SampleRate, maxVal, maxLoc + startOverall); // start and end samples relative to
                                                                                                                         // the current data block, and irrelevant thereafter, but needed by the recursive getpeaks to work out where to look next
            }
            catch (StackOverflowException ex)
            {
                throw (ex);
            }

            return (result);
        }

        /// <summary>
        /// Recursive function to extract all peaks in the given data sample
        /// </summary>
        /// <param name="envelopeData"></param>
        private void getPeaks(List<double> Data, int startOverall)
        {
            Debug.Write($"\nGet Peaks in region {startOverall / samplesPerMs}ms -> {(startOverall + Data.Count()) / samplesPerMs}ms");
            if (Data.Count / samplesPerMs < 10.0d)
            {
                Debug.Write("\t too little data - return\n");
                return;
            }

            try
            {
                Peak peak = getPeak(Data, startOverall);
                if (Peak.isValidPeak(peak))
                {
                    AllPeaks.Add(peak);
                    Debug.Write($"\t(P at {peak.peakInMs}ms)");
                }
                else
                {
                    Debug.Write("\t (No Peaks in section)");
                    return;
                }

                int leftMargin = peak.startSample - (10 * samplesPerMs); // left margin 10ms left of the peak start
                int rightMargin = (peak.startSample + peak.lengthInSamples) + (10 * samplesPerMs); // right margin 10ms right of end of peak

                if (leftMargin > 0) getPeaks(Data.Take(leftMargin).ToList(), startOverall);
                if (rightMargin < Data.Count) getPeaks(Data.Skip(rightMargin).ToList(), startOverall + rightMargin);
            }
            catch (StackOverflowException)
            {
                Debug.WriteLine("Stack Overflow exception");
                return;
            }
            depth--;
        }

        /// <summary>
        /// Breadth first analysis of the waveform to find peaks from largest to smallest
        /// </summary>
        /// <param name="envelopeData"></param>
        /// <param name="v"></param>
        private void getPeaks2(List<double> envelopeData, int v)
        {
            Peak peak = getPeak(envelopeData, 0); // get hte first and biggest peak
            if (Peak.isValidPeak(peak))
            {
                AllPeaks.Add(peak);
                recursiveGetPeaks(envelopeData, peak);
            }
        }

        /// <summary>
        /// Low pass filters a list of doubles at the specified cut-off frequency
        /// </summary>
        /// <param name="envelopeData"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private List<double> LowPassFilter(List<double> envelopeData, int freq)
        {
            var filter = BiQuadFilter.LowPassFilter(SampleRate, freq, 1);

            var result = envelopeData.Select(v => (double)filter.Transform((float)v)).ToList();

            return (result);
        }

        private void recursiveGetPeaks(List<double> envelopeData, Peak peak)
        {
            double maxLeft = double.MinValue;
            double maxRight = double.MinValue;

            if (envelopeData.Count < (samplesPerMs * 20)) return;
            int leftMargin = peak.startSample - (10 * samplesPerMs);
            if (leftMargin < 0) leftMargin = 0;
            int rightMargin = peak.startSample + peak.lengthInSamples + (10 * samplesPerMs);
            if (rightMargin >= envelopeData.Count) rightMargin = envelopeData.Count - 1;
            var leftData = envelopeData.Take(leftMargin).ToList();
            if (leftData.Count > samplesPerMs) maxLeft = leftData.Max();
            var rightData = envelopeData.Skip(rightMargin).ToList();
            if (rightData.Count > samplesPerMs) maxRight = rightData.Max();

            Peak peakL = null;
            Peak peakR = null;
            if (maxLeft > maxRight)
            {
                peakL = getPeak(leftData, peak.startOverall);
                peakR = getPeak(rightData, peak.startOverall + peak.lengthInSamples);
                if (Peak.isValidPeak(peakL))
                {
                    AllPeaks.Add(peakL);
                    recursiveGetPeaks(leftData, peakL);
                }
                if (Peak.isValidPeak(peakR))
                {
                    AllPeaks.Add(peakR);
                    recursiveGetPeaks(rightData, peakR);
                }
            }
            else
            {
                peakR = getPeak(rightData, peak.startOverall + peak.lengthInSamples);
                peakL = getPeak(leftData, peak.startOverall);
                if (Peak.isValidPeak(peakR))
                {
                    AllPeaks.Add(peakR);
                    recursiveGetPeaks(rightData, peakR);
                }
                if (Peak.isValidPeak(peakL))
                {
                    AllPeaks.Add(peakL);
                    recursiveGetPeaks(leftData, peakL);
                }
            }
        }

        private List<double> reduceIntervalList(List<double> intervalList)
        {
            foreach (var d in intervalList)
            {
                Debug.Write($"{d:#0.###}\t");
            }
            Debug.WriteLine("\nReduces to:-");
            if (intervalList == null || intervalList.Count < 2) return (intervalList);

            List<double> result = new List<double>();

            double mean = intervalList.Average();
            double sd = intervalList.StandardDeviation();
            while (sd > mean) sd /= 2.0d;

            foreach (var interval in intervalList)
            {
                if (interval < mean - sd) continue; // skip any very short intervals
                if (interval <= mean) // just add any interval between mean and mean-sd
                {
                    result.Add(interval);
                    continue;
                }
                double gap = 0.0d;
                double gapdiv = 2.0d;
                double factor = 2.0d;
                double bestguess = interval;
                while (true)
                {
                    gap = Math.Abs(bestguess - mean);
                    gapdiv = Math.Abs((interval / factor) - mean);
                    if (gapdiv > gap)
                    {
                        result.Add(bestguess);
                        break;
                    }
                    else
                    {
                        if (bestguess < mean)
                        {
                            result.Add(bestguess);
                            break;
                        }
                        else
                        {
                            bestguess = interval / factor;
                            factor += 1.0d;
                        }
                    }
                }
            }
            foreach (var d in result)
            {
                Debug.Write($"{d:#0.###}\t");
            }
            Debug.WriteLine("");
            return (result);
        }

        /// <summary>
        /// given a Point within the spectrogram, locates the nearest peak in unfilteredPeaks and places it as
        /// the only item in AllPeaks for further processing
        /// </summary>
        /// <param name="pe"></param>
        private void selectNearestPeak(Point pe)
        {
            double pointMs = (pe.X * FFTOverlapSamples * 1000.0d) / SampleRate;
            var orderedPeaks = from pk in unfilteredPeaks
                               where Peak.isValidPeak(pk)
                               orderby Math.Pow(pk.peakInMs - pointMs, 2.0d)
                               select pk;
            var nearestPeak = orderedPeaks.First();
            AllPeaks.Clear();
            AllPeaks.Add(nearestPeak);
            Debug.WriteLine($"Nearest Peak is at {nearestPeak.peakInMs}ms");
        }
    }
}