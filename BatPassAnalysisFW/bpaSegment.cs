using DspSharp.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// A class that represents a single segment of a recording and the results of the analysis of it.
    /// A segment may be an entire (short) file or <see langword="abstract"/>segment corresponding to a labelled portion
    /// of a recording as identified in a sidecar .txt file.
    /// A segment will be split into 'passes' where a pass is nominally 5 seconds long, but maybe less if the segment is
    /// less than 5s and may be up to 7.5s if it is the final segment in the pass.
    /// </summary>

    public class bpaSegment
    {

        /// <summary>
        /// segment number in the pass
        /// </summary>
        public int No
        {
            get
            {
                return (segNumber);
            }
        }

        /// <summary>
        /// formatted offset of the segment nto the recording mins:sescs:ms
        /// </summary>
        public String Start
        {
            get
            {
                double secs = (double)OffsetInRecordingInSamples / SampleRate;
                TimeSpan time = TimeSpan.FromSeconds(secs);
                
                return ($"{time.Minutes:G2}:{time.Seconds:G2}.{time.Milliseconds:G3}");

            }
        }

        /// <summary>
        /// formatted duration of the segment in secs
        /// </summary>
        public string Length
        {
            get
            {
                return ($"{duration.TotalSeconds:#0.0#}");
            }
        }

        /// <summary>
        /// Number of passes in PassList
        /// </summary>
        public int Number_Of_Passes
        {
            get
            {
                return (PassList.Count());
            }
        }

        /// <summary>
        /// sum of the pulses in the passes in PassList
        /// </summary>
        public int Number_Of_Pulses
        {
            get
            {
                int tot = 0;
                foreach (var pass in PassList)
                {
                    tot += pass.getPulseList().Count();
                }
                return (tot);
            }
        }

        /// <summary>
        /// Original analysers comments from metadata or .txt file
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// The start location of the segment in the recording in samples
        /// </summary>
        private int OffsetInRecordingInSamples { get; set; } = -1;

        /// <summary>
        /// The original sample rate of the recording and of this segment
        /// </summary>
        private int SampleRate { get; set; }

        /// <summary>
        /// The length of the segment in samples
        /// </summary>
        private int SegmentLengthInSamples { get; set; } = -1;

        private TimeSpan startTime { get; set; }

        private TimeSpan duration { get; set; }


        private DataAccessBlock segmentAccessBlock { get; set; }

        private ObservableList<bpaPass> PassList { get; set; } = new ObservableList<bpaPass>();

        private int recNumber;

        private int segNumber;

        

        public bpaSegment(int recNumber, int index, int offset, DataAccessBlock dab, int sampleRate,string comment)
        {
            OffsetInRecordingInSamples = offset;
            SegmentLengthInSamples = (int)dab.Length;
            SampleRate = sampleRate;
            segmentAccessBlock = dab;

            startTime = TimeSpan.FromSeconds((double)offset / (double)sampleRate);
            duration = TimeSpan.FromSeconds((double)SegmentLengthInSamples / (double)sampleRate);
            this.segNumber = index;
            this.recNumber = recNumber;
            Comment = comment;
            //CreatePasses();
        }

        public ObservableList<bpaPass> getPassList()
        {
            return (PassList);
        }

        public void AddPass(bpaPass pass)
        {
            PassList.Add(pass);
        }

        public bool CreatePasses(decimal thresholdFactor,decimal spectrumFactor)
        {
            bool result = false;
            PassList.Clear();
            int index = 1;
            if (duration.TotalSeconds < 7.5d)
            {
                //data = new float[SegmentLengthInSamples];
                // pass the entire segment

                bpaPass pass = new bpaPass(recNumber, segNumber, index++, 0, segmentAccessBlock, SampleRate,Comment,(float)SegmentLengthInSamples/(float)SampleRate);
                PassList.Add(pass);
            }
            else
            {
                int startOfPassInSegment = 0;
                int blockSize = 5 * SampleRate; // 5 seconds worth of samples
                int extendedBlockSize = (int)(7.5f * SampleRate);
                int remainingLength = SegmentLengthInSamples;
                while (remainingLength > extendedBlockSize)
                {
                    DataAccessBlock dab = new DataAccessBlock(segmentAccessBlock.FQfileName, segmentAccessBlock.BlockStartInFileInSamples + startOfPassInSegment, blockSize);
                    bpaPass pass = new bpaPass(recNumber, segNumber, index++, startOfPassInSegment, dab, SampleRate,Comment, (float)SegmentLengthInSamples / (float)SampleRate);
                    PassList.Add(pass);
                    startOfPassInSegment += blockSize;
                    remainingLength -= blockSize;
                }
                if (remainingLength > 0)
                {
                    DataAccessBlock dab = new DataAccessBlock(segmentAccessBlock.FQfileName, segmentAccessBlock.BlockStartInFileInSamples + startOfPassInSegment, remainingLength);
                    bpaPass pass = new bpaPass(recNumber, segNumber, index++, startOfPassInSegment, dab, SampleRate,Comment, (float)SegmentLengthInSamples / (float)SampleRate);
                    PassList.Add(pass);
                }
            }
            Debug.WriteLine($"Segment at {startTime} of length {Length} has {PassList.Count} passes");

            foreach (var pass in PassList)
            {
                pass.CreatePass(thresholdFactor,spectrumFactor);
            }


            return (result);
        }

        /// <summary>
        /// returns the length of the segment as a timespan
        /// </summary>
        /// <returns></returns>
        internal TimeSpan GetDuration()
        {

            return (duration);
        }

        /// <summary>
        /// returns the offset of the segment into the recording <see langword="async"/>a Timespan
        /// </summary>
        /// <returns></returns>
        internal TimeSpan GetOffsetInRecording()
        {
            double secs = (double)OffsetInRecordingInSamples / SampleRate;
            TimeSpan time = TimeSpan.FromSeconds(secs);
            return (time);
        }
    }
}
