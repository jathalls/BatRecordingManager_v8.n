using System.Windows;
using Microsoft.Maps.MapControl.WPF;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MapWindow.xaml
    /// </summary>
    public partial class MapWindow : Window
    {
        private readonly bool _isDialog;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapWindow" /> class. The parameter is
        ///     set true if the window is to be displayed using ShowDialog rather than Show so that
        ///     the DialogResult can be set before closing.
        /// </summary>
        /// <param name="isDialog">
        ///     if set to <c>true</c> [is dialog].
        /// </param>
        public MapWindow(bool isDialog)
        {
            this._isDialog = isDialog;
            InitializeComponent();
            MapControl.OkButton.Click += OKButton_Click;
        }

        /// <summary>
        ///     Gets or sets the coordinates of the centre of the map window
        /// </summary>
        /// <value>
        ///     The coordinates.
        /// </value>
        public Location Coordinates
        {
            get => MapControl.coordinates;
            set => MapControl.coordinates = value;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public Location lastSelectedLocation
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return MapControl.lastInsertedPinLocation; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDialog) DialogResult = true;
            Close();
        }
    }
}