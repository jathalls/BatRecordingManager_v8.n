using Acr.Settings;
using DspSharp.Utilities.Collections;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatPassAnalysisFW

{
    /// <summary>
    /// Class representing the data in a single recording or .wav file.  If the file has an associated .txt file
    /// it is split into identified segments, otherwise it is considered to be a single segment.
    /// </summary>
    public class bpaRecording
    {
        public bpaRecording(int recNumber, string FQfileName)
        {
            if (!string.IsNullOrWhiteSpace(FQfileName))
            {
                this.FQfilename = FQfileName;
            }
            else
            {
                this.FQfilename = "";
            }
            if (File.Exists(FQfilename))
            {
                var created = File.GetCreationTime(FQfilename);
                recorded = created.ToShortDateString() + " " + created.ToShortTimeString();
            }
            this.recNumber = recNumber;
        }

        public string Comment { get; set; }

        // Visible public members
        public string File_Name
        {
            get
            {
                string fn;
                if (FQfilename.Contains(@"\"))
                {
                    fn = FQfilename.Substring(FQfilename.LastIndexOf(@"\"));
                }
                else
                {
                    fn = FQfilename;
                }
                return (fn);
            }
        }

        /// <summary>
        /// the name of the file containing the recording being processed
        /// </summary>
        public string FQfilename { get; set; } = "";

        public int? PassCount
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

        public int recNumber { get; set; }
        public string recorded { get; set; }
        public int SampleRate { get; set; } = 384000;

        public int segmentCount
        {
            get
            {
                return (segmentList.Count());
            }
        }

        /// <summary>
        /// adds a label to the specified text file with the specified start and end times and no comment
        /// unless start and end are both zero in which case eadd a text of 'No Bats'
        /// </summary>
        /// <param name="textFileName"></param>
        /// <param name="startOfLabel"></param>
        /// <param name="endOfLabel"></param>
        public static void CreateLabel(string textFileName, float startOfLabel, float endOfLabel, string comment = "")
        {
            startOfLabel -= 1.0f;
            if (startOfLabel < 0.0f) startOfLabel = 0.0f;
            endOfLabel += 1.0f;

            if ((endOfLabel - startOfLabel) < 0.0005)
            {
                File.AppendAllText(textFileName, $"{startOfLabel}\t{endOfLabel}\tNo Bats\n");
            }
            else
            {
                File.AppendAllText(textFileName, $"{startOfLabel}\t{endOfLabel}\t{comment}".Trim() + "\n");
            }
        }

        //private float[] data;
        public void AddSegment(bpaSegment segment)
        {
            segmentList.Add(segment);
        }

        public bool CreateSegments(decimal thresholdFactor, decimal spectrumFactor)
        {
            bool result = false;

            if (!string.IsNullOrWhiteSpace(FQfilename) && File.Exists(FQfilename) && FQfilename.ToUpper().EndsWith(".WAV"))
            {
                int segNumber = 1;
                try
                {
                    TimeSpan duration = new TimeSpan();
                    try
                    {
                        using (AudioFileReader afr = new AudioFileReader(FQfilename))
                        {
                            SampleRate = afr.WaveFormat.SampleRate;
                            duration = afr.TotalTime;
                        }
                    }
                    catch (Exception ex)
                    {
                        AnalysisMainControl.ErrorLog($"Error using AudioFileReader ({segNumber}):" + ex.Message);
                    }
                    segmentList.Clear();
                    string textFQFileName = FQfilename.Substring(0, FQfilename.LastIndexOf(".")) + ".txt";
                    if (File.Exists(textFQFileName))
                    {
                        using (var sr = File.OpenText(textFQFileName))
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
                                    getLabelLine(line, out double startTimeOfSegment, out double end, out string comment);
                                    if (!string.IsNullOrWhiteSpace(comment))
                                    {
                                        Comment = comment;
                                    }
                                    else
                                    {
                                        Comment = "";
                                    }
                                    var segmentLength = (int)((end - startTimeOfSegment) * SampleRate);
                                    if (duration.TotalSeconds > 0) segmentLength = (int)(duration.TotalSeconds * SampleRate);
                                    //data = new float[segmentLength];
                                    //afr.Position = 0;
                                    //var sp = afr.ToSampleProvider();
                                    //sp.Skip(TimeSpan.FromSeconds(start));
                                    //sp.Read(data, 0, data.Length);

                                    DataAccessBlock dab = new DataAccessBlock(FQfilename, (long)(startTimeOfSegment * SampleRate), segmentLength);
                                    bpaSegment segment = new bpaSegment(recNumber, segNumber++, (int)(startTimeOfSegment * SampleRate), dab, SampleRate, Comment);

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
                        using (var wfr = new WaveFileReader(FQfilename))
                        {
                            var metadata = wfr.ExtraChunks;
                            foreach (var md in metadata)
                            {
                                if (md.IdentifierAsString == "guan")
                                {
                                    Comment += ReadGuanoComment(wfr, md);
                                }
                                else if (md.IdentifierAsString == "wamd")
                                {
                                    string c = ReadWAMDComment(wfr, md);
                                    Debug.WriteLine($"WAMD:- {c}");
                                }
                            }
                        }
                        //data = new float[totalSamples];
                        //afr.ToSampleProvider().Read(data, 0, (int)totalSamples);
                        DataAccessBlock dab = new DataAccessBlock(FQfilename, 0, totalSamples); // dab for the whole recording is a single segment
                        bpaSegment segment = new bpaSegment(recNumber, segNumber++, 0, dab, SampleRate, Comment);

                        segmentList.Add(segment);
                    }

                    foreach (var segment in segmentList)
                    {
                        segment.CreatePasses(thresholdFactor, spectrumFactor);
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

        public void getLabelLine(string line, out double start, out double end, out string comment)
        {
            start = 0.0d;
            end = 0.0d;
            comment = "";
            string pattern = @"([0-9.]*)[\s-]*([0-9.]*)[\s-]*(.*)";
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
                        if (match.Groups.Count > 3)
                        {
                            comment = match.Groups[3].Value;
                        }
                    }
                }
            }
        }

        public ObservableList<bpaSegment> getSegmentList()
        {
            return (segmentList);
        }

        /// <summary>
        /// appends <see langword="abstract"/>comment string for the designated pass and its parent segment
        /// </summary>
        /// <param name="pass"></param>
        /// <param name="v"></param>
        internal void appendCommentForPass(bpaPass pass, string comment)
        {
            var segment = (from seg in segmentList
                           where seg.No == pass.segmentNumber
                           select seg).FirstOrDefault();
            if (segment != null && segment.No > 0)
            {
                segment.Comment += $"({pass.Pass_Number}:{comment}";
                segment.AppendCommentForPass(pass, comment);
            }
        }

        /// <summary>
        /// Deletes the specified pass and segment if appropriate
        /// </summary>
        /// <param name="pass"></param>
        internal void DeletePass(bpaPass pass)
        {
            var segment = (from seg in segmentList
                           where seg.No == pass.segmentNumber
                           select seg).SingleOrDefault();
            if (segment != null)
            {
                int passesRemaining = segment.DeletePass(pass);
                if (passesRemaining == 0)
                {
                    segmentList.Remove(segment);
                }
            }
        }

        /// <summary>
        /// For a recording without a lebel file, if the recording length is greater than 10s, then
        /// creates an Audacity style label file using pulse data to create new segments
        /// </summary>
        internal void GenerateLabelFile()
        {
            string textFileName = Path.ChangeExtension(FQfilename, ".txt");
            if (File.Exists(textFileName))
            {
                string bakFileName = Path.ChangeExtension(textFileName, ".bak");
                if (File.Exists(bakFileName)) File.Delete(bakFileName);
                File.Copy(textFileName, bakFileName);
                File.Delete(textFileName);
            }
            bool inLabel = false;
            float timeOfLastPulse = 0.0f;
            float startOfLabel = 0.0f;
            float endOfLabel = 0.0f;
            int pulseCount = 0;
            int startPass = 0;
            int endPass = 0;
            foreach (var segment in segmentList)
            {
                string segComment = segment.Comment;
                foreach (var pass in segment.getPassList())
                {
                    float startOfSegmentInRecording = (float)segment.GetOffsetInRecording().TotalSeconds;
                    foreach (var pulse in pass.getPulseList())
                    {
                        float pulseStartInSegment = pulse.getPeak().GetStartAsSampleInSeg() / (float)SampleRate;
                        float pulseStartInrecording = pulseStartInSegment + startOfSegmentInRecording;
                        if (!inLabel)
                        {
                            startOfLabel = pulseStartInrecording;
                            startPass = pass.Pass_Number;
                            inLabel = true;
                        }
                        else
                        {
                            if (pulseStartInrecording - timeOfLastPulse > 1.5f)
                            {
                                endOfLabel = timeOfLastPulse;
                                if (pulseCount >= 3)
                                {
                                    CreateLabel(textFileName, startOfLabel, endOfLabel, segComment + $" passes {startPass}-{endPass}");
                                }
                                startOfLabel = pulseStartInrecording;
                                startPass = pass.Pass_Number;
                                pulseCount = 0;
                            }
                        }
                        timeOfLastPulse = pulseStartInrecording;
                        pulseCount++;
                    }
                }
            }
            endOfLabel = timeOfLastPulse;
            if (pulseCount >= 3)
            {
                CreateLabel(textFileName, startOfLabel, endOfLabel, $"Passes {startPass}-endOfSeg");
            }
        }

        internal decimal getSpectrumThresholdFactor()
        {
            if (segmentList != null && segmentList.Count > 0)
            {
                return (segmentList.First().getSpectrumThresholdFactor());
            }
            return (CrossSettings.Current.Get<decimal>("SpectrumThresholdFactor"));
        }

        internal decimal getThresholdFactor()
        {
            if (segmentList != null && segmentList.Count > 0)
            {
                return (segmentList.First().getEnvelopeThresholdFactor());
            }
            return (CrossSettings.Current.Get<decimal>("EnvelopeThresholdFactor"));
        }

        /// <summary>
        /// Given a list of passes set them back into their segments in the recordings segment list
        /// </summary>
        /// <param name="list"></param>
        internal void setPassList(List<bpaPass> passList)
        {
            var segmentsUsedByThesePasses = (from seg in segmentList
                                             from pass in passList
                                             where seg.No == pass.segmentNumber
                                             select seg).Distinct();
            foreach (var seg in segmentsUsedByThesePasses)
            {
                var passesForThisSegment = from pass in passList
                                           where pass.segmentNumber == seg.No
                                           select pass;
                seg.setPassList(passesForThisSegment.ToList<bpaPass>());
            }
        }

        /// <summary>
        /// A list of all the segments in the recording and their associated data
        /// </summary>
        private ObservableList<bpaSegment> segmentList { get; set; } = new ObservableList<bpaSegment>();

        private string ReadGuanoComment(WaveFileReader wfr, RiffChunk md)
        {
            string result = "";
            var chunk = wfr.GetChunkData(md);
            string guanoChunk = System.Text.Encoding.UTF8.GetString(chunk);
            var lines = guanoChunk.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Manual ID:"))
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

        private string ReadWAMDComment(WaveFileReader wfr, RiffChunk md)
        {
            string result = "";
            var chunk = wfr.GetChunkData(md);
            string wamdChunk = System.Text.Encoding.UTF8.GetString(chunk);
            if (!string.IsNullOrWhiteSpace(wamdChunk))
            {
                result = wamdChunk;
            }
            return (result);
        }
    }
}