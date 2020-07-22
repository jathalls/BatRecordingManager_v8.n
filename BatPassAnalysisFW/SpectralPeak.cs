using System;
using System.Linq;

namespace BatPassAnalysisFW
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///class SpectralPeak, derived from Peak but with added spectarl measureents
    ///
    public class SpectralPeak : Peak
    {
        //private float[] data;

        public int parentPulseIndex = -1;

        public int Pass_ { get; set; } = 1;

        public int Pulse_ { get; set; } = 1;

        public float Pulse_Length_ms { get; set; } = 0.0f;

        public float Pulse_Interval_ms { get; set; } = 0.0f;

        public float AbsoluteThreshold { get; set; } = 0.0f;



        private int _peakFrequency = -1;
        /// <summary>
        /// the frequency of the highest value in the spectrum
        /// </summary>
        public int peakFrequency
        {
            get
            {
                
                return (_peakFrequency);
            }
            set
            {
                _peakFrequency = value;
            }
        }



        private int _highFrequency = -1;
        /// <summary>
        /// the frequency at the  end of the spectral peak i.e the highest frequency of the peak
        /// </summary>
        public int highFrequency
        {
            get
            {
                if (_highFrequency < 0)
                {
                    _highFrequency = (int)((GetStartAsSampleInSeg() + getPeakWidthSamples()) * HzPerSample);
                }
                return (_highFrequency);
            }

            set { _highFrequency = value; }
        }

        private int _lowFrequency = -1;
        /// <summary>
        /// the frequency at the start of the spectral peak i.e. the lowest frequency of the pulse
        /// </summary>
        public int lowFrequency
        {
            get
            {
                if (_lowFrequency < 0)
                {
                    _lowFrequency = (int)(GetStartAsSampleInSeg() * HzPerSample);
                }
                return (_lowFrequency);
            }

            set { _lowFrequency = value; }
        }

        private int _halfHeightLowFrequency = -1;
        private int _halfHeightHighFrequency = -1;

        private int _halfHeightWidthHz = -1;
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
        /// The scale of the spectrum in Hz per sample in the data supplied
        /// </summary>
        private int HzPerSample
        {
            get; set;
        }

        private int sampleRate { get; set; }

        public int recordingNumber { get; set; }

        public bool IsValidPulse { get; set; } = false;

        //private float[] autoCorrelation { get; set; }

        private Peak parentPulse { get; set; }
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
        /// <param name="data"></param>
        public SpectralPeak(int peakNumber, int rate, int segmentStart, int peakStart, int peakWidth, double peakArea, float peakMaxHeight, int interval, ref float[] data,
            int HzPerSample, float autoCorrelationWidth, Peak parentPeak = null,bool isValidPulse=true, int PassNumber = 1, int RecordingNumber = 1, float AbsoluteThreshold = 0.0f) :
            base(peakNumber, rate, segmentStart, peakStart, peakWidth, peakArea, peakMaxHeight, interval, AbsoluteThreshold: AbsoluteThreshold)
        {

            //findPeakFrequency(ref data);
            //halfHeightValue(ref data);
            this.HzPerSample = HzPerSample;
            sampleRate = rate;
            halfHeightValue(ref data);
            if (parentPeak != null)
            {
                this.parentPulse = parentPeak;
                Pass_ = PassNumber;
                Pulse_ = parentPeak.peak_Number;
                Pulse_Length_ms = parentPeak.peakWidthMs;
                Pulse_Interval_ms = parentPeak.prevIntervalMs;
                

            }
            IsValidPulse = isValidPulse;
            this.recordingNumber = RecordingNumber;
            this.AutoCorrelationWidth = autoCorrelationWidth;
            this.AbsoluteThreshold = AbsoluteThreshold;
            findPeakFrequency(ref data,peakMaxHeight);

            //AutoCorrelationWidth = getAutoCorrelationWidth();
        }

        private void findPeakFrequency(ref float[] data,float max)
        {
            if (data == null || data.Length <= 0)
            {
                _peakFrequency = -1;

            }
            else
            {
                if (_peakFrequency < 0)
                {
                    //var max = data.Max<float>();
                    var index = Array.IndexOf(data,max);
                    _peakFrequency = (index + (int)GetStartAsSampleInSeg()) * HzPerSample;
                    /*
                    double max = double.MinValue;

                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] > max)
                        {
                            max = data[i];
                            _peakFrequency = (i + (int)GetStartAsSampleInSeg()) * HzPerSample;
                        }
                    }*/
                }
            }
        }

        

        public Peak getPulsePeak()
        {
            return (parentPulse);
        }

        /// <summary>
        /// Creates a record of the detected peak based on the sample start number and the width of
        /// the detected peak in samples
        /// </summary>
        /// <param name="peakStart"></param>
        /// <param name="peakCount"></param>
        public static SpectralPeak Create(int peakNumber, int peakStart, int peakCount, double peakArea, float maxHeight, int interval, int sampleRate, float autoCorrelationWidth,
            Peak parentPeak = null,bool isValidPulse=true, double startOffset = 0.0d, float[] data = null, int HzPerSample = 1, int PassNumber = 1, int RecordingNumber = 1, float AbsoluteThreshold = 0.0f)
        {

            //Debug.WriteLine($"Peak at {peakStart} for {peakCount} - {(float)peakStart / sampleRate}/{(float)peakCount / sampleRate}");
            SpectralPeak peak = new SpectralPeak(peakNumber, sampleRate, (int)(startOffset * sampleRate), peakStart, peakCount, peakArea, maxHeight, interval, ref data, HzPerSample,
                autoCorrelationWidth, parentPeak,isValidPulse, PassNumber, RecordingNumber, AbsoluteThreshold);

            return (peak);

        }


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
            _halfHeightHighFrequency = (int)((GetStartAsSampleInSeg() + highEdge) * HzPerSample);
            _halfHeightLowFrequency = (int)((GetStartAsSampleInSeg() + lowEdge) * HzPerSample);
            _halfHeightWidthHz = _halfHeightHighFrequency - _halfHeightLowFrequency;


        }

        internal int GetHzPerSample()
        {
            return (HzPerSample);
        }
    }
}
