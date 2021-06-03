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

using BatCallAnalysisControlSet;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Windows;
using System.Windows.Media.Imaging;
using UniversalToolkit;
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;
using Pen = System.Drawing.Pen;

namespace BatRecordingManager
{
    public class Spectrum
    {
        public double[] fft = null;
        public double fftMean = 0.0d;
        public int HzPerBin;
        public int PeakBin;
        public float PeakFrequency;
        public double PeakValue;

        public Spectrum(int FFTOrder)
        {
            if (FFTOrder < 14)
            {
                this.FFTOrder = FFTOrder;
            }
            FFTSize = (int)Math.Pow(2, FFTOrder);
            fft = new double[FFTSize / 2];
            rawFFT = new Complex[FFTSize];
            for (int i = 0; i < FFTSize / 2; i++) fft[i] = 0.0d;
        }

        public void Create(float[] data, int sampleRate, float scale = 1.0f)
        {
            this.sampleRate = sampleRate;
            HzPerBin = sampleRate / FFTSize;
            PeakBin = -1;
            PeakFrequency = 0.0f;
            PeakValue = 0.0d;
            List<float> paddedData = new List<float>();
            for (int i = 0; i < FFTSize; i++)
            {
                if (i < data.Count())
                {
                    paddedData.Add(data[i]);
                }
                else
                {
                    paddedData.Add(0.0f);
                }
            }
            data = paddedData.ToArray();

            for (int i = 0; i < FFTSize; i++)
            {
                rawFFT[i].X = (float)(data[i] * scale * FastFourierTransform.HammingWindow(i, FFTSize));
                rawFFT[i].Y = 0.0f;
            }
            FastFourierTransform.FFT(true, FFTOrder, rawFFT);
            double spectSum = 0.0d;
            for (int i = 0; i < FFTSize / 2; i++)
            {
                fft[i] = Math.Sqrt((rawFFT[i].X * rawFFT[i].X) + (rawFFT[i].Y * rawFFT[i].Y));
                spectSum += fft[i];
                if (fft[i] > PeakValue)
                {
                    PeakValue = fft[i];
                    PeakBin = i;
                    PeakFrequency = i * HzPerBin;
                }
            }
            GetZeroCrossings();
            spectSum /= FFTSize / 2;
            fftMean = spectSum;
        }

        internal static List<Spectrum> Normalize(List<Spectrum> spectra)
        {
            List<Spectrum> result = new List<Spectrum>();
            if (spectra != null && spectra.Count > 0)
            {
                Spectrum sum = new Spectrum(spectra[0].FFTOrder);
                foreach (var spect in spectra)
                {
                    for (int i = 0; i < spect.fft.Count(); i++)
                    {
                        sum.fft[i] += spect.fft[i];
                    }
                }
                for (int i = 0; i < sum.fft.Count(); i++)
                {
                    sum.fft[i] = sum.fft[i] / spectra.Count;
                }

                for (int i = 0; i < spectra.Count; i++)
                {
                    for (int j = 0; j < spectra[i].fft.Count(); j++)
                    {
                        spectra[i].fft[j] = spectra[i].fft[j] - sum.fft[j];
                        if (spectra[i].fft[j] < 0) spectra[i].fft[j] = 0;
                    }
                    result.Add(spectra[i]);
                }
            }
            return (result);
        }

        private readonly int FFTOrder = 9;
        private readonly int FFTSize;
        private Complex[] rawFFT = null;
        private int sampleRate = 0;

        /// <summary>
        /// Extracts the portion of the FFT around the local maximum and zeroes all other values
        /// in the rawFFT.  Then does on inverse FFT on the remainder and identifies zero crossings in that
        /// sample, taking frequency of the mean zero crossing period.
        /// </summary>
        private void GetZeroCrossings()
        {
        }
    }

    /// <summary>
    /// Class to perform a deep and detailed analysis of a section of a recording.
    ///
    /// </summary>
    internal class DeepAnalysis
    {
        /// <summary>
        /// The number of data samples each FFT advances beyond the last one so that FFTs overlap by FFTSiz-FFTAdvance
        /// </summary>
        public float FFTAdvanceFactor = 0.5f;

        public int FFTOrder = 9;
        public int FFTOverlapSamples;
        public int FFTSize;

        public int HzPerBin;

        public int sampleRate = 384000;

        /// <summary>
        /// A list of spectra of order FFTOrder at about 95% overlap
        /// </summary>
        public List<Spectrum> spectra = new List<Spectrum>();

