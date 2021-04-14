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

using Microsoft.Maps.MapControl.WPF;
using Microsoft.VisualStudio.Language.Intellisense;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UniversalToolkit;
using WindowsInput;
using WindowsInput.Native;
using Application = System.Windows.Application;
using Brushes = System.Drawing.Brushes;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using DataGrid = System.Windows.Controls.DataGrid;
using MessageBox = System.Windows.Forms.MessageBox;

namespace BatRecordingManager
{
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static class ImagingConverterClass
    {
        /// <summary>
        ///     A static function to convert a Bitmap into a BitmapSOurce for display in a
        ///     wpf Image
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }

    public static class StringHelper
    {
        /// <summary>
        /// Extension method on string to split the string on the first occurence of the specified character
        /// returning a two string array the first contining everything up to the splitter and the second
        /// everything after the splitter.  The splitter is not in either.  string[1] may be empty.
        /// if c is not in the string the full string is returned in string[0].
        /// if the splitter is the first character string[0] will be empty and string[1] will be the original
        /// without the splitter.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string[] SplitOnFirst(this string s, char c)
        {
            string[] result = new string[2];
            result[0] = s;
            result[1] = "";
            if (s.Contains(c))
            {
                int index = s.IndexOf(c);
                if (index >= 0)
                {
                    result[0] = s.Substring(0, index);
                    result[1] = s.Substring(index) + " "; // in case the string ends with c, add some spaces to make it longer than 1 char
                    result[1] = result[1].Substring(1).Trim(); // still contains the c, so remove it and trim the space padding away - may leave an empty string
                }
            }

            return (result);
        }
    }

    /// <summary>
    ///     Class of miscellaneous, multi access functions - all static for ease of re-use
    /// </summary>
    public static class Tools
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static string macroFileName = @"C:\audacity-win-portable\Portable Settings\Macros\BRM-Macro.txt";

        /// <summary>
        ///     BlobTypes are used to identify the type of binary data object stored in the database.
        ///     The enum types are 3 or 4 char strings that are stored as string literals in the database
        ///     but the enum allows simple internal handling.  The enum is converted to a string to be
        ///     stored in the database and is converted back to an enum on retrieval.  enum names must be limited
        ///     to 4 chars to fit into the database type field.
        ///     BMP is a raw bitmap
        ///     BMPS is a BitmapSource object
        ///     WAV is a snippet of waveform read from a .wav file.
        /// </summary>
        public enum BlobType
        {
            NONE = 0,
            ANY = 1,
            BMP,
            BMPS,
            WAV,
            PNG
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Clears all the children of this canvas except children of type Grid
        /// </summary>
        /// <param name="canvas"></param>
        public static void ClearExceptGrids(this Canvas canvas)
        {
            if (canvas.Children != null && canvas.Children.Count > 0)
            {
                var elementsToRemove = new List<UIElement>();
                foreach (var child in canvas.Children)
                    if (!(child is Canvas || child is Grid))
                        elementsToRemove.Add((UIElement)child);
                if (!elementsToRemove.IsNullOrEmpty())
                    foreach (var element in elementsToRemove)
                        canvas.Children.Remove(element);
            }
        }

        /// <summary>
        /// Copies a directory and its contents recursively or not depending
        /// on the boolean parameter
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
                File.SetAttributes(temppath, FileAttributes.Normal);
                File.SetCreationTime(temppath, file.CreationTime);
                File.SetLastAccessTime(temppath, file.LastAccessTime);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// Recursively deletes this directory and all sub directories and allthe files in those
        /// directories, setting attributes to Normal as it goes so even Read-Only items will get deleted
        /// </summary>
        /// <param name="topDir"></param>
        public static void DirectoryDelete(string topDir)
        {
            if (!Directory.Exists(topDir)) return;
            File.SetAttributes(topDir, FileAttributes.Normal);
            var files = Directory.EnumerateFiles(topDir);
            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            var folders = Directory.EnumerateDirectories(topDir);
            foreach (var folder in folders)
            {
                Tools.DirectoryDelete(folder);
            }
            Directory.Delete(topDir);
        }

        public static void ErrorLog(string error)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var path = @"C:\BRM-Error\";
            var errorFile = path + "BRM-Error-Log.txt";
            try
            {
                var build = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                using (var stream = new StreamWriter(errorFile, true))
                {
                    if (!HasErred) stream.WriteLine("\n" + DateTime.Now + "Bat Recording Manager v" + build + "\n");
                    var stackTrace = new StackTrace();
                    var caller = stackTrace.GetFrame(1).GetMethod().Name;
                    stream.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "[" +
                                     caller + "] :- " + error);
                    Debug.WriteLine("ERROR:- in " + caller + ":- " + error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\n\n**** Error writing to error log" + ex);
                File.AppendAllText(path + "FatalError.txt", "Error writing to Log file!!!!!!!!!!!\n");
            }
        }

        /// <summary>
        ///     Finds a descendant of the object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T FindDescendant<T>(DependencyObject obj) where T : DependencyObject

        {
            // Check if this object is the specified type
            if (obj is T dependencyObject)
                return dependencyObject;

            // Check for children
            var childrenCount = VisualTreeHelper.GetChildrenCount(obj);
            if (childrenCount < 1)
                return null;

            // First check all the children
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T o)
                    return o;
            }

            // Then check the childrens children
            for (var i = 0; i < childrenCount; i++)
            {
                DependencyObject child = FindDescendant<T>(VisualTreeHelper.GetChild(obj, i));
                if (child is T)
                    return child as T;
            }

