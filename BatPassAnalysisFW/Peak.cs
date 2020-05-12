using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Class to record the parameters of a peak detected in the waveform or spectrum
    /// </summary>
    public class Peak
    {
        /// <summary>
        /// The ordinal number of the peak in the sequence
        /// </summary>
        public int pulse_Number { get; set; } = -1;

        /// <summary>
        /// The time of the start of the peak in secs from the beginning of the recording
        /// </summary>
        //private float start_As_Secs_In_Rec 
        //{
        //    get
        //    {
        //        return (ConvertSamplesToSecs(startAsSampleInSegment));
        //    }
        //}

        /// <summary>
        /// time from the start of the recording tot he start of the peak
        /// </summary>
        private float start_As_Secs_In_Seg
        {
            get
            {
                return (ConvertSamplesToSecs(startAsSampleInSegment));
            }
        }

        /// <summary>
        /// The time of the end of the peak in secs from the start of the recording
        /// </summary>
        //private float endAsSecsInRec 
        //{
        //    get
        //    {
        //        return (ConvertSamplesToSecs(endAsSampleInSegment));
        //    }
        //}

        internal float GetMaxVal()
        {
            return (maxVal);
        }

        /// <summary>
        /// time of the end of the peak from the start of the segment in seconds
        /// </summary>
        private float endAsSecsInSeg
        {
            get
            {
                return (ConvertSamplesToSecs(endAsSampleInSegment));
            }
        }

        /// <summary>
        /// The width of the peak in secs = endSecs-startSecs
        /// </summary>
        public int peakWidthMs
        {
            get
            {
                return ((int)(ConvertSamplesToSecs(endAsSampleInPass - startAsSampleInPass)*1000));
            }
        }

        private int peakWidthSamples
        {
            get
            {
                return (int)(endAsSampleInPass - startAsSampleInPass);
            }
        }

       

        /// <summary>
        /// Number of samples from the start of the segmen tot he start of the peak
        /// </summary>
        private int startAsSampleInPass { get; set; }

        /// <summary>
        /// number of samples from the start of the segment to the end of the peak
        /// </summary>
        private int endAsSampleInPass { get; set; }

        /// <summary>
        /// number of samples from the start of the recording to the start of the peak
        /// </summary>
        private int startAsSampleInSegment { get; set; }

        /// <summary>
        /// number of samples from the start of the recording to the end of the peak
        /// </summary>
        private int endAsSampleInSegment { get; set; }

        /// <summary>
        /// time in secs from the end of the previous peak to he start of this peak
        /// or zero if there was no previous peak
        /// </summary>
        public int prevIntervalMs 
        {
            get
            {
                return ((int)(ConvertSamplesToSecs(prevIntervalSamples)*1000));
            } 
        }

        /// <summary>
        /// number of samples from the start of the previous peak tot he start of this peak or zero if
        /// there was no previous peak
        /// </summary>
        private int prevIntervalSamples { get; set; }

        /// <summary>
        /// the maximum value of the envelope for this peak
        /// </summary>
        private float maxVal { get; set; }

        /// <summary>
        /// the sample rate of the recording in samples per second
        /// </summary>
        private int sampleRatePerSecond { get; set; }

        /// <summary>
        /// Area of the peak - sum of the values within peakStart-peakEnd
        /// </summary>
        private double peakArea { get; set; }

        internal double GetPeakArea()
        {
            return (peakArea);
        }


        public int recordingNumber { get; set; }

        public float AbsoluteThreshold { get; set; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rate">
        /// sample rate in samples per second</param>
        /// <param name="startOfPassInSegment">
        /// offset in samples to the from the start of the recording tot he start of the segment</param>
        /// <param name="startOfPeakInPass">
        /// offset in samples from the start of the segment tot he start of the peak</param>
        /// <param name="peakWidth">
        /// width of the peak in samples</param>
        /// <param name="peakMaxHeight">
        /// maximum height of the peak envelope</param>
        /// <param name="interval">
        /// interval between the start of this peak and the end of the previous peak or zer0 if no
        /// previous peak </param>
        public Peak(int peakNumber, int rate, int startOfPassInSegment, int startOfPeakInPass, int peakWidth,double peakArea,float peakMaxHeight,int interval,int RecordingNumber=1,float AbsoluteThreshold=0.0f)
        {
            this.pulse_Number = peakNumber;
            sampleRatePerSecond = rate;
            startAsSampleInPass = startOfPeakInPass;
            endAsSampleInPass = startOfPeakInPass + peakWidth;
            startAsSampleInSegment = startOfPassInSegment + startOfPeakInPass;
            endAsSampleInSegment = startOfPassInSegment + startOfPeakInPass + peakWidth;
            prevIntervalSamples = interval;
            maxVal = peakMaxHeight;
            this.peakArea = peakArea;
            this.recordingNumber = RecordingNumber;
            this.AbsoluteThreshold = AbsoluteThreshold;
        }

        internal int GetStartAsSampleInSeg()
        {
            return (startAsSampleInSegment);
        }

        internal int getPeakWidthSamples()
        {
            return (peakWidthSamples);
        }

        private float ConvertSamplesToSecs(long samples)
        {
            float result = 0.0f;
            if (samples > 0)
            {
                result=(float)samples / (float)sampleRatePerSecond;
            }
            return (result);
        }

        internal int GetSampleRatePerSecond()
        {
            return ((int)sampleRatePerSecond);
        }

        /// <summary>
        /// Creates a record of the detected peak based on the sample start number and the width of
        /// the detected peak in samples
        /// </summary>
        /// <param name="peakStart"></param>
        /// <param name="peakCount"></param>
        public static Peak Create(int peakNumber, int peakStart, int peakCount, double peakArea, float maxHeight, int interval, int sampleRate, 
            int startOfPassInSegment = 0,int RecordingNumber=1,float AbsoluteThreshold=0.0f)
        {

            //Debug.WriteLine($"Peak at {peakStart} for {peakCount} - {(float)peakStart / sampleRate}/{(float)peakCount / sampleRate}");
            Peak peak = new Peak(peakNumber, sampleRate, startOfPassInSegment, peakStart, peakCount, peakArea, maxHeight, interval,RecordingNumber,AbsoluteThreshold);
            return (peak);

        }

        //internal float GetStart_As_Secs_In_Rec()
        //{
        //    return (start_As_Secs_In_Rec);
        //}

        //internal float GetEndAsSecsInRec()
        //{
        //    return (endAsSecsInRec);
        //}
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///class SpectralPeak, derived from Peak but with added spectarl measureents
    ///
    public class SpectralPeak : Peak
    {
        private float[] data;

        public int parentPulseIndex = -1;

        public int Pass_ { get; set; } = 1;

        public int Pulse_ { get; set; } = 1;

        public int Pulse_Length_ms { get; set; } = 0;

        public int Pulse_Interval_ms { get; set; } = 0;

        public float AbsoluteThreshold { get; set; } = 0.0f;

        

        private int _peakFrequency = -1;
        /// <summary>
        /// the frequency of the highest value in the spectrum
        /// </summary>
        public int peakFrequency
        {
            get
            {
                if (data == null || data.Length <= 0)
                {
                    _peakFrequency = -1;

                }
                else
                {
                    if (_peakFrequency < 0)
                    {
                        double max = double.MinValue;
                        
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (data[i] > max)
                            {
                                max = data[i];
                                _peakFrequency = (i+(int)GetStartAsSampleInSeg()) * HzPerSample;
                            }
                        }
                    }
                }
                return (_peakFrequency);
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
                    _highFrequency = (int)((GetStartAsSampleInSeg()+getPeakWidthSamples()) * HzPerSample);
                }
                return (_highFrequency);
            }
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
                if (_halfHeightWidthHz < 0)
                {
                    halfHeightValue();
                }
                
                return (_halfHeightWidthHz);
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
                if (_halfHeightLowFrequency < 0)
                {
                    halfHeightValue();
                }
                return (_halfHeightLowFrequency);
            }
        }

        /// <summary>
        /// The high frequency bound of the peak at half the maximum amplitude
        /// May call halfHeightWidthHz to initialise the values
        /// </summary>
        public int halfHeightHighFrequency
        {
            get
            {
                if (_halfHeightHighFrequency < 0)
                {
                    halfHeightValue();
                }
                return (_halfHeightHighFrequency);
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
            get;set;
        }

        private int sampleRate { get; set; }

        public int recordingNumber { get; set; }

        private float[] autoCorrelation;

        private Peak parentPulse { get; set; }
        /// <summary>
        /// Constructor for spectralPeak initialises all Peak parameters but also retains all the raw data for the peak
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
        public SpectralPeak(int peakNumber, int rate, int segmentStart, int peakStart, int peakWidth, double peakArea, float peakMaxHeight, int interval, float[] data,
            int HzPerSample, ref float[] autoCorrelation, Peak parentPulse=null,int PassNumber=1,int RecordingNumber=1,float AbsoluteThreshold=0.0f):
            base(peakNumber,rate,segmentStart,peakStart,peakWidth,peakArea,peakMaxHeight,interval,AbsoluteThreshold:AbsoluteThreshold)
        {
            this.data = data;
            this.HzPerSample = HzPerSample;
            sampleRate = rate;
            halfHeightValue();
            if (parentPulse != null)
            {
                this.parentPulse = parentPulse;
                Pass_ = PassNumber;
                Pulse_ = parentPulse.pulse_Number;
                Pulse_Length_ms = parentPulse.peakWidthMs;
                Pulse_Interval_ms = parentPulse.prevIntervalMs;
                
            }
            this.recordingNumber = RecordingNumber;
            this.autoCorrelation = autoCorrelation;
            this.AbsoluteThreshold = AbsoluteThreshold;

            AutoCorrelationWidth = getAutoCorrelationWidth();
        }

        private float getAutoCorrelationWidth()
        {
            if(autoCorrelation==null || autoCorrelation.Length <= 0) { return (0.0f); }
            float max = autoCorrelation.Max();
            float half = max / 2;
            for(int i = 0; i < autoCorrelation.Length; i++)
            {
                if (autoCorrelation[i] < half)
                {
                    float secs = (float)i / (float)sampleRate;
                    return (secs * 1000);
                }
            }
            return (0.0f);
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
        public static SpectralPeak Create(int peakNumber, int peakStart, int peakCount, double peakArea, float maxHeight, int interval, int sampleRate, ref float[] autoCorrelation,
            Peak parentPulse=null, double startOffset = 0.0d, float[] data=null,int HzPerSample=1,int PassNumber=1,int RecordingNumber=1,float AbsoluteThreshold=0.0f)
        {

            //Debug.WriteLine($"Peak at {peakStart} for {peakCount} - {(float)peakStart / sampleRate}/{(float)peakCount / sampleRate}");
            SpectralPeak peak = new SpectralPeak(peakNumber, sampleRate, (int)(startOffset * sampleRate), peakStart, peakCount, peakArea, maxHeight, interval, data,HzPerSample, 
                ref autoCorrelation, parentPulse,PassNumber,RecordingNumber,AbsoluteThreshold);
            
            return (peak);

        }

        public float[] getData()
        {
            return (data);
        }

        private void halfHeightValue()
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
            _halfHeightHighFrequency = (int)((GetStartAsSampleInSeg()+ highEdge)  * HzPerSample);
            _halfHeightLowFrequency = (int)((GetStartAsSampleInSeg() + lowEdge) * HzPerSample);
            _halfHeightWidthHz = _halfHeightHighFrequency - _halfHeightLowFrequency;


        }

        internal int GetHzPerSample()
        {
            return (HzPerSample);
        }
    }

}