        public event EventHandler<EventArgs> SaveClicked;

        public Parametrization param { get; set; } = null;

        public static Bitmap decorateBitmap(Bitmap bmp, int FFTSize, int FFTAdvance, int SampleRate, Parametrization param = null)
        {
            int HzPerBin = (int)((double)SampleRate / (double)FFTSize);

            Bitmap tempBmp = new Bitmap(bmp.Width, bmp.Height * 2);
            int size = tempBmp.Height - 1;
            using (var g = Graphics.FromImage(tempBmp))
            {
                g.DrawImage(bmp, new Rectangle(0, 0, tempBmp.Width, tempBmp.Height));
                //g.DrawImage(bmp, 0, 0);
                double binsPer10kHz = (10000.0d / HzPerBin) * 2.0d;
                Pen blackPen = new Pen(Color.FromArgb(0x8f, Color.DarkBlue));

                Pen redPen = new Pen(Color.Red);
                int f = 0;
                for (int y = size; y >= 0; y -= (int)Math.Ceiling((binsPer10kHz)))//f=frequency in kHz
                {
                    g.DrawLine(blackPen, 0.0f, y, bmp.Width - 1, y);
                    if (f > 0)
                    {
                        g.DrawString(f.ToString() + "kHz", new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Regular), new SolidBrush(Color.Black), 10.0f, y);
                    }
                    f += 10;
                }

                double spDurationMs = (1000.0d * FFTAdvance) / SampleRate;
                int spPer100ms = (int)Math.Floor(100 / spDurationMs);

                float xPos = 0.0f;
                float tim = 0.0f;
                while (xPos < bmp.Width)
                {
                    g.DrawLine(blackPen, xPos, 0.0f, xPos, (float)size - 1);
                    if (tim > 0.0f)
                    {
                        g.DrawString($"{tim:F1}",
                            new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Regular),
                            new SolidBrush(Color.Black),
                            xPos - 20,
                            5);
                    }
                    xPos += spPer100ms;
                    tim += 0.1f;
                }

