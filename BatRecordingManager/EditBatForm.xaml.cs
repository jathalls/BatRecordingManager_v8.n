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
using System.Data.Linq;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for EditBatForm.xaml
    /// </summary>
    public partial class EditBatForm : Window
    {
        /// <summary>
        ///     This flag is set if the image scroller is to display images of the bate.  If clear then
        ///     the ImageScroller displays images for the selected BatCall
        /// </summary>
        private bool _imageScrollerDisplaysBatImages = true;

        private int _selectedCallIndex;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EditBatForm" /> class.
        /// </summary>
        public EditBatForm()
        {
            InitializeComponent();
            NewBat = new Bat { Name = "Unknown", Batgenus = "Unknown", BatSpecies = "unknown", Id = -1 };
            CallList.Clear();

            NewBat.Notes = "";
            var bt = new BatTag { BatTag1 = "bat", BatID = NewBat.Id };
            NewBat.BatTags.Add(bt);
            //SelectedCallIndex = -1;
            AddNewTagButton.IsEnabled = true;
            DeleteTagButton.IsEnabled = true;
            DataContext = NewBat;
            //batCallControl.SetReadOnly(false);
            //EditBatFormImageScroller.ButtonPressed += EditBatFormImageScroller_ButtonPressed;
            EditBatFormImageScroller.IsReadOnly = false;
            BatCallControl.e_ShowImageButtonPressed += BatCallControl_ShowImageButtonPressed;
        }

        /// <summary>
        ///     The list of defined call types for this bat
        /// </summary>
        public BulkObservableCollection<Call> CallList { get; } = new BulkObservableCollection<Call>();

        //public BulkObservableCollection<BitmapSource> ImageList { get; } = new BulkObservableCollection<BitmapSource>();

        /// <summary>
        ///     SelectedCallIndex is the index to the specific call which is to be displayed in detail
        ///     in the Call detail control and whose images are to be displayed in the ImageScroller control
        ///     if the ImageScrollerDisplaysBatImages flag is cleared (i.e. the Image scroller is displaying
        ///     call images insted of bat images.
        ///     The Setter is responsible for updating the ImageScroller display and the Bat Call Detail display,
        ///     as well as setting the states of the Prev and Next  Call Buttons
        /// </summary>
        public int SelectedCallIndex
        {
            get => _selectedCallIndex;
            set
            {
                if (!_imageScrollerDisplaysBatImages) EditBatFormImageScroller.SelectedCallIndex = value;
                if (_selectedCallIndex >= 0 && _selectedCallIndex < CallList.Count)
                    CallList[_selectedCallIndex] = BatCallControl.BatCall;
                _selectedCallIndex = value;
                if (value >= 0 && value < CallList.Count)
                {
                    var maxIndex = CallList.Count - 1;
                    PrevCallButton.IsEnabled = value > 0;
                    NextCallButton.IsEnabled = value < maxIndex;
                    CallIndexTextBox.Text = (SelectedCallIndex + 1).ToString();
                    TotalCallsTextBox.Text = CallList.Count.ToString();

                    if (value >= 0 && value < CallList.Count)
                        BatCallControl.BatCall = CallList[value];
                    else
                        BatCallControl.BatCall = null;
                }
            }
        }

        private void AddCallButton_Click(object sender, RoutedEventArgs e)
        {
            Update();
            var buttonLabel = AddCallButton.Content as string;
            if (buttonLabel == "Add")
            {
                CallList.Add(new Call());
                SelectedCallIndex = CallList.Count - 1;
                BatCallControl.SetReadOnly(false);
                AddCallButton.Content = "Save";
                DeleteCallButton.Content = "Cancel";
                PrevCallButton.IsEnabled = false;
                NextCallButton.IsEnabled = false;
            }
            else if (buttonLabel == "Save")
            {
                AddCallButton.Content = "Add";
                DeleteCallButton.Content = "Del";
                if (SelectedCallIndex >= 0 && SelectedCallIndex < CallList.Count)
                {
                    CallList[SelectedCallIndex] = BatCallControl.BatCall;
                    SelectedCallIndex = CallList.Count - 1;
                }
            }
        }

        private void AddNewTagButton_Click(object sender, RoutedEventArgs e)
        {
            Update();
            if (NewBat == null)
            {
                NewBat = new Bat { Id = -1 };
            }

            if (NewBat.BatTags == null) NewBat.BatTags = new EntitySet<BatTag>();
            if (string.IsNullOrWhiteSpace(TagEditBox.Text)) return;
            //TextBox senderTextBox = sender as TextBox;
            if (!NewBat.BatTags.IsNullOrEmpty())
            {
                var matchingTags = from tg in NewBat.BatTags
                                   where tg.BatTag1 == TagEditBox.Text
                                   select tg;
                if (!matchingTags.IsNullOrEmpty())
                    return; // tag in the edit box is already in the tag list
                AddTag(TagEditBox.Text);
            }
            else
            {
                AddTag(TagEditBox.Text);
            }

            var view = CollectionViewSource.GetDefaultView(BatTagList.ItemsSource);
            view?.Refresh();
        }

        private void AddTag(string text)
        {
            Update();
            if (string.IsNullOrWhiteSpace(text)) return;
            var tag = DBAccess.GetTag(text);
            if (tag != null)
            {
                MessageBox.Show("Tag <" + text + "> is already in use by " + tag.Bat.Name, "Duplicate Tag");
                return;
            }

            if (NewBat == null)
            {
                NewBat = new Bat { Id = -1 };
            }

            if (NewBat.BatTags == null) NewBat.BatTags = new EntitySet<BatTag>();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var newTag = new BatTag { BatID = NewBat.Id, BatTag1 = text };
                NewBat.BatTags.Add(newTag);

                DataContext = NewBat;

                var view = CollectionViewSource.GetDefaultView(BatTagList.ItemsSource);
                view?.Refresh();
            }
        }

        /// <summary>
        ///     Responds to an event raised by the BatCallControl in the BatDetail pane which toggles
        ///     whether the local ImageScrollerControl should display the images associated with the
        ///     selected bat or the images associated with the selected BatCall.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatCallControl_ShowImageButtonPressed(object sender, EventArgs e)
        {
            //EditBatFormImageScroller.Clear();
            if (_imageScrollerDisplaysBatImages)
            {
                EditBatFormImageScroller.ImageScrollerDisplaysBatImages = false;
                _imageScrollerDisplaysBatImages = false;
            }
            else
            {
                EditBatFormImageScroller.ImageScrollerDisplaysBatImages = true;
                _imageScrollerDisplaysBatImages = true;
            }
        }

        private void BatTagList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e == null)
            {
                TagEditBox.Text = "";
                return;
            }

            if (e.AddedItems == null || e.AddedItems.Count <= 0)
            {
                TagEditBox.Text = "";
                return;
            }

            TagEditBox.Text = (e.AddedItems[0] as BatTag).BatTag1;
        }

        private void DeleteCallButton_Click(object sender, RoutedEventArgs e)
        {
            Update();
            var buttonLabel = DeleteCallButton.Content as string;
            if (buttonLabel == "Del")
            {
                var temp = SelectedCallIndex;
                CallList.RemoveAt(SelectedCallIndex);
                if (SelectedCallIndex >= CallList.Count) SelectedCallIndex--;
                else
                    SelectedCallIndex = temp;

                if (SelectedCallIndex <= 0) PrevCallButton.IsEnabled = false;
                if (SelectedCallIndex >= CallList.Count - 1) NextCallButton.IsEnabled = false;
                //CallIndexTextBox.Text = SelectedCallIndex.ToString();
                TotalCallsTextBox.Text = CallList.Count.ToString();
            }
            else if (buttonLabel == "Cancel")
            {
                CallList.RemoveAt(CallList.Count - 1);
                SelectedCallIndex = CallList.Count - 1;

                AddCallButton.Content = "Add";
                DeleteCallButton.Content = "Del";
            }
        }

        private void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            Update();
            if (NewBat == null)
            {
                NewBat = new Bat { Id = -1 };
            }

            if (NewBat.BatTags == null) NewBat.BatTags = new EntitySet<BatTag>();
            if (BatTagList.SelectedItem != null)
            {
                var selectedTag = BatTagList.SelectedItem as BatTag;
                if (NewBat.BatTags.Contains(selectedTag)) NewBat.BatTags.Remove(selectedTag);
            }

            DataContext = NewBat;
            var view = CollectionViewSource.GetDefaultView(BatTagList.ItemsSource);
            view?.Refresh();
        }

        /// <summary>
        ///     responds to the OK button by saving the current data to the database and closing the
        ///     eidt bat window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditBatFormOKButton_Click(object sender, RoutedEventArgs e)
        {
            string error = null;
            using (new WaitCursor("Saving bat form data..."))
            {
                NewBat.BatSpecies = BatSpeciesTextBlock.Text ?? "";
                NewBat.Batgenus = BatGenusTextBlock.Text ?? "";
                NewBat.Notes = BatNotesTextBlock.Text ?? "";
                if (!string.IsNullOrWhiteSpace(CommonNameTextBox.Text))
                    NewBat.Name = CommonNameTextBox.Text;
                else
                    NewBat.Name = NewBat.Batgenus + "_" + NewBat.BatSpecies;
                if (NewBat.BatTags == null || NewBat.BatTags.Count <= 0)
                {
                    if (NewBat.BatTags == null) NewBat.BatTags = new EntitySet<BatTag>();
                    AddTag(NewBat.Name);
                }

                Update();

                if (!_imageScrollerDisplaysBatImages) BatCallControl_ShowImageButtonPressed(this, new EventArgs());

                error = NewBat != null ? NewBat.Validate() : "No bat defined to validate";
                if (string.IsNullOrWhiteSpace(error))
                {
                    EditBatFormImageScroller.Update();
                    DBAccess.UpdateBat(NewBat, CallList, EditBatFormImageScroller.BatImages,
                        EditBatFormImageScroller.ListofCallImageLists);
                    BatCallControl.CallImageList.Clear();
                    foreach (var im in EditBatFormImageScroller.CallImageList) BatCallControl.CallImageList.Add(im);
                    DialogResult = true;

                    Close();
                }
            }

            if (!string.IsNullOrWhiteSpace(error)) MessageBox.Show(error);
        }

        private void NextCallButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCallIndex = SelectedCallIndex + 1;
            if (SelectedCallIndex <= 0) PrevCallButton.IsEnabled = false;
            if (SelectedCallIndex >= CallList.Count - 1) NextCallButton.IsEnabled = false;
            //CallIndexTextBox.Text = SelectedCallIndex.ToString();
        }

        private void PrevCallButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCallIndex = SelectedCallIndex - 1;
            if (SelectedCallIndex <= 0) PrevCallButton.IsEnabled = false;
            if (SelectedCallIndex >= CallList.Count - 1) NextCallButton.IsEnabled = false;
            //CallIndexTextBox.Text = SelectedCallIndex.ToString();
        }

        private void TagEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        ///     replaces the selected call in CallList with the copy in batCallControl
        /// </summary>
        private void Update()
        {
            if (SelectedCallIndex >= 0 && SelectedCallIndex < CallList.Count)
                CallList[SelectedCallIndex] = BatCallControl.BatCall;
        }

        #region newBat

        /// <summary>
        ///     newBat Dependency Property
        /// </summary>
        public static readonly DependencyProperty newBatProperty =
            DependencyProperty.Register(nameof(NewBat), typeof(Bat), typeof(EditBatForm),
                new FrameworkPropertyMetadata(new Bat()));

        /// <summary>
        ///     Gets or sets the newBat property. This dependency property indicates the bat to be edited or created
        /// </summary>
        public Bat NewBat
        {
            get => (Bat)GetValue(newBatProperty);
            set
            {
                SetValue(newBatProperty, value);
                if (BatCallControl != null)
                {
                    //EditBatFormImageScroller.CurrentBat = value;
                    //SelectedCallIndex = EditBatFormImageScroller.SelectedCallIndex;

                    //var rawCallList = DBAccess.GetCallsForBat(value);
                    CallList.Clear();
                    //if (rawCallList != null)
                    //{
                    CallList.AddRange(DBAccess.GetCallsForBat(value));
                    //}

                    if (CallList.Any())
                    {
                        SelectedCallIndex = 0;
                        BatCallControl.BatCall = CallList[0];
                    }
                    else
                    {
                        SelectedCallIndex = -1;
                        BatCallControl.BatCall = null;
                    }

                    if (BatCallControl != null && CallList != null && SelectedCallIndex >= 0)
                    {
                        //CallIndexTextBox.Text = SelectedCallIndex.ToString();
                        TotalCallsTextBox.Text = CallList.Count.ToString();
                        NumberOfCallsStackPanel.Visibility = Visibility.Visible;
                        PrevNextButtonBarStackPanel.Visibility = Visibility.Visible;
                        if (CallList.Count > SelectedCallIndex + 1) NextCallButton.IsEnabled = true;
                        PrevCallButton.IsEnabled = false;
                        DeleteCallButton.IsEnabled = true;
                        BatCallControl.SetReadOnly(false);
                    }
                    else
                    {
                        NumberOfCallsStackPanel.Visibility = Visibility.Hidden;
                        PrevNextButtonBarStackPanel.Visibility = Visibility.Hidden;
                        DeleteCallButton.IsEnabled = false;
                        BatCallControl.SetReadOnly(true);
                    }

                    DataContext = NewBat;
                }

                if (EditBatFormImageScroller != null)
                {
                    EditBatFormImageScroller.CurrentBat = value;
                    _imageScrollerDisplaysBatImages = true;
                    SelectedCallIndex = EditBatFormImageScroller.SelectedCallIndex;
                }
            }
        }

        #endregion newBat
    }
}