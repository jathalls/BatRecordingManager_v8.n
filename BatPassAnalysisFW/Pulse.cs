﻿using Acr.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BatPassAnalysisFW
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    public class Pulse
    {
        private Peak peak { get; set; }

        /// <summary>
        /// number of the pass in the segment
        /// </summary>
        public int Pass { get; set; }

        /// <summary>
        /// get returns peak.pulse_Number
        /// </summary>
        public int Pulse_Number { get { return (peak.peak_Number); } }

        /// <summary>
        /// et returns peak.peakWidthms
        /// </summary>
        public float Pulse_Length_ms { get { return (peak.peakWidthMs); } }

        /// <summary>
        /// get returns peak.prevIntervalMs
        /// </summary>
        public float Pulse_Interval_ms { get { return (peak.prevIntervalMs); } }

        private DataAccessBlock passDAB;

        internal SpectrumDetails spectralDetails { get; set; }

        private int startPosInPass;

        private int endposInPass;

        private int quietStart;

        public bool isValidPulse { get; set; } = false;

        

        /// <summary>
        /// constructor for the definition of a single pulse corresponding to a peak in the envelope
        /// </summary>
        /// <param name="dataInPass"></param>
        /// <param name="passStartInSegment"></param>
        /// <param name="peak"></param>
        /// <param name="pass"></param>
        /// <param name="quietStart"> start of a region of data below a threshold of at least 5000 samples</param>
        public Pulse( DataAccessBlock passDAB,  int passStartInSegment, Peak peak, int pass,int quietStart,decimal spectrumFactor, SpectrumDetails spectralDetails=null)
        {
            
            int FFTSize = CrossSettings.Current.Get<int>("FftSize");
            this.peak = peak;
            Pass = pass;
            this.passDAB = passDAB;
            List<float> sectionData;

            this.quietStart = quietStart;
            //List<float> sectionDataList = new List<float>();

            int peakStartInSeg = (int)(peak.GetStartAsSampleInSeg());
            int peakStartInPass = peakStartInSeg - passStartInSegment;
            int peakWidth = (int)(peak.getPeakWidthSamples());
            float sr = (float)peak.GetSampleRatePerSecond();

            float peakStartSecs = (float)peakStartInSeg / sr;
            float peakEnd = ((float)peakStartInSeg + (float)peakWidth) / sr;

            //Debug.WriteLine($"peakStart secs {peakStartSecs} ends at {peakEnd} and has {peakWidth} samples");


            // start a peakswidth to the left of the peakStart or an FFTSize to the left of the peak whcihever is the greater
            startPosInPass = peakStartInPass - (Math.Max((int)(peak.getPeakWidthSamples()), FFTSize));
            if (startPosInPass < 0) startPosInPass = 0;
            peak.startPosInPulse = peakStartInPass - startPosInPass;
            endposInPass = startPosInPass;
            while (endposInPass < peakStartInPass + (peakWidth * 2))
            {
                endposInPass += FFTSize / 2;
            }
            if (endposInPass > passDAB.Length) endposInPass = startPosInPass+(int)passDAB.Length;
            if (endposInPass < startPosInPass)
            {
                Debug.WriteLine($"Invalid Pulse end, Pass start={startPosInPass} for {passDAB.Length}; Pulse start in Pass={peakStartInPass}");
            }

            sectionData = new List<float>();
            List<float> preData = new List<float>();

            getData(ref sectionData, ref preData);

            //sectionData = dataInPass.Skip(startPos).Take(endpos - startPos).ToArray<float>();
            float duration = (float)endposInPass / (float)(peak.GetSampleRatePerSecond());

            float startTime = (float)startPosInPass / (float)(peak.GetSampleRatePerSecond());
            //Debug.WriteLine($"analyse region {peak.pulse_Number} - leangth {sectionData.Length} at {startTime} for {duration} in Pass {pass}");
            float max = sectionData.Max();
            float mean = sectionData.Average();
            //Debug.WriteLine($" data range=max{max}, mean {mean}");




            //Debug.WriteLine($"predata start={quietStart / sr} of size {preData.Length}={(preData.Length) / sr}s");

            if (spectralDetails == null)
            {
                Spectrum spectrum = new Spectrum((int)peak.GetSampleRatePerSecond(), FFTSize, peak.peak_Number);
                List<double> fft = new List<double>();
                List<float> autoCorr = new List<float>();
                isValidPulse=spectrum.GetSpectralData(sectionData.ToArray(), preData.ToArray(),peak, out fft, out autoCorr);
                this.spectralDetails = new SpectrumDetails(spectrum);
                this.spectralDetails.GetDetailsFromSpectrum(fft, peak,isValidPulse, pass, spectrumFactor);
            }
            else
            {
                this.spectralDetails = spectralDetails;
            }

        }

        public int GetLength()
        {
            //DataAccessBlock pulseDAB = new DataAccessBlock(passDAB.FQfileName, passDAB.BlockStartInFileInSamples + startPosInPass, endposInPass - startPosInPass);
            return (endposInPass - startPosInPass);
        }


        /// <summary>
        /// Returns the data for this Pulse, which includes leadin of a peak width and a leadout of up to a peakwidth
        /// but truncated so that the data length is a multiple of the FFTSize.
        /// Returns the offset of the actual peak into the Pulse.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="preData"></param>
        /// <returns></returns>
        public int getData(ref List<float> data, ref List<float> preData)
        {
            DataAccessBlock pulseDAB = new DataAccessBlock(passDAB.FQfileName, passDAB.BlockStartInFileInSamples + startPosInPass, endposInPass - startPosInPass);
            data = pulseDAB.getData().ToList<float>();
            int offset = peak.getStartAsSampleInPass() - startPosInPass;

            pulseDAB = new DataAccessBlock(passDAB.FQfileName, passDAB.BlockStartInFileInSamples, endposInPass - startPosInPass);
            if (quietStart >= 0)
            {
                pulseDAB = new DataAccessBlock(passDAB.FQfileName, passDAB.BlockStartInFileInSamples + quietStart, endposInPass - startPosInPass);
                //preData = dataInPass.Skip(quietStart).Take(sectionData.Length).ToArray<float>();
            }

            preData = pulseDAB.getData().ToList<float>();
            return (offset);
        }


        /// returns the spectral details of the pulse
        public SpectrumDetails GetSpectrumDetails()
        {
            return (spectralDetails);
        }

        /// <summary>
        /// returns the peak for this pulse
        /// </summary>
        /// <returns></returns>
        public Peak getPeak()
        {
            return (peak);
        }

        internal void getFFT(out List<float> fftData, out List<float> autoCorr)
        {
            List<float> sectionData = new List<float>();
            List<float> preData = new List<float>();

            getData(ref sectionData, ref preData);

            Spectrum spectrum = spectralDetails.getSpectrum();
            spectrum.getFrequencyDomain(out fftData, out autoCorr, sectionData, preData,peak);

        }

        internal BitmapImage getEnvelopeBitmap()
        {
            var bmp = GetGraph();
            return (bpaPass.loadBitmap(bmp));
        }

        internal Bitmap GetGraph()
        {
            //List<float> paddedPassData = new List<float>();
            List<float> preData = new List<float>();
            List<float> peakdata = new List<float>();

            //_ = getData(ref paddedPassData, ref preData);
            //passDAB.BlockStartInFileInSamples, endposInPass - startPosInPass
            int sampleRate = peak.GetSampleRatePerSecond();
            float[] envelope=bpaPass.GetEnvelope(passDAB, sampleRate);
            int length = endposInPass - startPosInPass;
            float[] paddedPassData = envelope.Skip(startPosInPass).Take(length).ToArray();
            int factor = 1;
            if (paddedPassData.Count() > 3800)
            {
                paddedPassData = shrinkData(paddedPassData, 3800,out factor);
            }

            Debug.WriteLine($"Pulse {Pulse_Number} of {length} samples from {startPosInPass}");
            Debug.WriteLine($"Peak at {peak.startPosInPulse} of length {peak.getPeakWidthSamples()}");


            var bmp = new Bitmap(paddedPassData.Count(), (int)(0.56f * (float)paddedPassData.Count()), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                System.Drawing.Pen blackPen = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Black));
                System.Drawing.Pen redPen = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Red));
                float max = paddedPassData.Max();
                
                Point last = new Point(0,Scale(paddedPassData[0], max, bmp.Height));
                for(int i = 1; i < paddedPassData.Count(); i++)
                {
                    System.Drawing.Pen pen = blackPen;
                    if(i>(peak.startPosInPulse)/factor && i < (peak.startPosInPulse + peak.getPeakWidthSamples())/factor)
                    {
                        pen=redPen;
                    }
                    Point newPoint = new Point(i, Scale(paddedPassData[i], max, bmp.Height));
                    g.DrawLine(pen, last, newPoint);
                    last = newPoint;
                }

                int th = Scale(peak.AbsoluteThreshold, max, bmp.Height);
                g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Green),new Point(0,th),new Point(bmp.Width-1,th));
                Debug.WriteLine($"Threshold drawn at {th}");
            }

            return (bmp);
           
        }

        /// <summary>
        /// reduces the length of a float array by averaging
        /// </summary>
        /// <param name="paddedPassData"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private float[] shrinkData(float[] paddedPassData, int sizeLimit,out int factor)
        {
            List<float> result = new List<float>();
            int ratio = (int)(paddedPassData.Count() / sizeLimit);
            factor = ratio;
            for(int i = 0; i < paddedPassData.Count() - ratio; i += ratio)
            {
                float mean = paddedPassData.Skip(i).Take(ratio).Average();
                result.Add(mean);
            }
            return (result.ToArray());

        }

        private int Scale(float val, float max, int height)
        {
            int result=height-(int)((val/max)*(float)height);
            
            return (result);
        }
    }
}
