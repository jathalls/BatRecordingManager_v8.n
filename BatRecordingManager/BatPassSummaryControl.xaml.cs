using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatPassSummaryControl.xaml
    /// </summary>
    public partial class BatPassSummaryControl : UserControl
    {
        private BatStats _passSummary;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatPassSummaryControl" /> class.
        /// </summary>
        public BatPassSummaryControl()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Gets or sets the PassSummary property. This dependency property indicates ....
        /// </summary>
        public BatStats PassSummary
        {
            get => _passSummary;
            set
            {
                _passSummary = value;

                SummaryStackpanel.Children.Clear();

                var statstring = Tools.GetFormattedBatStats(value, false);
                var statLabel = new Label();
                statLabel.Content = statstring;
                SummaryStackpanel.Children.Add(statLabel);
                InvalidateArrange();
                UpdateLayout();
            }
        }
    }
}