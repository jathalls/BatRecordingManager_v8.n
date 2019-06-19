using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Class to handle selection of input, and by derivation or selection, and output file, the
    ///     names of which are made available publicly Public functions:- Select folder Select
    ///     Manual Log File Create Output File
    /// </summary>
    public class FileBrowser
    {
        /// <summary>
        ///     The existing log file name
        /// </summary>
        private string _existingLogFileName = "";

        private bool _isProcessingFolder;

        /// <summary>
        ///     The output log file name
        /// </summary>
        private string _outputLogFileName = "";

        /// <summary>
        ///     The root folder
        /// </summary>
        public string RootFolder = "";

        /// <summary>
        ///     The wav file folders
        /// </summary>
        public BulkObservableCollection<string> WavFileFolders = new BulkObservableCollection<string>();

        public BulkObservableCollection<string> WavFileNames = new BulkObservableCollection<string>();

        /// <summary>
        ///     The text file names
        /// </summary>
        //private BulkObservableCollection<String> TextFileNames = new BulkObservableCollection<String>();
        /// <summary>
        ///     The working folder
        /// </summary>
        private string _workingFolder = "";

        /// <summary>
        ///     Accessor for existingLogFileName. existingLogFileName contains the fully qualified
        ///     file name of a pre-existing log file.
        /// </summary>
        /// <value>
        ///     The name of the existing log file.
        /// </value>
        public string ExistingLogFileName
        {
            get => _existingLogFileName;
            set => _existingLogFileName = value;
        }

        /// <summary>
        ///     Gets or sets the name of the header file.
        /// </summary>
        /// <value>
        ///     The name of the header file.
        /// </value>
        public string HeaderFileName { get; set; }

        /// <summary>
        ///     Accessor for outputLogFileName. outputLogFileName contains the manufactured, fully
        ///     qualified path and file name to which the concatenation of log files will be written.
        /// </summary>
        /// <value>
        ///     The name of the output log file.
        /// </value>
        public string OutputLogFileName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_outputLogFileName))
                    if (!string.IsNullOrWhiteSpace(WorkingFolder))
                    {
                        var folderName = WorkingFolder.Substring(0, WorkingFolder.Length - 1);

                        if (folderName.Contains(@"\"))
                        {
                            int finalSeparator;
                            finalSeparator = folderName.LastIndexOf(@"\");
                            folderName = folderName.Substring(finalSeparator);
                            if (!string.IsNullOrWhiteSpace(folderName))
                                if (folderName.StartsWith(@"\"))
                                    folderName = folderName.Substring(1);
                        }

                        _outputLogFileName = WorkingFolder + folderName + ".log.txt";
                        Debug.WriteLine("getting op folder -" + _outputLogFileName + "-");
                    }

                return _outputLogFileName;
            }
            set
            {
                Debug.WriteLine("Set op file to -" + value + "-");
                _outputLogFileName = value;
            }
        }

        /// <summary>
        ///     public accessor for textFileNames. textFileNames lists all the files to be processed
        ///     and concatenated into a single log file.
        /// </summary>
        /// <value>
        ///     The text file names.
        /// </value>
        public BulkObservableCollection<string> TextFileNames { get; set; } = new BulkObservableCollection<string>();

        /// <summary>
        ///     Gets or sets the working folder.
        /// </summary>
        /// <value>
        ///     The working folder.
        /// </value>
        public string WorkingFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_workingFolder)) return @"";
                if (_workingFolder.EndsWith(@"\"))
                    return _workingFolder;
                return _workingFolder + @"\";
            }
            set
            {
                _workingFolder = value;
                if (string.IsNullOrWhiteSpace(_workingFolder) || !Directory.Exists(_workingFolder)) TextFileNames.Clear();
            }
        }

        /// <summary>
        ///     Generic Open file function.  Can be passed an initial directory or NULL to use the
        ///     application home directory, a fileTypeFilter to chose what types of file to display/select
        ///     and a boolean to set if it is desired to be able to select multiple files.  Returns a List of String
        ///     containing the selected fully qualified filenames.
        ///     Filter inn the form "foobar (*.foo)|*.foo|barfoo (*.bar)|*.bar"
        /// </summary>
        /// <param name="title"> The title of the dialog box</param>
        /// <param name="initialPath"> The Directory to open initially or NULL</param>
        /// <param name="fileTypeFilter">The filter parameters for what type of file to select</param>
        /// <param name="multiSelect">TRUE for multi file selection</param>
        /// <returns></returns>
        public static List<string> SelectFile(string title, string initialPath, string fileTypeFilter, bool multiSelect)
        {
            if (string.IsNullOrWhiteSpace(title)) title = "Select File";
            var result = new List<string>();
            if (initialPath == null) initialPath = Directory.GetCurrentDirectory();
            if (!Directory.Exists(initialPath)) initialPath = Directory.GetCurrentDirectory();
            if (string.IsNullOrWhiteSpace(fileTypeFilter)) fileTypeFilter = "all Files (*.*)|*.*";
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = initialPath;
                dialog.Filter = fileTypeFilter;
                dialog.Multiselect = multiSelect;
                dialog.Title = "title";
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                    if (dialog.FileNames.Any())
                        result.AddRange(dialog.FileNames);
            }

            return result;
        }

        /// <summary>
        ///     Pops the wav folder. Returns the next wav file folder from the list and removes it
        ///     from the list.
        /// </summary>
        /// <returns>
        /// </returns>
        public string PopWavFolder()
        {
            var result = "";
            if (!WavFileFolders.IsNullOrEmpty())
            {
                result = WavFileFolders[0];
                WavFileFolders.Remove(result);
                WorkingFolder = result;
                TextFileNames.Clear();
                _outputLogFileName = "";
                Debug.WriteLine("Popped:- " + result);
            }

            return result;
        }

        /// <summary>
        ///     Processes the folder. Called when a new folder is to be dealt with. If there is a
        ///     manifest file, copies the file names from that into the file list. if not, then
        ///     copies names of all .txt files except .log.txt files. if there are still just 0 or 1
        ///     file in the list, try to extract comment files from a log file
        /// </summary>
        /// <param name="folder">
        ///     The folder.
        /// </param>
        /// <returns>
        /// </returns>
        public string ProcessFolder(string folder)
        {
            var folderPath = _workingFolder;
            Debug.WriteLine("Processing:- " + folder + " ... workingFolder is:- " + _workingFolder);
            if (!string.IsNullOrWhiteSpace(folder)) // can't do anything without a folder to read files from
            {
                if (Directory.Exists(folder)
                ) // still can't do anything if the folder doesn't exist (in which case return null)
                {
                    folderPath = folder;
                    TextFileNames.Clear();
                    var manifests = Directory.GetFiles(folderPath, "*.manifest", SearchOption.TopDirectoryOnly);
                    if (!manifests.IsNullOrEmpty() && File.Exists(manifests[0])) // we have a manifest
                    {
                        // so add the files in the manifest tot he files list
                        TextFileNames.Clear();
                        var manifestFiles = File.ReadAllLines(manifests[0]);
                        foreach (var file in manifestFiles)
                            if (file.ToUpper().EndsWith(".TXT") && File.Exists(file))
                                TextFileNames.Add(file);
                    }

                    if (TextFileNames.Count <= 0) // if we have no files yet - no or empty manifest
                    {
                        //get all the text files in the folder
                        var txtFiles = Directory.EnumerateFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);
                        //var TXTFiles= Directory.EnumerateFiles(folderPath, "*.TXT", SearchOption.TopDirectoryOnly);
                        //txtFiles = txtFiles.Concat<string>(TXTFiles);
                        var files = from file in txtFiles
                            orderby file
                            select file;
                        TextFileNames.Clear();
                        foreach (var filename in files)
                            if (!filename.ToLower().EndsWith(".log.txt")) // except for .log.txt files
                                TextFileNames.Add(filename);
                    }

                    if (TextFileNames.Count <= 1)
                        if (!_isProcessingFolder)
                        {
                            _isProcessingFolder =
                                true; // to prevent call to this function from ExtractCommentFilesFromLogFile
                            // recursing. It can recurse once but after that will be blocked
                            folderPath = ExtractCommentFilesFromLogFile(folder);
                            _isProcessingFolder = false;
                        }
                }
                else
                {
                    _isProcessingFolder = false;
                    folderPath = null;
                }
            }

            WorkingFolder = folderPath;

            _isProcessingFolder = false;
            return folderPath;
        }

        /// <summary>
        ///     Selects the folder.
        /// </summary>
        /// <returns>
        /// </returns>
        public string SelectHeaderTextFile()
        {
            var folderPath = Directory.GetCurrentDirectory();


            using (var dialog = new OpenFileDialog())
            {
                dialog.DefaultExt = ".txt";
                dialog.Filter = "Text files (*.txt)|*.txt|Wav files (*.wav)|*.wav|All Files (*.*)|*.*";
                dialog.FilterIndex = 0;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Title = "Select Header text File";

                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    HeaderFileName = dialog.FileName;
                    //folderPath = Path.GetDirectoryName(HeaderFileName);
                    folderPath = Tools.GetPath(HeaderFileName);
                    ProcessFolder(folderPath);
                }
            }

            return folderPath;
        }

        public static string SelectFolderByDialog(string dialogTitle)
        {
            string folderPath = null;
            using (var dialog = new OpenFileDialog())
            {
                dialog.DefaultExt = ".txt";
                dialog.Filter = "Text files (*.txt)|*.txt|Wav files (*.wav)|*.wav|All Files (*.*)|*.*";
                dialog.FilterIndex = 2;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Title = "Select Header text File";
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Select Folder";

                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == DialogResult.OK) folderPath = Tools.GetPath(dialog.FileName);
            }

            return folderPath;
        }

        /// <summary>
        ///     Selects the log files.
        /// </summary>
        /// <returns>
        /// </returns>
        public string SelectLogFiles()
        {
            using (var dialog = new OpenFileDialog())
            {
                if (!string.IsNullOrWhiteSpace(WorkingFolder))
                {
                    dialog.InitialDirectory = WorkingFolder;
                }
                else
                {
                    WorkingFolder = Directory.GetCurrentDirectory();
                    dialog.InitialDirectory = WorkingFolder;
                }

                dialog.Filter = "txt files|*.txt|Log files|*.log";

                dialog.Multiselect = true;
                dialog.Title = "Select one or more descriptive text files";
                if (dialog.ShowDialog() == DialogResult.OK)
                    if (dialog.FileNames.Any())
                    {
                        TextFileNames.Clear();
                        foreach (var filename in dialog.FileNames) TextFileNames.Add(filename);
                    }
            }

            if (TextFileNames.Any()) return TextFileNames[0]; // access count from private element to prevent recursion
            return "";
        }

        /// <summary>
        ///     Selects the root folder using the FolderBrowserDialog and puts all the folders with
        ///     both .wav and .txt files
        /// </summary>
        /// <returns>
        /// </returns>
        public string SelectRootFolder()
        {
            var folderPath = Directory.GetCurrentDirectory();


            /*using (System.Windows.Forms.OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.DefaultExt = "*.*";
                dialog.Filter = "Text files (*.txt)|*.txt|Wav files (*.wav)|*.wav|All Files (*.*)|*.*";
                dialog.FilterIndex = 3;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Title = "Select Folder of WAV/TXT files";
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Select Folder";

                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == DialogResult.OK)
                {*/

            folderPath = SelectFolderByDialog("Select Folder of WAV/TXT Files");
            //HeaderFileName = dialog.FileName;
            //folderPath = Path.GetDirectoryName(dialog.FileName);
            //folderPath = Tools.GetPath(dialog.FileName);
            RootFolder = folderPath;
            WavFileFolders = GenerateFolderList(RootFolder);
            /* }
             else
             {
                 return (null);
                 }
             }*/


            return RootFolder;
        }

        /// <summary>
        ///     Selects the wav file. Uses the OpenFileDialog to allow the user to select a single
        ///     .wav file and returns places that name in as the only entry in the textFileNames
        ///     list and also returns the name
        /// </summary>
        /// <returns>
        /// </returns>
        public string SelectWavFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                if (!string.IsNullOrWhiteSpace(WorkingFolder))
                {
                    dialog.InitialDirectory = WorkingFolder;
                }
                else
                {
                    WorkingFolder = Directory.GetCurrentDirectory();
                    dialog.InitialDirectory = WorkingFolder;
                }

                dialog.Filter = "wav files (*.wav)|*.wav";

                dialog.Multiselect = false;
                dialog.Title = "Select one  recording .wav file";
                if (dialog.ShowDialog() == DialogResult.OK)
                    if (dialog.FileNames.Any())
                    {
                        TextFileNames.Clear();
                        foreach (var filename in dialog.FileNames) TextFileNames.Add(filename);
                    }
            }

            if (TextFileNames.Any()) return TextFileNames[0];
            return "";
        }

        /// <summary>
        ///     Gets the header file.
        /// </summary>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal string[] GetHeaderFile()
        {
            if (!TextFileNames.IsNullOrEmpty())
            {
                foreach (var file in TextFileNames)
                    if (file.ToUpper().EndsWith(".TXT"))
                    {
                        var lines = File.ReadAllLines(file);
                        if (!lines.IsNullOrEmpty() && lines[0].Contains("[COPY]"))
                        {
                            HeaderFileName = file;
                            return lines;
                        }
                    }

                foreach (var file in TextFileNames)
                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        var wavfile = file.Substring(0, file.Length - 4) + ".wav";
                        if (!File.Exists(wavfile) && !file.Contains(".log.txt") && file.ToUpper().EndsWith(".TXT") &&
                            File.Exists(file))
                        {
                            var lines = File.ReadAllLines(file);
                            return lines;
                        }
                    }
            }

            return null;
        }

        internal string GetLabelFileForRecording(Recording recording)
        {
            var result = "";
            if (recording != null)
            {
                result = recording.RecordingSession.OriginalFilePath;
                if (result.EndsWith(@"\")) result = result.Substring(0, result.Length - 2);
                if (recording.RecordingName.StartsWith(@"\"))
                    result = result + recording.RecordingName;
                else
                    result = result + @"\" + recording.RecordingName;
                if (result.ToUpper().EndsWith(".WAV")) result = result.Substring(0, result.Length - 5);
                if (!result.ToUpper().EndsWith(".TXT")) result = result + ".txt";
                if (!File.Exists(result)) result = "";
            }

            return result;
        }

        /// <summary>
        ///     Gets the unqualified filename from the TextFileNames list at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The index of the filename to extract
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal string GetUnqualifiedFilename(int index)
        {
            var result = "";
            if (index >= 0 && TextFileNames != null && TextFileNames.Count > index)
            {
                var qualifiedname = TextFileNames[index];
                var lastSeperator = qualifiedname.LastIndexOf('\\');
                if (lastSeperator >= 0) result = qualifiedname.Substring(lastSeperator);
            }

            return result;
        }

        /// <summary>
        ///     Determines whether [contains wav and txt files] [the specified root folder].
        /// </summary>
        /// <param name="baseFolder">
        ///     The base folder.
        /// </param>
        /// <param name="ext">
        ///     the file extension to be matched
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private bool ContainsFilesOfType(string baseFolder, string ext)
        {
            if (!string.IsNullOrWhiteSpace(baseFolder))
            {
                var files = Directory.EnumerateFiles(baseFolder);
                if (!files.IsNullOrEmpty())
                {
                    var wavFiles = from file in files
                        where file.ToUpper().EndsWith(ext.ToUpper())
                        select file;
                    if (!wavFiles.IsNullOrEmpty()) return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Extracts the comment files from log file. there ar no or just one text file in the
        ///     selected folder except for .log.txt files. This function identifies a .log.txt file
        ///     with the same name as the folder and splits it into several separate files.
        ///     Everything up to the first occurrence of a .wav filename is extracted into a .txt
        ///     file with the same name as the folder - a new header file. For each .wav file in the
        ///     folder which does not have a matching .txt file, the .log.txt file will be split at
        ///     the first occurrence of the .wav file name and the contents as far the next .wav
        ///     file name will be copied to a .txt file with the same name as the .wav file. Finally
        ///     ProcessFolder is called and it's result returned.
        /// </summary>
        private string ExtractCommentFilesFromLogFile(string folder)
        {
            if (!folder.EndsWith(@"\")) folder = folder + @"\";
            var logFileText = "";
            var folderName = GetFoldernameFromPath(folder);
            if (File.Exists(folder + folderName + ".log.txt"))
                logFileText = File.ReadAllText(folder + folderName + ".log.txt");
            if (string.IsNullOrWhiteSpace(logFileText))
            {
                var logFiles = Directory.GetFiles(folder, "*.log.txt");
                if (logFiles == null || !logFiles.Any()) return folder;
                var biggestFile = "";
                var fileSize = long.MinValue;
                foreach (var file in logFiles)
                {
                    var f = new FileInfo(file);
                    if (f.Length > fileSize)
                    {
                        fileSize = f.Length;
                        biggestFile = file;
                    }
                }

                if (string.IsNullOrWhiteSpace(biggestFile)) return folder;
                logFileText = File.ReadAllText(biggestFile);
                if (string.IsNullOrWhiteSpace(logFileText)) return folder;
            }

            if (!File.Exists(folder + folderName + ".txt"))
            {
                var sw = File.AppendText(folder + folderName + ".txt");
                var index = logFileText.IndexOf(".wav");
                if (index > 0)
                {
                    sw.Write(logFileText.Substring(0, index));
                    sw.WriteLine();
                }
                else
                {
                    sw.Write(folderName);
                    sw.WriteLine();
                }

                sw.Close(); // Header file is complete
            }

            var wavFiles = Directory.EnumerateFiles(folder, "*.wav");
            //var WAVFiles= Directory.EnumerateFiles(folder, "*.wav");
            //wavFiles = wavFiles.Concat<string>(WAVFiles);
            if (!wavFiles.IsNullOrEmpty())
                foreach (var file in wavFiles)
                {
                    var matchingTextFile = file.Substring(0, file.Length - 3) + "txt";
                    if (!File.Exists(matchingTextFile))
                    {
                        var wavFileName = file.Substring(file.LastIndexOf(@"\") + 1);
                        var sw = File.AppendText(matchingTextFile);

                        var index1 = logFileText.IndexOf(wavFileName);
                        var index2 = -1;
                        if (index1 >= 0)
                        {
                            var rest = logFileText.Substring(index1);
                            var rest2 = rest.Substring(wavFileName.Length);
                            index2 = rest2.IndexOf(".wav") + wavFileName.Length;
                            if (index2 > 0)
                                try
                                {
                                    var outStr = rest.Substring(0, index2);
                                    sw.Write(outStr);
                                }
                                catch (Exception ex)
                                {
                                    Tools.ErrorLog(ex.Message);
                                    sw.Write(rest);
                                }
                            else
                                sw.Write(rest);
                        }

                        sw.WriteLine();
                        sw.Close();
                    }
                }

            ProcessFolder(folder);

            return folder;
        }

        /// <summary>
        ///     Generates a list of folders with wav files beneath the rootFolder supplied. The
        ///     Directory tree is traversed and each folder containing wav files is added to the
        ///     list, but once such a folder is identified its child folders are not included in the search.
        /// </summary>
        /// <param name="rootFolder">
        ///     The root folder.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private BulkObservableCollection<string> GenerateFolderList(string rootFolder)
        {
            var wavFolders = new BulkObservableCollection<string>();
            GetWavFileFolder(rootFolder, ref wavFolders);

            return wavFolders;
        }

        /// <summary>
        ///     Gets the foldername from path.
        /// </summary>
        /// <param name="folder">
        ///     The folder.
        /// </param>
        /// <returns>
        /// </returns>
        private string GetFoldernameFromPath(string folder)
        {
            if (folder.EndsWith(@"\"))
            {
                folder = folder.Substring(0, folder.Length - 1);
                var index = folder.LastIndexOf(@"\");

                if (index >= 0 && index < folder.Length) folder = folder.Substring(index + 1);
            }

            return folder;
        }

        /// <summary>
        ///     Gets the wav file folder by recursive search of the directory tree adding
        ///     appropriate folders to the list passed by reference.
        /// </summary>
        /// <param name="rootFolder">
        ///     The root folder.
        /// </param>
        /// <param name="wavFolders">
        ///     The wav folders.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private void GetWavFileFolder(string rootFolder, ref BulkObservableCollection<string> wavFolders)
        {
            if (ContainsFilesOfType(rootFolder, ".wav") && ContainsFilesOfType(rootFolder, ".txt"))
            {
                wavFolders.Add(rootFolder);
                return;
            }

            var children = Directory.EnumerateDirectories(rootFolder);
            if (!children.IsNullOrEmpty())
                foreach (var child in children)
                    GetWavFileFolder(child, ref wavFolders);
        }
    }
}