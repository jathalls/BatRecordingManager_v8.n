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
        private readonly XElement m_Call;
        private readonly XElement m_Label;

        /// <summary>
        /// The bat that relates to the call
        /// </summary>
        public string Bat { get; set; }

        /// <summary>
        /// Range for start frequencies
        /// </summary>
        public (float Upper, float Lower, float Median) fStart { get; set; }

        /// <summary>
        /// Range of end frequencies
        /// </summary>
        public (float Upper, float Lower, float Median) fEnd { get; set; }

        /// <summary>
        /// Range of peak (FmaxE) frequencies
        /// </summary>
        public (float Upper, float Lower, float Median) fpeak { get; set; }

        /// <summary>
        /// Range of intervals
        /// </summary>
        public (float Upper, float Lower, float Median) Interval { get; set; }

        /// <summary>
        /// Range of pulse durations
        /// </summary>
        public (float Upper, float Lower, float Median) Duration { get; set; }


        /// <summary>
        /// default constructor for the BatCall class
        /// </summary>
        public BatCall()
        {

        }


        /// <summary>
        ///  Given an XML call element and an XML label element, calculates pulse parameter ranges, and extracts a
        ///  bat name from the label
        /// </summary>
        /// <param name="call"></param>
        /// <param name="label"></param>
        public BatCall(XElement call, XElement label)
        {
            m_Call = call;
            m_Label = label;

            ParseFloats("fStart", out float lower, out float upper);
            fStart = (Upper: upper, Lower: lower, Median: (upper + lower) / 2.0f);

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

        /// <summary>
        /// Given a string in the form value-value where value is a floating point value, 
        /// extracts those values as floats
        /// </summary>
        /// <param name="name">The string to be parsed</param>
        /// <param name="first">The first floating point value</param>
        /// <param name="second">the second floating point value</param>
        /// <returns></returns>
        private bool ParseFloats(string name, out float first, out float second)
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
