using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalToolkit
{
    public class CustomSettings
    {
        public ReferenceCall call { get; set; }
        public (double min, double mean, double max) fBandwidth { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fEnd { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fHeel { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fKnee { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fPeak { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fStart { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) tDuration { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) tInterval { get; set; } = (0, 0, 0);
    }
}