﻿// *  Copyright 2016 Justin A T Halls
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatListControl.xaml
    /// </summary>
    public partial class BatListControl : UserControl, INotifyPropertyChanged
    {
        //private BatSummary batSummary;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatListControl" /> class.
        /// </summary>
        public BatListControl()
        {
            SetValue(SortedBatListProperty, new BulkObservableCollection<Bat>());
            InitializeComponent();
            DataContext = this;
            BatsDataGrid.EnableColumnVirtualization = true;
            BatsDataGrid.EnableRowVirtualization = true;

            //batSummary = new BatSummary();

            BatDetailControl.e_ListChanged += BatDetailControl_ListChanged;
            RefreshData();

            //batDetailControl.selectedBat = BatsDataGrid.SelectedItem as Bat;
        }

        /// <summary>
        ///     Returns the currently selected bat or null if none has been selected
        /// </summary>
        /// <returns></returns>
        internal Bat GetSelectedBat()
        {
            Bat result = null;

            if (BatDetailControl?.selectedBat != null)
                result = BatDetailControl.selectedBat;

            return result;
        }

        internal List<Bat> RefreshData_Async()
        {
            return DBAccess.GetSortedBatList();
            
            
            

        }

        internal async void RefreshData()
        {
            var index = BatsDataGrid.SelectedIndex;

            var data = await Task.Run(() => RefreshData_Async());
            SortedBatList.Clear();
            SortedBatList.AddRange(data);


            OnPropertyChanged(nameof(SortedBatList));
            BatsDataGrid.SelectedIndex = index < SortedBatList.Count ? index : SortedBatList.Count - 1;

            BatDetailControl.selectedBat = BatsDataGrid.SelectedItem as Bat;

        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddBatButton_Click(object sender, RoutedEventArgs e)
        {
            var batEditingForm = new EditBatForm { NewBat = new Bat { Id = -1 } };
            batEditingForm.ShowDialog();
            if (batEditingForm.DialogResult != null && batEditingForm.DialogResult.Value)
                //DBAccess.InsertBat(batEditingForm.newBat);
                RefreshData();
            BatDetailControl.selectedBat = BatsDataGrid.SelectedItem as Bat;
        }

        private void BatDetailControl_ListChanged(object sender, EventArgs e)
        {
            var bdc = sender as BatDetailControl;

            var tagIndex = bdc.BatTagsListView.SelectedIndex;

            RefreshData();
            bdc.BatTagsListView.SelectedIndex = tagIndex;
        }

        /// Double-click on the DataGrid listing the bats sends all the images for the selected bats to the comparison window
        private void BatsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (new WaitCursor("Selecting all images for Comparison"))
            {
                var images = new BulkObservableCollection<StoredImage>();
                if (BatsDataGrid.SelectedItems != null)
                {
                    //var selectedBat = BatsDataGrid.SelectedItem as Bat;

                    foreach (var item in BatsDataGrid.SelectedItems)
                    {
                        var bat = item as Bat;
                        var thisBatsImages = DBAccess.GetAllImagesForBat(bat);
                        if (thisBatsImages != null) images.AddRange(thisBatsImages);
                    }

                    //var images = DBAccess.GetImagesForBat(selectedBat, Tools.BlobType.PNG);
                    //var images = selectedBat.GetImageList();
                    if (!images.IsNullOrEmpty()) ComparisonHost.Instance.AddImageRange(images);
                }
            }
        }

        private void BatsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //DataGrid bsdg = sender as DataGrid;
            if (e.AddedItems == null || e.AddedItems.Count <= 0) return;
            var selected = e.AddedItems[0] as Bat;
            if (e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                var previous = e.RemovedItems[0] as Bat;
                if (previous == selected) return;
            }

            // therefore we have a selected item which is different from the previously selected item
            using (new WaitCursor("Bat selection changed"))
            {
                BatDetailControl.selectedBat = selected;
            }
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Add image to Comparison Window"))
            {
                var images = new BulkObservableCollection<StoredImage>();
                if (BatsDataGrid.SelectedItems != null)
                {
                    //var selectedBat = BatsDataGrid.SelectedItem as Bat;

                    foreach (var item in BatsDataGrid.SelectedItems)
                    {
                        var bat = item as Bat;
                        var thisBatsImages = DBAccess.GetBatAndCallImagesForBat(bat);
                        if (thisBatsImages != null) images.AddRange(thisBatsImages);
                    }

                    //var images = DBAccess.GetImagesForBat(selectedBat, Tools.BlobType.PNG);
                    //var images = selectedBat.GetImageList();
                    if (!images.IsNullOrEmpty()) ComparisonHost.Instance.AddImageRange(images);
                }
            }
        }

        private void DelBatButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatsDataGrid.SelectedItem != null)
            {
                var selectedBat = BatsDataGrid.SelectedItem as Bat;

                DBAccess.DeleteBat(selectedBat);
                RefreshData();
            }
        }

        private void EditBatButton_Click(object sender, RoutedEventArgs e)
        {
            BatDetailControl.Reset();
            var batEditingForm = new EditBatForm();
            if (BatsDataGrid.SelectedItem == null)
                batEditingForm.NewBat = new Bat { Id = -1 };
            else
                batEditingForm.NewBat = BatsDataGrid.SelectedItem as Bat;

            batEditingForm.ShowDialog();
            if (batEditingForm.DialogResult ?? false) RefreshData();
        }

        private void CompareImagesButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Selecting all images for Comparison"))
            {
                var images = new BulkObservableCollection<StoredImage>();
                if (BatsDataGrid.SelectedItems != null)
                {
                    //var selectedBat = BatsDataGrid.SelectedItem as Bat;

                    foreach (var item in BatsDataGrid.SelectedItems)
                    {
                        var bat = item as Bat;
                        var thisBatsImages = DBAccess.GetAllImagesForBat(bat);
                        if (thisBatsImages != null) images.AddRange(thisBatsImages);
                    }

                    //var images = DBAccess.GetImagesForBat(selectedBat, Tools.BlobType.PNG);
                    //var images = selectedBat.GetImageList();
                    if (!images.IsNullOrEmpty()) ComparisonHost.Instance.AddImageRange(images);
                }
            }
        }

        #region SortedBatList

        /// <summary>
        ///     SortedBatList Dependency Property
        /// </summary>
        public static readonly DependencyProperty SortedBatListProperty =
            DependencyProperty.Register(nameof(SortedBatList), typeof(BulkObservableCollection<Bat>), typeof(BatListControl),
                new FrameworkPropertyMetadata(new BulkObservableCollection<Bat>()));

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Gets or sets the SortedBatList property. This dependency property indicates ....
        /// </summary>
        public BulkObservableCollection<Bat> SortedBatList =>
            (BulkObservableCollection<Bat>)GetValue(SortedBatListProperty);

        #endregion SortedBatList
    }
}