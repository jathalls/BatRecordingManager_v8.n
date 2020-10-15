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

using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
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

            for (int i = 0; i < FFTSize; i++)
            {
                rawFFT[i].X = (float)(data[i] * scale * FastFourierTransform.HammingWindow(i, FFTSize));
                rawFFT[i].Y = 0.0f;
            }
            FastFourierTransform.FFT(true, 10, rawFFT);
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

        private readonly int FFTOrder = 10;
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
        public int FFTAdvance = 50;

        public int FFTOrder = 10;

        public int FFTSize;

        public int HzPerBin;

        public int sampleRate = 384000;

        /// <summary>
        /// A list of spectra of order FFTOrder at about 95% overlap
        /// </summary>
        public List<Spectrum> spectra = new List<Spectrum>();

        /// <summary>
        /// Given a labelled segment, extracts the relevant portion of the .wav file and then does a detaield
        /// analysis of the pulse train therein.  The results are written to a .txt file for the time being.
        /// </summary>
        /// <param name="sel"></param>
        internal bool AnalyseSegment(LabelledSegment sel)
        {
            if (sel == null) return false;
            string file = sel.Recording.GetFileName();
            //string file = Path.Combine(sel.Recording.RecordingSession.OriginalFilePath,sel.Recording.RecordingName);
            if (!File.Exists(file)) return false;
            FFTSize = (int)Math.Pow(2, FFTOrder);

            using (var wfr = new WaveFileReader(file))
            {
                var sp = wfr.ToSampleProvider();
                sampleRate = wfr.WaveFormat.SampleRate;
                HzPerBin = sampleRate / FFTSize;
                var data = sp.Skip(sel.StartOffset).Take((sel.Duration() ?? new TimeSpan()));
                float[] faData = new float[FFTSize];
                List<float> alldata = new List<float>();
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

                AnalyseData(alldata);
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
            if (spectra.Count <= 0) return (null);
            int size = spectra[0].fft.Length;
            Bitmap bmp = new Bitmap(spectra.Count, size);
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
                for (int row = 0; row < size; row++)
                {
                    var scaled = Scale(sp.fft[row]);
                    bmp.SetPixel(col, size - row - 1, scaled);
                }
                col++;
            }

            using (var g = Graphics.FromImage(bmp))
            {
                int binsPer10kHz = (int)Math.Floor(10000.0d / HzPerBin);
                Pen blackPen = new Pen(Color.LightGray);
                for (int i = FFTSize / 2 - 1; i >= 0; i -= binsPer10kHz)
                {
                    g.DrawLine(blackPen, 0.0f, i, spectra.Count, i);
                }

                double spDurationMs = (1000.0d * FFTAdvance) / sampleRate;
                int spPer100ms = (int)Math.Floor(100 / spDurationMs);

                float xPos = 0.0f;
                while (xPos < spectra.Count)
                {
                    g.DrawLine(blackPen, xPos, 0.0f, xPos, (float)size - 1);
                    xPos += spPer100ms;
                }

                Pen redPen = new Pen(Color.Red);
                Pen bluePen = new Pen(Color.Blue);
                float last = size / 2;
                bool inPulse = false;
                bool fm = false;
                bool cf = false;
                bool qcf = false;
                for (int i = 1; i < peakFrequency.Count && i < spectra.Count; i++)
                {
                    var startf = peakFrequency[i - 1].frequency;
                    var endf = peakFrequency[i].frequency;
                    var invertedStartBin = (float)size - (startf / HzPerBin);
                    var invertedEndBin = (float)size - (endf / HzPerBin);
                    float grad = size / 2 + (gradient[i] / HzPerBin) * 50;

                    if (grad < 0)
                    {
                        Debug.WriteLine($"gradient was {grad}");
                        grad = 0;
                    }
                    //grad = grad * 50;
                    if (grad > size)
                    {
                        Debug.WriteLine($"gradient was {grad}");
                        grad = size;
                    }

                    if (invertedStartBin != size && invertedEndBin != size && (startf > endf && startf < 2 * endf) && startf > endf * 0.8d)
                    {
                        g.DrawLine(redPen, i - 1, invertedStartBin, i, invertedEndBin);
                        inPulse = true;

                        //Debug.WriteLine($"{i} - {grad}");

                        //if (last > 0 && last < size && grad > 0 && grad < size)
                        //{
                        if (grad <= 0) grad = 1;
                        if (grad > size - 1) grad = size - 1;
                        g.DrawLine(bluePen, i - 1, last, i, grad);
                        //if (inPulse)
                        //{
                        // gradient=Hz/unitAdvance
                        var slope = ((double)gradient[i] / advanceMS) / 1000.0d; // gives kHz/ms
                        if (slope > 0.0d)
                        {
                            if (slope < 0.1d) cf = true;
                            else if (slope < 1.0d) qcf = true;
                            else fm = true;
                        }
                        //}
                        //}
                    }
                    else
                    {
                        if (inPulse)
                        {
                            string strType = "";
                            if (fm) strType += " fm";
                            if (cf) strType += " cf";
                            if (qcf) strType += " qcf";
                            if (!String.IsNullOrWhiteSpace(strType))
                            {
                                g.DrawString(strType, new Font(FontFamily.GenericSerif, 10.0f), new SolidBrush(Color.Blue), new Point(i, size - 20));
                            }
                            fm = false;
                            cf = false;
                            qcf = false;

                            inPulse = false;
                        }
                    }
                    last = grad;
                }
            }

            return (Tools.ToBitmapSource(bmp));
        }

        private readonly double logMaximumValue;
        private readonly double logMinimumValue;
        private double advanceMS = 0;
        private List<float> gradient = new List<float>();
        private double MaximumValue;
        private double MinimumValue;

        private List<(int frequency, double value, int bin)> peakFrequency = new List<(int frequency, double value, int bin)>();

        /// <summary>
        /// Given a data stream in the form of a SampleProvider, performs the deep analysis
        /// and generates the report.
        /// </summary>
        /// <param name="data"></param>
        private void AnalyseData(List<float> data)
        {
            //List<Spectrum> spectra = new List<Spectrum>();
            //int FFTOrder = 10;
            float scale = 0.9f / (Math.Abs(Math.Max(data.Max(), Math.Abs(data.Min()))));// scale all data to 90% of maximum value
            FFTAdvance = (int)Math.Floor(FFTSize * .5d);

            advanceMS = ((double)FFTAdvance / (double)sampleRate) * 1000.0d;

            double[] FFTQuiet = Enumerable.Repeat(0.0d, (FFTSize / 2) + 1).ToArray<double>();
            float[] buffer = new float[FFTSize];
            int offset = 0;

            while (data.Count - offset >= FFTSize)
            {
                buffer = data.Skip(offset).Take(FFTSize).ToArray();
                Spectrum spect = new Spectrum(FFTOrder);
                spect.Create(buffer, sampleRate, scale);
                spectra.Add(spect);
                offset += FFTAdvance;
            }
            var sortedSpectra = from sp in spectra
                                orderby sp.fftMean
                                select sp;
            int quietCount = (int)Math.Floor(sortedSpectra.Count() / 10.0d);
            if (quietCount > 0)
            {
                FFTQuiet = Enumerable.Range(0, FFTSize / 2).AsParallel()
                    .Select(i => sortedSpectra.Take(quietCount).Select(a => a.fft.Skip(i).First()).Average()).ToArray<double>();
            }

            MinimumValue = double.MaxValue;
            MaximumValue = double.MinValue;
            double spectrumMax = double.MinValue;
            int spectrumPeakFrequency;
            int spectrumPeakBin;

            peakFrequency = new List<(int frequency, double value, int bin)>();
            gradient = new List<float>();

            var tpf = new List<(int frequency, double value, int bin)>();
            var tgrad = new List<int>();
            foreach (var sp in spectra)
            {
                spectrumMax = 0.0d;
                spectrumPeakFrequency = 0;
                spectrumPeakBin = -1;
                for (int i = (FFTSize / 2) - 1; i >= 0; i--)
                {
                    sp.fft[i] = sp.fft[i] - FFTQuiet[i];
                    if (sp.fft[i] < 0.0d) sp.fft[i] = 0.0d;
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
        /// Scales a value return a color that can be displayed in the bitmap
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private Color Scale(double val)
        {
            double dbRange = 48.0d;
            //var s = Math.Abs(20 * Math.Log10(Math.Abs(val)));
            //if (double.IsNaN(s)) s = 0;
            /*
            var s = val - MinimumValue;
            var range = MaximumValue - MinimumValue;
            var proportion = s / range;
            if (proportion > 1) proportion = 1.0d;
            if (proportion < 0) proportion = 0.0d;
            var value = 255 - (int)Math.Floor(proportion * 255);

            var dval = Math.Floor(20.0d * (proportion == 0.0d ? 0.0d : Math.Log10(proportion*255.0d)));
            var maxlog = 20.0d * Math.Log10(255);
            proportion = dval / maxlog;
            var level = (int)Math.Floor(255 * proportion);
            value = 255-((level<=255 && level>=0)?level:0);*/

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
    }
}