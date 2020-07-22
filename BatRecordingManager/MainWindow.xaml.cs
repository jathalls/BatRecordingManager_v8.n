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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        ///     The build
        /// </summary>
        private readonly string _build;

        /// <summary>
        ///     The is saved
        /// </summary>
        //private bool isSaved = true;

        /// <summary>
        ///     The window title
        /// </summary>
        /// 
        //private string _windowTitle { get; set; } = "Bat Log Manager - v";

        #region _windowTitle

        /// <summary>
        /// _windowTitle Dependency Property
        /// </summary>
        public static readonly DependencyProperty _windowTitleProperty =
            DependencyProperty.Register("_windowTitle", typeof(string), typeof(MainWindow),
                new FrameworkPropertyMetadata((string)""));

        /// <summary>
        /// Gets or sets the _windowTitle property.  This dependency property 
        /// indicates ....
        /// </summary>
        public string _windowTitle
        {
            get { return (string) GetValue(_windowTitleProperty); }
            set { SetValue(_windowTitleProperty, value); }
        } 

        #endregion



        /// <summary>
        ///     Instance holder for the Analyze and Import class.  If null, then a new folder
        ///     needs to be selectedd, otherwise analyses the next .wav file in the currently
        ///     selected folder.
        /// </summary>
        private AnalyseAndImportClass _analyseAndImport;

        private bool _doingOnClosed;

        private ImportPictureDialog _importPictureDialog;

        private bool _runKaleidoscope;

        private bool _useCurrentSession;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            try
            {
                Application.Current.MainWindow = this;
                try
                {
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    
                    DBAccess.InitializeDatabase();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in Main Window prior to Initialisation");
                    Tools.ErrorLog("Error in Main Window prior to Initialisation " + ex.Message);
                }

                _windowTitle = "Bat Log Manager v";
                InitializeComponent();
                
                try
                {
                    ShowDatabase = App.ShowDatabase;
                    DataContext = this;

                    System.Windows.Data.Binding binding = new System.Windows.Data.Binding(nameof(_windowTitle));
                    binding.Source = this;
                    //System.Windows.Data.BindingOperations.SetBinding(Title, TitleProperty, binding);
                    this.SetBinding(TitleProperty, binding);


                    //statusText = "Starting Up";
                    //SetStatusText("Starting Up");
                    _build = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    var buildDateTime = new DateTime(2000, 1, 1);
                    var buildParts = _build.Split('.');
                    if (buildParts.Length >= 4)
                    {
                        int.TryParse(buildParts[2], out var days);
                        int.TryParse(buildParts[3], out var seconds);
                        if (days > 0) buildDateTime = buildDateTime.AddDays(days);
                        if (seconds > 0) buildDateTime = buildDateTime.AddSeconds(seconds * 2);
                    }

                    if (buildDateTime.Ticks > 0L)
                        _build = _build + " (" + buildDateTime.Date.ToShortDateString() + " " +
                                 buildDateTime.TimeOfDay +
                                 ")";
                    //windowTitle = "Bat Log File Processor " + Build;
                    //Title = _windowTitle + " " + _build;
                    _windowTitle = _windowTitle + _build;
                    Console.WriteLine(_windowTitle);

                    InvalidateArrange();
                    //DBAccess.InitializeDatabase();

                    BatRecordingListDetailControl.SessionsAndRecordings.e_SessionAction +=
                        SessionsAndRecordings_SessionAction;
                    miRecordingSearch_Click(this, new RoutedEventArgs());
                    //SetStatusText("");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in Main Window following Initialization");
                    Tools.ErrorLog("Error in Main Window following Initialization " + ex.Message);
                }

                try
                {
                    if (!MainWindowPaneGrid.Children.Contains(recordingSessionListControl))
                        MainWindowPaneGrid.Children.Add(recordingSessionListControl);
                    recordingSessionListControl.Visibility = Visibility.Visible;
                    recordingSessionListControl.RefreshData();
                    SetTitle();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in Main Window showing SessionsPane");
                    Tools.ErrorLog("Error in Main Window showing sessions pane " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex + "\n" + ex.Message);
            }
        }

        public void SetTitle()
        {
            _windowTitle = $"Bat Recording Manager v {_build} using {DBAccess.GetWorkingDatabaseName(DBAccess.GetWorkingDatabaseLocation())}";
            InvalidateVisual();
        }


        private ImportControl importControl { get; } = new ImportControl();
        private BatListControl batListControl { get; } = new BatListControl();

        private RecordingSessionListDetailControl recordingSessionListControl { get; } =
            new RecordingSessionListDetailControl();

        private BatRecordingsListDetailControl BatRecordingListDetailControl { get; } =
            new BatRecordingsListDetailControl();

        /// <summary>
        ///     Flag to indicate if the database listing item in help is enabled
        /// </summary>
        public bool ShowDatabase { get; set; }

        /// <summary>
        ///     Display the About box
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutScreen {Version = {Content = "v " + _build}};
            about.ShowDialog();
        }

        

        /// <summary>
        ///     Handles the Click event of the miBatReference control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miBatReference_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switch to Bat Reference Pane"))
            {
                HideAllControlPanes();
                if (!MainWindowPaneGrid.Children.Contains(batListControl))
                    MainWindowPaneGrid.Children.Add(batListControl);

                batListControl.Visibility = Visibility.Visible;
                batListControl.RefreshData();
                InvalidateArrange();
            }
        }

        private void HideAllControlPanes()
        {
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Hidden;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void GetSelectedItems(out RecordingSession session, out Recording recording, out Bat bat)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            session = null;
            recording = null;
            bat = null;
            if (recordingSessionListControl != null)
            {
                session = recordingSessionListControl.GetSelectedSession();
                recording = recordingSessionListControl.GetSelectedRecording();
            }

            if (batListControl != null) bat = batListControl.GetSelectedBat();
        }

        /// <summary>
        ///     Handles the Click event of the miBatSearch control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miBatSearch_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                HideAllControlPanes();
                if (!MainWindowPaneGrid.Children.Contains(BatRecordingListDetailControl))
                    MainWindowPaneGrid.Children.Add(BatRecordingListDetailControl);

                BatRecordingListDetailControl.Visibility = Visibility.Visible;

                BatRecordingListDetailControl.RefreshData();

                //this.InvalidateArrange();
                //this.UpdateLayout();
            }
        }

        private void miCreateDatabase_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = DBAccess.GetWorkingDatabaseLocation(),
                FileName = "_BatReferenceDB.mdf",
                DefaultExt = ".mdf"
            };
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var index = 1;
                while (File.Exists(dialog.FileName))
                    dialog.FileName = dialog.FileName.Substring(0, dialog.FileName.Length - 4) + index + ".mdf";
                using (new WaitCursor("Creating new empty database"))
                {
                    try
                    {
                        var err = DBAccess.CreateDatabase(dialog.FileName);
                        if (!string.IsNullOrWhiteSpace(err))
                        {
                            MessageBox.Show(err, "Unable to create database");
                        }
                        else
                        {
                            err = DBAccess.SetDatabase(dialog.FileName);
                            if (!string.IsNullOrWhiteSpace(err))
                                MessageBox.Show(err, "Unable to set new DataContext for selected Database");
                            using (new WaitCursor("Refreshing the display"))
                            {
                                RefreshAll();
                                miRecordingSearch_Click(sender, e);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the miDatabase control. Allows the user to select an
        ///     alternative .mdf database file with the name BatReferenceDB but in an alternative
        ///     location. Selection of a different filename will be rejected in case the database
        ///     structure is different. The location of the selected file will be stired in the
        ///     global static App.dbFileLocation variable whence it can be referenced by the
        ///     DBAccess static functions.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miDatabase_Click(object sender, RoutedEventArgs e)
        {
            var workingFolder = DBAccess.GetWorkingDatabaseLocation();
            if (string.IsNullOrWhiteSpace(workingFolder) || !Directory.Exists(workingFolder))
            {
                
                workingFolder = DBAccess.GetWorkingDatabaseLocation();
            }

            using (var dialog = new OpenFileDialog())
            {
                if (!string.IsNullOrWhiteSpace(workingFolder))
                {
                    dialog.InitialDirectory = workingFolder;
                }
                else
                {
                    workingFolder = Directory.GetCurrentDirectory();
                    dialog.InitialDirectory = workingFolder;
                }

                dialog.Filter = "mdf files|*.mdf";

                dialog.Multiselect = false;
                dialog.Title = "Select An Alternative BatReferenceDB.mdf database file";
                dialog.DefaultExt = ".mdf";

                dialog.FileName = "*.mdf";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var valid = DBAccess.ValidateDatabase(dialog.FileName);
                    if (valid == "bad")
                    {
                        MessageBox.Show(@"The selected file is not a valid BatRecordingManager Database.
                            Please reselect");
                    }
                    else
                    {
                        if (valid == "old")
                        {
                            var mbResult = MessageBox.Show(@"The elected file is an earlier version database.
Do you wish to update that database to the latest specification?", "Out of Date Database", MessageBoxButton.YesNo);
                            if (mbResult == MessageBoxResult.Yes)
                                using (new WaitCursor("Opening new database..."))
                                {
                                    DBAccess.SetDatabase(dialog.FileName);
                                    RefreshAll();
                                    miRecordingSearch_Click(sender, e);
                                }
                        }
                        else
                        {
                            using (new WaitCursor("Opening new database..."))
                            {
                                DBAccess.SetDatabase(dialog.FileName);
                                RefreshAll();
                                miRecordingSearch_Click(sender, e);
                            }
                        }
                    }
                }
            }
            SetTitle();
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void RefreshAll()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            using (new WaitCursor("Refreshing Data"))
            {
                recordingSessionListControl.RefreshData();
                BatRecordingListDetailControl.RefreshData();
                batListControl.RefreshData();
            }
        }

        /// <summary>
        ///     Quits the program
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            Close();

            //App.Current.Shutdown();
            //Environment.Exit(0);
        }

        /// <summary>
        ///     Handles the Click event of the miHelp control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpfile = @"Bat Recording Manager.chm";
            if (File.Exists(helpfile)) Help.ShowHelp(null, helpfile);
            /*
            HelpScreen help = new HelpScreen();
            help.ShowDialog();*/
        }

        /// <summary>
        ///     Handles the Click event of the miNewLogFile control. Now the Import item
        ///     to import analysis data from .txt files or .wav file metadata.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miNewLogFile_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switching to Import View"))
            {
                HideAllControlPanes();

                if (!MainWindowPaneGrid.Children.Contains(importControl))
                    MainWindowPaneGrid.Children.Add(importControl);

                importControl.Visibility = Visibility.Visible;
                InvalidateArrange();
                UpdateLayout();
            }
        }

        /// <summary>
        ///     Handles the Click event of the miRecordingSearch control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void miRecordingSearch_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switching to Sessions View..."))
            {
                HideAllControlPanes();
                recordingSessionListControl.Visibility = Visibility.Visible;
                recordingSessionListControl.RefreshData();
                InvalidateArrange();
                UpdateLayout();
            }
        }

        private void miSetToDefaultDatabase_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Switching to default database"))
            {
                DBAccess.SetDatabase(null);
                RefreshAll();
                miRecordingSearch_Click(sender, e);
            }
            SetTitle();
        }

        /// <summary>
        ///     Handles the SessionAction event of the SessionsAndRecordings control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="SessionActionEventArgs" /> instance containing the event data.
        /// </param>
        private void SessionsAndRecordings_SessionAction(object sender, SessionActionEventArgs e)
        {
            miRecordingSearch_Click(this, new RoutedEventArgs());
            recordingSessionListControl.Select(e.RecordingSessionId);
        }

        /// <summary>
        ///     Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.ComponentModel.CancelEventArgs" /> instance containing the
        ///     event data.
        /// </param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DBAccess.CloseDatabase();
            AudioHost.Instance.Close();
            ComparisonHost.Instance.Close();
        }

        /// <summary>
        ///     Menu Item to analyse a folder full of .wav files using Audacity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miAnalyseFiles_Click(object sender, RoutedEventArgs e)
        {
            _runKaleidoscope = false || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            _useCurrentSession = false || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            if (_analyseAndImport == null)
            {
                _importPictureDialog = new ImportPictureDialog();
                if (_useCurrentSession )
                {
                    var currentSession = recordingSessionListControl.GetSelectedSession();
                    if (currentSession != null)
                    {
                        _analyseAndImport=new AnalyseAndImportClass(currentSession.SessionTag);
                    }
                    else
                    {
                        _analyseAndImport=new AnalyseAndImportClass();
                    }
                }
                else
                {
                    _analyseAndImport = new AnalyseAndImportClass();
                }

                _analyseAndImport.e_Analysing += AnalyseAndImport_Analysing;
                _analyseAndImport.e_DataUpdated += AnalyseAndImport_DataUpdated;
                _analyseAndImport.e_AnalysingFinished += AnalyseAndImport_AnalysingFinished;
                if (_analyseAndImport == null || !_analyseAndImport.FolderSelected)
                {
                    MessageBox.Show("No Folder selected for analysis");
                    _analyseAndImport = null;
                    if (_importPictureDialog != null)
                    {
                        _importPictureDialog.Close();
                        _importPictureDialog = null;
                    }

                    return;
                }

                _importPictureDialog.Show();
                using (new WaitCursor("Import from Audacity or Kaleidoscope"))
                {
                    if (_runKaleidoscope)
                    {
                        _importPictureDialog.GotFocus += ImportPictureDialog_GotFocus;
                        _analyseAndImport.ImportFromKaleidoscope();
                    }
                    else
                    {
                        _analyseAndImport.AnalyseNextFile();
                    }
                }
            }
        }

        /// <summary>
        ///     For Kaleidoscope, generates an event when the import picture dialog gets the focus.
        ///     Causes the title of the current file to be placed in the image caption
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportPictureDialog_GotFocus(object sender, RoutedEventArgs e)
        {
            var windows = OpenWindowGetter.GetOpenWindows();

            foreach (var win in windows)
                if (win.Value.ToUpper().EndsWith(".WAV"))
                {
                    SetImportImageCaption(win.Value);
                    return;
                }
        }

        /// <summary>
        ///     responds to the Analysing event from AnalyseAndImport - supplies the name of
        ///     the file currently being analysed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnalyseAndImport_Analysing(object sender, EventArgs e)
        {
            var aea = e as AnalysingEventArgs;
            var fileName = aea.FileName;
            SetImportImageCaption(fileName);
        }

        private void SetImportImageCaption(string caption)
        {
            _importPictureDialog?.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => { _importPictureDialog.SetCaption(caption); }));
        }

        /// <summary>
        ///     Event handler when there are no more files to analyse or Audacity was closed without
        ///     producing a matching text file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnalyseAndImport_AnalysingFinished(object sender, EventArgs e)
        {
            try
            {
                if (_analyseAndImport != null)
                {
                    _analyseAndImport?.Close();

                    _analyseAndImport = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error closing AnalyseAndImport:- "+ex.Message);
                Tools.ErrorLog("Error closing AnalyseAndImport:- " + ex.Message);
            }

            try
            {
                _importPictureDialog?.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        DBAccess.ResolveOrphanImages();
                        _importPictureDialog?.Close();
                        _importPictureDialog = null;
                    }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error invoking a close of import picture dialog when ending analyse and import:- "+ex.Message);
                Tools.ErrorLog("Error invoking a close of import picture dialog when ending analyse and import:- " + ex.Message);
            }
        }

        /// <summary>
        ///     event raised when the database has been updated by AnalyseAndImport
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AnalyseAndImport_DataUpdated(object sender, EventArgs e)
        {
            if (sender != null && (sender is AnalyseAndImportClass))
            {
                recordingSessionListControl?.RefreshData();
                var UpdatedSessionTag = (sender as AnalyseAndImportClass).SessionTag;
                if (!string.IsNullOrWhiteSpace(UpdatedSessionTag) && _runKaleidoscope)
                {
                    var mbResult = MessageBox.Show("Do you wish to Generate a report for this dataset?",
                        "Generate Report?", MessageBoxButton.YesNo);
                    if (mbResult == MessageBoxResult.Yes)
                        recordingSessionListControl.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            //Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                //recordingSessionListControl.RefreshData();
                                //recordingSessionListControl.SelectSession(sessionUpdated);

                                //recordingSessionListControl.ReportSessionDataButton_Click(sender,
                                    //new RoutedEventArgs());
                                    RecordingSession upDatedSession = DBAccess.GetRecordingSession(UpdatedSessionTag);
                                    recordingSessionListControl.GenerateReportSet(upDatedSession, true);
                            }));
                }
            }

            if (_analyseAndImport != null)
            {
                _analyseAndImport.Close();

                _analyseAndImport = null;
            }
        }

        private void miImportBatData_Click(object sender, RoutedEventArgs e)
        {
            using (var fileDialog = new OpenFileDialog())
            {
                fileDialog.AddExtension = true;
                fileDialog.CheckFileExists = true;
                fileDialog.DefaultExt = ".xml";
                fileDialog.Multiselect = false;
                fileDialog.Title = "Import Bat Data from XML file";
                fileDialog.Filter = "XML files (*.xml)|*.xml|All Files (*.*)|*.*";

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var selectedFile = fileDialog.FileName;
                    if (File.Exists(selectedFile))
                        using (new WaitCursor("Importing new bat reference data"))
                        {
                            DBAccess.CopyXmlDataToDatabase(selectedFile);
                        }
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!_doingOnClosed)
            {
                _doingOnClosed = true;
                base.OnClosed(e);
                Application.Current.Shutdown();
            }
        }

        private void MiDatabaseDisplay_Click(object sender, RoutedEventArgs e)
        {
            Database display = null;
            try
            {
                display = new Database();
            }
            catch (NullReferenceException nre)
            {
                Trace.WriteLine("Database display creation null reference exception:- " + nre.StackTrace);
            }

            try
            {
                display?.Show();
            }
            catch (NullReferenceException nre)
            {
                Trace.WriteLine("display show null reference exception: " + nre.StackTrace);
            }
        }

        #region statusText
        /*
        /// <summary>
        ///     statusText Dependency Property
        /// </summary>
        public static readonly DependencyProperty statusTextProperty =
            DependencyProperty.Register(nameof(statusText), typeof(string), typeof(MainWindow),
                new FrameworkPropertyMetadata(""));

        /// <summary>
        ///     Gets or sets the statusText property.  This dependency property
        ///     indicates ....
        /// </summary>
        public string statusText
        {
            get => (string) GetValue(statusTextProperty);
            set => SetValue(statusTextProperty, value);
        }*/

        public string statusText { get; set; }

        #endregion

        private void StatusText_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Debug.WriteLine($"Status text changed to:-{StatusText.Text}");
        }

        /// <summary>
        /// Menu item to copy the database.  The user chooses a location from the save file dialog
        /// and the current .mdf and .ldf files are copied to the new location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiCopyDatabase_Click(object sender, RoutedEventArgs e)
        {
            var dc = DBAccess.GetFastDataContext();
            dc.Connection.Close();
            var destination = Tools.GetFileToWriteTo("", ".mdf");
            if (!destination.EndsWith(".mdf"))
            {
                destination += ".mdf";
            }

            var source = DBAccess.GetWorkingDatabaseLocation();
            source = source+DBAccess.GetWorkingDatabaseName(source);
            File.Copy(source,destination);

        }

        /// <summary>
        /// Expors the entire contents of the database to an XML file in text format.
        /// This will be a BIG file and will take a LONG time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiExportDatabase_Click(object sender, RoutedEventArgs e)
        {
            // Not yet implemented
        }

        /// <summary>
        /// Extracts .wav files from a selected folder which contain certainn keywords, to a suitably named
        /// sub-folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiFilter_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = "";

