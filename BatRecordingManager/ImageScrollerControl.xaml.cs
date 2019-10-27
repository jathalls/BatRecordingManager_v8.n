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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ImageScrollerControl.xaml
    ///     is used as a base class by BatAndCallImageScroller
    /// </summary>
    public partial class ImageScrollerControl : UserControl
    {
        private readonly object _buttonPressedEventLock = new object();

        private readonly object _imageDeletedEventLock = new object();
        private EventHandler<EventArgs> _buttonPressedEvent;
        private EventHandler<EventArgs> _imageDeletedEvent;

        private int _imageIndex = -1;

        private string _title = "";

        /// <summary>
        ///     default constructor for the class.  Clears the imageList and sets the
        ///     DataContext to the selectedImage which is null at this point
        /// </summary>
        public ImageScrollerControl()
        {
            imageList.Clear();

            InitializeComponent();
            DataContext = selectedImage;

            imageIndex = -1;
        }

        /// <summary>
        ///     protected read-only list of images to be displayed - the contents of the list may be changed
        /// </summary>
        public BulkObservableCollection<StoredImage> imageList { get; } = new BulkObservableCollection<StoredImage>();


        /// <summary>
        ///     getters and setters for a flag that controls the visibility of the ADD and EDIT buttons
        /// </summary>
        public bool IsEditable

        {
            get => EditImageButton.Visibility == Visibility.Visible;
            set
            {
                if (value)
                {
                    EditImageButton.Visibility = Visibility.Visible;
                    AddImageButton.Visibility = Visibility.Visible;

                    AddImageButton.IsEnabled = true;
                    DelImageButton.Visibility = Visibility.Visible;
                    DelImageButton.IsEnabled = true;
                    ImportImageButton.Visibility = Visibility.Visible;
                    ImportImageButton.IsEnabled = true;
                }
                else
                {
                    EditImageButton.Visibility = Visibility.Hidden;
                }
            }
        }

        public string Title
        {
            get => _title;

            set
            {
                _title = value;
                TitleTextBox.Text = value;
                if (value.Contains("Bat") || value.Contains("Call"))
                {
                    ImportImageButton.Visibility = Visibility.Visible;
                    ImportImageButton.IsEnabled = true;
                }
                else
                {
                    ImportImageButton.Visibility = Visibility.Hidden;
                    ImportImageButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        ///     getters and setters for an index into the imageList to the selected item in the list
        ///     sets the selectedImage as it changes and sets the visibility of navigation and editing
        ///     buttons as appropriate
        /// </summary>
        protected int imageIndex
        {
            get => _imageIndex;
            set
            {
                // if moving in the list, make sure the image in the list is updated to the selectedImage
                if (_imageIndex >= 0 && _imageIndex < imageList.Count && selectedImage != null)
                    imageList[_imageIndex] = selectedImage;

                // then change the index and associated details...
                _imageIndex = value;
                if (value < 0)
                {
                    selectedImage = null;
                    DisableAllButtons();
                    AddImageButton.IsEnabled = true;
                }
                else
                {
                    selectedImage = value < imageList.Count ? imageList[value] : null;
                    EnableAllButtons();
                }

                if (value == 0)
                {
                    FarLeftButton.IsEnabled = false;
                    OneLeftButton.IsEnabled = false;
                }

                if (value >= imageList.Count - 1 || imageIndex < 0)
                {
                    FarRightButton.IsEnabled = false;
                    OneRightButton.IsEnabled = false;
                }
                else
                {
                    FarRightButton.IsEnabled = true;
                    OneRightButton.IsEnabled = true;
                }

                if (imageIndex >= 0)
                    ImageNumberLabel.Content = imageIndex + 1 + " of " + imageList.Count;
                else
                    ImageNumberLabel.Content = "";
            }
        }

        /// <summary>
        ///     Adds a given image to the imageList
        /// </summary>
        /// <param name="newImage"></param>
        public void AddImage(StoredImage newImage)
        {
            if (imageList != null)
            {
                imageList.Add(newImage);

                imageIndex = imageList.Count - 1;
                selectedImage = imageList[imageIndex];
            }
        }

        /// <summary>
        ///     resets the selectedImage to the item in the imageList pointed to by the
        ///     imageIndex, or null if the index does not point to a valid entry
        /// </summary>
        public void Update()
        {
            if (imageIndex >= 0 && imageIndex < imageList.Count)
                imageList[imageIndex] = selectedImage;
            else
                selectedImage = null;
        }

        /// <summary>
        ///     deletes the currently selected image from the list but does not change the database
        /// </summary>
        public void DeleteImage()
        {
            if (!imageList.IsNullOrEmpty() && imageIndex >= 0 && imageIndex < imageList.Count)
            {
                var DeletedImageID = selectedImage.ImageID;
                imageList.Remove(selectedImage);
                selectedImage = null;
                CaptionTextBox.Text = "";
                DescriptionTextBox.Text = "";
                imageIndex--;
                if (imageIndex < 0 && imageList.Count > 0) imageIndex = 0;
                while (imageIndex > imageList.Count - 1) imageIndex--;

                OnImageDeleted(new ImageDeletedEventArgs(DeletedImageID));
            }
        }

        /// <summary>
        ///     disables all the controls buttons when there is no parent list that can be added to and no
        ///     images displayed to modify
        /// </summary>
        internal void EnableAllButtons()
        {
            FarLeftButton.IsEnabled = true;
            FarRightButton.IsEnabled = true;
            OneLeftButton.IsEnabled = true;
            OneRightButton.IsEnabled = true;
            AddImageButton.IsEnabled = true;
            DelImageButton.IsEnabled = true;
            EditImageButton.IsEnabled = true;
            FullScreenButton.IsEnabled = true;
            ImportImageButton.IsEnabled = true;
        }

        /// <summary>
        ///     Clears the list of currently displayed images but not the source lists from which
        ///     it gets populated.  The imageIndex is set to -1 and the selecetdImage to null.
        /// </summary>
        public void Clear()
        {
            imageList.Clear();
            imageIndex = -1;
            CaptionTextBox.Text = "";
            DescriptionTextBox.Text = "";

            AddImageButton.IsEnabled = true;
        }

        private void FarLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 0) imageIndex = 0;
        }

        /// <summary>
        ///     Disables all the buttons in the scrollers button bar
        /// </summary>
        internal void DisableAllButtons()
        {
            FarLeftButton.IsEnabled = false;
            FarRightButton.IsEnabled = false;
            OneLeftButton.IsEnabled = false;
            OneRightButton.IsEnabled = false;
            AddImageButton.IsEnabled = false;
            DelImageButton.IsEnabled = false;
            EditImageButton.IsEnabled = false;
            FullScreenButton.IsEnabled = false;
            ImportImageButton.IsEnabled = false;
        }

        internal void DisableAddButton()
        {
            AddImageButton.IsEnabled = false;
        }

        private void OneLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 1 && imageIndex > 0) imageIndex--;
        }

        private void OneRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 1 && imageIndex >= 0 && imageIndex < imageList.Count - 1)
                imageIndex++;
        }

        private void FarRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageList != null && imageList.Count > 1) imageIndex = imageList.Count - 1;
        }

        /// <summary>
        ///     Button to display the selected image full screen
        ///     A misnomer - adds the image to the comparisonwindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedImage != null) ComparisonHost.Instance.AddImage(selectedImage);
        }

        /// <summary>
        ///     Event raised after one of the handled buttons has been pressed.
        /// </summary>
        public event EventHandler<EventArgs> e_ButtonPressed
        {
            add
            {
                lock (_buttonPressedEventLock)
                {
                    _buttonPressedEvent += value;
                }
            }
            remove
            {
                lock (_buttonPressedEventLock)
                {
                    _buttonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_ButtonPressed" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnButtonPressed(ButtonPressedEventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_buttonPressedEventLock)
            {
                handler = _buttonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageIndex >= 0 && imageIndex < imageList.Count)
                OnButtonPressed(new ButtonPressedEventArgs(sender as Button, imageList[imageIndex], false));
            else
                OnButtonPressed(new ButtonPressedEventArgs(sender as Button, null, false));
        }

        /*
        private void PasteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AddImageButton.Content = "PASTE";
        }

        private void PasteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AddImageButton.Content = "ADD";
        }*/

        private void EditImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditImageButton.Content as string == "EDIT")
            {
                EditImageButton.Content = "SAVE";
                CaptionTextBox.IsReadOnly = false;
                DescriptionTextBox.IsReadOnly = false;
            }
            else
            {
                if (selectedImage.ImageID >= 0)
                {
                    DBAccess.UpdateImage(selectedImage);
                    if (imageIndex >= 0 && imageIndex < imageList.Count) imageList[imageIndex] = selectedImage;
                }

                EditImageButton.Content = "EDIT";
                CaptionTextBox.IsReadOnly = true;
                DescriptionTextBox.IsReadOnly = true;
            }
        }

        private void DelImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageIndex >= 0 && imageIndex < imageList.Count)
            {
                var fromDatabase = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                var args = new ButtonPressedEventArgs(sender as Button, imageList[imageIndex], fromDatabase);
                OnButtonPressed(args);
            }
            else
            {
                OnButtonPressed(new ButtonPressedEventArgs(sender as Button, null, false));
            }
        }

        private void Currentimage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var move = e.Delta;
            if (move > 0)
                OneRightButton_Click(sender, new RoutedEventArgs());
            else
                OneLeftButton_Click(sender, new RoutedEventArgs());
        }

        /// <summary>
        ///     Parses the StoredImage Uri for a collection of file names and opens those that have
        ///     existing files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (!String.IsNullOrWhiteSpace(selectedImage.Uri))
            {
                var fileNameSet = selectedImage.Uri.Split(';');
                foreach (var fn in fileNameSet)
                {
                    Tools.OpenWavFile(fn);
                }
            }*/
            if (selectedImage != null && selectedImage.isPlayable) selectedImage.Open();
        }

        internal void SetImportAllowed(bool allowed)
        {
            if (allowed)
            {
                ImportImageButton.Visibility = Visibility.Visible;
                ImportImageButton.IsEnabled = true;
            }
            else
            {
                ImportImageButton.Visibility = Visibility.Hidden;
                ImportImageButton.IsEnabled = false;
            }
        }

        /// <summary>
        ///     Sets up the image scroller control for view only mode, basically by disabling the
        ///     ADD button for when it is used in the image entry mode in which the add dialog is
        ///     permanently displayed and copies images across into the scroller as they are
        ///     created.
        /// </summary>
        internal void SetViewOnly(bool onoff)
        {
            AddImageButton.Visibility = onoff ? Visibility.Hidden : Visibility.Visible;
        }

        /// <summary>
        ///     Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> e_ImageDeleted
        {
            add
            {
                lock (_imageDeletedEventLock)
                {
                    _imageDeletedEvent += value;
                }
            }
            remove
            {
                lock (_imageDeletedEventLock)
                {
                    _imageDeletedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_ImageDeleted" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnImageDeleted(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_imageDeletedEventLock)
            {
                handler = _imageDeletedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     Get the currently selecetd image in the comparison window (if any) and
        ///     add it to the displayed list and also link it to the source of the currently displayed
        ///     list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComparisonHost.Instance != null)
            {
                var newImage = ComparisonHost.Instance.GetSelectedImage();
                if (newImage != null && !imageList.Contains(newImage))
                {
                    AddImage(newImage);
                    OnButtonPressed(new ButtonPressedEventArgs(sender as Button, newImage, false));
                }
            }
        }

        #region selectedImage

        /// <summary>
        ///     selectedImageProperty Dependency Property
        /// </summary>
        public static readonly DependencyProperty selectedImageProperty =
            DependencyProperty.Register(nameof(selectedImage), typeof(StoredImage), typeof(ImageScrollerControl),
                new FrameworkPropertyMetadata(null));


        /// <summary>
        ///     Gets or sets the selectedImageProperty property.  This dependency property
        ///     getters and setters for the currently displayed image
        ///     indicates ....
        /// </summary>
        public StoredImage selectedImage
        {
            get
            {
                var img = (StoredImage) GetValue(selectedImageProperty);

                if (img != null && !_locked)
                {
                    img.caption = CaptionTextBox.Text;
                    img.description = DescriptionTextBox.Text;
                }

                return img;
            }
            set
            {
                _locked = true; // since get reads the text boxes, make sure it can't while things are changing
                try
                {
                    if (value != null && CaptionTextBox != null && DescriptionTextBox != null)
                    {
                        //DataContext = value;
                        SetValue(selectedImageProperty, value);

                        CaptionTextBox.Text = value.caption;
                        DescriptionTextBox.Text = value.description;
                    }
                    else
                    {
                        SetValue(selectedImageProperty, value);
                    }
                }
                finally
                {
                    _locked = false;
                    DataContext = selectedImage;
                }
            }
        }

        private bool _locked;

        #endregion selectedImage

        #region IsReadOnly

        /// <summary>
        ///     IsReadOnly Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ImageScrollerControl),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     Gets or sets the IsReadOnly property.  This dependency property
        ///     indicates whether it is permissible to add, edit or delete images in
        ///     the collection.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool) GetValue(IsReadOnlyProperty);
            set
            {
                SetValue(IsReadOnlyProperty, value);
                if (value)
                {
                    AddImageButton.Visibility = Visibility.Hidden;
                    DelImageButton.Visibility = Visibility.Hidden;
                    //EditImageButton.Visibility = Visibility.Hidden;
                    IsEditable = false;
                    CaptionTextBox.IsReadOnly = true;
                    DescriptionTextBox.IsReadOnly = true;
                }
                else
                {
                    AddImageButton.Visibility = Visibility.Visible;
                    DelImageButton.Visibility = Visibility.Visible;
                    //EditImageButton.Visibility = Visibility.Hidden;
                    IsEditable = true;
                    CaptionTextBox.IsReadOnly = false;
                    DescriptionTextBox.IsReadOnly = false;
                }
            }
        }

        #endregion IsReadOnly
    }

    /// end class ImageScrollerControl
    /// <summary>
    ///     Arguments fort a ButtonPressed Event Handler
    /// </summary>
    [Serializable]
    public class ButtonPressedEventArgs : EventArgs
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public new static readonly ButtonPressedEventArgs Empty = new ButtonPressedEventArgs(null, null, false);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member


        #region Constructors

        /// <summary>
        ///     Constructs a new instance of the <see cref="ButtonPressedEventArgs" /> class.
        /// </summary>
        public ButtonPressedEventArgs(Button senderButton, StoredImage image, bool fromDatabase)
        {
            PressedButton = senderButton;
            Image = image;
            this.fromDatabase = fromDatabase;
        }

        #endregion Constructors

        #region Public Properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Button PressedButton { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public StoredImage Image;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        public bool fromDatabase { get; set; }

        #endregion Public Properties
    } // end class ButtonPressedEventArgs

    /// <summary>
    ///     ImageDeletedEventArgs
    /// </summary>
    [Serializable]
    public class ImageDeletedEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        ///     Constructs a new instance of the <see cref="ImageDeletedEventArgs" /> class.
        /// </summary>
        public ImageDeletedEventArgs(int ID)
        {
            imageID = ID;
        }

        #endregion Constructors

        #region Public

        public new static readonly ImageDeletedEventArgs Empty = new ImageDeletedEventArgs(-1);

        /// <summary>
        ///     ID of the image that has been removed from the list
        /// </summary>
        public int imageID { get; } = -1;

        #endregion Public
    }
} // end namespace