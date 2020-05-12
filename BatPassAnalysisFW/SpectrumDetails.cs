using Acr.Settings;
using DspSharp.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// class to hold the details of peaks in <see langword="abstract"/>spectrum with
    /// public fields suitable for display in a DataGrid
    /// </summary>
    public class SpectrumDetails
    {
        private Spectrum m_Spect;

        public int pulse { get; set; } = 0;

        public ObservableList<Peak> spectralPeakList = new ObservableList<Peak>();

        public float pfMean
        {
            get
            {
                var pfMeanList = from sp in spectralPeakList
                                 where sp!=null && (sp as SpectralPeak)!=null && (sp as SpectralPeak).peakFrequency>15000.0
                                          select (sp as SpectralPeak).peakFrequency;
                if(pfMeanList!=null && pfMeanList.Any())
                {
                    return ((float)pfMeanList.Average());
                }
                return (-1.0f);
            }
        }

        public float pfStart
        {
            get
            {
                var pfStartList = from sp in spectralPeakList
                                 where sp != null && (sp as SpectralPeak) != null && (sp as SpectralPeak).highFrequency > 15000.0
                                 select (sp as SpectralPeak).highFrequency;
                if (pfStartList != null && pfStartList.Any())
                {
                    return ((float)pfStartList.Average());
                }
                return (-1.0f);
            }
        }

        public float pfEnd
        {
            get
            {
                var pfEndList = from sp in spectralPeakList
                                 where sp != null && (sp as SpectralPeak) != null && (sp as SpectralPeak).lowFrequency > 15000.0
                                 select (sp as SpectralPeak).lowFrequency;
                if (pfEndList != null && pfEndList.Any())
                {
                    return ((float)pfEndList.Average());
                }
                return (-1.0f);
            }
        }

        /// <summary>
        /// Creator for SpectrumDetails
        /// </summary>
        /// <param name="spect">
        /// The spectrum to be analysed to provide the details
        /// </param>
        public SpectrumDetails(Spectrum spect)
        {
            m_Spect = spect;
        }

        public float[] getFFT()
        {
            

            float[] fft_f = (from value in m_Spect.fft
                             select (float)value).ToArray<float>();
            return (fft_f);

        }

        internal bool GetDetailsFromSpectrum(Peak parentPulse,int passNumber=1,decimal spectrumFactor=1.8m)
        {
            if (m_Spect == null || m_Spect.fft == null || m_Spect.fft.Length <= 0) return false;
            //Debug.WriteLine($"\nFor pulse number {m_Spect.pulseNumber}");

            var data = m_Spect.fft.Select(s => (float)s).ToArray<float>();

            int leadInSamples = CrossSettings.Current.Get<int>("SpectrumLeadInSamples");
            if (leadInSamples <= 0)
            {
                leadInSamples = 5;
                CrossSettings.Current.Set<int>("SpectrumLeadInSamples", leadInSamples);
            }
            

            int leadOutSamples = CrossSettings.Current.Get<int>("SpectrumLeadOutSamples");
            if (leadOutSamples <= 0.0f)
            {
                leadOutSamples = 4;
                CrossSettings.Current.Set<float>("SpectrumLeadOutSamples", leadOutSamples);
            }
            

            //float[] shortData = data.Skip(40).ToArray<float>();
            PassAnalysis.getPeaks(ref data, m_Spect.sampleRate, leadInSamples:leadInSamples, leadOutSamples:leadOutSamples,thresholdFactor:(float)spectrumFactor, 
                out spectralPeakList, ref m_Spect.autoCorrelation, startOfstartOfPassInSegment:0, asSpectralPeak:true,parentPulse,PassNumber:passNumber,RecordingNumber:parentPulse.recordingNumber);
            var orderedData = new List<Peak>();
            orderedData = (from d in spectralPeakList
                           orderby d.GetPeakArea() descending
                           select d).ToList();
            spectralPeakList.Clear();
            if (orderedData.Any())
            {
                spectralPeakList.Add(orderedData.First());
            }
            //spectralPeakList.AddRange(orderedData);
            Debug.WriteLine($"Detected {orderedData.Count()} peaks in the spectrum");

            pulse = m_Spect.pulseNumber;


            return (true);
        }

        internal float[] getAutoCorrelation(bool byFft)
        {
            float[] result = m_Spect.getAutoCorrelationAsFloatArray();
            return (result);
        }
    }
}