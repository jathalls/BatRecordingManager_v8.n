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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for DisplayStoredImageControl.xaml
    /// </summary>
    public partial class DisplayStoredImageControl : UserControl
    {
        /// Using a DependencyProperty as the backing store for storedImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty storedImageProperty =
            DependencyProperty.Register(nameof(storedImage), typeof(StoredImage), typeof(DisplayStoredImageControl),
                new PropertyMetadata(new StoredImage(null, "", "", -1)));

        #region GridControlsVisibility

        /// <summary>
        /// GridControlsVisibility Dependency Property
        /// </summary>
        public static readonly DependencyProperty GridControlsVisibilityProperty =
            DependencyProperty.Register("GridControlsVisibility", typeof(Visibility), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata((Visibility)Visibility.Hidden,
                    FrameworkPropertyMetadataOptions.None));

        /// <summary>
        /// Gets or sets the GridControlsVisibility property.  This dependency property 
        /// indicates whether the Grid controls are visible or not
        /// </summary>
        public Visibility GridControlsVisibility
        {
            get { return (Visibility)GetValue(GridControlsVisibilityProperty); }
            set { SetValue(GridControlsVisibilityProperty, value); }
        }

        #endregion



        private readonly double _defaultGridLeftMargin = 0.28d;
        private readonly double _defaultGridScale = 0.6782d;

        private readonly double _defaultGridTopMargin = 0.154d;


        private readonly object _delButtonPressedEventLock = new object();
        private readonly object _downButtonPressedEventLock = new object();

        private readonly object _duplicateEventLock = new object();

        private readonly object _fidsButtonRClickedEventLock = new object();
        private readonly object _upButtonPressedEventLock = new object();
        private Canvas _axisGrid = new Canvas();

        private EventHandler<EventArgs> _delButtonPressedEvent;

        private EventHandler<EventArgs> _downButtonPressedEvent;
        private EventHandler<EventArgs> _duplicateEvent;
        private EventHandler<EventArgs> _fidsButtonRClickedEvent;

        private int _gridToShow;


        //private bool _isPlacingFiducialLines = false;

        private bool _linesDrawn;

        private int _selectedLine = -1;

        private bool __showGrid = false;
        private bool _showGrid
        {
            get
            {
                return (__showGrid);
            }
            set
            {
                __showGrid = value;
                miFidsCopyGridFids.IsEnabled = value;
                Debug.WriteLine("_showGrid and miFidsCopyGridFids.IsEnabled set to " + value);
            }
        }
        private StoredImage _storedImage = new StoredImage(null, "", "", -1);

        private EventHandler<EventArgs> _upButtonPressedEvent;

        /// <summary>
        ///     Simple boolean to indicate if the storedImage has has grid lines added, deleted or changed and thus to
        ///     indicate if the gridlines need to be resaved
        /// </summary>
        public bool IsModified;


        /// <summary>
        ///     constructor for display of control for displaying comparison images
        /// </summary>
        public DisplayStoredImageControl()
        {
            InitializeComponent();
            Loaded += DisplayStoredImageControl_Loaded;
            DataContext = storedImage;
            AxisGrid675.DataContext = this;
            AxisGrid7029A.DataContext = this;
            miGridSpacer.DataContext = this;
            miEnlargeGrid5.DataContext = this;
            miEnlargeGrid1.DataContext = this;
            miShrinkGrid5.DataContext = this;
            miShrinkGrid1.DataContext = this;
            miCopyGridFids.DataContext = this;
            

            gridTopMargin = _defaultGridTopMargin;
            gridLeftMargin = _defaultGridLeftMargin;

            scaleValue = "0.5";
            gridScaleValue = _defaultGridScale;
            _showGrid = false;
            AxisGrid7029A.Visibility = Visibility.Hidden;
            AxisGrid675.Visibility = Visibility.Hidden;
            GridControlsVisibility = Visibility.Hidden;


            DisplayImageCanvas.Focus();
        }


        /// <summary>
        ///     The instance of a StoredImage to be displayed in this control
        /// </summary>
        public StoredImage storedImage
        {
            get => (StoredImage) GetValue(storedImageProperty);

            set
            {
                SetValue(storedImageProperty, DBAccess.GetImage(value));
                PlayButton.IsEnabled = value.isPlayable;
            }
        }

        /// <summary>
        ///     LineMap has two integers.  The key is the index of a Line in the children of displayImageCanvas
        ///     the value is the index of a HorizontalGridLine List of the storedImage.
        ///     The Y values of the Line are bound to the HorizontalGridLine element through a scaling
        ///     converter.  The line is moved by referencing the relevant HGL via this table and bumping the
        ///     value appropriately
        /// </summary>
        private Dictionary<int, int> HLineMap { get; set; } = new Dictionary<int, int>();

        /// <summary>
        ///     LineMap has two integers.  The key is the index of a Line in the children of displayImageCanvas
        ///     the value is the index of a VerticalGridLine List of the storedImage.
        ///     The Y values of the Line are bound to the VericalGridLine element through a scaling
        ///     converter.  The line is moved by referencing the relevant VGL via this table and bumping the
        ///     value appropriately
        /// </summary>
        private Dictionary<int, int> VLineMap { get; set; } = new Dictionary<int, int>();

        private void DisplayStoredImageControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                FiducialsButton.IsChecked = true;
                FiducialsButton_Click(this, new RoutedEventArgs());
            }
        }

        /// <summary>
        ///     Sets the state of the FIDS button according to the bool
        /// </summary>
        /// <param name="toChecked"></param>
        internal void SetImageFids(bool? toChecked)
        {
            FiducialsButton.IsChecked = toChecked;
        }

        /// <summary>
        ///     takes the line information from the DuplicateEvwentArgs and applies them to the current image
        ///     if there are not any existing definitions
        /// </summary>
        /// <param name="duplicateEventArgs"></param>
        internal void DuplicateThis(DuplicateEventArgs duplicateEventArgs)
        {
            if (duplicateEventArgs != null)
            {
                if (gridTopMargin != _defaultGridTopMargin) gridTopMargin = duplicateEventArgs.TopMargin;
                if (gridLeftMargin != _defaultGridLeftMargin) gridLeftMargin = duplicateEventArgs.LeftMargin;
                if (gridScaleValue != _defaultGridScale) gridScaleValue = duplicateEventArgs.Scale;
                if (storedImage.HorizontalGridlines != null && storedImage.HorizontalGridlines.Count > 0) return;
                if (storedImage.VerticalGridLines != null && storedImage.VerticalGridLines.Count > 0) return;
                //if we get here there are no horizontal or vertical gridlines defined
                storedImage.HorizontalGridlines.Clear();
                foreach (var hglProp in duplicateEventArgs.HLineProportions)
                    storedImage.HorizontalGridlines.Add((int) (hglProp * storedImage.image.Height));
                storedImage.VerticalGridLines.Clear();
                foreach (var vglProp in duplicateEventArgs.VLineProportions)
                    storedImage.VerticalGridLines.Add((int) (vglProp * storedImage.image.Width));

                FiducialsButton_Click(this, new RoutedEventArgs());
            }
        }


        /// <summary>
        ///     Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> e_DelButtonPressed
        {
            add
            {
                lock (_delButtonPressedEventLock)
                {
                    _delButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (_delButtonPressedEventLock)
                {
                    _delButtonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     event raised by pressing DOWN
        /// </summary>
        public event EventHandler<EventArgs> e_DownButtonPressed
        {
            add
            {
                lock (_downButtonPressedEventLock)
                {
                    _downButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (_downButtonPressedEventLock)
                {
                    _downButtonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> e_UpButtonPressed
        {
            add
            {
                lock (_upButtonPressedEventLock)
                {
                    _upButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (_upButtonPressedEventLock)
                {
                    _upButtonPressedEvent -= value;
                }
            }
        }


        /// <summary>
        ///     ERROR needs to be displayed at full screen, full size for best resolution and even then the lower part of
        ///     the image is lost - starts at about 30k.  Investigate taking a copy of the bitmapImage and drawing the
        ///     gridlines on it directly and then exporting that rather than exporting the canvas which uses the image as a
        ///     background. Corrected - see image.Save for details.
        /// </summary>
        /// <param name="folderPath"></param>
        internal string Export(string folderPath, int index, int count, bool isPng)
        {
            if (!folderPath.EndsWith(@"\")) folderPath = folderPath + @"\";
            var fname = folderPath + "BatCallImage";
            if (storedImage.ImageID >= 0) fname = storedImage.GetName();
            if (fname.Contains(@"\")) fname = fname.Substring(fname.LastIndexOf('\\') + 1);
            var formatString = @"{0,1:D1} - {1}";
            if (count >= 10) formatString = @"{0,2:D2} - {1}";
            if (count >= 100) formatString = @"{0,3:D3} - {1}";


            fname = string.Format(formatString, index, fname);
            var i = 0;

            while (File.Exists(folderPath + fname + (i > 0 ? "-" + i : "") + ".png")) i++;
            var image = storedImage;
            fname = fname + (i > 0 ? "-" + i : "");
            image.Save(folderPath + fname + (isPng ? ".png" : ".jpg"), FiducialsButton.IsChecked ?? false);
            File.WriteAllText(folderPath + fname + ".txt", storedImage.caption + ":- " + storedImage.description);
            return fname + (isPng ? ".png" : ".jpg");
        }

        /// <summary>
        ///     Saves any fiducial lines associated with the image at the current settings, replacing any previously
        ///     defined fiducial lines
        /// </summary>
        internal void Save()
        {
            if (IsModified) storedImage.Update();
            IsModified = false;
        }

        /// <summary>
        ///     Raises the <see cref="e_DelButtonPressed" /> event.
        ///     If the EventArgs flag is false (default state) then the image is deleted from the window
        ///     and not from the database.
        ///     If the flag is true then image is deleted from the database.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnDelButtonPressed(BoolEventArgs e)
        {
            
            EventHandler<EventArgs> handler = null;

            lock (_delButtonPressedEventLock)
            {
                handler = _delButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     Raises the <see cref="e_DownButtonPressed" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnDownButtonPressed(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_downButtonPressedEventLock)
            {
                handler = _downButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     Event raised by pressing UP
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnUpButtonPressed(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_upButtonPressedEventLock)
            {
                handler = _upButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> e_FidsButtonRClicked
        {
            add
            {
                lock (_fidsButtonRClickedEventLock)
                {
                    _fidsButtonRClickedEvent += value;
                }
            }
            remove
            {
                lock (_fidsButtonRClickedEventLock)
                {
                    _fidsButtonRClickedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_FidsButtonRClicked" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnFidsButtonRClicked(object sender, EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_fidsButtonRClickedEventLock)
            {
                handler = _fidsButtonRClickedEvent;

                if (handler == null)
                    return;
            }

            handler(sender, e);
        }

        private bool AdjustFiducials(object sender, KeyEventArgs e, int moveSize)
        {
            var result = false;
            switch (e.Key)
            {
                case Key.Tab:
                    if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        result = DecrementSelectedLine();
                    else
                        result = IncrementSelectedLine();
                    break;

                case Key.Up:
                    result = MoveLineUp(moveSize);
                    break;

                case Key.Down:
                    result = MoveLineDown(moveSize);
                    break;

                case Key.Right:
                    result = MoveLineRight(moveSize);
                    break;

                case Key.Left:
                    result = MoveLineLeft(moveSize);
                    break;

                case Key.Delete:
                    if (_selectedLine >= 0) result = DeleteGridLine();
                    break;
            }

            DisplayImageCanvas.UpdateLayout();
            DisplayImageCanvas.Focus();
            return result;
        }

        /// <summary>
        ///     A key is pressed while the FiducialGrid is Visible and the GridAdjustToggleButton is pressed.
        ///     Arrow keys move the grid up, down left and right;
        ///     SHIFT-arrowkeys move the top of the grid up and down compressing the row sizes
        ///     CTRL-arrowkeys moves the bottom of the grid up and down  compressing/expanding the row heights
        ///     LEFT-ALT-arrowkeys move the left margin of the grid right and left compressing/expanding the column widths
        ///     RIGHT-ALT-arrowkeys move the right margin of the grid compressing/expanding the column widths
        ///     NUMPAD-+ add a column at the right
        ///     NUMPAD - delete the rightmost column
        ///     NUMPAD * add a row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdjustGridSize(object sender, KeyEventArgs e)
        {
        }

        /// <summary>
        ///     Clears all Fiducial gridlines from the canvas and clears the H and V lineMaps.
        ///     Sets the selectedLine to -1.
        /// </summary>
        private void ClearGridlines()
        {
            _linesDrawn = false;
            if (DisplayImageCanvas != null)
            {
                if (DisplayImageCanvas.Children != null) DisplayImageCanvas.ClearExceptGrids();
                if (HLineMap != null)
                    HLineMap.Clear();
                else
                    HLineMap = new Dictionary<int, int>();
                if (VLineMap != null)
                    VLineMap.Clear();
                else
                    VLineMap = new Dictionary<int, int>();
                _selectedLine = -1;
            }
        }

        /// <summary>
        ///     Clicking the COPY button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CpyButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            System.Windows.Forms.DataFormats.Format myFormat = System.Windows.Forms.DataFormats.GetFormat("myFormat");

            /* Creates a new object and stores it in a DataObject using myFormat
             * as the type of format. */ /*
            MyNewObject myObject = new MyNewObject();
            DataObject myDataObject = new DataObject(myFormat.Name, myObject);

            // Copies myObject into the clipboard.
            Clipboard.SetDataObject(myDataObject);

            // Performs some processing steps.

            // Retrieves the data from the clipboard.
            IDataObject myRetrievedObject = Clipboard.GetDataObject();

            // Converts the IDataObject type to MyNewObject type.
            MyNewObject myDereferencedObject = (MyNewObject)myRetrievedObject.GetData(myFormat.Name);

            // Prints the value of the Object in a textBox.
            //textBox1.Text = myDereferencedObject.MyObjectValue;

            */
            Debug.WriteLine("Clicked the COPY button");
            var siFormat = DataFormats.GetDataFormat("siFormat");
            var obj = new DataObject();

            obj.SetText("***" + storedImage.ImageID);
            obj.SetImage(storedImage.image);
            Clipboard.SetDataObject(obj);

            //Clipboard.SetImage(storedImage.image);
            //Clipboard.SetText("***"+storedImage.ImageID.ToString());
            /*bool isImage = Clipboard.ContainsImage();
            bool isText = Clipboard.ContainsText();
            var cbtext = Clipboard.GetText();
            var cbimage = Clipboard.GetImage();*/
            DisplayImageCanvas.Focus();
        }

        private bool DecrementSelectedLine()
        {
            var result = false;
            if (!isGridHighlighted)
            {
                _selectedLine--;
            }
            while (_selectedLine >= 0 && !(DisplayImageCanvas.Children[_selectedLine] is Line)) _selectedLine--;
            if (_selectedLine < -1) _selectedLine = DisplayImageCanvas.Children.Count - 1;
            HighlightSelectedLine();
            result = true;
            return result;
        }

        /// <summary>
        ///     deletes the currently selected Fiducial Grid Line from the storedImage
        ///     and redraws all the lines from scratch.  Selected line is preserved if possible.
        /// </summary>
        private bool DeleteGridLine()
        {
            var result = false;
            if (_selectedLine >= 0 && DisplayImageCanvas.Children != null &&
                DisplayImageCanvas.Children.Count > _selectedLine)
                try
                {
                    var previouslySelectedLine = _selectedLine;
                    var lineIndex = -1;
                    var line = DisplayImageCanvas.Children[_selectedLine] as Line;
                    if (line.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        // we have selected a vertical line
                        lineIndex = VLineMap[_selectedLine];
                        if (lineIndex >= 0 && lineIndex < storedImage.VerticalGridLines.Count)
                            storedImage.VerticalGridLines.RemoveAt(lineIndex);
                    }
                    else
                    {
                        // we have selected a horizontal line
                        lineIndex = HLineMap[_selectedLine];
                        if (lineIndex >= 0 && lineIndex < storedImage.HorizontalGridlines.Count)
                            storedImage.HorizontalGridlines.RemoveAt(lineIndex);
                    }

                    ClearGridlines();
                    DrawAllLines();
                    if (previouslySelectedLine > 0 && previouslySelectedLine < DisplayImageCanvas.Children.Count)
                        _selectedLine = previouslySelectedLine;
                    else
                        _selectedLine = DisplayImageCanvas.Children.Count - 1;
                    HighlightSelectedLine();
                    result = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Line selection error:- " + ex.Message);
                    Tools.ErrorLog("DeleteGridLine:-" + ex.Message);
                    result = false;
                }

            DisplayImageCanvas.Focus();
            return result;
        }

        private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            OnDelButtonPressed(new BoolEventArgs());
            DisplayImageCanvas.Focus();
        }

        /// <summary>
        ///     EventHander for when the window is loaded - identifies the current
        ///     window and adds a handler for the PreviewKeyDown event which is used to
        ///     adjust the size of the scale grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayImage_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);

            DisplayImageCanvas.Focus();
            e.Handled = true;
        }

        /// <summary>
        ///     creates a fiducial line horizontally on the image which will be dragged by the mouse
        ///     and made permanent by releasing the mouse button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var pos = e.GetPosition(DisplayImageCanvas);
                bool horizontal = true;
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    horizontal = false;
                }
                DisplayImage_AddFiducial(horizontal,pos);
            }
        }

        private void DisplayImage_AddFiducial(bool horizontal,Point pos)
        {

            if (FiducialsButton.IsChecked ?? false)
            {
                //var pos = e.GetPosition(DisplayImageCanvas);
                Debug.WriteLine("X=" + pos.X + " Y=" + pos.Y + " dic.W=" + DisplayImageCanvas.ActualWidth +
                                " dic.H=" + DisplayImageCanvas.ActualHeight + "\n");

                if (pos.X >= 0 && pos.X < DisplayImageCanvas.ActualWidth && pos.Y >= 0 &&
                    pos.Y < DisplayImageCanvas.ActualHeight)
                {
                    //Tools.InfoLog("Right Mouse Button");
                    _selectedLine = -1;
                    HighlightSelectedLine();
                    var isVertical = false;
                    var imageLineIndex = -1;

                    var line = new Line();
                    if (!horizontal)
                    {
                        isVertical = true;

                        if (storedImage.VerticalGridLines == null) storedImage.VerticalGridLines = new List<int>();
                        storedImage.VerticalGridLines.Add(WidthDeScale(pos.X));
                        imageLineIndex = storedImage.VerticalGridLines.Count - 1;
                        DrawLine(imageLineIndex, Orientation.VERTICAL);

                        //TOD add binding to VGL of storedImage
                    }
                    else
                    {
                        try
                        {
                            isVertical = false;

                            var newGl = HeightDeScale(pos.Y);

                            if (storedImage.HorizontalGridlines == null)
                                storedImage.HorizontalGridlines = new List<int>();
                            storedImage.HorizontalGridlines.Add(newGl);
                            //Tools.InfoLog("At canvas=" + pos.Y + " Image=" + newGL);

                            imageLineIndex = storedImage.HorizontalGridlines.Count - 1;
                            DrawLine(imageLineIndex, Orientation.HORIZONTAL);
                        }
                        catch (Exception ex)
                        {
                            Tools.ErrorLog("££££££££££   " + ex.Message + "::" + ex);
                            ClearGridlines();
                            DrawAllLines();
                            return;
                        }
                    }

                    _selectedLine = DisplayImageCanvas.Children.Count - 1;
                    //Tools.InfoLog("Selected line " + selectedLine);
                    if (VLineMap == null) VLineMap = new Dictionary<int, int>();
                    if (HLineMap == null) HLineMap = new Dictionary<int, int>();

                    VLineMap.Add(_selectedLine, imageLineIndex);
                    HLineMap.Add(_selectedLine, imageLineIndex);

                    HighlightSelectedLine();
                    DisplayImageCanvas.UpdateLayout();
                    IsModified = true;
                }
            }

        }

        private void DisplayImage_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void displayImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine("MouseLeftButtonDown");
            DisplayImageCanvas.Focus();
        }

        private void displayImageCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                //Debug.WriteLine("dic.PreviewKeyDown");
                var isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
                var isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
                var canvas = sender as Canvas;
                Focus();

                if ((FiducialsButton.IsChecked ?? false) && isCtrlPressed && e.Key == Key.D)
                {
                    // CTRL-D while fiducials is active causes the fiducial lines of the current image (if any)
                    // to be duplicated to all images that do not already have some fiducial lines
                    // through an event handler so that the action is taken by the parent which has access to allthe sibling images
                    if (canvas != null && canvas.Children.Count > 2)
                    {
                        var hglProps = new List<double>();
                        var vglProps = new List<double>();
                        foreach (var hgl in storedImage.HorizontalGridlines)
                            hglProps.Add(hgl / storedImage.image.Height);
                        foreach (var vgl in storedImage.VerticalGridLines) vglProps.Add(vgl / storedImage.image.Width);
                        OnDuplicate(new DuplicateEventArgs(hglProps, vglProps,
                            _axisGrid == null ? DuplicateEventArgs.Grid.K675 :
                            _axisGrid == AxisGrid675 ? DuplicateEventArgs.Grid.K675 : DuplicateEventArgs.Grid.K7029,
                            gridLeftMargin, gridTopMargin, gridScaleValue));

                        return;
                    }
                }

                var moveSize = 5;
                var scaleSize = 1.1d;
                if (isShiftPressed)
                {
                    moveSize = 1;
                    scaleSize = 1.005d;
                }

                var moveScale = 0.002d;

                //Debug.WriteLine("wKey Previewed =" + e.Key.ToString());


                //var stackPanel = (StackPanel)(canvas.Parent as Grid).Parent);

                if (canvas != null)
                {
                    if ((FiducialsButton.IsChecked ?? false) && !isGridHighlighted)
                    {
                        //Debug.WriteLine("Adjust Fiducials");
                        if (AdjustFiducials(sender, e, moveSize))
                        {
                            e.Handled = true;
                            IsModified = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            //var dispImCont = cwin.ComparisonStackPanel.SelectedItem as DisplayStoredImageControl;

                            if (_showGrid)
                            {
                                if (GridSelectionComboBox.SelectedIndex == 0 ||
                                    GridSelectionComboBox.SelectedIndex == 1)
                                    _axisGrid = AxisGrid675;
                                else
                                    _axisGrid = AxisGrid7029A;

                                var img = DisplayImageCanvas;

                                var margin = _axisGrid.Margin;

                                switch (e.Key)
                                {
                                    case Key.Tab:
                                        if (AdjustFiducials(sender, e, moveSize))
                                        {
                                            IsModified = true;
                                        }
                                        e.Handled = true;
                                        break;

                                    case Key.PageUp:
                                        gridScaleValue = gridScaleValue * scaleSize;
                                        e.Handled = true;
                                        break;

                                    case Key.PageDown:
                                        gridScaleValue = gridScaleValue * (1 / scaleSize);
                                        e.Handled = true;
                                        break;

                                    case Key.Up:

                                        gridTopMargin -= moveSize * moveScale;
                                        if (gridTopMargin < 0) gridTopMargin = 0;
                                        e.Handled = true;
                                        break;

                                    case Key.Down:

                                        gridTopMargin += moveSize * moveScale;
                                        var hgridProportion = _axisGrid.ActualHeight / DisplayImageCanvas.ActualHeight;
                                        var hspaceProportion = 1.0d - hgridProportion;
                                        if (gridTopMargin > hspaceProportion) gridTopMargin = hspaceProportion;

                                        e.Handled = true;
                                        break;

                                    case Key.Right:
                                        // increase gridLeftMargin by moveSize*.005
                                        // gridLeftMargin is the proportion of displayImageCanvas.Width where the top elft corner will be

                                        gridLeftMargin += moveSize * moveScale;

                                        // the left margin proportion must be less than the space left by the grid
                                        var gridProportion = _axisGrid.ActualWidth / DisplayImageCanvas.ActualWidth;
                                        var spaceProportion = 1.0d - gridProportion;

                                        if (gridLeftMargin > spaceProportion) gridLeftMargin = spaceProportion;

                                        e.Handled = true;
                                        break;

                                    case Key.Left:

                                        gridLeftMargin -= moveSize * moveScale;
                                        if (gridLeftMargin < 0) gridLeftMargin = 0;
                                        e.Handled = true;
                                        break;
                                }

                                e.Handled = true;
                            }
                        }
                        catch (NullReferenceException nre)
                        {
                            Debug.WriteLine(nre);
                        }
                    }
                }

                var oldwidth = DisplayImageCanvas.RenderSize.Width;
                DisplayImageCanvas.RenderSize = new Size(oldwidth + 1, DisplayImageCanvas.RenderSize.Height);
                DisplayImageCanvas.InvalidateVisual();
                DisplayImageCanvas.UpdateLayout();
                //this.InvalidateVisual();
                //this.UpdateLayout();
                DisplayImageCanvas.RenderSize = new Size(oldwidth, DisplayImageCanvas.RenderSize.Height);
                DisplayImageCanvas.Focus();
            }
        }


        private void DownImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                OnDownButtonPressed(new EventArgs());
                DisplayImageCanvas.Focus();
            }
        }

        private void DrawAllLines()
        {
            Debug.WriteLine("DrawAllLines...");
            if (!_linesDrawn)
            {
                Debug.WriteLine(".....OK");
                if (storedImage.HorizontalGridlines != null)
                {
                    var i = 0;
                    foreach (var gridline in storedImage.HorizontalGridlines)
                    {
                        DrawLine(i, Orientation.HORIZONTAL);
                        HLineMap.Add(DisplayImageCanvas.Children.Count - 1, i++);
                        VLineMap.Add(DisplayImageCanvas.Children.Count - 1, i - 1);
                    }
                }

                if (storedImage.VerticalGridLines != null)
                {
                    var i = 0;
                    foreach (var gridline in storedImage.VerticalGridLines)
                    {
                        DrawLine(i, Orientation.VERTICAL);
                        VLineMap.Add(DisplayImageCanvas.Children.Count - 1, i++);
                        HLineMap.Add(DisplayImageCanvas.Children.Count - 1, i - 1);
                    }
                }
            }

            _linesDrawn = true;
            Debug.WriteLine("Number of lines is:-" + (DisplayImageCanvas.Children.Count - 2));
        }

        /// <summary>
        ///     draws the
        /// </summary>
        /// <param name="indexToGridLine"></param>
        /// <param name="direction"></param>
        private void DrawLine(int indexToGridLine, Orientation direction)
        {
            var line = new Line {Stroke = Brushes.Black, StrokeThickness = 1};


            if (direction == Orientation.HORIZONTAL && indexToGridLine >= 0 &&
                indexToGridLine <= storedImage.image.Height)
            {
                line.HorizontalAlignment = HorizontalAlignment.Stretch;
                line.VerticalAlignment = VerticalAlignment.Center;
                //line.Y1 = HeightScale(gridline);
                //line.Y2 = line.Y1;
                //line.X1 = 0;
                //Binding binding = new Binding();
                //binding.Source = displayImageCanvas;
                //binding.Path = new PropertyPath("ActualWidth");
                //BindingOperations.SetBinding(line, Line.X2Property, binding);

                var mbXBinding = new MultiBinding {Converter = new LeftMarginConverter()};
                var binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualWidth))
                };

                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualHeight))
                };
                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding {Source = this, Path = new PropertyPath(nameof(storedImage))};
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.X1Property, mbXBinding);

                mbXBinding = new MultiBinding {Converter = new RightMarginConverter()};
                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualWidth))
                };

                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualHeight))
                };
                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding {Source = this, Path = new PropertyPath(nameof(storedImage))};
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.X2Property, mbXBinding);


                var mBinding = new MultiBinding {Converter = new HGridLineConverter()};

                binding = new Binding {Source = indexToGridLine.ToString()};
                //double proportion = FindHScaleProportion(gridline);
                mBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualWidth))
                };
                //binding.Source = this;
                mBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualHeight))
                };
                //binding.Source = this;
                mBinding.Bindings.Add(binding);

                /*binding = new Binding();
                binding.Source = this;
                binding.Path = new PropertyPath("storedImage");*/
                binding = new Binding
                {
                    Source = storedImage, BindsDirectlyToSource = true, NotifyOnSourceUpdated = true
                };
                mBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.Y1Property, mBinding);
                BindingOperations.SetBinding(line, Line.Y2Property, mBinding);
            }
            else
            {
                line.VerticalAlignment = VerticalAlignment.Stretch;
                line.HorizontalAlignment = HorizontalAlignment.Center;
                //line.X1 = WidthScale(gridline);
                //line.X2 = line.X1;
                //line.Y1 = 0;
                //Binding binding = new Binding();
                //binding.Source = displayImageCanvas;
                //binding.Path = new PropertyPath("ActualHeight");
                //BindingOperations.SetBinding(line, Line.Y2Property, binding);

                var mbXBinding = new MultiBinding {Converter = new TopMarginConverter()};
                var binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualWidth))
                };

                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualHeight))
                };
                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding {Source = this, Path = new PropertyPath(nameof(storedImage))};
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.Y1Property, mbXBinding);

                mbXBinding = new MultiBinding {Converter = new BottomMarginConverter()};
                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualWidth))
                };

                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualHeight))
                };
                //binding.Source = this;
                mbXBinding.Bindings.Add(binding);

                binding = new Binding {Source = this, Path = new PropertyPath(nameof(storedImage))};
                mbXBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.Y2Property, mbXBinding);


                var mBinding = new MultiBinding {Converter = new VGridLineConverter()};
                binding = new Binding {Source = indexToGridLine.ToString()};
                //double proportion = FindHScaleProportion(gridline);
                mBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualWidth))
                };
                //binding.Source = this;
                mBinding.Bindings.Add(binding);

                binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Canvas), 1),
                    Path = new PropertyPath(nameof(ActualHeight))
                };
                //binding.Source = this;
                mBinding.Bindings.Add(binding);

                binding = new Binding {Source = this, Path = new PropertyPath(nameof(storedImage))};
                mBinding.Bindings.Add(binding);

                BindingOperations.SetBinding(line, Line.X1Property, mBinding);
                BindingOperations.SetBinding(line, Line.X2Property, mBinding);
            }

            DisplayImageCanvas.Children.Add(line);

            _selectedLine = DisplayImageCanvas.Children.Count - 1;
            DisplayImageCanvas.UpdateLayout();
        }

        private void FiducialsButton_Checked(object sender, RoutedEventArgs e)
        {
            FiducialsButton_Click(sender, e);
        }

        /// <summary>
        ///     Triggered by clicking the FIDS Toggle Button, toggles the display of
        ///     fiduciary lines on the image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FiducialsButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                ClearGridlines();
                if (FiducialsButton.IsChecked ?? false)
                {
                    //axisGrid675.Visibility = Visibility.Hidden;
                    //axisGrid7029A.Visibility = Visibility.Hidden;

                    //GridSelectionComboBox.Visibility = Visibility.Hidden;
                    //FiducialGrid.Visibility = Visibility.Hidden;

                    //showGrid = false;

                    DrawAllLines();
                    _selectedLine = -1;
                }

                DisplayImageCanvas.Focus();
            }
        }

        private void FiducialsButton_Unchecked(object sender, RoutedEventArgs e)
        {
            FiducialsButton_Click(sender, e);
        }

        /// <summary>
        ///     Actually handles MouseLeftButtonUp to prevent action when mouse is right clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullSizeButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("FullSizeButtonClick");


            var thisButton = sender as Button;

            SetImageFull(thisButton.Content as string == "FULL");


            BringIntoView();
            Focus();
            DisplayImageCanvas.Focus();
        }

        private void FullSizeButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("MouseRightButtonUp");
            if (!e.Handled)
            {
                e.Handled = true;
                OnFullButtonRClicked(e);
                BringIntoView();
                Focus();
                DisplayImageCanvas.Focus();
            }
        }

        private void FiducialsButton_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Mouse Right button uP on FIDS");
            if (!e.Handled)
            {
                e.Handled = true;
                OnFidsButtonRClicked(sender, e);
                BringIntoView();
                Focus();
                DisplayImageCanvas.Focus();
            }
        }

        public void SetImageFull(bool fullSize)
        {
            SetImageSize(fullSize ? 1.0d : 0.5d);
            FullSizeButton.Content = fullSize ? "HALF" : "FULL";
        }


        /// <summary>
        ///     Given an X position in the displayCanvas, return the corresponding position in the
        ///     original image
        /// </summary>
        /// <param name="x1"></param>
        /// <returns></returns>
        private int GetImageXPosition(double x1)
        {
            var pos = (int) (x1 / DisplayImageCanvas.ActualWidth * storedImage.image.Width);
            return pos;
        }

        /// <summary>
        ///     Given a Y position within the displayed canvas, finde the corresponding Y coordinate
        ///     in the original image
        /// </summary>
        /// <param name="y1"></param>
        /// <returns></returns>
        private int GetImageYPosition(double y1)
        {
            var pos = (int) (y1 / DisplayImageCanvas.ActualHeight * storedImage.image.Height);
            return pos;
        }

        private void GridButton_Click(object sender, RoutedEventArgs e)
        {
            //FiducialsButton.IsChecked = false;

            if (_showGrid)
            {
                HighlightGrid(false);
                _gridToShow = 0;
                if (AxisGrid675.Visibility == Visibility.Visible) _gridToShow = 675;
                if (AxisGrid7029A.Visibility == Visibility.Visible) _gridToShow = 7029;
                AxisGrid675.Visibility = Visibility.Hidden;
                AxisGrid7029A.Visibility = Visibility.Hidden;
                GridSelectionComboBox.Visibility = Visibility.Hidden;
                GridControlsVisibility = Visibility.Hidden;
                //FiducialGrid.Visibility = Visibility.Hidden;

                _showGrid = false;
                
            }
            else
            {
                //axisGrid.Visibility = Visibility.Visible;
                if (_gridToShow == 675)
                {
                    AxisGrid675.Visibility = Visibility.Visible;
                    _axisGrid = AxisGrid675;
                    GridControlsVisibility = Visibility.Visible;
                }
                else
                {
                    AxisGrid675.Visibility = Visibility.Hidden;
                }

                if (_gridToShow == 7029)
                {
                    AxisGrid7029A.Visibility = Visibility.Visible;
                    _axisGrid = AxisGrid7029A;
                    GridControlsVisibility = Visibility.Visible;
                }
                else
                {
                    AxisGrid7029A.Visibility = Visibility.Hidden;
                }

                GridSelectionComboBox.Visibility = Visibility.Visible;
                GridSelectionComboBox.IsDropDownOpen = true;
                _showGrid = true;
                
            }
        }

        private void GridSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridSelectionComboBox.IsDropDownOpen = false;
            if (_showGrid)
            {
                if (GridSelectionComboBox.SelectedIndex == 0 || GridSelectionComboBox.SelectedIndex == 1)
                {
                    AxisGrid675.Visibility = Visibility.Visible;
                    AxisGrid7029A.Visibility = Visibility.Hidden;
                    _axisGrid = AxisGrid675;
                    //gridTopMargin = 0.1d;
                    //gridLeftMargin = 0.1d;
                    _gridToShow = 675;
                    GridControlsVisibility = Visibility.Visible;
                    //FiducialGrid.Visibility = Visibility.Hidden;
                }

                if (GridSelectionComboBox.SelectedIndex == 2 || GridSelectionComboBox.SelectedIndex == 3)
                {
                    AxisGrid7029A.Visibility = Visibility.Visible;
                    AxisGrid675.Visibility = Visibility.Hidden;
                    _axisGrid = AxisGrid7029A;
                    //gridTopMargin = 0.1d;
                    //gridLeftMargin = 0.1d;
                    _gridToShow = 7029;
                    GridControlsVisibility = Visibility.Visible;
                    //FiducialGrid.Visibility = Visibility.Hidden;
                }

                if (GridSelectionComboBox.SelectedIndex == 4)
                {
                    AxisGrid675.Visibility = Visibility.Hidden;
                    AxisGrid7029A.Visibility = Visibility.Hidden;
                    _gridToShow = 0;
                    GridControlsVisibility = Visibility.Hidden;
                    //FiducialGrid.Visibility = Visibility.Visible;
                }
            }

            DisplayImageCanvas.Focus();
        }

        private int HeightDeScale(double y)
        {
            var hscale = DisplayImageCanvas.ActualWidth / storedImage.image.Width;
            var vscale = DisplayImageCanvas.ActualHeight / storedImage.image.Height;
            var actualScale = Math.Min(hscale, vscale);

            var rightAndLeftMargins = Math.Abs(DisplayImageCanvas.ActualWidth - storedImage.image.Width * actualScale);
            var topAndBottomMargins =
                Math.Abs(DisplayImageCanvas.ActualHeight - storedImage.image.Height * actualScale);

            var positionInScaledImage = y - topAndBottomMargins / 2;
            var proportionOfScaledImage = positionInScaledImage / (storedImage.image.Height * actualScale);
            var positionInImage = proportionOfScaledImage * storedImage.image.Height;
            return (int) positionInImage;
        }


        /// <summary>
        ///     Finds the ratio of the gridline position to the stored image height making allowance for the fit of
        ///     the image in the background of the displayImageCanvas in that it might be stretched to fit
        ///     vertically or horizontally but not both.  The returned value, when multiplied by the actual
        ///     height of the canvas should determine the location of the horizontal gridline on the canvas.
        /// </summary>
        /// <param name="linePositionInImage"></param>
        /// <param name="displayImageCanvas"></param>
        /// <param name="storedImage"></param>
        /// <returns></returns>
        public static double FindHScaleProportion(int linePositionInImage, double canvasWidth, double canvasHeight,
            StoredImage storedImage)
        {
            //Debug.WriteLine("============================================================================================");
            var hscale = canvasWidth / storedImage.image.Width;
            var vscale = canvasHeight / storedImage.image.Height;
            var actualScale = Math.Min(hscale, vscale);
            //Debug.WriteLine("Scale: H=" + hscale + " V=" + vscale + " Actual=" + actualScale);

            var linePositionAsProportionOfImage = linePositionInImage / storedImage.image.Height;
            //Debug.WriteLine("Initial:- Position=" + linePositionInImage + " of Heigh=" + storedImage.image.Height+" prop="+linePositionAsProportionOfImage);

            var linePositionInScaledImage = linePositionAsProportionOfImage * (storedImage.image.Height * actualScale);
            //Debug.WriteLine("Position In scale=" + linePositionInScaledImage+" in canvas of (h/w) "+canvasHeight+"/"+canvasWidth);

            var rightAndLeftMargins = Math.Abs(canvasWidth - storedImage.image.Width * actualScale);
            var topAndBottomMargins = Math.Abs(canvasHeight - storedImage.image.Height * actualScale);
            //Debug.WriteLine("Margins:- r+l=" + rightAndLeftMargins + " t+b=" + topAndBottomMargins);
            var linePositionInCanvas = linePositionInScaledImage + topAndBottomMargins / 2;

            var linePositionAsProportionOfCanvas = linePositionInCanvas / canvasHeight;
            //Debug.WriteLine("Pos in Canvas=" + linePositionInCanvas+" As proportion="+linePositionAsProportionOfCanvas);
            //Debug.WriteLine("----------------------------------------------------------------------------------------------");
            return linePositionAsProportionOfCanvas;
        }

        /// <summary>
        ///     Finds the ratio of the gridline position to the stored image height making allowance for the fit of
        ///     the image in the background of the displayImageCanvas in that it might be stretched to fit
        ///     vertically or horizontally but not both.  The returned value, when multiplied by the actual
        ///     height of the canvas should determine the location of the horizontal gridline on the canvas.
        /// </summary>
        /// <param name="linePositionInImage"></param>
        /// <param name="displayImageCanvas"></param>
        /// <param name="storedImage"></param>
        /// <returns></returns>
        public static double FindVScaleProportion(int linePositionInImage, double canvasWidth, double canvasHeight,
            StoredImage storedImage)
        {
            //Debug.WriteLine("============================================================================================");
            var hscale = canvasWidth / storedImage.image.Width;
            var vscale = canvasHeight / storedImage.image.Height;
            var actualScale = Math.Min(hscale, vscale);
            //Debug.WriteLine("Scale: H=" + hscale + " V=" + vscale + " Actual=" + actualScale);

            var linePositionAsProportionOfImage = linePositionInImage / storedImage.image.Width;
            //Debug.WriteLine("Initial:- Position=" + linePositionInImage + " of Heigh=" + storedImage.image.Height + " prop=" + linePositionAsProportionOfImage);

            var linePositionInScaledImage = linePositionAsProportionOfImage * (storedImage.image.Width * actualScale);
            //Debug.WriteLine("Position In scale=" + linePositionInScaledImage + " in canvas of (h/w) " + canvasHeight + "/" + canvasWidth);

            var rightAndLeftMargins = Math.Abs(canvasWidth - storedImage.image.Width * actualScale);
            var topAndBottomMargins = Math.Abs(canvasHeight - storedImage.image.Height * actualScale);
            //Debug.WriteLine("Margins:- r+l=" + rightAndLeftMargins + " t+b=" + topAndBottomMargins);
            var linePositionInCanvas = linePositionInScaledImage + rightAndLeftMargins / 2;

            var linePositionAsProportionOfCanvas = linePositionInCanvas / canvasWidth;
            //Debug.WriteLine("Pos in Canvas=" + linePositionInCanvas + " As proportion=" + linePositionAsProportionOfCanvas);
            //Debug.WriteLine("----------------------------------------------------------------------------------------------");
            return linePositionAsProportionOfCanvas;
        }


        private void HighlightSelectedLine()
        {
            if (DisplayImageCanvas.Children != null)
            {
                foreach (var child in DisplayImageCanvas.Children)
                    if (child is Line line)
                        line.StrokeThickness = 1;
                if (_selectedLine >= 0)
                {
                    if (DisplayImageCanvas.Children[_selectedLine] is Line)
                        (DisplayImageCanvas.Children[_selectedLine] as Line).StrokeThickness = 2;
                }
                else
                {
                    if (_showGrid)
                    {
                        if (isGridHighlighted)
                        {
                            HighlightGrid(false);
                        }
                        else
                        {
                            HighlightGrid(true);
                            
                        }
                    }
                    
                }
            }

            DisplayImageCanvas.UpdateLayout();
            DisplayImageCanvas.Focus();
        }

        private bool isGridHighlighted = false;

        private void HighlightGrid(bool setHighlighted)
        {
            var currentGrid = AxisGrid675;

            if (_gridToShow == 675)
            {
                currentGrid = AxisGrid675;
            }
            else
            {
                currentGrid = AxisGrid7029A;
            }
            foreach(var child in currentGrid.Children)
            {
                if (child is Line)
                {
                    if (setHighlighted)
                    {
                        (child as Line).StrokeThickness = 2;
                    }
                    else
                    {
                        (child as Line).StrokeThickness = 1;
                    }
                }
            }
            isGridHighlighted = setHighlighted;
        }

        private bool IncrementSelectedLine()
        {
            Debug.Write("Incrementing from " + _selectedLine);
            var result = false;
            if (!isGridHighlighted)
            {
                _selectedLine++;
            }
            while (_selectedLine < DisplayImageCanvas.Children.Count && _selectedLine>=0 &&
                   !(DisplayImageCanvas.Children[_selectedLine] is Line)) _selectedLine++;

            if (_selectedLine >= DisplayImageCanvas.Children.Count) _selectedLine = -1;
            HighlightSelectedLine();
            Debug.WriteLine(" to " + _selectedLine);
            result = true;
            return result;
        }


        private bool MoveLineLeft(int moveSize)
        {
            var result = false;
            if (DisplayImageCanvas.Children != null && _selectedLine >= 0 &&
                DisplayImageCanvas.Children.Count > _selectedLine)
                if (DisplayImageCanvas.Children[_selectedLine] is Line)
                {
                    var line = DisplayImageCanvas.Children[_selectedLine] as Line;
                    if (line.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        var xy = VLineMap[_selectedLine];
                        storedImage.VerticalGridLines[xy] -= moveSize;
                        if (storedImage.VerticalGridLines[xy] < 0) storedImage.VerticalGridLines[xy] = 0;

                        //line.X1 = WidthScale(storedImage.VerticalGridLines[xy]);
                        //line.X2 = line.X1;
                        result = true;
                    }
                }

            return result;
        }

        private bool MoveLineRight(int moveSize)
        {
            var result = false;
            if (DisplayImageCanvas.Children != null && _selectedLine >= 0 &&
                DisplayImageCanvas.Children.Count > _selectedLine)
                if (DisplayImageCanvas.Children[_selectedLine] is Line)
                {
                    var line = DisplayImageCanvas.Children[_selectedLine] as Line;
                    if (line.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        var xy = VLineMap[_selectedLine];
                        storedImage.VerticalGridLines[xy] += moveSize;
                        if (storedImage.VerticalGridLines[xy] > storedImage.image.Width)
                            storedImage.VerticalGridLines[xy] = (int) storedImage.image.Width;

                        //line.X1 = WidthScale(storedImage.VerticalGridLines[xy]);
                        //ine.X2 = line.X1;
                        result = true;
                    }
                }

            return result;
        }

        private bool MoveLineDown(int moveSize)
        {
            var result = false;
            if (DisplayImageCanvas.Children != null && _selectedLine >= 0 &&
                DisplayImageCanvas.Children.Count > _selectedLine)
                if (DisplayImageCanvas.Children[_selectedLine] is Line)
                {
                    var line = DisplayImageCanvas.Children[_selectedLine] as Line;
                    if (line.HorizontalAlignment == HorizontalAlignment.Stretch)
                    {
                        var xy = HLineMap[_selectedLine];
                        storedImage.HorizontalGridlines[xy] += moveSize;
                        if (storedImage.HorizontalGridlines[xy] > storedImage.image.Height)
                            storedImage.HorizontalGridlines[xy] = (int) storedImage.image.Height;
                        //line.Y1 = HeightScale(storedImage.HorizontalGridlines[xy]);
                        //line.Y2 = line.Y1;
                        result = true;
                    }
                }

            return result;
        }

        private bool MoveLineUp(int moveSize)
        {
            //Debug.WriteLine("--------------------------MOVELINE-UP---------------------------------");
            var result = false;
            if (DisplayImageCanvas.Children != null && _selectedLine >= 0 &&
                DisplayImageCanvas.Children.Count > _selectedLine)
                if (DisplayImageCanvas.Children[_selectedLine] is Line)
                {
                    var line = DisplayImageCanvas.Children[_selectedLine] as Line;
                    if (line.HorizontalAlignment == HorizontalAlignment.Stretch)
                    {
                        //Debug.WriteLine("Old Y=" + (double)line.GetValue(Line.Y1Property));

                        var xy = HLineMap[_selectedLine];
                        //Debug.WriteLine("Old Gridline=" + storedImage.HorizontalGridlines[xy]);
                        storedImage.HorizontalGridlines[xy] -= moveSize;
                        if (storedImage.HorizontalGridlines[xy] < 0) storedImage.HorizontalGridlines[xy] = 0;

                        result = true;
                    }
                }

            //Debug.WriteLine("-----------------------------------------" + result + "-----------------------------------------------");
            return result;
        }

        private void RotateImage90(bool clockwise)
        {
            var angle = clockwise ? 90 : -90;
            if (DisplayImageCanvas != null)
            {
                if (DisplayImageCanvas.LayoutTransform is RotateTransform transform)
                {
                    DisplayImageCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    transform.Angle += angle;
                }
                else
                {
                    DisplayImageCanvas.LayoutTransform = new RotateTransform();
                    transform = DisplayImageCanvas.LayoutTransform as RotateTransform;
                    DisplayImageCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    transform.Angle += angle;
                }

                while (transform.Angle < -180) transform.Angle += 360;

                while (transform.Angle > 180) transform.Angle -= 360;
            }

            DisplayImageCanvas.Focus();
        }

        private void RotateImageButton_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(true);
        }

        /// <summary>
        ///     sets the size of the displayImage panel by adjusting the binding
        ///     converter parameter.
        /// </summary>
        /// <param name="v"></param>
        private void SetImageSize(double v)
        {
            scaleValue = v.ToString();
        }

        private void UpImageButton_Click(object sender, RoutedEventArgs e)
        {
            OnUpButtonPressed(new EventArgs());
        }

        private int WidthDeScale(double x)
        {
            var hscale = DisplayImageCanvas.ActualWidth / storedImage.image.Width;
            var vscale = DisplayImageCanvas.ActualHeight / storedImage.image.Height;
            var actualScale = Math.Min(hscale, vscale);

            var rightAndLeftMargins = Math.Abs(DisplayImageCanvas.ActualWidth - storedImage.image.Width * actualScale);
            var topAndBottomMargins =
                Math.Abs(DisplayImageCanvas.ActualHeight - storedImage.image.Height * actualScale);

            var positionInScaledImage = x - rightAndLeftMargins / 2;
            var proportionOfScaledImage = positionInScaledImage / (storedImage.image.Width * actualScale);
            var positionInImage = proportionOfScaledImage * storedImage.image.Width;
            return (int) positionInImage;
        }

        /// <summary>
        ///     Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> e_Duplicate
        {
            add
            {
                lock (_duplicateEventLock)
                {
                    _duplicateEvent += value;
                }
            }
            remove
            {
                lock (_duplicateEventLock)
                {
                    _duplicateEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_Duplicate" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnDuplicate(DuplicateEventArgs e)
        {
            Debug.WriteLine(e.ToString());
            EventHandler<EventArgs> handler = null;

            lock (_duplicateEventLock)
            {
                handler = _duplicateEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     If <see langword="abstract" />GRID is displayed and the mouse is left-clicked, then Fiducial lines are
        ///     turned on (regardless of the previous state), all existing fiducial lines are deleted and a new set are
        ///     drawn to match the locations of the lines in the displayed grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FiducialsButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return; // not a CTRL-Click
            if (!_showGrid) return; // GRID is not displayed so do nothing
            if (!e.Handled)
            {
                e.Handled = true;
                CopyGridToFids();
            }
        }

        private void CopyGridToFids()
        {
            var gridWidth = 0.0d;
            var gridHeight = 0.0d;
            var gridTop = 0.0d;
            var gridLeft = 0.0d;
            var numHLines = 0;
            var numVLines = 0;

            ClearGridlines();
            storedImage.HorizontalGridlines.Clear();
            storedImage.VerticalGridLines.Clear();
            if (_axisGrid != null)
            {
                gridWidth = _axisGrid.ActualWidth;
                gridHeight = _axisGrid.ActualHeight;
                gridLeft = gridLeftMargin * DisplayImageCanvas.ActualWidth;
                gridTop = gridTopMargin * DisplayImageCanvas.ActualHeight;
                if (_gridToShow == 675)
                {
                    numHLines = 7;
                    numVLines = 6;
                }
                else
                {
                    numHLines = 9;
                    numVLines = 6;
                }

                var hLineSpacing = gridHeight / (numHLines - 1);
                var vLineSpacing = gridWidth / (numVLines - 1);
                var pos = gridTop;
                for (var i = 0; i < numHLines; i++)
                {
                    var newGl = HeightDeScale(pos);
                    storedImage.HorizontalGridlines.Add(newGl);
                    pos += hLineSpacing;
                }

                pos = gridLeft;
                for (var i = 0; i < numVLines; i++)
                {
                    var newGl = WidthDeScale(pos);
                    storedImage.VerticalGridLines.Add(newGl);
                    pos += vLineSpacing;
                }

                DrawAllLines();
            }
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IsModified = true;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;

                if (storedImage.isPlayable)
                {
                    AudioHost.Instance.audioPlayer.Stop();
                    if (!storedImage.segmentsForImage.IsNullOrEmpty())
                        foreach (var seg in storedImage.segmentsForImage)
                            AudioHost.Instance.audioPlayer.AddToList(seg);
                }
            }
        }

        /// <summary>
        ///     Opens the associated recording or segment in Audacity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;

                if (storedImage.isPlayable)
                    using (new WaitCursor("Opening recording in Audacity..."))
                    {
                        storedImage.Open();
                    }
            }
        }


        private enum Orientation
        {
            HORIZONTAL,
            VERTICAL
        }

        #region scaleValue

        /// <summary>
        ///     scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty scaleValueProperty =
            DependencyProperty.Register(nameof(scaleValue), typeof(string), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata("0.5"));

        /// <summary>
        ///     Gets or sets the scaleValue property.  This dependency property
        ///     indicates ....
        /// </summary>
        public string scaleValue
        {
            get => (string) GetValue(scaleValueProperty);
            set => SetValue(scaleValueProperty, value);
        }

        #endregion scaleValue

        #region gridScaleValue

        /// <summary>
        ///     scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty gridScaleValueProperty =
            DependencyProperty.Register(nameof(gridScaleValue), typeof(double), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata(1.1d));

        /// <summary>
        ///     Gets or sets the scaleValue property.  This dependency property
        ///     indicates ....
        /// </summary>
        public double gridScaleValue
        {
            get => (double) GetValue(gridScaleValueProperty);
            set => SetValue(gridScaleValueProperty, value);
        }

        #endregion gridScaleValue

        #region gridLeftMargin

        /// <summary>
        ///     scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty gridLeftMarginProperty =
            DependencyProperty.Register(nameof(gridLeftMargin), typeof(double), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata(0.28d));

        /// <summary>
        ///     Gets or sets the scaleValue property.  This dependency property
        ///     indicates ....
        /// </summary>
        public double gridLeftMargin
        {
            get => (double) GetValue(gridLeftMarginProperty);
            set => SetValue(gridLeftMarginProperty, value);
        }

        #endregion gridLeftMargin

        #region gridTopMargin

        /// <summary>
        ///     scaleValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty gridTopMarginProperty =
            DependencyProperty.Register(nameof(gridTopMargin), typeof(double), typeof(DisplayStoredImageControl),
                new FrameworkPropertyMetadata(0.154d));

        /// <summary>
        ///     Gets or sets the scaleValue property.  This dependency property
        ///     indicates ....
        /// </summary>
        public double gridTopMargin
        {
            get => (double) GetValue(gridTopMarginProperty);
            set => SetValue(gridTopMarginProperty, value);
        }

        #endregion gridTopMargin


        #region FullButtonRClickedEvent

        /// <summary>
        ///     Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> e_FullButtonRClicked
        {
            add
            {
                lock (_fullButtonRClickedEventLock)
                {
                    _fullButtonRClickedEvent += value;
                }
            }
            remove
            {
                lock (_fullButtonRClickedEventLock)
                {
                    _fullButtonRClickedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_FullButtonRClicked" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnFullButtonRClicked(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_fullButtonRClickedEventLock)
            {
                handler = _fullButtonRClickedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        // Add Event for adding image/s when there is no segment selected
        private readonly object _fullButtonRClickedEventLock = new object();

        private EventHandler<EventArgs> _fullButtonRClickedEvent;

        #endregion FullButtonRClickedEvent

        private void MiRotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(true);
        }

        private void MiRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(false);
        }

        private void MiDeleteImageFromList_Click(object sender, RoutedEventArgs e)
        {
            OnDelButtonPressed(new BoolEventArgs());
            DisplayImageCanvas.Focus();
        }

        private void MiDeleteImageFromDB_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(@"This will permanently remove this image from the database.
Are you sure?", "Delete Image from database", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                OnDelButtonPressed(new BoolEventArgs(true));
            }
            DisplayImageCanvas.Focus();
        }

        private void MiDeleteFiducialLine_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLine >= 0)  DeleteGridLine();
            DisplayImageCanvas.Focus();
        }

        private void MiAddHorizontalLine_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;

                var pos = rightMousePos;
                bool horizontal = true;
                
                DisplayImage_AddFiducial(horizontal, pos);
            }
        }

        private Point rightMousePos { get; set; } = new Point();
        private void DisplayImageCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            rightMousePos = e.GetPosition(DisplayImageCanvas);
            e.Handled = false;
        }

        private void MiAddVerticalLine_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;

                var pos = rightMousePos;
                bool horizontal = false;
                DisplayImage_AddFiducial(horizontal, pos);
            }
        }

        private void MiFidsOn_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                FiducialsButton.IsChecked = true;
            }
        }

        private void MiFidsOff_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                FiducialsButton.IsChecked = false;
            }
        }

        private void MiFidsOnGlobal_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Context All Fids On");
            if (!e.Handled)
            {
                e.Handled = true;
                FiducialsButton.IsChecked = true;
                OnFidsButtonRClicked(FiducialsButton, e);
                BringIntoView();
                Focus();
                DisplayImageCanvas.Focus();
            }
        }

        private void MiFidsOffGlobal_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Context All FIDS off");
            if (!e.Handled)
            {
                e.Handled = true;
                FiducialsButton.IsChecked = false;
                OnFidsButtonRClicked(FiducialsButton, e);
                BringIntoView();
                Focus();
                DisplayImageCanvas.Focus();
            }
        }

        private void MiFidsCopyGridFids_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                CopyGridToFids();
            }
        }

        private void MiFidsDeselectFids_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;

                if (DisplayImageCanvas.Children != null)
                {
                    foreach (var child in DisplayImageCanvas.Children)
                        if (child is Line line)
                            line.StrokeThickness = 1;
                    _selectedLine = -1;
                    HighlightGrid(false);
                }
            }
            DisplayImageCanvas.Focus();
        }

        private void MiFidsDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                storedImage.HorizontalGridlines.Clear();
                storedImage.VerticalGridLines.Clear();
                ClearGridlines();
                
            }
        }
    }

    //================================================================================================================================
    /// <summary>
    ///     Provides arguments for aDuplicate Lines event.
    /// </summary>
    [Serializable]
    public class DuplicateEventArgs : EventArgs
    {
        /// <summary>
        ///     default example of event args
        /// </summary>
        public new static readonly DuplicateEventArgs Empty = new DuplicateEventArgs(null, null, Grid.K675, 0, 0, 1.0);

        #region Public Properties

        public List<double> HLineProportions { get; set; }
        public List<double> VLineProportions { get; set; }

        public enum Grid
        {
            K675,
            K7029
        }

        public Grid GridType { get; set; }
        public double LeftMargin;
        public double TopMargin;
        public double Scale;

        #endregion Public Properties


        #region Constructors

        /// <summary>
        ///     Constructs a new instance of the <see cref="DuplicateEventArgs" /> class.
        /// </summary>
        public DuplicateEventArgs(List<double> hLines, List<double> vLines, Grid gridType, double leftMargin,
            double topMargin, double scale)
        {
            HLineProportions = hLines;
            VLineProportions = vLines;
            GridType = gridType;
            LeftMargin = leftMargin;
            TopMargin = topMargin;
            Scale = scale;
        }

        public new string ToString()
        {
            var result = "";

            if (HLineProportions != null)
            {
                result = "Horizontals at:-\n";
                foreach (var line in HLineProportions) result += line + ", ";
            }

            if (VLineProportions != null)
            {
                result = "\nVerticals at:-\n";
                foreach (var line in VLineProportions) result += line + ", ";
            }

            result += "\nGRID=" + GridType;
            result += "\nLeft Margin=" + LeftMargin;
            result += "\nTopMargin=" + TopMargin;
            result += "\nScale=" + Scale + "\n";

            return result;
        }

        #endregion Constructors
    }
}