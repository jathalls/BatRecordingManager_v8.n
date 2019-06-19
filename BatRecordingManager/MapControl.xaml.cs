using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Maps.MapControl.WPF;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MapControl.xaml
    /// </summary>
    public partial class MapControl : UserControl
    {
        private Location _coordinates;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapControl" /> class.
        /// </summary>
        public MapControl()
        {
            InitializeComponent();
            ThisMap.Focus();
            lastInsertedPinLocation = null;
        }

        /// <summary>
        ///     Gets or sets the coordinates.
        /// </summary>
        /// <value>
        ///     The coordinates.
        /// </value>
        public Location coordinates
        {
            get => _coordinates;
            set
            {
                _coordinates = value;
                ThisMap.Center = value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Location lastInsertedPinLocation { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Adds the push pin.
        /// </summary>
        /// <param name="pinCoordinates">
        ///     The pin coordinates.
        /// </param>
        /// <param name="text">
        ///     The text.
        /// </param>
        public void AddPushPin(Location pinCoordinates, string text)
        {
            var pin = new Pushpin();
            pin.Location = pinCoordinates;
            pin.Content = text;
            ThisMap.Children.Add(pin);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void AddPushPin(Location location)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var pin = new Pushpin();
            pin.Location = location;
            ThisMap.Children.Add(pin);
        }

        private void mapControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ThisMap.Children.Clear();
            var mousePosition = e.GetPosition(this);
            var pinLocation = ThisMap.ViewportPointToLocation(mousePosition);

            var pin = new Pushpin();
            pin.Location = pinLocation;
            lastInsertedPinLocation = pinLocation;
            ThisMap.Children.Add(pin);
        }
    }
}