using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatCallAnalysisControlSet
{
    public class CallCursor
    {
        public CallCursor(double min, double mean, double max)
        {
            cursor_min = min;
            cursor_mean = mean;
            cursor_max = max;
        }

        public double cursor_max { get; set; }
        public double cursor_mean { get; set; }

        public double cursor_min
        {
            get
            {
                return (_cursor_min < 0 ? 0.0d : _cursor_min);
            }
            set
            {
                _cursor_min = value;
            }
        }

        public double cursor_range
        {
            get
            {
                var result = cursor_max - cursor_min;
                return (result < 0 ? 0.0d : result);
            }
        }

        private double _cursor_min;
    }
}