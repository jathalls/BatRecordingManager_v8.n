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
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ImportControl.xaml
    /// </summary>
    public partial class ImportControl : UserControl
    {
        /// <summary>
        ///     The file browser
        /// </summary>
        private FileBrowser _fileBrowser;

        /// <summary>
        ///     The file processor
        /// </summary>
        private FileProcessor _fileProcessor;

        /// <summary>
        ///     The GPX handler
        /// </summary>
        private GpxHandler _gpxHandler;

        /// <summary>
        ///     indicates if the selected folder is to be processed as a set of
        ///     Audacity text files (false) or as a set of wav files with Kaleidoscope
        ///     metadata (true).
        /// </summary>
        private bool _processWavFiles;

        /// <summary>
        ///     The session for folder
        /// </summary>
        private RecordingSession _sessionForFolder;

        /// <summary>
        ///     The current session identifier
        /// </summary>
        public int CurrentSessionId = -1;

        /// <summary>
        ///     The current session tag
        /// </summary>
        public string CurrentSessionTag = "";

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportControl" /> class.
        /// </summary>
        public ImportControl()
        {
            _fileBrowser = new FileBrowser();
            ImportPictureControl = Activator.CreateInstance<ImportPictureControl>();
            InitializeComponent();
            //fileBrowser = new FileBrowser();
            //DBAccess.InitializeDatabase();
            _fileProcessor = new FileProcessor();
            ImportPictureControl.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Visible;

            UpdateRecordingButton.ToolTip = "Update a specific Recording by selecting a single .wav file";
        }

        /// <summary>
        ///     Processes the files. fileBrowser.TextFileNames contains a list of .txt files in the
        ///     folder that is to be processed. The .txt files are label files or at least in a
        ///     compatible format similar to that produced by Audacity. There may also be a header
        ///     file which contains information about the recording session which will be generated
        ///     from this file set. The header file should start with the tag [COPY].
        ///     fileProcessor.ProcessFile does the work on each file in turn.
        /// </summary>
        public bool ProcessFiles()
        {
            var result = false;
            TbkOutputText.Text = "[LOG]\n";

            if (!_processWavFiles)
            {
                var totalBatsFound = new Dictionary<string, BatStats>();

                // process the files one by one
                try
                {
                    if (_fileBrowser.TextFileNames.Count > 0)
                    {
                        if (_sessionForFolder != null && _sessionForFolder.Id > 0)
                        {
                            TbkOutputText.Text = _sessionForFolder.ToFormattedString();
                            foreach (var rec in _sessionForFolder.Recordings)
                                DBAccess.DeleteRecording(
                                    rec); //so that we can recreate them from scrathch using the file data
                        }

                        foreach (var filename in _fileBrowser.TextFileNames)
                            if (!string.IsNullOrWhiteSpace(_fileBrowser.HeaderFileName) &&
                                filename == _fileBrowser.HeaderFileName)
                            {
                                // skip this file if it has been identified as the header data file, since
                                // the information should have been included as the session record header
                                // and this would be a duplicate.
                            }
                            else
                            {
                                TbkOutputText.Text = TbkOutputText.Text + "***\n\n" +
                                                     FileProcessor.ProcessFile(filename, _gpxHandler, CurrentSessionId,
                                                         ref _fileProcessor.BatsFound) + "\n";
                                totalBatsFound = BatsConcatenate(totalBatsFound, _fileProcessor.BatsFound);
                            }

                        TbkOutputText.Text = TbkOutputText.Text + "\n#########\n\n";
                        if (totalBatsFound != null && totalBatsFound.Count > 0)
                            foreach (var bat in totalBatsFound)
                            {
                                bat.Value.batCommonName = bat.Key;
                                TbkOutputText.Text += Tools.GetFormattedBatStats(bat.Value, false) + "\n";

                                //tbkOutputText.Text = tbkOutputText.Text +
                                // FileProcessor.FormattedBatStats(bat) + "\n";
                            }
                    }

                    if (!string.IsNullOrWhiteSpace(TbkOutputText.Text))
                    {
                        SaveOutputFile();
                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    Debug.WriteLine("Processing of Recording Files failed:- " + ex.Message);
                    result = false;
                }
            }
            else
            {
                result = ProcessWavMetadata();
            }

            return result;
        }

        /// <summary>
        ///     Reads all the files selected through a File Open Dialog. File names are contained a
        ///     fileBrowser instance which was used to select the files. Adds all the file names to
        ///     a combobox and also loads the contents into a stack of Text Boxes in the left pane
        ///     of the screen.
        /// </summary>
        public string ReadSelectedFiles()
        {
            var outputLocation = "";
            if (_fileBrowser.TextFileNames != null && _fileBrowser.TextFileNames.Count > 0)
            {
                Debug.WriteLine("ReadSelectedFiles:- first=:- " + _fileBrowser.TextFileNames[0]);
                //File.Create(fileBrowser.OutputLogFileName);
                if (DpMMultiWindowPanel.Children.Count > 0)
                {
                    foreach (var child in DpMMultiWindowPanel.Children) (child as TextBox).Clear();
                    DpMMultiWindowPanel.Children.Clear();
                }

                var textFiles = new BulkObservableCollection<TextBox>();
                foreach (var file in _fileBrowser.TextFileNames)
                {
                    var tb = new TextBox
                    {
                        AcceptsReturn = true,
                        AcceptsTab = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    if (File.Exists(file))
                        using (var sr = File.OpenText(file))
                        {
                            try
                            {
                                if (sr != null)
                                {
                                    var firstline = sr.ReadLine();
                                    if (string.IsNullOrWhiteSpace(firstline)) firstline = "start - end\tNo Bats";
                                    //sr.Close();
                                    if (firstline != null)
                                    {
                                        if (!(firstline.Contains("[LOG]") || firstline.Contains("***")))
                                        {
                                            //if (!file.EndsWith(".log.txt"))
                                            //{
                                            tb.Text = file + @"
    " + sr.ReadToEnd();
                                            DpMMultiWindowPanel.Children.Add(tb);
                                        }
                                        else
                                        {
                                            TbkOutputText.Text = sr.ReadToEnd();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.ErrorLog(ex.Message);
                                Debug.WriteLine(ex);
                            }
                            finally
                            {
                                sr?.Close();
                            }
                        }
                }

                if (!string.IsNullOrWhiteSpace(_fileBrowser.OutputLogFileName))
                    outputLocation = "Output File:- " + _fileBrowser.OutputLogFileName;
                else
                    outputLocation = "Output to:- " + _fileBrowser.WorkingFolder;
            }
            else
            {
                outputLocation = "";
            }

            if (string.IsNullOrWhiteSpace(TbkOutputText.Text)) TbkOutputText.Text = "[LOG]\n";
            return outputLocation;
        }

        /// <summary>
        ///     Saves the output file.
        /// </summary>
        public bool SaveOutputFile()
        {
            var isSaved = false;
            var ofn = _fileBrowser.OutputLogFileName;
            if (!string.IsNullOrWhiteSpace(TbkOutputText.Text))
                //if (MessageBox.Show("Save Output File?", "Save", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                //{
                try
                {
                    if (File.Exists(_fileBrowser.OutputLogFileName))
                    {
                        if (MessageBox.Show
                            ("Overwrite existing\n" + _fileBrowser.OutputLogFileName +
                             "?", "Overwrite File", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            var index = 1;
                            ofn = _fileBrowser.OutputLogFileName.Substring(0,
                                      _fileBrowser.OutputLogFileName.Length - 4) +
                                  "." + index;

                            while (File.Exists(ofn + ".txt"))
                            {
                                index++;
                                ofn = ofn.Substring(0, ofn.LastIndexOf('.'));
                                ofn = ofn + "." + index;
                            }
                        }
                        else
                        {
                            File.Delete(_fileBrowser.OutputLogFileName);
                            ofn = _fileBrowser.OutputLogFileName;
                        }
                    }
                    else
                    {
                        ofn = _fileBrowser.OutputLogFileName;
                    }

                    File.WriteAllText(ofn, TbkOutputText.Text);
                    ofn = ofn.Substring(0, ofn.Length - 8) + ".manifest";

                    File.WriteAllLines(ofn, _fileBrowser.TextFileNames);
                    isSaved = true;
                    //}
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    MessageBox.Show(ex.Message, "Unable to write Log File");
                }

            return isSaved;
        }

        internal void ReadFolder()

        {
            try
            {
                if (_fileBrowser != null && !string.IsNullOrWhiteSpace(_fileBrowser.WorkingFolder))
                {
                    Debug.WriteLine("ReadFolder on:- " + _fileBrowser.WorkingFolder);
                    ReadSelectedFiles();
                    _gpxHandler = new GpxHandler(_fileBrowser.WorkingFolder);
                    //sessionForFolder = GetNewRecordingSession(fileBrowser);
                    _sessionForFolder = SessionManager.CreateSession(_fileBrowser.WorkingFolder,
                        SessionManager.GetSessionTag(_fileBrowser), _gpxHandler);
                    //sessionForFolder.OriginalFilePath = fileBrowser.WorkingFolder;
                    /*
                    RecordingSessionForm sessionForm = new RecordingSessionForm();

                    sessionForm.SetRecordingSession(sessionForFolder);
                    if (sessionForm.ShowDialog() ?? false)
                    {
                        sessionForFolder = sessionForm.GetRecordingSession();
                        //DBAccess.UpdateRecordingSession(sessionForFolder);
                        CurrentSessionTag = sessionForFolder.SessionTag;
                        var existingSession = DBAccess.GetRecordingSession(CurrentSessionTag);
                        if (existingSession != null)
                        {
                            //DBAccess.DeleteSession(existingSession);
                            CurrentSessionId = existingSession.Id;
                        }
                        else
                        {
                            CurrentSessionId = 0;
                        }
                    }*/
                }

                if (_sessionForFolder != null)
                {
                    DBAccess.UpdateRecordingSession(_sessionForFolder);
                    var existingSession = DBAccess.GetRecordingSession(_sessionForFolder.SessionTag);
                    CurrentSessionId = existingSession != null ? existingSession.Id : 0;
                }

                // Tools.SetFolderIconTick(fileBrowser.WorkingFolder);
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Import-ReadFolder Failed:-" + ex.Message);
            }
        }

        internal void SortFileOrder()
        {
            if (_fileBrowser != null && _fileBrowser.TextFileNames.Count > 1)
            {
                var fod = new FileOrderDialog();
                fod.Populate(_fileBrowser.TextFileNames);
                var result = fod.ShowDialog();
                if (result != null && result.Value)
                {
                    _fileBrowser.TextFileNames = fod.GetFileList();

                    ReadSelectedFiles();
                }
            }
        }

        /// <summary>
        ///     Batses the concatenate.
        /// </summary>
        /// <param name="totalBatsFound">
        ///     The total bats found.
        /// </param>
        /// <param name="newBatsFound">
        ///     The new bats found.
        /// </param>
        /// <returns>
        /// </returns>
        private Dictionary<string, BatStats> BatsConcatenate(Dictionary<string, BatStats> totalBatsFound,
            Dictionary<string, BatStats> newBatsFound)
        {
            if (totalBatsFound == null || newBatsFound == null) return totalBatsFound;
            if (newBatsFound.Count > 0)
                foreach (var bat in newBatsFound)
                    if (totalBatsFound.ContainsKey(bat.Key))
                        totalBatsFound[bat.Key].Add(bat.Value);
                    else
                        totalBatsFound.Add(bat.Key, bat.Value);
            return totalBatsFound;
        }

        private RecordingSession GetNewRecordingSession(FileBrowser fileBrowser)
        {
            var newSession = new RecordingSession {LocationGPSLatitude = null, LocationGPSLongitude = null};

            newSession = SessionManager.PopulateSession(newSession, fileBrowser);
            return newSession;
        }

        /// <summary>
        ///     Handles the Click event of the ImportFolderButton control. User selects a new
        ///     folder, the Next button is enabled, and auto-magically clicked.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void ImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            ImportPictureControl.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Visible;
            StackPanelScroller.Visibility = Visibility.Visible;
            UpdateRecordingButton.ToolTip = "Update a specific Recording by selecting a single .wav file";
            _processWavFiles = false;
            _fileBrowser = new FileBrowser();
            _fileBrowser.SelectRootFolder();
            NextFolderButton.IsEnabled = true;

            NextFolderButton_Click(sender, e);
        }

        /// <summary>
        ///     Set import pictures mode.  Sets up a window with an image editor
        ///     to allow images to be pasted in without being linked to a segment or call
        ///     or bat.  The caption to the picture should allow it to be allocated
        ///     appropriately.  A bat tag will associate the image with a Bat.
        ///     A .wav filename will attach the image to that recording either
        ///     now or when the recording eventually gets imported.  If the description
        ///     field is populated and matches LabelledSegment, now or later, then the
        ///     image will be associated with that labelledsegment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportPicturesButton_Click(object sender, RoutedEventArgs e)
        {
            StackPanelScroller.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Hidden;
            ImportPictureControl.Visibility = Visibility.Visible;
            UpdateRecordingButton.ToolTip = "Find possible links for orphaned images";
            ImportPictureControl.ImageEntryScroller.SetViewOnly(true);
            ImportPictureControl.ImageEntryScroller.Clear();
            var orphanImages = DBAccess.GetOrphanImages(null);
            if (!orphanImages.IsNullOrEmpty())
                foreach (var image in orphanImages)
                    ImportPictureControl.ImageEntryScroller.AddImage(image);
        }

        /// <summary>
        ///     Imports data using the metdata contined in a set of wav files which have been
        ///     annotated using Kaleidoscope rather than Audacity.  The annotations may be
        ///     encapsulated in either the Name or the Notes tag of the Kaleidoscope (wamd)
        ///     metadata.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportWavFilesButton_Click(object sender, RoutedEventArgs e)
        {
            ImportPictureControl.Visibility = Visibility.Hidden;
            OutputWindowScrollViewer.Visibility = Visibility.Visible;
            StackPanelScroller.Visibility = Visibility.Visible;
            _fileBrowser = new FileBrowser();
            _fileBrowser.SelectRootFolder();
            NextFolderButton.IsEnabled = true;
            UpdateRecordingButton.ToolTip = "Update a specific Recording by selecting a single .wav file";

            NextFolderButton_Click(sender, e);
            var fileList = Directory.EnumerateFiles(_fileBrowser.RootFolder, "*.wav");
            //var FILEList= Directory.EnumerateFiles(fileBrowser.rootFolder, "*.WAV");
            //fileList = fileList.Concat<string>(FILEList);

            var wavfiles = new List<string>(fileList);
            if (wavfiles == null || wavfiles.Count == 0)
            {
                ProcessFilesButton.IsEnabled = false;
                _processWavFiles = false;
                Debug.WriteLine("Non wav files");
            }
            else
            {
                ProcessFilesButton.IsEnabled = true;
                _processWavFiles = true;
                Debug.WriteLine("Process wav files");
            }
        }

        /// <summary>
        ///     Handles the Click event of the NextFolderButton control. Pops the next folder off
        ///     the fileBrowser folder queue, has fileBrowser Process the folder, then calls
        ///     ReadFolder() to load the files into the display. Enables buttons to allow further processing.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void NextFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fileBrowser.WavFileFolders != null && _fileBrowser.WavFileFolders.Count > 0)
            {
                DpMMultiWindowPanel.Children.Clear();
                TbkOutputText.Text = "";
                _fileBrowser.ProcessFolder(_fileBrowser.PopWavFolder());
                ReadFolder();
                SortFileOrderButton.IsEnabled = true;
                ProcessFilesButton.IsEnabled = true;
                FilesToProcessLabel.Content = _fileBrowser.WavFileFolders.Count + " Folders to Process";
                SelectFoldersButton.IsEnabled = _fileBrowser.WavFileFolders.Count > 1;
                NextFolderButton.IsEnabled = _fileBrowser.WavFileFolders.Count > 0;
            }
            else
            {
                SortFileOrderButton.IsEnabled = false;
                ProcessFilesButton.IsEnabled = false;
                SelectFoldersButton.IsEnabled = false;
                NextFolderButton.IsEnabled = false;
            }
        }

        private void ProcessFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessFiles()) Tools.SetFolderIconTick(_fileBrowser.WorkingFolder);
        }

        /// <summary>
        ///     Processes the wav files in the selected folder, extracting Kaleidoscope metadata
        ///     stored in the wamd chunk.  Looks for a header text file or requests one, and allows
        ///     manual data entry for the session data if necessary.
        /// </summary>
        private bool ProcessWavMetadata()
        {
            var result = false;
            var totalBatsFound = new Dictionary<string, BatStats>();

            try
            {
                _gpxHandler = new GpxHandler(_fileBrowser.WorkingFolder);
                //sessionForFolder = GetNewRecordingSession(fileBrowser);
                if (_sessionForFolder == null)
                {
                    _sessionForFolder = SessionManager.CreateSession(_fileBrowser.WorkingFolder,
                        SessionManager.GetSessionTag(_fileBrowser), _gpxHandler);
                }

                var wavFiles = Directory.EnumerateFiles(_fileBrowser.WorkingFolder, "*.wav");
                

                //var WAVFilesEnum = Directory.EnumerateFiles(fileBrowser.WorkingFolder, "*.WAV");
                //var wavFiles = wavFilesEnum.Concat<string>(WAVFilesEnum).ToList<string>();
                if (wavFiles != null && wavFiles.Any())
                {
                    if (_sessionForFolder != null && _sessionForFolder.Id > 0)
                    {
                        TbkOutputText.Text = _sessionForFolder.ToFormattedString();
                        foreach (var rec in _sessionForFolder.Recordings)
                            DBAccess.DeleteRecording(
                                rec); //so that we can recreate them from scrathch using the file data
                    }

                    foreach (var filename in wavFiles)
                        if (!string.IsNullOrWhiteSpace(_fileBrowser.HeaderFileName) &&
                            filename == _fileBrowser.HeaderFileName)
                        {
                            // skip this file if it has been identified as the header data file, since
                            // the information should have been included as the session record header
                            // and this would be a duplicate.
                        }
                        else
                        {
                            TbkOutputText.Text = TbkOutputText.Text + "***\n\n" + FileProcessor.ProcessFile(filename,
                                                     _gpxHandler, _sessionForFolder.Id, ref _fileProcessor.BatsFound) +
                                                 "\n";
                            totalBatsFound = BatsConcatenate(totalBatsFound, _fileProcessor.BatsFound);
                        }

                    TbkOutputText.Text = TbkOutputText.Text + "\n#########\n\n";
                    if (totalBatsFound != null && totalBatsFound.Count > 0)
                        foreach (var bat in totalBatsFound)
                        {
                            bat.Value.batCommonName = bat.Key;
                            TbkOutputText.Text += Tools.GetFormattedBatStats(bat.Value, false) + "\n";

                            //tbkOutputText.Text = tbkOutputText.Text +
                            // FileProcessor.FormattedBatStats(bat) + "\n";
                        }
                }

                if (!string.IsNullOrWhiteSpace(TbkOutputText.Text))
                {
                    SaveOutputFile();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Processing of .wav files failed:- " + ex.Message);
                result = false;
            }

            return result;
        }

        /*
        /// <summary>
        ///     Opens the folder.
        /// </summary>
        internal void OpenFolder()
        {
            if (!String.IsNullOrWhiteSpace(fileBrowser.SelectFolder()))
            {
                ReadFolder();
            }
        }*/

        private void SelectFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fileBrowser?.WavFileFolders != null && _fileBrowser.WavFileFolders.Count > 1)
            {
                var fsd = new FolderSelectionDialog {FolderList = _fileBrowser.WavFileFolders};
                fsd.ShowDialog();
                if (fsd.DialogResult ?? false)
                {
                    _fileBrowser.WavFileFolders = fsd.FolderList;
                    for (var i = 0; i < _fileBrowser.WavFileFolders.Count; i++)
                        _fileBrowser.WavFileFolders[i] = _fileBrowser.WavFileFolders[i].Replace('#', ' ').Trim();

                    //fileBrowser.wavFileFolders = fsd.FolderList;
                    FilesToProcessLabel.Content = _fileBrowser.WavFileFolders.Count + " Folders to Process";
                }
            }
        }

        private void SortFileOrderButton_Click(object sender, RoutedEventArgs e)
        {
            SortFileOrder();
        }

        /// <summary>
        ///     Allows the user to select a .wav file, then finds the corresponding label
        ///     file and updates the existing Recording.  If the label file or recording
        ///     do not exist simply returns without doig anything further.  NB could display a
        ///     message.  Sets a WaitCursor during processing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (ImportPictureControl.Visibility != Visibility.Visible)
            {
                // in normal entry mode, so do Updat Recording
                if (_fileBrowser == null) _fileBrowser = new FileBrowser();
                var filename = _fileBrowser.SelectWavFile();
                if (!string.IsNullOrWhiteSpace(filename))
                    using (new WaitCursor("Updating Recording"))
                    {
                        var recording = DBAccess.GetRecordingForWavFile(filename);
                        var labelFileName = _fileBrowser.GetLabelFileForRecording(recording);
                        if (!string.IsNullOrWhiteSpace(labelFileName))

                            FileProcessor.UpdateRecording(recording, labelFileName);

                    }
            }
            else
            {
                // in image entry mode so try to de-orphanise orphan images
                DBAccess.ResolveOrphanImages();
                ImportPictureControl.ImageEntryScroller.Clear();
                var orphanImages = DBAccess.GetOrphanImages(null);
                if (!orphanImages.IsNullOrEmpty())
                    foreach (var image in orphanImages)
                        ImportPictureControl.ImageEntryScroller.AddImage(image);
            }
        }
    }
}