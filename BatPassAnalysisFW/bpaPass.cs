using DspSharp.Utilities.Collections;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LinqStatistics;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using Acr.Settings;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// class representing a single pass within a segment with a duration of up to 7.5s
    /// </summary>
    public class bpaPass
    {
        

        public int Pass_Number
        {
            get
            {
                return (passNumber);
            }
        }

        public double Pass_Length_s
        {
            get
            {
                return ((double)PassLengthInSamples / SampleRate);
            }
        }

        public int Number_Of_Pulses
        {
            get
            {
                return (pulseList.Count());
            }
        }
        
        public string Peak_kHz
        {
            get
            {
                var pfMeanList = (from pulse in pulseList
                                  where pulse.GetSpectrumDetails().pfMean>15000
                                select pulse.GetSpectrumDetails().pfMean);
                if (pfMeanList != null && pfMeanList.Any())
                {
                    _peakFrequency = (float)(pfMeanList.Average() / 1000.0f);
                    if (pfMeanList.Count() > 3)
                    {
                        peakFrequencySD = (float)pfMeanList.StandardDeviation() / 1000.0f;
                    }
                    else
                    {
                        peakFrequencySD = 0.0f;
                    }
                    if (pfMeanList.Count() >= 2)
                    {
                        return ($"{_peakFrequency:G3}+/-{(pfMeanList.StandardDeviation() / 1000.0f):G3}");
                    }
                    else
                    {
                        return ($"{_peakFrequency:G3}");
                    }
                    
                }
                return ("");
            }
        }

        public string Start_kHz
        {
            get
            {
                var pfStartList = (from pulse in pulseList
                                  where pulse.GetSpectrumDetails().pfStart > 15000
                                  select pulse.GetSpectrumDetails().pfStart);
                if (pfStartList != null && pfStartList.Any())
                {
                    _startFrequency = (float)(pfStartList.Average() / 1000.0f);
                    if (pfStartList.Count() > 3)
                    {
                        startFrequencySD = (float)pfStartList.StandardDeviation() / 1000.0f;
                    }
                    else
                    {
                        startFrequencySD = 0.0f;
                    }
                    if (pfStartList.Count() >= 2)
                    {
                        return ($"{_startFrequency:G3}+/-{(pfStartList.StandardDeviation() / 1000.0f):G3}");
                    }
                    else
                    {
                        return ($"{_startFrequency:G3}");
                    }

                }
                return ("");
            }
        }

        public string End_kHz
        {
            get
            {
                var pfEndList = (from pulse in pulseList
                                   where pulse.GetSpectrumDetails().pfEnd > 15000
                                   select pulse.GetSpectrumDetails().pfEnd);
                if (pfEndList != null && pfEndList.Any())
                {
                    double avg = pfEndList.Average() / 1000.0f;
                    if (pfEndList.Count() > 3)
                    {
                        endFrequencySD = pfEndList.StandardDeviation() / 1000.0f;
                    }
                    else
                    {
                        endFrequencySD = 0.0f;
                    }
                    _endFrequency = (float)avg;
                    if (pfEndList.Count() >= 2)
                    {
                        return ($"{avg:G3}+/-{(pfEndList.StandardDeviation() / 1000.0f):G3}");
                    }
                    else
                    {
                        return ($"{_endFrequency:G3}");
                    }
                }
                return ("");
            }
        }

        private float _peakFrequency  = -1.0f;
        private float peakFrequency
        {
            get
            {
                if (_peakFrequency < 0.0f)
                {
                    _ = Peak_kHz;
                }
                return (_peakFrequency);
            }
        }

        private float peakFrequencySD { get; set; } = 0.0f;

        private float _endFrequency = -1.0f;
        private float endFrequency
        {
            get
            {
                if (_endFrequency < 0.0f)
                {
                    _ = End_kHz;
                }
                return (_endFrequency);
            }
        }

        private float _endFrequencySD = 0.0f;

        private float endFrequencySD
        {
            get { return (_endFrequencySD); }
            set { _endFrequencySD = value; }
        }

        private float _startFrequency = -1.0f;
        private float startFrequency
        {
            get
            {
                if (_startFrequency < 0.0f)
                {
                    _ = Start_kHz;
                }
                return (_startFrequency);
            }
        }

        private float startFrequencySD { get; set; } = 0.0f;

        public BitmapImage Frequency
        {
            get
            {
                int width = SampleRate/1000;
                Bitmap bmp = new Bitmap(width, 20);

                System.Drawing.Pen pen = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Red));
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    for(int f = 20; f < 70; f += 10)
                    {
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Gold,2), f*2, 0,f*2, 20);
                    }
                    if (endFrequency > 15)
                    {
                        System.Drawing.Pen faintPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightCoral));
                        g.FillRectangle(faintPen.Brush, (endFrequency - endFrequencySD) * 2, 0, endFrequencySD * 4, 7);

                        int x = (int)endFrequency*2;
                        x = x > width ? width : x;
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Red,2), x, 0,x, 20);
                        
                        

                        
                    }

                    if (startFrequency > 15)
                    {
                        System.Drawing.Pen faintPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightBlue));
                        g.FillRectangle(faintPen.Brush, (startFrequency - startFrequencySD) * 2, 14, startFrequencySD * 4, 7);

                        int x = (int)startFrequency*2;
                        x = x > width ? width : x;
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Blue,2), x, 0,x, 20);
                    }
                    if (peakFrequency > 15)
                    {
                        System.Drawing.Pen faintPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, System.Drawing.Color.LightGreen));
                        g.FillRectangle(faintPen.Brush, (peakFrequency - peakFrequencySD) * 2, 7, peakFrequencySD * 4, 7);

                        int x = (int)peakFrequency*2;
                        x = x > width ? width : x;
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Green,2),x, 0,x, 20);
                    }
                }

                return (loadBitmap(bmp));
            }
        }

        

        public static BitmapImage loadBitmap(System.Drawing.Bitmap source)
        {

            BitmapImage bmpi = null;
            try
            {
                MemoryStream ms = new MemoryStream();
                source.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                bmpi = new BitmapImage();
                bmpi.BeginInit();
                bmpi.StreamSource = ms;
                bmpi.EndInit();


            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in Load Bitmap:-" + ex.Message);
            }

            return bmpi;
        }

        private int OffsetInSegmentInSamples { get; set; } = -1;

        private int PassLengthInSamples { get; set; } = -1;

        internal int SampleRate { get; set; } 

        public DataAccessBlock passDataAccessBlock { get; set; }

        public TimeSpan segStart { get; set; }

        public float segLength { get; set; }
        //private float[] quiet;
        private float[] envelope;

        private int passNumber { get; set; }

        public int segmentNumber { get; set; }

        public int recordingNumber { get; set; }

        public string Comment { get; set; }

        public string fileName { get; set; }

        private decimal thresholdFactor { get; set; }

        private decimal spectrumfactor { get; set; }

        private ObservableList<Pulse> pulseList = new ObservableList<Pulse>();
        

        public bpaPass(int recNumber, int segmentNumber, int passNumber, int offsetInSegment, DataAccessBlock dab,int sampleRate,string Comment)
        {
            OffsetInSegmentInSamples = offsetInSegment;
            PassLengthInSamples = (int)(dab.length);
            SampleRate = sampleRate;
            passDataAccessBlock = dab;
            segStart = TimeSpan.FromSeconds((float)(offsetInSegment + dab.startLocation) / (float)sampleRate);
            segLength = (float)dab.segLength / (float)sampleRate;

            fileName = Path.GetFileName(dab.fileName);
            
            this.passNumber = passNumber;
            this.segmentNumber = segmentNumber > recNumber ? segmentNumber : recNumber;
            this.recordingNumber = recNumber;

            this.Comment = Comment;
        }

        public ObservableList<Pulse> getPulseList()
        {
            return (pulseList);
        }

        public enum peakState { NOTINPEAK, INPEAKLEADIN, INPEAK, INPEAKLEADOUT };
        public void CreatePass(decimal thresholdFactor,decimal spectrumFactor)
        {
            float[] passData = passDataAccessBlock.getData();

            this.thresholdFactor = thresholdFactor;
            this.spectrumfactor = spectrumfactor;


            HighPassFilter(ref passData, frequency: 15000, SampleRate);
            //quiet = getQuietPortion(secs: 0.1f,factor:1.0f);
            envelope = PassAnalysis.GetEnvelope(ref passData, SampleRate);
            float leadInms = CrossSettings.Current.Get<float>("EnvelopeLeadInMS");
            if (leadInms <= 0.0f)
            {
                leadInms = 0.2f;
                CrossSettings.Current.Set<float>("EnvelopeLeadInMS", leadInms);
            }
            int leadinLimit = (int)((SampleRate/1000) * leadInms); //0.2ms minimum duration

            float leadOutms = CrossSettings.Current.Get<float>("EnvelopeLeadOutMS");
            if (leadOutms <= 0.0f)
            {
                leadOutms = 1.0f;
                CrossSettings.Current.Set<float>("EnvelopeLeadOutMS", 1.0f);
            }
            int leadoutLimit = (int)((SampleRate/1000) * leadOutms); //1ms silence between peaks
            ObservableList<Peak> peakList = new ObservableList<Peak>();
            int quietStart=getQuietStart((float)(envelope.Average()*(float)thresholdFactor));
            if (quietStart < 0)
            {
                quietStart=getQuietStart((float)(envelope.Average() * (float)thresholdFactor * 2));
            }
            float[] dummy = new float[0];
            PassAnalysis.getPeaks(ref envelope, SampleRate, leadinLimit, leadoutLimit, (float)thresholdFactor, 
                out peakList,autoCorrelation:ref dummy, OffsetInSegmentInSamples ,PassNumber:passNumber,RecordingNumber:segmentNumber);
            foreach (var peak in peakList)
            {
                pulseList.Add(new Pulse(ref passData,OffsetInSegmentInSamples, peak,passNumber,quietStart,spectrumFactor));
            }
            //Debug.WriteLine($"Pass at Offset {OffsetInSegmentInSamples} of length {Pass_Length_s}s has {pulseList.Count} pulses");


        }

        /// <summary>
        /// Searches the envelope for a section in which all the envelope is below the threshold for a duration of 5000 samples
        /// </summary>
        /// <returns></returns>
        private int getQuietStart(float threshold)
        {
            peakState currentPeakState = peakState.NOTINPEAK;
            int counter = 0;
            for(int i=0;i<envelope.Length;i++)
            {
                if (envelope[i] < threshold)
                {
                    counter++;
                    if (counter >= 5000)
                    {
                        return (i - 5000);
                    }
                }
                else
                {
                    counter = 0;
                }
            }
            return (-1);
        }

        /// <summary>
        /// Gets the envelope of the pass and draws it into a bitmap with the identified peaks numbered
        /// </summary>
        /// <returns></returns>
        internal BitmapImage GetEnvelopeBitmap()
        {

            if (envelope != null && pulseList != null && envelope.Length > 5 && pulseList.Any())
            {
                ObservableList<Peak> peakList = new ObservableList<Peak>();
                foreach (var pulse in pulseList)
                {

                    peakList.Add(pulse.getPeak());
                }

                var bmp = PassAnalysis.GetGraph(envelope, ref peakList, thresholdFactor);
                return (loadBitmap(bmp));
            }

            return (null);

        }

        /// <summary>
        /// removes items from the PulseList in order to reduce the variation in frequency
        /// parameters
        /// </summary>
        internal void RemoveOutliers()
        {
            if(pulseList==null || pulseList.Count < 3)
            {
                return; // too few pulses to make removal practicable
            }
            List<Pulse> mutablePulseList = pulseList.ToList();
            var orderedList = (from p in mutablePulseList
                              orderby  getVariance(p, mutablePulseList) descending
                               select p).ToList();
            List<Pulse> toBeRemoved = new List<Pulse>();
            while (orderedList.Count() > 3)
            {
                var endfSD = (from p in orderedList
                              select p.GetSpectrumDetails().pfEnd).StandardDeviation();
                var startSD = orderedList.Select(p => p.GetSpectrumDetails().pfStart).StandardDeviation();
                var peakSD = orderedList.Select(p => p.GetSpectrumDetails().pfMean).StandardDeviation();

                var shortList = orderedList.ToList();
                shortList.RemoveAt(0);
                var oldvar = getVariance(null, orderedList.ToList());
                var newvar = getVariance(null, shortList);
                var pc = Math.Abs((oldvar - newvar) / oldvar);
                if (pc < 0.01d) break;
                else
                {
                    toBeRemoved.Add(orderedList[0]);
                    orderedList = shortList;
                }

            }
            foreach(var p in toBeRemoved)
            {
                pulseList.Remove(p);
            }
            
            
        }

        internal double getVariance(Pulse pulse,List<Pulse> plist)
        {

            double variance = 0.0d;

            var meanEnd = plist.Select(p => p.GetSpectrumDetails().pfEnd).Average();
            var meanStart= plist.Select(p => p.GetSpectrumDetails().pfStart).Average();
            var meanPeak= plist.Select(p => p.GetSpectrumDetails().pfMean).Average();

            if (pulse != null)
            {
                variance = Math.Sqrt(Math.Pow(pulse.GetSpectrumDetails().pfEnd - meanEnd, 2) +
                            Math.Pow(pulse.GetSpectrumDetails().pfStart - meanStart, 2) +
                            Math.Pow(pulse.GetSpectrumDetails().pfMean - meanPeak, 2));
            }
            else
            {
                variance = Math.Sqrt(Math.Pow(meanEnd, 2) +
                            Math.Pow(meanStart, 2) +
                            Math.Pow(meanPeak, 2));
            }

            return (variance);
        }

        public static void HighPassFilter(ref float[] data,int frequency,int sampleRate)
        {
            var filter = BiQuadFilter.HighPassFilter((float)sampleRate, frequency, q:1);
            for(int i = 0; i < data.Length; i++)
            {
                data[i] = filter.Transform(data[i]);
            }
        }

        private float[] getQuietPortion(float secs,float factor)
        {
            float[] result;
            float[] data = passDataAccessBlock.getData();
            int sizeInSamples = (int)(secs * SampleRate);
            result = new float[sizeInSamples];
            float avg = data.Average()*factor;
            for(int i = 0; i < data.Length - sizeInSamples; i++)
            {
                float[] sampleData = passDataAccessBlock.getData((int)(passDataAccessBlock.startLocation + i), sizeInSamples);
                if (sampleData.Max() < avg)
                {
                    result = sampleData;
                    return (result);
                }
            }
            // if we failed to find a portion of length secs that was below the average level in the pass
            // then we try again looking a for half that length below 1.5xthe average value of the pass
            // recursively with smaller samples size and hight factors until the sample size becomes less
            // than two fft's worth (5.3ms at 384000 sps)
            if (sizeInSamples > 2048) {
                result = getQuietPortion(secs / 2, 1.5f * factor);
            }



            return (null);
        }
    }

    


    #region BackgroundConverter (ValueConverter)

    public class BackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null || parameter == null) return (System.Windows.Media.Brushes.White);

                System.Windows.Media.Brush[] brushes = parameter as System.Windows.Media.Brush[];
                if (brushes == null) return (System.Windows.Media.Brushes.White);

                
                if (value is int)
                {
                    int val = (int)value;
                    if(val%2 == 0)
                    {
                        return (SolidColorBrush)brushes[0];
                    }
                }
                return (SolidColorBrush)brushes[1];

            }
            catch
            {
                return System.Windows.Media.Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion

}
