using DspSharp.Utilities.Collections;
using LinqStatistics;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

/* Notes
 * 
 * As at 16:50 on April 1 2020 there is a tendency to include some echoes in the pulse width making the duration estimate overlong.
 * There is typically (in this recording) a 3ms gap between pulse and echo so the leadout period should set to about 1ms while making sure that
 * the interval excludes the treatment of the echo as a a separate pulse.
 * The pulse interval is better given as start to start as the starts are better defined than the ends if some echo gets included.
 * */
namespace BatPassAnalysisFW
{
    /// <summary>
    /// PassAnalysis class handles the detailed analysis of a pre-defined bat pass.
    /// It is pre-loaded with a filename and segment start and end parameters, and
    /// optionally with start and end parameters for a 'quiet segment' which will be used for
    /// the removal of background noise from the segment to be processed.
    /// The class may also be provided with information about the bat species present or with
    /// optimal filter settings to isolate the bat calls.
    /// Eventually it should be able to provide data on the number of calls, the duration of
    /// each (with statistical summary) the maximum, minimum and peak frequencies and data on
    /// pulse intervals.
    /// </summary>
    public class PassAnalysis
    {
        /// <summary>
        /// the name of the file to be processed
        /// </summary>







        public enum peakState { NOTINPEAK, INPEAKLEADIN, INPEAK, INPEAKLEADOUT };

