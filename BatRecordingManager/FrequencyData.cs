using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatRecordingManager
{
    public class FrequencyDataSet
    {
        public FrequencyDataSet()
        {
            startFrequency = 0.0d;
            endFrequency = 0.0d;
            peakFrequency = 0.0d;
            kneeFrequency = 0.0d;
            heelFrequency = 0.0d;
        }

        public double endFrequency { get; set; }
        public double heelFrequency { get; set; }
        public double kneeFrequency { get; set; }
        public double peakFrequency { get; set; }
        public double startFrequency { get; set; }

        /// <summary>
        /// write essential data and all spectra to a file
        /// </summary>
        /// <param name="fname"></param>
        internal void WriteData(string fname)
        {
            File.AppendAllText(fname, $"sf={startFrequency}, ef={endFrequency}, pf={peakFrequency}\n");
        }
    }
}