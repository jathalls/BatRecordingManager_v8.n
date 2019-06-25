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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace BatRecordingManager
{
    /// <summary>
    ///     Provides arguments for an event.
    /// </summary>
    [Serializable]
    public class AnalysingEventArgs : EventArgs
    {
        /// <summary>
        ///     default example of event args
        /// </summary>
        public new static readonly AnalysingEventArgs Empty = new AnalysingEventArgs("");


        #region Constructors

        /// <summary>
        ///     Constructs a new instance of the <see cref="AnalysingEventArgs" /> class.
        /// </summary>
        public AnalysingEventArgs(string fileName)
        {
            FileName = fileName;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        ///     string to be used as a caption for the importPictureDialog
        /// </summary>
        public string FileName { get; set; } = "";

        #endregion Public Properties
    }


    /// <summary>
    ///     Analyzes all the .wav files in a folder sequentially using Audacity, and
    ///     imports the results to the database as each file is dealt with
    /// </summary>
    internal class AnalyseAndImportClass
    {
        private readonly object _analysingEventLock = new object();
        private readonly object _analysingFinishedEventLock = new object();
        private readonly object _dataUpdatedEventLock = new object();
        private EventHandler _analysingEvent;
        private EventHandler _analysingFinishedEvent;
        private EventHandler<EventArgs> _dataUpdatedEvent;

        private string _kaleidoscopeFolderPath = "";

        internal int FilesRemaining;
        internal bool FolderSelected;

        /// <summary>
        ///     class constructor
        /// </summary>
        public AnalyseAndImportClass()
        {
            FolderSelected = false;
            FilesRemaining = 0;
            FolderPath = SelectFolder();
            if (!string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath) && !WavFileList.IsNullOrEmpty())
                FolderSelected = true;
        }

        public string SessionTag { get; set; }
        private Process ExternalProcess { get; set; }
        private string FileToAnalyse { get; set; }
        private string FolderPath { get; set; }
        private GpxHandler ThisGpxHandler { get; set; }
        private RecordingSession ThisRecordingSession { get; set; }

        /// <summary>
        ///     wavFileList is a list of all .wav files in the current folder which
        ///     do not have associated .txt files
        /// </summary>
        private List<string> WavFileList { get; set; }

        /// <summary>
        ///     Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler e_Analysing
        {
            add
            {
                lock (_analysingEventLock)
                {
                    _analysingEvent += value;
                }
            }
            remove
            {
                lock (_analysingEventLock)
                {
                    _analysingEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Event raised after the Analysing property value has changed.
        /// </summary>
        public event EventHandler e_AnalysingFinished
        {
            add
            {
                lock (_analysingFinishedEventLock)
                {
                    _analysingFinishedEvent += value;
                }
            }
            remove
            {
                lock (_analysingFinishedEventLock)
                {
                    _analysingFinishedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Event raised after the  property value has changed.
        /// </summary>
        public event EventHandler<EventArgs> e_DataUpdated
        {
            add
            {
                lock (_dataUpdatedEventLock)
                {
                    _dataUpdatedEvent += value;
                }
            }
            remove
            {
                lock (_dataUpdatedEventLock)
                {
                    _dataUpdatedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Allows user to select a folder containing .wav files to be analyzed and
        ///     imported.
        /// </summary>
        /// <returns></returns>
        public string SelectFolder()
        {
            FolderPath = "";


            //using (System.Windows.Forms.OpenFileDialog dialog = new OpenFileDialog())
            //using(Ookii.Dialogs.Wpf.VistaOpenFileDialog dialog=new VistaOpenFileDialog())
            //{
            using (var dialog = new OpenFileDialog
            {
                DefaultExt = "*.*",
                Filter = "Text files (*.txt)|*.txt|Wav files (*.wav)|*.wav|All Files (*.*)|*.*",
                FilterIndex = 2,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Title = "Select Folder or WAV file",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            })
            {
                dialog.FileOk += Dialog_FileOk;


                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == DialogResult.OK)
                    //HeaderFileName = dialog.FileName;

                    FolderPath = Tools.GetPath(dialog.FileName);
                //FolderPath = Path.GetDirectoryName(dialog.FileName);
                else
                    return null;
            }


            if (string.IsNullOrWhiteSpace(FolderPath)) return null;
            if (!Directory.Exists(FolderPath)) return null;


            GetFileList();
            SessionTag = GetSessionTag();

            ThisRecordingSession = CreateSession();
            if (ThisRecordingSession == null) return null;

            if (!WavFileList.IsNullOrEmpty())
            {
                FilesRemaining = 0;

                FilesRemaining = (from file in WavFileList
                    where file.Substring(file.LastIndexOf(@"\")).Contains(SessionTag)
                    select file).Count();
                /*foreach (var file in WavFileList)
                {
                    if (file.Substring(file.LastIndexOf(@"\")).Contains(SessionTag))
                    {
                        filesRemaining++;
                    }
                }*/
                ThisGpxHandler = new GpxHandler(FolderPath);
                return FolderPath;
            }

            return null;
        }

        /// <summary>
        ///     Event handler fired when the dialog OK button is hit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dialog_FileOk(object sender, CancelEventArgs e)
        {
            e.Cancel = false;
            var f = (sender as OpenFileDialog).FileName;
            if (string.IsNullOrWhiteSpace(f)) e.Cancel = true;
            var folder = Tools.GetPath(f);
            if (string.IsNullOrWhiteSpace(folder)) e.Cancel = true;
            if (!Directory.Exists(folder)) e.Cancel = true;
            var files = Directory.EnumerateFiles(folder, "*.wav");
            if ( !files.Any()) e.Cancel = true;
            if (e.Cancel) (sender as OpenFileDialog).FileName = "Select Folder";
        }

        internal static void ActivateApp(string processName)
        {
            var p = Process.GetProcessesByName(processName);

            // Activate the first application we find with this name
            if (p.Any())
                SetForegroundWindow(p[0].MainWindowHandle);
        }

        /// <summary>
        ///     Analyzes the next file in the collection.  Returns the current session tag
        ///     or null if all the files have been dealt with.  Returns an empty string if the
        /// </summary>
        internal string AnalyseNextFile()
        {
            var file = GetNextFile();
            if (file == null) return null;
            if (Analyse(file)) return SessionTag;
            return "";
        }

        internal void Close()
        {
            Debug.WriteLine("AnalyseAndImport.Close()" + FolderPath);
        }

        /// <summary>
        ///     Opens Kaleidoscope in an ExternalProcess which will generate a call back when
        ///     Kaleidoscope is closed.  The call back will update the database from the folder.
        /// </summary>
        internal void ImportFromKaleidoscope()
        {
            //string file = GetNextFile();// ERR returns null if e very .wav file has a .txt file so Kaleidoscope doesn't start with no warning
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
            {
                MessageBox.Show("Invalid or no folder selected");
                return;
            }

            _kaleidoscopeFolderPath = FolderPath;
            ExternalProcess = new Process();
            if (ExternalProcess == null) return;

            ExternalProcess.StartInfo.FileName = "Kaleidoscope.exe";
            //externalProcess.StartInfo.Arguments = folder;
            ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;


            ExternalProcess.EnableRaisingEvents = true;
            ExternalProcess.Exited += ExternalProcess_ExitedKaleidoscope;
            ExternalProcess = Tools.OpenKaleidoscope(FolderPath, ExternalProcess);
        }


        /// <summary>
        ///     EventHandler triggered when the Kaleidoscope process has exited and presumably the user has
        ///     finished analysing as many files as they want to.  This triggers the importation of the session and
        ///     any wav file data not already in the database.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExternalProcess_ExitedKaleidoscope(object sender, EventArgs e)
        {
            using (new WaitCursor("Importing Kaleidoscope Data"))
            {
                if (!string.IsNullOrWhiteSpace(_kaleidoscopeFolderPath) && Directory.Exists(_kaleidoscopeFolderPath))
                {
                    ImportKaleidoscopeFolder(_kaleidoscopeFolderPath);
                    OnDataUpdated(new EventArgs());
                }
            }
        }

        private void ImportKaleidoscopeFolder(string kaleidoscopeFolderPath)
        {
            Debug.WriteLine("Starting to import recordings " + DateTime.Now);

            var wavFileArray = Directory.EnumerateFiles(kaleidoscopeFolderPath, "*.wav");
            //var WAVFileArray= Directory.EnumerateFiles(kaleidoscopeFolderPath, "*.WAV");
            //wavFileArray = wavFileArray.Concat<string>(WAVFileArray);
            foreach (var file in wavFileArray) ThisRecordingSession.ImportWavFile(file);

            Debug.WriteLine("Recordings imported at " + DateTime.Now);
        }

        internal void OpenWavFile(string folder, string bareFileName)
        {
            if (string.IsNullOrWhiteSpace(bareFileName)) bareFileName = "LabelTrack";
            if (bareFileName.LastIndexOf('.') > 0)
                bareFileName = bareFileName.Substring(0, bareFileName.LastIndexOf('.'));

            if (string.IsNullOrWhiteSpace(folder) || !File.Exists(folder)) return;
            if (ExternalProcess != null)
            {
                MessageBox.Show("Close previous instance of Audacity First!");
                return;
            }

            ExternalProcess = new Process();
            if (ExternalProcess == null) return;

            ExternalProcess.StartInfo.FileName = folder;
            //externalProcess.StartInfo.Arguments = folder;
            ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;


            ExternalProcess.EnableRaisingEvents = true;
            ExternalProcess.Exited += ExternalProcess_Exited;

            ExternalProcess = Tools.OpenWavAndTextFile(folder, ExternalProcess);

            try
            {
                /*
                 * CTRL-SHIFT-N         Select None
                 * SHIFT-UP             Select Audio Track
                 * J                    Goto start of track
                 * .....                Move right 5 secs
                 * SHIFT-J              Select Cursor to start of track = 5s
                 * CTRL-E               Zoom to selection
                 * CTRL-SHIFT-N         Select None
                 * */
                var ipSim = new InputSimulator();
                var epHandle = ExternalProcess.MainWindowHandle;
                if (epHandle == (IntPtr) 0L) return;


                // ALT-S,N - 'Select'-None
                SetForegroundWindow(epHandle);

                ipSim.Keyboard.ModifiedKeyStroke(new[] {VirtualKeyCode.CONTROL, VirtualKeyCode.LSHIFT},
                    VirtualKeyCode.VK_N);

                if (!Tools.WaitForIdle(ExternalProcess)) return;
                //Thread.Sleep(1000);
                // SHIFT-UP - Move focus to previous and select
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LSHIFT, VirtualKeyCode.UP);
                if (!Tools.WaitForIdle(ExternalProcess)) return;
                //Thread.Sleep(1000);
                // J - move to start of track
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.VK_J);
                if (!Tools.WaitForIdle(ExternalProcess)) return;
                //         Thread.Sleep(400);
                // . . . . . - Cursor short jump right (by one second)
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.OEM_PERIOD);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.OEM_PERIOD);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.OEM_PERIOD);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.OEM_PERIOD);
                ipSim.Keyboard.KeyPress(VirtualKeyCode.OEM_PERIOD);

                if (!Tools.WaitForIdle(ExternalProcess)) return;
                //          Thread.Sleep(200);
                // SHIFT-J - Region, track start to cursor
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_J);
                if (!Tools.WaitForIdle(ExternalProcess)) return;
                //          Thread.Sleep(200);
                // CTRL-E - Zoom to selection
                SetForegroundWindow(epHandle);
                ipSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_E);
                if (!Tools.WaitForIdle(ExternalProcess)) return;
                //          Thread.Sleep(200);
                // ALT-S,N - 'Select'-None
                SetForegroundWindow(epHandle);
                //s.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.VK_S);
                //Thread.Sleep(2000);
                //SetForegroundWindow(h);
                //s.Keyboard.KeyPress(VirtualKeyCode.VK_N);
                ipSim.Keyboard.ModifiedKeyStroke(new[] {VirtualKeyCode.CONTROL, VirtualKeyCode.LSHIFT},
                    VirtualKeyCode.VK_N);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("From OpenWavFile:- "+ex.Message);
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_Analysing" /> event.
        /// </summary>
        /// <param name="e"><see cref="AnalysingEventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnAnalysing(AnalysingEventArgs e)
        {
            EventHandler handler = null;

            lock (_analysingEventLock)
            {
                handler = _analysingEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     Raises the <see cref="e_AnalysingFinished" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnAnalysingFinished(EventArgs e)
        {
            EventHandler handler = null;

            lock (_analysingFinishedEventLock)
            {
                handler = _analysingFinishedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     Raises the <see cref="e_DataUpdated" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnDataUpdated(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_dataUpdatedEventLock)
            {
                handler = _dataUpdatedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        ///     Opens Audacity with the fileToaAnalyse
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool Analyse(string file)
        {
            if (!File.Exists(file)) return false;
            var bareFilename = file;
            if (file.Contains(@"\") && !file.EndsWith(@"\")) bareFilename = file.Substring(file.LastIndexOf(@"\") + 1);
            OnAnalysing(new AnalysingEventArgs(bareFilename));
            OpenWavFile(file, bareFilename);
            return true;
        }

        /// <summary>
        ///     Tries to find a header file and if found tries to populate a new RecordingSession
        ///     based on it.  Whether or no, then displays a RecordingSession Dialog for the user to
        ///     populate and/or amend as required.  The RecordingSession is then saved to the
        ///     database and the RecordingSessiontag is used to replace the current Sessiontag
        /// </summary>
        /// <returns></returns>
        private RecordingSession CreateSession()
        {
            var newSession = SessionManager.CreateSession(FolderPath, SessionTag, null);
            if (newSession != null) OnDataUpdated(new EventArgs());

            return newSession;
        }

        /// <summary>
        ///     Presents a RecordingSessionDialog to the user for filling in or amending and returns
        ///     the amended session.
        /// </summary>
        /// <param name="newSession"></param>
        /// <returns></returns>
        private RecordingSession EditSession(RecordingSession newSession)
        {
            return SessionManager.EditSession(newSession, SessionTag, FolderPath);
        }

        private void ExternalProcess_Exited(object sender, EventArgs e)
        {
            ExternalProcess.Close();
            ExternalProcess = null;
            if (IsCurrentMatchingTextFile())
            {
                SaveRecording();
                AnalyseNextFile();
            }
            else
            {
                OnAnalysingFinished(new EventArgs());
            }
        }

        /// <summary>
        ///     Uses the header file to try and populate a new recordingSession,
        ///     otherwise returns a new RecordingSession;
        /// </summary>
        /// <param name="headerFile"></param>
        /// <returns></returns>
        private RecordingSession FillSessionFromHeader(string headerFile)
        {
            return SessionManager.FillSessionFromHeader(headerFile, SessionTag);
        }

        internal string GetProcessWindowTitle()
        {
            return ExternalProcess.MainWindowTitle;
        }

        /// <summary>
        ///     Populates wavFileList with a list of all .wav files in the selected folder
        ///     which do nat have associated .txt files
        /// </summary>
        private void GetFileList()
        {
            if (!string.IsNullOrWhiteSpace(FolderPath))
            {
                WavFileList = new List<string>();
                if (string.IsNullOrWhiteSpace(FolderPath)) return;
                var listOfWavFiles = Directory.EnumerateFiles(FolderPath, "*.wav", SearchOption.TopDirectoryOnly);
                //var listOfWAVFiles= Directory.EnumerateFiles(FolderPath, "*.WAV", SearchOption.TopDirectoryOnly);
                //listOfWavFiles = listOfWavFiles.Concat<string>(listOfWAVFiles);
                if (listOfWavFiles.IsNullOrEmpty())
                {
                    WavFileList.Clear(); // there are no wav files to process
                    return;
                }

                var listOfTxtFiles = Directory.EnumerateFiles(FolderPath, "*.txt", SearchOption.TopDirectoryOnly);
                //var listOfTXTFiles= Directory.EnumerateFiles(FolderPath, "*.TXT", SearchOption.TopDirectoryOnly);
                //listOfTxtFiles = listOfTxtFiles.Concat<string>(listOfTXTFiles);
                if (listOfTxtFiles.IsNullOrEmpty()) // if there are no text files then all wav files are used
                {
                    WavFileList = listOfWavFiles.ToList(); // process all wav files
                    return;
                }

                WavFileList = (from wavs in listOfWavFiles
                    where !listOfTxtFiles.Contains(wavs.ToLower().Substring(0, wavs.Length - 4) + ".txt")
                    select wavs).ToList(); // process wav files that do not have matching txt files
            }
        }

        /// <summary>
        ///     Looks for a header text file in the selected folder which starts with a [COPY]
        ///     directive.
        /// </summary>
        /// <returns></returns>
        private string GetHeaderFile()
        {
            return SessionManager.GetHeaderFile(FolderPath);
        }

        private string GetNextFile()
        {
            if (!WavFileList.IsNullOrEmpty())
            {
                if (!string.IsNullOrWhiteSpace(FileToAnalyse))
                {
                    var matchingTextFile = FileToAnalyse.Substring(0, FileToAnalyse.LastIndexOf(".")) + ".txt";
                    if (WavFileList.Contains(FileToAnalyse) && File.Exists(matchingTextFile))
                    {
                        WavFileList.Remove(FileToAnalyse);
                        FileToAnalyse = "";
                        if (WavFileList.IsNullOrEmpty()) return null;
                    }
                }

                if (string.IsNullOrWhiteSpace(SessionTag))
                {
                    FileToAnalyse = WavFileList.First();
                    if (File.Exists(FileToAnalyse))
                    {
                        FilesRemaining--;
                        return FileToAnalyse;
                    }

                    FileToAnalyse = "";
                    return null;
                }

                foreach (var file in WavFileList)
                    if (File.Exists(file))
                    {
                        FileToAnalyse = file;
                        FilesRemaining--;
                        if (IsCurrentMatchingTextFile()) continue;

                        return file;
                    }
            }

            return null;
        }

        /// <summary>
        ///     returns the SessionTag for the selected folder to analyse
        /// </summary>
        /// <returns></returns>
        private string GetSessionTag()
        {
            var tagPattern = @"[A-Z0-9]+[-_]{1}([0-9a-zA-Z]+[-_]{1})?20[0-9]{6}";
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
            {
                var folder = SelectFolder();
                if (folder == null) return "";
            }

            if (WavFileList.IsNullOrEmpty()) GetFileList();
            if (!WavFileList.IsNullOrEmpty())
                foreach (var file in WavFileList)
                {
                    var fileName = file.Substring(file.LastIndexOf(@"\"));
                    var result = Regex.Match(fileName, tagPattern);
                    if (result.Success)
                    {
                        SessionTag = result.Value;
                        return SessionTag;
                    }
                }

            return "";
        }

        /// <summary>
        ///     returns true if there is a current file to analyse and it has a matching text file
        /// </summary>
        /// <returns></returns>
        private bool IsCurrentMatchingTextFile()
        {
            var result = false;
            if (!string.IsNullOrWhiteSpace(FileToAnalyse) && File.Exists(FileToAnalyse))
            {
                var matchingTextFile = FileToAnalyse.Substring(0, FileToAnalyse.LastIndexOf(".")) + ".txt";
                if (File.Exists(matchingTextFile)) result = true;
            }

            return result;
        }

        /// <summary>
        ///     Reads the text file associatedd with the fileToAnalyse and uses it
        ///     to create a Recording which is saved to the database as belonging to the
        ///     current session
        /// </summary>
        private void SaveRecording()
        {
            var batsFound = new Dictionary<string, BatStats>();
            var result = "Text File does not exist";
            var textFileToProcess = FileToAnalyse.Substring(0, FileToAnalyse.Length - 4) + ".txt";
            if (File.Exists(textFileToProcess))
                result = FileProcessor.ProcessFile(textFileToProcess, ThisGpxHandler, ThisRecordingSession.Id,
                    ref batsFound);
            Debug.WriteLine("AnalyseAndImport.SaveRecording:-" + FileToAnalyse + "\n" + result + "\n~~~~~~~~~~~~\n");

            OnDataUpdated(new EventArgs());
        }

        /// <summary>
        ///     Saves the recordingSession to the database
        /// </summary>
        /// <param name="newSession"></param>
        /// <returns></returns>
        private RecordingSession SaveSession(RecordingSession newSession)
        {
            var savedSession = SessionManager.SaveSession(newSession);

            OnDataUpdated(new EventArgs());
            return savedSession;
        }
    }
}