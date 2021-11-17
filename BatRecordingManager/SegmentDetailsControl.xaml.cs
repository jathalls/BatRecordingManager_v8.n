using System;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for SegmentDetailsControl.xaml
    /// </summary>
    public partial class SegmentDetailsControl : UserControl
    {
        public SegmentDetailsControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public LabelledSegment segment { get; set; }
        public string segmentEnd { get; set; }
        public string segmentStart { get; set; }

        public void setSegment(LabelledSegment segment)
        {
            this.segment = segment;
            segmentStart = $"{segment.Recording.RecordingDate?.ToShortDateString()} " +
                $"{segment.Recording.RecordingStartTime ?? new TimeSpan() + segment.StartOffset}";
            segmentEnd = $"{segment.Recording.RecordingDate?.ToShortDateString()} " +
                $"{segment.Recording.RecordingStartTime ?? new TimeSpan() + segment.EndOffset}";
        }
    }
}