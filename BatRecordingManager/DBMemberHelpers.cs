﻿// *  Copyright 2016 Justin A T Halls
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
using System.Globalization;
using System.IO;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Helper functions for various database member classes which are auto-generated and therefore
    ///     cannot be modified directly
    /// </summary>
    public static class DbMemberHelpers
    {
        /// <summary>
        ///     Returns the fully qaulified filename of the recording
        ///     Returns null if the directory or the fully qualified file are not
        ///     found on this computer
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public static string GetFileName(this Recording recording, RecordingSession session = null)
        {
            if (session == null) session = recording.RecordingSession;
            if (session == null) return null;
            string filename;
            var path = session.OriginalFilePath;
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (!path.EndsWith("\\")) path = path + "\\";
            if (!Directory.Exists(path)) return null;
            if (string.IsNullOrWhiteSpace(recording.RecordingName)) return null;
            if (recording.RecordingName.StartsWith("\\"))
                recording.RecordingName = recording.RecordingName.Substring(1);
            filename = path + recording.RecordingName;
            if (!File.Exists(filename) || (new FileInfo(filename).Length<=0L)) return null;


            return filename;
        }

        /// <summary>
        /// Writes a new text file for this recording in the original folder and with the same name as
        /// the recording but with a .txt extension.  The text file contains times and comments from each
        /// Labelled segment in the recording.  If partial is true the file is only written if it does not
        /// already exist.  Otherwise any existing text file will be renamed .bak and replaced.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="partial"></param>
        public static void WriteTextFile(this Recording recording, bool partial)
        {
            string filename = recording.RecordingSession.OriginalFilePath + recording.RecordingName;
            filename = filename.Substring(0,filename.Length - 3) + "txt";
            if (!partial && File.Exists(filename))
            {
                string bakfilename = filename.Substring(0,filename.Length - 3) + "bak";
                if(File.Exists(bakfilename))File.Delete(bakfilename);
                File.Move(filename,bakfilename);
            }

            if (File.Exists(filename)) return;
            string contents = "";
            foreach (var segment in recording.LabelledSegments)
            {
                contents+= segment.StartOffset.TotalSeconds + "\t" +
                         segment.EndOffset.TotalSeconds + "\t" + segment.Comment+"\n";

            }
            File.WriteAllText(filename,contents);

        }

        /// <summary>
        /// Extension method on RecordingSession.  Recreates the header text file for the session
        /// using the data in the database.  If partial is true then an existing file will not
        /// be overwritten.  If false then an existing file is renamed as .bak and replaced with
        /// the newly created file - this may result in text duplication if the session Notes contain
        /// the text of the original file.
        /// Header file is assumed to be the same name as the sessionTag
        /// </summary>
        /// <param name=""></param>
        /// <param name="session"></param>
        /// <param name="partial"></param>
        public static void WriteTextFile(this RecordingSession session,bool partial)
        {
            string filename = session.OriginalFilePath + session.SessionTag + ".txt";
            
            if (!partial && File.Exists(filename))
            {
                string bakfilename = filename.Substring(0,filename.Length - 3) + "bak";
                if (File.Exists(bakfilename)) File.Delete(bakfilename);
                File.Move(filename, bakfilename);
            }

            if (File.Exists(filename)) return;
            string contents = "";
            contents = "[COPY]\n";
            contents += session.Location+"\n";
            contents += $"{session.Operator}\n";

            contents += $"{session.SessionDate.Date.ToLongDateString()} {session.SessionStartTime.ToString()} - ";
            if (session.EndDate != null && session.EndDate.Value.Date != session.SessionDate.Date)
                contents += $"{session.EndDate.Value.Date.ToLongDateString()} ";
            if (session.SessionEndTime != null) contents += $"{session.SessionEndTime.Value.ToString()}";
            contents += "\n";

            contents += $"{session.Equipment}\n";
            contents += $"{session.Microphone}\n";
            contents += $"{session.Weather}\n";
            if (session.Sunset != null) contents += $"Sunset:- {session.Sunset.Value.ToString()}\n";

            if (session.hasGPSLocation)
            {
                contents +=
                    $"{session.LocationGPSLatitude.Value.ToString("G", CultureInfo.InvariantCulture)}, {session.LocationGPSLongitude.Value.ToString("G",CultureInfo.InvariantCulture)}\n";
            }

            contents += session.SessionNotes.Trim() + "\n";

            File.WriteAllText(filename,contents);

        }

        /// <summary>
        /// Calculates the contributions to the frequency table due to this segment
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="FirstBlock"></param>
        /// <param name="OccupiedMinutesPerBlock"></param>
        /// <param name="tableStartTimeInMinutes"></param>
        /// <param name="BlockSize"></param>
        /// <returns></returns>
        public static bool FrequencyContributions(this LabelledSegment segment,out int FirstBlock,out List<int> OccupiedMinutesPerBlock,
            double tableStartTimeInMinutes=12.0d*60.0d, int BlockSize = 10)
        {
            FirstBlock = -1;
            int BlockSizeSeconds = BlockSize * 60;
            int tableStartTimeInSeconds = (int) (tableStartTimeInMinutes * 60.0d);
            OccupiedMinutesPerBlock=new List<int>();

            if ((segment.Duration() ?? new TimeSpan()).TotalSeconds <= 0.0d) return (false);// give up as we don't have a segment Duration

            if (segment.Recording == null) return false; //Give up as we don't have recording to be a parent to this segment

            if (segment.Recording.RecordingStartTime == null) return false; //Give up as the recording for the segment does not have a start time

            var segmentStartSeconds = (int)((segment.Recording.RecordingStartTime.Value + segment.StartOffset).TotalSeconds) -
                               tableStartTimeInSeconds; // time from the start of the table to the start of the segment in seconds
            while (segmentStartSeconds > (24 * 60 * 60)) segmentStartSeconds-=(24 * 60 * 60);
            while (segmentStartSeconds < 0) segmentStartSeconds += (24 * 60 * 60);

            FirstBlock = ((int) (segmentStartSeconds)) / BlockSizeSeconds; 

            int segStart = segmentStartSeconds;
            
            var segEnd = segStart + (int)(segment.Duration().Value.TotalSeconds);
            if (segEnd > (24 * 60 * 60)) segEnd = (24 * 60 * 60); // truncate the segment to the end of table

            Debug.WriteLine($"\nFrom {segStart} to {segEnd}");

            int thisBlockStartSeconds = FirstBlock * BlockSizeSeconds;
            int thisBlockEndSeconds = thisBlockStartSeconds + BlockSizeSeconds;
            while(segStart < segEnd)
            {
                Debug.WriteLine($"blockstart={thisBlockStartSeconds}, segStart={segStart}, segEnd={segEnd}");
                if (thisBlockStartSeconds > segStart) break;
                int diff =  thisBlockEndSeconds-segStart;// ie segstart to the end of the block
                Debug.WriteLine($"diff = {diff}");
                if (thisBlockEndSeconds >= segEnd)
                {
                    diff -= thisBlockEndSeconds - segEnd;// minus the end of the segment to the end of the block
                }

                int wholeMinutes = diff / 60;
                int surplusSeconds = diff % 60;
                diff = wholeMinutes + (surplusSeconds > 0 ? 1 : 0);
                
                if (diff <= 0) diff = 1;
                Debug.WriteLine($"Corrected diff={diff}");
                OccupiedMinutesPerBlock.Add(diff);
                Debug.WriteLine("Added to OccupiedMinutes\n");
                thisBlockStartSeconds = thisBlockEndSeconds;
                thisBlockEndSeconds += BlockSizeSeconds;
                segStart = thisBlockStartSeconds;
                Debug.WriteLine($"new segStart={segStart}");
            }

            return (true);
        }


        /// <summary>
        ///     Returns the duration of the segment as a TimeSpan? or null if either of the start
        ///     or end offsets are null
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static TimeSpan? Duration(this LabelledSegment segment)
        {
            TimeSpan? result = null;
            if (segment != null) result = segment.EndOffset - segment.StartOffset;


            return result;
        }

       

        /// <summary>
        ///     Given a labelled segment, adds it to the recording in the database
        /// </summary>
        /// <param name="result"></param>
        /// <param name="dc"></param>
        public static void AddLabelledSegment(this Recording recording, LabelledSegment result,
            BatReferenceDBLinqDataContext dc)
        {
            recording.LabelledSegments.Add(result);
            dc.SubmitChanges();
        }

        /// <summary>
        ///     Imports a .wav file with embedded GUANO data from analysing with Kaleidoscope.
        ///     Does nothing if there is no GUANO/WAMD data but otherwise looks for an existing
        ///     matching recording entry for the file and updates it if so, or creates a new single segment
        ///     recording if not.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="file"></param>
        public static void ImportWavFile(this RecordingSession session, string file)
        {
            //string note = guano.note;
            //if (note!=null)
            //{
            //    note = note.Replace("\n", " ");
            //    note = note.Replace(@"\n", " ");
            //}
            if (!File.Exists(file) || (new FileInfo(file).Length<=0L)) return;
            var bareFilename = Tools.StripPath(file);
            
            var existingRecording = DBAccess.GetRecordingForWavFile(file);

            var fileMetaData = new WavFileMetaData(file);

            var recordingDate = File.GetCreationTime(file).Date;
            var recordingTime = File.GetCreationTime(file).TimeOfDay;
            if (existingRecording == null)
                existingRecording = CreateRecording(file, recordingDate, recordingTime,
                    fileMetaData.m_Duration ?? TimeSpan.FromSeconds(10),
                    fileMetaData.m_Location != null
                        ? new Tuple<double, double>(fileMetaData.m_Location.m_Latitude,
                            fileMetaData.m_Location.m_Longitude)
                        : null,
                    fileMetaData.FormattedText());
            existingRecording.RecordingSessionId = session.Id;
            var listOfSegmentAndBatList = new BulkObservableCollection<SegmentAndBatList>();
            var segmentAndBatList = new SegmentAndBatList();
            var segment = new LabelledSegment
            {
                StartOffset = new TimeSpan(0L), EndOffset = fileMetaData.m_Duration ?? new TimeSpan(0L)
            };

            var moddedIdentification = "";
            if (!string.IsNullOrWhiteSpace(fileMetaData.m_ManualID))
                moddedIdentification = fileMetaData.m_ManualID.Trim();
            if (string.IsNullOrWhiteSpace(fileMetaData.m_ManualID) && !string.IsNullOrWhiteSpace(fileMetaData.m_AutoID))
                // we have an auto id but no manual id
                moddedIdentification = fileMetaData.m_AutoID.Trim() + " (Auto)";
            if (!string.IsNullOrWhiteSpace(fileMetaData.m_ManualID) &&
                !string.IsNullOrWhiteSpace(fileMetaData.m_AutoID))
                // we have both auto and manual ID fields
                moddedIdentification = fileMetaData.m_ManualID.Trim() + " (Auto=" + fileMetaData.m_AutoID.Trim() + ")";
            segmentAndBatList.BatList =
                DBAccess.GetDescribedBats(moddedIdentification.Trim(), out moddedIdentification);
            segment.Comment = moddedIdentification;
            if (string.IsNullOrWhiteSpace(segment.Comment)) segment.Comment = "";
            var note = fileMetaData.m_Note;
            if (!string.IsNullOrWhiteSpace(note))
            {
                note = note.Replace(@"\n", "");
                segment.Comment = segment.Comment + ": " + note.Trim();
            }

            segmentAndBatList.Segment = segment;

            listOfSegmentAndBatList.Add(segmentAndBatList);

            DBAccess.UpdateRecording(existingRecording, listOfSegmentAndBatList, null);
        }

        public static Recording CreateRecording(string file, DateTime date, TimeSpan startTime, TimeSpan duration,
            Tuple<double, double> location, string notes)
        {
            var result = new Recording
            {
                Id = -1,
                RecordingDate = date,
                RecordingStartTime = startTime,
                RecordingEndTime = startTime + duration
            };
            if (location != null && location.Item1 < 200.0 && location.Item2 < 200.0)
            {
                result.RecordingGPSLatitude = location.Item1.ToString();
                result.RecordingGPSLongitude = location.Item2.ToString();
            }

            result.RecordingNotes = notes;
            result.RecordingName = Tools.StripPath(file);


            return result;
        }

        /// <summary>
        /// Returns the timespan as a string in the format [-]HH:MM
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns></returns>
        public static string ToHMString(this TimeSpan timespan)
        {
            string result = "";
            if (timespan.Ticks < 0) result = "-";
            result = result + timespan.Duration().ToString(@"hh\:mm");
            return result;
        }
    }

    //&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

    public partial class LabelledSegment
    {
        private TimeSpan _startTime=new TimeSpan();
        private TimeSpan _endTime=new TimeSpan();
        

        public TimeSpan startTime
        {
            get
            {
                if (_startTime.Ticks > 0L) return (_startTime);
                if (Recording != null && Recording.RecordingStartTime != null)
                {
                    TimeSpan result = Recording.RecordingStartTime.Value + StartOffset;
                    _startTime = result;
                    return (result);
                }

                return (TimeSpan.FromMinutes(-1.0d));
            }
        }

        public TimeSpan endTime
        {
            get
            {
                if (_endTime.Ticks >0L) return (_endTime);
                if (Recording != null && Recording.RecordingStartTime != null)
                {
                    TimeSpan result = Recording.RecordingStartTime.Value + EndOffset;
                    _endTime = result;
                    return (result);
                }

                return (TimeSpan.FromMinutes(-1.0d));
            }
        }

        public TimeSpan StartTime(int refTimeMinutesSinceMidnight)
        {
            if (startTime.Ticks <= 0) return (startTime); // we dont have a valid start time so give up

            if (startTime.TotalMinutes < refTimeMinutesSinceMidnight)
                return (startTime + new TimeSpan(1, 0, 0, 0) - TimeSpan.FromMinutes(refTimeMinutesSinceMidnight));
            return (startTime - TimeSpan.FromMinutes(refTimeMinutesSinceMidnight));
        }

        public TimeSpan EndTime(int refTimeMinutesSinceMidnight)
        {
            if (endTime.Ticks <= 0) return (endTime); // we dont have a valid start time so give up

            if (endTime.TotalMinutes < refTimeMinutesSinceMidnight)
                return (endTime + new TimeSpan(1, 0, 0, 0) - TimeSpan.FromMinutes(refTimeMinutesSinceMidnight));
            return (endTime - TimeSpan.FromMinutes(refTimeMinutesSinceMidnight));
        }
    }

    public partial class Recording
    {
        private TimeSpan? _sunset = null;
        private TimeSpan? _startAfterSunset = null;
        private TimeSpan? _endAfterSunset = null;
        /// <summary>
        /// If the parent recording session has a sunset time or has a location, then this will return a timespan that is the time
        /// after sunset on the date of the recording.  If the parent has a location but no sunset then sunset will be calculated
        /// from the parent start date and the location.  The calculated time will be stored and returned in future to save doing
        /// multiple calculations and checks. If the parent has no location, but the recordingdoes, then sunset will be calculated
        /// for that location instead. If the time after sunset cannot be determined then the getter will return the normal start of
        /// recording.
        /// </summary>
        public TimeSpan? startTimeAfterSunset
        {
            get
            {
                
                if (_startAfterSunset != null) return _startAfterSunset.Value;
                if (RecordingStartTime == null)
                {
                    if (RecordingSession != null)
                    {
                        RecordingStartTime = RecordingSession.SessionStartTime;
                    }
                }

                if (RecordingStartTime == null) return (null);

                if (sunset == null)
                {
                    return (RecordingStartTime.Value);
                }
                else
                {
                    if (RecordingStartTime.Value.TotalHours < 12.0d)
                    {
                        // the recording starts after midnight, so add the start time to (midnight-sunset)
                        _startAfterSunset = RecordingStartTime.Value + (new TimeSpan(24, 0, 0) - sunset.Value);
                    }
                    else
                    {
                        _startAfterSunset = RecordingStartTime.Value - sunset.Value;
                    }

                    
                    
                    
                }

                return _startAfterSunset ?? RecordingStartTime;
            }
            
        }

        public TimeSpan? endAfterSunset
        {
            get
            {
                if (_endAfterSunset != null) return _endAfterSunset.Value;
                if (RecordingEndTime == null) return (null);

                if (sunset == null)
                {
                    return (RecordingEndTime.Value);
                }
                else
                {

                    if (RecordingEndTime.Value.TotalHours < 12.0d)
                    {
                        _endAfterSunset = RecordingEndTime.Value + (new TimeSpan(24, 0, 0) - sunset.Value);
                    }
                    else
                    {
                        _endAfterSunset = (RecordingEndTime.Value - sunset.Value);
                    }

                }

                return _endAfterSunset ?? RecordingEndTime;
            }
        }

        public TimeSpan? sunset
        {
            get
            {
                if (_sunset == null)
                {
                    if (RecordingSession != null)
                    {
                        if (RecordingSession.Sunset != null && RecordingSession.Sunset.Value.TotalMinutes>0.0d)
                        {
                            _sunset = RecordingSession.Sunset;
                        }
                        else if (RecordingSession.hasGPSLocation)
                        {
                            _sunset = SessionManager.CalculateSunset(RecordingSession.SessionDate.Date,
                                RecordingSession.LocationGPSLatitude, RecordingSession.LocationGPSLongitude);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(RecordingGPSLatitude) &&
                                !string.IsNullOrWhiteSpace(RecordingGPSLongitude))
                            {
                                if (decimal.TryParse(RecordingGPSLatitude, out decimal latit) &&
                                    decimal.TryParse(RecordingGPSLongitude, out decimal longit))
                                {
                                    _sunset = SessionManager.CalculateSunset(RecordingSession.SessionDate.Date, latit,
                                        longit);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(RecordingGPSLatitude) &&
                            !string.IsNullOrWhiteSpace(RecordingGPSLongitude))
                        {
                            if (decimal.TryParse(RecordingGPSLatitude, out decimal latit) &&
                                decimal.TryParse(RecordingGPSLongitude, out decimal longit) && RecordingDate != null)
                            {
                                _sunset = SessionManager.CalculateSunset(RecordingDate.Value.Date, latit,
                                    longit);
                            }
                        }
                        else
                        {
                            _sunset = SessionManager.CalculateSunset(RecordingDate.Value.Date, 51.9178783m,
                                -1.1448518m);
                        }
                    }

                }

                return (_sunset);
            }
        }
    }

    //##################################################################################################################################

    public partial class RecordingSession
    {
        private bool? _hasGPSLocation = null;

        /// <summary>
        /// cached boolean returns true if the GPS co-ordinates are non-null
        /// in the range 90 to -90 and 180 to -180 and they are not both
        /// less than .0001 (i.e.  GPS of 0,0 is considered invalid)
        /// </summary>
        public bool hasGPSLocation
        {
            get
            {
                //if (_hasGPSLocation != null) return _hasGPSLocation.Value;
                //else
                //{
                    if (LocationGPSLatitude == null || LocationGPSLongitude == null ||
                        (LocationGPSLatitude <= 0.0001m && LocationGPSLongitude <= 0.0001m) ||
                        LocationGPSLatitude > 90.0m || LocationGPSLatitude < -90.0m || LocationGPSLongitude < -180.0m ||
                        LocationGPSLongitude > 180.0m)
                    {
                        _hasGPSLocation = false;
                    }
                    else
                    {
                        _hasGPSLocation = true;
                    }
                //}

                return (_hasGPSLocation.Value);
            }

            set { _hasGPSLocation = null; }
        }
    }
}