// *  Copyright 2016 Justin A T Halls
//  *
//  *  This file is part of the Bat Recording Manager Project
//
//         Licensed under the Apache License, Version 2.0 (the "License");
//         you may not use this file except in compliance with the License.
//         You may obtain a copy of the License at
//
//             http://www.apache.org/licenses/LICENSE-2.0
//
//         Unless required by applicable law or agreed to in writing, software
//         distributed under the License is distributed on an "AS IS" BASIS,
//         WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//         See the License for the specific language governing permissions and
//         limitations under the License.

using BatPassAnalysisFW;

using Microsoft.Maps.MapControl.WPF;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UniversalToolkit;

namespace BatRecordingManager
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Provides arguments for an event.
    /// </summary>
    [Serializable]
    public class ImageListEventArgs : EventArgs
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public new static readonly ImageListEventArgs Empty =
            new ImageListEventArgs(null, new BulkObservableCollection<StoredImage>());

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #region Constructors

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        ///     Constructs a new instance of the <see cref="CustomEventArgs" /> class.
        /// </summary>
        public ImageListEventArgs(object listOwner, BulkObservableCollection<StoredImage> imageList)
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            this.imageList.Clear();
            if (imageList != null && imageList.Count > 0)
                foreach (var im in imageList)
                    this.imageList.Add(im);
            ListOwner = listOwner;
        }

        #endregion Constructors

        #region Public Properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public object ListOwner;
        public BulkObservableCollection<StoredImage> imageList { get; } = new BulkObservableCollection<StoredImage>();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion Public Properties
    }

    /// <summary>
    ///     Interaction logic for RecordingsDetailListControl.xaml
    /// </summary>
    public partial class RecordingsDetailListControl : UserControl, INotifyPropertyChanged
    {
        //-----------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingsDetailListControl" /> class.
        /// </summary>
        public RecordingsDetailListControl()
        {
            selectedSession = null;
            InitializeComponent();
            DataContext = this;
            if (recordingsList == null) recordingsList = new BulkObservableCollection<Recording>();
            //virtualRecordingsList.CollectionChanged += VirtualRecordingsList_CollectionChanged;
            //RefreshData(5, 100);

            //RecordingsListView.ItemsSource = recordingsList;
            CreateSearchDialog();
        }

        /// <summary>
        ///     Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler<RecordingChangedEventArgs> e_RecordingChanged
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            add
            {
                lock (_recordingChangedEventLock)
                {
                    _recordingChangedEvent += value;
                }
            }
            remove
            {
                lock (_recordingChangedEventLock)
                {
                    _recordingChangedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Event raised after the  property value has changed.
        /// </summary>
        [Category("Property Changed")]
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        [Description("Event raised after the SegmentSelection property value has changed.")]
        public event EventHandler<EventArgs> e_SegmentSelectionChanged
        {
            add
            {
                lock (_segmentSelectionChangedEventLock)
                {
                    _segmentSelectionChangedEvent += value;
                }
            }
            remove
            {
                lock (_segmentSelectionChangedEventLock)
                {
                    _segmentSelectionChangedEvent -= value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool isCallDisplayEnabled
        {
            get
            {
                return (_isCallDisplayEnabled);
            }
            set
            {
                _isCallDisplayEnabled = value;
                NotifyPropertyChanged(nameof(isCallDisplayEnabled));
            }
        }

        private BulkObservableCollection<Recording> _recordingsList;
        public BulkObservableCollection<Recording> recordingsList {
            get
            {
                if (_recordingsList == null)
                {
                    
                    _recordingsList=new BulkObservableCollection<Recording>();
                }
                return(_recordingsList);
            }
            set
            {
                if (value == null)
                {
                    _recordingsList = new BulkObservableCollection<Recording>();
                }
                else
                {
                    _recordingsList = value;
                }
                NotifyPropertyChanged(nameof(recordingsList));
            }
        }

        public int selectedRecordingIndex
        {
            get { return _selectedRecordingIndex; }
            set { _selectedRecordingIndex = value; NotifyPropertyChanged(nameof(selectedRecordingIndex)); }
        }

        /// <summary>
        ///     Gets or sets the selected session.   .SessionNotes?.Contains("[TimeCorrection]"))??false
        ///     
        /// .SessionNotes?.Contains("[TimeCorrection]"))??false
        /// </summary>
        /// <value>
        ///     The selected session.
        /// </value>
        public RecordingSession selectedSession
        {
            get => _selectedSession;
            set
            {
                if (value == null)//mod
                {
                    _selectedSession = new RecordingSession();
                    _selectedSession.TimeCorrection();
                    return;
                }

                _selectedSession = value;


                //mod virtualRecordingsList = new AsyncVirtualizingCollection<Recording>(new RecordingProvider((selectedSession?.Id) ?? -1), 10, 3000, defRec);
                // virtualRecordingsList.CollectionChanged += VirtualRecordingsList_CollectionChanged;

                if (OffsetsButton != null)
                {
                    OffsetsButton.IsEnabled = false;

                    if (_selectedSession != null)
                    {
                        if (_selectedSession.Sunset != null)
                        {
                            OffsetsButton.IsEnabled = true;
                        }
                        else
                        {
                            var sunset = SessionManager.CalculateSunset(value.SessionDate, value.LocationGPSLatitude,
                                value.LocationGPSLongitude);
                            _selectedSession.Sunset = sunset;
                            if (sunset != null)
                            {
                                OffsetsButton.IsEnabled = true;
                            }
                            else
                            {
                                if (!value.Recordings.IsNullOrEmpty())
                                {
                                    var rec = value.Recordings.First();
                                    if (!string.IsNullOrWhiteSpace(rec.RecordingGPSLatitude) &&
                                        !string.IsNullOrWhiteSpace(rec.RecordingGPSLongitude))
                                    {
                                        if (decimal.TryParse(rec.RecordingGPSLatitude, out decimal lat) &&
                                            decimal.TryParse(rec.RecordingGPSLongitude, out decimal longit))
                                        {
                                            sunset = SessionManager.CalculateSunset(value.SessionDate, lat,
                                                 longit);
                                            if (sunset != null)
                                            {
                                                _selectedSession.Sunset = sunset;
                                                OffsetsButton.IsEnabled = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                recordingsList.Clear();
                try
                {
                    recordingsList.AddRange(_selectedSession.Recordings);
                }
                catch (Exception) { }

                
                //NotifyPropertyChanged()

                NotifyPropertyChanged(nameof(recordingsList));
                //RefreshData(5, 100);
                //mod  NotifyPropertyChanged(nameof(virtualRecordingsList));
            }
        }

        public bool selectionIsWavFile { get; set; } = true;

        /// <summary>
        /// Clears the selection of recordings
        /// </summary>
        internal void ClearSelection()
        {
            RecordingsListView.UnselectAll();
        }

        internal void NotifyPropertyChanged(string propertyName) =>
                                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        ///     Called when [ListView item focused].
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="args">
        ///     The  instance containing the event data.
        /// </param>
        internal void OnListViewItemFocused(object sender, RoutedEventArgs args)
        {
            var lvi = sender as ListViewItem;

            lvi.BringIntoView();
            lvi.IsSelected = true;
        }

        internal void RefreshRecordings(List<Recording> recs)
        {
            RecordingSession session = null;
            if (recs != null)
            {
                foreach (var rec in recs)
                {
                    int i = recordingsList.IndexOf(rec);
                    bool selected = recordingsList[i].isSelected;
                    recordingsList[i] = DBAccess.GetRecording(rec.Id);
                    if (session == null)
                    {
                        session = recordingsList[i].RecordingSession;
                    }
                    recordingsList[i].isSelected = selected;
                }
            }

            NotifyPropertyChanged(nameof(recordingsList));
            if (session != null)
            {
                OnRecordingChanged(new RecordingChangedEventArgs(session));
            }
        }

        /// <summary>
        /// given a list of strings which start with a bat Common Name, causes all recordings
        /// in the list which contain an example of that bat to be selected.
        /// </summary>
        /// <param name="batList"></param>
        internal void SelectByBats(List<string> batList)
        {
            var recordingListFoRBats = DBAccess.GetRecordingsForBats(batList, selectedSession.Id);
            if (recordingListFoRBats != null)
            {
                foreach (var rec in recordingsList)
                {
                    if (recordingListFoRBats.Contains(rec))
                    {
                        rec.isSelected = true;
                    }
                    else
                    {
                        rec.isSelected = false;
                    }
                }
                NotifyPropertyChanged(nameof(recordingsList));
            }

        }

        /// <summary>
        ///     Raises the <see cref="e_RecordingChanged" /> event.
        /// </summary>
        /// <param name="rce"><see cref="RecordingChangedEventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnRecordingChanged(RecordingChangedEventArgs rce)
        {
            EventHandler<RecordingChangedEventArgs> handler = null;

            lock (_recordingChangedEventLock)
            {
                handler = _recordingChangedEvent;

                if (handler == null)
                    return;
            }

            handler(this, rce);
        }

        /// <summary>
        ///     Raises the <see cref="e_SegmentSelectionChanged" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnSegmentSelectionChanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_segmentSelectionChangedEventLock)
            {
                handler = _segmentSelectionChangedEvent;
            }

            handler?.Invoke(this, e);
        }

        private readonly object _recordingChangedEventLock = new object();

        private readonly SearchableCollection _searchTargets = new SearchableCollection();

        private readonly object _segmentSelectionChangedEventLock = new object();

        private bool _isCallDisplayEnabled = false;

        //private bool _hasMetadata = false;
        private bool _isSegmentSelected;

        private EventHandler<RecordingChangedEventArgs> _recordingChangedEvent;

        private SearchDialog _searchDialog;

        private EventHandler<EventArgs> _segmentSelectionChangedEvent;

        private int _selectedRecordingIndex;

        private LabelledSegment _selectedSegment;

        private RecordingSession _selectedSession;

        private AnalyseAndImportClass analyseAndImport;

        private int currentSelectedRecording;

        //private int currentSelectedSegment;
        private DeepAnalysis deepAnalyser = null;

        //private bool isChanging = false;
        //private double offset = -1.0d;

        private static DataGridCell GetCell(DataGrid recordingsListView, DataGridRow row, int column)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = WPFToolkit.FindVisualChild<DataGridCellsPresenter>(row);
                if (presenter == null)
                {
                    row.ApplyTemplate();
                    presenter = WPFToolkit.FindVisualChild<DataGridCellsPresenter>(row);
                }
                if (presenter != null)
                {
                    DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    if (cell == null)
                    {
                        recordingsListView.ScrollIntoView(row, recordingsListView.Columns[column]);
                        cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    }
                    return (cell);
                }
            }
            return (null);
        }

        private void Aai_e_DataUpdated(object sender, EventArgs e)
        {
            //Tools.FindParent<RecordingSessionListDetailControl>(this).RefreshData();
            if (analyseAndImport.ThisRecording != null && Tools.IsTextFileModified(analyseAndImport.startedAt ?? DateTime.Now, analyseAndImport.ThisRecording))
            {
                var UpdatedSession = analyseAndImport.ThisRecording.RecordingSession;
                DependencyObject d = this;
                while (true)
                {
                    if (d == null) break;
                    if (d.Dispatcher.CheckAccess())
                    {
                        d = VisualTreeHelper.GetParent(d);
                    }
                    else
                    {
                        d.Dispatcher.Invoke(DispatcherPriority.Background,
                            new Action(() => { d = VisualTreeHelper.GetParent(d); }));
                    }

                    if (d == null) break;
                    if (d is RecordingSessionListDetailControl t)
                    {
                        if (t.Dispatcher.CheckAccess())
                        {
                            t.RefreshData(UpdatedSession);
                        }
                        else
                        {
                            t.Dispatcher.Invoke(DispatcherPriority.Background,
                                new Action(() => { t.RefreshData(UpdatedSession); }));
                        }

                        break;
                    }
                }

                /* Unnecessary as RefreshData does a SelectionChanged which forces a refresh of this
                if (Dispatcher.CheckAccess())
                {
                    Refresh();
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new Action(() => { Refresh(); }));
                }*/
            }
        }

        /// <summary>
        ///     Adds the edit recording.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        private void AddEditRecording(Recording recording)
        {
            var oldIndex = RecordingsListView.SelectedIndex;
            if (selectedSession == null) return;
            if (recording == null) recording = new Recording();
            if (recording.RecordingSession == null) recording.RecordingSessionId = selectedSession.Id;

            var recordingForm = new RecordingForm { recording = recording };

            if (recordingForm.ShowDialog() ?? false)
                if (recordingForm.DialogResult ?? false)
                {
                    //DBAccess.UpdateRecording(recordingForm.recording, null);
                }

            UpdateRecordingsList(recording.Id);


            SearchButton.IsEnabled = (selectedSession != null && _selectedSession.Recordings.Count > 0);

        }

        private void AddRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            var recording = new Recording();
            AddEditRecording(recording);

        }

        private void AddSegImgButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedRecording = RecordingsListView.SelectedItem as Recording;
            var processedSegments = new BulkObservableCollection<SegmentAndBatList>();
            var listOfCallImageLists = new BulkObservableCollection<BulkObservableCollection<StoredImage>>();
            StoredImage newImage = null;

            if (button != null && button.Content as string == "Add Segment")
            {
                var segmentForm = new LabelledSegmentForm { labelledSegment = new LabelledSegment() };

                var result = segmentForm.ShowDialog();
                {
                    if (result ?? false)
                    {
                        if (selectedRecording != null)
                            selectedRecording.LabelledSegments.Add(segmentForm.labelledSegment);
                        else
                            return; // dialog was cancelled
                    }
                }
            }
            else if (button != null && button.Content as string == "Add Image")
            {
                var imageDialog = new ImageDialog(
                    _selectedSegment != null
                        ? _selectedSegment.Recording != null ? _selectedSegment.Recording.RecordingName :
                        "image caption"
                        : "image caption",
                    _selectedSegment != null ? _selectedSegment.Comment : "new image");
                var result = imageDialog.ShowDialog();
                result = imageDialog.DialogResult;
                if (result ?? false)
                    newImage = imageDialog.GetStoredImage();
                else
                    return; // dialog was cancelled
            }

            // applies to both types of add...
            if (_selectedSegment?.Recording != null)
            {
                selectedRecording = _selectedSegment.Recording;
            }
            else
            {
                if (RecordingsListView.SelectedItem is Recording)
                    selectedRecording = RecordingsListView.SelectedItem as Recording;
            }

            if (selectedRecording == null) return;
            foreach (var seg in selectedRecording.LabelledSegments)
            {
                var segment = seg;

                var bats = DBAccess.GetDescribedBats(segment.Comment, out string _);
                var segmentLine = Tools.FormattedSegmentLine(segment);
                string autoID = Tools.getAutoIdFromComment(segment.Comment);
                var thisProcessedSegment = SegmentAndBatList.ProcessLabelledSegment(segmentLine, bats, autoID);
                thisProcessedSegment.Segment = segment;
                processedSegments.Add(thisProcessedSegment);
                var segmentImageList = segment.GetImageList();
                if (newImage != null)
                    if (segment.Id == _selectedSegment.Id)
                        segmentImageList.Add(newImage);
                listOfCallImageLists.Add(segmentImageList);
            }

            DBAccess.UpdateRecording(selectedRecording, processedSegments, listOfCallImageLists);
            //this.Parent.RefreshData();
            Tools.FindParent<RecordingSessionListDetailControl>(this).RefreshData(selectedRecording.RecordingSession);
        }

        private void AnalyseRecordingPulses(Recording recording)
        {
            PulseAnalysisWindow pulseWindow = null;
            string[] parameters = new string[2];
            if (recording == null) return;
            using (new WaitCursor())
            {
                string folder = recording.RecordingSession.OriginalFilePath;
                string file = recording.RecordingName;
                try
                {
                    if (folder.EndsWith(@"\") && file.StartsWith(@"\"))
                    {
                        folder = folder.Substring(0, folder.Length - 1);
                    }
                    else if (!folder.EndsWith(@"\") && !file.StartsWith(@"\"))
                    {
                        folder = folder + @"\" + file;
                    }
                    string fqFileName = folder + file;

                    pulseWindow = new PulseAnalysisWindow();
                    //string[] parameters = new string[2];
                    parameters[0] = "BatRecordingManager";
                    parameters[1] = fqFileName;
                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"AnalyseRecordingPulses: settingup recording {recording.GetFileName()}:-{ex.Message}");
                }

                try
                {
                    pulseWindow?.AnalysisControlMain.CommandLineArgs(parameters);
                    pulseWindow?.Show();
                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"AnalyseRecordingPulses: set parameters and Show:-{ex.Message}");
                }
            }
            return;
        }

        /// <summary>
        ///     If the Calls toggle button is Checked then the call parameter data must be
        ///     made visible.  If there is no such data the button should be disabled
        ///     anyway
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CallsToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            isCallDisplayEnabled = true;
        }

        /// <summary>
        ///     Unchecking the Calls toggle button hides the call parameter data associated
        ///     with any relevnat segments.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CallsToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            isCallDisplayEnabled = false;
        }

        private void cmiGenerateSpectrograms_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                Recording recording = RecordingsListView?.SelectedItem as Recording;
                List<LabelledSegment> segments = recording.LabelledSegments.ToList();
                SegmentSonagrams sonagramGenerator = new SegmentSonagrams();
                sonagramGenerator.GenerateForSegments(segments);
            }
        }

        private void cmiRecordingNameAnalyse_Click(object sender, RoutedEventArgs e)
        {
            TextBlock tb = null;
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                tb = ((ContextMenu)mi.Parent).PlacementTarget as TextBlock;
            }
            Recording recording = RecordingsListView?.SelectedItem as Recording;
            if (recording != null)
            {
                AnalyseRecordingPulses(recording);
            }
        }

        private void cmiRecordingNameOpen_Click(object sender, RoutedEventArgs e)
        {
            TextBlock tb = null;
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                tb = ((ContextMenu)mi.Parent).PlacementTarget as TextBlock;
            }
            using (new WaitCursor())
            {
                RecordingNameTextBox_OpenRecording(tb);
            }
        }

        private void ContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as ContentControl)?.Content is TextBlock)
            {
                LabelledSegmentTextBlock_MouseRightButtonUp((sender as ContentControl).Content, e);
                e.Handled = true;
            }
        }

        private void CreateSearchDialog()
        {
            if (_selectedSession?.Recordings?.Any() ?? false)
            {
                _searchDialog = new SearchDialog();
                _searchDialog.e_Searched += SearchDialog_Searched;
                _searchDialog.Closed += SearchDialog_Closed;
                _searchTargets.Clear();/*
            foreach (var recording in _selectedSession.Recordings)
            {
                _searchTargets.Add(recording.Id, -1, recording.RecordingNotes);
                _searchTargets.AddRange(recording.Id, GetSegmentComments(recording));
                _searchDialog.targetStrings = _searchTargets.GetStringCollection();
            }*/

                if (SearchButton != null)
                {
                    SearchButton.IsEnabled = true;
                }
            }
        }

        private void DeepAnalyser_SaveClicked(object sender, EventArgs e)
        {
        }

        /// <summary>
        ///     Handles the Click event of the DeleteRecordingButton control. Deletes the selected
        ///     recording and removes it from the database
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void DeleteRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            var oldIndex = RecordingsListView.SelectedIndex;
            RecordingSession recordingSession = null;
            if (RecordingsListView.SelectedItem != null)
            {
                var recording = RecordingsListView.SelectedItem as Recording;
                recordingSession = recording.RecordingSession;
                DBAccess.DeleteRecording(recording);
                recordingsList.Remove(recording);
                NotifyPropertyChanged(nameof(recordingsList));
                OnRecordingChanged(new RecordingChangedEventArgs(recordingSession));
            }

            if (oldIndex >= 0 && oldIndex < RecordingsListView.Items.Count) RecordingsListView.SelectedIndex = oldIndex;
            SearchButton.IsEnabled = (selectedSession != null && selectedSession.Recordings.Count > 0);

        }

        private void DisableSearchButton()
        {
            SearchButton.IsEnabled = false;
        }

        private void EditRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            var recording = new Recording();
            if (RecordingsListView.SelectedItem != null) recording = RecordingsListView.SelectedItem as Recording;
            AddEditRecording(recording);

            if (selectedSession != null && selectedSession.Recordings.Count > 0)
                SearchButton.IsEnabled = true;
            else
                DisableSearchButton();
        }

        private void ExternalProcess_Exited(object sender, EventArgs e)
        {
            Debug.WriteLine("Exited Analysis");
        }

        /// <summary>
        ///     given an instance of a recording, returns the comments from all the LabelledSegments as an
        ///     ObservableCollection of strings.
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        private BulkObservableCollection<string> GetSegmentComments(Recording recording)
        {
            var result = new BulkObservableCollection<string>();
            foreach (var seg in recording.LabelledSegments) result.Add(seg.Comment);

            return result;
        }

        private List<LabelledSegment> GetSegmentsForSelectedRecordings()
        {
            var result = new List<LabelledSegment>();
            if (RecordingsListView.SelectedItem != null)
            {
                var selectedRecording = RecordingsListView.SelectedItem as Recording;
                foreach (var segment in selectedRecording.LabelledSegments)
                    if ((segment.Duration() ?? new TimeSpan()).Ticks > 0L)
                        result.Add(segment);
            }

            return result;
        }

        private List<LabelledSegment> GetSelectedSegments()
        {
            var result = new List<LabelledSegment>();
            if (_selectedSegment != null) result.Add(_selectedSegment);
            if ((_selectedSegment.Duration() ?? new TimeSpan()).Ticks == 0L) return GetSegmentsForSelectedRecordings();
            return result;
        }

        private void GPSLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var thisLabel = sender as Label;
            var labelContent = thisLabel.Content as string;
            if (!string.IsNullOrWhiteSpace(labelContent))
                if (labelContent.Contains(","))
                {
                    var numbers = labelContent.Split(',');
                    if (numbers.Length >= 2)
                    {
                        double.TryParse(numbers[0].Trim(), out var lat);
                        double.TryParse(numbers[1].Trim(), out var longit);
                        if (Math.Abs(lat) <= 90.0d && Math.Abs(longit) <= 180.0d)
                        {
                            var oldLocation = new Location(lat, longit);
                            var mapWindow = new MapWindow(false);

                            mapWindow.MapControl.ThisMap.Center = oldLocation;
                            mapWindow.MapControl.AddPushPin(oldLocation);

                            mapWindow.Title = mapWindow.Title + " Recording Location";
                            mapWindow.Show();
                        }
                    }
                }
        }

        /// <summary>
        /// event raised when the spectrogram display has the SelectButton toggled on and a point is selected on the bitmap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_pointClickedEvent(object sender, EventArgs e)
        {
            var pe = e as PointEeventArgs;
            Debug.WriteLine($"Point selected at {pe.point.X}, {pe.point.Y}");
            if (deepAnalyser != null)
            {
                deepAnalyser.reAnalyseSegment(pe);
            }
        }

        private void LabelledSegmentListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                };
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void LabelledSegmentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                if (e.AddedItems != null && e.AddedItems.Count > 0)
                {
                    var selectedSegment = e.AddedItems[e.AddedItems.Count - 1] as LabelledSegment;
                    var lvSender = sender as ListView;

                    Debug.WriteLine("LabelledSegmentListView_SelectionChanged:- Selected <" + selectedSegment.Comment +
                                    ">");
                    var images = selectedSegment.GetImageList();
                    OnSegmentSelectionChanged(new ImageListEventArgs(selectedSegment, images));

                    AddSegImgButton.Content = "Add Image";
                    AddSegImgButton.IsEnabled = true;
                    _isSegmentSelected = true;
                    _selectedSegment = selectedSegment;
                    var isRecordingSelected = false;
                    if (RecordingsListView.SelectedItem != null)
                        if (selectedSegment.RecordingID == (RecordingsListView.SelectedItem as Recording).Id)
                            isRecordingSelected = true;
                    if (!isRecordingSelected)
                    {
                        RecordingsListView.UnselectAll();
                        foreach (var rec in recordingsList)
                        {
                            if (rec.Id == selectedSegment.RecordingID) rec.isSelected = true;
                            else rec.isSelected = false;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("LabelledSegmentListView_SelectionChanged-RESET");
                    AddSegImgButton.Content = "Add Segment";
                    _isSegmentSelected = false;
                    _selectedSegment = null;
                }
            }
        }

        private void LabelledSegmentTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            if (CallsToggleButton.IsChecked ?? false)
                if (sender is TextBlock segTextBlock)
                {
                    var callControl = new BatCallControl();

                    var seg = segTextBlock.DataContext as LabelledSegment;
                    if (seg.SegmentCalls != null && seg.SegmentCalls.Count > 0)
                    {
                        var call = seg.SegmentCalls?.Last()?.Call;
                        if (call != null) callControl.BatCall = call;
                        ((segTextBlock.Parent as ContentControl).Parent as StackPanel).Children.Add(callControl);
                    }
                }
        }

        private void LabelledSegmentTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            if (CallsToggleButton.IsChecked ?? false)
                if (sender is TextBlock segTextBlock)

                    for (var i = ((segTextBlock.Parent as ContentControl).Parent as StackPanel).Children.Count - 1; i >= 0; i--)
                    {
                        var child = ((segTextBlock.Parent as ContentControl).Parent as StackPanel).Children[i];
                        if (child.GetType() == typeof(BatCallControl))
                            ((segTextBlock.Parent as ContentControl).Parent as StackPanel).Children.RemoveAt(i);
                    }
        }

        private void LabelledSegmentTextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            var selectedSegment = textBlock.DataContext as LabelledSegment;

            Debug.WriteLine("right clicked on:-" + selectedSegment.Recording.RecordingName + " at " +
                            selectedSegment.StartOffset + " - " + selectedSegment.EndOffset);
            var wavFile = selectedSegment.Recording.RecordingSession.OriginalFilePath +
                          selectedSegment.Recording.RecordingName;
            wavFile = wavFile.Replace(@"\\", @"\");
            if (File.Exists(wavFile) && (new FileInfo(wavFile).Length > 0L))
                Tools.OpenWavFile(wavFile, selectedSegment.StartOffset, selectedSegment.EndOffset);
            e.Handled = true;
        }

        /// <summary>
        /// Opens the add image dialog with a caption for this recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miAddImage_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecording = RecordingsListView.SelectedItem as Recording;
            currentSelectedRecording = (RecordingsListView?.SelectedIndex) ?? -1;
            var processedSegments = new BulkObservableCollection<SegmentAndBatList>();
            var listOfCallImageLists = new BulkObservableCollection<BulkObservableCollection<StoredImage>>();
            StoredImage newImage = null;

            var imageDialog = new ImageDialog(
                    _selectedSegment != null
                        ? _selectedSegment.Recording != null ? _selectedSegment.Recording.RecordingName :
                        "image caption"
                        : "image caption",
                    _selectedSegment != null ? _selectedSegment.Comment : "new image");
            var result = imageDialog.ShowDialog();
            result = imageDialog.DialogResult;
            if (result ?? false)
                newImage = imageDialog.GetStoredImage();
            else
                return; // dialog was cancelled

            // applies to both types of add...
            if (_selectedSegment?.Recording != null)
            {
                selectedRecording = _selectedSegment.Recording;
            }
            else
            {
                if (RecordingsListView.SelectedItem is Recording)
                    selectedRecording = RecordingsListView.SelectedItem as Recording;
            }

            if (selectedRecording == null) return;
            foreach (var seg in selectedRecording.LabelledSegments)
            {
                var segment = seg;

                var bats = DBAccess.GetDescribedBats(segment.Comment, out string _);
                var segmentLine = Tools.FormattedSegmentLine(segment);
                string autoID = Tools.getAutoIdFromComment(segment.Comment);
                var thisProcessedSegment = SegmentAndBatList.ProcessLabelledSegment(segmentLine, bats, autoID);
                thisProcessedSegment.Segment = segment;
                processedSegments.Add(thisProcessedSegment);
                var segmentImageList = segment.GetImageList();
                if (newImage != null)
                    if (segment.Id == _selectedSegment.Id)
                        segmentImageList.Add(newImage);
                listOfCallImageLists.Add(segmentImageList);
            }

            Recording recording = DBAccess.UpdateRecording(selectedRecording, processedSegments, listOfCallImageLists);
            if (recording != null)
            {
                UpdateRecordingsList(recording.Id);
            }


        }

        /// <summary>
        /// extract the segment from the recording .wav file and perform a deep analysis on it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miAnalyseSegment_Click(object sender, RoutedEventArgs e)
        {
            var _sender = sender as MenuItem;
            var selection = GetSelectedSegments();
            if (selection.IsNullOrEmpty()) selection = GetSegmentsForSelectedRecordings();
            if (!selection.IsNullOrEmpty())
            {
                currentSelectedRecording = (RecordingsListView?.SelectedIndex) ?? -1;
                var recId = (selection?.First()?.RecordingID) ?? -1;
                deepAnalyser = new DeepAnalysis();
                deepAnalyser.SaveClicked += DeepAnalyser_SaveClicked;
                var sel = selection.FirstOrDefault();
                int AnalysisMode = 0;
                if (_sender.Name.Contains("1p")) AnalysisMode = 1;
                if (_sender.Name.Contains("5p")) AnalysisMode = 5;
                if (deepAnalyser.AnalyseSegment(sel, AnalysisMode))
                {
                    List<LabelledSegment> segList = new List<LabelledSegment>();
                    segList.Add(sel);
                    BitmapSource bmps = deepAnalyser.GetImage();
                    var sonagramGenerator = new SegmentSonagrams();
                    var image = sonagramGenerator.GenerateForSegment(sel, deepAnalyser.param);
                    //sel = DBAccess.GetLabelledSegment(sel.Id);
                    //var images = sel.GetImageList();
                    //if (images.Any())
                    //{
                    //ComparisonHost.Instance.AddImage(image);
                    //}

                    if (image != null)
                    {
                        image.DisplayActualSize = true;
                        if (image.segmentsForImage == null) image.segmentsForImage = new List<LabelledSegment>();
                        if (!image.segmentsForImage.Contains(sel))
                        {
                            image.segmentsForImage.Add(sel);
                        }
                        image.IsSelectable = true;
                        image.pointClickedEvent += Image_pointClickedEvent;
                        ComparisonHost.Instance.AddImage(image);
                    }
                }


                UpdateRecordingsList(recId);


            }
        }

        /// <summary>
        /// replaces the recording in the recordingslist with <see langword="abstract"/>newly derived one from the database
        /// or adds the recording to the list if not already present
        /// </summary>
        /// <param name="recordingID"></param>
        private void UpdateRecordingsList(int recordingID)
        {
            if (recordingID >= 0)
            {
                var recording = DBAccess.GetRecording(recordingID);

                if (recording != null)
                {
                    recording.isSelected = true;

                    var existing = recordingsList.Where(rec => rec.Id == recording.Id).FirstOrDefault();
                    if (existing != null)
                    {
                        int i = recordingsList.IndexOf(existing);
                        recordingsList[i] = recording;
                        RecordingsListView.SelectedIndex = i;
                    }
                    else
                    {
                        recordingsList.Add(recording);
                        RecordingsListView.SelectedIndex = recordingsList.Count - 1;
                    }
                }

                OnRecordingChanged(new RecordingChangedEventArgs(recording?.RecordingSession));
                NotifyPropertyChanged(nameof(recordingsList));
            }
        }

        /// <summary>
        /// Opens a new window to display and manage data on all call details associated with the currently
        /// selected segment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miDisplayCalls_Click(object sender, RoutedEventArgs e)
        {
            CallDataDisplayWindow window = new CallDataDisplayWindow();
            var selecteddSegment = _selectedSegment;
            window.setSegmentToDisplay(selecteddSegment);
            window.ShowDialog();
        }

        private void miDisplayMetaData_Click(object sender, RoutedEventArgs e)
        {
            Recording recording = _selectedSegment?.Recording;
            MetaDataDialog metadataDialog = new MetaDataDialog();
            metadataDialog.recording = recording;
            metadataDialog.ShowDialog();
        }

        private void miGenerateSegSpectrograms_Click(object sender, RoutedEventArgs e)
        {
            currentSelectedRecording = RecordingsListView.SelectedIndex;
            bool experimental = false;
            if (Keyboard.IsKeyDown(Key.LeftCtrl)) experimental = true;

            var selection = GetSelectedSegments();
            if (selection != null)
            {
                var recordingId = (selection?.First()?.RecordingID) ?? -1;
                SegmentSonagrams sonagramGenerator = new SegmentSonagrams();
                sonagramGenerator.GenerateForSegments(selection, experimental);
                UpdateRecordingsList(recordingId);
            }
        }

        /// <summary>
        /// Opens the selected segment in Audacity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miOpenSegment_Click(object sender, RoutedEventArgs e)
        {
            var selectedSegment = _selectedSegment;

            Debug.WriteLine("right clicked on:-" + selectedSegment.Recording.RecordingName + " at " +
                            selectedSegment.StartOffset + " - " + selectedSegment.EndOffset);
            var wavFile = selectedSegment.Recording.RecordingSession.OriginalFilePath +
                          selectedSegment.Recording.RecordingName;
            wavFile = wavFile.Replace(@"\\", @"\");
            if (File.Exists(wavFile) && (new FileInfo(wavFile).Length > 0L))
                Tools.OpenWavFile(wavFile, selectedSegment.StartOffset, selectedSegment.EndOffset);
            e.Handled = true;
        }

        /// <summary>
        /// Opens the play dialog primed for the selected segment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miPlaySegment_Click(object sender, RoutedEventArgs e)
        {
            var selection = GetSelectedSegments();
            if (selection.IsNullOrEmpty()) selection = GetSegmentsForSelectedRecordings();
            if (!selection.IsNullOrEmpty())
                foreach (var sel in selection)
                    AudioHost.Instance.audioPlayer.AddToList(sel);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            //var playWindow = new AudioPlayer();
            //playWindow.Show();

            var selection = GetSelectedSegments();
            if (selection.IsNullOrEmpty()) selection = GetSegmentsForSelectedRecordings();
            if (!selection.IsNullOrEmpty())
                foreach (var sel in selection)
                    AudioHost.Instance.audioPlayer.AddToList(sel);
        }

        /// <summary>
        ///     Populates the segment list. The recordingSessionControl has been automatically
        ///     updated by writing the selected session to it. This function uses the selected
        ///     recordingSession to fill in the list of LabelledSegments.
        /// </summary>
        private void PopulateRecordingsList()
        {
            // TODO each recording will give access to a passes summary these should be merged into
            // a session summary and each 'bat' in the summary must be added to the SessionSummaryStackPanel

            if (selectedSession != null)
            {
                //virtualRecordingsList.Clear();
                //recordingsList.AddRange(DBAccess.GetRecordingsForSession(selectedSession));
                //virtualRecordingsList.AddRange(selectedSession.Recordings);
            }
            else
            {
                //virtualRecordingsList.Clear();
            }

            SearchButton.IsEnabled = (_selectedSession != null && _selectedSession.Recordings.Any());
        }

        private void RecordingNameContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (new WaitCursor())
            {
                RecordingNameTextBox_MouseDoubleClick(sender, e);
                e.Handled = true;
            }
        }

        private void RecordingNameTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RecordingNameTextBox_OpenRecording(sender as TextBlock);
        }

        private void RecordingNameTextBox_OpenRecording(TextBlock thisTextBox)
        {
            //var thisTextBox = sender as TextBlock;
            var recording = RecordingsListView.SelectedItem as Recording;
            if (!File.Exists(recording.RecordingSession.OriginalFilePath + recording.RecordingName))
            {
                thisTextBox.Foreground = Brushes.Red;
                return;
            }

            analyseAndImport = new AnalyseAndImportClass(recording);
            analyseAndImport.e_DataUpdated += Aai_e_DataUpdated;
            analyseAndImport.AnalyseRecording();
        }

        private void RecordingsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!e.Handled)
            {
                if (RecordingsListView.SelectedItem != null)
                {
                    var selectedRecording = RecordingsListView.SelectedItem as Recording;
                    selectionIsWavFile = selectedRecording?.isWavRecording ?? false;
                    currentSelectedRecording = RecordingsListView.SelectedIndex;
                }
                else
                {
                    currentSelectedRecording = RecordingsListView.SelectedIndex;
                    selectionIsWavFile = true;
                }

                e.Handled = true;
                if (e.AddedItems != null && e.AddedItems.Count > 0)
                {
                    if (AddRecordingButton != null && DeleteRecordingButton != null && EditRecordingButton != null)
                    {
                        EditRecordingButton.IsEnabled = true;
                        DeleteRecordingButton.IsEnabled = true;

                        if (!_isSegmentSelected)
                        {
                            AddSegImgButton.Content = "Add Segment";
                            AddSegImgButton.IsEnabled = true;
                            Debug.WriteLine("Recording Selected");
                        }


                    }
                }
                else
                {
                    if (AddRecordingButton != null && DeleteRecordingButton != null && EditRecordingButton != null)
                    {
                        EditRecordingButton.IsEnabled = false;
                        DeleteRecordingButton.IsEnabled = false;

                        AddSegImgButton.IsEnabled = false;
                        Debug.WriteLine("Recording De-Selected");
                        _isSegmentSelected = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Searches the comments in this session for matching strings.
        ///     The search string may be a regular expression.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_searchDialog == null)
            {
                CreateSearchDialog();
            }
            if (_searchDialog.IsLoaded)
            {
                _searchDialog.Visibility = Visibility.Visible;
                _searchDialog.Focus();
                _searchDialog.FindNextButton_Click(sender, e);
            }
            else
            {
                _searchDialog = null;
                CreateSearchDialog();
                _searchDialog.Show();
            }

            UpdateLayout();
            _searchDialog.Activate();
        }

        //private double vo = -1.0;
        private void SearchDialog_Closed(object sender, EventArgs e)
        {
            //CreateSearchDialog();
        }

        /// <summary>
        ///     If the search dialog is activated, this event indicates that a search
        ///     has been performed.  If successful the args will contain the found string,
        ///     the search pattern and the index of the string in the string collection
        ///     that was used to initialize the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchDialog_Searched(object sender, EventArgs e)
        {
            try
            {
                var seArgs = e as SearchedEventArgs;
                //Debug.WriteLine("Found:- " + seArgs.searchPattern + " in " + seArgs.foundItem + " @ " + seArgs.indexOfFoundItem);
                //Debug.WriteLine("Resolves to:- " + searchTargets.searchableCollection[seArgs.indexOfFoundItem].ToString());

                //ReWritten Dec2020 for revised Control structure with DataGrid

                var itemFound = _searchTargets.ItemAt(seArgs.IndexOfFoundItem);
                if (itemFound != null)
                {
                    Recording foundRecording = null;
                    int recordingId = itemFound.Item1;
                    int segmentIndex = itemFound.Item2;
                    if (selectedSession != null)
                    {
                        var recordings = from rec in selectedSession.Recordings
                                         where rec.Id == recordingId
                                         select rec;
                        if (!recordings.IsNullOrEmpty()) foundRecording = recordings.First();
                        RecordingsListView.Focus();
                        foundRecording.isSelected = true;
                        RecordingsListView.ScrollIntoView(foundRecording);
                    }

                    if (selectedSession != null)
                    {
                        var recordings = from rec in selectedSession.Recordings
                                         where rec.Id == recordingId
                                         select rec;
                        if (!recordings.IsNullOrEmpty()) foundRecording = recordings.First();
                        RecordingsListView.Focus();
                        RecordingsListView.ScrollIntoView(foundRecording);
                        foundRecording.isSelected = true;
                    }

                    if (segmentIndex >= 0)
                    {
                        var foundSegment = foundRecording.LabelledSegments[segmentIndex];
                        if (foundSegment != null)
                        {
                            var currentSelectedListBoxItem =
                                RecordingsListView.ItemContainerGenerator.ContainerFromIndex(RecordingsListView
                                    .SelectedIndex) as ListViewItem;
                            var lsegListView = Tools.FindDescendant<ListView>(currentSelectedListBoxItem);
                            if (lsegListView != null)
                            {
                                lsegListView.UnselectAll();
                                lsegListView.SelectedItem = foundSegment;
                                foundSegment.isSelected = true;
                                //lsegListView.ScrollIntoView(foundSegment);
                                var lvi = (ListViewItem)lsegListView.ItemContainerGenerator.ContainerFromItem(lsegListView
                                    .SelectedItem);
                                OnListViewItemFocused(lvi, new RoutedEventArgs());
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Search internal error:-" + ex);
            }
            finally
            {
                (sender as SearchDialog).Focus();
            }
        }

        private void SelectRecordingAndSegment(int currentRecordingIndex, LabelledSegment segment)
        {
            if (currentRecordingIndex < 0 && segment != null && RecordingsListView.ItemsSource != null)
            {
                return;
            }
            if (!RecordingsListView.SelectionUnit.Equals(DataGridSelectionUnit.FullRow))
            {
                return;
            }
            if (currentRecordingIndex < 0 || (currentRecordingIndex > RecordingsListView.Items.Count - 1))
            {
                return;
            }

            RecordingsListView.UnselectAll();
            var item = RecordingsListView.Items[currentRecordingIndex] as Recording;
            item.isSelected = true;
            if (item.LabelledSegments != null)
            {
                foreach (var seg in item.LabelledSegments)
                {
                    if (seg.Id == segment.Id)
                    {
                        seg.isSelected = true;
                    }
                    else
                    {
                        seg.isSelected = false;
                    }
                }
            }

            var row = RecordingsListView.ItemContainerGenerator.ContainerFromIndex(currentRecordingIndex) as DataGridRow;
            if (row == null)
            {
                RecordingsListView.ScrollIntoView(item);
                row = RecordingsListView.ItemContainerGenerator.ContainerFromIndex(currentRecordingIndex) as DataGridRow;
            }

            if (row != null)
            {
                DataGridCell cell = GetCell(RecordingsListView, row, 0);
                if (cell != null)
                {
                    cell.Focus();
                }
            }
        }

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
    } // End of Class RecordingDetailListControl

    public class RecordingChangedEventArgs : EventArgs
    {
        public RecordingChangedEventArgs(RecordingSession recordingSession)
        {
            this.recordingSession = recordingSession;
        }

        public RecordingSession recordingSession { get; set; }
    }

    #region RecordingToGPSConverter (ValueConverter)

    /// <summary>
    ///     Converter to extract GPS data from a recording instance and format it into a string
    /// </summary>
    public class RecordingToGpsConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var result = "";
                if (value is Recording recording)
                {
                    if (!string.IsNullOrWhiteSpace(recording.RecordingGPSLatitude) &&
                        !string.IsNullOrWhiteSpace(recording.RecordingGPSLongitude))
                    {
                        result = recording.RecordingGPSLatitude + ", " + recording.RecordingGPSLongitude;
                    }
                    else
                    {
                        if (recording.RecordingEndTime != null && recording.RecordingStartTime != null)
                            result = recording.RecordingStartTime.Value.ToString(@"hh\:mm\:ss") + " - " +
                                     recording.RecordingEndTime.Value.ToString(@"hh\:mm\:ss");
                    }
                }

                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     Not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RecordingToGPSConverter (ValueConverter)

    #region RecordingDetailsConverter (ValueConverter)

    /// <summary>
    ///     Converts the essential details of a Recording instance to a string
    /// </summary>
    public class RecordingDetailsConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var result = "";

                if (value is Recording recording)
                {
                    var dur = new TimeSpan();

                    dur = Tools.GetRecordingDuration(recording);

                    var durStr = dur.ToString(@"dd\:hh\:mm\:ss");
                    while (durStr.StartsWith("00:")) durStr = durStr.Substring(3);
                    var recDate = "";
                    if (recording.RecordingDate != null)
                    {
                        recDate = recording.RecordingDate.Value.ToShortDateString();
                        if (recording.RecordingDate.Value.Hour > 0 || recording.RecordingDate.Value.Minute > 0 ||
                            recording.RecordingDate.Value.Second > 0)
                            recDate = recDate + " " + recording.RecordingDate.Value.ToShortTimeString();
                    }

                    result = recording.RecordingName + " " + recDate + " " + durStr;
                }

                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RecordingDetailsConverter (ValueConverter)

    #region RecordingDurationConverter (ValueConverter)

    /// <summary>
    ///     Converts the essential details of a Recording instance to a string
    /// </summary>
    public class RecordingDurationConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <see langword="null" />, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var result = "";

                if (value is Recording recording)
                {
                    var dur = new TimeSpan();

                    dur = Tools.GetRecordingDuration(recording);

                    var durStr = dur.ToString(@"dd\:hh\:mm\:ss");
                    while (durStr.StartsWith("00:")) durStr = durStr.Substring(3);
                    var recDate = "";
                    if (recording.RecordingDate != null)
                    {
                        recDate = recording.RecordingDate.Value.ToShortDateString();
                        if (recording.RecordingDate.Value.Hour > 0 || recording.RecordingDate.Value.Minute > 0 ||
                            recording.RecordingDate.Value.Second > 0)
                            recDate = recDate + " " + recording.RecordingDate.Value.ToShortTimeString();
                    }

                    result = durStr;
                }

                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <see langword="null" />, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RecordingDurationConverter (ValueConverter)

    #region RecordingPassSummaryConverter (ValueConverter)

    /// <summary>
    ///     From an instance of Recording provides a list of strings summarising the number of
    ///     passes organised by type of bat
    /// </summary>
    public class RecordingPassSummaryConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var result = "";
                if (value is Recording recording)
                {
                    var summary = recording.GetStats();
                    if (summary != null && summary.Count > 0)
                        foreach (var batType in summary)
                            result = result + "\n" + Tools.GetFormattedBatStats(batType, false);
                }

                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion RecordingPassSummaryConverter (ValueConverter)
}