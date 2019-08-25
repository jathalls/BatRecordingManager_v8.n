// *  Copyright 2016 Justin A T Halls
//  *
//  *  This file is part of the Bat Recording Manager Project
// 
//         Licensed under the Apache License, Version 2.0 (the "License");
//         you may not use this file except in compliance with the License.
//         You may obtain a copy of the License at
// 
//             http://www.apache.org/licenses/LICENSE-2.0
// 
//         Unless required by applicable law or agreed to in writing, software
//         distributed under the License is distributed on an "AS IS" BASIS,
//         WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//         See the License for the specific language governing permissions and
//         limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

using Microsoft.VisualStudio.Language.Intellisense;


namespace BatRecordingManager
{
    /// <summary>
    ///     Class to hold details of a specific LabelledSegment, and a List of Bats that were present
    ///     during this segment.
    /// </summary>
    public class SegmentAndBatList
    {
        /// <summary>
        ///     The List of Bats present during the segment
        /// </summary>
        public BulkObservableCollection<Bat> BatList = new BulkObservableCollection<Bat>();

        /// <summary>
        ///     The Labelled Segment
        /// </summary>
        public LabelledSegment Segment = new LabelledSegment();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public bool Updated;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Initializes a new instance of the <see cref="SegmentAndBatList" /> class.
        /// </summary>
        public SegmentAndBatList()
        {
            Segment = new LabelledSegment();
            BatList = new BulkObservableCollection<Bat>();
            Updated = false;
        }

        /// <summary>
        ///     Creates a SegmentAndBatList item using the provided labelledSegment.
        ///     The SegmentAndBatList contains the provided segment and a list of all
        ///     the bats referenced by the segment comment.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        internal static SegmentAndBatList Create(LabelledSegment segment)
        {
            var segBatList = new SegmentAndBatList
            {
                Segment = segment, BatList = DBAccess.GetDescribedBats(segment.Comment)
            };


            //DBAccess.InsertParamsFromComment(segment.Comment, null);
            var listOfSegmentImages = segment.GetImageList();
            DBAccess.UpdateLabelledSegment(segBatList, segment.RecordingID, listOfSegmentImages, null);
            return segBatList;
        }

