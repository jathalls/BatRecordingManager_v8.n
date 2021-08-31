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
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    #region BatRecordingListDetailControl

    /// <summary>
    ///     Interaction logic for BatRecordingsListDetailControl.xaml
    /// </summary>
    public partial class BatRecordingsListDetailControl : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatRecordingsListDetailControl" /> class.
        /// </summary>
        public BatRecordingsListDetailControl()
        {
            InitializeComponent();
            //this.DataContext = this.BatStatisticsList;
            DataContext = this;
            //BatStatisticsList = DBAccess.GetBatStatistics();

            //BatStatsDataGrid.ItemsSource = BatStatisticsList;
            //RefreshData();
            ListByBatsImageScroller.IsReadOnly = true;
            BatStatsDataGrid.EnableColumnVirtualization = true;
            BatStatsDataGrid.EnableRowVirtualization = true;
            //sessionsAndRecordings.imageScroller = ListByBatsImageScroller;
        }

        /// <summary>
        ///     Gets or sets the bat statistics list.
        /// </summary>
        /// <value>
        ///     The bat statistics list.
        /// </value>
        public BulkObservableCollection<BatStatistics> BatStatisticsList { get; } =
            new BulkObservableCollection<BatStatistics>();

        /// <summary>
        ///     Refreshes the data from the databse during a context switch from any other display
        ///     screen to this one.
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal void RefreshData()
        {
            var oldSelection = BatStatsDataGrid.SelectedIndex;
            var multiSelection = BatStatsDataGrid.SelectedItems;

            BatStatisticsList.Clear();
            //BatStatisticsList.AddRange(DBAccess.GetBatStatistics());
            // Stopwatch watch = Stopwatch.StartNew();
            BatStatisticsList.AddRange(DBAccess.GetBatStatistics());
            // watch.Stop();
            // Debug.WriteLine("GetBatStatistics took " + watch.ElapsedMilliseconds + "ms");

            //BatStatsDataGrid.ItemsSource = BatStatisticsList;
            if (multiSelection.Count > 0)
            {
                foreach (var item in multiSelection)
                {
                    BatStatsDataGrid.SelectedItems.Add(item);
                }
            }
            else
            {
                if (BatStatsDataGrid.SelectedIndex != oldSelection)
                {
                    if (oldSelection < BatStatisticsList.Count) BatStatsDataGrid.SelectedIndex = oldSelection;
                }
                else
                {
                    BatStatsDataGrid_SelectionChanged(this, null);
                }
            }

            //  watch.Reset();
            //  watch.Start();
            BatStatsDataGrid.Items.Refresh();
            //  watch.Stop();
            // Debug.WriteLine("DataGrid Item refresh took " + watch.ElapsedMilliseconds + "ms");
        }

        internal void SelectSession(string sessionUpdated)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Initiates the generation of a report for the selected recordings of the
        ///     selected sessions of the selected bats.  If there are no selections in a
        ///     panel then the report is generated to cover all the items in the panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatListReportButton_Click(object sender, RoutedEventArgs e)
        {
            var reportBatStatsList = new List<BatStatistics>();
            var reportSessionList = new List<RecordingSession>();
            var reportRecordingList = new List<Recording>();
            var reportWindow = new ReportMainWindow();

            using (new WaitCursor("Generating Report..."))
            {
                if (BatStatsDataGrid.SelectedItems != null && BatStatsDataGrid.SelectedItems.Count > 0)
                    foreach (var bs in BatStatsDataGrid.SelectedItems)
                        reportBatStatsList.Add(bs as BatStatistics);
                else
                    reportBatStatsList.AddRange(BatStatisticsList);

                reportSessionList = SessionsAndRecordings.GetSelectedSessions();
                reportRecordingList = SessionsAndRecordings.GetSelectedRecordings();

                reportWindow.SetReportData(reportBatStatsList, reportSessionList, reportRecordingList);
                
            }

            reportWindow.ShowDialog();
        }

        private void BatStatsDataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        ///     Double clicking on the bat list adds all the images for the segments of the selected
        ///     bat to the comparison window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatStatsDataGrid_MouseDoubleClick(object sender, EventArgs e)
        {
            using (new WaitCursor("Adding all images to Comparison Window..."))
            {
                var images = SessionsAndRecordings.RecordingImageScroller.imageList;
                ComparisonHost.Instance.AddImageRange(images);
            }
        }

        private void BatStatsDataGrid_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            CompareImagesButton_Click(sender, e);
        }

        private void BatStatsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor("Bat selection changed"))
            {
                //DataGrid bsdg = sender as DataGrid;
                ListByBatsImageScroller.Clear();
                if (e != null)
                {
                    if (e.AddedItems == null || e.RemovedItems == null) return;
                    if (e.AddedItems.Count <= 0 && e.RemovedItems.Count <= 0) return; // nothing has changed
                }

                var selectedBatDetailsList = new List<BatStatistics>();
                foreach (var item in BatStatsDataGrid.SelectedItems) selectedBatDetailsList.Add(item as BatStatistics);
                SessionsAndRecordings.SelectedBatDetailsList.Clear();
                SessionsAndRecordings.SelectedBatDetailsList.AddRange(selectedBatDetailsList);
                SessionsAndRecordings.SetMatchingSessionsAndRecordings();
                SessionsAndRecordings.SelectAll();

                foreach (var item in BatStatsDataGrid.SelectedItems)
                {
                    var batstats = item as BatStatistics;
                    if (batstats.numBatImages > 0)
                    {
                        //var images = DBAccess.GetImagesForBat(batstats.bat, Tools.BlobType.BMPS);
                        var images = batstats.bat.GetImageList();
                        if (!images.IsNullOrEmpty())
                            foreach (var img in images)
                                ListByBatsImageScroller.BatImages.Add(img);
                    }
                }

                ListByBatsImageScroller.ImageScrollerDisplaysBatImages = true;
            }
        }

        private void CompareImagesButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Adding all images to Comparison Window..."))
            {
                //var images = SessionsAndRecordings.RecordingImageScroller.imageList;
                var sessionList = SessionsAndRecordings.GetSelectedSessions();
                foreach (var session in sessionList)
                {
                    var images = session.GetImageList();
                    ComparisonHost.Instance.AddImageRange(images);
                }
                //ComparisonHost.Instance.AddImageRange(images);
            }
        }
    }

    #endregion BatRecordingListDetailControl

    //==============================================================================================================================================
    //==============================================================================================================================================
    //================================== BAT STATISTICS ============================================================================================

    #region BatStatistics

    /// <summary>
    /// </summary>
    public class BatStatistics
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatStatistics" /> class.
        /// </summary>
        public BatStatistics()
        {
            bat = null;

            displayable.Clear();
        }

        public BatStatistics(Bat bat)
        {
            displayable.Clear();
            this.bat = bat;
            displayable.Name = Name;
            displayable.Genus = Genus;
            displayable.Species = Species;
            displayable.Sessions = numSessions;
            displayable.Recordings = numRecordings;
            displayable.Passes = passes;
            displayable.BatImages = bat.BatPictures.Count;
            displayable.RecImages = numRecordingImages;
        }

        /// <summary>
        ///     The bat to which these statistics apply.  Applies a lazy loading protocol for recordings and sessions
        ///     which only get populated from bat when they are first accesses.  If they have been initialised to empty
        ///     collections (or full collections) before the bat is entered then they will be updated to the correct data
        ///     for the bat being entered. Similalry for the stats.
        /// </summary>
        public Bat bat
        {
            get => _bat;
            set
            {
                Clear();
                _bat = value;
            }
        }

        public Displayable displayable { get; set; } = new Displayable();

        /// <summary>
        ///     The genus
        /// </summary>
        public string Genus => bat != null ? bat.Batgenus : "";

        /// <summary>
        ///     The name
        /// </summary>
        public string Name => bat != null ? bat.Name : "";

        /// <summary>
        ///     number of images of the bat itself
        /// </summary>
        public int numBatImages
        {
            get
            {
                if (bat != null) return bat.BatPictures.Count;
                return 0;
            }
        }

        /// <summary>
        ///     number of images associated with recordings which include this bat
        /// </summary>
        public int numRecordingImages
        {
            get
            {
                if (bat == null) return 0;

                if (_numRecordingImages < 0)
                    //_numRecordingImages = bat.BatSegmentLinks.Sum(lnk => lnk.LabelledSegment.SegmentDatas.Count);
                    _numRecordingImages = DBAccess.GetNumRecordingImagesForBat(bat.Id);
                //var sum1 = bat.BatSegmentLinks.Select(lnk => lnk.LabelledSegment.SegmentDatas.Count).Sum();
                return _numRecordingImages;
            }
        }

        public int numRecordings
        {
            get
            {
                if (_numRecordings < 0) _numRecordings = bat.BatRecordingLinks.Where(brl => !(brl.ByAutoID ?? false)).Count();
                return _numRecordings;
            }
        }

        public int numSessions
        {
            get
            {
                if (_numSessions < 0) _numSessions = bat.BatSessionLinks.Where(bsl => !(bsl.ByAutoID ?? false)).Count();
                return _numSessions;
            }
        }

        public int passes
        {
            get
            {
                if (_passes < 0) _passes = bat.BatSegmentLinks.Where(lnk => !(lnk.ByAutoID ?? false)).Sum(lnk => lnk.NumberOfPasses);
                return _passes;
            }
        }

        /// <summary>
        ///     The species
        /// </summary>
        public string Species => bat != null ? bat.BatSpecies : "";

        public class Displayable : INotifyPropertyChanged

        {
            public Displayable()
            {
                Clear();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public int BatImages
            {
                get => _batImages;
                set
                {
                    _batImages = value;
                    Pc("BatImages");
                }
            }

            public string Genus
            {
                get => _genus;
                set
                {
                    _genus = value;
                    Pc("Genus");
                }
            }

            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    Pc("Name");
                }
            }

            public int Passes
            {
                get => _passes;
                set
                {
                    _passes = value;
                    Pc("Passes");
                }
            }

            public int RecImages
            {
                get => _recImages;
                set
                {
                    _recImages = value;
                    Pc("RecImages");
                }
            }

            public int Recordings
            {
                get => _recordings;
                set
                {
                    _recordings = value;
                    Pc("Recordings");
                }
            }

            public int Sessions
            {
                get => _sessions;
                set
                {
                    _sessions = value;
                    Pc("Sessions");
                }
            }

            public string Species
            {
                get => _species;
                set
                {
                    _species = value;
                    Pc("Species");
                }
            }

            public void Clear()
            {
                Name = "";
                Genus = "";
                Species = "";
                Sessions = 0;
                Recordings = 0;
                Passes = 0;
                BatImages = 0;
                RecImages = 0;
            }

            private int _batImages;
            private string _genus;
            private string _name;
            private int _passes;
            private int _recImages;
            private int _recordings;
            private int _sessions;
            private string _species;

            private void Pc(string item)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(item));
            }
        }

        private Bat _bat;
        private int _numRecordingImages = -1;
        private int _numRecordings = -1;
        private int _numSessions = -1;
        private int _passes = -1;

        private void Clear()
        {
            _passes = -1;
            _bat = null;
            _numRecordingImages = -1;
            //_numRecordings = -1;
            _numSessions = -1;
        }
    }

    #endregion BatStatistics
}