#if DEBUG
            string actual = @"C:\BRMTestData\Actual\";
            string original = @"C:\BRMTestData\Original\";
            using (new WaitCursor())
            {
                
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (Directory.Exists(actual))
                    {
                        Tools.DirectoryDelete(actual);
                    }
                    if (Directory.Exists(original))
                    {
                        Tools.DirectoryCopy(original, actual, true);
                    }
                }
            }
            folderPath = Path.GetDirectoryName(actual+@"WAVFILES.DIR\");
#endif //DEBUG
            
            if(recordingSessionListControl!=null)
            {
                var sess = recordingSessionListControl.GetSelectedSession();
                if (sess != null)
                {
                    folderPath = sess.OriginalFilePath;
                }
            }
            AppMainWindow amw=new AppMainWindow();
            AppFilter appFilter = new AppFilter();
            appFilter.SetDefaultFolderPath(folderPath);
            amw.DisplayControl(appFilter);
            amw.Show();


            //AppFilter appFilter=new AppFilter();


        }

        private void miAnalyse_Click(object sender, RoutedEventArgs e)
        {
            string workingFolder=Environment.CurrentDirectory;
            if (Directory.Exists(workingFolder))
            {
                string excutable = Path.Combine(new string[]{ workingFolder,@"PulseTrainAnalysis.exe"});
                Process ExternalProcess = new Process();
                if (ExternalProcess == null) return;

                ExternalProcess.StartInfo.FileName = excutable;
                //externalProcess.StartInfo.Arguments = folder;
                ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;



                ExternalProcess.Start();
            }
        }
    }
}