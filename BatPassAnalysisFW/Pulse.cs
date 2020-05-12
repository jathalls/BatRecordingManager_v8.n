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

        public int Pass { get; set; }

        public int Pulse_Number { get { return (peak.pulse_Number); } }

        public int Pulse_Length_ms { get { return (peak.peakWidthMs); } }

        public int Pulse_Interval_ms { get { return (peak.prevIntervalMs); } }



        private SpectrumDetails details { get; set; }


        /// <summary>
        /// constructor for the definition of a single pulse corresponding to a peak in the envelope
        /// </summary>
        /// <param name="dataInPass"></param>
        /// <param name="passStartInSegment"></param>
        /// <param name="peak"></param>
        /// <param name="pass"></param>
        /// <param name="quietStart"> start of a region of data below a threshold of at least 5000 samples</param>
        public Pulse(ref float[] dataInPass, int passStartInSegment, Peak peak, int pass,int quietStart,decimal spectrumFactor)
        {
            int FFTSize = CrossSettings.Current.Get<int>("FftSize");
            this.peak = peak;
            Pass = pass;
            float[] sectionData;
            List<float> sectionDataList = new List<float>();

            int peakStartInSeg = (int)(peak.GetStartAsSampleInSeg());
            int peakStartInPass = peakStartInSeg - passStartInSegment;
            int peakWidth = (int)(peak.getPeakWidthSamples());
            float sr = (float)peak.GetSampleRatePerSecond();

            float peakStartSecs = (float)peakStartInSeg / sr;
            float peakEnd = ((float)peakStartInSeg + (float)peakWidth) / sr;

            Debug.WriteLine($"peakStart secs {peakStartSecs} ends at {peakEnd} and has {peakWidth} samples");


            // start a peakswidth to the left of the peakStart or an FFTSize to the left of the peak whcihever is the greater
            int startPos = peakStartInPass - (Math.Max((int)(peak.getPeakWidthSamples()), FFTSize));
            if (startPos < 0) startPos = 0;

            int endpos = startPos;
            while (endpos < peakStartInPass + (peakWidth * 2))
            {
                endpos += FFTSize / 2;
            }
            if (endpos > dataInPass.Length) endpos = dataInPass.Length;


            sectionData = dataInPass.Skip(startPos).Take(endpos - startPos).ToArray<float>();
            float duration = (float)endpos / (float)(peak.GetSampleRatePerSecond());

            float startTime = (float)startPos / (float)(peak.GetSampleRatePerSecond());
            Debug.WriteLine($"analyse region {peak.pulse_Number} - leangth {sectionData.Length} at {startTime} for {duration} in Pass {pass}");
            float max = sectionData.Max();
            float mean = sectionData.Average();
            Debug.WriteLine($" data range=max{max}, mean {mean}");
            float[] preData = null;

            
            if (quietStart >= 0)
            {
                preData = dataInPass.Skip(quietStart).Take(sectionData.Length).ToArray<float>();
            }
            else
            {
                preData = dataInPass.Take(sectionData.Length).ToArray<float>();
                quietStart = 0;
            }
            Debug.WriteLine($"predata start={quietStart / sr} of size {preData.Length}={(preData.Length) / sr}s");


            Spectrum spectrum = new Spectrum((int)peak.GetSampleRatePerSecond(), FFTSize, peak.pulse_Number);
            spectrum.GetSpectralData(sectionData, preData);
            details = new SpectrumDetails(spectrum);
            details.GetDetailsFromSpectrum(peak, pass,spectrumFactor);

        }


        /// returns the spectral details of the pulse
        public SpectrumDetails GetSpectrumDetails()
        {
            return (details);
        }

        /// <summary>
        /// returns the peak for this pulse
        /// </summary>
        /// <returns></returns>
        public Peak getPeak()
        {
            return (peak);
        }
    }
}
