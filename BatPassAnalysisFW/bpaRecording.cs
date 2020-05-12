using DspSharp.Utilities.Collections;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BatPassAnalysisFW

{
    /// <summary>
    /// Class representing the data in a single recording or .wav file.  If the file has an associated .txt file
    /// it is split into identified segments, otherwise it is considered to be a single segment.
    /// </summary>
    public class bpaRecording
    {
        // Visible public members
        public string File_Name
        {
            get
            {
                string fn;
                if (filename.Contains(@"\"))
                {
                    fn = filename.Substring(filename.LastIndexOf(@"\"));
                }
                else
                {
                    fn = filename;
                }
                return (fn);
            }
        }

        public string recorded { get; set; }

        

        public int recNumber { get; set; }
        

        
        
        /// the name of the file containing the recording being processed
        /// </summary>
        public string filename { get; set; } = "";

        /// <summary>
        /// A list of all the segments in the recording and their associated data
        /// </summary>
        private ObservableList<bpaSegment> segmentList { get; set; } = new ObservableList<bpaSegment>();

        public int segmentCount 
        { 
            get
            {
                return (segmentList.Count());
            } 
        }

        public int? passCount
        {
            get
            {
                int? passes = (from seg in segmentList
                              select seg.Number_Of_Passes)?.Sum();
                return (passes);
            }
        }

        public int? pulseCount
        {
            get
            {
                int? pulses = (from seg in segmentList
                              select seg.Number_Of_Pulses)?.Sum();
                return (pulses);
            }
        }

        private float[] data;

        public bpaRecording(int recNumber, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                this.filename = fileName;
            }
            else
            {
                this.filename = "";
            }
            if (File.Exists(filename))
            {
                var created = File.GetCreationTime(filename);
                recorded = created.ToShortDateString() + " " + created.ToShortTimeString();
            }
            this.recNumber = recNumber;

        }

        public ObservableList<bpaSegment> getSegmentList()
        {
            return (segmentList);
        }

        public string Comment { get; set; }

        public int SampleRate { get; set; } = 384000;

        
        public bool CreateSegments(decimal thresholdFactor,decimal spectrumFactor)
        {
            bool result = false;

            if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename) && filename.ToUpper().EndsWith(".WAV"))
            {
                int segNumber = 1;
                try
                {
                    
                    
                    TimeSpan duration = new TimeSpan();
                    try
                    {
                        using (AudioFileReader afr = new AudioFileReader(filename))
                        {
                            SampleRate = afr.WaveFormat.SampleRate;
                            duration = afr.TotalTime;

                        }
                    }catch(Exception ex)
                    {
                        AnalysisMainControl.ErrorLog($"Error using AudioFileReader ({segNumber}):" + ex.Message);
                    }
                    segmentList.Clear();
                    string textFileName = filename.Substring(0, filename.LastIndexOf(".")) + ".txt";
                    if (File.Exists(textFileName))
                    {
                        using (var sr = File.OpenText(textFileName))
                        {
                            if (sr != null)
                            {
                                while (!sr.EndOfStream)
                                {
                                    string line = sr.ReadLine();
                                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(@"\"))
                                    {
                                        continue; // no text or a continuation line so ignore it
                                    }
                                    getLabelLine(line, out double start, out double end,out string comment);
                                    if (!String.IsNullOrWhiteSpace(comment))
                                    {
                                        Comment = comment;
                                    }
                                    else
                                    {
                                        Comment = "";
                                    }
                                    var segmentLength = (int)((end - start) * SampleRate);
                                    //data = new float[segmentLength];
                                    //afr.Position = 0;
                                    //var sp = afr.ToSampleProvider();
                                    //sp.Skip(TimeSpan.FromSeconds(start));
                                    //sp.Read(data, 0, data.Length);

                                    DataAccessBlock dab = new DataAccessBlock(filename, (long)(start * SampleRate), (long)segmentLength,(long)segmentLength);
                                    bpaSegment segment = new bpaSegment(recNumber, segNumber++, (int)(start * SampleRate), dab, SampleRate,Comment);

                                    segmentList.Add(segment);
                                }
                            }
                            Debug.WriteLine($"Recording of {duration} in {segmentList.Count} segments");
                        }
                    }
                    else
                    {


                        long totalSamples = (int)(duration.TotalSeconds * SampleRate);
                        if (totalSamples > int.MaxValue)
                        {
                            throw new Exception("File to large to handle");
                        }
                        Comment = "";
                        using(var wfr=new WaveFileReader(filename))
                        {
                            var metadata = wfr.ExtraChunks;
                            foreach(var md in metadata)
                            {
                                if (md.IdentifierAsString == "guan")
                                {
                                    Comment += ReadGuanoComment(wfr, md);
                                }
                                else if (md.IdentifierAsString == "wamd")
                                {
                                    string c= ReadWAMDComment(wfr, md);
                                    Debug.WriteLine($"WAMD:- {c}");
                                }
                            }
                        }
                        //data = new float[totalSamples];
                        //afr.ToSampleProvider().Read(data, 0, (int)totalSamples);
                        DataAccessBlock dab = new DataAccessBlock(filename, 0, totalSamples,totalSamples);
                        bpaSegment segment = new bpaSegment(recNumber, segNumber++, 0, dab, SampleRate,Comment);

                        segmentList.Add(segment);

                    }


                    foreach (var segment in segmentList)
                    {
                        segment.CreatePasses(thresholdFactor,spectrumFactor);
                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"CreateSegments {segNumber}:" + ex.Message);
                    Debug.WriteLine(ex.Message);
                    result = false;
                }
            }
            return (result);
        }

        private string ReadWAMDComment(WaveFileReader wfr, RiffChunk md)
        {
            string result = "";
            var chunk = wfr.GetChunkData(md);
            string wamdChunk = System.Text.Encoding.UTF8.GetString(chunk);
            if (!String.IsNullOrWhiteSpace(wamdChunk))
            {
                result = wamdChunk;
            }
            return (result);
        }

        private string ReadGuanoComment(WaveFileReader wfr, RiffChunk md)
        {
            string result = "";
            var chunk = wfr.GetChunkData(md);
            string guanoChunk = System.Text.Encoding.UTF8.GetString(chunk);
            var lines = guanoChunk.Split('\n');
            foreach(var line in lines)
            {
                if(line.Contains("Manual ID:"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        return (parts[1]);
                    }
                }
            }
            return (result);
        }

        public void getLabelLine(string line,out double start,out double end,out string comment)
        {
            start = 0.0d;
            end = 0.0d;
            comment = "";
            string pattern= @"([0-9.]*)[\s-]*([0-9.]*)[\s-]*(.*)";
            var match = Regex.Match(line, pattern);
            if (match.Success)
            {
                if (match.Groups.Count > 1)
                {
                    if (double.TryParse(match.Groups[1].Value, out double startSecs))
                    {
                        start = startSecs;
                    }
                    if (match.Groups.Count > 2)
                    {
                        if (double.TryParse(match.Groups[2].Value, out double endSecs))
                        {
                            end = endSecs;
                        }
                        if (match.Groups.Count >3)
                        {
                            comment = match.Groups[3].Value;
                        }
                    }
                }
            }

        }
    }

}
