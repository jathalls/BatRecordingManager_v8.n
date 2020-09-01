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
        public int peak_Number { get; set; } = -1;



        internal float GetMaxVal()
        {
            return (maxVal);
        }



        /// <summary>
        /// The width of the peak in secs = endSecs-startSecs
        /// </summary>
        public float peakWidthMs
        {
            get
            {
                return ((float)(ConvertSamplesToSecs(endAsSampleInPass - startAsSampleInPass) * 1000.0f));
            }
        }

        private int peakWidthSamples
        {
            get
            {
                return endAsSampleInPass - startAsSampleInPass;
            }
        }

        internal int startPosInPulse;



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
        public float prevIntervalMs
        {
            get
            {
                return ((float)(ConvertSamplesToSecs(prevIntervalSamples) * 1000.0f));
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
        public Peak(int peakNumber, int rate, int startOfPassInSegment, int startOfPeakInPass, int peakWidth, double peakArea, float peakMaxHeight, int interval, int RecordingNumber = 1, float AbsoluteThreshold = 0.0f)
        {
            this.peak_Number = peakNumber;
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

        internal int getStartAsSampleInPass()
        {
            return (startAsSampleInPass);
        }


        private float ConvertSamplesToSecs(long samples)
        {
            float result = 0.0f;
            if (samples > 0)
            {
                result = samples / (float)sampleRatePerSecond;
            }
            return (result);
        }

        internal int GetSampleRatePerSecond()
        {
            return sampleRatePerSecond;
        }

        /// <summary>
        /// Creates a record of the detected peak based on the sample start number and the width of
        /// the detected peak in samples
        /// </summary>
        /// <param name="peakStartInPass"></param>
        /// <param name="peakCount"></param>
        public static Peak Create(int peakNumber, int peakStartInPass, int peakCount, double peakArea, float maxHeight, int interval, int sampleRate,
            int startOfPassInSegment = 0, int RecordingNumber = 1, float AbsoluteThreshold = 0.0f)
        {

            //Debug.WriteLine($"Peak at {peakStartInPass} for {peakCount} - {(float)peakStartInPass / sampleRate}/{(float)peakCount / sampleRate}");
            Peak peak = new Peak(peakNumber, sampleRate, startOfPassInSegment, peakStartInPass, peakCount, peakArea, maxHeight, interval, RecordingNumber, AbsoluteThreshold);
            return (peak);

        }

        internal int? GetPrevIntervalSamples()
        {
            return (prevIntervalSamples);
        }

        internal void SetPrevIntervalSamples(int interval)
        {
            prevIntervalSamples = interval;
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



}
