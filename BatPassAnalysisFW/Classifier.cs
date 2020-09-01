using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Classifier class to analyse instances of bpaPass and make a best guess as to the 
    /// predominnant species present
    /// </summary>
    public class Classifier
    {
        private readonly Bats bats;
        /// <summary>
        /// default constructor for the Classifier class
        /// </summary>
        public Classifier()
        {
            bats = new Bats();
        }

        public string Classify(bpaPass passToClassify)
        {
            string result = "";
            string shapeStr = "";
            var callList = bats.getAllCalls();
            List<(string Bat, float Score)> scores = new List<(string Bat, float Score)>();
            Pulse pulse = null;
            try
            {
                pulse = (from p in passToClassify.getPulseList()
                         where p.getPeak().getPeakWidthSamples() > 326
                         orderby p.getPeak().GetMaxVal() descending
                         select p).FirstOrDefault();
                if (pulse != null && pulse.Pulse_Number > 0)
                {
                    List<float> sample = new List<float>();
                    List<float> pre_sample = new List<float>();
                    var peakPos = (Offset: pulse.getData(ref sample, ref pre_sample), Length: pulse.getPeak().getPeakWidthSamples());

                    (float startSlope, float midSlope, float endSlope, float allSLope) shape = pulse.spectralDetails.getSpectrum().GetFFTDetail(sample.ToArray(), pre_sample.ToArray(), peakPos);
                    float slopeLoLimit = shape.allSLope * .8f;
                    float slopeHiLimit = shape.allSLope * 1.2f;
                    string shapeCode = "";
                    if (shape.startSlope > slopeHiLimit) shapeCode += "H";
                    else if (shape.startSlope < slopeLoLimit) shapeCode += "L";
                    else shapeCode += "M";

                    if (shape.midSlope > slopeHiLimit) shapeCode += "H";
                    else if (shape.midSlope < slopeLoLimit) shapeCode += "L";
                    else shapeCode += "M";

                    if (shape.endSlope > slopeHiLimit) shapeCode += "H";
                    else if (shape.endSlope < slopeLoLimit) shapeCode += "L";
                    else shapeCode += "M";

                    switch (shapeCode)
                    {
                        case "MMM": shapeStr = "FM1"; break;
                        case "HHH": shapeStr = "FM1"; break;
                        case "LLL": shapeStr = "FM1"; break;
                        case "HML": shapeStr = "FM/qCF"; break;
                        case "HHL": shapeStr = "FM/qCF"; break;
                        case "MML": shapeStr = "FM/qCF"; break;
                        case "HLL": shapeStr = "FM/qCF"; break;
                        case "MLL": shapeStr = "FM/qCF"; break;
                        case "LMH": shapeStr = "qCF/FM"; break;
                        case "HMH": shapeStr = "FM2"; break;
                        case "MLM": shapeStr = "FM2"; break;
                        case "LLH": shapeStr = "CF-FM"; break;
                        case "LLM": shapeStr = "CF-FM"; break;

                        default: shapeStr = shapeCode; break;
                    }

                    Debug.WriteLine($"Slopes={shape.startSlope:0.00}, {shape.midSlope:0.00}, {shape.endSlope:0.00}. mean={shape.allSLope:0.00} => {shapeCode}");

                }
            }
            catch (Exception) { }

            foreach (var call in callList)
            {
                (string Bat, float Score) score = getScore(passToClassify, call);
                scores.Add(score);
            }
            var goodList = from sc in scores
                           where sc.Score > 0.0
                           orderby sc.Score descending
                           select sc;
            if (goodList != null && goodList.Count() > 0)
            {
                foreach (var item in goodList)
                {
                    result += $"{item.Bat}";
                    if (goodList.Count() > 1)
                    {
                        result += $" p={item.Score:0.00}; ";
                    }
                    else
                    {
                        result += "; ";
                    }
                }
                result += "shape=" + shapeStr;
            }

            return (result);
        }

        /// <summary>
        /// Calculates a score between 0 and 1 for the comparison of this pass to a given call
        /// characteristic
        /// </summary>
        /// <param name="passToClassify"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        private (string Bat, float Score) getScore(bpaPass passToClassify, BatCall call)
        {


            float score = 1.0f;
            System.Diagnostics.Debug.WriteLine($"-------{call.Bat}");
            if (passToClassify != null && call != null)
            {
                var Score1 = scoreForParameter(passToClassify.startDetails, call.fStart);
                System.Diagnostics.Debug.WriteLine($"fStart {Score1.Score}/{Score1.Possible}");
                score *= Score1.Score / (Score1.Possible != 0 ? Score1.Possible : 1);


                var Score2 = scoreForParameter(passToClassify.endDetails, call.fEnd);
                System.Diagnostics.Debug.Write($", fEnd {Score2.Score}/{Score2.Possible}");
                score *= Score2.Score / (Score2.Possible != 0 ? Score2.Possible : 1);

                var Score3 = scoreForParameter(passToClassify.peakDetails, call.fpeak);
                System.Diagnostics.Debug.Write($", fPeak {Score3.Score}/{Score3.Possible}");
                score *= Score3.Score / (Score3.Possible != 0 ? Score3.Possible : 1);

                var Score4 = scoreForParameter(passToClassify.intervalDetails, call.Interval);
                System.Diagnostics.Debug.Write($", Interval {Score4.Score}/{Score4.Possible}");
                score *= Score4.Score / (Score4.Possible != 0 ? Score4.Possible : 1);

                var Score5 = scoreForParameter(passToClassify.durationDetails, call.Duration);
                System.Diagnostics.Debug.Write($", Durtn {Score5.Score}/{Score5.Possible}");
                score *= Score5.Score / (Score5.Possible != 0 ? Score5.Possible : 1);

                float refPeakPosition = (call.fpeak.Median - call.fEnd.Median) / (call.fStart.Median - call.fEnd.Median);
                float passPeakPosition = (passToClassify.peakDetails.Mean - passToClassify.endDetails.Mean) /
                    (passToClassify.startDetails.Mean - passToClassify.endDetails.Mean);
                score *= (1.0f - (float)Math.Abs(refPeakPosition - passPeakPosition));

                Debug.WriteLine($"\nScore={score}");
            }

            return (Bat: call.Bat, Score: score);
        }

        /// <summary>
        /// Calculates a score for similarity between the pass details and the reference call structure.
        /// 
        /// </summary>
        /// <param name="PassParams"></param>
        /// <param name="CallParams"></param>
        /// <returns></returns>
        private (float Score, int Possible) scoreForParameter((float Mean, float SD, float NoPulses) PassParams,
            (float Upper, float Lower, float Median) CallParams)
        {
            float score = 0.0f;
            int possible = 0;

            if (PassParams.NoPulses > 1)
            {
                //if (paramDetails.NoPulses > 5) // bonus poiunt for having a decent number of pulses to work with
                //{
                //    score += 1.0f;
                //    possible++;
                // }

                score += Overlap(PassParams, CallParams); // one point for overlapping ranges
                possible++;
                if (score > 0)
                {
                    float diff = Math.Abs((CallParams.Median * CallParams.Median) - (PassParams.Mean * PassParams.Mean));
                    float variance = (float)Math.Sqrt(diff);
                    score += 1.0f - (float)Math.Abs(variance / CallParams.Median); // one point for proximity of the means

                    possible++;
                }
                //if (score > 0.0f)
                //{
                //    score *= paramDetails.NoPulses;
                //    possible *= (int)paramDetails.NoPulses;
                //}





            }
            else
            {
                possible++;
            }



            return (Score: score, Possible: possible);
        }

        /// <summary>
        /// returns a value between 0 and 1 for the extent to which the pass range overlaps the reference range
        /// </summary>
        /// <param name="startDetails"></param>
        /// <param name="fStart"></param>
        /// <returns></returns>
        private float Overlap((float Mean, float SD, float NoPulses) startDetails, (float Upper, float Lower, float Median) fStart)
        {
            float passUpper = startDetails.Mean + startDetails.SD;
            float passLower = startDetails.Mean - startDetails.SD;
            float refUpper = fStart.Upper;
            float refLower = fStart.Lower;

            if (passUpper < refLower || passLower > refUpper) return (0.0f); // no overlap at all
            if (passUpper < refUpper && passLower > refLower) return (1.0f); // pass entirely within the ref range
            if (passLower < refLower && passUpper > refUpper) return (0.75f);// pass encompasses the ref range, so a less perfect match

            float overlap = Math.Abs(Math.Min(refUpper, passUpper) - Math.Max(refLower, passLower));
            float score = overlap / (refUpper - refLower);

            return (score);
        }
    }
}