            return null;
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // get parent item
            var parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we want
            if (parentObject is T parent)
                return parent;
            return FindParent<T>(parentObject);
        }

        /// <summary>
        ///     Formats the time span. Given a Timespan returns a formatted string as mm'ss.sss" or 23h59'58.765"
        /// </summary>
        /// <param name="time">
        ///     The time.
        /// </param>
        /// <returns>
        /// </returns>
        public static string FormattedTimeSpan(TimeSpan time)
        {
            TimeSpan absTime = time;
            var result = "";
            if (time != null)
            {
                absTime = time.Duration();
                if (absTime.Hours > 0) result = result + absTime.Hours + "h";
                if (absTime.Hours > 0 || absTime.Minutes > 0) result = result + absTime.Minutes + "'";
                var seconds = absTime.Seconds + absTime.Milliseconds / 1000.0m;
                result = result + $"{seconds:0.0#}\"";
            }

            if (time.Ticks < 0L)
            {
                result = "(-" + result + ")";
            }

            return result;
        }

        /// <summary>
        /// / parses the recording name to try and get date and time from it.
        /// Essentially the same as getDateTimeFromFilename(string file,out DateTime date)
        /// Works with both .wav files containing yyyymmdd[-_]hhmmss formatted date time
        /// or ZC files as YMddhhmm[-_]ss where Y and M may be alphanumeric
        /// if Parsing fails returns DateTime.Now
        /// </summary>
        /// <param name="wavfile"></param>
        /// <returns></returns>
        public static DateTime getDateTimeFromFilename(string wavfile)
        {
            DateTime result = DateTime.Now;
            if (GetDateTimeFromFilename(wavfile, out DateTime date))
            {
                result = date;
            }
            else
            {
                Debug.WriteLine("Unable to get date time from {" + wavfile + "}");
            }
            return (result);
        }

        /// <summary>
        ///     parses the recording filename to see if it contains sequences that correspond to a date
        ///     and/or time and if so returns those dates and times combined in a single dateTime parameter.
        ///     returns true if valid dates/times are established and false otherwise.
        ///     Works with both .wav files containing yyyymmdd[-_]hhmmss formatted date time
        ///     or ZC files as YMddhhmm[-_]ss where Y and M may be alphanumeric
        ///     if parsing fails returns new DateTime()
        /// </summary>
        /// <param name="fullyQualifiedFileName"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool GetDateTimeFromFilename(string fullyQualifiedFileName, out DateTime date)
        {
            date = new DateTime();
            var pattern =
                @"([12][09][0-9]{2})[-_]?([0-1][0-9])[-_]?([0-3][0-9])[-_\s]?([0-2][0-9])[-_:]?([0-5][0-9])[-_:]?([0-5][0-9])";
            var match = Regex.Match(fullyQualifiedFileName, pattern);
            if (match.Success)
            {
                if (match.Groups.Count >= 4)
                {
                    var year = match.Groups[1].Value.Trim();
                    var month = match.Groups[2].Value.Trim();
                    var day = match.Groups[3].Value.Trim();
                    var hour = "00";
                    var minute = "00";
                    var second = "00";
                    if (match.Groups.Count >= 7)
                    {
                        hour = match.Groups[4].Value.Trim();
                        minute = match.Groups[5].Value.Trim();
                        second = match.Groups[6].Value.Trim();
                    }

                    var result = new DateTime();
                    var enGB = new CultureInfo("en-GB");
                    var extractedString = year + "/" + month + "/" + day + " " + hour + ":" + minute + ":" + second;

                    if (DateTime.TryParseExact(extractedString, "yyyy/MM/dd HH:mm:ss", null, DateTimeStyles.AssumeLocal,
                        out result))
                    {
                        Debug.WriteLine("Found date time of " + result + " in " + fullyQualifiedFileName);
                        date = result;
                        return true;
                    }
                }
            }
            // Only gets here if it did not find a normally coded filename and has already returned true
            if (fullyQualifiedFileName.EndsWith(".zc", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    pattern = @"([0-9A-Z]{1})([0-9A-Z]{1})([0-9]{2})([0-9]{2})([0-9]{2})_([0-9]{2})";
                    match = Regex.Match(Path.GetFileName(fullyQualifiedFileName), pattern);
                    if (match.Success && match.Groups.Count == 7)
                    {
                        char c = match.Groups[1].Value[0];
                        int year;
                        if (char.IsDigit(c))
                        {
                            year = 1990 + (int)(c - '0');
                        }
                        else
                        {
                            year = 2000 + (int)(c - 'A');
                        }

                        c = match.Groups[2].Value[0];
                        int month;
                        if (char.IsDigit(c))
                        {
                            month = (int)(c - '0');
                        }
                        else
                        {
                            month = 10 + (int)(c - 'A');
                        }

                        var day = int.Parse(match.Groups[3].Value.Trim());
                        var hour = int.Parse(match.Groups[4].Value.Trim());
                        var minute = int.Parse(match.Groups[5].Value.Trim());
                        var second = int.Parse(match.Groups[6].Value.Trim());
                        var result = new DateTime(year, month, day, hour, minute, second);
                        date = result;
                        return (true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error parsing .zc filename ", fullyQualifiedFileName);
                    return (false);
                }
            }

            Debug.WriteLine("No datetime found in " + fullyQualifiedFileName);
            return false;
        }

        /// <summary>
        ///     Gets the duration of the file. (NB would be improved by using various Regex to parse the
        ///     filename into dates and times for .wav or .zc files
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
        public static TimeSpan GetFileDatesAndTimes(string fileName, out string wavfile, out DateTime fileStart,
            out DateTime fileEnd)
        {
            fileStart = DateTime.Now;
            fileEnd = new DateTime();

            var duration = new TimeSpan(0L);
            wavfile = fileName;
            try
            {
                var wavfilename = Path.ChangeExtension(fileName, ".wav");

                var zcFileName = Path.ChangeExtension(fileName, ".zc");
                if (File.Exists(zcFileName)) fileName = zcFileName;
                if (File.Exists(wavfilename)) fileName = wavfilename; // fileName now explicitly the wav or zc filename
                                                                      // priority to the wavfile if both exist

                if ((File.Exists(wavfilename) || File.Exists(zcFileName)) && (new FileInfo(fileName).Length > 0L))
                {
                    //var info = new FileInfo(fileName);

                    var fa = File.GetAttributes(fileName); // OK for both .wav and .zc
                    DateTime created = File.GetCreationTime(fileName);  // OK for both .wav and .zc
                    if (created.Year < 1990)
                    {
                        // files created earlier than this are likely to be corrupt or to have had an invalid or no creation date
                        created = DateTime.Now;
                    }
                    DateTime modified = File.GetLastWriteTime(fileName); //// OK for both .wav and .zc
                    if (modified.Year < 1990)
                    {
                        modified = DateTime.Now;
                    }
                    DateTime named;
                    if (!Tools.GetDateTimeFromFilename(fileName, out named))  // OK for both .wav and .zc
                    {
                        named = Tools.getDateTimeFromFilename(fileName); //// OK for both .wav and .zc
                    }
                    DateTime recorded = GetDateTimeFromMetaData(fileName, out duration, out string zcTextHeader);
                    if (recorded.Year < 1990)
                    {
                        // unlikely to have been generating wamd or guano files before 1990
                        recorded = DateTime.Now;
                    }

                    // set fileStart to the earliest of the three date times since we don't which if any have been
                    // corrupted by copying since the file was recorded, but the earliest must be our best guess for
                    // the time being.
                    if (fileStart > created) fileStart = created;
                    if (fileStart > modified) fileStart = modified;
                    if (fileStart > named) fileStart = named;
                    if (fileStart > recorded) fileStart = recorded;

                    if (File.Exists(wavfilename)) // get the duration from the metadata for .wav files
                    {
                        using (WaveFileReader wfr = new WaveFileReader(wavfilename))
                        {
                            duration = wfr.TotalTime;
                            fileEnd = fileStart + duration;
                            wavfile = wavfilename;
                            return (duration);
                        }
                    }
                    else if (File.Exists(zcFileName)) // assume a duration of 15s for .zc files
                    {
                        duration = TimeSpan.FromSeconds(15);
                        fileEnd = fileStart + duration;
                        wavfile = zcFileName;
                        return (duration);
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
            fileEnd = fileStart + duration;
            return duration;
        }

        /// <summary>
        /// Displays a file save dialog to the user to allow them to select a location and filename to
        /// write to.  Parameters give an prefferred location, which can be null, and a prefferred file
        /// extension which can be null.  The function checks if the requested file already exists and
        /// takes appropriate actions.  The file extension can be changed by the user.
        /// If the dialog is cancelled an empty string is returned.
        /// Defaults to MyDocuments and .wav
        /// </summary>
        /// <param name="initialLocation"></param>
        /// <param name="desiredExtension"></param>
        /// <returns></returns>
        public static string GetFileToWriteTo(string initialLocation, string desiredExtension)
        {
            string result = "";
            if (string.IsNullOrWhiteSpace(initialLocation))
            {
                initialLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if (!Directory.Exists(initialLocation))
            {
                initialLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if (string.IsNullOrWhiteSpace(desiredExtension))
            {
                desiredExtension = ".wav";
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.OverwritePrompt = true;
                sfd.AddExtension = true;
                sfd.CheckFileExists = false;
                sfd.CheckPathExists = true;
                sfd.DefaultExt = desiredExtension;

                sfd.InitialDirectory = initialLocation;
                var outcome = sfd.ShowDialog();
                if (outcome == DialogResult.OK)
                {
                    result = sfd.FileName;
                }
            }

            return (result);
        }

        /// <summary>
        ///     Returns the path component from the fully qualified file name
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        public static string GetPath(string wavFile)
        {
            if (string.IsNullOrWhiteSpace(wavFile)) return "";
            if (wavFile.EndsWith(@"\")) return wavFile;
            if (!wavFile.Contains(@"\")) return "";

            return wavFile.Substring(0, wavFile.LastIndexOf(@"\") + 1);
        }

        /// <summary>
        ///     Uses the recording start and end times to calculate the recording duration
        ///     and if the end time is earlier than the start time adds 1 day to the end time
        ///     to allow for recordings starting before midnight and ending after midnight.
        ///     Does not mak allowance for multi-day recordings.
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        public static TimeSpan GetRecordingDuration(Recording recording)
        {
            var dur = new TimeSpan();
            if (recording.RecordingEndTime != null && recording.RecordingStartTime != null)
            {
                if (recording.RecordingEndTime.Value > recording.RecordingStartTime.Value)
                {
                    dur = recording.RecordingEndTime.Value - recording.RecordingStartTime.Value;
                }
                else
                {
                    var start = new DateTime() + recording.RecordingStartTime.Value;
                    var end = new DateTime().AddDays(1) + recording.RecordingEndTime.Value;
                    dur = end - start;
                }
            }

            return dur;
        }

        /// <summary>
        ///     Extension method for IEnumerable(T) to check if the list is null or empty
        ///     before committing to a foreach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return !(list?.Any() ?? false);
        }

        /// <summary>
        ///     Opens Kaleidoscope with a folder path or a filename.
        ///     If the string is a filename the file part is stripped from it to leave
        ///     the bare path.  Then the oldest .wav file in the specified folder is opened
        ///     in Kaleidoscope.  It is assumed that Kaleidoscope.exe is in the system path
        ///     and does not need to be explicitly located in order to run.
        ///     If a process is passed to the function then a callback ill be generated on that
        ///     process when Kaleidoscope exits.  If no process is provided a new one will be
        ///     generated.
        /// </summary>
        /// <param name="wavFile"></param>
        /// <param name="process" default="null"></param>
        public static Process OpenKaleidoscope(string wavFile, Process externalProcess = null)
        {
            if (string.IsNullOrWhiteSpace(wavFile)) return null;
            var file = GetOldestFile(wavFile); // returns the fully qualified path and filename
            if (string.IsNullOrWhiteSpace(file)) return null;

            if (externalProcess == null)
            {
                externalProcess = new Process();
                externalProcess.Exited += ExternalProcess_Exited;
                if (externalProcess == null) return null;
            }

            var result = DialogResult.Retry;
            while (result == DialogResult.Retry)
            {
                var p = Process.GetProcessesByName("kaleidoscope");

                if (p.Length > 0)
                    result = MessageBox.Show("Please close open copies of Kaleidoscope first.",
                        "Kaleidoscope Already Open", MessageBoxButtons.RetryCancel);
                else
                    break;
            }

            if (result == DialogResult.Cancel)
            {
                externalProcess.Close();
                return null;
            }

            var executable = @"C:\Program Files (x86)\Wildlife Acoustics\kaleidoscope\kaleidoscope.exe";
            externalProcess.StartInfo.FileName = "\"" + executable + "\"";
            externalProcess.StartInfo.Arguments = "\"" + file + "\""; // enclosed in quotes in case the path has spaces

            //externalProcess.StartInfo.Arguments = folder;
            externalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;

            var started = externalProcess.Start();
            while (!externalProcess.Responding)
            {
                Thread.Sleep(100);
                Debug.Write("!");
            }

            Thread.Sleep(1000);
            externalProcess.EnableRaisingEvents = true;
            //ExternalProcess.Exited += ExternalProcess_Exited;

            //int startSeconds = (int)startOffset.TotalSeconds;
            //int endSeconds = (int)endOffset.TotalSeconds;
            //if (endSeconds == startSeconds) endSeconds = startSeconds + 1;
            try
            {
                Application.Current.MainWindow.Focus();
            }
            catch (InvalidOperationException)
            {
            }

            while (externalProcess.MainWindowHandle == (IntPtr)0L)
                if (externalProcess.HasExited)
                {
                    externalProcess.Close();
                    return null;
                }

            if (!WaitForIdle(externalProcess, "!", "Starting")) return null;
            var epHandle = externalProcess.MainWindowHandle;
            SetForegroundWindow(epHandle);

            return externalProcess;
        }

        public static void SetFolderIcon(string path, string iconPath, string folderToolTip)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            /* Remove any existing desktop.ini */
            if (File.Exists(path + @"desktop.ini")) File.Delete(path + @"desktop.ini");

            /* Write the desktop.ini */
            using (var sw = File.CreateText(path + @"desktop.ini"))
            {
                if (sw != null)
                {
                    sw.WriteLine("[.ShellClassInfo]");
                    sw.WriteLine("InfoTip=" + folderToolTip);
                    sw.WriteLine("IconResource=" + iconPath);
                    sw.WriteLine("IconIndex=0");
                    sw.Close();
                }
            }

            /* Set the desktop.ini to be hidden */
            File.SetAttributes(path + @"desktop.ini",
                File.GetAttributes(path + @"desktop.ini") | FileAttributes.Hidden);

            /* Set the path to system */
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System);
        }

        /// <summary>
        ///     Using Reflection to force a sort on a column of a System.Windows.Controls.DataGrid
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="columnIndex"></param>
        public static void SortColumn(DataGrid dataGrid, int columnIndex)
        {
            var performSortMethod =
                typeof(DataGrid).GetMethod("PerformSort", BindingFlags.Instance | BindingFlags.NonPublic);
            performSortMethod?.Invoke(dataGrid, new[] { dataGrid.Columns[columnIndex] });
        }

        /// <summary>
        ///     Given a filename removes the path if any
        /// </summary>
        /// <param name="textFileName"></param>
        /// <returns></returns>
        public static string StripPath(string textFileName)
        {
            if (textFileName.EndsWith(@"\")) return "";
            if (textFileName.Contains(@"\")) textFileName = textFileName.Substring(textFileName.LastIndexOf(@"\") + 1);
            //Debug.WriteLine("Open text file:-" + textFileName);
            return textFileName;
        }

        /// <summary>
        ///     Parses a line in the format 00'00.00 into a TimeSpan the original strting has been
        ///     matched by a Regex of the form [0-9\.\']+
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public static TimeSpan TimeParse(string value)
        {
            var regPattern = @"([0-9]*\')?([0-9]+)[\.]?([0-9]*)";
            var minutes = 0;
            var seconds = 0;
            var millis = 0;

            var result = Regex.Match(value, regPattern);
            if (result.Success && result.Groups.Count >= 4)
            {
                // we have matched and identified the fields
                if (!string.IsNullOrWhiteSpace(result.Groups[1].Value))
                {
                    var minstr = result.Groups[1].Value.Substring(0, result.Groups[1].Value.Length - 1);
                    var r1 = int.TryParse(minstr, out minutes);
                }

                if (!string.IsNullOrWhiteSpace(result.Groups[2].Value))
                {
                    var r2 = int.TryParse(result.Groups[2].Value, out seconds);
                }

                if (!string.IsNullOrWhiteSpace(result.Groups[3].Value))
                {
                    var s = "0." + result.Groups[3].Value;
                    var r3 = double.TryParse(s, out var dm);
                    millis = (int)(dm * 1000);
                }
            }

            var ts = new TimeSpan(0, 0, minutes, seconds, millis);
            return ts;
        }

        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern bool DeleteObject(IntPtr hObject);
        /// <summary>
        ///     Converts a System.Drawing.Bitmap to a WPF compatible BitmapImage
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static BitmapSource ToBitmapSource(this Bitmap source)
        {
            //IntPtr hBitmap = source.GetHbitmap();
            BitmapSource result;

            result = ImagingConverterClass.CreateBitmapSourceFromBitmap(source);
            //hBitmap,
            //IntPtr.Zero,
            //Int32Rect.Empty,
            //BitmapSizeOptions.FromEmptyOptions());
            return result;
        }

        /// <summary>
        ///     To the formatted string.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <returns>
        /// </returns>
        public static string ToFormattedString(this RecordingSession session)
        {
            var result = "";
            result += session.SessionTag + "\n";
            result += session.Location + "\n";
            result += session.SessionDate.ToShortDateString() + " " +
                      session.SessionStartTime ?? "" + " - " +
                      (session.EndDate ?? session.SessionDate).ToShortDateString() + " " +
                      session.SessionEndTime ?? "" + "\n";
            result += session.Operator ?? "" + "\n";
            result += (session.LocationGPSLatitude ?? 0.0m) + ", " + (session.LocationGPSLongitude ?? 0.0m) + "\n";
            result += session.Equipment ?? "" + "\n";
            result += session.Microphone ?? "" + "\n";
            result += session.SessionNotes ?? "" + "\n";
            result += "==================================================================\n";

            return result;
        }

        /// <summary>
        ///     Given an external process, waits for the process to be responding and for Inputidle as well
        ///     as a static 100ms wait at the start.  If the ExternalProcess exits during the wait then the
        ///     function returns false, otherwise it returns true.  Will wait indefinitiely if the process
        ///     does not exit and never becomes idle.
        /// </summary>
        /// <param name="externalProcess"></param>
        /// <param name="marker"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public static bool WaitForIdle(Process externalProcess, string marker = "", string location = "")
        {
            Debug.Write(marker);
            Thread.Sleep(100);

            for (int i = 0; i < 5; i++)
            {
                if (externalProcess.WaitForInputIdle(100)) break;
                if (externalProcess.Responding) break;
                Debug.Write(marker);
                if (externalProcess.HasExited)
                {
                    externalProcess.Close();
                    Debug.Write("Process Exited at:- " + location);
                    return false;
                }
            }

            return true;
        }

        internal static void ActivateApp(string processName)
        {
            var p = Process.GetProcessesByName(processName);

            // Activate the first application we find with this name
            if (p.Length > 0)
                SetForegroundWindow(p[0].MainWindowHandle);
        }

        /// <summary>
        ///     Given a string, removes any curly brackets and replaces them around all the text following
        ///     a $ if any, or around the entire string if there is no $
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        internal static string AdjustBracketedText(string comment)
        {
            comment = comment.Replace("{", " ");
            comment = comment.Replace("}", " ");
            comment = comment.Trim();
            if (comment.Contains("$"))
                comment = comment.Replace("$", "${");
            else
                comment = "{" + comment;
            comment = comment + "}";
            return comment;
        }

        /// <summary>
        ///     Condenses the stats list. Given a List of BatStats for a wide collection of bats and
        ///     passes, condenses it to have a single BatStat for each bat type along with the
        ///     cumulative number of passes and segments.
        /// </summary>
        /// <param name="statsForSession">
        ///     The stats for session.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static BulkObservableCollection<BatStats> CondenseStatsList(
            BulkObservableCollection<BatStats> statsForSession)
        {
            var result = new BulkObservableCollection<BatStats>();
            foreach (var stat in statsForSession)
            {
                var matchingStats = from s in result
                                    where s.batCommonName == stat.batCommonName
                                    select s;

                if (matchingStats != null && matchingStats.Any()) // list of all stats in result for thisbat - should be just one
                                                                  // since we add the new data to it each time rather than creating a new
                                                                  // entry in result
                {
                    BatStats existingStat = matchingStats.First();
                    existingStat.Add(stat);                         // merge the new stat with the existing one
                }
                else
                {                                               // but if this bat is not in result yet, we simply add it
                    if (!string.IsNullOrEmpty(stat.batAutoID))
                    {
                        if (stat.batAutoID.StartsWith(";"))
                        {
                            stat.batAutoID = stat.batAutoID.Substring(1).Trim() + ";";
                        }
                    }
                    result.Add(stat);
                }
            }

            return result;
        }

        /// <summary>
        ///     Converts the double in seconds to time span.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <returns>
        /// </returns>
        internal static TimeSpan ConvertDoubleToTimeSpan(double? value)
        {
            if (value == null) return new TimeSpan();
            var seconds = (int)Math.Floor(value.Value);
            var millis = (int)Math.Round((value.Value - seconds) * 1000.0d);

            var minutes = Math.DivRem(seconds, 60, out seconds);
            return new TimeSpan(0, 0, minutes, seconds, millis);
        }

        /// <summary>
        ///     looks in a string for the sequence .wav and truncates the string after that
        ///     then removes leading charachters up to the last \ to remove any path and pre-amble.
        ///     This should leave just the filename.wav unless there was textual preamble to the filename
        ///     which cannot be distinguished from part of the name.
        ///     returns null if no such string is found
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static string ExtractWavFilename(string description)
        {
            if (description.ToUpper().Contains(".WAV"))
            {
                var fullname = description.Substring(0, description.ToUpper().IndexOf(".WAV") + 4);
                if (fullname.Contains(@"\")) fullname = fullname.Substring(description.LastIndexOf(@"\") + 1);
                return fullname;
            }

            return null;
        }

        /// <summary>
        ///     Given a valid Segment, generates a formatted string in the format mm'ss.ss" - mm'ss.ss"
        ///     = mm'ss.ss" comment
        /// </summary>
        /// <param name="segment">
        ///     The segment.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static string FormattedSegmentLine(LabelledSegment segment, bool offsetIntoRecording = true)
        {
            if (segment == null) return "";

            TimeSpan start = segment.StartOffset;
            TimeSpan end = segment.EndOffset;
            if (!offsetIntoRecording)
            {
                try
                {/*
                    var sunset = segment.Recording.RecordingSession.Sunset;
                    if (sunset != null && segment.Recording.RecordingStartTime!=null && segment.Recording.RecordingEndTime!=null)
                    {
                        start = (segment.Recording.RecordingSession.SessionDate.Date+segment.Recording.RecordingStartTime.Value + segment.StartOffset - sunset.Value).TimeOfDay;
                        end = ((segment.Recording.RecordingSession.EndDate??segment.Recording.RecordingSession.SessionDate).Date+segment.Recording.RecordingEndTime.Value + segment.StartOffset - sunset.Value).TimeOfDay;
                    }*/
                    var recordingStartTimeAfterSunset = segment.Recording.startTimeAfterSunset;
                    if (recordingStartTimeAfterSunset != null)
                    {
                        start = recordingStartTimeAfterSunset.Value + segment.StartOffset;
                        end = recordingStartTimeAfterSunset.Value + segment.EndOffset;
                    }
                }
                catch (Exception)
                {
                    start = segment.StartOffset;
                    end = segment.EndOffset;
                }
            }

            var result = (!offsetIntoRecording ? "SS + " : "") +
                         FormattedTimeSpan(start) + " - " +
                         FormattedTimeSpan(end) + " = " +
                         FormattedTimeSpan(segment.EndOffset - segment.StartOffset) + "; " +
                         segment.Comment;

            //var calls = DBAccess.GetCallParametersForSegment(segment);
            //if (calls != null && calls.Count > 0)
            //    foreach (Call call in calls)
            //    {
            //        result = result + call.GetFormattedString();
            //    }

            return result;
        }

        internal static string FormattedValuePair(string header, double? value, double? variation)
        {
            var result = header;

            if (value == null || value <= 0.0d) return "";
            result = (header ?? "") + $"{value:##0.0}";
            if (variation != null && variation >= 0.0) result = result + "+/-" + $"{variation:##0.0}";

            return result;
        }

        /// <summary>
        /// checks to see if the passed comment contains a bracketed string containing an Auto ID
        /// and if so returns the AutoID
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        internal static string getAutoIdFromComment(string comment)
        {
            string autoID = null;
            if (comment.Contains("("))
            {
                string pattern = @"\(Auto=([^\)]+)";
                var match = Regex.Match(comment, pattern);
                if (match.Success)
                {
                    if (match.Groups != null && match.Groups.Count >= 2)
                    {
                        autoID = match.Groups[1].Value;
                    }
                }
            }
            return (autoID);
        }

        /// <summary>
        ///     returns a DateTime containing the date defined in a sessionTag of the format
        ///     [alnum]*[-_][alnum]+[-_]20yymmdd
        /// </summary>
        /// <returns></returns>
        internal static DateTime GetDateFromTag(string tag)
        {
            var result = new DateTime();

            var dateField = tag.Substring(tag.LastIndexOfAny(new[] { '-', '_' }));
            if (dateField.Length == 9)
            {
                var stryear = dateField.Substring(1, 4);
                var strmonth = dateField.Substring(5, 2);
                var strday = dateField.Substring(7, 2);
                var Year = DateTime.Now.Year;
                var Month = DateTime.Now.Month;
                var Day = DateTime.Now.Day;
                int.TryParse(stryear, out Year);
                int.TryParse(strmonth, out Month);
                int.TryParse(strday, out Day);
                result = new DateTime(Year, Month, Day);
            }

            return result;
        }

        internal static string GetFormattedBatStats(BatStats value, bool showNoBats)
        {
            var result = "";
            if (value == null) return result;

            if (value.batCommonName.ToUpper() == "NO BATS" || value.batCommonName.ToUpper() == "NOBATS")
            {
                if (showNoBats)
                    return "No Bats";
                return "";
            }

            if (value.passes > 0 || value.segments > 0)
                result = value.batCommonName + " " + value.passes + (value.passes == 1 ? " pass in " : " passes in ") +
                         value.segments + " segment" + (value.segments != 1 ? "s" : "") +
                         " = ( " +
                         "Min=" + FormattedTimeSpan(value.minDuration) +
                         ", Max=" + FormattedTimeSpan(value.maxDuration) +
                         ", Mean=" + FormattedTimeSpan(value.meanDuration) + " )" +
                         "Total duration=" + FormattedTimeSpan(value.totalDuration) +
                         (string.IsNullOrWhiteSpace(value.batAutoID) ? "" : $" AutoID={value.batAutoID}");
            return result;
        }

        internal static int GetNumberOfPassesForSegment(LabelledSegment segment)
        {
            var stat = new BatStats();
            stat.Add(segment.EndOffset - segment.StartOffset, segment.AutoID);
            return stat.passes;
        }

        /// <summary>
        ///     Given a recording Session, returns a list of strings each of which contains a summary
        ///     of number and duration od passes for a specific type of bat for that session.
        /// </summary>
        /// <param name="recordingSession"></param>
        /// <returns></returns>
        internal static List<string> GetSessionSummary(RecordingSession session)
        {
            BulkObservableCollection<BatStats> statsForSession = new BulkObservableCollection<BatStats>();
            session = DBAccess.getIndependantSession(session.Id);
            var result = new List<string>();
            lock (session)
            {
                statsForSession = session.GetStats();
            }

            statsForSession = CondenseStatsList(statsForSession);
            foreach (var batStat in statsForSession)
            {
                var summary = GetFormattedBatStats(batStat, false);
                if (!string.IsNullOrWhiteSpace(summary)) result.Add(summary);
            }

            return result;
        }

        /// <summary>
        ///     Takes a string with two values as either mean+/-variation
        ///     or as min-max, converts them tot he standard mean and
        ///     variation format as two doubles and returns those two
        ///     values.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="mean"></param>
        /// <param name="variation"></param>
        /// <returns></returns>
        internal static bool GetValuesAsMeanAndVariation(string parameters, out double mean, out double variation)
        {
            mean = 0.0d;
            variation = 0.0d;
            if (string.IsNullOrWhiteSpace(parameters)) return false;

            var match = Regex.Match(parameters, @"\s*([0-9.]+)\s*([+\-\/]*)\s*([0-9.]*)");
            /* then parse the two doubles, read the middle matched segment
             * and do the conversion as below if it is necessary,
             * then assign the values and return;
             * */

            if (match.Success)
            {
                for (var i = 1; i < match.Groups.Count; i++)
                {
                    if (i == 0) continue;
                    double.TryParse(match.Groups[i].Value, out var v);
                    switch (i)
                    {
                        case 1:
                            mean = v;
                            break;

                        case 2: break;
                        case 3:
                            variation = v;
                            break;
                    }
                }

                if (match.Groups.Count > 2)
                {
                    var sep = match.Groups[2].Value;
                    if (!sep.Contains("+/-"))
                    {
                        var temp = (mean + variation) / 2;
                        variation = Math.Max(mean, variation) - temp;
                        mean = temp;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Writes an information string to the Error Log without the additional burden of a stack trace
        /// </summary>
        /// <param name="v"></param>
        internal static void InfoLog(string error)
        {
            var path = @"C:\BRM-Error\";
            var errorFile = path + "BRM-Error-Log.txt";
            try
            {
                var build = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                using (var stream = new StreamWriter(errorFile, true))
                {
                    if (isFirstError)
                    {
                        stream.WriteLine(@"
==========================================================================================================

");
                        isFirstError = false;
                    }
                    stream.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":-" +
                                     error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\n\n**** Error writing to info log" + ex);
                File.AppendAllText(path + "FatalError.txt", "Error writing to Info Log file!!!!!!!!!!!\n");
            }
        }

        /// <summary>
        /// given an instance of a Recording, looks for a .txt file of the same name and returns true if
        /// the file modified date and time are within 10s of Now.  Otherwise returns false;
        /// </summary>
        /// <param name="thisRecording"></param>
        /// <returns></returns>
        internal static bool IsTextFileModified(DateTime since, Recording thisRecording)
        {
            if (thisRecording == null) return false;
            string filename = (thisRecording.RecordingSession.OriginalFilePath ?? "") + thisRecording.RecordingName;
            filename = Tools.GetMatchingTextFile(filename);
            if (!File.Exists(filename)) return false;
            DateTime whenModified = File.GetLastWriteTime(filename);
            if (whenModified >= since) return true;
            return false;
        }

        internal static Process OpenWavAndTextFile(string FQWavFileName, Process externalProcess = null)
        {
            Debug.WriteLine("Selected wavFile=" + FQWavFileName);
            FQWavFileName = FQWavFileName.Replace(@"\\", @"\");
            Debug.WriteLine("Corrected wavFile=" + FQWavFileName);
            if (!File.Exists(FQWavFileName) && (new FileInfo(FQWavFileName).Length > 0L))
            {
                Debug.WriteLine("Wav file does not exist");
                return null;
            }

            //int sleep = 1000;
            if (externalProcess == null)
            {
                externalProcess = new Process();
                externalProcess.Exited += ExternalProcess_Exited;
                if (externalProcess == null) return null;
            }

            var result = DialogResult.Retry;
            while (result == DialogResult.Retry)
            {
                var p = Process.GetProcessesByName("audacity");

                if (p.Length > 0)
                    result = MessageBox.Show("Please close open copies of Audacity first.", "Audacity Already Open",
                        MessageBoxButtons.RetryCancel);
                else
                    break;
            }

            if (result == DialogResult.Cancel)
            {
                externalProcess.Close();
                return null;
            }

            var audacityFileLocation = FindAudacity();
            if (string.IsNullOrWhiteSpace(audacityFileLocation) || !File.Exists(audacityFileLocation))
            {
                externalProcess.StartInfo.FileName = FQWavFileName;
            }
            else
            {
                externalProcess.StartInfo.FileName = audacityFileLocation;
            }

            externalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;

            var started = externalProcess.Start();
            while (!externalProcess.Responding)
            {
                Thread.Sleep(100);
                Debug.Write("!");
            }

            externalProcess.EnableRaisingEvents = true;

            try
            {
                MainWindowFocus();
            }
            catch (InvalidOperationException)
            {// may get an InvalidOperationException which can be ignored
            }

            while (externalProcess.MainWindowHandle == (IntPtr)0L)
                if (externalProcess.HasExited)
                {
                    externalProcess.Close();
                    return null;
                }

            if (!WaitForIdle(externalProcess, "!", "Starting")) return null;
            var epHandle = externalProcess.MainWindowHandle;
            SetForegroundWindow(epHandle);

            try
            {
                var ipSim = new InputSimulator();

                Debug.WriteLine(externalProcess.MainWindowTitle);
                externalProcess.WaitForInputIdle();

                NamedPipeClientStream inStream;
                NamedPipeClientStream outStream;
                setPipeStrem(out inStream, out outStream);
                StreamReader streamReader = new StreamReader(inStream);
                StreamWriter streamWriter = new StreamWriter(outStream);
                startAudacityWithPipes(streamReader, streamWriter, FQWavFileName, ipSim);

                ZoomAudacity(0, 5, streamReader, streamWriter);

                if (!WaitForIdle(externalProcess)) return null;

                var textFileName = GetMatchingTextFile(FQWavFileName);

                Debug.WriteLine("Matches {" + FQWavFileName + "} to {" + textFileName + "}");

                if (!string.IsNullOrWhiteSpace(textFileName) && File.Exists(textFileName))
                {
                    if (!OpenAudacityLabelFile(externalProcess, ipSim, textFileName, streamReader, streamWriter)) return null;
                }
                else
                {
                    if (!CreateAudacityLabelFile(externalProcess, ipSim, textFileName, streamReader, streamWriter)) return null;
                }
                string command = $"FirstTrack:\n";
                DoPipeCommand(streamReader, streamWriter, command);
                command = $"CursTrackStart:\n";
                DoPipeCommand(streamReader, streamWriter, command);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error trying to open audacity with a .wav and a .txt:- " + ex.Message);
                ErrorLog("Error trying to open .wav and .txt file in Audacity:-" + ex.Message);
            }

            return externalProcess;
        }

        internal static void OpenWavFile(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !File.Exists(folder) || (new FileInfo(folder).Length <= 0L)) return;

            OpenWavAndTextFile(folder);
        }

        /// <summary>
        ///     Opens the specified .wav file in Audacity and sends the necessarty keyboard commands to zoom in to
        ///     the segment defined by the specified start and end offsets.
        /// </summary>
        /// <param name="wavFile"></param>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        internal static void OpenWavFile(string wavFile, TimeSpan startOffset, TimeSpan endOffset)
        {
            if (string.IsNullOrWhiteSpace(wavFile) || !File.Exists(wavFile) || (new FileInfo(wavFile).Length <= 0L))
                return; // since we don't have a valid file name to work with
            var startSeconds = (int)startOffset.TotalSeconds;
            var endSeconds = (int)endOffset.TotalSeconds;
            if (endSeconds == startSeconds) endSeconds = startSeconds + 1;
            Debug.WriteLine("Open Audacity from " + startOffset.TotalSeconds + " to " + endOffset.TotalSeconds);
            var externalProcess = OpenWavAndTextFile(wavFile);
            if (externalProcess == null) return; // since we have failed to start an external program successfully
            Debug.WriteLine("Audacity running and file opened");
            var epHandle = externalProcess.MainWindowHandle;

            try
            {
                /*
                 * J            Move to start of track
                 * ....         Move right (end-start) seconds
                 * SHIFT-J      select cursor to start of track
                 * CTRL-E       Zoom to selection
                 * J            Move to start of track and clear selection
                 * ....         Move right (end) seconds
                 *
                 *
                 *
                 *
                 * */
                var ipSim = new InputSimulator();
                SetForegroundWindow(epHandle);
                ZoomAudacity(startOffset.TotalSeconds, endOffset.TotalSeconds, ipSim);

                Debug.WriteLine("Audacity zoomed");
            }
            catch (Exception ex)
            {
                ErrorLog("Error opening and zooming Audacity:-" + ex.Message);
                Debug.WriteLine("Error opening and zooming Audacity:-" + ex.Message);
            }
        }

        /// <summary>
        ///     Returns the size of the overlap between the time of the labelled segment and the period defined by the Tuple.  The
        ///     Tuple is defined on the basis of noon-noon, so all times have 12hrs subtracted from them to normalise to midnight
        ///     to
        ///     midnight for the comparisons removing any complications due to days overlapping.
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="samplePeriod"></param>
        /// <returns></returns>
        internal static int SegmentOverlap(LabelledSegment seg, Tuple<DateTime, DateTime> samplePeriod)
        {
            var tDay = new TimeSpan(12, 0, 0);

            var normalisedSegmentStart = (seg.Recording.RecordingStartTime ?? new TimeSpan()) - tDay + seg.StartOffset;
            var normalisedSegmentEnd = (seg.Recording.RecordingStartTime ?? new TimeSpan()) - tDay + seg.EndOffset;

            var normalisedSampleStart = samplePeriod.Item1.TimeOfDay - tDay;
            var normalisedSampleEnd = samplePeriod.Item2.TimeOfDay - tDay;

            var overlap = (normalisedSegmentEnd < normalisedSampleEnd ? normalisedSegmentEnd : normalisedSampleEnd) -
                          (normalisedSegmentStart > normalisedSampleStart
                              ? normalisedSegmentStart
                              : normalisedSampleStart);
            if (overlap.TotalMinutes < 0) overlap = new TimeSpan();
            return (int)Math.Ceiling(overlap.TotalMinutes);
        }

        internal static string SelectWavFileFolder(string initialDirectory)
        {
            string FolderPath = "";

            if (string.IsNullOrWhiteSpace(initialDirectory) || !Directory.Exists(initialDirectory))
            {
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            //using (System.Windows.Forms.OpenFileDialog dialog = new OpenFileDialog())
            //using(Ookii.Dialogs.Wpf.VistaOpenFileDialog dialog=new VistaOpenFileDialog())
            //{
            using (var dialog = new OpenFileDialog
            {
                DefaultExt = ".wav",
                Filter = "Text files (*.txt)|*.txt|Wav files (*.wav)|*.wav|All Files (*.*)|*.*",
                FilterIndex = 3,
                InitialDirectory = initialDirectory,
                Title = "Select Folder or WAV file",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            })
            {
                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == DialogResult.OK)
                    //HeaderFileName = dialog.FileName;
                    FolderPath = Path.GetDirectoryName(dialog.FileName);
                //FolderPath = Tools.GetPath(dialog.FileName);
                //FolderPath = Path.GetDirectoryName(dialog.FileName);
                else
                    return null;
            }

            return (FolderPath);
        }

        /// <summary>
        ///     changes the folder icon for the specified folder to a folder symbol with a green tick
        ///     The change may or may not be apparent until a reboot
        /// </summary>
        /// <param name="workingFolder"></param>
        internal static void SetFolderIconTick(string workingFolder)
        {
            SetFolderIcon(workingFolder, @"C:\Windows\system32\SHELL32.dll,144",
                "Data Imported to Bat Recording Manager");
        }

        /// <summary>
        ///     Valids the coordinates as GPS lat and long in text format and returns those
        ///     coordinates as a Location or null if they are not valid
        /// </summary>
        /// <param name="latit">
        ///     The latitude
        /// </param>
        /// <param name="longit">
        ///     The longitude
        /// </param>
        /// <returns>
        /// </returns>
        internal static Location ValidCoordinates(string latit, string longit)
        {
            Location result = null;
            if (!string.IsNullOrWhiteSpace(latit) && !string.IsNullOrWhiteSpace(longit))
            {
                double.TryParse(latit, out var dLat);
                double.TryParse(longit, out var dlong);
                result = ValidCoordinates(new Location(dLat, dlong));
            }

            return result;
        }

        /// <summary>
        ///     Valids the coordinates in the location as valid GPS coordinates and returns the valid
        ///     Location or null if they are not valid.
        /// </summary>
        /// <param name="location">
        ///     The last selected location.
        /// </param>
        /// <returns>
        /// </returns>
        internal static Location ValidCoordinates(Location location)
        {
            Location result = null;
            if (location != null)
                if (Math.Abs(location.Latitude) <= 90.0d && Math.Abs(location.Longitude) <= 180.0d)
                    result = location;
            return result;
        }

        private static readonly bool HasErred = false;

        private static bool isFirstError = true;

        /// <summary>
        ///     Given a Process in which Audacity is running, and an Input Simulator, sends keyboard commands to
        ///     Audacity to create a new label track with the name of the text file after removing the extension.
        ///     CTRL-SHIFT-N    Clear selection
        ///     CTRL-SHIFT-B    Create Label Track
        ///     CTRL-SHIFT-M    Open Track menu
        ///     N               Select Name from Menu
        ///     trackname       Enter the new track namw
        ///     RETURN          Accept the name and close the dialog
        /// </summary>
        /// <param name="externalProcess"></param>
        /// <param name="ipSim"></param>
        /// <param name="textFileName"></param>
        /// <returns></returns>
        private static bool CreateAudacityLabelFile(Process externalProcess, InputSimulator ipSim, string textFileName, StreamReader sr = null, StreamWriter sw = null)
        {
            if (externalProcess == null || ipSim == null || externalProcess.HasExited) return false;
            var epHandle = externalProcess.MainWindowHandle;
            if (epHandle == (IntPtr)0L) return false;
            var result = true;
            var bareFileName = "LabelTrack";
            if (string.IsNullOrWhiteSpace(textFileName))
            {
                bareFileName = "LabelTrack";
            }
            else
            {
                if (textFileName.ToUpper().EndsWith(".TXT"))
                    bareFileName = textFileName.ExtractFilename(".txt");
                else if (textFileName.ToUpper().EndsWith(".WAV")) bareFileName = textFileName.ExtractFilename(".wav");
            }

            try
            {
                SetForegroundWindow(epHandle);

                #region using Pipes

                string command = $"SelectNone:\n";
                DoPipeCommand(sr, sw, command);
                command = $"NewLabelTrack:\n";
                DoPipeCommand(sr, sw, command);
                command = $"SetTrack:Name=\"{bareFileName}\"\n";
                DoPipeCommand(sr, sw, command);
                command = $"SelectTracks:Mode=\"Set\" Track=\"0\" TrackCount=\"1\"\n";
                DoPipeCommand(sr, sw, command);
                command = $"SelectNone:\n";
                DoPipeCommand(sr, sw, command);
                return (true);

                #endregion using Pipes
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error trying to create audacity label file:- " + ex.Message);
                ErrorLog("Error trying to create label file in Audacity:-" + ex.Message);
                result = false;
            }

            return result;
        }

        private static string DoPipeCommand(StreamReader sr, StreamWriter sw, string command)
        {
            Debug.WriteLine($"Sent: {command}");
            sw.Write(command);
            sr.DiscardBufferedData();
            sw.Flush();

            string line;
            string result = "";
            do
            {
                line = sr.ReadLine();
                Debug.WriteLine($"Recvd:- <{line}>");
                result = result + line;
            } while (!string.IsNullOrWhiteSpace(line));
            return (result);
        }

        private static void ExternalProcess_Exited(object sender, EventArgs e)
        {
            Debug.WriteLine("************** Process Exited *****************");
        }

        /// <summary>
        ///     Looks for a copy of Audacity in a subfolder of the current folder and if found
        ///     returns the fully qualified name of the executabe file, trimmed
        /// </summary>
        /// <returns></returns>
        private static string FindAudacity()
        {
            var folder = @"C:\audacity-win-portable\"; //Audacity installation folder used by InnoScript installer
            var file = "audacity.exe";
            if (Directory.Exists(folder))
                if (File.Exists(folder + file))
                    return folder + file;
            var subdirs = Directory.GetDirectories(System.Windows.Forms.Application.ExecutablePath.Substring(0,
                System.Windows.Forms.Application.ExecutablePath.LastIndexOf(@"\")));
            foreach (var rawsubdir in subdirs)
            {
                var subdir = rawsubdir.Trim();
                if (!subdir.EndsWith(@"\")) subdir = subdir + @"\";
                Debug.WriteLine(subdir);
                if (subdir.ToUpper().Contains("AUDACITY"))
                    if (File.Exists(subdir + file))
                        return subdir + file;
            }

            return "";
        }

        /// <summary>
        /// Examines file metadata for a guano or wamd section which includes information about
        /// the time the file was originally recorded.
        /// Returns the earliest of the guano or wamd timestamps or returns Now;
        /// </summary>
        /// <param name="wavfile"></param>
        /// <returns></returns>
        private static DateTime GetDateTimeFromMetaData(string wavfile, out TimeSpan duration, out string textHeader)
        {
            DateTime result = DateTime.Now;
            duration = new TimeSpan();
            DateTime guanoTime = result;
            DateTime wamdTime = result;
            textHeader = "";

            if (wavfile.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && File.Exists(wavfile))
            {
                using (var wfr = new WaveFileReader(wavfile))
                {
                    var metadata = wfr.ExtraChunks;
                    foreach (var md in metadata)
                    {
                        if (md.IdentifierAsString == "guan")
                        {
                            guanoTime = ReadGuanoTimeStamp(wfr, md, out duration);
                            if (guanoTime.Year < 2000) guanoTime = DateTime.Now;
                        }
                        else if (md.IdentifierAsString == "wamd")
                        {
                            wamdTime = ReadWAMDTimeStamp(wfr, md, out duration);
                            if (wamdTime.Year < 2000) wamdTime = DateTime.Now;
                        }
                    }
                }
                if (result > guanoTime) result = guanoTime;
                if (result > wamdTime) result = wamdTime;
            }
            else
            {
                if (wavfile != null)
                {
                    wavfile = Path.ChangeExtension(wavfile, ".zc");
                    if (File.Exists(wavfile))
                    {
                        ZcMetadata zcMetadata = new ZcMetadata(wavfile);
                        result = zcMetadata.GetTimeAndDuration(out duration, out textHeader);
                    }
                }
            }

            return (result);
        }

        /// <summary>
        ///     Given a .wav filename (or indeed any other filename) replaces the last four characters
        ///     of the name with .txt and returns that modified string.  Does not do any explicit checks to see
        ///     if the string passed is indeed a filename, with or without a path.
        ///     If the input string is null, empty or less than 4 characters long then the function returns
        ///     an unmodified string
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        private static string GetMatchingTextFile(string wavFile)
        {
            if (string.IsNullOrWhiteSpace(wavFile) || wavFile.Length < 4) return wavFile;
            wavFile = wavFile.Substring(0, wavFile.Length - 4);
            wavFile = wavFile + ".txt";
            return wavFile;
        }

        /// <summary>
        ///     Given a fully qualified file name, returns the fully qualified name
        ///     of the oldest .wav file in the same folder, based on the last modified date.
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        private static string GetOldestFile(string wavFile)
        {
            var folder = GetPath(wavFile);
            if (!Directory.Exists(folder)) return null;
            var fileList = Directory.EnumerateFiles(folder, "*.wav");
            //var FILEList= Directory.EnumerateFiles(folder, "*.WAV");
            //fileList = fileList.Concat<string>(FILEList);
            var earliestDate = DateTime.Now;
            var file = "";
            foreach (var f in fileList)
            {
                var thisDate = File.GetLastWriteTime(f);
                if (thisDate < earliestDate)
                {
                    file = f;
                    earliestDate = thisDate;
                }
            }

            return file;
        }

        private static void MainWindowFocus()
        {
            try
            {
                var mw = Application.Current.MainWindow;
                if (mw != null)
                {
                    mw.Dispatcher.Invoke(delegate
                    {
                        mw.Focus();
                    });
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        ///     Opens an existing text file as a label file by sending keyboard commands to Audacity using the supplied
        ///     InputSimulator.  Audacity is running in the provided Process.
        ///     ALT-F       Open file menu
        ///     I           Select Import
        ///     L           Select Labels, opens a file dialog
        ///     textfilename    Type in the name of the text file
        ///     RETURN      Accept and close the dialog
        ///     UP          Move focus back to the audio track
        /// </summary>
        /// <param name="externalProcess"></param>
        /// <param name="ipSim"></param>
        /// <param name="textFileName"></param>
        /// <returns></returns>
        private static bool OpenAudacityLabelFile(Process externalProcess, InputSimulator ipSim, string textFileName, StreamReader sr, StreamWriter sw)
        {
            sw.Write("ImportLabels:\n");
            sr.DiscardBufferedData();
            sw.Flush();
            Thread.Sleep(3000);
            ipSim.Keyboard.TextEntry($"\"{textFileName}\"");
            Thread.Sleep(100);
            ipSim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(100);
            string line;
            do { line = sr.ReadLine(); Debug.WriteLine($"Recvd:- <{line}>"); } while (!string.IsNullOrWhiteSpace(line));
            return (true);
        }

        /// <summary>
        /// extracts the timestamp from a guano metadata chunk
        /// </summary>
        /// <param name="wfr"></param>
        /// <param name="md"></param>
        /// <returns></returns>
        private static DateTime ReadGuanoTimeStamp(WaveFileReader wfr, RiffChunk md, out TimeSpan duration)
        {
            DateTime result = DateTime.Now;
            duration = new TimeSpan();
            var chunk = wfr.GetChunkData(md);
            string guanoChunk = System.Text.Encoding.UTF8.GetString(chunk);
            var lines = guanoChunk.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Timestamp"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        if (DateTime.TryParse(parts[1], out DateTime dt))
                        {
                            result = dt;
                        }
                    }
                }
                if (line.Contains("Length"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        if (int.TryParse(parts[1], out int seconds))
                        {
                            duration = TimeSpan.FromSeconds(seconds);
                        }
                    }
                }
            }
            return (result);
        }

        /// <summary>
        /// extracts the timestamp from a wamd metadata chunk
        /// </summary>
        /// <param name="wfr"></param>
        /// <param name="md"></param>
        /// <returns></returns>
        private static DateTime ReadWAMDTimeStamp(WaveFileReader wfr, RiffChunk md, out TimeSpan duration)
        {
            DateTime result = DateTime.Now;
            duration = TimeSpan.FromSeconds(15);
            var chunk = wfr.GetChunkData(md);

            var entries = new Dictionary<short, string>();

            var bReader = new BinaryReader(new MemoryStream(chunk));

            while (bReader.BaseStream.Position < bReader.BaseStream.Length)
            {
                var type = bReader.ReadInt16(); // 01 00
                var size = bReader.ReadInt32(); // 03 00 00 00
                var bData = bReader.ReadBytes(size);
                if (type > 0)
                    try
                    {
                        var data = System.Text.Encoding.UTF8.GetString(bData);
                        if (type == 0x0005)
                        {
                            var dt = DateTime.Now;
                            if (DateTime.TryParse(data, out dt))
                            {
                                result = dt;
                                if (duration > new TimeSpan()) return (dt);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        Debug.WriteLine(ex);
                    }
            }
            return (result);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void setPipeStrem(out NamedPipeClientStream inStream, out NamedPipeClientStream outStream)
        {
            string toName = @"ToSrvPipe";
            string fromName = @"FromSrvPipe";

            inStream = new NamedPipeClientStream(".", fromName, PipeDirection.In);
            inStream.Connect();
            outStream = new NamedPipeClientStream(".", toName, PipeDirection.Out);
            outStream.Connect();
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint SHGetSetFolderCustomSettings(ref Lpshfoldercustomsettings pfcs, string pszPath,
                    uint dwReadWrite);

        [DllImport("user32")]
        private static extern bool ShowWindowAsync(IntPtr hwnd, int a);

        private static void startAudacityWithPipes(StreamReader sr, StreamWriter sw, string fQWavFileName, InputSimulator ipSime)
        {
            string command = $"MultiTool:\n";
            DoPipeCommand(sr, sw, command);
            command = $"Import2: Filename=\"{fQWavFileName}\"\n";
            DoPipeCommand(sr, sw, command);
        }

        /// <summary>
        /// Zooms Audacity using Pipes
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="streamReader"></param>
        /// <param name="streamWriter"></param>
        private static void ZoomAudacity(int start, int end, StreamReader streamReader, StreamWriter streamWriter)
        {
            String command = $"SelectTime:Start=\"{start}\" End=\"{end}\" RelativeTo=\"ProjectStart\"\n";
            DoPipeCommand(streamReader, streamWriter, command);
            command = $"ZoomSel:\n";
            DoPipeCommand(streamReader, streamWriter, command);
            command = $"SelStart:\n";
            DoPipeCommand(streamReader, streamWriter, command);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        /*
                internal static void OpenWavFile(Recording selectedRecording)
                {
                    if (selectedRecording?.RecordingSession == null) return;
                    var folder = selectedRecording.RecordingSession.OriginalFilePath;
                    if (string.IsNullOrWhiteSpace(folder)) return;
                    folder = folder.Trim();

                    if (!Directory.Exists(folder))
                        // try to find the folder on a different drive if necessary
                        if (folder[1] == ':')
                        {
                            // then the folder name starts with a drive letter - almost definite
                            var drivelessFolder = folder.Substring(2);
                            if (drivelessFolder.StartsWith(@"\")) drivelessFolder = drivelessFolder.Substring(1);
                            if (!drivelessFolder.EndsWith(@"\")) drivelessFolder = drivelessFolder + @"\";

                            var allDrives = DriveInfo.GetDrives();
                            foreach (var drive in allDrives)
                                if (Directory.Exists(drive.Name + drivelessFolder))
                                {
                                    folder = drive.Name + drivelessFolder;
                                    break;
                                }

                            if (folder[1] != ':') return; // we didn't find a drive with the folder path so give up
                        }

                    if (!Directory.Exists(folder))
                    {
                        // if after trying the folder still doesnt exist, give up
                        return;
                    }

                    if (selectedRecording.RecordingName.StartsWith(@"\"))
                        selectedRecording.RecordingName = selectedRecording.RecordingName.Substring(1);
                    folder = folder + @"\" + selectedRecording.RecordingName;
                    OpenWavFile(folder);
                }*/

        private static void ZoomAudacity(double start, double end, InputSimulator ipSim)
        {
            if (File.Exists(macroFileName))
            {
                File.Delete(macroFileName);
            }
            string[] macro =
            {
                        "SelectNone",
                        $"Select:End=\"{end}\" Mode=\"Set\" Start=\"{start}\" Track=\"0\" TrackCount=\"1\"",
                        "ZoomSel:",
                        "SelectNone:"//,
                        //$"Import2:Filename=\"{textFileName}\"",
                        //"SelectTracks:Mode=\"Set\" Track=\"0\" TrackCount=\"1\""
                    };
            File.WriteAllLines(macroFileName, macro);
            ipSim.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.LMENU, VirtualKeyCode.LSHIFT }, VirtualKeyCode.VK_B);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        /*
        /// <summary>
        ///     Assumes that a filename may include the date in the format yyyymmdd
        ///     preceded and followed by either - or _
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static DateTime? GetDateFromFilename(string fileName)
        {
            DateTime? result = null;
            if (string.IsNullOrWhiteSpace(fileName)) return result;

            var pattern = @"[_-]([0-9]{4}).?([0-9]{2}).?([0-9]{2})[_-]";
            var match = Regex.Match(fileName, pattern);
            if (match.Success)
            {
                var year = -1;
                var month = -1;
                var day = -1;

                if (match.Groups.Count > 3)
                {
                    int.TryParse(match.Groups[1].Value, out year);
                    int.TryParse(match.Groups[2].Value, out month);
                    int.TryParse(match.Groups[3].Value, out day);
                }

                if (year > 1970 && month >= 0 && month <= 12 && day >= 0 && day <= 31)
                {
                    result = new DateTime(year, month, day);

                    var hour = -1;
                    var minute = -1;
                    var secs = -1;
                    pattern = @"[_-]([0-9]{4}).?([0-9]{2}).?([0-9]{2})[_-]([0-9]{2}).?([0-9]{2}).?([0-9]{2})";
                    match = Regex.Match(fileName, pattern);
                    if (match.Success && match.Groups.Count > 6)
                    {
                        int.TryParse(match.Groups[4].Value, out hour);
                        int.TryParse(match.Groups[5].Value, out minute);
                        int.TryParse(match.Groups[6].Value, out secs);
                        if (hour >= 0 && hour <= 24 && minute >= 0 && minute <= 60 && secs >= 0 && secs <= 60)
                            result = new DateTime(year, month, day, hour, minute, secs);
                    }
                }
            }

            return result;
        }*/
    } // end of Class Tools

    //########################################################################################################################
    //########################################################################################################################

    /// <summary>
    ///     static functions to operate on visual UI elements
    /// </summary>
    public static class UiHelper
    {
        /// <summary>
        ///     Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the queried item.</param>
        /// <returns>
        ///     The first parent item that matches the submitted type parameter.
        ///     If not matching item can be found, a null reference is being returned.
        /// </returns>
        public static T FindVisualParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            // get parent item
            var parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            if (parentObject is T parent)
                return parent;
            return FindVisualParent<T>(parentObject);
        }
    }

    public static class UiServices
    {
        /// <summary>
        /// Sets the busystate as busy.
        /// </summary>
        public static void SetBusyState()
        {
            SetBusyState(true);
        }

        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        private static bool IsBusy;

        /// <summary>
        /// Handles the Tick event of the dispatcherTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var dispatcherTimer = sender as DispatcherTimer;
            if (dispatcherTimer != null)
            {
                SetBusyState(false);
                dispatcherTimer.Stop();
            }
        }

        /// <summary>
        /// Sets the busystate to busy or not busy.
        /// </summary>
        /// <param name="busy">if set to <c>true</c> the application is now busy.</param>
        private static void SetBusyState(bool busy, [CallerMemberName] string caller = null, [CallerLineNumber] int linenumber = 0)
        {
            if (busy != IsBusy)
            {
                IsBusy = busy;
                if (Mouse.OverrideCursor == null)
                {
                    var mw = (App.Current.MainWindow as MainWindow);
                    if (mw != null)
                    {
                        mw.Dispatcher.Invoke(delegate
                        {
                            Mouse.OverrideCursor = busy ? Cursors.Wait : null;
                            Debug.WriteLine(
                                $"%%%%%%%%%%%%%%%%%%%%%%%%%    busy={busy} - from {caller} at {linenumber} - {DateTime.Now.ToLongTimeString()}");
                        });
                    }
                }

                if (IsBusy)
                {
                    new DispatcherTimer(TimeSpan.FromSeconds(0), DispatcherPriority.ApplicationIdle, dispatcherTimer_Tick, Application.Current.Dispatcher);
                }
            }
        }
    }

    /// <summary>
    /// Uses a converterparameter to increase or decrease the numerical (double) value in the object
    /// </summary>
    public class AddValueConverter : IValueConverter

    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                double? dValue = value as double?;
                // Here's where you put the code to handle the value conversion.
                double.TryParse(parameter as string, out var factor);
                if (double.IsNaN(dValue ?? double.NaN) || (dValue ?? 0.0d) < 0.0d)
                {
                    dValue = 0.0d;
                }
                var result = (double)(dValue ?? 0.0d) + factor;
                if (result < 0.0d) result = 0.0d;
                return (result);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// convertback not implemented
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    public class BSPassesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value is BatStats)
                {
                    BatStats bs = value as BatStats;
                    string result = bs.passes + "/" + bs.segments;
                    return (result);
                }

                return (" - ");
            }
            catch
            {
                return "ERR";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    public class DebugBreak : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                //Debug.WriteLine("&&& DebugBreakConverter:- " + value == null ? "null" : (value.ToString()));
                return value;
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    /// format converter for decimal values to and from a formatted string, format
    /// specified in the parameter field without the curly braces
    /// </summary>
    public class DecimalToStringConverter : IValueConverter

    {
        /// <summary>
        /// converter
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                if (value == null) return ("");
                decimal dblValue = (decimal)value;

                string format = "{0.00}";
                if (!string.IsNullOrWhiteSpace((parameter as string)))
                {
                    format = "{" + (parameter as string) + "}";
                }
                // Here's where you put the code do handle the value conversion.
                var str = "";

                str = string.Format(format, dblValue);

                return str;
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// unconverter
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)

        {
            decimal.TryParse((string)value, out var d);

            return d;
        }
    }

    public class DivideConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                if (value != null && parameter != null)
                {
                    var val = (double)value;
                    var parm = (double)parameter;
                    return val / parm;
                }

                return (double)value / 2;
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    public class DoubleStringConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                double dblValue = (double)value;

                string format = "{0.00}";
                if (!string.IsNullOrWhiteSpace((parameter as string)))
                {
                    format = "{" + (parameter as string) + "}";
                }
                // Here's where you put the code do handle the value conversion.
                var str = "";

                str = string.Format(format, dblValue);

                return str;
            }
            catch
            {
                return value.ToString();
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            double.TryParse((string)value, out var d);
            if (d < 0) return null;

            return d;
        }
    }

    #region TimeSpanDateConverter (ValueConverter)

    /// <summary>
    /// Converter to return a red brush if the directory in value does not exist and a black brush if it does or in the event of any error
    /// </summary>
    public class FilePathBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value is string)
                {
                    string folder = value as string;
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        if (Directory.Exists(folder))
                        {
                            return (new SolidColorBrush(Colors.Black));
                        }
                    }
                }
            }
            catch
            {
                return new SolidColorBrush(Colors.Red);
            }

            return (new SolidColorBrush(Colors.Red));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    public class GPSConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value is RecordingSession)
                {
                    RecordingSession session = value as RecordingSession;
                    return (session.LocationGPSLatitude.ToString() + ", " + session.LocationGPSLongitude.ToString());
                }

                return ("");
            }
            catch
            {
                return "ERR";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Used to set the height of a scale grid inside a canvas of variable size.
    ///     The converter is passed to bound values the height of the parent canvas and a
    ///     scale factor.  it returns a value of the height multiplied by the scale factor.
    /// </summary>
    public class GridScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0.0d as double?;
            if (values.Length == 1) return values[0] as double?;
            var height = values[0] as double?;
            var scale = values[1] as double?;
            return height * scale;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     ImageConverter converts either a Bitmap or a BitmapImage to a BitmapSource suitable for
    ///     display in a wpf Image control.  The BitmapImage is first converted to a Bitmap then to
    ///     a BitmapSource.
    /// </summary>
    public class ImageConverter : IValueConverter
    {
        //[DllImport("gdi32.dll")]
        //private static extern bool DeleteObject(IntPtr hObject);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                //<summary>
                // Converts System.Drawing.Bitmap to BitmapSource
                //</summary>
                //
                if (value == null) return null;

                var bmp = new Bitmap(10, 10);
                //No conversion to be done if value is null

                if (value is BitmapImage)
                    try
                    {
                        var bmi = value as BitmapImage;
                        return bmi;

                        /*
                        using (var stream = new MemoryStream())
                        {
                            BitmapEncoder enc = new BmpBitmapEncoder();
                            try
                            {
                                // NB Known to throw a SystemNotSupported Exception which is not an error but a WPF 'Feature'
                                enc.Frames.Add(BitmapFrame.Create(bmi));
                            }catch(Exception)
                            {
                                return (null);
                            }
                            enc.Save(stream);
                            bmp = new Bitmap(stream);
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        return null;
                    }

                //Validate object being converted
                if (value is Bitmap)
                {
                    if (value == null) value = new Bitmap(10, 10);
                    bmp = value as Bitmap;
                }

                return bmp?.ToBitmapSource();
                /*IntPtr HBitmap = bmp.GetHbitmap();
                    try
                    {
                        System.Windows.Media.Imaging.BitmapSizeOptions sizeOptions =
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions();

                        BitmapSource bmps= System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            HBitmap, IntPtr.Zero, Int32Rect.Empty, sizeOptions);
                        return (bmps);
                    }finally
                    {
                        DeleteObject(HBitmap);
                    }
                    */
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    public class MapRefConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value is RecordingSession)
                {
                    RecordingSession session = value as RecordingSession;

                    if (session.hasGPSLocation)
                    {
                        var lat = (double)session.LocationGPSLatitude;
                        var longit = (double)session.LocationGPSLongitude;
                        var gridRef = GPSLocation.ConvertGPStoGridRef(lat, longit);
                        return (gridRef);
                    }
                    else
                    {
                        return (" - ");
                    }
                }

                return (" - ");
            }
            catch
            {
                return "ERR";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter class for scaling height or width of an image
    /// </summary>
    public class MultiscaleConverter : IMultiValueConverter

    {
        /// <summary>
        ///     Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (values != null && values.Length >= 2)
                {
                    var height = 1000.0d;
                    var factor = 1.0d;

                    if (values[0] is string)
                    {
                        var strHeight = values[0] == null ? string.Empty : values[0].ToString();
                        double.TryParse(strHeight, out height);
                    }

                    if (values[0] is double) height = ((double?)values[0]).Value;

                    if (values[1] is string)
                    {
                        var strFactor = values[1] == null ? string.Empty : values[1].ToString();
                        double.TryParse(strFactor, out factor);
                    }
                    else if (values[1] is double) factor = ((double?)values[1]).Value;
                    else if (values[1] is int) factor = ((double?)values[1]).Value;

                    return height * factor;
                }

                return 1000.0d;
            }
            catch
            {
                return 1000.0d;
            }
        }

        /// <summary>
        ///     Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converts a LabelledSegment into a Text form as 'start - end comment'
    ///     and appends an asterisk if the segnent has associated images
    /// </summary>
    public class SegmentToTextConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "";
                var segment = value as LabelledSegment;
                var result = Tools.FormattedTimeSpan(segment.StartOffset) + " - " +
                             Tools.FormattedTimeSpan(segment.EndOffset) +
                             "  " + segment.Comment;
                while (result.Trim().EndsWith("*")) result = result.Substring(0, result.Length - 1);
                var pattern = @"\(\s*[0-9]*\s*images?\s*\)";
                result = Regex.Replace(result, pattern, "");
                result = result.Trim();
                if (segment.SegmentDatas.Count > 0) result = result + " (" + segment.SegmentDatas.Count + " images )";
                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            var modifiedSegment = new LabelledSegment { Comment = text };

            return modifiedSegment;
        }
    }

    public class SessionEndDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value is RecordingSession)
                {
                    RecordingSession session = value as RecordingSession;
                    string result = ((session.EndDate ?? session.SessionDate).Date +
                                     (session.SessionEndTime ?? new TimeSpan(23, 59, 0))).ToString();
                    return (result);
                }

                return (" - ");
            }
            catch
            {
                return "ERR";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    public class SessionStartDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value is RecordingSession)
                {
                    RecordingSession session = value as RecordingSession;
                    string result = (session.SessionDate.Date +
                                     (session.SessionStartTime ?? new TimeSpan(18, 0, 0))).ToString();
                    return (result);
                }

                return (" - ");
            }
            catch
            {
                return "ERR";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converts a nullable DateTime to a short date string safely even for null values
    /// </summary>
    public class ShortDateConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var dateToDisplay = value as DateTime? ?? DateTime.Now;
                return dateToDisplay.ToShortDateString();
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            var result = new DateTime();
            DateTime.TryParse(text, out result);
            return result;
        }
    }

    /// <summary>
    ///     Converts a nullable DateTime to a short date string safely even for null values
    /// </summary>
    public class ShortTimeConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.

                if (value as TimeSpan? == null) return "--:--:--";

                var timeToDisplay = value as TimeSpan? ?? new TimeSpan();

                return timeToDisplay.ToString(@"hh\hmm\mss\s");
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            var result = new DateTime();
            DateTime.TryParse(text, out result);
            return result;
        }
    }

    public class TextColourConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                if (value is LabelledSegment)
                {
                    LabelledSegment seg = value as LabelledSegment;
                    if (seg.isConfidenceLow)
                    {
                        return new SolidColorBrush(System.Windows.Media.Colors.LightPink);
                    }

                    string text = seg.Comment.Trim();
                    //String text = (value as LabelledSegment).Comment;

                    if (string.IsNullOrWhiteSpace(text)) return new SolidColorBrush(System.Windows.Media.Colors.LightCyan);
                    if (text.EndsWith("H"))
                        return new SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                    if (text.EndsWith("M"))
                        return new SolidColorBrush(System.Windows.Media.Colors.LightGoldenrodYellow);
                    if (text.EndsWith("L")) return new SolidColorBrush(System.Windows.Media.Colors.LightPink);
                    return new SolidColorBrush(System.Windows.Media.Colors.LightCyan);
                }
            }
            catch
            {
                return new SolidColorBrush(System.Windows.Media.Colors.LightCyan);
            }
            return new SolidColorBrush(System.Windows.Media.Colors.LightCyan);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    public class Times2Converter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                double.TryParse(parameter as string, out var factor);
                return (double)value * factor;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converts a nullable Timespan into a DateTime of the same number of ticks, or a
    ///     DateTime.Now if it is null
    /// </summary>
    public class TimeSpanDateConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return DateTime.Now;
                var time = value as TimeSpan? ?? new TimeSpan();
                var result = new DateTime(time.Ticks);
                return result;
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = new TimeSpan((value as DateTime? ?? DateTime.Now).Ticks);
            return result;
        }
    }

    #endregion TimeSpanDateConverter (ValueConverter)

    #region TextColourConverter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #endregion TextColourConverter (ValueConverter)

    #region DebugBreak (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #endregion DebugBreak (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Universal wait cursor class
    /// </summary>
    public class WaitCursor : IDisposable
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        /// <summary>
        /// creates and displays a wait cursor which will revert when the class instance is disposed.
        /// Allows for nested calls.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="caller"></param>
        /// <param name="linenumber"></param>
        public WaitCursor(string status = "null", [CallerMemberName] string caller = null, [CallerLineNumber] int linenumber = 0)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                /* if (status != "null")
                 {
                     //(App.Current.MainWindow as MainWindow).Dispatcher.Invoke((Action)delegate
                     //var currentMainWindow = Application.Current.MainWindow;
                     //MainWindow window = (currentMainWindow as MainWindow);
                     //window.Dispatcher.Invoke(delegate
                     //{
                         Debug.WriteLine("-=-=-=-=-=-=-=-=- "+status+" -=-=-=-=-=-=-=-=-");
                         _oldStatus = MainWindow.SetStatusText(status);
                         //Debug.WriteLine("old Status=" + oldStatus);
                         _previousCursor = Mouse.OverrideCursor;
                         //Debug.WriteLine("old cursor saved");
                         Mouse.OverrideCursor = Cursors.Wait;
                         //Debug.WriteLine("Wait cursor set");

                     //});
                 }
                 else
                 {*/

                if (Mouse.OverrideCursor == null)
                {
                    var mw = (App.Current.MainWindow as MainWindow);
                    if (mw != null)
                    {
                        mw.Dispatcher.Invoke(delegate
                        {
                            _previousCursor = Mouse.OverrideCursor;
                            Mouse.OverrideCursor = Cursors.Wait;
                            Debug.WriteLine(
                                $"%%%%%%%%%%%%%%%%%%%%%%%%%    WAIT - from {caller} at {linenumber} - {DateTime.Now.ToLongTimeString()}");
                        });
                    }
                }
                else
                {
                    Depth = 1;
                    Debug.WriteLine($"No wait cursor set from {caller}");
                }

                //Application.Current.MainWindow.Dispatcher.InvokeAsync(() => { Mouse.OverrideCursor = _previousCursor; },
                //System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("%%%%%%%%%%%%%%%%%  WaitCursor failed for \"" + status + "\":-" + ex.Message);
            }
        }

        public void Dispose()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool all)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                if (Depth == 0)
                {
                    var mw = (App.Current.MainWindow as MainWindow);
                    if (mw != null)
                    {
                        mw.Dispatcher.Invoke(delegate
                        {
                            //Mouse.OverrideCursor = _previousCursor ?? Cursors.Arrow;
                            Mouse.OverrideCursor = null;
                            Debug.WriteLine(
                                $"%-%-%-%-%-%-%_%-%-%-%-%-%-- RESUME {Mouse.OverrideCursor} at {DateTime.Now.ToLongTimeString()}");
                        });
                    }
                    else
                    {
                        Debug.WriteLine("No Main Window, failed to reset cursor");
                    }
                }
                else
                {
                    Debug.WriteLine("No cursor reset");
                }
                /*
                if (_oldStatus != "null")
                    //(App.Current.MainWindow as MainWindow).Dispatcher.Invoke((Action)delegate

                    MainWindow.SetStatusText(_oldStatus);*/
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error disposing of wait cursor:- " + ex.Message);
            }
        }

        private readonly string _oldStatus = "null";
        private readonly int Depth = 0;
        private Cursor _previousCursor = Cursors.Arrow;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    }

    #region DivideConverter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #endregion DivideConverter (ValueConverter)

    #region Times2Converter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #endregion Times2Converter (ValueConverter)

    #region HGridLineConverter (ValueConverter)

    /// <summary>
    ///     Converts a LabelledSegment instance to an intelligible string for display
    /// </summary>
    public class BatCallConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var result = new Call();
                if (value is LabelledSegment segment)
                {
                    result = DBAccess.GetSegmentCall(segment) ?? new Call();
                }

                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter class for scaling height or width of an image
    ///     It is passed the location of the line in the stored image and a copy of
    ///     the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class BottomMarginConverter : IMultiValueConverter

    {
        /// <summary>
        ///     Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                var width = values[0] as double?;
                var height = values[1] as double?;

                if (width != null && height != null && values[2] is StoredImage si)
                {
                    //Debug.WriteLine("============================================================================================");
                    var hscale = width.Value / si.image.Width;
                    var vscale = height.Value / si.image.Height;
                    var actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - si.image.Width * actualScale);
                    var topAndBottomMargins = Math.Abs(height.Value - si.image.Height * actualScale);

                    return topAndBottomMargins / 2 + si.image.Height * actualScale;
                }

                return 0.0d;
            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        ///     Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter to get the number of images associated with a bat and return that value
    ///     as a string for display in a DataItem Text Column
    /// </summary>
    public class ConvertGetNumberOfImages : IValueConverter
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value != null)
                {
                    var bat = value as Bat;
                    var cnt = 0;
                    if (bat.BatPictures != null) cnt = bat.BatPictures.Count;
                    return cnt.ToString();
                }

                return "-";
            }
            catch
            {
                return "-";
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter class for scaling height or width of an image
    ///     It is passed the location of the line in the stored image and a copy of
    ///     the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class HGridLineConverter : IMultiValueConverter

    {
        /// <summary>
        ///     Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                int.TryParse(values[0] as string, out var indexToGridline);
                var width = values[1] as double?;
                var height = values[2] as double?;

                var si = values[3] as StoredImage;
                //Debug.WriteLine("HGridLineConverter:- storedValue=" + si.HorizontalGridlines[indexToGridline]);

                if (width == null || height == null || indexToGridline < 0 ||
                    indexToGridline >= si.HorizontalGridlines.Count || si == null) return null;

                var displayPosition =
                    DisplayStoredImageControl.FindHScaleProportion(si.HorizontalGridlines[indexToGridline], width.Value,
                        height.Value, si) * height;
                //Debug.WriteLine("      DisplayedPosition=" + displayPosition);

                return displayPosition;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HGridLineConverter error:- " + ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion HGridLineConverter (ValueConverter)

    #region VGridLineConverter (ValueConverter)

    /// <summary>
    ///     converter getting images for all recordings
    /// </summary>
    public class ImagesForAllRecordingsConverter : IMultiValueConverter
    {
        /// <summary>
        ///     converter - takes an array of 2 objects.  object[0] is a BulkObservableCollection
        ///     of Recordings and  object[1] is a bat.  It returns a string representation of the
        ///     number of images in all recordings that include that bat.
        ///     i.e. the number of images linked to labelled segments for these recordings that
        ///     include the named bat
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (values == null || values.Length < 2) return "-";

                var numberOfImages = 0;
                if (values[1] == null) return "-";
                var bat = values[1] as Bat;
                if (!(values[0] is BulkObservableCollection<Recording> recordings) || recordings.Count <= 0) return "-";

                numberOfImages = (from rec in recordings.AsParallel()
                                  from seg in rec.LabelledSegments.AsParallel()
                                  from link in seg.BatSegmentLinks.AsParallel()
                                  where !(link.ByAutoID ?? false) && link.BatID == bat.Id
                                  select seg.SegmentDatas.Count).Sum();

                /*
                foreach(var rec in recordings)
                {
                    if(!rec.LabelledSegments.IsNullOrEmpty())
                    {
                        bool RecordingHasBat = false;
                        numberOfImages+=rec.GetImageCount(bat,out RecordingHasBat);
                    }
                }*/

                return numberOfImages.ToString();
            }
            catch
            {
                return "-";
            }
        }

        /// <summary>
        ///     unconverter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter which takes a StoredImage and returns the image component overlaid with horizontal and vertical
    ///     grid lines as defined in the StoredImage lists.
    /// </summary>
    public class ImageWithGridConverter : IValueConverter
    {
        /// <summary>
        ///     Converter to add the grid lines to the image component and reutrn it
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = new WriteableBitmap(new BitmapImage());
            try
            {
                // Here's where you put the code do handle the value conversion.
                var sImage = value as StoredImage;
                result = new WriteableBitmap(sImage.image);
                return result;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    /// class determines visibility by a boolean - true is hidden, false is visible
    /// </summary>
    public class InVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// converts a boolean to visibility true=hidden false=visible default visible
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value is bool)
                {
                    bool? b = value as bool?;
                    if (b ?? false)
                    {
                        return (Visibility.Hidden);
                    }
                    else
                    {
                        return (Visibility.Visible);
                    }
                }
                return Visibility.Visible;
            }
            catch
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converts a LabelledSegment instance to an intelligible string for display
    /// </summary>
    public class LabelledSegmentConverter : IMultiValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var result = "";
                bool offsets = true;
                LabelledSegment segment = null;
                if (values == null) return (result);
                if (values.Length < 2) return (result);

                if (values[0] != null)
                {
                    if (values[0] is ToggleButton)
                    {
                        var bt = values[0] as ToggleButton;
                        if (bt.IsEnabled)
                        {
                            offsets = !bt.IsChecked ?? true;
                        }
                        else
                        {
                            offsets = true;
                        }
                    }
                }

                if (values[1] is LabelledSegment)
                {
                    segment = (LabelledSegment)values[1];
                }

                if (segment != null)
                {
                    result = Tools.FormattedSegmentLine(segment, offsets);
                    while (result.Trim().EndsWith("*")) result = result.Substring(0, result.Length - 1);
                    result = result.Trim();
                    if (!result.EndsWith(")") && segment.SegmentDatas.Count > 0)
                        result = result + " (" + segment.SegmentDatas.Count + " images )";
                }

                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     ConvertBack not implemented
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter class for scaling height or width of an image
    ///     It is passed the location of the line in the stored image and a copy of
    ///     the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class LeftMarginConverter : IMultiValueConverter

    {
        /// <summary>
        ///     Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                var width = values[0] as double?;
                var height = values[1] as double?;

                if (width != null && height != null && values[2] is StoredImage si)
                {
                    //Debug.WriteLine("============================================================================================");
                    var hscale = width.Value / si.image.Width;
                    var vscale = height.Value / si.image.Height;
                    var actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - si.image.Width * actualScale);
                    var topAndBottomMargins = Math.Abs(height.Value - si.image.Height * actualScale);

                    return rightAndLeftMargins / 2;
                }

                return 0.0d;
            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        ///     Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Is passed an EntitySet of Recordings and calculates the total number of images
    ///     associated with those recordings, returning the value as a string
    /// </summary>
    public class NumberOfImagesConverter : IValueConverter
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value == null) return "0";
                var recordings = value as EntitySet<Recording>;
                if (recordings.Count <= 0) return "0";
                var imgs = 0;

                imgs = (from rec in recordings
                        from seg in rec.LabelledSegments
                        select seg.SegmentDatas.Count).Sum();
                /*
                foreach(var rec in recordings)
                {
                    if(rec.LabelledSegments!=null && rec.LabelledSegments.Count > 0)
                    {
                        foreach(var seg in rec.LabelledSegments)
                        {
                            imgs+=seg.SegmentDatas.Count;
                        }
                    }
                }*/
                return imgs.ToString();
            }
            catch
            {
                return "0";
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter class for scaling height or width of an image
    ///     It is passed the location of the line in the stored image and a copy of
    ///     the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class RightMarginConverter : IMultiValueConverter

    {
        /// <summary>
        ///     Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                var width = values[0] as double?;
                var height = values[1] as double?;

                if (width != null && height != null && values[2] is StoredImage si)
                {
                    //Debug.WriteLine("============================================================================================");
                    var hscale = width.Value / si.image.Width;
                    var vscale = height.Value / si.image.Height;
                    var actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - si.image.Width * actualScale);
                    var topAndBottomMargins = Math.Abs(height.Value - si.image.Height * actualScale);

                    return rightAndLeftMargins / 2 + si.image.Width * actualScale;
                }

                return 0.0d;
            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        ///     Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter class for scaling height or width of an image
    ///     It is passed the location of the line in the stored image and a copy of
    ///     the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class TopMarginConverter : IMultiValueConverter

    {
        /// <summary>
        ///     Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                var width = values[0] as double?;
                var height = values[1] as double?;

                if (width != null && height != null && values[2] is StoredImage si)
                {
                    //Debug.WriteLine("============================================================================================");
                    var hscale = width.Value / si.image.Width;
                    var vscale = height.Value / si.image.Height;
                    var actualScale = Math.Min(hscale, vscale);

                    var rightAndLeftMargins = Math.Abs(width.Value - si.image.Width * actualScale);
                    var topAndBottomMargins = Math.Abs(height.Value - si.image.Height * actualScale);

                    return topAndBottomMargins / 2;
                }

                return 0.0d;
            }
            catch
            {
                return 0.0d;
            }
        }

        /// <summary>
        ///     Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    /// <summary>
    ///     Converter class for scaling height or width of an image
    ///     It is passed the location of the line in the stored image and a copy of
    ///     the displayImageCanvas it is to be written and the storedImage it relates to
    /// </summary>
    public class VGridLineConverter : IMultiValueConverter

    {
        /// <summary>
        ///     Forward scale converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            try
            {
                int.TryParse(values[0] as string, out var indexToGridline);
                var width = values[1] as double?;
                var height = values[2] as double?;

                var si = values[3] as StoredImage;

                if (width == null || height == null || indexToGridline < 0 ||
                    indexToGridline >= si.VerticalGridLines.Count || si == null) return null;

                var displayPosition =
                    DisplayStoredImageControl.FindVScaleProportion(si.VerticalGridLines[indexToGridline], width.Value,
                        height.Value, si) * width;

                return displayPosition;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Reverse converter not used
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion VGridLineConverter (ValueConverter)

    #region VisibilityConverter (ValueConverter)

    /// <summary>
    /// converter class for boolean to visibility
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        /// <summary>
        /// converts a bool to visibility true=visible false=hidden default visible
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value is bool)
                {
                    bool? b = value as bool?;
                    if (b ?? false)
                    {
                        return (Visibility.Visible);
                    }
                    else
                    {
                        return (Visibility.Hidden);
                    }
                }
                return Visibility.Visible;
            }
            catch
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion VisibilityConverter (ValueConverter)

    /// <summary>
    ///     A simple class to accommodate the parsed and analysed contents of the wamd
    ///     metadata chunk from a .wav file.  The data in the chunk is identified by a
    ///     numerical type and contents which should be a string.  The data structure
    ///     holds items for each known type and getters return the contents by name or add
    ///     contents by type.
    /// </summary>
    public class WAMD_Data
    {
        /// <summary>
        ///     initialises the data structure with empty strings throughout
        /// </summary>
        public WAMD_Data()
        {
            model = "";
            version = "";
            header = "";
            timestamp = "";
            source = "";
            note = "";
            identification = "";
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public string comment
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get
            {
                var s = note + " " + identification;
                return s.Trim();
            }
        }

        public double duration { get; set; }
        public string header { get; private set; }
        public string identification { get; private set; }

        public Tuple<short, string> item
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            set
            {
                switch (value.Item1)
                {
                    case 1:
                        model = value.Item2;
                        break;

                    case 3:
                        version = value.Item2;
                        break;

                    case 4:
                        header = value.Item2;
                        break;

                    case 5:
                        timestamp = value.Item2;
                        break;

                    case 12:
                        identification = value.Item2;
                        break;

                    case 10:
                        note = value.Item2;
                        break;
                }
            }
        }

        public string model { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string note { get; private set; }
        public string source { get; }
        public string timestamp { get; private set; }
        public string version { get; private set; }

        public double? versionAsDouble { get; internal set; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct Lpshfoldercustomsettings
    {
        public uint dwSize;
        public uint dwMask;
        public IntPtr pvid;
        public string pszWebViewTemplate;
        public uint cchWebViewTemplate;
        public string pszWebViewTemplateVersion;
        public string pszInfoTip;
        public uint cchInfoTip;
        public IntPtr pclsid;
        public uint dwFlags;
        public string pszIconFile;
        public uint cchIconFile;
        public int iIconIndex;
        public string pszLogo;
        public uint cchLogo;
    }
}