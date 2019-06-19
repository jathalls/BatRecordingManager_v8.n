using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ImageDialog.xaml
    /// </summary>
    public partial class ImageDialogControl : UserControl
    {
        private readonly object _okButtonClickedEventLock = new object();
        private string _defaultCaption = "";
        private string _defaultDescription = "";
        private EventHandler<EventArgs> _okButtonClickedEvent;

        /// <summary>
        /// An instance of StoredImage to hold the image and its related metadata
        /// </summary>
        //public StoredImage storedImage { get; set; } = new StoredImage(null, "", "", -1);

        /// <summary>
        ///     Basic constructor for the ImageDialogControl
        /// </summary>
        public ImageDialogControl()
        {
            InitializeComponent();
            DataContext = storedImage;
        }

        /// <summary>
        ///     Event raised after the OK Button is clicked.
        /// </summary>
        public event EventHandler<EventArgs> e_OkButtonClicked
        {
            add
            {
                lock (_okButtonClickedEventLock)
                {
                    _okButtonClickedEvent += value;
                }
            }
            remove
            {
                lock (_okButtonClickedEventLock)
                {
                    _okButtonClickedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Grabs the screen within the given rectangle
        /// </summary>
        /// <param name="rect"></param>
        public Bitmap GrabRect(Rectangle rect)
        {
            var rectWidth = rect.Width - rect.Left;
            var rectHeight = rect.Height - rect.Top;
            var bm = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bm);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bm.Size, CopyPixelOperation.SourceCopy);
            //DrawMousePointer(g, Cursor.Position.X - rect.Left, Cursor.Position.Y - rect.Top);
            //this.screengrab.Image = bm;
            return bm;
        }

        /// <summary>
        ///     Inserts current and default caption and description for the displayedImage
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="description"></param>
        public void SetImageDialogControl(string caption, string description)
        {
            //this.caption = caption;
            _defaultCaption = caption;
            //this.description = description;
            _defaultDescription = description;
            if (storedImage == null) storedImage = new StoredImage(null, "", "", -1);
            storedImage.caption = caption;
            storedImage.description = description;
        }

        /// <summary>
        ///     Clears the currently diaplyed image, the caption and the description
        ///     The caption is retained for re-use with later images so that a long filename
        ///     does not have to be retyped over and over but can be left or modified for
        ///     successive images
        /// </summary>
        internal void Clear(bool clearCaption)
        {
            //displayedImage = null;
            //caption = "";
            //description = "";
            storedImage.Clear(clearCaption);
        }

        internal void Clear()
        {
            Clear(true);
        }

        /// <summary>
        ///     If there is a displayed image returns it as a StoredImage using the current
        ///     caption and description and an ID of -1.
        /// </summary>
        /// <returns></returns>
        internal StoredImage GetStoredImage()
        {
            return storedImage;
        }

        /// <summary>
        ///     Raises the <see cref="e_OkButtonClicked" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnOkButtonClicked(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_okButtonClickedEventLock)
            {
                handler = _okButtonClickedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            storedImage.Clear();
            storedImage.description = "";
            storedImage.caption = "";
            OnOkButtonClicked(new EventArgs());
        }

        private void CCWImageButton_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(false);
        }

        private void ClearImageButton_Click(object sender, RoutedEventArgs e)
        {
            //displayedImage = null;
            //caption = defaultCaption;
            //description = defaultDescription;

            storedImage.Clear();
            DataContext = storedImage;
            this.Refresh();
        }

        private void CWImageButton_Click(object sender, RoutedEventArgs e)
        {
            RotateImage90(true);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (storedImage != null && (!string.IsNullOrWhiteSpace(storedImage.caption) ||
                                        !string.IsNullOrWhiteSpace(storedImage.description)))
                OnOkButtonClicked(EventArgs.Empty);
        }

        /// <summary>
        ///     Button handler for OPEN
        ///     Displays a file dialog to allow the user to select a suitable
        ///     image file.  Converts the file into a BitmapSource and displays it in
        ///     the image window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = FileBrowser.SelectFile("Select Image File", null,
                "Image file (*.bmp,*.jpg,*.png)|*.bmp;*.jpg;*.png|All files (*.*)|*.*", false);
            if (!selectedFiles.IsNullOrEmpty())
            {
                var uri = new Uri(selectedFiles[0], UriKind.Absolute);
                var bmps = new BitmapImage(uri);
                storedImage.Clear();
                storedImage.image = bmps;
                storedImage.caption = uri.ToString();
                storedImage.description = "";
            }
        }

        private void PasteImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                var grf = new GrabRegionForm();
                var parent = Parent;
                var avoidInfiniteLoop = 0;
                // Search up the visual tree to find the first parent window.
                while (parent is Window == false)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                    avoidInfiniteLoop++;
                    if (avoidInfiniteLoop == 1000)
                    {
                        // Something is wrong - we could not find the parent window.
                        Debug.WriteLine("Failed to find a parent of type Window");
                        return;
                    }
                }

                var window = parent as Window;

                //window.Visibility = Visibility.Hidden;
                window.WindowState = WindowState.Minimized;

                grf.ShowDialog();
                var rect = grf.rect;
                grf.Close();

                var bm = GrabRect(rect);

                storedImage = new StoredImage(bm.ToBitmapSource(),
                    storedImage != null ? storedImage.caption : "", storedImage != null ? storedImage.description : "",
                    -1);

                //window.Visibility = Visibility.Visible;
                window.WindowState = WindowState.Normal;
                Visibility = Visibility.Visible;
                InvalidateVisual();
                this.Refresh();
                window.InvalidateVisual();
                window.Refresh();
                Thread.Sleep(500);
            }
            else
            {
                if (Clipboard.ContainsText() && Clipboard.ContainsImage())
                {
                    int id;
                    // then we may have been passed an imageID to be linked to the current
                    //object that wants the image
                    var inboundText = Clipboard.GetText();
                    if (inboundText.StartsWith("***"))
                    {
                        inboundText = inboundText.Substring(3);
                        id = -1;
                        if (int.TryParse(inboundText, out id))
                            if (DBAccess.ImageExists(id))
                                storedImage = DBAccess.GetImage(id);
                    }
                }
                else
                {
                    if (Clipboard.ContainsImage())
                    {
                        //storedImage.Clear();
                        //storedImage.image = StoredImage.GetClipboardImageAsBitmapImage();
                        storedImage = new StoredImage(StoredImage.GetClipboardImageAsBitmapImage(),
                            storedImage != null ? storedImage.caption : "",
                            storedImage != null ? storedImage.description : "", -1);

                        if (Clipboard.ContainsText())

                            try
                            {
                                //var image = StoredImage.ConvertBitmapSourceToBitmapImage(Clipboard.GetImage());
                                var text = Clipboard.GetText();

                                //StoredImage si = new StoredImage(displayedImage, "", "", -1);
                                storedImage.SetCaptionAndDescription(text);
                                //displayedImage = si.image;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Error pasting from clipboard:- " + ex.Message);
                            }
                    }
                }
            }

            DataContext = storedImage;
            this.Refresh();
        }

        private void RotateImage90(bool clockwise)
        {
            var angle = clockwise ? 90 : -90;
            if (storedImage.image != null)
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
        }

        #region storedImage

        /// <summary>
        ///     storedImage Dependency Property
        /// </summary>
        public static readonly DependencyProperty storedImageProperty =
            DependencyProperty.Register("storedImage", typeof(StoredImage), typeof(ImageDialogControl),
                new FrameworkPropertyMetadata(new StoredImage(null, "", "", -1)));

        /// <summary>
        ///     storedImage with metadata - used as the DataContext
        /// </summary>
        public StoredImage storedImage
        {
            get => (StoredImage) GetValue(storedImageProperty);
            set => SetValue(storedImageProperty, value);
        }

        #endregion storedImage
    }
}