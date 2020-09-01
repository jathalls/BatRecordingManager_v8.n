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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatEditor.xaml
    /// </summary>
    public partial class BatEditor : Window
    {
        // Using a DependencyProperty as the backing store for BatList. This enables animation,
        // styling, binding, etc...
        /// <summary>
        ///     The bat list property
        /// </summary>
        public static readonly DependencyProperty BatListProperty =
            DependencyProperty.Register(nameof(BatList), typeof(BulkObservableCollection<Bat>), typeof(BatEditor),
                new PropertyMetadata(new BulkObservableCollection<Bat>()));

        /// <summary>
        ///     Gets or sets the bat list.
        /// </summary>
        /// <value>
        ///     The bat list.
        /// </value>
        public BulkObservableCollection<Bat> BatList => (BulkObservableCollection<Bat>)GetValue(BatListProperty);

        /// <summary>
        ///     The changing
        /// </summary>
        private bool _changing;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatEditor" /> class.
        /// </summary>
        /// The bat reference data context.
        public BatEditor()
        {
            SetValue(BatListProperty, new BulkObservableCollection<Bat>());
            BatList.Clear();

            InitializeComponent();
            DataContext = this;

            LoadDataToBatList();
            BatNameListBox.ItemsSource = BatList;
        }



        /// <summary>
        ///     Handles the Click event of the AddRecordButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void AddRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var errString = BatNameListBox.SelectedItem != null
                ? (BatNameListBox.SelectedItem as Bat).Validate()
                : "No bat selected to validate";
            //int index = BatNameListBox.SelectedIndex;
            if (string.IsNullOrWhiteSpace(errString))
            {
                var bat = new Bat { Name = "bat", Batgenus = "BatGenus", BatSpecies = "BatSpecies" };

                var max = short.MinValue;

                var bt = new BatTag { BatTag1 = "BatTag" };
                max = short.MinValue;
                foreach (var tg in bat.BatTags)
                    if (tg.SortIndex != null && tg.SortIndex.Value > max)
                        max = tg.SortIndex.Value;
                bt.SortIndex = ++max;

                bat.BatTags.Add(bt);

                int? maxi = int.MaxValue;
                foreach (var b in BatList)
                    if (b.SortIndex > maxi)
                        maxi = b.SortIndex;
                bat.SortIndex = maxi.Value + 1;
                BatList.Add(bat);
                var sorted = BatList.OrderBy(newbat => newbat.SortIndex);
                BatList.Clear();
                BatList.AddRange(sorted);
                BatNameListBox.ItemsSource = BatList;
                BatNameListBox.SelectedItem = bat;
            }
            else
            {
                DisplayInvalidErrorMessage(errString);
            }
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the BatNameListBox control. Ensures that the
        ///     old bat was properly filled out - if not it sets a flag to prevent recursion and
        ///     resets the selected index to the old value.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.Controls.SelectionChangedEventArgs" /> instance
        ///     containing the event data.
        /// </param>
        private void BatNameListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_changing) return;
            _changing = true;
            //ListBox senderListBox = sender as ListBox;
            if (e?.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                var deselected = (Bat)e.RemovedItems[0]; //is the item we are moving away from
                if (deselected != null)
                {
                    var index = BatNameListBox.Items.IndexOf(deselected);

                    var errString = deselected != null ? deselected.Validate() : "No bat to validate";
                    if (!string.IsNullOrWhiteSpace(errString))
                    {
                        BatNameListBox.SelectedIndex = index;
                        DisplayInvalidErrorMessage(errString);
                        _changing = false;
                        return;
                    }
                }
            }

            IdTagEditBox.Text = "";
            _changing = false;
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the BatTagsListView control. Copies the
        ///     selected tag text to the edit box.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.Controls.SelectionChangedEventArgs" /> instance
        ///     containing the event data.
        /// </param>
        private void BatTagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatTagsListView.SelectedItem != null)
                IdTagEditBox.Text = ((XmlElement)BatTagsListView.SelectedItem).InnerText;
        }

        /// <summary>
        ///     Handles the Click event of the DelRecordButton control. Deletes the selected bat from
        ///     the list of bats.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void DelRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBat = SelectedBat();
            var index = BatNameListBox.SelectedIndex;

            BatList.Remove(selectedBat);
            var sorted = BatList.OrderBy(bat => bat.SortIndex);
            BatList.Clear();
            BatList.AddRange(sorted);
            BatNameListBox.ItemsSource = BatList;
            BatNameListBox.SelectedIndex = index;
        }

        /// <summary>
        ///     Invalids the bat. Throws a message box warning of an invalid bat descriptor using the
        ///     provided string.
        /// </summary>
        /// <param name="v">
        ///     The v.
        /// </param>
        private void DisplayInvalidErrorMessage(string v)
        {
            var oldChanging = _changing;
            _changing = true;

            MessageBox.Show(v, "All fields must be completed", MessageBoxButton.OK);
            _changing = oldChanging;
        }

        /// <summary>
        ///     Handles the LostFocus event of the GenusTextBlock control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void GenusTextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GenusTextBlock.Text)) return;
            var selectedBat = SelectedBat();
            selectedBat.Batgenus = GenusTextBlock.Text;
            DBAccess.UpdateBat(selectedBat, null, null, null);

            //selectedBat.Descendants("BatGenus").FirstOrDefault().Value = GenusTextBlock.Text;
            RefreshBatNameListBox(true);
        }

        /// <summary>
        ///     Handles the LostFocus event of the IDTagEditBox control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void IDTagEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TagAddButton_Click(this, new RoutedEventArgs());
        }

        //public BulkObservableCollection<Bat> BatList;
        private void LoadDataToBatList()
        {
            //int lastSelectedIndex = BatNameListBox.SelectedIndex;
            BatList.Clear();
            BatList.AddRange(DBAccess.GetSortedBatList());
        }

        /// <summary>
        ///     Handles the Click event of the MoveDownRecordButton control. Moves the selected bat
        ///     name down in the list, unless it is already at the bottom
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void MoveDownRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatNameListBox.SelectedIndex >= 0)
            {
                var index = BatNameListBox.SelectedIndex;
                BatList.ElementAt(index).SortIndex++;
                BatList.ElementAt(index + 1).SortIndex--;
                var sorted = BatList.OrderBy(bat => bat.SortIndex);
                BatList.Clear();
                BatList.AddRange(sorted);
                BatNameListBox.ItemsSource = BatList;
                BatNameListBox.SelectedIndex = index;
            }
        }

        /// <summary>
        ///     Handles the Click event of the MoveUpRecordButton control. Moves the selected bat
        ///     name up one place inthe list unless it is already at the top.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void MoveUpRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatNameListBox.SelectedIndex > 0)
            {
                //var selectedBat = SelectedBat();

                var index = BatNameListBox.SelectedIndex;

                BatList.ElementAt(index).SortIndex--;
                BatList.ElementAt(index - 1).SortIndex++;
                var sorted = BatList.OrderBy(bat => bat.SortIndex);
                BatList.Clear();
                BatList.AddRange(sorted);
                BatNameListBox.ItemsSource = BatList;
                BatNameListBox.SelectedIndex = index;

                /*      BindingExpression be = BindingOperations.GetBindingExpression(this, BatList);
                      if (be != null) be.UpdateSource();*/
            }
        }

        /// <summary>
        ///     Handles the Click event of the OKButton control. Checks the validity of the currently
        ///     selected bat - if not OK displays a message box and does nothing else. If OK, returns
        ///     with a true result -
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatNameListBox.SelectedIndex >= 0)
            {
                var errstring = BatNameListBox.SelectedItem != null
                    ? (BatNameListBox.SelectedItem as Bat).Validate()
                    : "No bat to validate";
                if (!string.IsNullOrWhiteSpace(errstring))
                {
                    DisplayInvalidErrorMessage(errstring);
                    return;
                }
            }

            // OK, so the bat selected is valid, others are all untouched or checked when they were deselected
            DBAccess.UpdateBatList(BatList);

            DialogResult = true;
            Close();
        }

        private void RefreshBatNameListBox(bool noNewSelection)
        {
            if (noNewSelection) _changing = true;
            var lastSelectedIndex = BatNameListBox.SelectedIndex;

            var bats = from bat in BatList
                       orderby bat.SortIndex
                       select bat;
            short i = 1;
            foreach (var bat in bats) bat.SortIndex = i++;

            BatList.Clear();
            BatNameListBox.ItemsSource = BatList;
            BatNameListBox.SelectedIndex = lastSelectedIndex;

            try
            {
                if (lastSelectedIndex < BatNameListBox.Items.Count)
                    BatNameListBox.SelectedIndex = lastSelectedIndex >= 0 ? lastSelectedIndex : 0;
                else
                    BatNameListBox.SelectedIndex = BatNameListBox.Items.Count - 1;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        ///     Selecteds the bat. Returns the XElement from the master BatList corresponding to the
        ///     bat selected in the BatNameListBox.
        /// </summary>
        /// <returns>
        /// </returns>
        private Bat SelectedBat()
        {
            var selectedBatElement = (Bat)BatNameListBox.SelectedItem;
            return selectedBatElement;
        }

        /// <summary>
        ///     Handles the LostFocus event of the SpeciesTextBlock control. Forces the bat list to
        ///     be updated with the contents of the text box just in case it has been changed.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void SpeciesTextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SpeciesTextBlock.Text)) return;
            var selectedBat = SelectedBat();
            if (selectedBat != null)
            {
                selectedBat.BatSpecies = SpeciesTextBlock.Text;
                DBAccess.UpdateBat(selectedBat, null, null, null);
                //selectedBat.Descendants("BatSpecies").FirstOrDefault().Value = SpeciesTextBlock.Text;
                RefreshBatNameListBox(true);
            }
        }

        /// <summary>
        ///     Handles the Click event of the TagAddButton control. Adds the text in the edit box as
        ///     a new TagID
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void TagAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(IdTagEditBox.Text))
            {
                var selectedBat = SelectedBat(); // returns NULL !!!!!!!!!!!!!!!!!!!!!!!!!
                if (selectedBat != null)
                {
                    var tags = from tg in selectedBat.BatTags
                               orderby tg.SortIndex
                               select tg;

                    if (tags != null)
                    {
                        foreach (var tag in tags)
                            if (tag.BatTag1 == IdTagEditBox.Text)
                                return; // tag already exists so we can't add it again

                        foreach (var tag in tags) tag.SortIndex++;

                        var newTag = new BatTag { BatTag1 = IdTagEditBox.Text, SortIndex = 0 };
                        selectedBat.BatTags.Add(newTag);
                        DBAccess.UpdateBat(selectedBat, null, null, null);
                    }
                }

                RefreshBatNameListBox(true);
            }
        }

        /// <summary>
        ///     Handles the Click event of the TagDelButton control. Deletes the selected tag ID
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void TagDelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(IdTagEditBox.Text))
            {
                var selectedTag = IdTagEditBox.Text;
                var selectedBat = SelectedBat();
                if (selectedBat != null)
                {
                    var selTag = (from tg in selectedBat.BatTags
                                  where tg.BatTag1 == selectedTag
                                  select tg).First();

                    DBAccess.DeleteTag(selTag);
                }

                RefreshBatNameListBox(true);
            }
        }

        /// <summary>
        ///     Handles the Click event of the TagDownButton control. Moves the selected tag ID down
        ///     one place in the list if it is not already at the bottom
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void TagDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(IdTagEditBox.Text))
            {
                //String selectedTag = IDTagEditBox.Text;
                var selectedBat = SelectedBat();
                if (selectedBat != null)
                {
                    var tags = from tg in selectedBat.BatTags
                               orderby tg.SortIndex
                               select tg;

                    if (!tags.IsNullOrEmpty())
                    {
                        for (var i = 0; i < tags.Count(); i++) tags.ElementAt(i).SortIndex = (short)i;

                        for (var i = 0; i < tags.Count() - 1; i++)
                            if (tags.ElementAt(i).BatTag1 == IdTagEditBox.Text)
                            {
                                tags.ElementAt(i).SortIndex = (short)(i + 1);
                                tags.ElementAt(i + 1).SortIndex = (short)i;
                                break;
                            }

                        RefreshBatNameListBox(true);
                    }
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the TagUpButton control. Moves the selected item up one
        ///     place in the list if it not already at the top
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void TagUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(IdTagEditBox.Text))
            {
                //String selectedTag = IDTagEditBox.Text;
                var selectedBat = SelectedBat();
                if (selectedBat != null)
                {
                    var tags = from tg in selectedBat.BatTags
                               orderby tg.SortIndex
                               select tg;

                    if (!tags.IsNullOrEmpty())
                    {
                        for (var i = 0; i < tags.Count(); i++) tags.ElementAt(i).SortIndex = (short)i;

                        for (var i = 1; i < tags.Count(); i++)
                            if (tags.ElementAt(i).BatTag1 == IdTagEditBox.Text)
                            {
                                tags.ElementAt(i).SortIndex = (short)(i - 1);
                                tags.ElementAt(i - 1).SortIndex = (short)i;
                                break;
                            }

                        RefreshBatNameListBox(true);
                    }
                }
            }
        }
    }
}