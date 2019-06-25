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
using System.IO;
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
            if (!File.Exists(filename)) return null;


            return filename;
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
            if (!File.Exists(file)) return;
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
    }
}