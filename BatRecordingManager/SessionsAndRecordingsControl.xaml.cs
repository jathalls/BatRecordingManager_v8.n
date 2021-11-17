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

using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BatRecordingManager
{
    /// <summary>
    ///     special class to combine an instance of a recording with a specific bat so
    ///     that the combined object can be the source fot a datagrid and elements of the
    ///     recording that refer to the named bat can be displayed in the grid
    /// </summary>
    public class BatRecording
    {
        /// <summary>
        ///     default constructor
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="bat"></param>
        public BatRecording(Recording recording, Bat bat)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                this.recording = recording;
                this.bat = bat;
                if (recording != null && bat != null)
                {
                    imageCount = (from seg in recording.LabelledSegments
                                  from link in seg.BatSegmentLinks
                                  where !(link.ByAutoID ?? false) && link.BatID == bat.Id
                                  select seg.SegmentDatas.Count).Sum();

                    segmentCountForBat = (from seg in recording.LabelledSegments
                                          from lnk in seg.BatSegmentLinks
                                          where !(lnk.ByAutoID ?? false) && lnk.BatID == bat.Id
                                          select seg).Count();
                }

                //segmentCountForBat = recording.GetSegmentCount(bat);
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        ///     bat for this bat/recording pair
        /// </summary>
        public Bat bat { get; set; }

        /// <summary>
        ///     number of images in the recordings with this bat
        /// </summary>
        public int imageCount { get; }

        /// <summary>
        ///     recording for this bat/recording pair
        /// </summary>
        public Recording recording { get; set; }

        /// <summary>
        ///     number of segments in this recording with this bat
        /// </summary>
        public int segmentCountForBat { get; }
    }

    /// <summary>
    ///     special class to combine an instance of a RecordingSession with a specific bat
    ///     so that the the combination can be used as the ItemSource for a DataGrid and
    ///     that datagrid can display only infomration relevant to the selected bat.
    /// </summary>
    public class BatSession
    {
        /// <summary>
        ///     A composite class with data about recordings with a specified bat in the specified session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="bat"></param>
        public BatSession(RecordingSession session, Bat bat)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                this.session = session;
                this.bat = bat;
                imageCount = 0;
                //var segmentDatasForBat = DBAccess.GetImportedSegmentDatasForBat(bat);
                batRecordings.Clear();
                //foreach (var rec in session.Recordings)
                //{
                /*
                bool recordingHasBat = false;
                imageCount += rec.GetImageCount(bat,out recordingHasBat); // includes imported image count
                if (recordingHasBat)
                {
                    batRecordings.Add(rec);
                }*/
                var recordingsWithBat = from rec in session.Recordings
                                        from brLink in rec.BatRecordingLinks
                                        where !(brLink.ByAutoID ?? false) && brLink.BatID == bat.Id
                                        select rec;

                imageCount = (from rec in recordingsWithBat
                              from seg in rec.LabelledSegments
                              from lnk in seg.BatSegmentLinks
                              where !(lnk.ByAutoID ?? false) && lnk.BatID == bat.Id
                              select seg.SegmentDatas.Count).Sum();
                batRecordings.AddRange(recordingsWithBat);

                //}
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        ///     The bat for this bat/session pair
        /// </summary>
        public Bat bat { get; set; }

        /// <summary>
        ///     collection of recordings related to a specific bat
        /// </summary>
        public BulkObservableCollection<Recording> batRecordings { get; set; } =
            new BulkObservableCollection<Recording>();

        /// <summary>
        ///     The number of images for recordings with this bat
        /// </summary>
        public int imageCount { get; }

        /// <summary>
        ///     the session for this bat/session pair
        /// </summary>
        public RecordingSession session { get; set; }
    }

    /// <summary>
    ///     Provides arguments for an event.
    /// </summary>
    [Serializable]
    public class SessionActionEventArgs : EventArgs
    {
        /// <summary>
        ///     The empty
        /// </summary>
        public new static readonly SessionActionEventArgs Empty = new SessionActionEventArgs("");

        #region Public Properties

        /// <summary>
        ///     The recording session
        /// </summary>
        public RecordingSession recordingSession { get; set; }

        public int RecordingSessionId => recordingSession.Id;

        #endregion Public Properties

        #region Constructors

        /// <summary>
        ///     Constructs a new instance of the <see cref="SessionActionEventArgs" /> class.
        /// </summary>
        public SessionActionEventArgs(RecordingSession session)
        {
            recordingSession = session;
        }

        /// <summary>
        ///     Constructor using just the sessiontag to retrieve the full recording session
        /// </summary>
        /// <param name="sessionTag"></param>
        public SessionActionEventArgs(string sessionTag)
        {
            recordingSession = DBAccess.GetRecordingSession(sessionTag);
        }

        #endregion Constructors
    }

    /// <summary>
    ///     Interaction logic for SessionsAndRecordingsControl.xaml
    /// </summary>
    public partial class SessionsAndRecordingsControl : UserControl
    {
        /// <summary>
        ///     The selected bat identifier
        /// </summary>
        public int SelectedBatId = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SessionsAndRecordingsControl" /> class.
        /// </summary>
        public SessionsAndRecordingsControl()
        {
            RecordingImageScroller = Activator.CreateInstance<ImageScrollerControl>();
            InitializeComponent();
            DataContext = this;

            //SessionsDataGrid.ItemsSource = matchingSessions;

            SessionsDataGrid.EnableColumnVirtualization = true;
            SessionsDataGrid.EnableRowVirtualization = true;
            //RecordingsDataGrid.ItemsSource = matchingRecordings;
            //RecordingsDataGrid.EnableColumnVirtualization = true;
            //RecordingsDataGrid.EnableRowVirtualization = true;
            RecordingImageScroller.Title = "Recording Images";
            RecordingImageScroller.IsReadOnly = true;
            //BindingOperations.EnableCollectionSynchronization(matchingRecordingData, matchingRecordingData);
        }

        /// <summary>
        ///     Event raised after the session property value has changed.
        /// </summary>
        public event EventHandler<SessionActionEventArgs> e_SessionAction
        {
            add
            {
                lock (_sessionActionEventLock)
                {
                    _sessionActionEvent += value;
                }
            }
            remove
            {
                lock (_sessionActionEventLock)
                {
                    _sessionActionEvent -= value;
                }
            }
        }

        public BulkObservableCollection<BatSessionRecordingData> matchingRecordingData { get; } = new BulkObservableCollection<BatSessionRecordingData>();
        //public BulkObservableCollection<BatSessionRecordingData> matchingRecordingData { get; set; }

        // <summary>
        //     A list of all the recordings in the selected sessions
        // </summary>
        //public BulkObservableCollection<BatRecording> matchingRecordings { get; } = new BulkObservableCollection<BatRecording>();

        /// <summary>
        ///     list of tailored class of items containg displayable data for the sessions list
        /// </summary>
        public BulkObservableCollection<BatSessionData> matchingSessionData { get; } =
            new BulkObservableCollection<BatSessionData>();

        /// <summary>
        ///     Gets or sets the selected bat details.
        /// </summary>
        /// <value>
        ///     The selected bat details.
        /// </value>
        public BatStatistics SelectedBatDetails
        {
            get => _selectedBatDetails;
            set
            {
                using (new WaitCursor("Changed selected bat"))
                {
                    _selectedBatDetails = value;
                    //matchingSessions.Clear();
                    matchingSessionData.Clear();
                    //matchingRecordings.Clear();
                    //matchingRecordingData.Clear();

                    //imageScroller.Clear();
                    if (value != null)
                    {
                        //matchingSessions.AddRange(value.sessions);
                        //foreach(var bsLink in value.bat.BatSessionLinks)
                        //{
                        //    matchingSessions.Add(new BatSession(bsLink.RecordingSession, value.bat));
                        //}
                        matchingSessionData.AddRange(DBAccess.GetBatSessionData(value.bat.Id));
                        SessionsDataGrid.SelectAll();

                        //matchingSessions.AddRange(value.sessions);
                        //SessionsDataGrid.Items.Refresh();
                        //NB returns image and segments count a stotals not those specific to the bat
                        /*
                        foreach (var brLink in value.bat.BatRecordingLinks)
                        {
                            bool recordingHasBat = false;
                            var data = new BatSessionRecordingData(brLink.Recording.RecordingSessionId, brLink.RecordingID, brLink.BatID,
                                brLink.Recording.RecordingName, brLink.Recording.RecordingDate, brLink.Recording.RecordingStartTime,
                                brLink.Recording.LabelledSegments.Count(),
                                brLink.Recording.GetImageCount(brLink.Bat, out recordingHasBat));
                            matchingRecordingData.Add(data);
                        }
                        //matchingRecordings.AddRange(value.recordings);

                        RecordingsDataGrid.Items.Refresh();*/
                    }
                }
            }
        }

        /// <summary>
        ///     Accommodates multiple selections of BatStatistics to populate the sessions panel
        /// </summary>
        public BulkObservableCollection<BatStatistics> SelectedBatDetailsList
        {
            get => _selectedBatDetailsList;

            internal set
            {
                using (new WaitCursor("Changed select bat details"))
                {
                    _selectedBatDetailsList = value;
                }
            }
        }

        /// <summary>
        ///     Returns a list of the selected recordings or if no recordings are selected
        ///     all of the displayed list of recordings.
        /// </summary>
        /// <returns></returns>
        internal List<Recording> GetSelectedRecordings()
        {
            var result = new List<Recording>();
            if (RecordingsDataGrid.SelectedItems != null && RecordingsDataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in RecordingsDataGrid.SelectedItems)
                {
                    if (item == null || ((item as BatSessionRecordingData).RecordingId ?? -1) < 0) continue;
                    result.Add(DBAccess.GetRecording((item as BatSessionRecordingData).RecordingId ?? -1));
                }
            }
            else
            {
                foreach (var item in RecordingsDataGrid.Items)
                {
                    if (item == null || ((item as BatSessionRecordingData).RecordingId ?? -1) < 0) continue;
                    result.Add(DBAccess.GetRecording((item as BatSessionRecordingData).RecordingId ?? -1));
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns a lest of the selected sessions, or if no sessions are selected
        ///     all of the displayed sessions.
        /// </summary>
        /// <returns></returns>
        internal List<RecordingSession> GetSelectedSessions()
        {
            var result = new List<RecordingSession>();
            if (SessionsDataGrid.SelectedItems != null && SessionsDataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in SessionsDataGrid.SelectedItems)
                {
                    if (item == null || ((item as BatSessionData).id) < 0) continue;
                    result.Add(DBAccess.GetRecordingSession((item as BatSessionData).id));
                }
            }
            else
            {
                foreach (var item in SessionsDataGrid.Items)
                {
                    if (item == null || ((item as BatSessionData).id) < 0) continue;
                    result.Add(DBAccess.GetRecordingSession((item as BatSessionData).id));
                }
            }

            return result;
        }

        /// <summary>
        ///     forces a select all for sessions data grid
        /// </summary>
        internal void SelectAll()
        {
            SessionsDataGrid.SelectAll();
        }

        internal void SetMatchingSessionsAndRecordings()
        {
            //matchingSessions.Clear();
            matchingSessionData.Clear();
            //matchingRecordings.Clear();

            foreach (var bs in SelectedBatDetailsList)
                matchingSessionData.AddRange(DBAccess.GetBatSessionData(bs.bat.Id));
            SessionsDataGrid.SelectAll();
        }

        /// <summary>
        ///     Raises the <see cref="e_SessionAction" /> event.
        /// </summary>
        /// <param name="e">
        ///     <see cref="SessionActionEventArgs" /> object that provides the arguments for the event.
        /// </param>
        protected virtual void OnSessionAction(SessionActionEventArgs e)
        {
            EventHandler<SessionActionEventArgs> handler = null;

            lock (_sessionActionEventLock)
            {
                handler = _sessionActionEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private readonly object _sessionActionEventLock = new object();

        private readonly AnalyseAndImportClass aai = null;
        private ImportPictureDialog _importPictureDialog;
        private bool _isPictureDialogOpen;
        private BatStatistics _selectedBatDetails;

        private BulkObservableCollection<BatStatistics> _selectedBatDetailsList =
            new BulkObservableCollection<BatStatistics>();

        private EventHandler<SessionActionEventArgs> _sessionActionEvent;

        //public BatAndCallImageScrollerControl imageScroller { get; internal set; }
        private Recording thisRecording { get; set; } = null;

        private void Aai_e_DataUpdated(object sender, EventArgs e)
        {
            if (thisRecording != null && aai != null)
            {
                if (Tools.IsTextFileModified(aai.startedAt ?? DateTime.Now, thisRecording))
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            new Action(() => { RefreshParentData(); }));
                    }
                    else
                    {
                        RefreshParentData();
                    }
                }
            }
        }

        private void ImportPictureDialog_Closed(object sender, EventArgs e)
        {
            _isPictureDialogOpen = false;
        }

        private void MatchingRecordingData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("recording data changed:-" + e.PropertyName);
        }

        private void miExportFiles_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((RecordingsDataGrid.SelectedItems.Count) <= 0) return;
            string folder = Tools.SelectWavFileFolder("");
            if (string.IsNullOrWhiteSpace(folder)) return;
            if (!Directory.Exists(folder)) return;
            foreach (var obj in RecordingsDataGrid.SelectedItems)
            {
                var bsrd = obj as BatSessionRecordingData;
                var session = DBAccess.GetRecordingSession(bsrd?.SessionId ?? -1);
                var parentFolder = session?.OriginalFilePath;
                if (!Directory.Exists(parentFolder))
                {
                    Debug.WriteLine($"Session folder <{parentFolder}> does not exist");
                    continue;
                }
                if (!File.Exists(parentFolder + bsrd.RecordingName))
                {
                    Debug.WriteLine($"Recording <{bsrd.RecordingName}> does not exist in folder <{parentFolder}>");
                    continue;
                }
                string filename = parentFolder + bsrd?.RecordingName;
                AppFilter.TransferFile(filename, folder, false);
            }
        }

        private void miGenerateRecordingSpectrograms_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                if (RecordingsDataGrid.SelectedItems != null && RecordingsDataGrid.SelectedItems.Any())
                {
                    foreach (var recordingData in RecordingsDataGrid.SelectedItems)
                    {
                        BatSessionRecordingData bsrd = recordingData as BatSessionRecordingData;
                        List<LabelledSegment> segList = DBAccess.GetBatRecordingSegments(bsrd.RecordingId, SelectedBatId);
                        if (!segList.IsNullOrEmpty())
                        {
                            SegmentSonagrams sonagramGenerator = new SegmentSonagrams();
                            sonagramGenerator.GenerateForSegments(segList);
                        }
                    }
                    this.RefreshParentData();
                }
            }
        }

        /// <summary>
        /// Generates spectrograms for all the segments in the selected sessions which contain the selected bat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miGenerateSessionSpectrograms_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                if (SessionsDataGrid.SelectedItems != null && SessionsDataGrid.SelectedItems.Any())
                {
                    foreach (var sessionData in SessionsDataGrid.SelectedItems)
                    {
                        BatSessionData bsd = sessionData as BatSessionData;
                        var tag = bsd.SessionTag;
                        List<LabelledSegment> segList = DBAccess.GetBatSegments(tag, bsd.BatId);
                        if (segList != null && segList.Any())
                        {
                            SegmentSonagrams sonagramGenerator = new SegmentSonagrams();
                            sonagramGenerator.GenerateForSegments(segList);
                        }
                    }
                    this.RefreshParentData();
                }
            }
        }

        private void miOpenImportPicture_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!_isPictureDialogOpen)
            {
                _importPictureDialog = new ImportPictureDialog();
                _importPictureDialog.Closed += ImportPictureDialog_Closed;
            }

            if (RecordingsDataGrid.SelectedItem as BatSessionRecordingData != null)
            {
                var fileName = (RecordingsDataGrid.SelectedItem as BatSessionRecordingData).RecordingName;
                _importPictureDialog.SetCaption(fileName);
            }

            if (!_isPictureDialogOpen)
            {
                _importPictureDialog.Show();
                _isPictureDialogOpen = true;
            }
        }

        private void RecordingsDataGrid_MouseDoubleClick(object sender, EventArgs e)
        {
            //var dg = sender as DataGrid;
            var dg = RecordingsDataGrid;
            var selectedItem = dg.SelectedItem as BatSessionRecordingData;
            thisRecording = DBAccess.GetRecording(selectedItem.RecordingId ?? -1);
            if (thisRecording != null)
            {
                var aai = new AnalyseAndImportClass(thisRecording);
                aai.e_DataUpdated += Aai_e_DataUpdated;
                aai.AnalyseRecording();
            }

            //Tools.OpenWavFile(DBAccess.GetRecording(selectedItem.RecordingId ?? -1));
        }

        /// <summary>
        ///     Use a right mouse button click ending on a recording to bring up the Import Picture
        ///     Dialog as a non-modal window witht he caption set to the name of the recording file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingsDataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPictureDialogOpen)
            {
                _importPictureDialog = new ImportPictureDialog();
                _importPictureDialog.Closed += ImportPictureDialog_Closed;
            }

            if (RecordingsDataGrid.SelectedItem as BatSessionRecordingData != null)
            {
                var fileName = (RecordingsDataGrid.SelectedItem as BatSessionRecordingData).RecordingName;
                _importPictureDialog.SetCaption(fileName);
            }

            if (!_isPictureDialogOpen)
            {
                _importPictureDialog.Show();
                _isPictureDialogOpen = true;
            }
        }

        /// <summary>
        ///     The selected recording has changed so find the newly selected recording and populate the
        ///     image control with the images for this recording if any.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecordingImageScroller.Clear();
            if (e.AddedItems != null && e.AddedItems.Count > 0 || e.RemovedItems != null && e.RemovedItems.Count > 0)
                if (RecordingsDataGrid.SelectedItems != null)
                    foreach (var item in RecordingsDataGrid.SelectedItems)
                        if (item is BatSessionRecordingData)
                        {
                            var brec = item as BatSessionRecordingData;
                            //Recording recording = DBAccess.GetRecording(brec.RecordingId??-1);
                            //var recordingImages = recording.GetImageList(brec.BatID);

                            var recordingImages = DBAccess.GetRecordingImagesForBat(brec.RecordingId, brec.BatId);

                            if (!recordingImages.IsNullOrEmpty())
                            {
                                foreach (var image in recordingImages) RecordingImageScroller.AddImage(image);
                                RecordingImageScroller.IsReadOnly = true;
                            }
                        }

            if (_importPictureDialog != null && _isPictureDialogOpen)
                if (RecordingsDataGrid.SelectedItem as BatSessionRecordingData != null)
                    _importPictureDialog.SetCaption((RecordingsDataGrid.SelectedItem as BatSessionRecordingData)
                        .RecordingName);
        }

        /// <summary>
        /// Locates the top level parent window (ListByBats) and refreshes the data in that
        /// window, forcing a SelectionChanged event which will cascade down and refresh this
        /// window.
        /// </summary>
        private void RefreshParentData()
        {
            var topParent = Tools.FindParent<BatRecordingsListDetailControl>(this);
            topParent.RefreshData();
        }

        private void SessionsDataGrid_MouseDoubleClick(object sender, EventArgs e)
        {
            var dg = SessionsDataGrid;
            if (!(dg.SelectedItem is BatSessionData selectedSession)) return;

            OnSessionAction(new SessionActionEventArgs(selectedSession.SessionTag));
        }

        private void SessionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //BatSession selected;
            var dg = SessionsDataGrid;
            Debug.WriteLine("Session selection changed:-");
            foreach (var item in e.AddedItems ?? new List<BatSessionData>())
                Debug.Write("+" + (item as BatSessionData).id + ", ");
            Debug.WriteLine("");
            foreach (var item in e.RemovedItems ?? new List<BatSessionData>())
                Debug.Write("-" + (item as BatSessionData).id + ", ");

            if (e.AddedItems != null && e.AddedItems.Count > 0 || e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                if (dg.SelectedItems == null || dg.SelectedItems.Count <= 0)
                    // if no selection, select all items
                    dg.SelectAll();

                if (dg.SelectedItems != null && dg.SelectedItems.Count > 0)
                {
                    var sessionIdList = new List<int>();
                    var batIdList = new List<int>();
                    // only need to do something if there are some selected items

                    batIdList = SelectedBatDetailsList.Select(selbat => selbat.bat.Id).ToList();
                    var numRecordings = 0;
                    foreach (var item in dg.SelectedItems)
                    {
                        var batSession = item as BatSessionData;
                        if (!sessionIdList.Contains(batSession.id)) sessionIdList.Add(batSession.id);
                        numRecordings += batSession.BatRecordingsCount;
                    }

                    matchingRecordingData.Clear();
                    var newMatchingRecordingData = new BulkObservableCollection<BatSessionRecordingData>();
                    newMatchingRecordingData.AddRange(DBAccess.GetPagedBatSessionRecordingData(batIdList, sessionIdList, 0, 1000));
                    matchingRecordingData.AddRange(newMatchingRecordingData);
                    //Binding binding = new Binding();
                    //binding.Path = new System.Windows.PropertyPath(matchingRecordingData);
                    //binding.IsAsync = true;
                    //RecordingsDataGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
                }
            }
        }

        private void SessionsDataGrid_MouseDoubleClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var dg = SessionsDataGrid;
            if (!(dg.SelectedItem is BatSessionData selectedSession)) return;

            OnSessionAction(new SessionActionEventArgs(selectedSession.SessionTag));
        }
    }
}