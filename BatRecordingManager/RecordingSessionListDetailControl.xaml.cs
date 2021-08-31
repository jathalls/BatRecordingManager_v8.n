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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

//using Task = Microsoft.Build.Utilities.Task;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingSessionListDetailControl.xaml
    /// </summary>
    public partial class RecordingSessionListDetailControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        ///     The index of the first session on the current page or the page to be loaded
        /// </summary>
        public int CurrentTopOfScreen = 0;

        /// <summary>
        ///     A string representing the field to be used for ordering the page entries before loading
        ///     The Datagrid will sort within the page, this controls the items loaded into the page
        ///     The value is taken from the navigation combobox.
        ///     if NONE then the items are loaded in native databse order, i.e. the order in which they were
        ///     added tothe database.
        ///     The final character except for NONE is an arrow indicating if ascending or descending order
        ///     i.e. ^ or v
        /// </summary>
        public string Field = "NONE";

        public int MaxRecordingSessions;

        /// <summary>
        ///     The number of sessions to load ina page view
        /// </summary>
        public int PageSize = 100;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingSessionListDetailControl" /> class.
        /// </summary>
        public RecordingSessionListDetailControl()
        {
            //displayedRecordings = new BulkObservableCollection<Recording>();

            InitializeComponent();
            DataContext = this;

            SegmentImageScroller.AddImageButton.IsEnabled = false;
            RecordingSessionListView.Initialized += RecordingSessionListView_Initialized;

            RecordingsListControl.e_RecordingChanged += RecordingsListControl_RecordingChanged;
            RecordingsListControl.e_SegmentSelectionChanged += RecordingsListControl_SegmentSelectionChanged;
            SegmentImageScroller.IsReadOnly = true;
            SegmentImageScroller.AddImageButton.IsEnabled = false;
            SegmentImageScroller.Title = "Segment Images";

            MaxRecordingSessions = DBAccess.GetRecordingSessionCount();

            //RefreshData(pageSize,currentTopOfScreen);
            //RecordingsListView.ItemsSource = displayedRecordingControls;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<EventArgs> SessionChanged;

        public BulkObservableCollection<string> sessionSummaryList { get; set; } = new BulkObservableCollection<string>();

        public bool GenerateReportSet(object sender, RoutedEventArgs e)
        {
            bool doFullExport = false;
            if (sender is AnalyseAndImportClass) doFullExport = true;
            return (GenerateReportSet(null, doFullExport));
        }

        /// <summary>
        /// Alternative GenerateReport which actually does allthe work aand can be called
        /// directly without a sender or eventArgs.  doFullExport flags that all report formats
        /// should be exported as .csv files.
        /// </summary>
        /// <param name="doFullExport"></param>
        /// <returns></returns>
        public bool GenerateReportSet(RecordingSession SpecificSession = null, bool doFullExport = false)
        {
            var reportBatStatsList = new List<BatStatistics>();
            var reportSessionList = new List<RecordingSession>();
            var reportRecordingList = new List<Recording>();
            var reportWindow = new ReportMainWindow();
            var statsForAllSessions = new BulkObservableCollection<BatStats>();
            using (new WaitCursor())
            {
                Debug.WriteLine("GenerateReport at" + DateTime.Now.ToLongTimeString());
                try
                {
                    if (SpecificSession != null)
                    {
                        statsForAllSessions.AddRange(SpecificSession.GetStats());

                        reportSessionList.Add(SpecificSession);
                    }
                    else
                    {
                        if (RecordingSessionListView.SelectedItems != null &&
                            RecordingSessionListView.SelectedItems.Count > 0)
                        {
                            Debug.WriteLine("Get Data for " + RecordingSessionListView.SelectedItems.Count +
                                            " items at " +
                                            DateTime.Now.ToLongTimeString());
                            foreach (var item in RecordingSessionListView.SelectedItems)
                            {
                                if (!(item is RecordingSessionData sessionData)) return (false);
                                Debug.WriteLine("Get Data for Session " + sessionData.SessionTag + " at " +
                                                DateTime.Now.ToLongTimeString());
                                var session = DBAccess.GetRecordingSession(sessionData.Id);
                                if (session == null) return (false);
                                Debug.WriteLine("GetStats for Session at " + DateTime.Now.ToLongTimeString());
                                statsForAllSessions.AddRange(session.GetStats());

                                reportSessionList.Add(session);
                                Debug.WriteLine(reportSessionList.Count + " items in the sessionList at " +
                                                DateTime.Now.ToLongTimeString());
                            }
                        }
                        else
                        {
                            Debug.WriteLine("No selection made so reporting for all sessions!!!!!!! at " +
                                            DateTime.Now.ToLongTimeString());
                            if (RecordingSessionListView.Items != null && RecordingSessionListView.Items.Count > 0)
                                foreach (var item in RecordingSessionListView.Items)
                                {
                                    if (!(item is RecordingSessionData sessionData)) return (false);
                                    var session = DBAccess.GetRecordingSession(sessionData.Id);
                                    if (session == null) return (false);
                                    statsForAllSessions.AddRange(session.GetStats());

                                    reportSessionList.Add(session);
                                }
                        }
                    }

                    Debug.WriteLine("Condensing Stats List at " + DateTime.Now.ToLongTimeString());
                    statsForAllSessions = Tools.CondenseStatsList(statsForAllSessions);
                    Debug.WriteLine("collating stats for " + statsForAllSessions.Count + " at " +
                                    DateTime.Now.ToLongTimeString());
                    foreach (var bs in statsForAllSessions)
                    {
                        Debug.WriteLine("Processing next stat at " + DateTime.Now.ToLongTimeString());
                        var bstat = new BatStatistics(DBAccess.GetNamedBat(bs.batCommonName));
                        reportBatStatsList.Add(bstat);
                        var recordingsToreport = (from brLink in bstat.bat.BatRecordingLinks
                                                  where !(brLink.ByAutoID ?? false)
                                                  join sess in reportSessionList on brLink.Recording.RecordingSessionId equals sess.Id
                                                  select brLink.Recording).Distinct();

                        if (recordingsToreport != null)
                            foreach (var rec in recordingsToreport)
                                if (reportRecordingList.All(existingRec => existingRec.Id != rec.Id))
                                    reportRecordingList.Add(rec);
                        //ReportRecordingList.AddRange(recordingsToreport);
                    }

                    Debug.WriteLine("Setting ReportData at " + DateTime.Now.ToLongTimeString());
                    reportWindow.SetReportData(reportBatStatsList, reportSessionList, reportRecordingList);
                    Debug.WriteLine("Completed at " + DateTime.Now.ToLongTimeString());
                    if (doFullExport) reportWindow.ExportAll();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error generating Report:-" + ex.Message);
                    return (false);
                }
            }

            reportWindow.ShowDialog();
            return (true);
        }

        public Task<bool> GenerateReportSetAsync(object sender, RoutedEventArgs e)
        {
            return Task<bool>.Run(() => GenerateReportSet(sender, e));
        }

        /// <summary>
        ///     Generates a report for the selected sessions or for all sessions if none are selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReportSessionDataButton_Click(object sender, RoutedEventArgs e)
        {
            //UiServices.SetBusyState();
            GenerateReportSet(sender, e);
        }

        //private WaitCursor storedWaitCursor = null;
        internal int oldSelectionIndex { get; set; } = -1;

        /// <summary>
        ///     Returns the currently selected recording if any or a null
        /// </summary>
        /// <returns></returns>
        internal Recording GetSelectedRecording()
        {
            Recording result = null;

            if (RecordingsListControl?.RecordingsListView?.SelectedItems != null &&
                RecordingsListControl.RecordingsListView.SelectedItems.Count > 0)
                result = RecordingsListControl.RecordingsListView.SelectedItems[0] as Recording;

            return result;
        }

        /// <summary>
        ///     if a session has been selected it is returned from this function, otherwise a
        ///     null is returned
        /// </summary>
        /// <returns></returns>
        internal RecordingSession GetSelectedSession()
        {
            RecordingSession result = null;
            if (RecordingSessionListView?.SelectedItems != null && RecordingSessionListView.SelectedItems.Count > 0)
            {
                result = RecordingSessionListView.SelectedItems[0] is RecordingSessionData sessionData
                    ? DBAccess.GetRecordingSession(sessionData.Id)
                    : null;
            }

            return result;
        }

        internal void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        internal void RefreshData()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                //Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => { RefreshData(PageSize, CurrentTopOfScreen); }));
        }

        /// <summary>
        ///     Refreshes the data in the display when this pane is made visible; It might slow down
        ///     context switches, but is necessary if other panes have changed the data. A more
        ///     sophisticated approach would be to have any display set a 'modified' flag which
        ///     would trigger the update or not as necessary;
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal void RefreshData(int pageSize, int topOfScreen)
        {
            if (RecordingSessionListView == null) return;
            //storedWaitCursor=new WaitCursor();
            using (new WaitCursor("Refresh screen data"))
            {
                //  Stopwatch overallWatch = Stopwatch.StartNew();
                oldSelectionIndex = -1;
                if(RecordingSessionListView.SelectedItem != null)
                {
                    oldSelectionIndex = (RecordingSessionListView.SelectedItem as RecordingSessionData).Id;
                }
                //oldSelectionIndex = selectedIndex;
                //recordingSessionDataList.Clear();
                //recordingSessionDataList.AddRange(DBAccess.GetPagedRecordingSessionDataList(pageSize, topOfScreen, field));
                recordingSessionDataList = null;
                var defaultRSD = new RecordingSessionData();
                defaultRSD.SessionTag = "Loading...";
                recordingSessionDataList =
                    new BulkObservableCollection<RecordingSessionData>();
                recordingSessionDataList.AddRange(DBAccess.GetAllRecordingSessionData());
                //recordingSessionDataList.CollectionChanged += RecordingSessionDataList_CollectionChanged;

                //if (!recordingSessionDataList.IsLoading) recordingSessionDataList.Refresh();
                if (oldSelectionIndex >= 0) {
                    var selectedList = recordingSessionDataList.Where(red => red.Id == oldSelectionIndex);
                    if (selectedList?.Any() ?? false)
                    {
                        selectedList.First().IsSelected = true;
                    }
                    
                }
                SegmentImageScroller.Clear();
                if (RecordingSessionListView.SelectedItem == null)
                {
                    RecordingSessionControl.recordingSession = null;
                    RecordingsListControl.selectedSession = null;
                    //RecordingsListControl.virtualRecordingsList.Clear();
                }
                else
                {
                    var selectedID = (RecordingSessionListView.SelectedItem as RecordingSessionData).Id;
                    if ((RecordingSessionControl.recordingSession ?? new RecordingSession()).Id != selectedID)
                    {
                        var session = DBAccess.GetRecordingSession(selectedID);
                        RecordingSessionControl.recordingSession = session;
                        RecordingsListControl.selectedSession = session;
                        //RecordingsListControl.virtualRecordingsList.Clear();
                        //RecordingsListControl.virtualRecordingsList.AddRange(RecordingSessionControl.recordingSession
                        //.Recordings);
                    }
                }

                //RecordingSessionListView_SelectionChanged(this, null);
            }

            //CollectionViewSource.GetDefaultView(RecordingSessionListView.ItemsSource).Refresh();
        }

        public int selectedIndex { get; set; }

        /// <summary>
        ///     Selects the specified recording session.
        /// </summary>
        /// <param name="recordingSessionId">
        ///     The recording session.
        /// </param>
        internal void Select(int recordingSessionId)
        {
            for (var i = 0; i < RecordingSessionListView.Items.Count; i++)
            {
                var sessionData = RecordingSessionListView.Items[i] as RecordingSessionData;
                if (sessionData.Id == recordingSessionId)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        ///     selects and brings into view the session indicated by the sessionTag
        /// </summary>
        /// <param name="sessionUpdated"></param>
        internal void SelectSession(string sessionUpdated)
        {
            if (RecordingSessionListView.Items != null)
                foreach (var item in RecordingSessionListView.Items)
                {
                    if (item is RecordingSessionData rs && rs.SessionTag == sessionUpdated)
                    {
                        RecordingSessionListView.SelectedItem = rs;
                        RecordingSessionListView.ScrollIntoView(item);
                        //rs.BringIntoView();
                    }
                }
        }

        protected virtual void OnSessionChanged(EventArgs e) => SessionChanged?.Invoke(this, e);

        private void AddEditRecordingSession(RecordingSessionForm recordingSessionForm)
        {
            using (new WaitCursor())
            {
                

                var error = "No Data Entered";
                Mouse.OverrideCursor = null;
                if (!recordingSessionForm.ShowDialog() ?? false)
                    if (recordingSessionForm.DialogResult ?? false)
                        if (!string.IsNullOrWhiteSpace(error))
                            MessageBox.Show(error);

                RefreshData(PageSize, CurrentTopOfScreen);
            }

            /*this.recordingSessionList = DBAccess.GetRecordingSessionList();
if (selectedIndex >= 0 && selectedIndex <= this.RecordingSessionListView.Items.Count)
{
    RecordingSessionListView.SelectedIndex = selectedIndex;
}
Mouse.OverrideCursor = null;*/
        }

        private void AddRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                var recordingSessionForm = new RecordingSessionForm();

                recordingSessionForm.Clear();
                var newSession = new RecordingSession
                {
                    LocationGPSLatitude = null,
                    LocationGPSLongitude = null,
                    SessionDate = DateTime.Today
                };
                newSession.EndDate = newSession.SessionDate;
                newSession.SessionStartTime = new TimeSpan(18, 0, 0);
                newSession.SessionEndTime = new TimeSpan(24, 0, 0);
                recordingSessionForm.SetRecordingSession(newSession);
                Mouse.OverrideCursor = null;
                AddEditRecordingSession(recordingSessionForm);
                OnSessionChanged(EventArgs.Empty);
            }
        }

        private void CompareImagesButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Add all images to the comparison window..."))
            {
                //BulkObservableCollection<StoredImage> images = DBAccess.GetAllImagesForSession(session);
                if (RecordingSessionListView.SelectedItem is RecordingSessionData sessionData)
                {
                    var session = DBAccess.GetRecordingSession(sessionData.Id);
                    if (session != null)
                    {
                        var images = session.GetImageList();
                        if (images == null || images.Count <= 0)
                        {
                            ComparisonHost.Instance.AddImage(new StoredImage(null, "", "", -1));
                            ComparisonHost.Instance.Close();
                        }

                        ComparisonHost.Instance.AddImageRange(images);
                    }
                }
            }
        }

        private void DeleteRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            int oldIndex = 0;
            using (new WaitCursor("Deleting Recording Session"))
            {
                if (RecordingSessionListView.SelectedItem != null && RecordingSessionListView.SelectedItems.Count > 0)
                {
                    oldIndex = selectedIndex; ;
                    for (int i = 0; i < RecordingSessionListView.SelectedItems.Count; i++)
                    {
                        var session =
                            DBAccess.GetRecordingSession((RecordingSessionListView.SelectedItems[i] as RecordingSessionData)
                                .Id);
                        var result = MessageBox.Show(
                            $"This will remove session {session.SessionTag} From the Database\nAre You Sure?",
                            "Delete RecordingSession", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            //recordingSessionDataList.RemoveAt(oldIndex);
                            if (RecordingSessionListView.Items.Count > 0)
                            {
                                oldIndex--;
                                if (oldIndex < 0) oldIndex = 0;
                                //RecordingSessionListView.SelectedIndex = oldIndex;
                            }
                            else
                            {
                                RecordingSessionControl.recordingSession = null;
                                RecordingsListControl.selectedSession = null;
                                //RecordingsListControl.virtualRecordingsList.Clear();
                            }

                            DBAccess.DeleteSession(session);
                        }
                    }

                    //RefreshData(PageSize, CurrentTopOfScreen);
                    if (RecordingSessionListView.SelectedItem != null)
                        RecordingSessionListView.ScrollIntoView(RecordingSessionListView.SelectedItem);
                }
                OnSessionChanged(EventArgs.Empty);
            }
        }

        private async void DisplaySessionSummary(RecordingSession session)
        {
            var sessionSummary = await DisplaySessionSummaryAsync(session);

            SessionSummaryListView.Dispatcher.Invoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    sessionSummaryList.Clear();
                    foreach (var item in sessionSummary)
                    {
                        //var batPassSummary = new BatPassSummaryControl { Content = item };
                        sessionSummaryList.Add(item);
                    }
                }));
        }

        private Task<List<string>> DisplaySessionSummaryAsync(RecordingSession session)
        {
            return Task.Run(() => Tools.GetSessionSummary(session));
        }

        private void EditRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Set Recording session form data"))
            {
                var form = new RecordingSessionForm();
                if (RecordingSessionListView.SelectedItems != null && RecordingSessionListView.SelectedItems.Count > 0)
                {
                    var sessionData = RecordingSessionListView.SelectedItems[0] as RecordingSessionData;
                    form.RecordingSessionControl.recordingSession = DBAccess.GetRecordingSession(sessionData.Id);
                }
                else
                {
                    form.Clear();
                }

                AddEditRecordingSession(form);
            }
        }

        /// <summary>
        ///     Handles the Click event of the ExportSessionDataButton control.
        /// Modified functionality - now rewrites all the text files for recordings in this session
        /// using the comments in the relevant labelled segments.  By default will replace all the old text files
        /// with new ones, but CTRL-Click will just write text files where they do not already exist and
        /// existing text files will be left untouched.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void ExportSessionDataButton_Click(object sender, RoutedEventArgs e)
        {
            bool partial = false;
            if (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                partial = true;
            }
            RecordingSession selectedSession = GetSelectedSession();
            if (selectedSession == null) return;
            if (selectedSession.Recordings == null || selectedSession.Recordings.Count <= 0) return;
            if (!Directory.Exists(selectedSession.OriginalFilePath)) return;

            // only carry on if we have just one session selected
            using (new WaitCursor())
            {
                selectedSession.WriteTextFile(partial);
                foreach (var recording in selectedSession.Recordings)
                {
                    recording.WriteTextFile(partial);
                }
            }

            e.Handled = true;
            return;
        }

        /// <summary>
        ///     Handles the Click event of the ExportSessionDataButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void ExportSessionDataButton_Click_old(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Export session data"))
            {
                if (RecordingSessionListView.SelectedItems.Count > 0)
                    foreach (var item in RecordingSessionListView.SelectedItems)
                    {
                        var session = DBAccess.GetRecordingSession(item is RecordingSessionData sessionData ? sessionData.Id : -1);
                        if (session != null)
                        {
                            var statsForSession = session.GetStats();
                            statsForSession = Tools.CondenseStatsList(statsForSession);
                            var folder = @"C:\ExportedBatData\";

                            if (!Directory.Exists(folder))
                            {
                                try
                                {
                                    var info = Directory.CreateDirectory(folder);
                                    if (!info.Exists)
                                    {
                                        folder = @"C:\ExportedBatData\";
                                        Directory.CreateDirectory(folder);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Tools.ErrorLog(ex.Message);
                                    folder = @"C:\ExportedBatData\";
                                    Directory.CreateDirectory(folder);
                                }

                                if (!Directory.Exists(folder))
                                {
                                    MessageBox.Show("Unable to create folder for export files: " + folder);
                                    Mouse.OverrideCursor = null;
                                    return;
                                }
                            }

                            var file = session.SessionTag.Trim() + ".csv";
                            if (File.Exists(folder + file))
                            {
                                if (File.Exists(folder + session.SessionTag.Trim() + ".bak"))
                                    File.Delete(folder + session.SessionTag.Trim() + ".bak");
                                File.Move(folder + file, folder + session.SessionTag.Trim() + ".bak");
                            }

                            var sw = File.AppendText(folder + file);
                            sw.Write("Date,Place,Gridref,Comment,Observer,Species,Abundance=Passes,Additional Info" +
                                     Environment.NewLine);
                            foreach (var stat in statsForSession)
                            {
                                var line = session.SessionDate.ToShortDateString();
                                line += "," + session.Location;
                                line += ",\"" + session.LocationGPSLatitude + "," + session.LocationGPSLongitude + "\"";
                                line += "," + (session.SessionStartTime != null
                                            ? session.SessionStartTime.Value + " - " + (session.SessionEndTime != null
                                                  ? session.SessionEndTime.Value.ToString()
                                                  : "")
                                            : "") + "; "
                                        + "\"" + session.Equipment + "; " + session.Microphone + "\"";
                                line += "," + "\"" + session.Operator + "\"";
                                line += "," + DBAccess.GetBatLatinName(stat.batCommonName);
                                line += "," + stat.passes;
                                line += "," + "\"" + session.SessionNotes.Replace("\n", "\t") + "\"" +
                                        Environment.NewLine;
                                sw.Write(line);
                            }

                            sw.Close();
                        }
                    }
            }
        }

        async private void generateForSegments(List<LabelledSegment> segments)
        {
            SegmentSonagrams sonagramGenerator = new SegmentSonagrams();
            var success = await sonagramGenerator.GenerateForSegmentsAsync(segments);
            OnSessionChanged(EventArgs.Empty);
        }

        private void GenerateSonagrams_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                var selectedSession = GetSelectedSession();
                var sonagrams = new SegmentSonagrams();
                sonagrams.GenerateForSession(selectedSession);
                oldSelectionIndex = selectedIndex; 
                recordingSessionDataList.Clear();

                // recordingSessionDataList.Refresh(oldSelectionIndex);

                OnSessionChanged(EventArgs.Empty);
            }
        }

        private List<LabelledSegment> getSegmentsForSelectedSummaries()
        {
            List<Bat> batList = new List<Bat>();
            if (SessionSummaryListView.SelectedItems != null && SessionSummaryListView.SelectedItems.Count > 0)
            {
                foreach (var summaryItem in SessionSummaryListView.SelectedItems)
                {
                    string summary = summaryItem as string;
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        Bat bat = DBAccess.GetBatByName(summary);
                        if (bat != null)
                            batList.Add(bat);
                    }
                }
            }

            var segments = (from rec in RecordingsListControl.recordingsList
                            from bat in batList
                            from seg in rec.LabelledSegments
                            from lnk in seg.BatSegmentLinks
                            where lnk.BatID == bat.Id
                            select seg).ToList();
            return (segments);
        }

        /// <summary>
        /// context menu selection to generate spectrograms for every segment which feature the selected bat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miBatSonagrams_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                List<LabelledSegment> segments = new List<LabelledSegment>();
                segments = getSegmentsForSelectedSummaries();

                generateForSegments(segments);

                List<Recording> recs = segments.Select(seg => seg.Recording).Distinct().ToList();
                RecordingsListControl.RefreshRecordings(recs);
            }
        }

        private void miCompareImages_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                var segments = getSegmentsForSelectedSummaries();

                var images = new BulkObservableCollection<StoredImage>();
                if (segments != null)
                {
                    foreach (var seg in segments)
                    {
                        var segImages = seg.GetImageList();
                        if (segImages != null)
                        {
                            images.AddRange(segImages);
                        }
                    }
                }

                if (images == null || !images.Any())
                {
                    ComparisonHost.Instance.AddImage(new StoredImage(null, "", "", -1));
                    ComparisonHost.Instance.Close();
                }

                ComparisonHost.Instance.AddImageRange(images);
            }
        }

        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            //ListViewItem lvi = sender as D
            //lvi.IsSelected = true;
            //lvi.BringIntoView();
        }

        private void RecordingSessionDataList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (oldSelectionIndex >= 0 && oldSelectionIndex < RecordingSessionListView.Items.Count)
            {
                if (selectedIndex != oldSelectionIndex)
                {
                    selectedIndex = oldSelectionIndex;
                }
                else
                {
                    RecordingSessionListView_SelectionChanged(this, null);
                }
            }
        }

        /// <summary>
        ///     called when the control is initialized and the data can be refreshed for the first time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingSessionListView_Initialized(object sender, EventArgs e)
        {
            //if (recordingSessionDataList == null ||
            // !recordingSessionDataList.IsLoading && recordingSessionDataList.Count <= 0) RefreshData();
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the RecordingSessionListView control.
        ///     Selection has changed in the list, so update the details panel with the newly
        ///     selected item.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="SelectionChangedEventArgs" /> instance containing the event data.
        /// </param>
        private void RecordingSessionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor("Change Recording Session Selection"))

            {
                SplitByDateButton.IsEnabled = false;
                if (RecordingSessionListView.SelectedItems == null ||
                    RecordingSessionListView.SelectedItems.Count <= 0) return;
                if (RecordingSessionListView.SelectedItems.Count == 1)
                {
                    ExportSessionDataButton.IsEnabled = true;
                }
                else
                {
                    ExportSessionDataButton.IsEnabled = false;
                }
                foreach (var item in RecordingSessionListView.SelectedItems)
                {
                    if ((item as RecordingSessionData).multiDaySession)
                    {
                        SplitByDateButton.IsEnabled = true;
                        break;
                    }
                }

                var id = (RecordingSessionListView.SelectedItems[0] as RecordingSessionData).Id;
                RecordingSessionControl.recordingSession = DBAccess.GetRecordingSession(id);
                RecordingsListControl.selectedSession = RecordingSessionControl.recordingSession;
                //statsForSession = CondenseStatsList(statsForSession);
                sessionSummaryList.Clear();
                SegmentImageScroller.Clear();
                if (RecordingSessionControl.recordingSession == null)
                {
                    ExportSessionDataButton.IsEnabled = false;
                    EditRecordingSessionButton.IsEnabled = false;
                    DeleteRecordingSessionButton.IsEnabled = false;
                    CompareImagesButton.IsEnabled = false;
                }
                else
                {
                    DisplaySessionSummary(RecordingSessionControl.recordingSession); // runs asynchronously

                    ExportSessionDataButton.IsEnabled = true;
                    EditRecordingSessionButton.IsEnabled = true;
                    DeleteRecordingSessionButton.IsEnabled = true;
                    CompareImagesButton.IsEnabled = true;
                }
            }

            oldSelectionIndex = selectedIndex;
        }

        private void RecordingsListControl_RecordingChanged(object sender,RecordingChangedEventArgs e)
        {
            //RefreshData(PageSize, CurrentTopOfScreen);
            //OnSessionChanged(EventArgs.Empty);
            var session = e.recordingSession;

            var index = selectedIndex;
            if (session!=null)
            {
                var data = DBAccess.GetRecordingSessionData(session.Id);
                data.IsSelected = true;

                var existing=recordingSessionDataList.Where(rsd=>rsd.Id==data.Id).FirstOrDefault();
                if (existing != null)
                {
                    int i = recordingSessionDataList.IndexOf(existing);
                    recordingSessionDataList[i] = data;
                }

                
                
            }
        }

        /// <summary>
        ///     Event handler triggered when the user selects a new labelled segment within the list
        ///     The event args contain the list of images which are to be displayed in the ImageScroller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordingsListControl_SegmentSelectionChanged(object sender, EventArgs e)
        {
            var ile = e as ImageListEventArgs;

            Debug.WriteLine("RecordingsListControl_SegmentSelectionChanged:- " + ile.imageList.Count + " images");
            SegmentImageScroller.Clear();
            if (ile.imageList != null && ile.imageList.Count > 0)
                foreach (var im in ile.imageList)
                    SegmentImageScroller.AddImage(im);
        }

        #region recordingSessionList

        /// <summary>
        ///     Gets or sets the recordingSessionList property. This dependency property indicates ....
        /// </summary>

        #region recordingSessionDataList

        /// <summary>
        ///     recordingSessiondataList Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingSessionDataListProperty =
            DependencyProperty.Register(nameof(recordingSessionDataList),
                typeof(BulkObservableCollection<RecordingSessionData>), typeof(RecordingSessionListDetailControl),
                new FrameworkPropertyMetadata(new BulkObservableCollection<RecordingSessionData>()));

        /// <summary>
        ///     Gets or sets the recordingSessiondataList property.  This dependency property
        ///     indicates ....
        /// </summary>
        public BulkObservableCollection<RecordingSessionData> recordingSessionDataList
        {
            get => (BulkObservableCollection<RecordingSessionData>)GetValue(recordingSessionDataListProperty);
            set => SetValue(recordingSessionDataListProperty, value);
        }

        #endregion recordingSessionDataList

        // public AsyncVirtualizingCollection<RecordingSessionData> recordingSessionDataList = new AsyncVirtualizingCollection<RecordingSessionData>(new RecordingSessionDataProvider(), 50, 100);

        #endregion recordingSessionList

        /// <summary>
        /// Selecting a line in the summary, or changing the (possibly multiple or extended
        /// selection, de-selects all labelled segments and then goes through selecting all
        /// Labelled segments which contain an occurrence of the bat specified in the selected
        /// summary lines.
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SessionSummaryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        /// <summary>
        /// Displays a BING map with the route of the current session and highlighting bat locations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordingSessionListView.SelectedItems != null &&
                            RecordingSessionListView.SelectedItems.Count > 0)
            {
                var sessionData = RecordingSessionListView.SelectedItems[0] as RecordingSessionData;
                var session = DBAccess.GetRecordingSession(sessionData.Id);
                if (session != null)
                {
                    MapHTML map = new MapHTML();
                    map.Create(session);
                }
            }
        }

        /// <summary>
        /// Takes each selected session and if it covers more than one day divides it into
        /// separate sessions of up to 24 hours running noon-noon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitByDateButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordingSessionListView.SelectedItems != null &&
                            RecordingSessionListView.SelectedItems.Count > 0)
            {
                foreach (var item in RecordingSessionListView.SelectedItems)
                {
                    var sessionData = item as RecordingSessionData;

                    DBAccess.SplitSessionByDate(sessionData.Id);
                }
                this.RefreshData();
            }
        }
    }
}