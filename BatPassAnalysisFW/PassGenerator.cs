using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BatPassAnalysisFW
{
    class PassGenerator
    {
        public List<string> textFiles = new List<string>();
        /// <summary>
        /// default constructor called from the static Create function
        /// </summary>
        private PassGenerator()
        {

        }

        /// <summary>
        /// Creates a new instance of the class and uses it to generate a .txt file for every
        /// .wav file in the specified folder.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static PassGenerator Create(string path)
        {
            PassGenerator passGenerator = new PassGenerator();
            if (Directory.Exists(path))
            {
                var allWavFiles = Directory.EnumerateFiles(path, "*.wav");
                if (allWavFiles != null && allWavFiles.Any())
                {
                    foreach (var file in allWavFiles)
                    {
                        passGenerator.CreateLabelFile(file);
                    }
                }
            }
            return (passGenerator);
        }

        private void CreateLabelFile(string FQWavFileName)
        {
            if (!File.Exists(FQWavFileName)) return;
            var FQTextFileName = Path.ChangeExtension(FQWavFileName, ".txt");
            if (File.Exists(FQTextFileName)) return;
            File.WriteAllText(FQTextFileName, $"0\t0\t{FQWavFileName}\n");
            textFiles.Add(FQTextFileName);
            (float start, float end, string comment)[] passes = GetPasses(FQWavFileName);
            if (passes != null && passes.Any())
            {
                foreach (var pass in passes)
                {
                    File.AppendAllText(FQTextFileName, $"{pass.start}\t{pass.end}\t{pass.comment}\n");
                }
            }


        }

        private (float start, float end, string comment)[] GetPasses(string FQWavFileName)
        {
            List<(float start, float end, string comment)> result = new List<(float start, float end, string comment)>();
            float[] envelope = GetFilteredEnvelope(FQWavFileName, 15000, 65000, out float mean, out int envelopeRate);
            result = ScanForPasses(envelope, mean * 3.0f, 0.001f, 0.5f, 0.5f, envelopeRate);

            return (result.ToArray());
        }

        /// <summary>
        /// Scans an envelope trace
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="threshold"></param>
        /// <param name="LeadInSecs"></param>
        /// <param name="leadOutSecs"></param>
        /// <param name="PrependSecs"></param>
        /// <param name="effectiveSampleRate"></param>
        /// 
        /// <returns></returns>
        private List<(float start, float end, string comment)> ScanForPasses(float[] envelope, float threshold,
            float LeadInSecs, float leadOutSecs, float PrependSecs, int effectiveSampleRate)
        {
            var result = new List<(float start, float end, string comment)>();
            int leadInSamples = (int)(LeadInSecs * effectiveSampleRate);
            int leadOutSamples = (int)(leadOutSecs * effectiveSampleRate);
            PassAnalysis.peakState currentPeakState = PassAnalysis.peakState.NOTINPEAK;
            int i = 0;
            int startSample = 0;
            int endSample = 0;
            Debug.WriteLine($"SCAN:- Threshold={threshold}, leadin={leadInSamples}, leadout={leadOutSamples}\n");
            int peaksInPass = 0;
            while (i < envelope.Length)
            {
                switch (currentPeakState)
                {
                    case PassAnalysis.peakState.NOTINPEAK:
                        peaksInPass = 0;
                        while (i < envelope.Length && envelope[i] < threshold)
                        {

                            i++;
                        }
                        if (i >= envelope.Length) return (result);
                        startSample = i;
                        currentPeakState = PassAnalysis.peakState.INPEAKLEADIN;
                        Debug.WriteLine($"Leadin:- {i}");
                        break;
                    case PassAnalysis.peakState.INPEAKLEADIN:
                        int leadinCount = 1;
                        while (i < envelope.Length && envelope[i] > threshold)
                        {

                            if (leadinCount++ >= leadInSamples)
                            {
                                currentPeakState = PassAnalysis.peakState.INPEAK;
                                Debug.WriteLine($"In Peak: {i}");
                                break;
                            }
                            i++;

                        }
                        if (currentPeakState != PassAnalysis.peakState.INPEAKLEADIN) break;
                        if (i >= envelope.Length) return (result);

                        currentPeakState = PassAnalysis.peakState.NOTINPEAK;
                        Debug.WriteLine($"Out {i}\n");
                        break;


                    case PassAnalysis.peakState.INPEAKLEADOUT:
                        int leadOutCount = 1;
                        while (i < envelope.Length && envelope[i] < threshold)
                        {
                            if (leadOutCount++ > leadOutSamples)
                            {
                                endSample = i;
                                Debug.WriteLine("LeadOut exceeded");
                                if (peaksInPass >= 3)
                                {
                                    Debug.WriteLine($"Make Pass for {peaksInPass} peaks");
                                    result.Add(makePass(startSample / effectiveSampleRate, endSample / effectiveSampleRate, PrependSecs));
                                    peaksInPass = 0;
                                }
                                currentPeakState = PassAnalysis.peakState.NOTINPEAK;
                                i++;

                                break;
                            }
                            i++;
                        }
                        if (currentPeakState != PassAnalysis.peakState.INPEAKLEADOUT) break;
                        if (i >= envelope.Length)
                        {
                            endSample = i;
                            result.Add(makePass(startSample / effectiveSampleRate, endSample / effectiveSampleRate, PrependSecs));
                            Debug.WriteLine($"Out and Finish with Pass {i}\n\n");
                            return (result);
                        }
                        currentPeakState = PassAnalysis.peakState.INPEAK;
                        i++;
                        Debug.WriteLine($"In Peak {i}");
                        break;
                    case PassAnalysis.peakState.INPEAK:
                        peaksInPass++;
                        while (i < envelope.Length && envelope[i] > threshold)
                        {

                            i++;

                        }
                        if (i >= envelope.Length)
                        {
                            endSample = i;
                            result.Add(makePass(startSample / effectiveSampleRate, endSample / effectiveSampleRate, PrependSecs));
                            Debug.WriteLine($"Finish in Peak with a pass {i}\n\n");
                            return (result);
                        }
                        currentPeakState = PassAnalysis.peakState.INPEAKLEADOUT;
                        Debug.WriteLine($"LeadOut {i}");
                        break;
                    default: break;
                }
            }

            return result;

        }

        /// <summary>
        /// Creates a pass based on startSample and endSample
        /// </summary>
        /// <param name="startSecs"></param>
        /// <param name="endSecs"></param>
        /// <param name="prependSecs"></param>
        /// <returns></returns>
        private (float start, float end, string comment) makePass(float startSecs, float endSecs, float prependSecs)
        {
            (float start, float end, string comment) result;
            if (startSecs < prependSecs) result.start = 0.0f;
            else result.start = startSecs - prependSecs;
            result.end = endSecs;
            result.comment = "";


            return (result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fQWavFileName"></param>
        /// <returns></returns>
        private float[] GetFilteredEnvelope(string fQWavFileName, int lowFrequency, int highFrequency, out float mean, out int effectiveSampleRate)
        {
            AudioFileReader afr = new AudioFileReader(fQWavFileName);
            int floatsToRead = (int)(afr.Length / sizeof(float));
            float[] data = new float[floatsToRead];
            double total = 0.0d;
            mean = 0.0f;

            int sampleRate = afr.WaveFormat.SampleRate;
            int floatsRead = afr.ToSampleProvider().Read(data, 0, floatsToRead);
            Debug.WriteLine($"Read {floatsRead} samples = {floatsRead / sampleRate}secs");
            List<float> envelope = new List<float>();

            effectiveSampleRate = sampleRate / 10;
            var HiPassfilter = BiQuadFilter.HighPassFilter(sampleRate, lowFrequency, 5);
            var LowPassfilter = BiQuadFilter.LowPassFilter(sampleRate, highFrequency, 5);
            var SmoothingFilter = BiQuadFilter.LowPassFilter(sampleRate, 500.0f, 1);
            double shortTotal = 0.0d;
            double max = double.MinValue;
            for (int i = 0, e = 0; i < floatsRead; i++)
            {
                var s = HiPassfilter.Transform(data[i]);
                s = LowPassfilter.Transform(s);
                s = s * s;
                s = Math.Abs(SmoothingFilter.Transform(s));
                shortTotal += s;
                e++;
                if (e % 10 == 0)
                {
                    double val = Math.Sqrt(shortTotal / 10.0d);
                    if (!double.IsNaN(val))
                    {
                        envelope.Add((float)val);
                        total += val;
                        max = max > val ? max : val;
                        shortTotal = 0.0d;
                        e = 0;
                    }
                    else
                    {
                        Debug.WriteLine($"NAN at i={i} e={e} s={s} shortTotal={shortTotal} val={val}");
                    }
                }

            }

            mean = (float)(total / envelope.Count());
            Debug.WriteLine($"Total={total} count={envelope.Count()}, Mean={mean}, Max={max}");

            return (envelope.ToArray());

        }
    }
}