        /// <summary>
        ///     Processes the labelled segment. Accepts a processed segment comment line consisting
        ///     of a start offset, end offset, duration and comment string and generates a new
        ///     Labelled segment instance and BatSegmentLink instances for each bat represented in
        ///     the Labelled segment. The instances are merged into a single instance of
        ///     CombinedSegmentAndBatPasses to be returned. If the line to be processed is not in the
        ///     correct format then an instance containing an empty LabelledSegment instance and an
        ///     empty List of ExtendedBatPasses. The comment section is checked for the presence of a
        ///     call parameter string and if present new Call is created and populated.
        /// </summary>
        /// <param name="processedLine">
        ///     The processed line.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public static SegmentAndBatList ProcessLabelledSegment(string processedLine, BulkObservableCollection<Bat> bats)
        {
            var segment = new LabelledSegment();
            var result = new SegmentAndBatList();
            var match = Regex.Match(processedLine,
                "([0-9\\.\\']+)[\\\"]?\\s*-?\\s*([0-9\\.\\']+)[\\\"]?\\s*=\\s*([0-9\\.\']+)[\\\"]?\\s*(.+)");
            //e.g. (123'12.3)" - (123'12.3)" = (123'12.3)" (other text)
            if (match.Success)
                //int passes = 1;
                // The line structure matches a labelled segment
                if (match.Groups.Count > 3)
                {
                    segment.Comment = match.Groups[4].Value;

                    var ts = Tools.TimeParse(match.Groups[2].Value);
                    segment.EndOffset = ts;
                    ts = Tools.TimeParse(match.Groups[1].Value);
                    segment.StartOffset = ts;
                    result.Segment = segment;
                    result.BatList = bats;
                    //ts = TimeParse(match.Groups[3].Value);
                    //passes = new BatStats(ts).passes;
                }
            // result.batPasses = IdentifyBatPasses(passes, bats);

            return result;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////// FILE PROCESSOR CLASS  /////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    ///     This class handles the data processing for a single file, whether a manually generated
    ///     composite file or a label file created by Audacity.
    /// </summary>
    internal class FileProcessor
    {
        /// <summary>
        ///     The bats found
        /// </summary>
        public Dictionary<string, BatStats> BatsFound = new Dictionary<string, BatStats>();

        //private BulkObservableCollection<string> _linesToMerge = null;

        /// <summary>
        ///     The m bat summary
        /// </summary>
        //private BatSummary mBatSummary;
        //private Mode _mode = Mode.PROCESS;

        /// <summary>
        ///     The output string
        /// </summary>
        //private string _outputString = "";

        /// <summary>
        ///     Determines whether [is label file line] [the specified line].
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool IsLabelFileLine(string line, out string startStr, out string endStr, out string comment)
        {
            startStr = "";
            endStr = "";
            comment = "";
            //string regexLabelFileLine = "\\A\\s*(\\d*\\.?\\d*)\\\"?\\s+-?\\s*(\\d*\\.?\\d*)\\\"?\\s*(.*)";
            var regexLabelFileLine = "([0-9\\.\\'\\\"]+)\\s*-?\\s*([0-9\\.\\'\\\"]+)\\s*(.*)";
            // e.g. (groups in brackets) <start> (nnn.nnn)" - (nnn.nnn)" (other text)
            // (startTime)[ ][-][ ](endTime)[ ]([text][{text}])
            var match = Regex.Match(line, regexLabelFileLine);
            if (match.Success)
            {
                startStr = match.Groups[1].Value;
                if (startStr.Contains("'"))
                {
                    var ts = GetTimeOffset(startStr);
                    startStr = ts.TotalSeconds.ToString();
                }

                endStr = match.Groups[2].Value;
                if (endStr.Contains("'"))
                {
                    var ts = GetTimeOffset(endStr);
                    endStr = ts.TotalSeconds.ToString();
                }

                comment = match.Groups[3].Value;
                var moddedComment = comment;
                DBAccess.GetDescribedBats(comment, out moddedComment);
                comment = moddedComment;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Identifies a line of text as AboutScreen valid line from a label file and returns the comment section
        ///     and time fields in out parameters and true or false depending on whether it is a valid line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static bool IsLabelFileLine(string line, out TimeSpan start, out TimeSpan end, out string comment)
        {
            var endStr = "";
            if (!IsLabelFileLine(line, out var startStr, out endStr, out comment))
            {
                start = new TimeSpan();
                end = new TimeSpan();
                return false;
            }

            start = Tools.TimeParse(startStr);
            end = Tools.TimeParse(endStr);

            return true;
        }

        /// <summary>
        ///     Adds to bat summary.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="newDuration">
        ///     The new duration.
        /// </param>
        public static BulkObservableCollection<Bat> AddToBatSummary(string line, TimeSpan newDuration,
            ref Dictionary<string, BatStats> batsFound)
        {
            var bats = DBAccess.GetDescribedBats(line);
            if (!bats.IsNullOrEmpty())
                foreach (var bat in bats)
                {
                    var batname = bat.Name;
                    if (!string.IsNullOrWhiteSpace(batname))
                    {
                        if (batsFound.ContainsKey(batname))
                            batsFound[batname].Add(newDuration);
                        else
                            batsFound.Add(batname, new BatStats(newDuration));
                    }
                }

            return bats;
        }

        /// <summary>
        ///     Processes the file using ProcessLabelOrManualFile.
        ///     The file may be a .txt file which is a comment/log file made with
        ///     Audacity or a .wav file with embedded 'wamd' metadata
        /// </summary>
        /// <param name="batSummary">
        ///     The bat summary.
        /// </param>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="gpxHandler">
        ///     The GPX handler.
        /// </param>
        /// <param name="currentRecordingSessionId">
        ///     The current recording session identifier.
        /// </param>
        /// <returns>
        /// </returns>
        public static string ProcessFile(string fileName, GpxHandler gpxHandler, int currentRecordingSessionId,
            ref Dictionary<string, BatStats> batsFound)
        {
            //mBatSummary = batSummary;
            var outputString = "";
            if (fileName.ToUpper().EndsWith(".TXT") || fileName.ToUpper().EndsWith(".WAV"))
                outputString = ProcessLabelOrManualFile(fileName, gpxHandler, currentRecordingSessionId, ref batsFound);
            return outputString;
        }

        /// <summary>
        ///     Processes the manual file line.
        /// </summary>
        /// <param name="match">
        ///     The match.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        public static string ProcessManualFileLine(Match match, out BulkObservableCollection<Bat> bats,
            ref Dictionary<string, BatStats> batsFound)
        {
            var comment = "";
            bats = new BulkObservableCollection<Bat>();

            if (match.Groups.Count >= 5)
            {
                var strStartOffset = match.Groups[1].Value;
                var startTime = GetTimeOffset(strStartOffset);
                comment = comment + Tools.FormattedTimeSpan(startTime) + " - ";
                var strEndOffset = match.Groups[3].Value;
                var endTime = GetTimeOffset(strEndOffset);
                comment = comment + Tools.FormattedTimeSpan(endTime) + " = ";
                var thisDuration = endTime - startTime;
                comment = comment + Tools.FormattedTimeSpan(endTime - startTime) + " \t";
                for (var i = 4; i < match.Groups.Count; i++) comment = comment + match.Groups[i];
                bats = AddToBatSummary(comment, thisDuration, ref batsFound);
            }

            return comment + "\n";
        }

        /// <summary>
        ///     Gets the time offset.
        /// </summary>
        /// <param name="strTime">
        ///     The string time.
        /// </param>
        /// <returns>
        /// </returns>
        private static TimeSpan GetTimeOffset(string strTime)
        {
            var minutes = 0;
            var seconds = 0;
            var milliseconds = 0;
            var result = new TimeSpan();

            if (strTime.ToUpper().Contains("START") || strTime.ToUpper().Contains("END")) strTime = "0.0";

            var numberRegex = @"[0-9]+";
            var regex = new Regex(numberRegex);
            var allMatches = regex.Matches(strTime);
            if (allMatches != null)
            {
                if (allMatches.Count == 3)
                {
                    int.TryParse(allMatches[0].Value, out minutes);
                    int.TryParse(allMatches[1].Value, out seconds);
                    int.TryParse(allMatches[2].Value, out milliseconds);
                }
                else if (allMatches.Count == 2)
                {
                    if (strTime.Contains(@"'"))
                    {
                        int.TryParse(allMatches[0].Value, out minutes);
                        int.TryParse(allMatches[1].Value, out seconds);
                    }
                    else
                    {
                        int.TryParse(allMatches[0].Value, out seconds);
                        int.TryParse(allMatches[1].Value, out milliseconds);
                    }
                }
                else if (allMatches.Count == 1)
                {
                    if (strTime.Contains(@"'"))
                        int.TryParse(allMatches[0].Value, out minutes);
                    else
                        int.TryParse(allMatches[0].Value, out seconds);
                }

                result = new TimeSpan(0, 0, minutes, seconds, milliseconds);
            }

            return result;
        }

        /// <summary>
        ///     Gets the duration of the file. (NB would be improved by using various Regex to parse the
        ///     filename into dates and times
        /// </summary>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="wavfile">
        ///     The wavfile.
        /// </param>
        /// <param name="fileStart">
        ///     The file start.
        /// </param>
        /// <param name="fileEnd">
        ///     The file end.
        /// </param>
        /// <returns>
        /// </returns>
        private static TimeSpan GetFileDuration(string fileName, out string wavfile, out DateTime fileStart,
            out DateTime fileEnd)
        {
            DateTime creationTime;
            fileStart = new DateTime();
            fileEnd = new DateTime();

            var duration = new TimeSpan(0L);
            wavfile = "";
            try
            {
                var wavfilename = fileName.Substring(0, fileName.Length - 4);
                wavfilename = wavfilename + ".wav";
                if (File.Exists(wavfilename) && (new FileInfo(wavfilename).Length>0L))
                {
                    var info = new FileInfo(wavfilename);
                    wavfile = wavfilename;
                    var fa = File.GetAttributes(wavfile);

                    var recordingTime =
                        wavfilename.Substring(Math.Max(fileName.LastIndexOf('_'), fileName.LastIndexOf('-')) + 1, 6);

                    DateTime recordingDateTime;
                    creationTime = File.GetLastWriteTime(wavfilename);
                    if (string.IsNullOrWhiteSpace(recordingTime))
                        recordingTime = creationTime.Hour + creationTime.Minute.ToString() + creationTime.Second;

                    if (recordingTime.Length == 6)
                    {
                        if (!int.TryParse(recordingTime.Substring(0, 2), out var hour)) hour = -1;
                        if (!int.TryParse(recordingTime.Substring(2, 2), out var minute)) minute = -1;
                        if (!int.TryParse(recordingTime.Substring(4, 2), out var second)) second = -1;
                        if (hour >= 0 && minute >= 0 && second >= 0)
                        {
                            recordingDateTime = new DateTime(creationTime.Year, creationTime.Month, creationTime.Day,
                                hour, minute, second);
                            duration = creationTime - recordingDateTime;
                            if (duration < new TimeSpan()) duration = duration.Add(new TimeSpan(24, 0, 0));
                            fileStart = recordingDateTime;
                            fileEnd = creationTime;
                        }
                    }
                    else
                    {
                        if (creationTime != null)
                        {
                            fileStart = creationTime;
                            fileEnd = creationTime;
                            duration = new TimeSpan();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }

            return duration;
        }

        /*
        private BulkObservableCollection<BatSegmentLink> IdentifyBatPasses(int passes, BulkObservableCollection<Bat> bats)
        {
            BulkObservableCollection<BatSegmentLink> passList = new BulkObservableCollection<BatSegmentLink>();
            foreach (var bat in bats)
            {
                BatSegmentLink pass = new BatSegmentLink();
                pass.Bat = bat;
                pass.NumberOfPasses = passes;
                passList.Add(pass);
            }
            return (passList);
        }*/

        /// <summary>
        ///     Determines whether [is manual file line] [the specified line].
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <returns>
        /// </returns>
        private static Match IsManualFileLine(string line)
        {
            //string regexLabelFileLine = @"\A((\d*'?\s*\d*\.?\d*)|START)\s*-\s*((\d*'?\s*\d*\.?\d*)|END)\s*.*";
            var regexLabelFileLine = "([0-9.'\"]+)([\\s\t-]+)([0-9.'\"]+)\\s+(.*)";
            var match = Regex.Match(line, regexLabelFileLine);
            if (match == null || match.Groups.Count < 5) match = null;

            if (match != null && match.Success) return match;
            return null;
        }

        /// <summary>
        ///     Processes the label file line.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        private static string ProcessLabelFileLine(string line, string startStr, string endStr, string comment,
            out BulkObservableCollection<Bat> bats, ref Dictionary<string, BatStats> batsFound)
        {
            var result = "";
            bats = new BulkObservableCollection<Bat>();

            if (!string.IsNullOrWhiteSpace(line) && char.IsDigit(line[0]))
            {
                result = ProcessLabelLine(line, startStr, endStr, comment, out var newDuration) + "\n";
                bats = AddToBatSummary(line, newDuration, ref batsFound);
            }
            else
            {
                result = line + "\n";
            }

            return result;
        }

        /// <summary>
        ///     Processes the label line.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <param name="newDuration">
        ///     The new duration.
        /// </param>
        /// <returns>
        /// </returns>
        private static string ProcessLabelLine(string line, string startStr, string endStr, string comment,
            out TimeSpan newDuration)
        {
            newDuration = new TimeSpan(0L);
            line = line.Trim();
            if (!char.IsDigit(line[0])) return line;
            var outLine = "";
            TimeSpan startTime;
            TimeSpan endTime;
            TimeSpan duration;
            var shortened = line;

            /*Regex regexSeconds = new Regex(@"([0-9]+\.[0-9]+)\s*-*\s*([0-9]+\.[0-9]+)\s*(.*)");
            Match match = Regex.Match(line, @"([0-9]+\.[0-9]+)\s*-*\s*([0-9]+\.[0-9]+)\s*(.*)");
            //MatchCollection allMatches = regexSeconds.Matches(line);
            if (match.Success)
            {*/
            double.TryParse(startStr, out var startTimeSeconds);
            double.TryParse(endStr, out var endTimeSeconds);

            var minutes = (int) Math.Floor(startTimeSeconds / 60);
            var seconds = (int) Math.Floor(startTimeSeconds - minutes * 60);
            var milliseconds = (int) Math.Floor(1000 * (startTimeSeconds - Math.Floor(startTimeSeconds)));
            startTime = new TimeSpan(0, 0, minutes, seconds, milliseconds);
            minutes = (int) Math.Floor(endTimeSeconds / 60);
            seconds = (int) Math.Floor(endTimeSeconds - minutes * 60);
            milliseconds = (int) Math.Floor(1000 * (endTimeSeconds - Math.Floor(endTimeSeconds)));
            endTime = new TimeSpan(0, 0, minutes, seconds, milliseconds);

            duration = endTime - startTime;
            newDuration = duration;
            shortened = comment;

            outLine = Tools.FormattedTimeSpan(startTime) + " - " + Tools.FormattedTimeSpan(endTime) + " = " +
                      Tools.FormattedTimeSpan(duration) + "\t" + shortened;
            //outLine = String.Format("{0:00}\'{1:00}.{2:0##} - {3:00}\'{4:00}.{5:0##} = {6:00}\'{7:00}.{8:0##}\t{9}",
            //StartTime.Minutes, StartTime.Seconds, StartTime.Milliseconds,
            //EndTime.Minutes, EndTime.Seconds, EndTime.Milliseconds,
            //duration.Minutes, duration.Seconds, duration.Milliseconds, shortened);
            /*
            outLine = StartTime.Minutes + @"'" + StartTime.Seconds + "." + StartTime.Milliseconds +
            " - " + EndTime.Minutes + @"'" + EndTime.Seconds + "." + EndTime.Milliseconds +
            " = " + duration.Minutes + @"'" + duration.Seconds + "." + duration.Milliseconds +
            "\t" + shortened;*/
            /* }
             else
             {
                 StartTime = new TimeSpan();
                 EndTime = new TimeSpan();
                 outLine = line;
             }*/

            return outLine;
        }

        /// <summary>
        ///     Re-processes the specified label file, updating the Labelled segments in the
        ///     database with new ones derived from the specified file.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="labelFileName"></param>
        public static string UpdateRecording(Recording recording, string labelFileName)
        {
            string result = "";
            var batsFound = new Dictionary<string, BatStats>();
            DBAccess.DeleteAllSegmentsForRecording(recording.Id);
            result=ProcessLabelOrManualFile(labelFileName, new GpxHandler(recording.RecordingSession.Location),
                recording.RecordingSession.Id,recording, ref batsFound);
           
            
            return (result);
        }

        /// <summary>
        ///     Processes a text file with a simple .txt extension that has been generated as an
        ///     Audacity LabelTrack. The fileName will be added to the output at the start of the OutputString.
        ///     Mod 22/3/2017 allow the use of txt files from Audacity 2.1.3 which may include spectral info
        ///     in the label on a second line starting with a '\'
        /// </summary>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="gpxHandler">
        ///     The GPX handler.
        /// </param>
        /// <param name="currentRecordingSessionId">
        ///     The current recording session identifier.
        /// </param>
        /// <returns>
        /// </returns>
        private static string ProcessLabelOrManualFile(string fileName, GpxHandler gpxHandler,
            int currentRecordingSessionId, ref Dictionary<string, BatStats> batsFound)
        {
            batsFound = new Dictionary<string, BatStats>();
            var recording = new Recording();
            if (currentRecordingSessionId <= 0)
            {
                MessageBox.Show("No session identified for these recordings", "ProcessLabelOrManualFile");
                Tools.ErrorLog("No session defined for recordings");
                return "";
            }

            recording.RecordingSessionId = currentRecordingSessionId;

            return ProcessLabelOrManualFile(fileName, gpxHandler, currentRecordingSessionId, recording, ref batsFound);
        }

        private static string ProcessLabelOrManualFile(string fileName, GpxHandler gpxHandler,
            int currentRecordingSessionId, Recording recording, ref Dictionary<string, BatStats> batsFound)
        {
            var listOfsegmentAndBatLists = new BulkObservableCollection<SegmentAndBatList>();
            var outputString = "";

            var mode = Mode.PROCESS;
            var duration = new TimeSpan();

            batsFound = new Dictionary<string, BatStats>();

            WavFileMetaData wfmd = null;

            try
            {
                if (File.Exists(fileName))
                {
                    var wavfile = fileName.Substring(0, fileName.Length - 4) + ".wav";
                    duration = GetFileDuration(fileName, out wavfile, out var fileStart, out var fileEnd);
                    if (File.Exists(wavfile) && (new FileInfo(wavfile).Length>0L))
                    {
                        wfmd = new WavFileMetaData(wavfile);
                        //Guano GuanoData=new Guano(Guano.GetGuanoData(CurrentRecordingSessionId, wavfile));
                        if (wfmd?.m_Duration != null)
                        {
                            duration = wfmd.m_Duration.Value;
                            if (fileEnd > fileStart + duration)
                                fileStart = fileEnd - duration;
                            else
                                fileEnd = fileStart + duration;

                            recording.RecordingNotes = wfmd.FormattedText();
                        }

                        if (wfmd != null)
                        {
                            recording.RecordingNotes = wfmd.FormattedText();
                        }
                    }

                    recording.RecordingStartTime = fileStart.TimeOfDay;
                    recording.RecordingEndTime = fileEnd.TimeOfDay;
                    recording.RecordingDate = Tools.GetDateFromFilename(fileName);
                    outputString = fileName;
                    if (!string.IsNullOrWhiteSpace(wavfile))
                    {
                        outputString = wavfile;
                        recording.RecordingName = wavfile.Substring(wavfile.LastIndexOf('\\'));
                    }

                    if (duration.Ticks > 0L)
                        outputString = outputString + " \t" + duration.Minutes + "m" + duration.Seconds + "s";
                    outputString = outputString + "\n";
                    var gpsLocation = gpxHandler.GetLocation(fileStart);
                    if (gpsLocation != null && gpsLocation.Count == 2)
                    {
                        outputString = outputString + gpsLocation[0] + ", " + gpsLocation[1];
                        recording.RecordingGPSLatitude = gpsLocation[0].ToString();
                        recording.RecordingGPSLongitude = gpsLocation[1].ToString();
                        if (recording.RecordingSession != null)
                            if (recording.RecordingSession.LocationGPSLatitude == null ||
                                recording.RecordingSession.LocationGPSLatitude < 5.0m)
                            {
                                recording.RecordingSession.LocationGPSLatitude = gpsLocation[0];
                                recording.RecordingSession.LocationGPSLongitude = gpsLocation[1];
                            }
                    }

                    gpsLocation = gpxHandler.GetLocation(fileEnd);
                    if (gpsLocation != null && gpsLocation.Count == 2)
                        outputString = outputString + " => " + gpsLocation[0] + ", " + gpsLocation[1] + "\n";
                    if (string.IsNullOrWhiteSpace(recording.RecordingGPSLatitude))
                        try
                        {
                            if (wfmd?.m_Location != null && wfmd.m_Location.m_Latitude < 200.0d &&
                                wfmd.m_Location.m_Longitude < 200.0d)
                            {
                                var location = new Tuple<double, double>(wfmd.m_Location.m_Latitude,
                                    wfmd.m_Location.m_Longitude);
                                recording.RecordingGPSLatitude = location.Item1.ToString();
                                recording.RecordingGPSLongitude = location.Item2.ToString();
                                if (recording.RecordingSession != null &&
                                    (recording.RecordingSession.LocationGPSLatitude == null ||
                                     recording.RecordingSession.LocationGPSLatitude < 5.0m))
                                {
                                    recording.RecordingSession.LocationGPSLatitude = (decimal) location.Item1;
                                    recording.RecordingSession.LocationGPSLongitude = (decimal) location.Item2;
                                }
                            }
                        }
                        catch (NullReferenceException nex)
                        {
                            Tools.ErrorLog(nex.Message);
                            Debug.WriteLine("*** ProcessLabelOrManualFile:-LocationData:-" + nex.Message);
                        }

                    if (fileName.ToUpper().EndsWith(".TXT"))
                    {
                        outputString = outputString + ProcessTextFile(fileName, duration, ref listOfsegmentAndBatLists,
                                           mode, ref batsFound);
                    }
                    else if (fileName.ToUpper().EndsWith(".WAV"))
                    {
                        var comment = ProcessWavFile(fileName, duration, ref listOfsegmentAndBatLists, mode,
                            ref batsFound);
                        recording.RecordingNotes = recording.RecordingNotes + " " + comment;
                        outputString = outputString + comment;
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Error Processing File <" + fileName + ">: " + ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(outputString) && !batsFound.IsNullOrEmpty())
                foreach (var bat in batsFound)
                {
                    bat.Value.batCommonName = bat.Key;
                    outputString = outputString + "\n" + Tools.GetFormattedBatStats(bat.Value, true);
                }

            if (!listOfsegmentAndBatLists.IsNullOrEmpty())
            {
                for (var i = listOfsegmentAndBatLists.Count - 1; i >= 0; i--)
                    if (string.IsNullOrWhiteSpace(listOfsegmentAndBatLists[i].Segment.Comment))
                        listOfsegmentAndBatLists.RemoveAt(i);
                DBAccess.UpdateRecording(recording, listOfsegmentAndBatLists, null);
            }

            return outputString;
        }

        private static string ProcessTextFile(string fileName, TimeSpan duration,
            ref BulkObservableCollection<SegmentAndBatList> listOfsegmentAndBatLists, Mode mode,
            ref Dictionary<string, BatStats> batsFound)
        {
            var allLines = new string[1];
            var outputString = "";

            try
            {
                if (fileName.ToUpper().EndsWith(".TXT"))
                {
                    allLines = File.ReadAllLines(fileName);
                    if (!allLines.Any() || string.IsNullOrWhiteSpace(allLines[0]))
                        allLines = new[] {"Start - End \t No Bats"};
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex+"\n  ******** Assuming empty text file and no bats");
                allLines = new[] { "Start - End \t No Bats" };
            }

            outputString = ProcessText(allLines, duration, ref listOfsegmentAndBatLists, mode, ref batsFound);
            return outputString;
        }

        private static string ProcessWavFile(string fileName, TimeSpan duration,
            ref BulkObservableCollection<SegmentAndBatList> listOfsegmentAndBatLists, Mode mode,
            ref Dictionary<string, BatStats> batsFound)
        {
            var allLines = new string[1];
            var outputString = "";
            WavFileMetaData wfmd;
            var line = "";

            try
            {
                wfmd = new WavFileMetaData(fileName);
                if (wfmd != null)
                {
                    if (wfmd.m_Duration != null)
                    {
                        line = "0 - " + wfmd.m_Duration.Value.TotalSeconds;
                        duration = wfmd.m_Duration.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(wfmd.m_ManualID)) line += " " + wfmd.m_ManualID;
                    if (!string.IsNullOrWhiteSpace(wfmd.m_AutoID)) line += ", " + wfmd.m_AutoID;
                    if (!string.IsNullOrWhiteSpace(wfmd.m_Note)) line += ", " + wfmd.m_Note;
                }

                allLines[0] = line;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }

            outputString = ProcessText(allLines, duration, ref listOfsegmentAndBatLists, mode, ref batsFound);
            return outputString;
        }

        private static string ProcessText(string[] allLines, TimeSpan duration,
            ref BulkObservableCollection<SegmentAndBatList> listOfsegmentAndBatLists, Mode mode,
            ref Dictionary<string, BatStats> batsFound)
        {
            var outputString = "";
            var linesToMerge = new BulkObservableCollection<string>();

            Match match = null;

            if (allLines.Length > 1 && allLines[0].StartsWith("["))
            {
                if (allLines[0].ToUpper().StartsWith("[SKIP]") || allLines[0].ToUpper().StartsWith("[LOG]"))
                {
                    mode = Mode.COPY;
                    return "";
                }

                if (allLines[0].ToUpper().StartsWith("[COPY]"))
                {
                    mode = Mode.COPY;
                    outputString = "";
                    foreach (var line in allLines)
                    {
                        if (line.Contains("[MERGE]"))
                        {
                            mode = Mode.MERGE;

                            linesToMerge = new BulkObservableCollection<string>();
                        }

                        if (!line.Contains("[COPY]") && !line.Contains("[MERGE]"))
                        {
                            if (mode == Mode.MERGE)
                                linesToMerge.Add(line);
                            else
                                outputString = outputString + line + "\n";
                        }
                    }

                    return outputString;
                }
            }

            if (!allLines.IsNullOrEmpty())
            {
                if (!linesToMerge.IsNullOrEmpty())
                {
                    outputString = outputString + linesToMerge[0] + "\n";
                    linesToMerge.Remove(linesToMerge[0]);
                }

                for (var ln = 0; ln < allLines.Length; ln++)
                {
                    var line = allLines[ln];
                    if (ln + 1 < allLines.Length && allLines[ln + 1].StartsWith(@"\"))
                    {
                        var spectralParams = allLines[ln + 1];
                        ln++;

                        line = AddSpectralParametersToLine(line, spectralParams);
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var modline = Regex.Replace(line, @"[Ss][Tt][Aa][Rr][Tt]", "0.0");
                        modline = Regex.Replace(modline, @"[Ee][Nn][Dd]", ((decimal) duration.TotalSeconds).ToString());
                        var processedLine = "";
                        var bats = new BulkObservableCollection<Bat>();
                        if (IsLabelFileLine(modline, out string startStr, out var endStr, out var comment))
                            processedLine = ProcessLabelFileLine(modline, startStr, endStr, comment, out bats,
                                ref batsFound);
                        else if ((match = IsManualFileLine(modline)) != null)
                            processedLine = ProcessManualFileLine(match, out bats, ref batsFound);
                        else
                            processedLine = line + "\n";
                        listOfsegmentAndBatLists.Add(SegmentAndBatList.ProcessLabelledSegment(processedLine, bats) ??
                                                     new SegmentAndBatList());
                        // one added for each line that is processed as a segment label
                        outputString = outputString + processedLine;
                    }
                }
            }

            return outputString;
        }

        /// <summary>
        ///     When reading a line from an Audacity Label file, since Audacity 2.1.3
        ///     the label may include a second line starting with a '\' containing the
        ///     upper and lower frequencies of the selection when the label was added.
        ///     This function is passed as parameters the label text line and the second line
        ///     starting with the '\'.
        ///     If the label includes the string "{}" the selection parameters are ignored.
        ///     If the label does not include a parameter section in {} then the frequency
        ///     parameters are added as start and end frequencies.
        ///     If the label includes a parameters section which includes s= or e= then the
        ///     selection parameters are ignored.
        ///     If the label includes a parameters section which starts with a number and includes
        ///     a comma then it is assumed to be an implicit parameter section and the selection
        ///     parameters are ignored.
        ///     Otherwise the selection parameters are trimmed to two decimal places and inserted
        ///     as {s=high,end=low}
        /// </summary>
        /// <param name="line"></param>
        /// <param name="spectralParams"></param>
        /// <returns></returns>
        private static string AddSpectralParametersToLine(string line, string spectralParams)
        {
            if (line.Contains(@"{}")) return line.Replace("{}", "");

            var fmax = -1.0d;
            var fmin = -1.0d;
            if (spectralParams.StartsWith(@"\"))
            {
                spectralParams = spectralParams.Substring(1);
                var freqs = spectralParams.Split('\t');
                var maxparam = 0;

                if (freqs.Length > 2)
                    maxparam = 2;
                else if (freqs.Length > 1) maxparam = 1;
                if (maxparam > 0)
                {
                    double.TryParse(freqs[maxparam - 1], out fmin);
                    double.TryParse(freqs[maxparam], out fmax);
                    if (fmax < fmin)
                    {
                        var temp = fmin;
                        fmin = fmax;
                        fmax = temp;
                    }
                }
            }

            if (fmin < 0.0d) return line;

            if (line.Contains("{"))
            {
                var parts = line.Split('{');
                if (parts.Length > 1)
                {
                    if (parts[1].StartsWith("{")) parts[1] = parts[1].Substring(1);

                    if (parts[1].Contains("s=") || parts[1].Contains("e=")) return line;

                    if (char.IsDigit(parts[1].Trim()[0]) && parts[1].Contains(",")) return line;

                    line = $"{parts[0] + "{"}s={fmax:F2},e={fmin:F2},{parts[1]}";
                }
            }
            else
            {
                line = $"{line + " {"}s={fmax:F2},e={fmin:F2}{"}"}";
            }

            return line;
        }

        private enum Mode
        {
            PROCESS,
            SKIP,
            COPY,
            MERGE
        }

        /*       /// <summary>
               /// using a string that matches the regex @"[0-9]+\.[0-9]+" or a string that matches
               /// the regex @"[0-9]+'?[0-9]*\.?[0-9]+" extracts one to three numeric portions and
               /// converts them to a timespan. 3 number represent minute,seconds,fraction 2 numbers
               /// represent seconds,fraction or minutes,seconds 1 number represents minutes or
               /// seconds </summary> <param name="match">The match.</param> <returns></returns>
               private static TimeSpan GetTimeOffset(Match match)
               {
                   return (FileProcessor.GetTimeOffset(match.Value));
               }*/
    }
}