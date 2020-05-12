using DspSharp.Exceptions;
using DspSharp.Utilities.Collections;
using LinqStatistics;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
  

  

        internal static float[] GetEnvelope(ref float[] data,int sampleRate)
        {
            float[] result1 = new float[data.Length];
            float[] result2 = new float[data.Length];
            if (data != null && data.Length > 384)
            {
                for (int s = 0; s < data.Length; s++)
                {
                    result1[s] = Math.Abs(data[s]);
                }
                //for (int s = 384; s < result1.Length; s++)
                //{
                //    var tot = 0.0d;
                //    for (int d = s - 384; d < s; d++)
                //    {
                //        tot += result1[d];
                //    }
                //    result2[s] = (float)(tot / 384);
                var filter = BiQuadFilter.LowPassFilter(sampleRate, 2000, 1);
                for(int s = 0; s < data.Length; s++)
                {
                    result2[s] = filter.Transform(result1[s]);
                }
                
            }
            return (result2);
        }
       

        public enum peakState { NOTINPEAK,INPEAKLEADIN,INPEAK,INPEAKLEADOUT};
        
        public static string getPeaks(ref float[] dataInPass,int sampleRate, int leadInSamples,int leadOutSamples,float thresholdFactor,out ObservableList<Peak> peakList, ref float[] autoCorrelation,
            int startOfstartOfPassInSegment = 0,bool asSpectralPeak=false,Peak parentPulse=null,int PassNumber=1,int RecordingNumber=1)
        {
            peakList = new ObservableList<Peak>();
            peakState currentPeakState=peakState.NOTINPEAK;
            string result = "";
            float limit;
            float maxValue = float.MinValue;
           
            limit = dataInPass.Average()*thresholdFactor;

            Debug.WriteLine($"GetPeaks (asSpectrum={asSpectralPeak}) data Average={dataInPass.Average()} Limit={limit} Factor={thresholdFactor} Leadin={leadInSamples} LeadOut={leadOutSamples}");
            //Debug.WriteLine($"Threshold={limit}");
            int lastStart = 0;
            bool inPeak = false;
            int peakNumber = 0;
            int leadInCount=0;
            int leadinLimit = leadInSamples;// (int)(sampleRate*0.0002f);//0.2ms minimum duration
            int leadoutLimit = leadOutSamples;// (int)(sampleRate*0.001f);//5ms silence between peaks
            int leadOutCount=0;
            int peakCount=0;
            int peakStartInPass = 0;
            int peakEndInPass = 0;
            currentPeakState = peakState.NOTINPEAK;
            double peakArea = 0.0d;
            double leadArea = 0.0d;
            int start15kHz = 0;
            if (asSpectralPeak)
            {
                start15kHz=(int)(15000.0f / (((float)sampleRate / 2.0f) / (float)dataInPass.Length)); // if a spectrum ignore the first 15kHz
            }
            for(int sampleInPass = asSpectralPeak?start15kHz:0; sampleInPass < dataInPass.Length; sampleInPass++)
            {
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
                                currentPeakState = peakState.INPEAK;
                                if (asSpectralPeak)
                                {
                                    Debug.WriteLine($"$$$$$ in peak at {sampleInPass}");
                                }
                                peakCount = leadInCount;
                                peakArea = leadArea;
                            }
                        }
                        else
                        {
                            currentPeakState = peakState.NOTINPEAK;
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
                            leadArea = dataInPass[sampleInPass];
                        }
                        break;
                    case peakState.INPEAKLEADOUT:
                        //Debug.WriteLine("\nleadout");
                        if (dataInPass[sampleInPass] > limit)
                        {
                            peakCount += leadOutCount;
                            currentPeakState = peakState.INPEAK;
                            peakArea += leadArea;
                        }
                        else
                        {
                            leadOutCount++;
                            leadArea += dataInPass[sampleInPass];
                            if (leadOutCount > leadoutLimit)
                            {
                                currentPeakState = peakState.NOTINPEAK;
                                int interval = lastStart > 0 ? peakStartInPass - lastStart : 0;
                                if (asSpectralPeak || ((interval == 0 || interval > (sampleRate *0.03)) ))// sampleRate/50 == 30ms min separation
                                {
                                    if (asSpectralPeak)
                                    {
                                        SpectralPeak peak=SpectralPeak.Create(++peakNumber, peakStartInPass, peakCount, peakArea, maxValue, lastStart > 0 ? peakStartInPass - lastStart : 0, sampleRate, 
                                            ref autoCorrelation, parentPulse, startOfstartOfPassInSegment,dataInPass.Skip(peakStartInPass).Take(peakCount).ToArray<float>(),(sampleRate/2)/dataInPass.Length,PassNumber,RecordingNumber,limit);
                                        peakList.Add(peak);
                                    }
                                    else
                                    {

                                        float[] peakData = dataInPass.Skip(peakStartInPass).Take(peakCount).ToArray<float>();
                                        float peakStartSecs = (float)peakStartInPass / (float)sampleRate;
                                        float peakEndSecs = (float)(peakEndInPass) / (float)sampleRate;
                                        float passStartSecs = (float)startOfstartOfPassInSegment / (float)sampleRate;
                                        Peak peak = Peak.Create(++peakNumber, peakStartInPass, peakCount, peakArea, maxValue, lastStart > 0 ? peakStartInPass - lastStart : 0, 
                                            sampleRate, startOfstartOfPassInSegment,RecordingNumber:RecordingNumber,AbsoluteThreshold:limit);
                                        peakList.Add(peak);
                                    }
                                    lastStart = peakStartInPass;
                                }
                                maxValue = float.MinValue;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            return ($"found {peakList.Count} peaks");
        }

        
        

        public static Bitmap GetGraph(float[] data, ref ObservableList<Peak> pulseList,decimal Factor)
        {

            
            if (data == null || data.Length <= 1900) return (null);
            List<float> shortData = new List<float>();
            int blocksize = (int)(data.Length/ 1900);
            if (data.Length > blocksize)
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

            Bitmap bmp = GetBitmap(shortData,ref pulseList,Factor,blocksize);
            return (bmp);
        }

        /// <summary>
        /// Returns a bitmap of a graph of the supplied data with the width of the size of the data array
        /// </summary>
        /// <param name="shortData">The data to be graphed</param>
        /// <param name="pulseList">optional list of Peak of the detected peaks in the graph</param>
        /// <param name="blocksize">If the datasize was reduced by averaging over a number of points, this is the number of points averaged</param>
        /// <returns></returns>
        public static Bitmap GetBitmap(List<float> shortData, ref ObservableList<Peak> pulseList, decimal Factor, int blocksize = 1)

        {
            shortData = (from d in shortData select (float)Math.Abs(d)).ToList<float>();

            int widthFactor = 1;
            int dataSize = shortData.Count();
            while (dataSize * widthFactor < 1500) widthFactor++;
            int imageHeight = (int)(0.56f * dataSize*widthFactor);
            var bmp = new Bitmap(dataSize*widthFactor, imageHeight, PixelFormat.Format32bppArgb);
            float AbsoluteThreshold = 0.0f;
            int sampleRate = 0;
            int HzPerSample = 0;
            if (pulseList!=null && pulseList.Any())
            {
                if (pulseList.First() is SpectralPeak sp)
                {
                    sampleRate = 0;
                    HzPerSample = sp.GetHzPerSample();
                    AbsoluteThreshold = sp.AbsoluteThreshold;

                }
                else
                {
                    sampleRate = pulseList.First().GetSampleRatePerSecond();
                    HzPerSample = 0;
                    AbsoluteThreshold = pulseList.First().AbsoluteThreshold;
                }
            }

            Debug.WriteLine($"AbsThreshold={AbsoluteThreshold}");
            
            int dataOffset = 50;
            if (pulseList == null || pulseList.Count() <= 0) dataOffset = 0;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                //int[] normalData = new float[data.Length];
                int plotHeight = imageHeight - dataOffset;
                var max = shortData.Max();
                var min = shortData.Min();
                var range = max - min;
                var mean = shortData.Average();
                var scaleFactor = plotHeight/max;
                Debug.WriteLine($"ScaleFactor={scaleFactor} Min={min}, Range={range}");

                //draw the graph of the data
                //Debug.WriteLine($"shortData:- Max={max}, Min={min}, Range={range} peak value={(int)(((max - min) / range) * (height - 1))}, mean={mean}");
                int i = 0;
                Pen blackPen = new Pen(new SolidBrush(Color.Black));
                Pen redPen = new Pen(new SolidBrush(Color.Red));
                var first= (int)(((shortData[0] ) / range) * (plotHeight ));
                int scaledThreshold = (int)((AbsoluteThreshold ) *scaleFactor);
                Point last = new Point(0, plotHeight-first);
                Debug.WriteLine($"ScaledThreshold={scaledThreshold} ImageHt={imageHeight} plotHt={plotHeight} offset={dataOffset}");

                for (int k = 1; k < shortData.Count(); k++)
                {
                    var val = shortData[k];
                    Pen pen = blackPen;
                    /*if (val > AbsoluteThreshold)
                    {
                        pen = redPen;
                    }
                    else
                    {
                        pen = blackPen;
                    }*/
                    var p = (int)((val )*scaleFactor);
                    Point pt = new Point(i += widthFactor, imageHeight-(p+dataOffset ));
                    
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
                g.DrawLine(new Pen(new SolidBrush(Color.Green)), new Point(0, imageHeight-(scaledThreshold+dataOffset )), new Point((dataSize * widthFactor) - 1, imageHeight-(scaledThreshold+dataOffset )));
                Debug.WriteLine($"Draw threshold at {imageHeight - (scaledThreshold+dataOffset )}");
                if(sampleRate==0)
                {// assume we are plotting a spectrum and include a frequency scale
                    int fsdHz = shortData.Count * HzPerSample;
                    for(int j = 0,k=0; j < fsdHz; j += 1000,k++)
                    {
                        int ht = 10;
                        if (k % 10 == 0) ht = 20;
                        int xpos = widthFactor*(int)((float)j/(float)HzPerSample);
                        g.DrawLine(blackPen, new Point(xpos, imageHeight-dataOffset ), new Point(xpos, imageHeight - (dataOffset - ht)));
                    }
                }
                else
                {// assume we are plotting envelopes and have a time scale
                    int ms = sampleRate / 1000;
                    int fullSize = shortData.Count * blocksize;
                    Debug.WriteLine($"Bitmap:- SR={sampleRate} step={ms} size={fullSize} for block={blocksize}");
                        
                    for(int j = 0, k=0; j < fullSize; j+=ms,k++)
                    {
                        int xpos = (j / blocksize);
                        int ht = 5;
                        if ((k % 10) == 0) ht = 10;
                        if ((k % 100) == 0) ht = 15;
                        if((k%1000)==0) ht = 20;
                        g.DrawLine(blackPen, new Point(xpos, imageHeight-dataOffset), new Point(xpos, imageHeight-(dataOffset - ht)));
                    }


                }

                if (pulseList != null && pulseList.Count > 0)
                {
                    foreach (var peak in pulseList)
                    {
                        //Debug.WriteLine($"{peak.pulse_Number} at {peak.GetStartAsSampleInSeg() / blocksize},{height/2} to {(int)((peak.GetStartAsSampleInSeg() + peak.getPeakWidthSamples()) / blocksize)}" +
                         //   $"mean={mean} ptmean={ptMean} line at {ptMean+dataOffset}");
                        g.DrawString(peak.pulse_Number.ToString(), new Font(FontFamily.GenericMonospace, 8), new SolidBrush(Color.Blue), new PointF((peak.GetStartAsSampleInSeg() / blocksize)*widthFactor, dataOffset/2));
                        //g.DrawLine(new Pen(new SolidBrush(Color.DarkOrange)), new Point((int)peak.GetStartAsSampleInSeg()/blocksize, height/2), new Point((int)((peak.GetStartAsSampleInSeg() + peak.getPeakWidthSamples())/blocksize), height/2));
                        Rectangle rect = new Rectangle();
                        rect.Width = (peak.getPeakWidthSamples() / blocksize)*widthFactor;
                        rect.Height = 10;
                        rect.X = ((int)(peak.GetStartAsSampleInSeg() / blocksize))*widthFactor;
                        rect.Y = plotHeight / 2;

                        g.DrawRectangle(new Pen(new SolidBrush(Color.Chocolate)), rect);
                    }
                }
                Debug.WriteLine($"Draw zero-line and scale at imageheight-{dataOffset}={imageHeight - dataOffset}");
                g.DrawLine(new Pen(new SolidBrush(Color.LightGray)), new Point(0, imageHeight-dataOffset), new Point((dataSize*widthFactor) - 1, imageHeight-dataOffset));
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
