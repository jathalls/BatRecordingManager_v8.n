using System;
using System.Linq;

namespace BatPassAnalysisFW
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///class SpectralPeak, derived from Peak but with added spectarl measureents
    ///
    public class SpectralPeak
    {
        public Peak parentPeak;

        /// <summary>
        /// Constructor for spectralPeak initialises all Peak parameters but does not retains all the raw data for the peak
        /// </summary>
        /// <param name="peakNumber"></param>
        /// <param name="rate"></param>
        /// <param name="segmentStart"></param>
        /// <param name="peakStart"></param>
        /// <param name="peakWidth"></param>
        /// <param name="peakArea"></param>
        /// <param name="peakMaxHeight"></param>
        /// <param name="interval"></param>
        /// <param name="waveformData"></param>
        public SpectralPeak(int fLow, int fHigh, double peakAreaHz, int fPeak, int sampleRate, float autoCorrelationWidth,
            Peak parentPeak = null, bool isValidPulse = true, float[] waveformData = null, int HzPerSample = 1, float AbsoluteThreshold = 0.0f)
        {
            this.fLow = fLow;
            this.fHigh = fHigh;
            this.fPeak = fPeak;
            this.peakAreaHz = peakAreaHz;
            this.sampleRate = sampleRate;
            this.IsValidPulse = IsValidPulse;
            this.waveformData = waveformData;
            this.HzPerSample = HzPerSample;

            //halfHeightValue(ref waveformData);
            if (parentPeak != null)
            {
                this.parentPeak = parentPeak;
            }
            IsValidPulse = isValidPulse;

            this.AutoCorrelationWidth = autoCorrelationWidth;
            this.AbsoluteThreshold = AbsoluteThreshold;

            //halfHeightValue(ref waveformData);

            //AutoCorrelationWidth = getAutoCorrelationWidth();
        }

        public float AbsoluteThreshold { get; set; } = 0.0f;

        public float AutoCorrelationWidth
        {
            get;
            set;
        }

        public float AutoCorrelationWidthCms
        {
            get
            {
                return (34 * AutoCorrelationWidth);
            }
        }

        /// <summary>
        /// high (start) frequency of the call
        /// </summary>
        public int fHigh { get; set; } = -1;

        /// <summary>
        /// low (end) frequency of the call
        /// </summary>
        public int fLow { get; set; } = -1;

        /// <summary>
        /// frequency of maximum amplitude in the call spectrum
        /// </summary>
        public int fPeak { get; set; } = -1;

        /// <summary>
        /// The high frequency bound of the peak at half the maximum amplitude
        /// May call halfHeightWidthHz to initialise the values
        /// </summary>
        public int halfHeightHighFrequency
        {
            get
            {
                return (_halfHeightHighFrequency);
            }
            set
            {
                _halfHeightHighFrequency = value;
            }
        }

        /// <summary>
        /// The low frequency bound of the peak at half the maximum amplitude
        /// May call halfHeightWidth to initialise the values
        /// </summary>
        public int halfHeightLowFrequency
        {
            get
            {
                return (_halfHeightLowFrequency);
            }
            set { _halfHeightLowFrequency = value; }
        }

        /// <summary>
        /// The width in Hz of the peak at the points next above half the maximum height
        /// </summary>
        public int halfHeightWidthHz
        {
            get
            {
                return (_halfHeightWidthHz);
            }

            set { _halfHeightWidthHz = value; }
        }

        public bool IsValidPulse { get; set; } = false;

        public int? Pass_
        {
            get
            {
                return (parentPeak?.peak_Number);
            }
        }

        public double peakAreaHz { get; set; } = -1;

        /// <summary>
        /// the frequency of the highest value in the spectrum returns fPeak
        /// </summary>

        public int? Pulse_
        {
            get
            {
                return (parentPeak?.peak_Number);
            }
        }

        public float? Pulse_Interval_ms
        {
            get
            {
                return (parentPeak.prevIntervalMs);
            }
        }

        public float? Pulse_Length_ms
        {
            get
            {
                return (parentPeak?.peakWidthMs);
            }
        }

        public float? Pulse_Start_ms
        {
            get
            {
                return (1000 * (parentPeak.getStartAsSampleInPass() / (float)sampleRate));
            }
        }

        public int? recordingNumber
        {
            get
            {
                return (parentPeak?.recordingNumber);
            }
        }

        /// <summary>
        /// Creates a record of the detected peak based on the sample start number and the width of
        /// the detected peak in samples
        /// </summary>
        /// <param name="fLow"></param>
        /// <param name="peakCount"></param>
        public static SpectralPeak CreateSP(int fLow, int fHigh, double peakAreaHz, int fPeak, int sampleRate, float autoCorrelationWidth,
            Peak parentPeak = null, bool isValidPulse = true, float[] WaveformData = null, int HzPerSample = 1, float AbsoluteThreshold = 0.0f)
        {
            //Debug.WriteLine($"Peak at {peakStart} for {peakCount} - {(float)peakStart / sampleRate}/{(float)peakCount / sampleRate}");
            SpectralPeak peak = new SpectralPeak(fLow, fHigh, peakAreaHz, fPeak, sampleRate, autoCorrelationWidth,
                parentPeak, isValidPulse, WaveformData, HzPerSample, AbsoluteThreshold);

            return (peak);
        }

        internal int GetHzPerSample()
        {
            return (HzPerSample);
        }

        private int _halfHeightHighFrequency = -1;
        private int _halfHeightLowFrequency = -1;
        private int _halfHeightWidthHz = -1;

        /// <summary>
        /// The scale of the spectrum in Hz per sample in the data supplied
        /// </summary>
        private int HzPerSample
        {
            get; set;
        }

        private int sampleRate { get; set; }
        private float[] waveformData { get; set; }
        //private float[] autoCorrelation { get; set; }

        /*
        /// <summary>
        /// Calculates and stores the half-height parameters
        /// </summary>
        /// <param name="data"></param>
        private void halfHeightValue(ref float[] data)
        {
            if (data == null || data.Length <= 0)
            {
                _halfHeightWidthHz = -1;
                _halfHeightLowFrequency = -1;
                _halfHeightHighFrequency = -1;
                return;
            }

            int lowEdge = -1;
            int highEdge = -1;
            double halfHeight = data.Max() / 2;
            for (int i = 0; i < data.Length; i++)
            {
                if (lowEdge < 0 && data[i] > halfHeight)
                {
                    lowEdge = i;
                    i = data.Length;
                }
            }
            for (int i = data.Length - 1; i >= 0; i--)
            {
                if (highEdge < 0 && data[i] > halfHeight)
                {
                    highEdge = i;
                    i = -1;
                }
            }
            //Debug.WriteLine($"halfHeightWidth:- he={highEdge} le={lowEdge} hhw={highEdge - lowEdge}");
            _halfHeightHighFrequency = (fHigh + highEdge) * HzPerSample;
            _halfHeightLowFrequency = (fHigh + lowEdge) * HzPerSample;
            _halfHeightWidthHz = _halfHeightHighFrequency - _halfHeightLowFrequency;
        }*/ // halfHeightValue
    }
}