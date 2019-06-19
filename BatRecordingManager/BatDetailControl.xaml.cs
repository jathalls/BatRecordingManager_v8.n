﻿using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatDetailControl.xaml
    /// </summary>
    public partial class BatDetailControl : UserControl
    {
        /// <summary>
        ///     The list changed event lock
        /// </summary>
        private readonly object _listChangedEventLock = new object();

        private int _selectedCallIndex;

        /// <summary>
        ///     The list changed event
        /// </summary>
        private EventHandler _listChangedEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatDetailControl" /> class.
        /// </summary>
        public BatDetailControl()
        {
            //CallList = new BulkObservableCollection<Call>();

            InitializeComponent();
            selectedCallIndex = -1;
            DataContext = selectedBat;
            BatDetailImageScroller.IsReadOnly = true;
            BatCallControl.e_ShowImageButtonPressed += BatCallControl_ShowImageButtonPressed;
            BatDetailImageScroller.CanAdd = false;
        }

        private BulkObservableCollection<Call> CallList { get; } = new BulkObservableCollection<Call>();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public int selectedCallIndex
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return _selectedCallIndex; }
            set
            {
                _selectedCallIndex = value;
                BatDetailImageScroller.SelectedCallIndex = value;
                if (BatCallControl != null && CallList != null)
                {
                    if (CallList.Count > value && value >= 0)
                        BatCallControl.BatCall = CallList[value];
                    else
                        BatCallControl.BatCall = null;
                    CallCountTextBox.Text = CallList.Count.ToString();
                    CallIndexTextBox.Text = (value + 1).ToString();
                    if (value < 1)
                        PrevCallButton.IsEnabled = false;
                    else
                        PrevCallButton.IsEnabled = true;
                    if (value >= CallList.Count - 1)
                        NextCallButton.IsEnabled = false;
                    else
                        NextCallButton.IsEnabled = true;
                }
                else
                {
                    if (BatCallControl != null) BatCallControl.BatCall = null;
                    CallCountTextBox.Text = "0";
                    CallIndexTextBox.Text = "-";
                    PrevCallButton.IsEnabled = false;
                    NextCallButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        ///     EventHandler for the ImageButton embedded in the BatCallControl.
        ///     When this button is toggled the display of the images or Notes toggles
        ///     as well
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatCallControl_ShowImageButtonPressed(object sender, EventArgs e)
        {
            if (BatDetailImageScroller.ImageScrollerDisplaysBatImages)
                BatDetailImageScroller.ImageScrollerDisplaysBatImages = false;
            else
                BatDetailImageScroller.ImageScrollerDisplaysBatImages = true;
        }

        /// <summary>
        ///     restores the call list to the first call and resets the Images utton
        ///     to its non-pressed state.
        /// </summary>
        internal void Reset()
        {
            if (CallList.Any())
                selectedCallIndex = 0;
            else
                selectedCallIndex = -1;
            if (!BatDetailImageScroller.ImageScrollerDisplaysBatImages) BatCallControl.Reset();
        }

        /// <summary>
        ///     Event raised after the List property value has changed.
        /// </summary>
        public event EventHandler e_ListChanged
        {
            add
            {
                lock (_listChangedEventLock)
                {
                    _listChangedEvent += value;
                }
            }
            remove
            {
                lock (_listChangedEventLock)
                {
                    _listChangedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_ListChanged" /> event.
        /// </summary>
        /// <param name="e">
        ///     <see cref="EventArgs" /> object that provides the arguments for the event.
        /// </param>
        protected virtual void OnListChanged(EventArgs e)
        {
            EventHandler handler = null;

            lock (_listChangedEventLock)
            {
                handler = _listChangedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            var thisBat = selectedBat;
            if (thisBat == null) return;
            var sortIndex = BatTagsListView.SelectedIndex;
            var newTagForm = new NewTagForm();
            newTagForm.ShowDialog();
            if (newTagForm.DialogResult != null && newTagForm.DialogResult.Value)
                sortIndex = DBAccess.AddTag(newTagForm.TagText, thisBat.Id);
            OnListChanged(new EventArgs());
            BatTagsListView.SelectedIndex = sortIndex;
        }

        private void DelTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatTagsListView.SelectedItem == null) return;
            var tag = BatTagsListView.SelectedItem as BatTag;
            DBAccess.DeleteTag(tag);
            OnListChanged(new EventArgs());
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatTagsListView.SelectedItem != null && BatTagsListView.SelectedItem is BatTag tag)
            {
                var thisBat = DataContext as Bat;
                if (thisBat == null) return;
                var sortIndex = BatTagsListView.SelectedIndex;
                var newTagForm = new NewTagForm();
                newTagForm.TagText = tag.BatTag1;
                newTagForm.ShowDialog();
                if (newTagForm.DialogResult != null && newTagForm.DialogResult.Value)
                {
                    tag.BatTag1 = newTagForm.TagText;
                    sortIndex = DBAccess.UpdateTag(tag);
                    //sortIndex = DBAccess.AddTag(newTagForm.TagText, thisBat.Id);
                }

                OnListChanged(new EventArgs());
                if (sortIndex >= 0 && sortIndex < BatTagsListView.Items.Count)
                    BatTagsListView.SelectedIndex = sortIndex;
                else
                    BatTagsListView.SelectedIndex = 0;
            }
        }

        private void NextCallButton_Click(object sender, RoutedEventArgs e)
        {
            selectedCallIndex = selectedCallIndex + 1;
        }

        private void PrevCallButton_Click(object sender, RoutedEventArgs e)
        {
            selectedCallIndex = selectedCallIndex - 1;
        }

        private void BatTagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatTagsListView.SelectedItem == null)
            {
                EditTagButton.IsEnabled = false;
                DelTagButton.IsEnabled = false;
            }
            else
            {
                EditTagButton.IsEnabled = true;
                DelTagButton.IsEnabled = true;
            }
        }

        #region selectedBat

        /// <summary>
        ///     selectedBat Dependency Property
        /// </summary>
        public static readonly DependencyProperty selectedBatProperty =
            DependencyProperty.Register("selectedBat", typeof(Bat), typeof(BatDetailControl),
                new FrameworkPropertyMetadata(new Bat()));

        /// <summary>
        ///     Gets or sets the selectedBat property. This dependency property indicates ....
        /// </summary>
        public Bat selectedBat
        {
            get => (Bat) GetValue(selectedBatProperty);
            set
            {
                SetValue(selectedBatProperty, value);
                Reset();
                BatDetailImageScroller.Clear();
                if (value != null)
                {
                    CommonNameTextBlock.Text = value.Name;
                    LatinNameTextBlock.Text = value.Batgenus + " " + value.BatSpecies;
                    BatTagsListView.ItemsSource = value.BatTags;
                    BatNotesTextBox.Text = value.Notes;
                    if (!value.BatCalls.IsNullOrEmpty())
                    {
                        CallList.Clear();
                        CallList.AddRange(DBAccess.GetCallsForBat(value));
                        if (!CallList.IsNullOrEmpty())
                            selectedCallIndex = 0;
                        else
                            selectedCallIndex = -1;

                        BatCallLabel.Visibility = Visibility.Visible;
                        BatCallControl.Visibility = Visibility.Visible;

                        //batCallControl.BatCall = value.BatCalls.First().Call;
                    }
                    else
                    {
                        selectedCallIndex = -1;

                        CallList.Clear();
                        BatCallLabel.Visibility = Visibility.Hidden;
                        BatCallControl.Visibility = Visibility.Hidden;
                    }

                    BatDetailImageScroller.CurrentBat = value;
                    BatDetailImageScroller.CanAdd = false;
                    AddTagButton.IsEnabled = true;
                }
                else
                {
                    // value==null
                    CommonNameTextBlock.Text = "";
                    LatinNameTextBlock.Text = "";
                    BatTagsListView.ItemsSource = null;
                    BatNotesTextBox.Text = "";
                    selectedCallIndex = -1;
                    CallList.Clear();
                    BatCallLabel.Visibility = Visibility.Hidden;
                    BatCallControl.Visibility = Visibility.Hidden;
                    BatDetailImageScroller.CanAdd = false;
                    AddTagButton.IsEnabled = false;
                    EditTagButton.IsEnabled = false;
                    DelTagButton.IsEnabled = false;
                }
            }
        }

        #endregion selectedBat
    }

    #region BatLatinNameConverter (ValueConverter)

    /// <summary>
    /// </summary>
    public class BatLatinNameConverter : IValueConverter
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
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var bat = value as Bat;
                var latinName = "";
                if (bat != null) latinName = bat.Batgenus + " " + bat.BatSpecies;
                return latinName;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
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

    #endregion BatLatinNameConverter (ValueConverter)

    #region BatTagSortConverter (ValueConverter)

    /// <summary>
    /// </summary>
    public class BatTagSortConverter : IValueConverter
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

                var tagList = value as EntitySet<BatTag>;
                var sortedTagList = from tag in tagList
                    orderby tag.SortIndex
                    select tag;
                return sortedTagList;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
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

    #endregion BatTagSortConverter (ValueConverter)
}