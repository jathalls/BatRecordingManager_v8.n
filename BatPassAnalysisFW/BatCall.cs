using System;
using System.Linq;
using System.Xml.Linq;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// class to hold details of a specific type of call for a bat
    /// </summary>
    public class BatCall
    {
        private XElement m_Call;
        private XElement m_Label;
        public string Bat { get; set; }
        
        public (float Upper, float Lower, float Median) fStart { get; set; }

        public (float Upper, float Lower, float Median) fEnd { get; set; }

        public (float Upper, float Lower, float Median) fpeak { get; set; }

        public (float Upper, float Lower, float Median) Interval { get; set; }

        public (float Upper, float Lower, float Median) Duration { get; set; }
        

        /// <summary>
        /// default constructor for the BatCall class
        /// </summary>
        public BatCall()
        {

        }

        public BatCall(XElement call, XElement label)
        {
            m_Call = call;
            m_Label = label;

            ParseFloats("fStart", out float lower, out float upper);
            fStart = (Upper:upper,Lower:lower,Median:(upper+lower)/2.0f);

            ParseFloats("fEnd", out lower, out upper);
            fEnd = (Upper: upper, Lower: lower, Median: (upper + lower) / 2.0f);

            ParseFloats("fPeak", out lower, out upper);
            fpeak = (Upper: upper, Lower: lower, Median: (upper + lower) / 2.0f);

            ParseFloats("Interval", out lower, out upper);
            Interval = (Upper: upper, Lower: lower, Median: (upper + lower) / 2.0f);

            ParseFloats("Duration", out lower, out upper);
            Duration = (Upper: upper, Lower: lower, Median: (upper + lower) / 2.0f);

            Bat = m_Label?.Value;

        }

        private bool ParseFloats(string name,out float first,out float second)
        {
            first = 0.0f;
            second = 0.0f;
            bool result = false;
            try
            {
                float lower = 0.001f;
                float upper = 0.002f;
                string[] freqs = m_Call.Element(name).Value.Split('-');
                if (freqs.Count() > 0)
                {
                    result = float.TryParse(freqs[0], out lower);
                    if (freqs.Count() > 1)
                    {
                        result &= float.TryParse(freqs[1], out upper);
                    }
                    else
                    {
                        upper = lower + .001f;
                        lower -= .001f;
                        
                        
                    }
                }
                else
                {
                    result = false;
                }
                first = lower;
                second = upper;
            }
            catch (Exception)
            {
                result = false;
            }
            return (result);
        }
    }

    
}
