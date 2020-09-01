using Acr.Settings;
using DspSharp.Utilities.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// class to hold the details of peaks in <see langword="abstract"/>spectrum with
    /// public fields suitable for display in a DataGrid
    /// </summary>
    public class SpectrumDetails
    {
        private readonly Spectrum m_Spect;

        public int pulse { get; set; } = 0;

        public ObservableList<Peak> spectralPeakList = new ObservableList<Peak>();


        private float? _pfMeanOfPeakFrequenciesInSpectralPeaksList = null;
        /// <summary>
        /// returns the average of the peak frequencies in the spectralPeakList
        /// </summary>
        public float pfMeanOfPeakFrequenciesInSpectralPeaksList
        {
            get
            {
                if (_pfMeanOfPeakFrequenciesInSpectralPeaksList == null)

                {
                    var pfMeanList = from sp in spectralPeakList
                                     where sp != null && (sp as SpectralPeak) != null && (sp as SpectralPeak).peakFrequency >= 15000.0
                                     select (sp as SpectralPeak).peakFrequency;
                    if (pfMeanList != null && pfMeanList.Any())
                    {
                        _pfMeanOfPeakFrequenciesInSpectralPeaksList = ((float)pfMeanList.Average());
                    }
                }
                return _pfMeanOfPeakFrequenciesInSpectralPeaksList ?? -1.0f;
            }

            set
            {
                _pfMeanOfPeakFrequenciesInSpectralPeaksList = value;
            }
        }

        private float? _pfStart = null;
        public float pfStart
        {
            get
            {
                if (_pfStart == null)
                {
                    var pfStartList = from sp in spectralPeakList
                                      where sp != null && (sp as SpectralPeak) != null && (sp as SpectralPeak).highFrequency >= 15000.0
                                      select (sp as SpectralPeak).highFrequency;
                    if (pfStartList != null && pfStartList.Any())
                    {
                        _pfStart = ((float)pfStartList.Average());
                    }
                }
                return _pfStart ?? -1.0f;
            }

            set
            {
                _pfStart = value;
            }
        }


        private float? _pfEnd = null;
        public float pfEnd
        {
            get
            {
                if (_pfEnd == null)
                {
                    var pfEndList = from sp in spectralPeakList
                                    where sp != null && (sp as SpectralPeak) != null && (sp as SpectralPeak).lowFrequency >= 15000.0
                                    select (sp as SpectralPeak).lowFrequency;
                    if (pfEndList != null && pfEndList.Any())
                    {
                        _pfEnd = ((float)pfEndList.Average());
                    }
                }
                return _pfEnd ?? -1.0f;
            }
            set { _pfEnd = value; }
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

        public void AddSpectralPeak(SpectralPeak spectPeak)
        {
            spectralPeakList.Add(spectPeak);
        }




        public Spectrum getSpectrum()
        {
            return (m_Spect);
        }

        internal bool GetDetailsFromSpectrum(List<double> fft, Peak parentPeak, bool isValidPulse, int passNumber = 1, decimal spectrumFactor = 1.8m)
        {
            if (m_Spect == null) return false;
            //Debug.WriteLine($"\nFor pulse number {m_Spect.pulseNumber}");

            var data = fft.Select(s => (float)s).ToArray<float>();

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
            PassAnalysis.getPeaks(ref data, m_Spect.sampleRate, leadInSamples: leadInSamples, leadOutSamples: leadOutSamples, thresholdFactor: (float)spectrumFactor,
                out spectralPeakList, m_Spect.autoCorrelationWidth, startOfPassInSegment: 0, asSpectralPeak: true, parentPeak, isValidPulse, PassNumber: passNumber, RecordingNumber: parentPeak.recordingNumber);
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
            //Debug.WriteLine($"Detected {orderedData.Count()} peaks in the spectrum");

            pulse = m_Spect.pulseNumber;


            return (true);
        }



    }
}