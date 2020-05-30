using Acr.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public int Pulse_Number { get { return (peak.pulse_Number); } }

        /// <summary>
        /// et returns peak.peakWidthms
        /// </summary>
        public int Pulse_Length_ms { get { return (peak.peakWidthMs); } }

        /// <summary>
        /// get returns peak.prevIntervalMs
        /// </summary>
        public int Pulse_Interval_ms { get { return (peak.prevIntervalMs); } }

        private DataAccessBlock passDAB;

        internal SpectrumDetails spectralDetails { get; set; }

        private int startPosInPass;

        private int endposInPass;

        private int quietStart;


        /// <summary>
        /// constructor for the definition of a single pulse corresponding to a peak in the envelope
        /// </summary>
        /// <param name="dataInPass"></param>
        /// <param name="passStartInSegment"></param>
        /// <param name="peak"></param>
        /// <param name="pass"></param>
        /// <param name="quietStart"> start of a region of data below a threshold of at least 5000 samples</param>
        public Pulse(DataAccessBlock passDAB,  int passStartInSegment, Peak peak, int pass,int quietStart,decimal spectrumFactor, SpectrumDetails spectralDetails=null)
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

            Debug.WriteLine($"peakStart secs {peakStartSecs} ends at {peakEnd} and has {peakWidth} samples");


            // start a peakswidth to the left of the peakStart or an FFTSize to the left of the peak whcihever is the greater
            startPosInPass = peakStartInPass - (Math.Max((int)(peak.getPeakWidthSamples()), FFTSize));
            if (startPosInPass < 0) startPosInPass = 0;

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
                Spectrum spectrum = new Spectrum((int)peak.GetSampleRatePerSecond(), FFTSize, peak.pulse_Number);
                List<double> fft = new List<double>();
                List<float> autoCorr = new List<float>();
                spectrum.GetSpectralData(sectionData.ToArray(), preData.ToArray(), out fft, out autoCorr);
                this.spectralDetails = new SpectrumDetails(spectrum);
                this.spectralDetails.GetDetailsFromSpectrum(fft, peak, pass, spectrumFactor);
            }
            else
            {
                this.spectralDetails = spectralDetails;
            }

        }

        

            private void getData(ref List<float> data,ref List<float> preData)
        {
            DataAccessBlock pulseDAB = new DataAccessBlock(passDAB.FQfileName, passDAB.BlockStartInFileInSamples + startPosInPass, endposInPass - startPosInPass);
            data = pulseDAB.getData().ToList<float>();

            pulseDAB = new DataAccessBlock(passDAB.FQfileName, passDAB.BlockStartInFileInSamples, endposInPass - startPosInPass);
            if (quietStart >= 0)
            {
                pulseDAB = new DataAccessBlock(passDAB.FQfileName, passDAB.BlockStartInFileInSamples + quietStart, endposInPass - startPosInPass);
                //preData = dataInPass.Skip(quietStart).Take(sectionData.Length).ToArray<float>();
            }
            
            preData = pulseDAB.getData().ToList<float>();

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
            spectrum.getFrequencyDomain(out fftData, out autoCorr, sectionData, preData);

        }
    }
}
