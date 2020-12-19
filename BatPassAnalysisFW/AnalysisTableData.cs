using DspSharp.Utilities.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace BatPassAnalysisFW
{
    public class AnalysisTableData : INotifyPropertyChanged
    {
        public AnalysisTableData()
        {
            try
            {
                FrequencyHeader = createFrequencyHeaderBitmap();

                headerText = "file or folder";

                thresholdFactor = 1.5m;
                spectrumFactor = 1.8m;
            }
            catch (Exception ex)
            {
                AnalysisMainControl.ErrorLog($"Error initilising TableData:{ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AutoCorEnabled
        {
            get { return _autoCorEnabled; }
            set
            {
                _autoCorEnabled = value;
                NotifyPropertyChanged(nameof(AutoCorEnabled));
            }
        }

        public ObservableList<bpaPass> combinedPassList { get; set; } = new ObservableList<bpaPass>();
        public ObservableList<Pulse> combinedPulseList { get; set; } = new ObservableList<Pulse>();
        public ObservableList<bpaRecording> combinedRecordingList { get; set; } = new ObservableList<bpaRecording>();
        public ObservableList<bpaSegment> combinedSegmentList { get; set; } = new ObservableList<bpaSegment>();
        public ObservableList<SpectralPeak> combinedSpectrumList { get; set; } = new ObservableList<SpectralPeak>();

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

        public bool EnvelopeEnabled
        {
            get { return _envelopeEnabled; }
            set
            {
                _envelopeEnabled = value;
                NotifyPropertyChanged(nameof(EnvelopeEnabled));
            }
        }

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

        public bool PulseEnvelopeEnabled
        {
            get { return (_PulseEnvelopEnabled); }
            internal set
            {
                _PulseEnvelopEnabled = value;
                NotifyPropertyChanged(nameof(PulseEnvelopeEnabled));
            }
        }

        public BitmapImage pulseImageBmp
        {
            get
            {
                return (_pulseImageBmp);
            }
            private set
            {
                _pulseImageBmp = value;
                NotifyPropertyChanged(nameof(pulseImageBmp));
                if (value != null)
                {
                    PulseEnvelopeEnabled = true;
                }
            }
        }

        public bool SpectrumEnabled
        {
            get { return _spectrumEnabled; }
            set
            {
                _spectrumEnabled = value;
                NotifyPropertyChanged(nameof(SpectrumEnabled));
            }
        }

        public decimal spectrumFactor
        {
            get
            {
                return (_spectrumFactor);
            }
            set
            {
                _spectrumFactor = Math.Floor(value * 100) / 100.0m;
                NotifyPropertyChanged(nameof(spectrumFactor));
            }
        }

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

        public decimal thresholdFactor
        {
            get
            {
                return (_thresholdFactor);
            }
            set
            {
                _thresholdFactor = Math.Floor(value * 100) / 100.0m;
                NotifyPropertyChanged(nameof(thresholdFactor));
            }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; NotifyPropertyChanged(nameof(Version)); }
        }

        //public Visibility segmentDataGridVisibility { get; set; } = Visibility.Hidden;
        public BitmapImage createFrequencyHeaderBitmap(int width = 384)
        {
            //int width = 384;

            Bitmap bmp = new Bitmap(width, 20);
            System.Diagnostics.Debug.WriteLine($"CreateHeader width {width}");

            System.Drawing.Pen pen = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Red));
            using (Graphics g = Graphics.FromImage(bmp))
            {
                for (int f = 20; f < 140; f += 10)
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

        /// <summary>
        /// if the pass datagrid selection has changed update the pulse datagrid to hold
        /// the pulses for the selected passes
        /// </summary>
        /// <param name="selectedItems"></param>
        public void passDataGrid_SelectionChanged(IList selectedItems)
        {
            List<bpaPass> selectedPasses = new List<bpaPass>();
            selectedPasses.AddRange(combinedPassList);
            combinedPulseList.Clear();

            if (selectedItems != null && selectedItems.Count > 0)
            {
                selectedPasses = new List<bpaPass>();
                foreach (var item in selectedItems) selectedPasses.Add(item as bpaPass);
            }

            foreach (var pass in selectedPasses)
            {
                if (selectedPasses.Count == 1)
                {
                    //DisplayEnvelope(pass);
                }
                foreach (var pulse in pass.getPulseList())
                {
                    combinedPulseList.Add(pulse);
                }

                //combinedPulseList.AddRange(pass.getPulseList());
            }

            EnvelopeEnabled = false;

            pulseDataGrid_SelectionChange(combinedPulseList);
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
        /// if the recordings datagrid selection has changed, update the sgement datagrid
        /// to hold the segments for the selected recordings
        /// </summary>
        /// <param name="selectedItems"></param>
        public void recordingsDataGrid_SelectionChanged(IList selectedItems)
        {
            List<bpaRecording> recList = new List<bpaRecording>();
            combinedSegmentList.Clear();
            if (selectedItems != null && selectedItems.Count > 0)
            {
                combinedPassList.Clear();
                foreach (var item in selectedItems)
                {
                    if (item is bpaRecording)
                    {
                        bpaRecording recording = item as bpaRecording;

                        recList.Add(recording);
                    }
                }
            }
            else
            {
                foreach (var rec in combinedRecordingList) recList.Add(rec);
            }

            foreach (var recording in recList)
            {
                foreach (var seg in recording.getSegmentList())
                {
                    combinedSegmentList.Add(seg);
                }
            }
            segmentDataGrid_SelectionChanged(combinedSegmentList);
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

            //headerText = recording.File_Name;
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

                        var bmp = PassAnalysis.GetBitmap(ref fftData, ref peakList, (double)spectrumFactor);
                        var bmpi = bpaPass.loadBitmap(bmp);
                        SpectrumImage = bmpi;
                        SpectrumEnabled = true;
                        ObservableList<Peak> emptyList = new ObservableList<Peak>();
                        //var corrData = autoCorr.ToList<float>();
                        var acBmp = PassAnalysis.GetBitmap(ref corrData, ref emptyList, 0.0d);
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

        internal void AutoDeleteBlankPasses()
        {
            Debug.WriteLine($"\n\nAutoDeleting ");
            for (int i = 0; i < combinedRecordingList.Count(); i++)
            {
                var passesToBeRemoved = (from seg in combinedRecordingList[i].getSegmentList()
                                         from pass in seg.getPassList()
                                         where pass.getPulseList().Count < 3
                                         select pass)?.ToList();
                if (passesToBeRemoved != null)
                {
                    foreach (var pass in passesToBeRemoved)
                    {
                        combinedRecordingList[i].DeletePass(pass);
                        if (combinedPassList.Contains(pass))
                        {
                            combinedPassList.Remove(pass);
                        }
                    }
                }
                Debug.WriteLine($"Deleted {passesToBeRemoved.Count()} passes from rec number {combinedRecordingList[i].recNumber}");
            }

            recordingsDataGrid_SelectionChanged(null);
        }

        /// <summary>
        /// Delete any pulses where the peak, start or end frequency is more than 2SD away from the pass mean
        /// </summary>
        internal void AutoDeleteExtremePulses()
        {
            foreach (var rec in combinedRecordingList)
            {
                foreach (var seg in rec.getSegmentList())
                {
                    foreach (var pass in seg.getPassList())
                    {
                        pass.DeleteExtremePulses();
                    }
                }
            }
        }

        /// <summary>
        /// RemoveOutliers for any pass where the peakSD>10kHz
        /// </summary>
        internal void AutoRemoveOutliers()
        {
            Debug.WriteLine("\n\n                   Auto Remove Outliers");
            foreach (var rec in combinedRecordingList)
            {
                foreach (var seg in rec.getSegmentList())
                {
                    Debug.WriteLine($"\n{seg.getPassList().Count()} passes in segment {seg.No}");
                    var passList = seg.getPassList();
                    //var passesToBeProcessed = (from pass in passList
                    //                        where pass.peakFrequencykHzSD >= 10.0f
                    //                      select pass)?.ToList<bpaPass>();
                    var passesToBeProcessed = passList.Where(p => (double)p.GetPeakFrequencykHzSD() > 10.0d);

                    int i = 1;

                    while (passesToBeProcessed != null && passesToBeProcessed.Count() > 0)
                    {
                        List<Pulse> pulsesToBeRemoved = new List<Pulse>();
                        Debug.WriteLine($"{passesToBeProcessed.Count()} passes to be processed in round {i++}");
                        if (i > 10) break;
                        foreach (var pass in passesToBeProcessed)
                        {
                            pulsesToBeRemoved.AddRange(pass.RemoveOutliers());

                            Debug.WriteLine($"Processed Pass {pass.Pass_Number}");
                        }
                        if (pulsesToBeRemoved != null && pulsesToBeRemoved.Count > 0)
                        {
                            passList = seg.DeletePulses(pulsesToBeRemoved);
                        }

                        passesToBeProcessed = passList.Where(p => (double)p.GetPeakFrequencykHzSD() > 10.0d);
                        if (passesToBeProcessed == null || passesToBeProcessed.Count() <= 0) break;
                        Debug.WriteLine($"{passesToBeProcessed?.Count()} passes need to be reprocessed\n");
                    }

                    //if (combinedPassList.Contains(pass)) combinedPassList.Remove(pass);
                    //foreach (var pulse in pulsesToBeRemoved)
                    //{
                    //    if (combinedPulseList.Contains(pulse))
                    //    {
                    //        combinedPulseList.Remove(pulse);
                    //    }

                    //}

                    Debug.WriteLine($"RemovedOutliers from {passesToBeProcessed.Count()} passes in seg{seg.No} of rec {rec.recNumber}");
                }
            }
        }

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

        /// <summary>
        /// displays a graph of a specific pulse and its peak
        /// </summary>
        /// <param name="selectedSpectrum"></param>
        internal void DisplayPulseEnvelope(SpectralPeak selectedSpectrum)
        {
            if (selectedSpectrum != null)
            {
                Peak selectedPeak = selectedSpectrum.getPulsePeak();
                Pulse pulse = (from p in combinedPulseList
                               where p.getPeak() == selectedPeak
                               select p).SingleOrDefault();
                if (pulse != null)
                {
                    BitmapImage bmpi = pulse.getEnvelopeBitmap();
                    pulseImageBmp = bmpi;
                }
            }
        }

        internal void NotifyPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        //public Visibility recordingsDataGridVisibility { get; set; } = Visibility.Visible;
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

        internal void ReProcessFile(string file, decimal thresholdFactor, decimal spectrumFactor)
        {
            bpaRecording recording = combinedRecordingList.Where(rec => rec.FQfilename == file)?.FirstOrDefault();
            if (recording != null && recording.recNumber > 0)
            {
                recording.CreateSegments(thresholdFactor, spectrumFactor);
            }
            recordingsDataGrid_SelectionChanged(null);
        }

        //public string headerTextBlock { get; set; } = "file or folder";
        internal void SaveToDatabase()
        {
            foreach (var rec in combinedRecordingList)
            {
                PTA_DBAccess.SaveRecording(rec);
            }
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
            else
            {
                foreach (var seg in combinedSegmentList)
                {
                    foreach (var pass in seg.getPassList())
                    {
                        combinedPassList.Add(pass);
                    }
                }
            }
            passDataGrid_SelectionChanged(combinedPassList);
        }

        /// <summary>
        /// recalculates the peaks in the specified pass
        /// </summary>
        /// <param name="pass"></param>
        internal void UpdatePass(bpaPass pass)
        {
            List<bpaPass> selection = new List<bpaPass>();
            selection.Add(pass);
            bpaRecording rec = combinedRecordingList.Where(r => r.recNumber == pass.recordingNumber).FirstOrDefault();
            bpaSegment seg = rec.getSegmentList().Where(s => s.No == pass.segmentNumber).FirstOrDefault();
            bpaPass actualPass = seg.getPassList().Where(p => p.Pass_Number == pass.Pass_Number).FirstOrDefault();
            actualPass.CreatePass(thresholdFactor, spectrumFactor);

            BitmapImage bmpi = pass.GetSegmentBitmap();
            //OnBmpiCreated(new BmpiEventArgs(bmpi,BmpiEventArgs.ImageType.ENVELOPE));

            seg.ReplacePass(actualPass);
            recordingsDataGrid_SelectionChanged(combinedRecordingList);
            //passDataGrid_SelectionChanged(selection);

            EnvelopeImage = bmpi;
            EnvelopeEnabled = true;
        }

        private bool _autoCorEnabled = false;
        private BitmapImage _correlationImage;
        private bool _enableFilter = false;
        private bool _envelopeEnabled = false;
        private BitmapImage _envelopeImage;
        private BitmapImage _frequencyHeader;
        private string _headerText;
        private bool _PulseEnvelopEnabled = false;
        private BitmapImage _pulseImageBmp;
        private bool _spectrumEnabled = false;
        private decimal _spectrumFactor = 1.0m;
        private BitmapImage _spectrumImage;
        private BitmapImage _tdCorrelationImage;
        private decimal _thresholdFactor = 1.0m;
        private string _version = "Version";
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
        private int SampleRate { get; set; } = 384000;
    }
}