/*
 *  Copyright 2016 Justin A T Halls

        Licensed under the Apache License, Version 2.0 (the "License");
        you may not use this file except in compliance with the License.
        You may obtain a copy of the License at

            http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS,
        WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        See the License for the specific language governing permissions and
        limitations under the License.

 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualStudio.Language.Intellisense;
using Binding = System.Windows.Data.Binding;
using Control = System.Windows.Controls.Control;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ComparisonWindow.xaml
    /// </summary>
    public partial class ComparisonWindow : Window
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public ComparisonWindow()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        ///     Returns the currently selected image or if no image is the first image in the image list.
        ///     If there are  no images returns null.
        /// </summary>
        /// <returns></returns>
        internal StoredImage GetSelectedImage()
        {
            StoredImage result = null;
            if (ComparisonStackPanel.SelectedItem is DisplayStoredImageControl selectedImageControl) result = selectedImageControl.storedImage;

            if (result == null)
                if (storedImageList != null && storedImageList.Count > 0)
                    result = storedImageList.First().storedImage;
            return result;
        }

        internal void AddImage(StoredImage image, bool asModified = false)
        {
            var displayImage = new DisplayStoredImageControl();
            displayImage.e_UpButtonPressed += DisplayImage_UpButtonPressed;
            displayImage.e_DownButtonPressed += DisplayImage_DownButtonPressed;
            displayImage.e_DelButtonPressed += DisplayImage_DelButtonPressed;
            displayImage.e_FullButtonRClicked += DisplayImage_FullButtonRClicked;
            displayImage.e_FidsButtonRClicked += DisplayImage_FidsButtonRClicked;
            displayImage.e_Duplicate += DisplayImage_Duplicate;
            //DisplayImage.DisplayImage.Source = image.image;
            //DisplayImage.CaptionTextBox.Text = image.caption;
            //DisplayImage.DescriptionTextBox.Text = image.description;
            displayImage.storedImage = image;
            displayImage.DataContext = displayImage.storedImage;
            displayImage.IsModified = asModified;
            //DisplayImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;

            var binding = new MultiBinding {Converter = new MultiscaleConverter()};
            binding.Bindings.Add(new Binding("ActualHeight") {Source = this});
            binding.Bindings.Add(new Binding("scaleValue") {Source = displayImage});

            //Binding binding = new Binding();
            //binding.Source = this;
            //binding.Path = new PropertyPath(ActualHeightProperty);

            //binding.Converter = new Times2Converter();
            //binding.ConverterParameter = "0.5";
            displayImage.SetBinding(HeightProperty, binding);

            binding = new MultiBinding {Converter = new MultiscaleConverter()};
            binding.Bindings.Add(new Binding("ActualWidth") {Source = this});
            binding.Bindings.Add(new Binding("scaleValue") {Source = displayImage});
            displayImage.SetBinding(WidthProperty, binding);
            storedImageList.Add(displayImage);

            var view = CollectionViewSource.GetDefaultView(ComparisonStackPanel.ItemsSource);
            view?.Refresh();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Focus();
        }

        /// <summary>
        ///     Event handler when the Fids button is right clicked to allow the FIDS state to be toggled for
        ///     all images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayImage_FidsButtonRClicked(object sender, EventArgs e)
        {
            var thisButton = sender as ToggleButton;
            var toChecked = thisButton.IsChecked;
            if (storedImageList != null && storedImageList.Count > 0)
                using (new WaitCursor("Enabling Fiducial Lines"))
                {
                    foreach (var item in storedImageList)
                        if (item!=null && item is DisplayStoredImageControl)
                        {
                            var dsic = item;
                            dsic?.SetImageFids(toChecked);
                        }
                }
        }

        /// <summary>
        ///     Event handler for the OnDuplicate event of DisplayStoredImageControl.  It is passed a DuplicateEeventArgs
        ///     which contains parameters for the fiduical lines and grid of the currently selected DisplayStoredImageControl.
        ///     These args are passed intact tot he duplicate function of DisplayStoredImageControl for every image in the list.
        ///     Only those with no existing fiducial ines will copy the lines, others will simply return.  Grid positions are
        ///     also set by that function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayImage_Duplicate(object sender, EventArgs e)
        {
            if (storedImageList != null && storedImageList.Count > 0)
                using (new WaitCursor("Copying Grid to Fiducial Lines"))
                {
                    foreach (var item in storedImageList)
                    {
                        var control = item as DisplayStoredImageControl;
                        if (control != null)
                            control.DuplicateThis(e as DuplicateEventArgs);
                    }
                }
        }

        private void DisplayImage_FullButtonRClicked(object sender, EventArgs e)
        {
            var thisButton = (sender as DisplayStoredImageControl).FullSizeButton;
            var toFull = thisButton.Content as string == "FULL";
            if (storedImageList != null && storedImageList.Count > 0)
                foreach (var item in storedImageList)
                    if (item != null)
                    {
                        var dsic = item;
                        dsic?.SetImageFull(toFull);
                    }
        }

        private void DisplayImage_DelButtonPressed(object sender, EventArgs e)
        {
            var item = sender as DisplayStoredImageControl;
            ComparisonStackPanel.SelectedItem = item;
            if (ComparisonStackPanel.SelectedIndex >= 0) storedImageList.RemoveAt(ComparisonStackPanel.SelectedIndex);
        }

        private void DisplayImage_DownButtonPressed(object sender, EventArgs e)
        {
            var item = sender as DisplayStoredImageControl;
            ComparisonStackPanel.SelectedItem = item;

            if (ComparisonStackPanel.SelectedIndex < storedImageList.Count - 1 &&
                ComparisonStackPanel.SelectedIndex >= 0)
            {
                var index = ComparisonStackPanel.SelectedIndex;

                var temp = storedImageList[index];
                storedImageList[index] = storedImageList[index + 1];
                storedImageList[index + 1] = temp;
                ComparisonStackPanel.SelectedIndex = index + 1;
            }
        }

        private void DisplayImage_UpButtonPressed(object sender, EventArgs e)
        {
            var item = sender as DisplayStoredImageControl;
            ComparisonStackPanel.SelectedItem = item;

            if (ComparisonStackPanel.SelectedIndex > 0)
            {
                var index = ComparisonStackPanel.SelectedIndex;

                var temp = storedImageList[index];
                storedImageList[index] = storedImageList[index - 1];
                storedImageList[index - 1] = temp;
                ComparisonStackPanel.SelectedIndex = index - 1;
            }

            //ICollectionView view = CollectionViewSource.GetDefaultView(ComparisonStackPanel.ItemsSource);
            //if (view != null) view.Refresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ComparisonStackPanel.Items != null && ComparisonStackPanel.Items.Count > 0)
                using (new WaitCursor("Saving Images to the Database..."))
                {
                    foreach (var item in ComparisonStackPanel.Items) (item as DisplayStoredImageControl).Save();
                }

            storedImageList.Clear();
            ShowInTaskbar = true;
            WindowState = WindowState.Minimized;

            e.Cancel = true;
        }

        private void ComparisonStackPanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var move = e.Delta;
            var dsic = sender as DisplayStoredImageControl;

            if (!(dsic?.Parent is ScrollViewer)) return;
            var sv = dsic.Parent as ScrollViewer;
            if (move > 0) sv.PageUp();
            if (move < 0) sv.PageDown();
        }

        /// <summary>
        ///     Imports all the pictures in a folder to the comparison window, but does not insert
        ///     them into the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportPicturesButton_Click(object sender, RoutedEventArgs e)
        {
            var folderpath = GetFolderPath();
            if (!string.IsNullOrWhiteSpace(folderpath))
            {
                var fileList = Directory.EnumerateFiles(folderpath, "*.png");
                //var FILEList= Directory.EnumerateFiles(folderpath, "*.PNG");

                //fileList = fileList.Concat<string>(FILEList);
                if (!fileList.IsNullOrEmpty())
                    using (new WaitCursor("Importing Image Files..."))
                    {
                        storedImageList.Clear();
                        foreach (var file in fileList)
                        {
                            var si = StoredImage.Load(file);
                            if (si != null) AddImage(si, true);
                        }
                    }
            }
        }

        /// <summary>
        ///     Exports all the pictures in the comparison window to a folder, but does not affect them
        ///     in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportPicturesButton_Click(object sender, RoutedEventArgs e)
        {
            var isPng = true;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) isPng = false;
            if (!storedImageList.IsNullOrEmpty())
            {
                var folderPath = GetFolderPath();
                using (new WaitCursor("Exporting Images..."))
                {
                    if (!string.IsNullOrWhiteSpace(folderPath))
                    {
                        if (!folderPath.EndsWith(@"\")) folderPath = folderPath + @"\";
                        var fileNumber = 1;
                        var captionsText = "";
                        foreach (var displayObject in storedImageList)
                        {
                            var displayImage = displayObject;
                            displayImage.Save();
                            var fname = displayImage.Export(folderPath, fileNumber, storedImageList.Count, isPng);
                            captionsText = captionsText + fname + "|" + displayImage.storedImage.caption + "|" +
                                           displayImage.storedImage.description + "\n";

                            //var image = displayImage.storedImage;
                            //image.Save(folderPath+fileNumber.ToString()+".png");
                            fileNumber++;
                        }

                        File.WriteAllText(folderPath + "captions.txt", captionsText, Encoding.UTF8);
                    }
                }
            }
        }

        /// <summary>
        ///     Asks the user to select a folder for the import or export of picture sets
        /// </summary>
        /// <returns></returns>
        private string GetFolderPath()
        {
            var folderPath = Directory.GetCurrentDirectory();

            using (var dialog = new OpenFileDialog())
            {
                dialog.DefaultExt = "*.*";
                dialog.Filter = "Image files (*.jpg)|*.jpg|Png files (*.png)|*.png|All Files (*.*)|*.*";
                dialog.FilterIndex = 3;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Title = "Select Folder in Folder Tree For Image files";
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Select Folder";

                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    //HeaderFileName = dialog.FileName;
                    //folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                    folderPath = Tools.GetPath(dialog.FileName);
                else
                    return null;
            }

            return folderPath;
        }

        private void ComparisonStackPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent, Source = sender
                };
                var parent = ((Control) sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        /// <summary>
        ///     Sorts the order of the images in the comparison window on the basis of the text in the
        ///     description.  To sort in a specific order add numbers at the start of the description.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortOnDescButton_Click(object sender, RoutedEventArgs e)
        {
            if (storedImageList == null || storedImageList.Count <= 0) return;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                RemoveLeadingNumbersFromDescriptions();
            }
            else
            {
                var ieList = new List<DisplayStoredImageControl>();
                ieList.AddRange(storedImageList);
                var sortedList = from item in ieList
                    orderby item.DescriptionTextBox.Text
                    select item;
                storedImageList.Clear();
                storedImageList.AddRange(sortedList);
            }
        }

        /// <summary>
        ///     Goes through the images list and removes all leading numbers in the descriptions fields
        /// </summary>
        private void RemoveLeadingNumbersFromDescriptions()
        {
            foreach (var t in storedImageList)
                t.storedImage.description = Denumber(t.storedImage.description);
        }

        /// <summary>
        ///     removes leading digits from the string
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        private string Denumber(string description)
        {
            description = description.Trim();
            while (!string.IsNullOrWhiteSpace(description) && char.IsDigit(description[0]))
                description = description.Substring(1);
            description = description.Trim();
            return description;
        }
        //public readonly BulkObservableCollection<DisplayStoredImageControl> storedImageList = new BulkObservableCollection<DisplayStoredImageControl>();

        #region storedImageList

        /// <summary>
        ///     storedImageList Dependency Property
        /// </summary>
        public static readonly DependencyProperty storedImageListProperty =
            DependencyProperty.Register("storedImageList", typeof(BulkObservableCollection<DisplayStoredImageControl>),
                typeof(ComparisonWindow),
                new FrameworkPropertyMetadata(new BulkObservableCollection<DisplayStoredImageControl>()));

        /// <summary>
        ///     Gets or sets the storedImageList property.  This dependency property
        ///     indicates ....
        /// </summary>
        public BulkObservableCollection<DisplayStoredImageControl> storedImageList
        {
            get => (BulkObservableCollection<DisplayStoredImageControl>) GetValue(storedImageListProperty);
            set => SetValue(storedImageListProperty, value);
        }

        internal void AddImageRange(BulkObservableCollection<StoredImage> images)
        {
            if (!images.IsNullOrEmpty())
                foreach (var img in images)
                    AddImage(img);
        }

        #endregion storedImageList
    }

    #region DivideBy2Converter (ValueConverter)

    /// <summary>
    ///     Converter to divide numeric values by 2
    /// </summary>
    public class DivideBy2Converter : IValueConverter

    {
        /// <summary>
        ///     Converter to divide values by 2
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                return (double) value / 2;
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     convert back not implemented
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion DivideBy2Converter (ValueConverter)
}