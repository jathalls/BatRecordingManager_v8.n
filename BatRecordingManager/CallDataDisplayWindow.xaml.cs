using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for CallDataDisplayWindow.xaml
    /// </summary>
    public partial class CallDataDisplayWindow : Window
    {
        public CallDataDisplayWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// call contains:-
        /// StartFrequency
        /// StartFrequencyVariation
        /// EndFrequency
        /// EndFrequencyVariation
        /// PeakFrequency
        /// PeakFrequencyVariation
        /// PulseDuration
        /// PulseDurationvariation
        /// PulseInterval
        /// PulseIntervalVariation
        /// CallType
        /// CallFunction
        /// CallNotes
        ///
        /// </summary>
        public ObservableCollection<Call> CallData { get; set; }

        public LabelledSegment displayedSegment { get; set; }

        public void setSegmentToDisplay(LabelledSegment segment)
        {
            displayedSegment = segment;
            SegmentDetails?.setSegment(segment);
            CallData = new ObservableCollection<Call>();
            if (!segment.SegmentCalls.IsNullOrEmpty())
            {
                var calls = segment.SegmentCalls.Select(lnk => lnk.Call);
                if (!calls.IsNullOrEmpty())
                {
                    foreach (var call in calls) CallData.Add(call);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}