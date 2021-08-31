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

using F23.StringSimilarity;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatRecordingManager
{
    /// <summary>
    /// </summary>
    public static class SessionManager
    {
        public static TimeSpan? CalculateSunset(DateTime sessionDate, decimal? latitude, decimal? longitude)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            TimeSpan? sunset = new TimeSpan();
            var dtSunrise = new DateTime();
            var dtSunset = new DateTime();
            var isSunrise = false;
            var isSunset = false;

            if (latitude == null || longitude == null || Math.Abs((double)latitude) > 90.0 ||
                Math.Abs((double)longitude) > 180.0 || sessionDate.Year < 1900) return sunset;

            if (SunTimes.Instance.CalculateSunRiseSetTimes((double)latitude.Value, (double)longitude.Value,
                sessionDate, ref dtSunrise, ref dtSunset, ref isSunrise, ref isSunset))
            {
                if (isSunset)
                    /*
                        if(dtSunset.IsDaylightSavingTime()){
                            dtSunset = dtSunset.AddHours(1);
                        }*/
                    sunset = dtSunset.TimeOfDay;
            }
            else
            {
                Tools.ErrorLog("Failed to calculate Sunset");
            }

            return sunset;
        }

        /// <summary>
        ///     Presents a RecordingSessionDialog to the user for filling in or amending and returns
        ///     the amended session.
        /// </summary>
        /// <param name="newSession"></param>
        /// <param name="sessionTag"></param>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static RecordingSession EditSession(RecordingSession newSession, string sessionTag, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(newSession.SessionTag)) newSession.SessionTag = sessionTag;
            if (newSession.SessionDate == null || newSession.SessionDate.Year < 1900)
            {
                newSession.SessionDate = GetDateFromTag(sessionTag);
                if (newSession.SessionDate == null || newSession.SessionDate.Year < 1900)
                {
                    newSession.SessionDate = GetDateFromFileOrFolder(newSession, folderPath);
                }
                newSession.SessionStartTime = new TimeSpan(18, 0, 0);
            }

            if (newSession.EndDate == null || newSession.EndDate.Value.Year < 1900)
            {
                newSession.EndDate = newSession.SessionDate;
                newSession.SessionEndTime = new TimeSpan(23, 59, 59);
            }

            if (string.IsNullOrWhiteSpace(newSession.OriginalFilePath)) newSession.OriginalFilePath = folderPath;
            if (GetDateTimesFromFiles(folderPath, sessionTag, out var start, out var end)) // Gets it wrong
            {
                if (start.Date == newSession.SessionDate.Date) // simple, everything agrees
                {
                    newSession.SessionDate = start;
                    newSession.SessionStartTime = start.TimeOfDay;
                    newSession.EndDate = end;
                    newSession.SessionEndTime = end.TimeOfDay;
                }
                else // we have discrepancy between tag and file dates and times, we use a composite
                {
                    newSession.SessionStartTime = start.TimeOfDay;
                    newSession.EndDate = end;
                    newSession.SessionEndTime = end.TimeOfDay;
                }
            }

            var sessionForm = new RecordingSessionForm();

            sessionForm.SetRecordingSession(newSession);
            if (sessionForm.ShowDialog() ?? false)
            {
                newSession = sessionForm.GetRecordingSession();
                //DBAccess.UpdateRecordingSession(sessionForFolder);
                sessionTag = newSession.SessionTag;
                var existingSession = DBAccess.GetRecordingSession(sessionTag);
                newSession.Id = existingSession != null ? existingSession.Id : 0;
            }
            else
            {
                newSession = null; // we hit Cancel in the form so nullify the entire process
            }

            return newSession;
        }

        /// <summary>
        ///     Uses the header file to try and populate a new recordingSession,
        ///     otherwise returns a new RecordingSession;
        /// </summary>
        /// <param name="headerFile"></param>
        /// <returns></returns>
        public static RecordingSession FillSessionFromHeader(string headerFile, string sessionTag,
            BulkObservableCollection<string> wavFileFolders = null)
        {
            var recordingSession = new RecordingSession { SessionTag = sessionTag };
            //if (!string.IsNullOrWhiteSpace(headerFile) && File.Exists(headerFile))
            recordingSession = PopulateSession(recordingSession, headerFile, sessionTag, wavFileFolders);

            return recordingSession;
        }

        /// <summary>
        ///     Looks for a header text file in the selected folder which starts with a [COPY]
        ///     directive.
        /// </summary>
        /// <returns></returns>
        public static string GetHeaderFile(string folderPath)
        {
            var listOfTxtFiles = Directory.EnumerateFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);
            //var listOfTXTFiles= Directory.EnumerateFiles(folderPath, "*.TXT", SearchOption.TopDirectoryOnly);
            //listOfTxtFiles = listOfTxtFiles.Concat<string>(listOfTXTFiles);
            if (!listOfTxtFiles.IsNullOrEmpty())
                foreach (var file in listOfTxtFiles)
                    if (File.Exists(file))
                    {
                        var lines = File.ReadLines(file);
                        if (!lines.IsNullOrEmpty())
                        {
                            var line = lines.First();
                            if (line.Contains("[COPY]")) return file;
                        }
                    }

            return null;
        }

        /// <summary>
        ///     Saves the recordingSession to the database
        /// </summary>
        /// <param name="newSession"></param>
        /// <returns></returns>
        public static RecordingSession SaveSession(RecordingSession newSession)
        {
            DBAccess.UpdateRecordingSession(newSession);
            DBAccess.ResolveOrphanImages();

            return DBAccess.GetRecordingSession(newSession.SessionTag);
        }

        /// <summary>
        ///     Tries to find a header file and if found tries to populate a new RecordingSession
        ///     based on it.  Whether or no, then displays a RecordingSession Dialog for the user to
        ///     populate and/or amend as required.  The RecordingSession is then saved to the
        ///     database and the RecordingSessiontag is used to replace the current Sessiontag
        /// </summary>
        /// <returns></returns>
        internal static RecordingSession CreateSession(string folderPath, string sessionTag = "", GpxHandler gpxHandler = null)
        {
            var newSession = new RecordingSession();
            string headerFile = "";
            if (gpxHandler == null) gpxHandler = new GpxHandler(folderPath);
            var folderList = new BulkObservableCollection<string> { folderPath };
            if (!string.IsNullOrWhiteSpace(sessionTag))
            {
                var sess = DBAccess.GetRecordingSession(sessionTag);
                if (sess != null) newSession = sess;
            }
            if (newSession.Id <= 0)
            {
                if (string.IsNullOrWhiteSpace(sessionTag))
                {
                    sessionTag = CreateTag(folderPath);
                }

                headerFile = GetHeaderFile(folderPath);
                newSession = FillSessionFromHeader(headerFile, sessionTag, folderList);
                newSession = SetGpsCoordinates(newSession, gpxHandler);
            }

            newSession.OriginalFilePath = folderPath;

            newSession = EditSession(newSession, sessionTag, folderPath);
            if (newSession == null) return null;
            newSession = SaveSession(newSession);
            sessionTag = newSession.SessionTag;

            return newSession;
        }

        internal static string GetSessionTag(FileBrowser fileBrowser)
        {
            string FolderPath = fileBrowser.WorkingFolder;
            
            
            if (!string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath))
            {
                FolderPath = Path.GetDirectoryName(FolderPath);
                if(FolderPath.EndsWith("\\")) FolderPath=FolderPath.Substring(0,FolderPath.Length - 1);

                
                string parentFolder = FolderPath.Substring(FolderPath.LastIndexOf('\\') + 1);
                string tag = GetLeadingTag(parentFolder);
                if (!string.IsNullOrWhiteSpace(tag)) return (tag);

                var TxtFileList = Directory.EnumerateFiles(FolderPath,"*.txt");
                if (!TxtFileList.IsNullOrEmpty())
                {
                    foreach(var file in TxtFileList)
                    {
                        var fileName = Path.GetFileName(file);
                        if (!string.IsNullOrWhiteSpace(fileName))
                        {
                            tag=GetLeadingTag(fileName);
                            if(!string.IsNullOrWhiteSpace(tag)) return (tag);
                        }
                    }
                }

                var WavFileList = Directory.EnumerateFiles(FolderPath, "*.wav");
                if (WavFileList.IsNullOrEmpty())
                {
                    WavFileList = Directory.EnumerateFiles(FolderPath, "*.zc");
                    if (WavFileList.IsNullOrEmpty()) return ("");
                }

                foreach (var file in WavFileList)
                {
                    var fileName = Path.GetFileName(file);
                    tag = GetLeadingTag(fileName);
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        return (tag);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(fileBrowser.WorkingFolder))
            {
                string parentFolder = Path.GetDirectoryName(FolderPath);
                string tag = GetLeadingTag(parentFolder);
                if (!string.IsNullOrWhiteSpace(tag)) return (tag);
            }



            return "";
        }

        /// <summary>
        /// Return a valid sessionTag from the data supplied.
        /// 1) if a valid tag is supplied, return it.
        /// 2) if the folder name starts with a valid tag, return it
        /// 3) if any line in the header file starts with a tag, return it
        /// 4) if any .WAV file in the working folder starts with a valid tag, return it.
        /// 5) if any .TXT file in the working folder starts with a valid tag, return it
        /// 6) return an empty string
        /// </summary>
        /// <param name="newSession"></param>
        /// <param name="sessionTag"></param>
        /// <param name="workingFolder"></param>
        /// <param name="headerFileLines"></param>
        /// <returns></returns>
        private static string GetSessionTagFromheader(RecordingSession newSession, string sessionTag, string workingFolder, ref string[] headerFileLines)
        {
            string tag = GetLeadingTag(sessionTag);
            if (!string.IsNullOrWhiteSpace(tag)) return (tag);

            string parentFolder = Path.GetDirectoryName(workingFolder);
            tag = GetLeadingTag(parentFolder);
            if (!string.IsNullOrEmpty(tag)) return (tag);

            if (headerFileLines != null && headerFileLines.Any())
            {
                for (int i = 0; i < headerFileLines.Length; i++)
                {
                    tag = GetLeadingTag(headerFileLines[i]);
                    if (!string.IsNullOrEmpty(tag))
                    {
                        return (tag);
                    }
                }
            }

            var wavFiles = Directory.GetFiles(workingFolder, "*.wav");
            if (wavFiles != null)
            {
                foreach (var file in wavFiles)
                {
                    tag = GetLeadingTag(Path.GetFileName(file));
                    if (!string.IsNullOrEmpty(tag)) return (tag);
                }
            }

            var txtFiles = Directory.GetFiles(workingFolder, "*.txt");
            if (txtFiles != null)
            {
                foreach (var file in txtFiles)
                {
                    tag = GetLeadingTag(Path.GetFileName(file));
                    if (!string.IsNullOrEmpty(tag)) return (tag);
                }
            }
            return (null);
        }

        /// <summary>
        /// Uses a Regex to see if the provided string starts with a valid sessionTag and if so returns the tag.
        /// A tag should comprise:-
        /// string in caps and 2 digits = e.g. AAA21
        /// separator, underline or dash
        /// 1 or more digits followed by 0 or more letters
        /// 1 or more separators, underline or dash
        /// a date in the format YYYYMMDD, or YYYY-MM-DD 
        /// 
        /// If not found returns an empty string
        /// </summary>
        /// <param name="sessionTag"></param>
        /// <returns></returns>
        private static string GetLeadingTag(string stringToMatch)
        {
            string tag = "";
            string pattern = @"^([A-Z]{2,5}[0-9]{2}[-_]+[0-9]+[a-zA-Z]*[0-9]{0,2}[-_]+)";
            // matches leading  nnn20-1am3_
            var result = Regex.Match(stringToMatch, pattern);
            if (result.Success)
            {
                if (result.Groups != null && result.Groups.Count > 0)
                    tag = result.Groups[1].Value;
                else tag = result.Value;
                
            }

            //matches trailing yy-mm-dd or yyyy-mm-dd with or without seperators
            pattern = @"(([12][0-9])?[0-9]{2}[-_\s]?[0-9]{2}[-_\s]?[0-9]{2} )\.";
            result = Regex.Match(stringToMatch, pattern,RegexOptions.IgnorePatternWhitespace);
            if (result.Success)
            {
                if (result.Groups != null && result.Groups.Count > 0)
                {
                    tag = tag + result.Groups[1].Value;
                }
                else tag += result.Value;
            }
            return (tag);
        }

        /// <summary>
        /// TODO this needs rewriting - the session end time is not reported correctly and the code does not make it clear
        /// where data is taken from or with what priority.
        /// </summary>
        /// <param name="newSession">An instance of a session to receive the parameters</param>
        /// <param name="headerFile">fully qualified name of the header file</param>
        /// <param name="sessionTag">The tag to be used for the session</param>
        /// <param name="wavFileFolders">Collection of file paths containg wav files</param>
        /// <returns></returns>
        internal static RecordingSession PopulateSession2(RecordingSession newSession, string headerFile,
                    string sessionTag, BulkObservableCollection<string> wavFileFolders)
        {
            bool noHeader = false;
            string workingFolder = "";
            string[] headerFileLines = null;

            if (newSession == null) newSession = new RecordingSession();

            var existingSession = DBAccess.GetRecordingSession(newSession.SessionTag);
            if (existingSession != null) return existingSession; // short circuit the process if session exists in the database



            if (string.IsNullOrWhiteSpace(headerFile) || !File.Exists(headerFile))
            {
                noHeader = true;
            }
            else
            {
                headerFileLines = File.ReadAllLines(headerFile);
            }

            if (wavFileFolders.IsNullOrEmpty())

            {
                if (!string.IsNullOrWhiteSpace(headerFile))
                {
                    workingFolder = Path.GetDirectoryName(headerFile);
                    wavFileFolders = new BulkObservableCollection<string>();
                    wavFileFolders.Add(workingFolder);
                    newSession.OriginalFilePath = workingFolder;
                }
                else
                {
                    return (newSession);
                }// nothing to do if no folders to process
            }
            else
            {
                workingFolder = Path.GetDirectoryName(wavFileFolders.First());
                newSession.OriginalFilePath = workingFolder;
            }
            existingSession = DBAccess.GetRecordingSessionByFolder(workingFolder);
            if (existingSession != null)
            {
                return (existingSession); // another short circuit - should not have multiple sessions using the same data folder
            }

            newSession = ExtractHeaderData(workingFolder, sessionTag, headerFileLines);


            newSession.SessionTag = GetSessionTagFromheader(newSession, sessionTag, workingFolder, ref headerFileLines);
            newSession = GetSessiondateAndTimes(newSession, workingFolder, ref headerFileLines);
            //newSession.Equipment = GetSessionEquipment(newSession, ref headerFileLines);
            //newSession.Microphone = GetSessionMicrophone(newSession, ref headerFileLines);
            //newSession.Operator = GetSessionOperator(newSession, ref headerFileLines);
            //newSession.Location = GetSessionLocation(newSession, workingFolder, ref headerFileLines);
            //newSession.Weather = GetSessionWeather(newSession, workingFolder, ref headerFileLines);
            //newSession.Temp = GetSessionTemp(newSession, ref headerFileLines);


            return (newSession);
        }

        /// <summary>
        /// Given a RecordingSession, path to a folder of .wav files, and a collection of lines from a header file, 
        /// determines the start and end dates and times for the session and writes them to the supplied session
        /// which is then returned.
        /// 1) gets times and dates from the files in the working folder
        /// OR 2) gets tmes and dates from the header file lines
        /// OR 3) gets the date from the session Tag and defaults the times
        /// OR 4) defaults the dates and times
        /// </summary>
        /// <param name="newSession">The Recording Session to be updated with dates and times</param>
        /// <param name="workingFolder">The path to the folder of .wav files</param>
        /// <param name="headerFileLines">The contents of the header file as a collection of lines</param>
        /// <returns></returns>
        private static RecordingSession GetSessiondateAndTimes(RecordingSession newSession, string workingFolder, ref string[] headerFileLines)
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;



            DateTime firstFileDateAndTimeFromName = new DateTime(1, 1, 1);
            DateTime lastFileDateAndTimeFromName = new DateTime(1, 1, 1);
            var wavFileList = Directory.EnumerateFiles(workingFolder, "*.wav").OrderBy(wf => wf);
            if (wavFileList.IsNullOrEmpty())
            {
                wavFileList = Directory.EnumerateFiles(workingFolder, "*.zc").OrderBy(wf => wf);
            }
            if (!wavFileList.IsNullOrEmpty())
            {
                // uses FileProcessor to determine the start times and dates of the first and last files in the list
                //GetFileDuration finds the earliest out of the modified, creation, filename and metadata dates and
                // times to establish the real time when the file was recorded
                var wavfileName = wavFileList.First();

                _ = Tools.GetFileDatesAndTimes(wavfileName, out string explicitwavfilename, out DateTime fileStart, out DateTime fileEnd);
                firstFileDateAndTimeFromName = fileStart;

                wavfileName = wavFileList.Last();

                _ = Tools.GetFileDatesAndTimes(wavfileName, out explicitwavfilename, out fileStart, out fileEnd);
                lastFileDateAndTimeFromName = fileEnd;
                if (lastFileDateAndTimeFromName > firstFileDateAndTimeFromName && firstFileDateAndTimeFromName.Year > 1970)
                {
                    newSession.SessionDate = firstFileDateAndTimeFromName;
                    newSession.SessionStartTime = newSession.SessionDate.TimeOfDay;
                    newSession.EndDate = lastFileDateAndTimeFromName;
                    newSession.SessionEndTime = newSession.EndDate?.TimeOfDay;
                    return (newSession);
                }
            }

            // Couldn't get a reasonable date from the files, so try looking in the header file
            return (newSession);
        }

       

        /// <summary>
        /// TODO this needs rewriting - the session end time is not reported correctly and the code does not make it clear
        /// where data is taken from or with what priority.
        /// </summary>
        /// <param name="newSession">An instance of a session to receive the parameters</param>
        /// <param name="headerFile">fully qualified name of the header file</param>
        /// <param name="sessionTag">The tag to be used for the session</param>
        /// <param name="wavFileFolders">Collection of file paths containg wav files</param>
        /// <returns></returns>
        internal static RecordingSession PopulateSession(RecordingSession newSession, string headerFile,
                    string sessionTag, BulkObservableCollection<string> wavFileFolders)
        {
            newSession.SessionTag = sessionTag;
            bool noHeader = false;
            string workingFolder = "";
            string[] headerFileLines = null;

            if (string.IsNullOrWhiteSpace(headerFile) || !File.Exists(headerFile))
            {
                noHeader = true;
            }
            else
            {
                headerFileLines = File.ReadAllLines(headerFile);
            }

            if (wavFileFolders.IsNullOrEmpty())

            {
                if (!string.IsNullOrWhiteSpace(headerFile))
                {
                    workingFolder = Path.GetDirectoryName(headerFile);
                    wavFileFolders = new BulkObservableCollection<string>();
                    wavFileFolders.Add(workingFolder);
                    newSession.OriginalFilePath = workingFolder;
                }
                else
                {
                    return (newSession);
                }// nothing to do if no folders to process
            }
            else
            {
                workingFolder = Path.GetDirectoryName(wavFileFolders.First());
                newSession.OriginalFilePath = workingFolder;
            }

            DateTime firstFileDateAndTimeFromName = new DateTime(1, 1, 1);
            DateTime lastFileDateAndTimeFromName = new DateTime(1, 1, 1);
            var wavFileList = Directory.EnumerateFiles(workingFolder, "*.wav").OrderBy(wf => wf);
            if (wavFileList.IsNullOrEmpty())
            {
                wavFileList = Directory.EnumerateFiles(workingFolder, "*.zc").OrderBy(wf => wf);
            }
            if (!wavFileList.IsNullOrEmpty())
            {
                // uses FileProcessor to determine the start times and dates of the first and last files in the list
                //GetFileDuration finds the earliest out of the modified, creation, filename and metadata dates and
                // times to establish the real time when the file was recorded
                var wavfileName = wavFileList.First();

                _ = Tools.GetFileDatesAndTimes(wavfileName, out string explicitwavfilename, out DateTime fileStart, out DateTime fileEnd);
                firstFileDateAndTimeFromName = fileStart;

                wavfileName = wavFileList.Last();

                TimeSpan duration = Tools.GetFileDatesAndTimes(wavfileName, out explicitwavfilename, out fileStart, out fileEnd);
                lastFileDateAndTimeFromName = fileEnd;
            }

            var existingSession = DBAccess.GetRecordingSession(newSession.SessionTag);
            if (existingSession != null) return existingSession;

            if (!noHeader && headerFileLines != null && headerFileLines.Length > 0)
            {
                newSession = ExtractHeaderData(workingFolder, newSession.SessionTag, headerFileLines);
                if (newSession.SessionDate.Year < 1950)
                {
                    var dateRegex = @".*[-0-9]*(20[0-9]{6})[-0-9]*.*";
                    var folder = workingFolder;

                    var match = Regex.Match(folder, dateRegex);
                    if (match.Success)
                    {
                        newSession.SessionDate = GetCompressedDate(match.Groups[1].Value);
                        newSession.EndDate = newSession.SessionDate;
                    }
                    else
                    {
                        if (!wavFileFolders.IsNullOrEmpty())
                            foreach (var wavfolder in wavFileFolders)
                            {
                                match = Regex.Match(wavfolder, dateRegex);
                                if (match.Success)
                                {
                                    newSession.SessionDate = GetCompressedDate(match.Groups[1].Value);
                                    newSession.EndDate = newSession.SessionDate;
                                    break;
                                }
                            }

                        if (newSession.SessionDate.Year < 1950)
                            if (Directory.Exists(workingFolder))
                            {
                                newSession.SessionDate = Directory.GetCreationTime(workingFolder);
                                newSession.EndDate = newSession.SessionDate;
                            }
                    }
                }

                if (firstFileDateAndTimeFromName.Year > 1)
                {
                    newSession.SessionDate = firstFileDateAndTimeFromName;
                    newSession.SessionStartTime = firstFileDateAndTimeFromName.TimeOfDay;
                }

                if (newSession.EndDate < newSession.SessionDate)
                {
                    newSession.EndDate = newSession.SessionDate;
                    newSession.SessionEndTime = newSession.SessionStartTime + new TimeSpan(2, 0, 0);
                }

                if (lastFileDateAndTimeFromName.Year > 1)
                {
                    newSession.EndDate = lastFileDateAndTimeFromName;
                    newSession.SessionEndTime = lastFileDateAndTimeFromName.TimeOfDay;
                }
            }
            else
            {
                // we can't get a header file so we need to fill in some fundamental defaults
                // for the blank session.

                if (firstFileDateAndTimeFromName.Year > 1 && lastFileDateAndTimeFromName.Year > 1)
                {
                    newSession.SessionDate = firstFileDateAndTimeFromName;
                    newSession.SessionStartTime = firstFileDateAndTimeFromName.TimeOfDay;
                    newSession.EndDate = lastFileDateAndTimeFromName;
                    newSession.SessionEndTime = lastFileDateAndTimeFromName.TimeOfDay;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(workingFolder) && Directory.Exists(workingFolder))
                    {
                        newSession.SessionDate = Directory.GetCreationTime(workingFolder);
                        newSession.EndDate = newSession.SessionDate;
                    }

                    newSession.SessionStartTime = new TimeSpan(18, 0, 0);
                    newSession.SessionEndTime = new TimeSpan(23, 0, 0);
                }
            }

            return newSession;
        }

        /// <summary>
        ///     Populates the session.
        /// </summary>
        /// <param name="newSession">
        ///     The new session.
        /// </param>
        /// <param name="fileBrowser">
        ///     The file browser.
        /// </param>
        /// <returns>
        /// </returns>
        internal static RecordingSession PopulateSession(RecordingSession newSession, FileBrowser fileBrowser)
        {
            var sessionTag = GetSessionTag(fileBrowser);
            var headerFile = fileBrowser.HeaderFileName;

            return PopulateSession(newSession, headerFile, sessionTag, fileBrowser.WavFileFolders);
        }

        /// <summary>
        ///     Uses the supplied gpxhandler to fill in the GPX co-ordinates for the supplied session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="gpxHandler"></param>
        /// <returns></returns>
        internal static RecordingSession SetGpsCoordinates(RecordingSession session, GpxHandler gpxHandler)
        {
            if (session.LocationGPSLatitude == null || session.LocationGPSLatitude < 5.0m)
            {
                var gpxLoc = gpxHandler?.GetLocation(session.SessionDate);
                if (gpxLoc != null && gpxLoc.Count == 2)
                {
                    session.LocationGPSLatitude = gpxLoc[0];
                    session.LocationGPSLongitude = gpxLoc[1];
                }
            }

            return session;
        }

        private static string CreateTag(string folderPath)
        {
            var result = Guid.NewGuid().ToString();
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                var path = Tools.GetPath(folderPath);
                if (path.Trim().EndsWith(@"\")) path = path.Substring(0, path.Length - 1);
                if (path.Contains(@"\"))
                {
                    path = path.Substring(path.LastIndexOf('\\'));
                    if (path.StartsWith("\\")) path = path.Substring(1);
                }

                var i = 0;
                result = path;
                while (DBAccess.SessionTagExists(path)) path = result + "-" + i++;
                result = path;
            }

            return result;
        }

        /// <summary>
        ///     Extracts the header data. Makes a best guess attempt to populate a RecordingSession
        ///     instance from a header file.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private static RecordingSession ExtractHeaderData(string folder, string sessionTag, string[] headerFile)
        {
            var wavFile = "";
            if (Directory.Exists(folder))
            {
                var wavFiles = Directory.EnumerateFiles(folder, "*.wav");
                //var WAVFiles= Directory.EnumerateFiles(folder, "*.WAV");
                //wavFiles = wavFiles.Concat<string>(WAVFiles);

                if (wavFiles != null && wavFiles.Any()) wavFile = wavFiles.First();
            }

            WavFileMetaData wfmd = null;

            if (!string.IsNullOrWhiteSpace(wavFile))
                if (File.Exists(wavFile) && (new FileInfo(wavFile).Length > 0L))
                    wfmd = new WavFileMetaData(wavFile);

            var session = new RecordingSession { SessionTag = sessionTag };
            var sunset = new TimeSpan();
            var startDateTime = new DateTime();
            var endDateTime = new DateTime();
            GetTimes(folder, sessionTag, headerFile, wfmd, out startDateTime, out endDateTime, out sunset);
            session.SessionStartTime = startDateTime.TimeOfDay;
            session.SessionEndTime = endDateTime.TimeOfDay;
            session.SessionDate = startDateTime;
            session.EndDate = endDateTime;
            session.Sunset = sunset;
            session.Temp = GetTemp(headerFile, wfmd);
            session.Equipment = GetEquipment(headerFile, wfmd);
            session.Microphone = GetMicrophone(headerFile, wfmd);
            session.Operator = GetOperator(headerFile);
            session.Location = GetLocation(headerFile);
            if (GetGpsCoOrdinates(headerFile, wfmd, out var latitude, out var longitude))
            {
                session.LocationGPSLongitude = longitude;
                session.LocationGPSLatitude = latitude;
                if (sunset.Hours == 0 && longitude != null && latitude != null)
                    session.Sunset = CalculateSunset(session.SessionDate, latitude, longitude);
            }

            session.SessionNotes = "";

            foreach (var line in headerFile) session.SessionNotes = session.SessionNotes + line + "\n";
            if (wfmd != null) session.SessionNotes += wfmd.FormattedText();
            return session;
        }

        /// <summary>
        ///     Gets the compressed date. Given a date in the format yyyymmdd returns the
        ///     corresponding DateTime
        /// </summary>
        /// <param name="group">
        ///     The group.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private static DateTime GetCompressedDate(string group)
        {
            if (string.IsNullOrWhiteSpace(group)) return new DateTime();
            if (group.Length != 8) return new DateTime();
            int.TryParse(group.Substring(0, 4), out var year);
            int.TryParse(group.Substring(4, 2), out var month);
            int.TryParse(group.Substring(6, 2), out var day);
            if (year < DateTime.Now.Year && month > 0 && month <= 12 && day > 0 && day <= 31)
                return new DateTime(year, month, day);
            return new DateTime();
        }

        /// <summary>
        ///     Gets the date.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <returns>
        /// </returns>
        private static DateTime GetDate(string[] headerFile, string sessionTag)
        {
            if (!string.IsNullOrWhiteSpace(sessionTag)) return GetDateFromTag(sessionTag);
            var result = new DateTime();
            var pattern = @"[0-9]+\s*[a-zA-Z]+\s*(20){0,1}[0-9]{2}";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    DateTime.TryParse(match.Value, out result);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the date from file or folder.  Is guranteed to return a valid datetime.
        /// First tries to get the date and time from a .wav file in the folder.  If that fails
        /// the tries to get the date from the folder (unreliable - probably not the recording date)
        /// and if all alse fails returns a date of 1/1/2000
        /// </summary>
        /// <param name="newSession">The new session.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static DateTime GetDateFromFileOrFolder(RecordingSession newSession, string folderPath)
        {
            DateTime result = new DateTime(1900, 1, 1, 0, 0, 0, 0);
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                if (Directory.Exists((folderPath)))
                {
                    var wavFiles = Directory.EnumerateFiles(folderPath, "*.wav");
                    if (wavFiles != null && wavFiles.Any())
                    {
                        result = File.GetCreationTime(wavFiles.First());
                        if (result != null && result.Year > 1900) return (result);
                    }

                    result = Directory.GetCreationTime(folderPath);
                    if (result != null && result.Year > 1900)
                    {
                        return (result);
                    }
                }
            }

            return (new DateTime(1900, 1, 1, 0, 0, 0, 0));
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Gets the date from tag.
        /// </summary>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <returns>
        /// </returns>
        private static DateTime GetDateFromTag(string sessionTag)
        {
            var tagRegex = new Regex("([a-zA-Z0-9-]+)(_+)([0-9]{4}).?([0-9]{2}).?([0-9]{2})");
            var result = new DateTime();
            var match = tagRegex.Match(sessionTag);
            if (match.Success)
            {
                if (match.Groups.Count == 6)
                {
                    int.TryParse(match.Groups[5].Value, out var day);
                    int.TryParse(match.Groups[4].Value, out var month);
                    int.TryParse(match.Groups[3].Value, out var year);
                    result = new DateTime(year, month, day);
                }
            }

            return result;
        }

        /// <summary>
        ///     uses the times of files in the specified folder to guess at session
        ///     start and end times.  Looks for .wav files with the same date as the
        ///     date included in the tag, then assumes a start at 4 minutes before the
        ///     time of the earlieast wav file and an end of the time of the last wav file.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="sessionTag"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private static bool GetDateTimesFromFiles(string folder, string sessionTag, out DateTime startTime,
            out DateTime endTime)
        {
            startTime = DateTime.Now;
            endTime = startTime;
            if (string.IsNullOrWhiteSpace(folder)) return false;
            var sessiondate = GetDateFromTag(sessionTag);

            try
            {
                var wavFiles = Directory.EnumerateFiles(folder, @"*.wav");
                if (wavFiles.IsNullOrEmpty())
                {
                    wavFiles = Directory.EnumerateFiles(folder, "*.zc");
                }
                //var WAVFiles = Directory.EnumerateFiles(folder, "*.WAV");

                List<string> firstAndLastWavFiles = new List<string>();
                // wavFiles = wavFiles.Concat(WAVFiles);
                if (wavFiles.Count() > 20)
                {
                    firstAndLastWavFiles = wavFiles.Take(10).ToList();

                    firstAndLastWavFiles.AddRange(wavFiles.Skip(wavFiles.Count() - 10));
                }
                else
                {
                    firstAndLastWavFiles = wavFiles.ToList();
                }

                DateTime startDateTime = DateTime.Now;
                DateTime endDateTime = new DateTime();
                foreach (var file in firstAndLastWavFiles)
                {
                    Tools.GetFileDatesAndTimes(file, out string wavfileName, out DateTime fileStart, out DateTime fileEnd);
                    if (fileStart < startDateTime) startDateTime = fileStart;
                    if (fileEnd > endDateTime) endDateTime = fileEnd;
                }

                startTime = startDateTime;
                endTime = endDateTime;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Gets the equipment.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        /// </returns>
        private static string GetEquipment(string[] headerFile, WavFileMetaData wfmd = null)
        {
            if (wfmd?.m_Device != null) return wfmd.m_Device;
            if (headerFile == null || headerFile.Length <= 0) return "";
            var knownEquipment = DBAccess.GetEquipmentList();
            if (knownEquipment == null || knownEquipment.Count <= 0) return "";

            string matchingEquipment = GetBestMatch(knownEquipment, headerFile);

            // get a line in the text containing a known operator
            //var matchingEquipment = headerFile.Where(line =>
            // knownEquipment.Any(txt => line.ToUpper().Contains(txt == null ? "none" : txt.ToUpper())));
            if (!string.IsNullOrWhiteSpace(matchingEquipment)) return matchingEquipment;
            return "";
        }

        /// <summary>
        /// Given two collections of strings, determines the pair with the greatest similarity and returns
        /// the member of the second list from the best matched pair.
        /// </summary>
        /// <param name="referenceList">The list of known items to be matched against</param>
        /// <param name="targetList">The list of unknowns from which to select a best match</param>
        /// <returns></returns>
        private static string GetBestMatch(BulkObservableCollection<string> referenceList, string[] targetList)
        {
            string bestSoFar = "";
            double bestMatchValue = 0;

            foreach (var target in targetList)
            {
                if (!string.IsNullOrWhiteSpace(target))
                {
                    foreach (var reference in referenceList)
                    {
                        if (!string.IsNullOrWhiteSpace(reference))
                        {
                            var matchValue = getSimilarity(reference, target);
                            if (matchValue > bestMatchValue)
                            {
                                bestMatchValue = matchValue;
                                bestSoFar = target;
                                Debug.Write("v/ ");
                            }
                            Debug.WriteLine($"{reference} - {target} = {matchValue}");
                        }
                    }
                }
            }
        
            return(bestSoFar);
        }

        /// <summary>
        /// Returns a value which is the measure of similarity between the two strings
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static double getSimilarity(string reference, string target)
        {
            /*QGram qGram = new QGram();
            var similarity=qGram.Distance(reference, target);*/

            var ro = new RatcliffObershelp();
            var similarity = ro.Similarity(reference, target);
            return similarity;

        }

        /// <summary>
        ///     Gets the GPS co ordinates.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <param name="latitude">
        ///     The latitude.
        /// </param>
        /// <param name="longitude">
        ///     The longitude.
        /// </param>
        private static bool GetGpsCoOrdinates(string[] headerFile, WavFileMetaData wfmd,
            out decimal? latitude, out decimal? longitude)
        {
            if (wfmd?.m_Location != null)
            {
                latitude = (decimal)wfmd.m_Location.m_Latitude;
                longitude = (decimal)wfmd.m_Location.m_Longitude;
                if (latitude < 200.0m && longitude < 200.0m) return true;
            }

            //Regex gpsRegex = new Regex();
            var result = false;
            if (headerFile == null)
            {
                latitude = null;
                longitude = null;
                return result;
            }

            latitude = null;
            longitude = null;
            var pattern = @"(-?[0-9]{1,}\.[0-9]{1,})\s*,\s*(-?[0-9]{1,2}\.[0-9]{1,})";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success && match.Groups.Count > 2)
                {
                    if (decimal.TryParse(match.Groups[1].Value, out var value)) latitude = value;

                    value = 0.0m;
                    if (decimal.TryParse(match.Groups[2].Value, out value)) longitude = value;
                    break;
                }
            }

            if (latitude != null && longitude != null) result = true;
            return result;
        }

        /// <summary>
        ///     Gets the location.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        /// </returns>
        private static string GetLocation(string[] headerFile)
        {
            if (headerFile == null) return "";
            if (headerFile.Length > 1) return headerFile[1];
            return "";
        }

        /// <summary>
        ///     Gets the microphone.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        /// </returns>
        private static string GetMicrophone(string[] headerFile, WavFileMetaData wfmd = null)
        {
            if (wfmd?.m_Microphone != null) return wfmd.m_Microphone;
            if (headerFile == null || headerFile.Length <= 0) return "";
            var knownMicrophones = DBAccess.GetMicrophoneList();
            if (knownMicrophones == null || knownMicrophones.Count <= 0) return "";
            string bestMatch = GetBestMatch(knownMicrophones, headerFile);
            // get a line in the text containing a known operator
            /*var mm = from line in headerFile
                     join mic in knownMicrophones on line equals mic
                     select mic;

            var matchingMicrophones = headerFile.Where(line =>
                knownMicrophones.Any(txt => line.ToUpper().Contains(txt == null ? "none" : txt.ToUpper())));
            if (!matchingMicrophones.IsNullOrEmpty()) return matchingMicrophones.First();*/
            return bestMatch;
        }

        /// <summary>
        ///     Gets the operator.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        /// </returns>
        private static string GetOperator(string[] headerFile)
        {
            if (headerFile == null || headerFile.Length <= 0) return "";
            var knownOperators = DBAccess.GetOperators();
            if (knownOperators == null || knownOperators.Count <= 0) return "";
            // get a line in the text containing a known operator

            return GetBestMatch(knownOperators, headerFile);
        }

        /// <summary>
        ///     Gets the temporary.
        /// </summary>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <returns>
        /// </returns>
        private static short? GetTemp(string[] headerFile, WavFileMetaData wfmd = null)
        {
            short temp = 0;
            if (wfmd?.m_Temperature != null)
                if (short.TryParse(wfmd.m_Temperature, out temp))
                    return temp;
            if (headerFile == null) return 0;

            var pattern = @"([0-9]{1,2})\s*[C\u00B0]{1}";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    short.TryParse(match.Groups[1].Value, out temp);
                    break;
                }
            }

            return temp;
        }

        /// <summary>
        ///     Revised to cope with either a line with a date and start
        ///     and end times, or a line with two date-time pairs for the
        ///     start and end of the session
        ///     Also checks for a line containing Sunset and a time
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="sessionTag"></param>
        /// <param name="headerFile">
        ///     The header file.
        /// </param>
        /// <param name="wfmd"></param>
        /// <param name="startTime">
        ///     The start time.
        /// </param>
        /// <param name="endTime">
        ///     The end time.
        /// </param>
        /// <param name="sunset"></param>
        private static void GetTimes(string folder, string sessionTag, string[] headerFile,
            WavFileMetaData wfmd, out DateTime startTime, out DateTime endTime, out TimeSpan sunset)
        {
            startTime = new DateTime();
            endTime = new DateTime();

            if (wfmd?.m_Start != null) startTime = wfmd.m_Start.Value;
            if (wfmd?.m_End != null) endTime = wfmd.m_End.Value;
            sunset = new TimeSpan();
            if (headerFile == null) return;
            var times = new BulkObservableCollection<TimeSpan>();
            if (times != null)
            {
                var formattedTimePattern = @"\d{1,2}:\d{2}:{0,1}\d{0,2}";
                foreach (var line in headerFile)
                    if (line.ToUpper().Contains("SUNSET"))
                    {
                        var timepart = line.Substring(line.ToUpper().IndexOf("SUNSET") + 6);
                        var match = Regex.Match(timepart, formattedTimePattern);
                        if (match.Success)
                        {
                            var ts = new TimeSpan();
                            if (TimeSpan.TryParse(match.Value, out ts)) sunset = ts;
                        }
                    }
                    else
                    {
                        // we have a line which is not a sunset line
                        var matchingTimes = Regex.Matches(line, formattedTimePattern);
                        if (matchingTimes.Count == 2)
                        {
                            // we have at two time fields in the line
                            var segments = line.Split('-');
                            if (segments.Length == 2)
                            {
                                // we have a start hyphen end format
                                var date = new DateTime();
                                var time = new TimeSpan();
                                if (!DateTime.TryParse(segments[0].Trim(), out date))
                                    date = Tools.getDateTimeFromFilename(folder);
                                //date = GetDateFromFileName(folder);

                                if (TimeSpan.TryParse(matchingTimes[0].Value, out time))
                                    startTime = date.Date + time;

                                date = date.Date;
                                DateTime.TryParse(segments[1].Trim(), out date);
                                if (TimeSpan.TryParse(matchingTimes[1].Value, out time))
                                {
                                    if (date.Date == DateTime.Now.Date)
                                        endTime = startTime.Date + time;
                                    else
                                        endTime = date.Date + time;
                                }
                            }
                        }
                    }
            }
        }

        /*
        private static DateTime GetDateFromFileName(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return new DateTime();
            var datePattern = @"(%d{4}).?(%d{2}).?(%d{2})";
            var match = Regex.Match(folder, datePattern);
            if (!match.Success)
                if (Directory.Exists(folder))
                {
                    var files = Directory.EnumerateFiles(folder);
                    if (files != null && files.Any())
                        foreach (var file in files)
                        {
                            match = Regex.Match(file, datePattern);
                            if (match.Success) break;
                        }
                }

            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out var year);
                int.TryParse(match.Groups[2].Value, out var month);
                int.TryParse(match.Groups[3].Value, out var day);
                return new DateTime(year, month, day);
            }

            return new DateTime();
        }*/
    }
}