        /// <summary>
        /// scans a data array looking for peaks that are above tha background noise level, and for each peak creates
        /// a Peak instance or a SpectralPeak instance depending on the value of asSpectralPeak
        /// </summary>
        /// <param name="dataInPass"></param>
        /// <param name="sampleRate"></param>
        /// <param name="leadInSamples"></param>
        /// <param name="leadOutSamples"></param>
        /// <param name="thresholdFactor"></param>
        /// <param name="peakList"></param>
        /// <param name="autoCorrelationWidth"></param>
        /// <param name="startOfPassInSegment"></param>
        /// <param name="asSpectralPeak"></param>
        /// <param name="parentPulse"></param>
        /// <param name="PassNumber"></param>
        /// <param name="RecordingNumber"></param>
        /// <returns></returns>
        public static string getPeaks(ref float[] dataInPass, int sampleRate, int leadInSamples, int leadOutSamples, float thresholdFactor, out ObservableList<Peak> peakList, float autoCorrelationWidth,
            int startOfPassInSegment = 0, bool asSpectralPeak = false, Peak parentPulse = null, bool isValidPulse = true, int PassNumber = 1, int RecordingNumber = 1)
        {
            peakList = new ObservableList<Peak>();
            peakState currentPeakState = peakState.NOTINPEAK;
            float limit = dataInPass.Average() * thresholdFactor;
            float maxValue = float.MinValue;
            float maxmaxvalue = float.MinValue;

            Dictionary<int, float> limitList = new Dictionary<int, float>();
            int LimitSegmentSize = dataInPass.Length;
            if (!asSpectralPeak && dataInPass.Length > sampleRate * 2)
            {
                LimitSegmentSize = sampleRate / 2;
                Debug.WriteLine($"Default limit={limit}");
            }

            //limit = dataInPass.Average()*thresholdFactor;

            //Debug.WriteLine($"GetPeaks (asSpectrum={asSpectralPeak}) data Average={dataInPass.Average()} Limit={limit} Factor={thresholdFactor} Leadin={leadInSamples} LeadOut={leadOutSamples}");
            //Debug.WriteLine($"Threshold={limit}");
            int lastStart = 0;
            int peakNumber = 0;
            int leadInCount = 0;
            int leadinLimit = leadInSamples;// (int)(sampleRate*0.0002f);//0.2ms minimum duration
            int leadoutLimit = leadOutSamples;// (int)(sampleRate*0.001f);//5ms silence between peaks
            int leadOutCount = 0;
            int peakCount = 0;
            int peakStartInPass = 0;
            int peakEndInPass = 0;
            currentPeakState = peakState.NOTINPEAK;
            double peakArea = 0.0d;
            double leadArea = 0.0d;
            int start15kHz = 0;

            int leadinstartsat = 0;
            int peakstartsat = 0;
            int leadoutstartsat = 0;
            int outofpeakstartsat = 0;
            int peakrestartat = 0;

            if (asSpectralPeak)
            {
                start15kHz = (int)(15000.0f / ((sampleRate / 2.0f) / dataInPass.Length)); // if a spectrum ignore the first 15kHz
            }
            while (asSpectralPeak && start15kHz < dataInPass.Length && dataInPass[start15kHz] > limit) start15kHz++;
            for (int sampleInPass = asSpectralPeak ? start15kHz : 0; sampleInPass < dataInPass.Length; sampleInPass++)
            {
                if (!asSpectralPeak && sampleInPass % LimitSegmentSize == 0)
                {
                    int takeTo = sampleInPass + LimitSegmentSize;
                    int start;

                    if (takeTo < dataInPass.Length)
                    {
                        start = sampleInPass;
                        //limit = dataInPass.Skip(sampleInPass).Take(LimitSegmentSize).Average() * thresholdFactor;
                    }
                    else
                    {
                        start = dataInPass.Length - LimitSegmentSize;
                        //limit = dataInPass.Skip(dataInPass.Length - LimitSegmentSize).Take(LimitSegmentSize).Average() * thresholdFactor;
                    }
                    var chunk = dataInPass.Skip(start).Take(LimitSegmentSize);
                    var avg = chunk.Average();
                    var sd = chunk.StandardDeviation();
                    var max = chunk.Max();
                    var allowed = avg + sd * 10.0f;
                    if (max < allowed)
                    {
                        sampleInPass += LimitSegmentSize - 1;
                        continue;
                    }
                    else
                    {
                        limit = avg * thresholdFactor;
                    }
                    Debug.WriteLine($"\tseg limit={limit}");
                }

                maxValue = maxValue > dataInPass[sampleInPass] ? maxValue : dataInPass[sampleInPass];
                switch (currentPeakState)
                {
                    case peakState.NOTINPEAK:
                        peakArea = 0.0d;
                        leadArea = 0.0d;
                        if (dataInPass[sampleInPass] > limit)
                        {
                            leadInCount = 1;
                            peakStartInPass = sampleInPass;
                            currentPeakState = peakState.INPEAKLEADIN;
                            leadinstartsat = sampleInPass;
                            maxValue = float.MinValue;
                            leadArea = dataInPass[sampleInPass];
                        }
                        break;
                    case peakState.INPEAKLEADIN:
                        if (dataInPass[sampleInPass] > limit)
                        {
                            maxValue = dataInPass[sampleInPass] > maxValue ? dataInPass[sampleInPass] : maxValue;
                            leadInCount++;
                            leadArea += dataInPass[sampleInPass];
                            if (leadInCount > leadinLimit)
                            {
                                peakStartInPass = sampleInPass - leadinLimit;
                                if (peakStartInPass < 0) peakStartInPass = 0;
                                currentPeakState = peakState.INPEAK;
                                peakstartsat = sampleInPass;
                                if (asSpectralPeak)
                                {
                                    //Debug.WriteLine($"$$$$$ in peak at {sampleInPass}");
                                }
                                peakCount = leadInCount;
                                peakArea = leadArea;
                            }
                        }
                        else
                        {
                            currentPeakState = peakState.NOTINPEAK;
                            outofpeakstartsat = sampleInPass;
                            //Debug.WriteLine($"lead in failed with count={leadInCount}");
                        }
                        break;
                    case peakState.INPEAK:
                        if (dataInPass[sampleInPass] > limit)
                        {
                            maxValue = dataInPass[sampleInPass] > maxValue ? dataInPass[sampleInPass] : maxValue;
                            peakCount++;
                            peakArea += dataInPass[sampleInPass];

                            //Debug.Write($"\r{peakCount} for {sampleEnvelope[s]}>{limit}");
                        }
                        else
                        {
                            leadOutCount = 1;
                            currentPeakState = peakState.INPEAKLEADOUT;
                            leadoutstartsat = sampleInPass;
                            leadArea = dataInPass[sampleInPass];
                        }
                        break;
                    case peakState.INPEAKLEADOUT:
                        //Debug.WriteLine("\nleadout");
                        if (dataInPass[sampleInPass] > limit)
                        {
                            peakCount += leadOutCount;
                            currentPeakState = peakState.INPEAK;
                            peakrestartat = sampleInPass;
                            peakArea += leadArea;
                        }
                        else
                        {
                            leadOutCount++;
                            leadArea += dataInPass[sampleInPass];
                            if (leadOutCount > leadoutLimit)
                            {
                                currentPeakState = peakState.NOTINPEAK;
                                outofpeakstartsat = sampleInPass;
                                peakEndInPass = sampleInPass - leadoutLimit;
                                if (peakEndInPass < 0) peakEndInPass = 0;
                                if (peakEndInPass > dataInPass.Length) peakEndInPass = dataInPass.Length - 1;
                                int interval = lastStart > 0 ? peakStartInPass - lastStart : 0;
                                if (asSpectralPeak || ((interval == 0 || interval > (sampleRate * 0.01))))// sampleRate/50 == 30ms min separation
                                {
                                    if (asSpectralPeak)
                                    {
                                        int HzPerSample = (sampleRate / 2) / dataInPass.Length;
                                        SpectralPeak peak = SpectralPeak.Create(
                                            ++peakNumber,
                                            peakStartInPass,
                                            peakCount,
                                            peakArea,
                                            maxValue,
                                            lastStart > 0 ? peakStartInPass - lastStart : 0,
                                            sampleRate,
                                            autoCorrelationWidth,
                                            parentPulse,
                                            isValidPulse,
                                            startOfPassInSegment,
                                            dataInPass.Skip(peakStartInPass).Take(peakCount).ToArray<float>(),
                                            HzPerSample,
                                            PassNumber,
                                            RecordingNumber,
                                            limit

                                            );
                                        peakList.Add(peak);
                                        lastStart = peakStartInPass;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"PEAKLEADIN at {leadinstartsat} INPEAK at {peakstartsat} LEADOUT at {leadoutstartsat} INPEAK at {peakrestartat} OUTOFPEAK at {outofpeakstartsat}-{leadoutLimit}");
                                        //float[] peakData = dataInPass.Skip(peakStartInPass).Take(peakCount).ToArray<float>();
                                        float peakStartSecs = peakStartInPass / (float)sampleRate;
                                        float peakEndSecs = peakEndInPass / (float)sampleRate;
                                        float width = (peakEndSecs - peakStartSecs) * 1000.0f;
                                        float passStartSecs = startOfPassInSegment / (float)sampleRate;
                                        maxmaxvalue = maxValue > maxmaxvalue ? maxValue : maxmaxvalue;
                                        Debug.WriteLine($"start at {peakStartInPass} end at {peakEndInPass} width= {peakEndInPass - peakStartInPass}");
                                        //if (width>0.0f && width < 30.0f)
                                        //{
                                        Peak peak = Peak.Create(++peakNumber, peakStartInPass, peakEndInPass - peakStartInPass, peakArea, maxValue, lastStart > 0 ? peakStartInPass - lastStart : 0,
                                            sampleRate, startOfPassInSegment, RecordingNumber: RecordingNumber, AbsoluteThreshold: limit);

                                        peakList.Add(peak);
                                        lastStart = peakStartInPass;
                                        //}
                                    }

                                }
                                maxValue = float.MinValue;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
            if (!asSpectralPeak && maxmaxvalue < (limit * 2))
            {
                peakList.Clear();
            }
            return ($"found {peakList.Count} peaks");
        }



        /// <summary>
        /// returns an envelope graph for the selected pass
        /// </summary>
        /// <param name="data"></param>
        /// <param name="peakList"></param>
        /// <param name="Factor"></param>
        /// <returns></returns>
        public static Bitmap GetGraph(ref float[] data, ref ObservableList<Peak> peakList, double Factor)
        {


            if (data == null || data.Length <= 1900) return (null);
            List<float> shortData = new List<float>();
            int blocksize = data.Length / 1900;



            if (data.Length > blocksize && blocksize > 1)
            {
                for (int s = 0; s < data.Length - blocksize; s += blocksize)
                {
                    var segment = data.Skip(s).Take(blocksize).Average();
                    shortData.Add(segment);

                }
            }
            else
            {
                foreach (var val in data) shortData.Add(val);
            }

            Bitmap bmp = GetBitmap(ref shortData, ref peakList, Factor, blocksize);
            return (bmp);
        }

        /// <summary>
        /// Returns a bitmap of a graph of the supplied data with the width of the size of the data array
        /// </summary>
        /// <param name="shortData">The data to be graphed</param>
        /// <param name="peakList">optional list of Peak of the detected peaks in the graph</param>
        /// <param name="Factor">ratio of pass width in samples to the size of the smoothed datablock plotted</param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        public static Bitmap GetBitmap(ref List<float> shortData, ref ObservableList<Peak> peakList, double Factor = 1.0, int blockSize = 1)

        {
            shortData = (from d in shortData select (float)Math.Abs(d)).ToList<float>();

            int widthFactor = 1;
            int dataSize = shortData.Count();
            while (dataSize * widthFactor < 1500) widthFactor++;
            int imageHeight = (int)(0.56f * dataSize * widthFactor);
            var bmp = new Bitmap(dataSize * widthFactor, imageHeight, PixelFormat.Format32bppArgb);
            //var bmp = new Bitmap(dataSize , imageHeight, PixelFormat.Format32bppArgb);


            Debug.WriteLine($"Image is {dataSize * widthFactor}x{imageHeight}");
            float AbsoluteThreshold = 0.0f;
            int sampleRate = 0;
            int HzPerSample = 0;
            if (peakList != null && peakList.Any())
            {
                if (peakList.First() is SpectralPeak sp)
                {
                    sampleRate = 0;
                    HzPerSample = sp.GetHzPerSample();
                    AbsoluteThreshold = sp.AbsoluteThreshold;

                }
                else
                {
                    sampleRate = peakList.First().GetSampleRatePerSecond();
                    HzPerSample = 0;
                    AbsoluteThreshold = peakList.First().AbsoluteThreshold;
                }
            }

            Debug.WriteLine($"AbsThreshold={AbsoluteThreshold}");

            int dataOffset = 50;
            if (peakList == null || peakList.Count() <= 0) dataOffset = 0;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                //int[] normalData = new float[data.Length];
                int plotHeight = imageHeight - dataOffset;
                var max = shortData.Max();
                var min = shortData.Min();
                var range = max;
                var mean = shortData.Average();
                var scaleFactor = plotHeight / max;
                Debug.WriteLine($"ScaleFactor={scaleFactor} Min={min}, Range={range}");
                var scaledData = shortData.Select(val => (int)(val * scaleFactor)).ToList();
                //draw the graph of the data
                //Debug.WriteLine($"shortData:- Max={max}, Min={min}, Range={range} peak value={(int)(((max - min) / range) * (height - 1))}, mean={mean}");
                int i = 0;
                Pen blackPen = new Pen(new SolidBrush(Color.Black));
                Pen redPen = new Pen(new SolidBrush(Color.Red));
                var first = scaledData[0];
                int scaledThreshold = (int)((AbsoluteThreshold) * scaleFactor);
                Point last = new Point(0, plotHeight - first);
                Debug.WriteLine($"ScaledThreshold={scaledThreshold} ImageHt={imageHeight} plotHt={plotHeight} offset={dataOffset}");

                for (int k = 1; k < scaledData.Count(); k++)
                {
                    var val = scaledData[k];
                    Pen pen = blackPen;
                    if (val > scaledThreshold)
                    {
                        pen = redPen;
                    }
                    else
                    {
                        pen = blackPen;
                    }

                    Point pt = new Point(i += widthFactor, plotHeight - (val));

                    g.DrawLine(pen, last, pt);
                    last = pt;
                }

                //draw the baseline and threshold line
                //float threshold = mean*(float)Factor;
                //int ptMean= (int)((mean ) *scaleFactor);
                //int ptThresold = (int)(((threshold - min) / range) * (height - 1));
                //int ptThreshold = (int)(ptMean * Factor);
                //int baseline = imageHeight - (ptMean+dataOffset );
                //Debug.WriteLine($"mean={mean} threshold={threshold} for factor {Factor} giving pos {ptMean}");
                //g.DrawLine(redPen, new Point(0, baseline), new Point((dataSize*widthFactor)-1, baseline));


                g.DrawLine(new Pen(new SolidBrush(Color.Green)), new Point(0, plotHeight - scaledThreshold), new Point((dataSize * widthFactor) - 1, plotHeight - scaledThreshold));
                Debug.WriteLine($"Draw threshold at {plotHeight - scaledThreshold}");



                if (sampleRate == 0)
                {// assume we are plotting a spectrum and include a frequency scale
                    int fsdHz = shortData.Count * HzPerSample;
                    for (int j = 0, k = 0; j < fsdHz; j += 1000, k++)
                    {
                        int ht = 10;
                        if (k % 10 == 0) ht = 20;
                        int xpos = widthFactor * (int)(j / (float)HzPerSample);
                        g.DrawLine(blackPen, new Point(xpos, plotHeight + 1), new Point(xpos, plotHeight + ht + 1));
                    }
                }
                else
                {// assume we are plotting envelopes and have a time scale
                    int ms = sampleRate / 1000;
                    int fullSize = shortData.Count;
                    Debug.WriteLine($"Bitmap:- SR={sampleRate} step={ms} size={fullSize} ");

                    for (int j = 0, k = 0; j < fullSize; j += ms, k++)
                    {
                        int xpos = j;
                        int ht = 5;
                        if ((k % 10) == 0) ht = 10;
                        if ((k % 100) == 0) ht = 15;
                        if ((k % 1000) == 0) ht = 20;
                        g.DrawLine(blackPen, new Point(xpos, plotHeight + 1), new Point(xpos, plotHeight + ht + 1));
                    }


                }

                if (peakList != null && peakList.Count > 0)
                {
                    foreach (var peak in peakList)
                    {
                        //Debug.WriteLine($"{peak.pulse_Number} at {peak.GetStartAsSampleInSeg() / blocksize},{height/2} to {(int)((peak.GetStartAsSampleInSeg() + peak.getPeakWidthSamples()) / blocksize)}" +
                        //   $"mean={mean} ptmean={ptMean} line at {ptMean+dataOffset}");
                        g.DrawString(peak.peak_Number.ToString(), new Font(FontFamily.GenericMonospace, 8), new SolidBrush(Color.Blue), new PointF((peak.getStartAsSampleInPass() / blockSize) / widthFactor, dataOffset / 2));



                        Rectangle rect = new Rectangle();
                        rect.Width = (int)((((peak.getPeakWidthSamples() * Factor) / blockSize) / widthFactor)) + 10;
                        rect.Height = 4;
                        rect.X = (int)(((((peak.getStartAsSampleInPass() * Factor) / blockSize) / widthFactor))) - 5;
                        if (rect.X < 0) rect.X = 0;
                        rect.Y = plotHeight / 2;
                        float scaledThresholdLine = peak.AbsoluteThreshold * scaleFactor;
                        var p = (int)(scaledThresholdLine);
                        rect.Y = plotHeight - (p);

                        g.DrawRectangle(new Pen(new SolidBrush(Color.Red)), rect);
                        Debug.WriteLine($"At peak {peak.peak_Number} Threshold={peak.AbsoluteThreshold}={scaledThreshold} plotted at {dataOffset}+{p}={dataOffset + p} width={peak.peakWidthMs:#0.##}");
                        Debug.WriteLine($"start={peak.getStartAsSampleInPass()},smooth=20,blockSize={blockSize},widthFactor={widthFactor}");
                        Debug.WriteLine($"x={rect.X} y={rect.Y} w={rect.Width} h={rect.Height}");
                    }
                }
                Debug.WriteLine($"Draw zero-line and scale at imageheight-{dataOffset}={plotHeight}");
                g.DrawLine(new Pen(new SolidBrush(Color.LightGray)), new Point(0, plotHeight + 1), new Point((dataSize * widthFactor) - 1, plotHeight + 1));
            }
            return (bmp);

        }

        /*
        /// <summary>
        /// Provides arguments for an event.
        /// </summary>
        [Serializable]
        public class AnalysisResultEventArgs : EventArgs
        {
            public new static readonly AnalysisResultEventArgs Empty = new AnalysisResultEventArgs("");

            #region Public Properties
            /// <summary>
            /// The text containing statistical details of the analysis so far
            /// to be inserted as is into a TextBox.text field
            /// </summary>
            public string text = "";
            #endregion

            #region Private / Protected
            #endregion

            #region Constructors
            /// <summary>
            /// Constructs a new instance of the <see cref="CustomEventArgs" /> class.
            /// </summary>
            public AnalysisResultEventArgs(string text)
            {
                this.text = text;
            }
            #endregion
        }

        public event EventHandler<AnalysisResultEventArgs> ResultProduced;

        protected virtual void OnResultProduced(AnalysisResultEventArgs e) => ResultProduced?.Invoke(this, e);
        */
    }

}
