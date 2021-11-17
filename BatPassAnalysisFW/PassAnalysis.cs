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
        /// Returns a bitmap of a graph of the supplied data with the width of the size of the data array
        /// </summary>
        /// <param name="shortData">The data to be graphed</param>
        /// <param name="peakList">optional list of Peak of the detected peaks in the graph</param>
        /// <param name="LengthFactor">ratio of pass width in samples to the size of the smoothed datablock plotted</param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        public static Bitmap GetBitmap(ref List<float> shortData, ref ObservableList<Peak> peakList, double PassLengthInSamples = 0.0d, int blockSize = 1, int HzPerSample = 0)

        { // short data of 1863 samples, 6 peaks, passLength=963740, blockSize=21
            // example numbers taken from a 2.04s pass at 384000sps
            //
            bool IsSpectralPlot = PassLengthInSamples == 0 || HzPerSample > 0;

            shortData = (from d in shortData select (float)Math.Abs(d)).ToList<float>(); //
            if (PassLengthInSamples <= 0.0d) PassLengthInSamples = shortData.Count;

            int widthFactor = 1;
            int dataSize = shortData.Count();
            while (dataSize * widthFactor < 1500) widthFactor++;
            int imageHeight = (int)(0.56f * dataSize * widthFactor);    // WidthFactor=1
            int imageWidth = dataSize * widthFactor;                    // image 1863 x 1043
            var bmp = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);
            //var bmp = new Bitmap(dataSize , imageHeight, PixelFormat.Format32bppArgb);

            Debug.WriteLine($"Image is {dataSize * widthFactor}x{imageHeight}");
            float AbsoluteThreshold = 0.0f;
            int sampleRate = 0;
            //int HzPerSample = 0;
            if (peakList != null && peakList.Any())
            {
                sampleRate = peakList.First().GetSampleRatePerSecond();
                //HzPerSample = 0;
                AbsoluteThreshold = peakList.First().AbsoluteThreshold;
            }

            Debug.WriteLine($"AbsThreshold={AbsoluteThreshold}");

            int dataOffset = 50;
            if (peakList == null || peakList.Count() <= 0) dataOffset = 0;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                //int[] normalData = new float[data.Length];
                int plotHeight = imageHeight - dataOffset;
                var max = shortData.Max(); //=.00054
                var min = shortData.Min();
                var range = max;    // range=.00054
                var mean = shortData.Average();
                var scaleFactor = plotHeight / max; // = 1837704
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

                double xScale = 1.0d;
                float scaleValue = 0.0f;
                if (IsSpectralPlot)
                {// assume we are plotting a spectrum and include a frequency scale
                    int fsdHz = shortData.Count * HzPerSample;
                    for (int hz = 0, k = 0; hz < fsdHz; hz += 1000, k++)
                    {
                        int ht = 10;
                        int xpos = widthFactor * (int)(hz / (float)HzPerSample);
                        if (k % 10 == 0)
                        {
                            ht = 20;
                            var offset = 9;
                            if (scaleValue >= 100) offset = 12;
                            var xloc = xpos - offset < 0 ? 0 : xpos - offset;
                            g.DrawString($"{scaleValue:##0}",
                                new Font(FontFamily.GenericMonospace, 8),
                                new SolidBrush(Color.Black),
                                new PointF(xloc, plotHeight + ht + 3));
                            scaleValue += 10.0f;
                        }

                        g.DrawLine(blackPen, new Point(xpos, plotHeight + 1), new Point(xpos, plotHeight + ht + 1));
                    }
                }
                else
                {// assume we are plotting envelopes and have a time scale
                    if (sampleRate <= 0) return (null);
                    int samplesPerMs = sampleRate / 1000;

                    xScale = imageWidth / PassLengthInSamples;
                    if (xScale <= 0) return (null);
                    int stepSize = 1;
                    var ms = (int)(samplesPerMs * xScale);

                    while (ms <= 0)
                    {
                        stepSize *= 10;
                        ms = (int)(samplesPerMs * stepSize * xScale);
                    }

                    int fullSize = shortData.Count;
                    Debug.WriteLine($"Bitmap:- SR={sampleRate} step={ms} size={fullSize} ");

                    for (int j = 0, k = 0; j < imageWidth; j += ms, k += stepSize)
                    {
                        int xpos = j;
                        int ht = 5;
                        if ((k % 10) == 0) ht = 10;
                        if ((k % 100) == 0)
                        {
                            var xloc = xpos - 12 < 0 ? 0 : xpos - 12;
                            ht = 15;
                            g.DrawString($"{scaleValue:0.0}",
                                new Font(FontFamily.GenericMonospace, 8),
                                new SolidBrush(Color.Black),
                                new PointF(xloc, plotHeight + ht + 3));
                            scaleValue += 0.1f;
                        }
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
                        int peakStartPos = (int)(peak.getStartAsSampleInPass() * xScale);

                        g.DrawString(peak.peak_Number.ToString(), new Font(FontFamily.GenericMonospace, 8), new SolidBrush(Color.Blue),
                            new PointF(peakStartPos, dataOffset / 2));

                        Rectangle rect = new Rectangle();
                        double rwidth = peak.getPeakWidthSamples() * xScale;
                        rect.Width = (int)Math.Ceiling(rwidth);
                        if (rect.Width < 4) rect.Width = 4;
                        rect.Height = 4;
                        rect.X = (int)(peakStartPos);// already scaled by xScale
                        if (rect.X < 0) rect.X = 0;
                        rect.Y = plotHeight + 1;
                        float scaledThresholdLine = peak.AbsoluteThreshold * scaleFactor;
                        var p = (int)(scaledThresholdLine);
                        //rect.Y = plotHeight - (p);

                        g.DrawRectangle(new Pen(new SolidBrush(Color.Red)), rect);
                        Debug.WriteLine("");
                        Debug.WriteLine($"Image {imageWidth} x {imageHeight}");
                        Debug.WriteLine($"Peak start at {peak.getStartAsSampleInPass()} in {PassLengthInSamples} of width {peak.getPeakWidthSamples()}");
                        Debug.WriteLine($"Rect is {rect.Width}x{rect.Height} at (x,y) {rect.X}, {rect.Y}");

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

        /// <summary>
        /// returns an envelope graph for the selected pass
        /// Length factor is a number less than 1, is the reduction in length from pass to envelope
        /// </summary>
        /// <param name="data"></param>
        /// <param name="peakList"></param>
        /// <param name="PasslengthInSamples"></param>
        /// <returns></returns>
        public static Bitmap GetGraph(ref float[] data, ref ObservableList<Peak> peakList, double PasslengthInSamples)
        {
            if (data == null || data.Length <= 1900)
            {
                Debug.WriteLine($"GetGraph: Too few data points to graph - data is {data.Length} is less than 1900");
                return (null);
            }
            List<float> shortData = new List<float>();
            int blocksize = (int)Math.Ceiling(data.Length / 1900.0d);

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

            Bitmap bmp = GetBitmap(ref shortData, ref peakList, PasslengthInSamples, blocksize);
            return (bmp);
        }

        /*
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
        public static string getPeaks(ref float[] dataInPass, int sampleRate, int leadInSamples, int leadOutSamples, float thresholdFactor,
            out ObservableList<Peak> peakList, float autoCorrelationWidth, out ObservableList<SpectralPeak> spectralPeakList,
            int startOfPassInSegment = 0, bool asSpectralPeak = false, Peak parentPulse = null, bool isValidPulse = true, int PassNumber = 1,
            int RecordingNumber = 1)
        {
            peakList = new ObservableList<Peak>();
            spectralPeakList = new ObservableList<SpectralPeak>();
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
                                int peakWidth = peakCount;
                                Peak parentPeak = parentPulse;
                                currentPeakState = peakState.NOTINPEAK;
                                outofpeakstartsat = sampleInPass;
                                peakEndInPass = sampleInPass - leadoutLimit;
                                if (peakEndInPass < 0) peakEndInPass = 0;
                                if (peakEndInPass > dataInPass.Length) peakEndInPass = dataInPass.Length - 1;
                                int interval = lastStart > 0 ? peakStartInPass - lastStart : 0;
                                if (((interval == 0 || interval > (sampleRate * 0.01))))// sampleRate/50 == 30ms min separation
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
        }*/ // GetPeaks

        /// <summary>
        /// Alternative version of getpeaks which uses slopes of the data rather than values
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sampleRate"></param>
        /// <param name="leadInSamples"></param>
        /// <param name="leadOutSamples"></param>
        /// <param name="thresholdFactor"></param>
        /// <param name="spectralPeakList"></param>
        /// <param name="autoCorrelationWidth"></param>
        /// <param name="startOfPassInSegment"></param>
        /// <param name="asSpectralPeak"></param>
        /// <param name="parentPeak"></param>
        /// <param name="isValidPulse"></param>
        /// <param name="PassNumber"></param>
        /// <param name="RecordingNumber"></param>
        internal static void getSpectralPeaks(ref float[] data, int sampleRate, int leadInSamples, int leadOutSamples, float thresholdFactor,
            out ObservableList<SpectralPeak> spectralPeakList, float autoCorrelationWidth, int startOfPassInSegment, bool asSpectralPeak,
            Peak parentPeak, bool isValidPulse, int PassNumber, int RecordingNumber)
        {
            spectralPeakList = new ObservableList<SpectralPeak>();

            float[] slope = new float[data.Length - 1];
            var HzPerBin = (sampleRate / 2) / data.Length;
            var binFor12kHz = 12000 / HzPerBin;
            for (int i = 0; i < binFor12kHz; i++) slope[i] = 0;
            List<(int Position, float initialValue, int NumOfValues, float avgValue, bool IsPositive)> slopeGroups = new List<(int, float, int, float, bool)>();
            bool LastSlopeIsPositive;
            if (data[binFor12kHz] > data[binFor12kHz - 1]) LastSlopeIsPositive = true;
            else LastSlopeIsPositive = false;
            int blockStart = binFor12kHz;
            int blockCount = 0;
            float startValue = 0.0f;
            float sumOfValues = 0.0f;
            for (int i = binFor12kHz; i < slope.Length; i++)
            {
                slope[i] = data[i] - data[i - 1];
                if (slope[i] > 0 && LastSlopeIsPositive) // continuing a started positive block
                {
                    blockCount++;
                    sumOfValues += slope[i];
                }
                else if (slope[i] > 0 && !LastSlopeIsPositive) // first positive slope of a block
                {
                    if (blockCount > 0) // end of a real negative block
                    {
                        slopeGroups.Add((Position: blockStart, initialValue: startValue, NumOfValues: blockCount, avgValue: sumOfValues / blockCount, IsPositive: false));
                    }
                    blockCount = 1;
                    blockStart = i;
                    sumOfValues = slope[i];
                    startValue = data[i];
                    LastSlopeIsPositive = true;
                }
                else if (slope[i] < 0 && !LastSlopeIsPositive) // continuing a negative slope block
                {
                    blockCount++;
                    sumOfValues += slope[i];
                }
                else if (slope[i] < 0 && LastSlopeIsPositive) // first negative slopeof a block
                {
                    if (blockCount > 0)
                    {
                        slopeGroups.Add((Position: blockStart, initialValue: startValue, NumOfValues: blockCount, sumOfValues / blockCount, IsPositive: true));
                    }
                    blockCount = 1;
                    blockStart = i;
                    sumOfValues = slope[i];
                    startValue = data[i];
                    LastSlopeIsPositive = false;
                }
                else
                {
                    if (blockCount > 0)
                    {
                        blockCount++;
                        sumOfValues += slope[i];
                    }
                }
            }

            var hfData = data.Skip(binFor12kHz);
            float valueAtPeak = hfData.Max();
            int PositionOfPeak = hfData.IndexOf(valueAtPeak) + binFor12kHz;

            var blocksBelowPeak = slopeGroups.Where(sg => sg.Position < PositionOfPeak);
            if (!blocksBelowPeak.Any()) blocksBelowPeak = null;
            var maxSlope = blocksBelowPeak?.Select(sg => sg.NumOfValues)?.Max();
            var positiveBlock = blocksBelowPeak?.Where(sg => sg.NumOfValues == (maxSlope ?? -1))?.Last();

            var blocksAbovePeak = slopeGroups.Where(sg => sg.Position > PositionOfPeak);
            if (!blocksAbovePeak.Any()) blocksAbovePeak = null;
            maxSlope = blocksAbovePeak?.Select(sg => sg.NumOfValues)?.Max();
            var negativeBlock = blocksAbovePeak?.Where(sg => sg.NumOfValues == (maxSlope ?? -1))?.First();

            if (positiveBlock != null && negativeBlock != null)
            {
                float maximum = hfData.Max();

                int fPeak = PositionOfPeak * HzPerBin;

                int fLow = getIntercept(positiveBlock, HzPerBin);
                int fHigh = getIntercept(negativeBlock, HzPerBin);

                int fHalfHeightLow = getHalfHeight(positiveBlock, HzPerBin, fLow, valueAtPeak);
                int fHalfHeightHigh = getHalfHeight(negativeBlock, HzPerBin, fHigh, valueAtPeak);
                int fHalfHeightWidth = fHalfHeightHigh - fHalfHeightLow;

                SpectralPeak sp = (SpectralPeak)SpectralPeak.CreateSP(fLow, fHigh, (fPeak * (fHigh - fLow)) / 2,
                    fPeak, sampleRate, 0.01f, parentPeak, true, data, HzPerBin, autoCorrelationWidth);

                sp.halfHeightHighFrequency = fHalfHeightHigh;
                sp.halfHeightLowFrequency = fHalfHeightLow;
                sp.halfHeightWidthHz = fHalfHeightWidth;

                spectralPeakList.Add(sp);
            }
        }

        private static int getHalfHeight((int Position, float initialValue, int NumOfValues, float avgValue, bool IsPositive)? slopeBlock, int hzPerBin, int fIntercept, float valueAtPeak)
        {
            if (slopeBlock == null) return (0);
            int bin = 0;
            int interceptBin = fIntercept / hzPerBin;
            if (slopeBlock.Value.IsPositive)
            {
                var binAtHalfHeight = (int)((valueAtPeak / 2) / Math.Abs(slopeBlock.Value.avgValue));
                bin = interceptBin + binAtHalfHeight;
                if (bin < 0) bin = 0;
            }
            else
            {
                var binAtHalfHeight = (int)((valueAtPeak / 2) / Math.Abs(slopeBlock.Value.avgValue));
                bin = interceptBin - binAtHalfHeight;
                if (bin < 0) bin = 0;
            }
            int result = bin * hzPerBin;
            return (result);
        }

        /// <summary>
        /// given a block of slope values and a starting bin calculates the interception of the slope with zero and converts that
        /// bin number to a frequency
        /// </summary>
        /// <param name="slopeBlock"></param>
        /// <param name="positiveBlock"></param>
        /// <param name="hzPerBin"></param>
        /// <returns></returns>
        private static int getIntercept((int Position, float initialValue, int NumOfValues, float avgValue, bool IsPositive)? slopeBlock, int hzPerBin)
        {
            if (slopeBlock == null) return (0);
            int bin = 0;
            if (slopeBlock.Value.IsPositive)
            {
                int binsBeforeStart = (int)(slopeBlock.Value.initialValue / slopeBlock.Value.avgValue);
                bin = slopeBlock.Value.Position - binsBeforeStart; ;
                if (bin < 0) bin = 0;
            }
            else
            {
                int binsAfterStart = (int)(slopeBlock.Value.initialValue / (Math.Abs(slopeBlock.Value.avgValue)));
                bin = slopeBlock.Value.Position + binsAfterStart;
            }
            int result = bin * hzPerBin;
            return (result);
        }
    }
}