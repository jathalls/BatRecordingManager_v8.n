using DspSharp.Algorithms;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Class to calculate and provide details of the spectral characteristics of a short
    /// segment of waveform equivalent to a single pulse or peak
    ///
    /// Public:
    /// float autoCorrelationWidth
    /// double fftMean
    /// int pulseNumber
    /// int sampleRate
    ///
    /// Private:
    /// int HzPerBin
    /// int frameSize
    ///
    /// </summary>
    public class Spectrum
    {
        /// <summary>
        /// the fft of the real sample provided
        /// </summary>
        //public double[] fft;

        //public float[] autoCorrelation;

        /// <summary>
        /// the original sample rate
        /// </summary>
        public int sampleRate;

        public Spectrum(int sampleRate, int FFTSize, int pulseNumber = 0)
        {
            this.sampleRate = sampleRate;
            this.pulseNumber = pulseNumber;
            this.frameSize = FFTSize;
            HzPerBin = (sampleRate / 2) / (frameSize / 2);
        }

        public float autoCorrelationWidth { get; set; } = 0.0f;

        public double fftMean { get; set; } = 0.0d;

        public int pulseNumber { get; set; }

        /// <summary>
        /// Uses fine resolution FFTs to tray and determine the shape of the pulse
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="pre_sample"></param>
        public (float startSlope, float midSlope, float endSlope, float allSLope) GetFFTDetail(float[] sample, float[] pre_sample, (int Offset, int Length) peakPos)
        {
            (float startSlope, float midSlope, float endSlope, float allSLope) result = (0.0f, 0.0f, 0.0f, 0.0f);
            int FFTSize = 512;
            if (sample != null && pre_sample != null && sample.Count() > 0 && pre_sample.Count() > 0)
            {
                if (peakPos.Length < FFTSize + 70)
                {
                    FFTSize = 256;
                    if (peakPos.Length < FFTSize + 70) return (result);
                }
                double[,] spectrogram = new double[FFTSize, FFTSize];
                result = GetSpectrogram(sample, pre_sample, FFTSize, peakPos);
            }
            return (result);
        }

        public int getFFTSize()
        {
            return (frameSize);
        }

        /// <summary>
        /// gets spectral data and returns HzPerBin
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="pre_sample"></param>
        /// <param name="peak"></param>
        /// <param name="fft"></param>
        /// <param name="autoCorr"></param>
        /// <param name="Overlap"></param>
        /// <param name="frameSize"></param>
        /// <returns></returns>
        public int GetSpectralData(float[] sample, float[] pre_sample, Peak peak, out List<double> fft, out List<float> autoCorr, int Overlap = -1, int frameSize = -1)
        {
            fft = null;
            autoCorr = null;
            if (frameSize < 0)
            {
                frameSize = this.frameSize;
            }
            if (Overlap < 0)
            {
                Overlap = frameSize / 2;
            }
            double[] sampleFFT = new double[frameSize / 2];
            double[] pre_sampleFFT = new double[frameSize / 2];
            double[] rawPreFFT = new double[frameSize / 2];
            bool isValidPulse = false;
            //GetSpectralDetail(sample, pre_sample);

            if (sample != null && sample.Length > 0)
            {
                _ = GetSpectrum(sample, pre_sample, frameSize, Overlap, peak, out sampleFFT, out rawPreFFT);
            }
            isValidPulse = true;
            //Scale(1000, ref sampleFFT);
            sampleFFT = Smooth(sampleFFT, 3);
            //WriteFile(@"X:\Demos\sampleData.csv",sample);

            //if (pre_sample != null && pre_sample.Length > frameSize / 2 && !float.IsNaN(pre_sample[0]))
            //{
            //    GetSpectrum(pre_sample, frameSize, Overlap / 2, peak, out rawPreFFT);
            //}
            if (double.IsNaN(rawPreFFT[0]))
            {
                for (int i = 0; i < rawPreFFT.Length; i++)
                {
                    rawPreFFT[i] = 0.0d;
                }
            }
            //Scale(1000, ref pre_sampleFFT);

            pre_sampleFFT = Smooth(rawPreFFT, 3);
            var fftArray = new double[sampleFFT.Length];

            //for (int i = 0; i < sampleFFT.Length; i++)
            //{
            //fft[i] = sampleFFT[i] - pre_sampleFFT[i];
            //    fftArray[i] = 20 * Math.Log10(sampleFFT[i] / pre_sampleFFT[i]);
            // } Already done in getSpectrum
            fftArray = sampleFFT;

            fft = fftArray.ToList();

            fftMean = fftArray.Average();

            autoCorr = getAutoCorrelationAsFloatArray(rawPreFFT).ToList();

            //Scale(1000, ref fft);
            return ((sampleRate) / frameSize);
        }

        internal float[] getAutoCorrelationAsFloatArray(double[] rawFft)
        {
            if (rawFft == null || rawFft.Length <= 0) return (null);
            int order = 1;
            for (order = 1; Math.Pow(2, order) < rawFft.Length; order++) { }
            Complex[] data = new Complex[2 * (int)Math.Pow(2, order)];
            int i = 0;
            foreach (var val in rawFft)
            {
                data[i].X = (float)val;
                data[i].Y = 0;
                i++;
            }

            FastFourierTransform.FFT(false, order + 1, data);

            float[] result = new float[data.Length];
            for (int j = 0; j < data.Length; j++)
            {
                result[j] = (float)Math.Sqrt((data[j].X * data[j].X) + (data[j].Y * data[j].Y));
            }

            float[] smoothed = new float[result.Length / 2];
            for (int j = smoothed.Length - 1; j >= 0; j--)
            {
                smoothed[j] = 0.0f;
                for (int k = 0; k < 8; k++)
                {
                    smoothed[j] += result[j + k];
                }
                smoothed[j] /= 8;
            }

            autoCorrelationWidth = getAutoCorrelationWidth(ref smoothed);
            return (smoothed);
        }

        ///calculates and retuns the smoothed FFT and the autocorrelation of the supplied data
        ///returns the number of HzPerBin of the FFT
        internal int getFrequencyDomain(out List<float> fftData, out List<float> autoCorr, List<float> sectionData, List<float> preData, Peak peak)
        {
            List<double> fftDataDbl = new List<double>();
            fftData = new List<float>();
            int hzPerBin = GetSpectralData(sectionData.ToArray(), preData.ToArray(), peak, out fftDataDbl, out autoCorr);
            foreach (var d in fftDataDbl)
            {
                fftData.Add((float)d);
            }
            return (HzPerBin);
        }

        private readonly int frameSize;

        /// <summary>
        /// the segment of waveform to be analysed
        /// </summary>
        private readonly int HzPerBin;

        private double[] calculateFFT(float[] data, int frameSize, int order, int Overlap)
        {
            double[] dataFFT = new double[frameSize / 2];
            for (int i = 0; i < dataFFT.Length; i++)
            {
                dataFFT[i] = 0.0d;
            }
            if (data == null || data.Length <= 0) return (dataFFT);
            Complex[] dataBlock = new Complex[frameSize];
            int numBlocks = 0;
            int locationOfData = 0;
            while (locationOfData >= 0 && locationOfData < data.Length)
            {
                locationOfData = GetDataBlock(data, locationOfData, frameSize, Overlap, out dataBlock);
                FastFourierTransform.FFT(true, order, dataBlock);

                for (int i = 0; i < dataFFT.Length && i < dataBlock.Length; i++)
                {
                    double temp = Math.Sqrt((dataBlock[i].X * dataBlock[i].X) + (dataBlock[i].Y * dataBlock[i].Y));
                    //dataFFT[i]+= 20.0 * Math.Log10(temp);
                    dataFFT[i] += temp;
                }
                numBlocks++;
            }

            for (int i = 0; i < dataFFT.Length; i++)
            {
                dataFFT[i] = dataFFT[i] / numBlocks;
            }
            return (dataFFT);
        }

        private float getAutoCorrelationWidth(ref float[] autoCorrelation)
        {
            if (autoCorrelation == null || autoCorrelation.Length <= 0) { return (0.0f); }
            float max = autoCorrelation.Max();
            float half = max / 2;
            for (int i = 0; i < autoCorrelation.Length; i++)
            {
                if (autoCorrelation[i] < half)
                {
                    float secs = i / (float)sampleRate;
                    return (secs * 1000);
                }
            }
            return (0.0f);
        }

        private int GetDataBlock(float[] sample, int locationOfData, int frameSize, int overlap, out Complex[] dataBlock)
        {
            dataBlock = new Complex[frameSize];
            int endOfData = locationOfData + frameSize - 1;
            int nextStart = locationOfData + overlap;

            for (int i = locationOfData; i < (endOfData) && i < sample.Length; i++)
            {
                int j = i - locationOfData;
                dataBlock[j].X = (float)(sample[i] * NAudio.Dsp.FastFourierTransform.HammingWindow(j, frameSize));
                dataBlock[j].Y = 0.0f;
            }
            if ((sample.Length - nextStart) > 0)
            {
                return (nextStart);
            }
            else
            {
                for (int i = endOfData; i < sample.Length; i++)
                {
                    dataBlock[i].X = 0.0f;
                    dataBlock[i].Y = 0.0f;
                }
                return (-1);
            }
        }

        /// <summary>
        /// Calculates and returns the power spectrum of a sample of data
        /// as an array of double
        /// </summary>
        /// <param name="section"></param>
        /// <param name="fFTSize"></param>
        /// <returns></returns>
        private double[] getFFT(float[] section)
        {
            double[] result = new double[section.Count()];
            int order = 8;
            switch (section.Count())
            {
                case 1024: order = 10; break;
                case 512: order = 9; break;
                case 256: order = 8; break;
                case 128: order = 7; break;
                case 64: order = 6; break;
                default:
                    Debug.WriteLine($"Trying to take an FFT of an invalid block size {section.Count()}");
                    return (result);
                    break;
            }
            Complex[] dataBlock = new Complex[section.Count()];
            for (int i = 0; i < dataBlock.Count(); i++)
            {
                //dataBlock[i].X = (float)(section[i]* NAudio.Dsp.FastFourierTransform.HammingWindow(i, section.Count()));
                dataBlock[i].X = section[i];
                dataBlock[i].Y = 0.0f;
            }
            FastFourierTransform.FFT(true, order, dataBlock);
            for (int i = 0; i < dataBlock.Length; i++)
            {
                double temp = Math.Sqrt((dataBlock[i].X * dataBlock[i].X) + (dataBlock[i].Y * dataBlock[i].Y));
                //dataFFT[i]+= 20.0 * Math.Log10(temp);
                result[i] += temp;
            }

            return (result);
        }

        /// <summary>
        /// Generates square spectrogram of the data with FFTs of the specified size advancing
        /// enough to rpvide a square result
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private (float startSlope, float midSlope, float endSlope, float allSLope) GetSpectrogram(float[] sample, float[] pre_sample, int FFTSize, (int Offset, int Length) peakPos)
        {
            (float startSlope, float midSlope, float endSlope, float allSLope) result = (0.0f, 0.0f, 0.0f, 0.0f);
            //double[,] result = new double[FFTSize, FFTSize];
            double[] preFFT = null;
            int advance = (int)(.0001f / (1.0f / sampleRate));   // advance corresponds to 0.1ms per datum in the result
            advance = 5;
            if (pre_sample.Length >= FFTSize)
            {
                float[] section = pre_sample.Take(FFTSize).ToArray();
                preFFT = getFFT(section);
            }
            if (peakPos.Offset > FFTSize / 2) peakPos.Offset = peakPos.Offset - FFTSize / 2;
            else peakPos.Offset = 0;
            float[] peakData = sample.Skip(peakPos.Offset).Take(peakPos.Length).ToArray();
            List<int> fPeakList = new List<int>();
            for (int start = 0, i = 0; (start + FFTSize < peakData.Length); start += advance, i++)
            {
                var section = peakData.Skip(start).Take(FFTSize).ToArray();

                double[] spFFT = getFFT(section);

                double max = double.MinValue;

                int jMax = 0;
                for (int j = 0; j < spFFT.Length / 2; j++)
                {
                    double val;
                    if (preFFT != null)
                    {
                        val = Math.Abs(spFFT[j] - preFFT[j]);
                    }
                    else
                    {
                        val = spFFT[j];
                    }

                    //result[j, i] = val;  // NB order of indices.  First index is by frequency, second by time
                    if (val > max)
                    {
                        if (j != 0 && j < FFTSize / 2)
                        {
                            max = val;
                            jMax = j;
                        }
                    }
                }
                fPeakList.Add(jMax);
            }
            int[] fPeakArray = fPeakList.ToArray();

            int last = fPeakArray[0];

            List<(float slope, float slopeGrad)> slopeList = new List<(float slope, float slopeGrad)>();
            var peakLength = fPeakArray.Length;
            if (peakLength > 10)
            {
                var segSize = (peakLength) / 5;
                var lastSlope = 1000.0f;
                float grandTotal = 0.0f;
                int allCount = 0;
                for (int seg = 0; seg < 5; seg++)
                {
                    if (seg * segSize < peakLength)
                    {
                        var newSlope = 0.0f;
                        float slopeTotal = 0.0f;
                        int count = 0;
                        float lastVal = fPeakArray[seg * segSize];
                        for (int s = (seg * segSize) + 1; s < ((seg + 1) * segSize) && s < peakLength; s++)
                        {
                            slopeTotal += fPeakArray[s] - lastVal;
                            grandTotal += fPeakArray[s] - lastVal;
                            lastVal = fPeakArray[s];
                            count++;
                            allCount++;
                        }
                        if (count > 0)
                        {
                            var slope = (float)slopeTotal / count;

                            if (slope > 0.0f) slope = 0.0f;
                            if (lastSlope < 1000)
                            {
                                newSlope = slope - lastSlope;
                            }
                            lastSlope = slope;
                            slopeList.Add((slope, newSlope));
                        }
                    }
                }
                var allSlope = grandTotal / allCount;
                if (slopeList.Count() >= 5)
                {
                    result = (Math.Abs(slopeList[0].slope), Math.Abs(slopeList[2].slope), Math.Abs(slopeList[4].slope), Math.Abs(allSlope));
                }
            }
            Debug.WriteLine("\n\n=======================");
            foreach (var v in slopeList) Debug.WriteLine($"{v.slope}\t{v.slopeGrad}");
            Debug.WriteLine("=====================\n");

            var revSlope = new List<int>();
            for (int i = fPeakArray.Max(); i >= fPeakArray.Min(); i--)
            {
                revSlope.Add((from v in fPeakArray
                              where v == i
                              select v).Count());
            }

            Debug.WriteLine("\n\n=======================");
            foreach (var v in revSlope) Debug.WriteLine(v);
            Debug.WriteLine("=====================\n");

#if DEBUG
            if (File.Exists(@"C:\BRMTestData\spect.csv")) File.Delete(@"C:\BRMTestData\spect.csv");

            for (int i = 0; i < fPeakArray.Length; i++)
            {
                File.AppendAllText(@"C:\BRMTestData\spect.csv", $"{fPeakArray[i]}, ");
            }
            File.AppendAllText(@"C:\BRMTestData\spect.csv", "\n");

#endif

            return (result);
        }

        /// <summary>
        /// gets the actual spectrum for the peak, suitable avaeraged and also the spectrum for the pre-sample data which will be used to remove
        /// background tones.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="preData"></param>
        /// <param name="frameSize"></param>
        /// <param name="Overlap"></param>
        /// <param name="peak"></param>
        /// <param name="dataFFT"></param>
        /// <param name="preFFT"></param>
        /// <returns></returns>
        private bool GetSpectrum(float[] data, float[] preData, int frameSize, int Overlap, Peak peak, out double[] dataFFT, out double[] preFFT)
        {
            dataFFT = new double[frameSize / 2];
            preFFT = new double[frameSize / 2];
            //autoCorr = new List<float>();
            if (data == null || data.Length <= 0) return (false);
            int order = 8;
            switch (frameSize)
            {
                case 1024: order = 10; break;
                case 512: order = 9; break;
                case 256: order = 8; break;
                case 128: order = 7; break;
                case 64: order = 6; break;
                default: break;
            }

            dataFFT = calculateFFT(data, frameSize, order, Overlap);
            preFFT = calculateFFT(preData, frameSize, order, Overlap);

            var pre_sampleFFT = Smooth(preFFT, 3);
            var fftArray = Smooth(dataFFT, 3);

            for (int i = 0; i < dataFFT.Length; i++)
            {
                //fft[i] = sampleFFT[i] - pre_sampleFFT[i];
                var ratio = fftArray[i] / pre_sampleFFT[i];
                fftArray[i] = 20 * Math.Log10(ratio);
            }

            dataFFT = fftArray;

            bool result = true;

            return (result);
        }

        private void Scale(double factor, ref double[] data)
        {
            double max = data.Max();
            double min = data.Min();
            double range = max - min;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = ((data[i] - min) / range) * factor;
            }
        }

        private double[] Smooth(double[] data, int size)
        {
            double[] result = new double[data.Length];
            int seg = size;

            for (int i = 0; i < data.Length; i++)
            {
                if (i < seg)
                {
                    result[i] = data.Skip(0).Take(seg).Average();
                }
                else if (i >= data.Length - seg)
                {
                    result[i] = data.Skip(data.Length - seg).Take(seg).Average();
                }
                else
                {
                    result[i] = data.Skip(i - seg).Take(seg * 2).Average();
                }
            }
            return (result);
        }

        /*
        /// <summary>
        /// takes smoothed and noise adjusted log scale fft data
        /// </summary>
        /// <param name="peak"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ValidateSpectrum(Peak peak, ref float[] data)
        {
            if (sampleRate >= 192000 && peak.getPeakWidthSamples() > sampleRate / 1000) // only validat pulses >1ms @ 384000 or 192000
            {
                int FFTSize = 128;
                int FFTOrder = 7;

                if (sampleRate < 384000)
                {
                    FFTSize = 64;
                    FFTOrder = 6;
                }

                double[] firstFFT = new double[FFTSize / 2];
                double[] lastFFT = new double[FFTSize / 2];
                double[] preFFT = new double[FFTSize / 2];

                int shortHzPerBin = (sampleRate / 2) / (FFTSize / 2);
                int short15KBorder = 15000 / shortHzPerBin;

                for (int i = 0; i < short15KBorder; i++)
                {
                    firstFFT[i] = 0.0d;
                    lastFFT[i] = 0.0d;
                    preFFT[i] = 0.0d;
                }
                Complex[] dataBlock;

                if (preData != null && preData.Length > FFTSize)
                {
                    int margin = (preData.Length - FFTSize) / 2;
                    _ = GetDataBlock(preData, margin, FFTSize, 0, out dataBlock);
                    FastFourierTransform.FFT(true, FFTOrder, dataBlock);
                    for (int i = short15KBorder; i < dataBlock.Length / 2 && i < firstFFT.Length; i++)
                    {
                        preFFT[i] = Math.Sqrt((dataBlock[i].X * dataBlock[i].X) + (dataBlock[i].Y * dataBlock[i].Y));
                    }
                }

                _ = GetDataBlock(data, peak.startPosInPulse + (FFTSize / 2), FFTSize, 0, out dataBlock);
                FastFourierTransform.FFT(true, FFTOrder, dataBlock);

                for (int i = short15KBorder; i < dataBlock.Length / 2 && i < firstFFT.Length; i++)
                {
                    firstFFT[i] = (Math.Sqrt((dataBlock[i].X * dataBlock[i].X) + (dataBlock[i].Y * dataBlock[i].Y))) - preFFT[i];
                }

                _ = GetDataBlock(data, peak.startPosInPulse + peak.getPeakWidthSamples() - (int)(1.5 * FFTSize), FFTSize, 0, out dataBlock);
                for (int i = short15KBorder; i < dataBlock.Length / 2 && i < firstFFT.Length; i++)
                {
                    lastFFT[i] = (Math.Sqrt((dataBlock[i].X * dataBlock[i].X) + (dataBlock[i].Y * dataBlock[i].Y))) - preFFT[i];
                }

                int firstPeak = firstFFT.MaxIndex();
                int lastPeak = lastFFT.MaxIndex();
                Debug.WriteLine($"for Pulse {peak.peak_Number} - {firstPeak * shortHzPerBin}->{lastPeak * shortHzPerBin}");
                if (lastPeak > firstPeak) return (false);
            }
            return (true);
        }*/
    }
}