                if (param != null)
                {
                    if (param.AllPeaks != null && param.AllPeaks.Count > 0)
                    {
                        for (int p = 0; p < param.AllPeaks.Count; p++)
                        {
                            var freqs = param.AllPeaks[p].frequencyData;
                            var peak = param.AllPeaks[p];
                            int xmin = (int)(peak.startOverall / (FFTAdvance));
                            int xmax = (int)((peak.startOverall + peak.lengthInSamples) / (FFTAdvance));
                            int ymin = size - (2 * (int)(freqs.endFrequency / HzPerBin));
                            int ymax = size - (2 * (int)(freqs.startFrequency / HzPerBin));

                            g.DrawLine(redPen, xmin, ymin, xmax, ymin);
                            g.DrawLine(redPen, xmin, ymin, xmin, ymin + 5);
                            g.DrawLine(redPen, xmax, ymin, xmax, ymin + 5);

                            g.DrawLine(redPen, xmin, ymax, xmax, ymax);
                            g.DrawLine(redPen, xmin, ymax, xmin, ymax - 5);
                            g.DrawLine(redPen, xmax, ymax, xmax, ymax - 5);
                        }
                    }
                }
            }
            return (tempBmp);
        }

        public static Bitmap GetBmpFromSpectra(List<Spectrum> spectra, int FFTOrder, float FFTAdvanceFactor, int sampleRate, Parametrization param = null)
        {
            int FFTSize = (int)Math.Pow(2, FFTOrder);
            int FFTOverlap = (int)(FFTAdvanceFactor * FFTSize);
            int HzPerBin = (int)((double)sampleRate / (double)FFTSize);
            if (spectra.Count <= 0) return (null);
            int fftSize = spectra[0].fft.Length;
            int size = spectra[0].fft.Length * 2;
            Bitmap bmp = new Bitmap(spectra.Count, size);

            double MaximumValue = Double.MinValue;
            foreach (var sp in spectra)
            {
                if (sp.PeakValue > MaximumValue) MaximumValue = sp.PeakValue;
            }

            /*
            logMinimumValue = Math.Abs(20 * Math.Log10(Math.Abs(MinimumValue)));
            if (double.IsNaN(logMinimumValue)) logMinimumValue = 0.0d;
            if (double.IsInfinity(logMinimumValue)) logMinimumValue = 0.0d;
            logMaximumValue = Math.Abs(20 * Math.Log10(Math.Abs(MaximumValue)));
            if (double.IsNaN(logMaximumValue)) logMaximumValue = 1.0d;*/

            Debug.WriteLine($"bitmap of {spectra.Count}x{size}");
            int col = 0;

            foreach (var sp in spectra)
            {
                for (int bin = 0; bin < fftSize; bin++)
                {
                    var scaled = DeepAnalysis.Scale(sp.fft[bin], MaximumValue);

                    bmp.SetPixel(col, size - (bin * 2) - 1, scaled);
                    bmp.SetPixel(col, size - (bin * 2) - 2, scaled);
                }
                col++;
            }

            bmp = decorateBitmap(bmp, FFTSize, FFTOverlap, sampleRate, param);

            return (bmp);
        }

        public static List<Spectrum> GetSpectrum(List<float> data, int sampleRate, int FFTOrder, float FFTAdvanceFactor, out int FFTOverlapSamples, out double advanceMS, out double[] FFTQuiet)
        {
            List<Spectrum> spectra = new List<Spectrum>();
            int FFTSize = (int)Math.Pow(2, FFTOrder);
            float scale = 0.9f / (Math.Abs(Math.Max(data.Max(), Math.Abs(data.Min()))));// scale all data to 90% of maximum value
            FFTOverlapSamples = (int)Math.Floor(FFTSize * FFTAdvanceFactor);

            advanceMS = ((double)FFTOverlapSamples / (double)sampleRate) * 1000.0d;

            FFTQuiet = Enumerable.Repeat(0.0d, (FFTSize / 2) + 1).ToArray<double>();

            float[] buffer = new float[FFTSize];
            int offset = 0;

            while (data.Count - offset >= FFTSize)
            {
                buffer = data.Skip(offset).Take(FFTSize).ToArray();
                Spectrum spect = new Spectrum(FFTOrder);
                spect.Create(buffer, sampleRate, scale);
                spectra.Add(spect);
                offset += FFTOverlapSamples;
            }
            var sortedSpectra = (from sp in spectra
                                 orderby sp.fftMean
                                 select sp).ToList();
            int quietCount = (int)Math.Floor(sortedSpectra.Count() / 10.0d);
            if (quietCount > 0)
            {
                //FFTQuiet = Enumerable.Range(0, FFTSize / 2).AsParallel()
                //    .Select(i => sortedSpectra.Take(quietCount).Select(a => a.fft.Skip(i).First()).Average()).ToArray<double>();

                for (int f = 0; f < spectra.First().fft.Count(); f++)
                {
                    for (int q = 0; q < quietCount; q++)
                    {
                        FFTQuiet[f] += sortedSpectra[q].fft[f];
                    }
                    FFTQuiet[f] = FFTQuiet[f] / quietCount;
                }
            }
            foreach (var sp in spectra)
            {
                for (int i = (FFTSize / 2) - 1; i >= 0; i--)
                {
                    sp.fft[i] = sp.fft[i] - FFTQuiet[i];
                    if (sp.fft[i] < 0.0d) sp.fft[i] = 0.0d;
                }
            }
            return (spectra);
        }

        /// <summary>
        /// Scales a value return a color that can be displayed in the bitmap
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Color Scale(double val, double MaximumValue)
        {
            double dbRange = 48.0d;

            int value;
            if (double.IsNaN(val) || double.IsInfinity(val))
            {
                val = 0.0d;
            }
            if (double.IsNaN(MaximumValue) || double.IsInfinity(MaximumValue))
            {
                MaximumValue = 1.0d;
            }

            var ratio = val / MaximumValue;

            if (ratio > 1.0d) ratio = 1.0d;

            int grey;
            if (ratio <= 0.0d)
            {
                grey = 255;
            }
            else
            {
                var db = 20.0d * Math.Log10(ratio); //ranges from 0 to minus lots

                if (db < -dbRange) db = -dbRange;

                db = db * (255.0d / dbRange);  // gives number in range 0 - -255

                db = Math.Abs(db); // make it positive 0-255

                grey = (int)Math.Floor(db);
            }

            value = grey;

            return (Color.FromArgb(value, value, value));
        }

        internal static int GetFFTOrder(int fFTSize)
        {
            int FFTOrder = 9;
            switch (fFTSize)
            {
                case 1024:
                    FFTOrder = 10;
                    break;

                case 512: FFTOrder = 9; break;
                case 256: FFTOrder = 8; break;
                case 128: FFTOrder = 7; break;
                case 64: FFTOrder = 6; break;
                case 2048: FFTOrder = 11; break;
                default: FFTOrder = 9; break;
            }
            return (FFTOrder);
        }

        /// <summary>
        /// Given a labelled segment, extracts the relevant portion of the .wav file and then does a detaield
        /// analysis of the pulse train therein.  The results are written to a .txt file for the time being.
        /// </summary>
        /// <param name="sel"></param>
        /// <param name="AnalysisMode">If zero, analyses 5s centred on the loudest pulse,
        /// if 1 analyses a single pulse,
        /// if 5 analyses the 5 loudest pulses</param>
        internal bool AnalyseSegment(LabelledSegment sel, int AnalysisMode, bool byZeroCrossing = false)
        {
            if (sel == null) return false;
            selectedSegment = sel;
            using (new WaitCursor())
            {
                string file = sel.Recording.GetFileName();
                //string file = Path.Combine(sel.Recording.RecordingSession.OriginalFilePath,sel.Recording.RecordingName);
                if (!File.Exists(file)) return false;
                FFTSize = (int)Math.Pow(2, FFTOrder);

                using (var wfr = new WaveFileReader(file))
                {
                    var sp = wfr.ToSampleProvider();
                    sampleRate = wfr.WaveFormat.SampleRate;
                    HzPerBin = sampleRate / FFTSize;
                    var requestedDuration = sel.EndOffset - sel.StartOffset;
                    if (requestedDuration.TotalSeconds > 5)
                    {
                        sel.EndOffset = sel.StartOffset + TimeSpan.FromSeconds(5);
                    }
                    var data = sp.Skip(sel.StartOffset).Take(sel.EndOffset - sel.StartOffset);
                    float[] faData = new float[FFTSize];
                    alldata = new List<float>();
                    int samplesRead;
                    while ((samplesRead = data.Read(faData, 0, FFTSize)) > 0)
                    {
                        alldata.AddRange(faData.Take(samplesRead));
                    }

                    var filter = BiQuadFilter.HighPassFilter(sampleRate, 15000, 1);
                    for (int i = 0; i < alldata.Count; i++)
                    {
                        alldata[i] = filter.Transform(alldata[i]);
                    }

                    spectra?.Clear();
                    AnalyseData(alldata, byZeroCrossing);// generates spectrograms of the data

                    ParameterizeData(alldata, AnalysisMode);
                }
            }
            return true;
        }

        /// <summary>
        /// Converts the calculated spectra into an image to be displayed in the ComparisonHost window, with suitable
        /// annotations.
        /// </summary>
        /// <returns></returns>
        internal BitmapSource GetImage()
        {
            return (Tools.ToBitmapSource(GetBmpFromSpectra(spectra, FFTOrder, FFTAdvanceFactor, sampleRate, param)));
        }

        internal void reAnalyseSegment(PointEeventArgs pe)
        {
            if (pe.point.X == pe.endPoint.X && pe.point.Y == pe.endPoint.Y)
            {
                if (param != null)
                {
                    param.AnalysePulse(pe.point);
                }
            }
            else
            {
                AnalyseRegion(pe.point, pe.endPoint);
            }
        }

        protected virtual void OnSaveClicked(EventArgs e) => SaveClicked?.Invoke(this, e);

        private readonly double logMaximumValue;
        private readonly double logMinimumValue;
        private double advanceMS = 0;
        private List<float> alldata = new List<float>();
        private double[] FFTQuiet;
        private List<float> gradient = new List<float>();
        private double MaximumValue;
        private double MinimumValue;
        private List<(int frequency, double value, int bin)> peakFrequency = new List<(int frequency, double value, int bin)>();
        private LabelledSegment selectedSegment = null;

        private static Color Scale(double[] recipFft, int bin, double max, double MaximumValue)
        {
            double val = recipFft[bin];

            val = val / max;

            double dbRange = 48.0d;

            int value;
            if (double.IsNaN(val) || double.IsInfinity(val))
            {
                val = 0.0d;
            }
            if (double.IsNaN(MaximumValue) || double.IsInfinity(MaximumValue))
            {
                MaximumValue = 1.0d;
            }

            var ratio = val / MaximumValue;

            if (ratio > 1.0d) ratio = 1.0d;

            int grey;
            if (ratio <= 0.0d)
            {
                grey = 255;
            }
            else
            {
                var db = 20.0d * Math.Log10(ratio); //ranges from 0 to minus lots

                if (db < -dbRange) db = -dbRange;

                db = db * (255.0d / dbRange);  // gives number in range 0 - -255

                db = Math.Abs(db); // make it positive 0-255

                grey = (int)Math.Floor(db);
            }

            value = grey;

            return (Color.FromArgb(value, value, value));
        }

        /// <summary>
        /// Given a data stream in the form of a SampleProvider, performs the deep analysis
        /// and generates the report.
        /// </summary>
        /// <param name="data"></param>
        private void AnalyseData(List<float> data, bool byZeroCrossing = false)
        {
            if (byZeroCrossing)
            {
                zcAnalyse(data);
                return;
            }
            //List<Spectrum> spectra = new List<Spectrum>();
            //int FFTOrder = 10;

            MinimumValue = double.MaxValue;
            MaximumValue = double.MinValue;
            double spectrumMax = double.MinValue;
            int spectrumPeakFrequency;
            int spectrumPeakBin;

            peakFrequency = new List<(int frequency, double value, int bin)>();
            gradient = new List<float>();

            var tpf = new List<(int frequency, double value, int bin)>();
            var tgrad = new List<int>();

            spectra = GetSpectrum(data, sampleRate, FFTOrder, FFTAdvanceFactor,
                out FFTOverlapSamples, out advanceMS, out FFTQuiet);
            foreach (var sp in spectra)
            {
                spectrumMax = 0.0d;
                spectrumPeakFrequency = 0;
                spectrumPeakBin = -1;
                for (int i = (FFTSize / 2) - 1; i >= 0; i--)
                {
                    //sp.fft[i] = sp.fft[i] - FFTQuiet[i];
                    //if (sp.fft[i] < 0.0d) sp.fft[i] = 0.0d;
                    if (i > 15000 / HzPerBin)
                    {
                        if (sp.fft[i] > spectrumMax)
                        {
                            spectrumMax = sp.fft[i];
                            spectrumPeakFrequency = i * HzPerBin;
                            spectrumPeakBin = i;
                        }
                    }
                    if (sp.fft[i] < MinimumValue) MinimumValue = sp.fft[i];
                }
                if (spectrumMax > MaximumValue) MaximumValue = spectrumMax;
                int lastf = spectrumPeakFrequency;
                if (tpf.Any())
                {
                    lastf = tpf.Last().frequency;
                }
                tpf.Add((spectrumPeakFrequency, spectrumMax, spectrumPeakBin));
                tgrad.Add(spectrumPeakFrequency - lastf);
            }
            for (int i = 0; i < tpf.Count; i++)
            {
                if (tpf[i].value < MaximumValue / 10.0d)
                {
                    peakFrequency.Add((0, tpf[i].value, i));
                }
                else
                {
                    peakFrequency.Add(tpf[i]);
                }

                if (i > 4 && i < tgrad.Count - 4)
                {
                    gradient.Add((float)tgrad.Skip(i - 3).Take(7).Average());
                }
                else if (i < 4)
                {
                    gradient.Add((float)tgrad.Take(7).Average());
                }
                else if (i > tpf.Count - 4)
                {
                    gradient.Add((float)tgrad.Skip(tgrad.Count - 8).Take(7).Average());
                }
                else
                {
                    gradient.Add(0.0f);
                }
            }
            bool inPulse = false;
            for (int i = 0; i < peakFrequency.Count; i++)
            {
                if (!inPulse)
                {
                    if (peakFrequency[i].frequency > 0)
                    {
                        inPulse = true;
                        int j = 1;
                        while (i - j > 0 && tpf[i - j].value > MaximumValue / 30.0d)
                        {
                            peakFrequency[i - j] = tpf[i - j];
                            j++;
                        }
                    }
                }
                else
                {
                    if (peakFrequency[i].frequency == 0)
                    {
                        inPulse = false;
                    }
                }
            }
        }

        /// <summary>
        /// Like Analyse segment, but the region of waveform to be processes in a relased manner is defined by a pair of points
        /// selectedd from the sonagram
        /// </summary>
        /// <param name="point"></param>
        /// <param name="endPoint"></param>
        private void AnalyseRegion(System.Windows.Point startPoint, System.Windows.Point endPoint)
        {
            var startX = Math.Min(startPoint.X, endPoint.X);
            var endX = Math.Max(startPoint.X, endPoint.X);
            var startY = Math.Min(startPoint.Y, endPoint.Y);
            var endY = Math.Max(startPoint.Y, endPoint.Y);

            var startSample = startX * FFTAdvanceFactor;
            var endSample = endX * FFTAdvanceFactor;

            int spectHeight = FFTSize;

            var highFreqHz = (spectHeight - startY) * HzPerBin / 2.0d;
            var lowFreqHz = (spectHeight - endY) * HzPerBin / 2.0d;

            if (alldata != null && alldata.Count > endX)
            {
                List<float> sectionData = new List<float>();
                for (int i = (int)startSample; i < endSample; i++)
                {
                    sectionData.Add(alldata[i]);
                }
                var HPfilter = BiQuadFilter.HighPassFilter(sampleRate, (float)lowFreqHz, 1);
                var LPFilter = BiQuadFilter.LowPassFilter(sampleRate, (float)highFreqHz, 1);

                for (int i = (int)startX; i < endX; i++)
                {
                    var val = (HPfilter.Transform(alldata[i]));
                    val = LPFilter.Transform(val);
                    sectionData.Add(val);
                }

                param?.AnalysePulse(sectionData, (int)startSample);
            }
        }

        /// <summary>
        /// Given an array of values at frequencies determined by HzPerPeriod, and a
        /// period, interpolate in the frequency scale to determine the value for the
        /// specified period
        /// </summary>
        /// <param name="fft"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private double interpolate(double[] fft, double period)
        {
            double desiredFrequency = 1.0d / period;

            int binBelow = (int)Math.Floor(desiredFrequency / (double)HzPerBin);
            int binAbove = (int)Math.Ceiling(desiredFrequency / (double)HzPerBin);
            double freqBelow = (double)(HzPerBin * binBelow);
            double freqAbove = (double)(HzPerBin * binAbove);
            double freqGapAbove = freqAbove - desiredFrequency;
            double freqGapBelow = desiredFrequency - freqBelow;
            double proportion = freqGapBelow / (freqAbove - freqBelow);
            if (binAbove >= fft.Length || binBelow >= fft.Length) return (fft[fft.Length - 1]);
            if (binBelow < 0 || binAbove < 0) return (fft[0]);
            double valBelow = fft[binBelow];
            double valAbove = fft[binAbove];
            double newVal = valBelow + (proportion * (valAbove - valBelow));

            return (newVal);
        }

        /// <summary>
        /// responds to a click on the save button in the chartform of the chartgrid via several
        /// event handlers.  Identifies the segment on which the analysis is operating and saves the
        /// returned calldata to the database, also appending the parameters as an addednum to the
        /// segment comment if there is not already one, or appends {} if there is one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Param_saveClicked(object sender, EventArgs e)
        {
            ReferenceCall call = (e as callEventArgs).call;
            DBAccess.AppendCallDetailsToSegment(call, selectedSegment);
            OnSaveClicked(new EventArgs());
        }

        private void ParameterizeData(List<float> alldata, int AnalysisMode)
        {
            param = new Parametrization(alldata, sampleRate, AnalysisMode);
            param.saveClicked += Param_saveClicked;
            param.CalculateParameters(spectra, FFTSize, FFTOverlapSamples);
        }

        /// <summary>
        /// fft contains data spaced at HzPerBin frequency spacing for FFTSize bins;
        /// This function changes the scaling by using the reciprocal of the frequencies
        /// equally spaced and the interpolating from the existing data to find values for the new
        /// spacings.  ffTdata is all in the range 0-1.
        /// </summary>
        /// <param name="fft"></param>
        /// <returns></returns>
        private double[] reciprocal(double[] fft)
        {
            List<double> result = new List<double>();
            int maxfreq = fft.Length * HzPerBin;
            double minPeriod = 1.0d / maxfreq;
            double maxPeriod = 1.0d / HzPerBin;
            double periodPerBin = (double)(maxPeriod - minPeriod) / (double)fft.Length;

            for (int i = 1; i <= fft.Length; i++)
            {
                result.Add(interpolate(fft, i * periodPerBin));
            }

            return (result.ToArray());
        }

        /// <summary>
        /// Converts the .wav filtered data into ZC data and extracts a parameter table from that data
        /// </summary>
        /// <param name="data"></param>
        private void zcAnalyse(List<float> data)
        {
            throw new NotImplementedException();
        }
    }
}