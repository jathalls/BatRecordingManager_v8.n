using Acr.Settings;
using DspSharp.Utilities.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BatPassAnalysisFW
{
    public class AnalysisTableData : INotifyPropertyChanged
    {
        private string _version = "Version";
        public string Version 
        {
            get { return _version; }
            set { _version = value; NotifyPropertyChanged(nameof(Version)); }
        }

        public ObservableList<bpaPass> combinedPassList { get; set; } = new ObservableList<bpaPass>();

        public ObservableList<Pulse> combinedPulseList { get; set; } = new ObservableList<Pulse>();

        public ObservableList<SpectralPeak> combinedSpectrumList { get; set; } = new ObservableList<SpectralPeak>();

        public ObservableList<bpaRecording> combinedRecordingList { get; set; } = new ObservableList<bpaRecording>();

        public ObservableList<bpaSegment> combinedSegmentList { get; set; } = new ObservableList<bpaSegment>();


        private decimal _thresholdFactor;
        public decimal thresholdFactor 
        { 
            get
            {
                return (_thresholdFactor);
            }
            set
            {
                _thresholdFactor = (decimal)(Math.Floor(value * 100)) / 100.0m;
                NotifyPropertyChanged(nameof(thresholdFactor));
            }
        }

        private decimal _spectrumFactor;
        public decimal spectrumFactor
        {
            get
            {
                return (_spectrumFactor);
            }
            set
            {
                _spectrumFactor = (decimal)(Math.Floor(value * 100)) / 100.0m;
                NotifyPropertyChanged(nameof(spectrumFactor));
            }
        }

        private bool _enableFilter = false;

        public bool EnableFilter
        {
            get
            {
                return (_enableFilter);
            }
            set
            {
                _enableFilter = value;
                NotifyPropertyChanged(nameof(EnableFilter));
            }
        }

        /*#region headerText

        /// <summary>
        /// headerText Dependency Property
        /// </summary>
        public static readonly DependencyProperty headerTextProperty =
            DependencyProperty.Register("headerText", typeof(string), typeof(AnalysisTableData),
                new FrameworkPropertyMetadata((string)"file"));

        /// <summary>
        /// Gets or sets the headerText property.  This dependency property 
        /// indicates ....
        /// </summary>
        public string headerText
        {
            get { return (string)GetValue(headerTextProperty); }
            set { SetValue(headerTextProperty, value); }
        }

        #endregion*/

        private string _headerText;
        public string headerText 
        {
            get
            {
                return _headerText;
            }
            set
            {
                _headerText = value;
                NotifyPropertyChanged(nameof(headerText));
            }
        }


        private BitmapImage _envelopeImage;
        public BitmapImage EnvelopeImage 
        {
            get
            {
                return _envelopeImage;
            }
            set
            {
                _envelopeImage = value;
                NotifyPropertyChanged(nameof(EnvelopeImage));
            }
        }

        private bool _envelopeEnabled = false;
        public bool EnvelopeEnabled {
            get { return _envelopeEnabled; }
            set { _envelopeEnabled = value;
                NotifyPropertyChanged(nameof(EnvelopeEnabled));
            }
        } 

        private BitmapImage _spectrumImage;
        public BitmapImage SpectrumImage 
        {
            get
            {
                return _spectrumImage;
            }
            set
            {
                _spectrumImage = value;
                NotifyPropertyChanged(nameof(SpectrumImage));
            }
        }

        private bool _spectrumEnabled = false;
        public bool SpectrumEnabled
        {
            get { return _spectrumEnabled; }
            set
            {
                _spectrumEnabled = value;
                NotifyPropertyChanged(nameof(SpectrumEnabled));
            }
        }

        private BitmapImage _correlationImage;
        public BitmapImage CorrelationImage
        {
            get
            {
                return _correlationImage;
            }
            set
            {
                _correlationImage = value;
                NotifyPropertyChanged(nameof(CorrelationImage));
            }
        }

        private bool _autoCorEnabled = false;
        public bool AutoCorEnabled
        {
            get { return _autoCorEnabled; }
            set
            {
                _autoCorEnabled = value;
                NotifyPropertyChanged(nameof(AutoCorEnabled));
            }
        }

        private BitmapImage _tdCorrelationImage;
        public BitmapImage TDCorrelationImage
        {
            get
            {
                return _tdCorrelationImage;
            }
            set
            {
                _tdCorrelationImage = value;
                NotifyPropertyChanged(nameof(TDCorrelationImage));
            }
        }

        private int SampleRate { get; set; } = 384000;

        

        internal void Clear()
        {
            SpectrumImage = null;
            EnvelopeImage = null;
            CorrelationImage = null;
            headerText = "Loading...";
            combinedPassList.Clear();
            combinedPulseList.Clear();
            combinedRecordingList.Clear();
            combinedSegmentList.Clear();
            combinedSpectrumList.Clear();

        }

        private BitmapImage _frequencyHeader;

        public BitmapImage FrequencyHeader 
        {
            get { return (_frequencyHeader); }
            set
            {
                _frequencyHeader = value;
                NotifyPropertyChanged(nameof(FrequencyHeader));
                System.Diagnostics.Debug.WriteLine("FrequencyHeaderChanged");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged(String propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        //public string headerTextBlock { get; set; } = "file or folder";

        //public Visibility recordingsDataGridVisibility { get; set; } = Visibility.Visible;

        //public Visibility segmentDataGridVisibility { get; set; } = Visibility.Hidden;



        public AnalysisTableData()
        {
            try
            {
                FrequencyHeader = createFrequencyHeaderBitmap();

                headerText = "file or folder";

                thresholdFactor = 1.5m;
                spectrumFactor = 1.8m;
            }catch(Exception ex)
            {
                AnalysisMainControl.ErrorLog($"Error initilising TableData:{ex.Message}");
            }
        }

        

        public BitmapImage createFrequencyHeaderBitmap(int width=384)
        {
            //int width = 384;
            
            Bitmap bmp = new Bitmap(width, 20);
            System.Diagnostics.Debug.WriteLine($"CreateHeader width {width}");

            System.Drawing.Pen pen = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Red));
            using (Graphics g = Graphics.FromImage(bmp))
            {
                for (int f = 20; f < 70; f += 10)
                {
                    if (f * 2 < width)
                    {
                        g.DrawLine(new System.Drawing.Pen(Color.Gold, 2), f * 2, 0, f * 2, 20);
                        g.DrawString(f.ToString(), new Font(FontFamily.GenericMonospace, 8), new SolidBrush(Color.Black), new System.Drawing.Point((f * 2) - 10, 0));
                    }
                }
                
                g.DrawString("kHz", new Font(FontFamily.GenericSansSerif, 8), new SolidBrush(Color.Black), new System.Drawing.Point(5, 0));

            }

            return (bpaPass.loadBitmap(bmp));
        }

        internal void RefreshFrequencyheader()
        {
            int? sampleRate = 384000;
            if (combinedPassList != null && combinedPassList.Any())
            {
                sampleRate = combinedPassList?.First().SampleRate;
            }
            if (sampleRate == null)
            {
                sampleRate = 384000;
            }
            FrequencyHeader = createFrequencyHeaderBitmap((int)(sampleRate.Value / 1000.0f));
            
        }

        internal void SaveToDatabase()
        {
            foreach(var rec in combinedRecordingList)
            {
                
                PTA_DBAccess.SaveRecording(rec);
            }
        }

        /// <summary>
        /// Sets a single recording to be analysed where the recording has an associated .txt file
        /// </summary>
        /// <param name="recording"></param>
        public void SetRecording(bpaRecording recording)
        {
            //this.recording = recording;

            combinedRecordingList.Clear();
            combinedSegmentList.Clear();

            SampleRate = recording.SampleRate;

            combinedRecordingList.Add(recording);
            //foreach (var seg in recording.getSegmentList())
            //{
            //    combinedSegmentList.Add(seg);
            //}

            headerText = $"{recording.File_Name}, {recording.getSegmentList().Count} segments";
            //recordingsDataGridVisibility = Visibility.Hidden;
            //segmentDataGridVisibility = Visibility.Visible;
            
            recordingsDataGrid_SelectionChanged(combinedRecordingList);

            headerText = recording.File_Name;


        }

        internal void segmentDataGrid_SelectionChanged(IList selectedItems)
        {
            combinedPassList.Clear();
            if (selectedItems != null && selectedItems.Count > 0)
            {
                
                foreach (var item in selectedItems)
                {
                    if (item is bpaSegment)
                    {
                        bpaSegment segment = item as bpaSegment;
                        foreach (var pass in segment.getPassList())
                        {
                            combinedPassList.Add(pass);
                        }

                        //combinedPassList.AddRange(segment.getPassList());
                    }
                }


            }
            passDataGrid_SelectionChanged(combinedPassList);
            
        }

        /// <summary>
        /// Initialises the display for multiple recordings of 1 segment each
        /// </summary>
        /// <param name="bpaRecordingList"></param>
        public void SetRecordings(List<bpaRecording> bpaRecordingList)
        {


            combinedRecordingList.Clear();
            combinedSegmentList.Clear();
            if (bpaRecordingList == null || bpaRecordingList.Count <= 0) return;

            string path = System.IO.Path.GetDirectoryName(bpaRecordingList[0].File_Name);
            var numFiles = Directory.EnumerateFiles(path).Count();
            headerText = $"{path}, contains {numFiles} recordings";

            foreach (var rec in bpaRecordingList)
            {
                combinedRecordingList.Add(rec);
                foreach (var seg in rec.getSegmentList())
                {
                    combinedSegmentList.Add(seg);
                }

            }
            //segmentDataGridVisibility = Visibility.Hidden;
            //recordingsDataGridVisibility = Visibility.Visible;
            segmentDataGrid_SelectionChanged(combinedSegmentList);
            
        }

        /// <summary>
        /// If there is just one spectral peak item selected, then find the corresponding pulse,
        /// re-create the spectrum of that pulse, and creata graph of the spectrum as a bitmap.  Then
        /// raise an event to pass the bitmap back to a display handling parent.
        /// </summary>
        /// <param name="selectedItems"></param>
        public void spectralPeakDataGrid_SelectionChanged(IList selectedItems)
        {
            SpectrumEnabled = false;
            AutoCorEnabled = false;
            if (selectedItems != null && selectedItems.Count == 1)
            {
                if (selectedItems[0] != null)
                {
                    SpectralPeak sp = selectedItems[0] as SpectralPeak;

                    int p = sp.Pulse_ - 1;
                    if (p >= 0 && p < combinedPulseList.Count())
                    {
                        Pulse pulse = combinedPulseList[p];

                        var details = pulse.GetSpectrumDetails();
                        var peakList = details.spectralPeakList;
                        List<float> corrData = new List<float>();
                        List<float> fftData = new List<float>();
                         pulse.getFFT(out fftData, out corrData);
                        //float[] autoCorr = pulse.getAutoCorrelation(byFft:true);

                        //ObservableList<Peak> peakList = new ObservableList<Peak>();

                        
                        var bmp = PassAnalysis.GetBitmap(ref fftData, ref peakList, spectrumFactor);
                        var bmpi = bpaPass.loadBitmap(bmp);
                        SpectrumImage = bmpi;
                        SpectrumEnabled = true;
                        ObservableList<Peak> emptyList = new ObservableList<Peak>();
                        //var corrData = autoCorr.ToList<float>();
                        var acBmp = PassAnalysis.GetBitmap(ref corrData, ref emptyList, 0.0m);
                        CorrelationImage = bpaPass.loadBitmap(acBmp);
                        AutoCorEnabled = true;
                        //OnBmpiCreated(new BmpiEventArgs(bmpi,BmpiEventArgs.ImageType.SPECTRUM));

                        //float[] TDCorrelation = details.getAutoCorrelation(byFft: false);

                        //var TDCorrData = TDCorrelation.ToList<float>();
                        //bmp = PassAnalysis.GetBitmap(ref TDCorrData, ref emptyList, 0.0m);
                        //TDCorrelationImage = bpaPass.loadBitmap(bmp);
                    }

                }
            }
        }

        /// <summary>
        /// if the recordings datagrid selection has changed, update the sgement datagrid
        /// to hold the segments for the selected recordings
        /// </summary>
        /// <param name="selectedItems"></param>
        public void recordingsDataGrid_SelectionChanged(IList selectedItems)
        {
            
            combinedSegmentList.Clear();
            if (selectedItems != null && selectedItems.Count>0)
            {
                combinedPassList.Clear();
                foreach (var item in selectedItems)
                {
                    if (item is bpaRecording)
                    {
                        bpaRecording recording = item as bpaRecording;
                        foreach(var seg in recording.getSegmentList())
                        {
                            combinedSegmentList.Add(seg);
                        }
                        

                        
                    }
                }
            }
            segmentDataGrid_SelectionChanged(combinedSegmentList);
            
        }

        /// <summary>
        /// if the pulse datagrid selection has changed update the spectral preak datagrid
        /// with the spectrs for the selected pulses
        /// </summary>
        /// <param name="selectedItems"></param>
        public void pulseDataGrid_SelectionChange(IList selectedItems)
        {
            combinedSpectrumList.Clear();
            if (selectedItems != null && selectedItems.Count > 0)
            {

                foreach (var item in selectedItems)
                {
                    if (item is Pulse)
                    {
                        Pulse pulse = item as Pulse;
                        int index = combinedPulseList.IndexOf(pulse);
                        foreach (var peak in pulse.GetSpectrumDetails().spectralPeakList)
                        {
                            SpectralPeak sp = peak as SpectralPeak;
                            sp.parentPulseIndex = index;
                            combinedSpectrumList.Add(sp);
                        }
                    }
                }
            }
            spectralPeakDataGrid_SelectionChanged(combinedSpectrumList);
            
        }

        /// <summary>
        /// if the pass datagrid selection has changed update the pulse datagrid to hold
        /// the pulses for the selected passes
        /// </summary>
        /// <param name="selectedItems"></param>
        public void passDataGrid_SelectionChanged(IList selectedItems)
        {
            combinedPulseList.Clear();
            if (selectedItems != null && selectedItems.Count>0)
            {

                foreach (var item in selectedItems)
                {
                    if (item is bpaPass)
                    {
                        bpaPass pass = item as bpaPass;
                        if (selectedItems.Count == 1)
                        {
                            //DisplayEnvelope(pass);
                        }
                        foreach(var pulse in pass.getPulseList())
                        {
                            combinedPulseList.Add(pulse);
                        }
                        

                        //combinedPulseList.AddRange(pass.getPulseList());
                    }
                }
            }
            EnvelopeEnabled = false;

            pulseDataGrid_SelectionChange(combinedPulseList);
            
            
        }



        /// <summary>
        /// Given a pass, extracts the envelope and displays that in a bitmapImage.
        /// The BitmapImage is passed through an event handler to a window which can display it.
        /// </summary>
        /// <param name="pass"></param>
        internal void DisplayEnvelope(bpaPass pass)
        {
            BitmapImage bmpi = pass.GetEnvelopeBitmap();
            //OnBmpiCreated(new BmpiEventArgs(bmpi,BmpiEventArgs.ImageType.ENVELOPE));
            EnvelopeImage = bmpi;
            EnvelopeEnabled = true;
        }

        
    }
}
