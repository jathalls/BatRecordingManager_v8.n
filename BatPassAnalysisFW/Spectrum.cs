﻿using DspSharp.Algorithms;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BatPassAnalysisFW
{
    /// <summary>
    /// Class to calculate and provide details of the spectral characteristics of a short
    /// segment of waveform equivalent to a single pulse or peak
    /// </summary>
    public class Spectrum
    {
        /// <summary>
        /// the fft of the real sample provided
        /// </summary>
        //public double[] fft;

        //public float[] autoCorrelation;

        public float autoCorrelationWidth { get; set; } = 0.0f;

        public double fftMean { get; set; } = 0.0d;

        public int pulseNumber { get; set; }

        /// <summary>
        /// the segment of waveform to be analysed
        /// </summary>
        

        /// <summary>
        /// the original sample rate
        /// </summary>
        public int sampleRate ;
        private int HzPerBin;
        private int frameSize;

        public Spectrum ( int sampleRate, int FFTSize, int pulseNumber = 0)
        {
            
            this.sampleRate = sampleRate;
            this.pulseNumber = pulseNumber;
            this.frameSize = FFTSize;
            HzPerBin = (sampleRate / 2) / (frameSize / 2);
        }

        public bool GetSpectralData(float[] sample, float[] pre_sample,out List<double> fft,out List<float> autoCorr)
        {

            int Overlap = frameSize / 2;
            double[] sampleFFT=new double[frameSize/2];
            double[] pre_sampleFFT=new double[frameSize/2];
            double[] rawFFT = new double[frameSize / 2];
            
            if(sample!=null && sample.Length > 0)
            {
                GetSpectrum(sample, frameSize, Overlap, out sampleFFT);
            }
            //Scale(1000, ref sampleFFT);
            sampleFFT = Smooth(sampleFFT,3);
            //WriteFile(@"X:\Demos\sampleData.csv",sample);
            
            
            if(pre_sample!=null && pre_sample.Length > frameSize/2 && !float.IsNaN(pre_sample[0]))
            {
                GetSpectrum(pre_sample, frameSize, Overlap/2, out rawFFT);
            }
            if(double.IsNaN(rawFFT[0]))
            {
                for(int i = 0; i < rawFFT.Length; i++)
                {
                    rawFFT[i] = 0.0d;
                }
            }
            //Scale(1000, ref pre_sampleFFT);

            pre_sampleFFT = Smooth(rawFFT,3);
            var fftArray = new double[sampleFFT.Length];
            
            for(int i = 0; i < sampleFFT.Length; i++)
            {
                //fft[i] = sampleFFT[i] - pre_sampleFFT[i];
                fftArray[i] = 20 * Math.Log10(sampleFFT[i]/pre_sampleFFT[i] );
            }

            fft = fftArray.ToList();
            
            fftMean = fftArray.Average();

            autoCorr=getAutoCorrelationAsFloatArray(rawFFT).ToList();

            //Scale(1000, ref fft);
            return (true);
        }

        private double[] Smooth(double[] data,int size)
        {
            
            double[] result = new double[data.Length];
            int seg = size;

            for(int i = 0; i < data.Length; i++)
            {
                if (i < seg)
                {
                    result[i] = data.Skip(0).Take(seg).Average();
                }
                else if(i>=data.Length-seg)
                {
                    result[i] = data.Skip(data.Length-seg).Take(seg).Average();
                }
                else
                {
                    result[i] = data.Skip(i - seg).Take(seg * 2).Average();
                }
            }
            return (result);
        }

        private void Scale(Double factor,ref Double[] data)
        {
            Double max = data.Max();
            Double min = data.Min();
            Double range = max - min;
            for(int i = 0; i < data.Length; i++)
            {
                data[i] = ((data[i] - min) / range) * factor;
            }
        }

        private bool GetSpectrum(float[] data, int frameSize, int Overlap,out double[] dataFFT) 
        {
            dataFFT = new double[frameSize / 2];
            //autoCorr = new List<float>();
            if (data == null || data.Length <= 0) return (false);
            int order = 8;
            switch (frameSize)
            {
                case 1024: order = 10;break;
                case 512: order = 9;break;
                case 256: order = 8;break;
                case 128: order = 7;break;
                case 64: order = 6;break;
                default:break;
            }

            string filename = "";
            
            for (int i = 0; i < dataFFT.Length; i++) dataFFT[i] = 0.0d;
            Complex[] dataBlock = new Complex[frameSize];
            int numBlocks = 0;
            int locationOfData = 0;
            while (locationOfData >= 0 )
            {
                locationOfData = GetDataBlock(data, locationOfData,frameSize,Overlap, out dataBlock);
                FastFourierTransform.FFT(true, order, dataBlock);
                
                for (int i = 0; i < dataFFT.Length && i<dataBlock.Length; i++)
                {

                    double temp= Math.Sqrt((dataBlock[i].X * dataBlock[i].X) + (dataBlock[i].Y * dataBlock[i].Y));
                    //dataFFT[i]+= 20.0 * Math.Log10(temp);
                    dataFFT[i] += temp;
                    
                }
                numBlocks++;
            }
            
            for(int i = 0; i < dataFFT.Length; i++)
            {
                dataFFT[i] = dataFFT[i] / numBlocks;
            
            }
            

            return (true);
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
        internal void getFrequencyDomain(out List<float> fftData, out List<float> autoCorr, List<float> sectionData, List<float> preData)
        {
            List<double> fftDataDbl = new List<double>();
            fftData = new List<float>();
            GetSpectralData(sectionData.ToArray(), preData.ToArray(), out fftDataDbl, out autoCorr);
            foreach(var d in fftDataDbl)
            {
                fftData.Add((float)d);
            }
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
                    float secs = (float)i / (float)sampleRate;
                    return (secs * 1000);
                }
            }
            return (0.0f);
        }

        public int getFFTSize()
        {
            return (frameSize);
        }

            private int GetDataBlock(float[] sample, int locationOfData,int frameSize,int overlap,  out Complex[] dataBlock)
        {
            dataBlock = new Complex[frameSize];
            int endOfData = locationOfData + frameSize-1;
            int nextStart = locationOfData + overlap;

            for(int i=locationOfData;i<(endOfData) && i < sample.Length; i++)
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
                for(int i = endOfData; i < sample.Length; i++)
                {
                    dataBlock[i].X = 0.0f;
                    dataBlock[i].Y = 0.0f;
                }
                return (-1);
            }
        }
    }
}
