using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

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