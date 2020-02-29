using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for AppFilter.xaml
    /// </summary>
    public partial class AppFilter : UserControl
    {
        private IEnumerable<String> _parentFileList;

        private List<String> _filteredFileList;

        private List<string> _errors = new List<string>();

        /// <summary>
        /// Name of the parent folder holding files to be filtered
        /// </summary>
        #region _parentFolderPath

        /// <summary>
        /// _parentFolderPath Dependency Property
        /// </summary>
        public static readonly DependencyProperty _parentFolderPathProperty =
            DependencyProperty.Register("_parentFolderPath", typeof(string), typeof(AppFilter),
                new FrameworkPropertyMetadata((string)""));

        /// <summary>
        /// Gets or sets the _parentFolderPath property.  This dependency property 
        /// indicates ....
        /// </summary>
        public string _parentFolderPath
        {
            get { return (string)GetValue(_parentFolderPathProperty); }
            set { SetValue(_parentFolderPathProperty, value); }
        }

        #endregion


        //public String _parentFolderPath { get; set; } = "";

        public String _defaultSubFolderName { get; set; }

        /// <summary>
        /// cumulative status string which is bound to statusTextBlock.text
        /// </summary>
        //public string statusText { get; set; } = "";
        #region statusText

        /// <summary>
        /// statusText Dependency Property
        /// </summary>
        public static readonly DependencyProperty statusTextProperty =
            DependencyProperty.Register("statusText", typeof(string), typeof(AppFilter),
                new FrameworkPropertyMetadata((string)""));

        /// <summary>
        /// Gets or sets the statusText property.  This dependency property 
        /// indicates ....
        /// </summary>
        public string statusText
        {
            get { return (string)GetValue(statusTextProperty); }
            set { SetValue(statusTextProperty, value); }
        }

        #endregion



        public AppFilter()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void SetDefaultFolderPath(string folderPath)
        {
            
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                if (!folderPath.EndsWith(@"\"))
                {
                    folderPath += @"\";
                }
                if (Directory.Exists(folderPath))
                {
                    _parentFolderPath = folderPath;
                    _defaultSubFolderName = _parentFolderPath + @"Filtered\";
                    _parentFileList = Directory.EnumerateFiles(_parentFolderPath, "*.wav");
                }
                else
                {
                    _parentFolderPath = "";
                    _defaultSubFolderName = "";
                    _parentFileList = (new List<string>()).AsEnumerable();
                }
            }
            else
            {
                _parentFolderPath = "";
                _defaultSubFolderName = "";
                _parentFileList = (new List<string>()).AsEnumerable();
            }
            _filteredFileList = new List<string>();
            statusText += $"Set parent folder to {_parentFolderPath}\n";
        }

        private void AppFilterSelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string FolderPath=Tools.SelectWavFileFolder("");
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
            {
                _ = MessageBox.Show($"Directory not found, unable to search", "Directory not found", MessageBoxButton.OK);
                return;
            }
            SetDefaultFolderPath(FolderPath);
            //_parentFileList = Directory.EnumerateFiles(_parentFolderPath, "*.wav");
            //AppFilterFolderText.Text = _parentFolderPath;
            

        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _errors = new List<string>();
            string destination = _defaultSubFolderName;
            Debug.WriteLine("Extract...");
            if (string.IsNullOrWhiteSpace(_parentFolderPath))
            {
                statusText += $"Extract Failed - parent folder is <{_parentFolderPath}>\n";
                return;       // no folder selected for filtering yet
            }
            if (!Directory.Exists(_parentFolderPath))
            {
                statusText += $"Extract Failed - parent folder {_parentFolderPath} does not exist\n";
                return;               // selected folder doesn't exist
            }
            
            if (_parentFileList == null || !_parentFileList.Any())
            {
                statusText += $"Extract Failed - No files in the parent folder {_parentFolderPath}\n";
                return;  // no .wav files in the selected folder
            }
            if (!_parentFolderPath.EndsWith(@"\")) _parentFolderPath += @"\";

            
            
            _defaultSubFolderName = _parentFolderPath + @"Filtered\";
            if (!Directory.Exists(_defaultSubFolderName))
            {
                Debug.WriteLine("no default so transfer all to " + _defaultSubFolderName);
                //TransferFilteredFiles(); // will create _defaultSubFolderName
                //return;
            }

            else if (!Directory.EnumerateFiles(_defaultSubFolderName).Any())
            {
                Debug.WriteLine("default empty so transfer all to " + _defaultSubFolderName);
                //TransferFilteredFiles(); // no existing files to deal with
                //return;
            }
            // if here, the sub-folder exists and has files in it
            else if (AppFilterNewFolder.IsChecked ?? false)
            {

                // new folder box ticked, so ignore existing folder or folders and create a new one to take the new files
                string newSubFolderName = CreateNewSubFolder();
                Debug.WriteLine($"make a new folder <{newSubFolderName}>");
                if (string.IsNullOrWhiteSpace(newSubFolderName))
                {
                    return;
                }
                destination = newSubFolderName;
                Debug.WriteLine("... and transfer files to " + newSubFolderName);
                //TransferFilteredFiles(newSubFolderName);
                //return;

            }
            else
            {
                // if here there is an existing sub-folder and the new files are to be merged with it after re-merging existing files to the database
                Debug.WriteLine("Restore first, then transfer files");
                int NumberOfFilesRestored=RestoreFilteredFiles(_defaultSubFolderName);
                statusText += $"Restored {NumberOfFilesRestored} from {_defaultSubFolderName} Prior to extraction\n";
            }
            if (Directory.Exists(_parentFolderPath)) _parentFileList = Directory.EnumerateFiles(_parentFolderPath, "*.wav");
            _filteredFileList = ApplyFilter(_parentFileList);                                                  // run the filter
            if (_filteredFileList == null || !_filteredFileList.Any())
            {
                statusText += $"No files extracted - No files matched the filter\n";
                return; // no files returned by the filter
            }

            int numberOfFiles=TransferFilteredFiles(destination);
            statusText += $"Extracted {numberOfFiles} files to {destination}\n";
            


        }

        /// <summary>
        /// Locates any files in the designated folder which no longer contain any of the keywords
        /// and remerge them into the original folder, updating the database at the same time
        /// </summary>
        private int RestoreFilteredFiles(string filteredFileFolderName)
        {
            _errors = new List<string>();
            List<string> filesToRestore = unFilterFiles(filteredFileFolderName);
            foreach (string file in filesToRestore??new List<string>())
            {
                RestoreFile(file);
            }
            return (filesToRestore.Count);
        }

        /// <summary>
        /// moves the specified file back to the parent folder along with any associated .txt sidecar file,
        /// and either enters it into the database or updates the database with the revised notations
        /// </summary>
        /// <param name="file"></param>
        private void RestoreFile(string file)
        {
            TransferFile(file, _parentFolderPath, true);
            string txtFileName = ChangeExtensionToTxt(file);
            if (File.Exists(txtFileName))
            {
                TransferFile(txtFileName, _parentFolderPath, true);
            }

            UpdateDatabase(_parentFolderPath+Tools.ExtractWavFilename(file));
        }

        /// <summary>
        /// Given the fully qualified path and name of a .wav file, (which may contain embedded
        /// WAMD/GUIANO data or have an associated .txt sidecar file), updates the database to
        /// include that file.  If the database already contains a record for the file then that
        /// record is updated.  If not, then the file data is added to a session which uses the same
        /// folder path.  If that cannot be found then a new session form is presented.
        /// </summary>
        /// <param name="v"></param>
        private void UpdateDatabase(string wavFile)
        {
            RecordingSession existingSession = null;
            if (string.IsNullOrWhiteSpace(wavFile)) return;
            if (!File.Exists(wavFile)) return;
            Recording existingRecording = DBAccess.GetRecordingForWavFile(wavFile);
            if (existingRecording == null)
            {
                
                existingSession = DBAccess.GetRecordingSessionForWavFile(wavFile);
            }
            else
            {
                existingSession = existingRecording.RecordingSession;
            }

            if (existingSession == null)
            {

                existingSession = SessionManager.CreateSession(Tools.GetPath(wavFile));// creates a session with a new tag, opens for edit and saves it
            }
            existingSession.ImportWavFile(wavFile);
        }

        /// <summary>
        /// Identifies a list of all files in the specified folder which do not contain
        /// any of the keywords
        /// </summary>
        /// <param name="filteredFileFolderName"></param>
        /// <returns></returns>
        private List<string> unFilterFiles(string filteredFileFolderName)
        {
            List<string> selectedFiles=new List<string>();
            var filesInFolder = Directory.EnumerateFiles(filteredFileFolderName, "*.wav");
            var filteredFiles = ApplyFilter(filesInFolder.AsEnumerable());
            foreach (var file in filesInFolder)
            {
                if (!filteredFiles.Contains(file))
                {
                    selectedFiles.Add(file);
                }
            }

            return (selectedFiles);
        }

        /// <summary>
        /// Creates a new filtered sub-folder with a unique name derived from Filtered
        /// </summary>
        /// <returns></returns>
        private string CreateNewSubFolder()
        {
            string folderName = _defaultSubFolderName;
            int suffix = 1;
            while (Directory.Exists(folderName))
            {
                if (_defaultSubFolderName.EndsWith(@"\"))
                {
                    _defaultSubFolderName = _defaultSubFolderName.Substring(0, _defaultSubFolderName.Length - 1);
                }
                folderName = _defaultSubFolderName + suffix.ToString()+@"\";
                suffix++;
                if (suffix > 100)
                {
                    var response =
                        MessageBox.Show(
                            "Over 100 filtered sub-folders is getting a bit silly. Do you wish to continue?",
                            "Excessive Sub-Folders", MessageBoxButton.YesNo);
                    if (response == MessageBoxResult.No)
                    {
                        return ("");
                    }
                }
            }

            return (folderName);
        }

        /// <summary>
        /// Copies or Moves (depending on the checkbox state) all the fles in the filtered file list
        /// from the parent folder to the sub-folder
        /// </summary>
        private int TransferFilteredFiles(string newSubFolderName)
        {
            
            bool move = AppFilterMoveFiles.IsChecked ?? false;
            if (!Directory.Exists(newSubFolderName))
            {
                Directory.CreateDirectory(newSubFolderName);
            }
            renameExistingFilesToBak(newSubFolderName);
            deleteWavFiles(newSubFolderName);
            int filesTransferred = 0;
            foreach (string file in _filteredFileList)
            {
                TransferFile(file,newSubFolderName,move);
                string txtFileName = ChangeExtensionToTxt(file);
                if (File.Exists(txtFileName))
                {
                    TransferFile(txtFileName,newSubFolderName,move);
                    
                }
                filesTransferred++;
            }
            return (filesTransferred);
        }

        private void deleteWavFiles(string newSubFolderName)
        {
            var files = Directory.EnumerateFiles(newSubFolderName, "*.wav");
            foreach(var file in files)
            {
                File.Delete(file);
            }
        }

        private void renameExistingFilesToBak(string newSubFolderName)
        {
            var files = Directory.EnumerateFiles(newSubFolderName,"*.wav");
            foreach(var file in files)
            {
                if (File.Exists(file + ".bak"))
                {
                    File.Delete(file + ".bak");
                }
                File.Move(file, file + ".bak");
            }
        }

        /// <summary>
        /// Copies or moves, depending on move flag, the specified file to the specified folder
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newSubFolderName"></param>
        /// <param name="move"></param>
        public static void TransferFile(string file, string newSubFolderName, bool move)
        {
            var created = File.GetCreationTime(file);
            var written = File.GetLastWriteTime(file);
            string nameOnly = System.IO.Path.GetFileName(file);
            string destination = System.IO.Path.Combine(newSubFolderName, nameOnly);
            if (File.Exists(destination))
            {
                if (File.Exists(destination + ".bak"))
                {
                    File.Delete(destination+".bak");
                }
                File.Move(destination, destination + ".bak");
            }
            if (move)
            {
                
                File.Move(file,destination);
            }
            else
            {
                File.Copy(file,destination);
            }
            File.SetCreationTime(destination,created);
            File.SetLastWriteTime(destination,written);
        }

        /// <summary>
        /// Copies or Moves (depending on the checkbox state) all the fles in the filtered file list
        /// from the parent folder to the sub-folder
        /// </summary>
        private int TransferFilteredFiles()
        {
            return(TransferFilteredFiles(_defaultSubFolderName));
        }

        /// <summary>
        /// Assuming that there is a defined source folder containing .wav files, goes through those files
        /// identifying all those with a WAMD or GUANO comment containing any of the defined keywords, or
        /// any that have a sidecar .txt file containing any of the defined keywords, while obeying the
        /// rules defined by the appropriate check boxes.  The files containing the keywords are listed in
        /// _filteredFileList.
        /// </summary>
        public List<string> ApplyFilter(IEnumerable<string> parentFileList)
        {
            var filteredFileList = new List<string>();
            if (parentFileList == null || !parentFileList.Any()) return(filteredFileList);

            string parentFolderPath = Tools.GetPath(parentFileList.First());
            using (new WaitCursor())
            {
                
                
                foreach (var fileName in parentFileList)
                {
                    if(!File.Exists(fileName)) continue;
                    string comments = GetCommentsForFile(fileName);
                    if (comments == null) continue;
                    if (ContainsKeywords(comments))
                    {
                        Debug.WriteLine($"<{comments}> contains a keyword fomr fileName");
                        filteredFileList.Add(fileName);
                    }
                    else
                    {
                        Debug.WriteLine($"{fileName} does not contain a keyword");
                    }
                }
            }
            Debug.WriteLine("FilteredFileList:-");
            foreach(var name in filteredFileList)
            {
                Debug.Write($"\t->\t{name}");
                if (_errors.Contains(name))
                {
                    Debug.WriteLine(" Was not examined!");
                }
                else
                {
                    Debug.WriteLine("");
                }
            }
            if (_errors.Any())
            {
                string failedFiles = "Unable to search the following files:-\n";
                foreach(var file in _errors)
                {
                    failedFiles += file + "\n";
                }
                _ = MessageBox.Show(failedFiles, "File search error", MessageBoxButton.OK);
            }
            return (filteredFileList);
        }

        /// <summary>
        /// Searches the supplied text for any of the keywords listed in the combobox and returns true if any
        /// of them are present taking into account the flags in the check boxes
        /// </summary>
        /// <param name="comments"></param>
        /// <returns></returns>
        public bool ContainsKeywords(string comments)
        {
            bool matchCase = AppFilterMatchCase.IsChecked ?? false;
            bool bracketed = AppFilterBrackets.IsChecked ?? false;
            var keywords = AppFilterComboBox.Items;
            List<String> keywordList=new List<string>();
            foreach (var item in keywords)
            {
                keywordList.Add(item as String);
            }

            if (keywordList.IsNullOrEmpty()) return (true); // if no keywords, all files are selected
            foreach (string key in keywordList)
            {
                if (ContainsKeyword(comments, key, matchCase, bracketed))
                {
                    return (true);
                }
            }

            return (false);


        }

        /// <summary>
        /// Searches the given string for a single keyword, applying the specified flags
        /// </summary>
        /// <param name="comments"></param>
        /// <param name="key"></param>
        /// <param name="matchCase"></param>
        /// <param name="bracketed"></param>
        /// <returns></returns>
        public bool ContainsKeyword(string comments, string key, bool matchCase, bool bracketed)
        {
            if (key == null) return (false);
            if (key == "<EMPTY>" && string.IsNullOrWhiteSpace(comments)) return (true);
            if (string.IsNullOrEmpty(comments)) return (false);
            if (!matchCase)
            {
                comments = comments.ToUpper();
                key = key.ToUpper();
            }

            if (bracketed)
            {
                comments = comments.Replace('{', ' ');
                comments = comments.Replace('}', ' ');
            }
            else
            {
                string pattern = @"({.*[}\n\r$])+";
                comments=Regex.Replace(comments??" ", pattern, " ");
            }
            Debug.WriteLine($"Finding /{key}/ in /{comments}/ case={matchCase} bracketed={bracketed}");

            if (comments.Contains(key??"")) return (true);
            return (false);
        }

        /// <summary>
        /// Extracts comments from .wav file metadata in GUANO or WAMD format and concatenates it
        /// into a single string which is returned.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetCommentsForFile(string fileName)
        {
            string result = "";
            if (!fileName.ToUpper().EndsWith(".WAV")) return (result);
            var wavFileMetadata=new WavFileMetaData(fileName);
            if (!wavFileMetadata.success)
            {
                _errors.Add(fileName);
                return (null);
            }
            if (AppFilterSearchNotes.IsChecked ?? false) result += wavFileMetadata.m_Note;
            if (AppFilterSearchManualID.IsChecked ?? true) result += " " + wavFileMetadata.m_ManualID;
            if (AppFilterSearchAutoId.IsChecked ?? false) result += " " + wavFileMetadata.m_AutoID;
            result = result.Trim();
            
            fileName = ChangeExtensionToTxt(fileName);
            if (File.Exists(fileName))
            {
                result += (" " + File.ReadAllText(fileName)).Trim();
            }

            return (result);
        }

        /// <summary>
        /// for a fully qualified or short file name changes the last four
        /// characters to .txt
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string ChangeExtensionToTxt(string fileName)
        {
            return (fileName.Substring(0, fileName.Length - 4)) + ".txt";
        }



        ///event handle arguments for OKClicked
        public class CloseClickedEventArgs : EventArgs
        {
            /// parameter for event args
            public readonly decimal value;
            /// constructor for event args
            public CloseClickedEventArgs(decimal value)
            {
                this.value = value;
            }

        }
        ///OKClicked eventhandler
        public event EventHandler<CloseClickedEventArgs> CloseClicked;
        ///OKClicked event invoker
        protected virtual void OnCloseClicked(CloseClickedEventArgs e) => CloseClicked?.Invoke(this, e);


        

        

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            OnCloseClicked(new CloseClickedEventArgs(0.0m));
        }

        /// <summary>
        /// restores files in the filtered folder which no longer have the keywords
        /// to the parent folder, updating the database in the process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            string source = _defaultSubFolderName;
            if (String.IsNullOrWhiteSpace(_parentFolderPath))
            {
                return;
            }
            if (!Directory.Exists(_parentFolderPath))
            {
                return;
            }
            var subDirList = Directory.EnumerateDirectories(_parentFolderPath);
            
            var validSubFolders = subDirList.Where(folder => folder.Contains("Filtered"));
            if (!validSubFolders.Any())
            {
                return;
            }
            if (validSubFolders.Count()>1)
            {
                source=SelectFromList(validSubFolders);
            }
            else
            {
                source = validSubFolders.First();
            }


            if (Directory.Exists(source))
            {
                int numberOfRestoredfiles=RestoreFilteredFiles(source);
                statusText += $"Restored {numberOfRestoredfiles} from {source}\n";
            }
            else
            {
                MessageBox.Show(@"You have not selected a folder to restore from. Pick a folder in the 
Text box, or select a session inn the View by Sessions window before
selecting the filter App", "Invalid Folder selected", MessageBoxButton.OK);
                statusText += $"Restore failed - no source folder selected\n";
            }
            StatusRefresh();
        }

        private void StatusRefresh()
        {
            if (StatusTextBlock != null)
            {

                if (StatusTextBlock.Dispatcher.CheckAccess())
                {
                    StatusTextBlock.InvalidateVisual();
                }
                else
                {
                    StatusTextBlock.Dispatcher.Invoke(DispatcherPriority.Background,
                        new Action(() => { StatusTextBlock.InvalidateVisual(); }));
                }


            }
        }

        /// <summary>
        /// Displays elements from a list to the user and requests that they select one
        /// Returns that one as a string
        /// </summary>
        /// <param name="validSubFolders"></param>
        /// <returns></returns>
        private string SelectFromList(IEnumerable<string> validSubFolders)
        {
            string result = "";
            if (!validSubFolders.IsNullOrEmpty())
            {
                var dialog = new SelectOneDialog();
                dialog.SetItems(validSubFolders.ToList());
                var dialogResult = dialog.ShowDialog();
                if (dialogResult ?? false)
                {
                    result = dialog.GetSelectedItem();
                }

            }
            return result;
        }

        private void AppFilterComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AppFilterComboBox.IsDropDownOpen = true;
        }

        private void AppFilterComboAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(AppFilterComboBox.Text))
            {
                if (!AppFilterComboBox.Items.Contains(AppFilterComboBox.Text))
                {
                    AppFilterComboBox.Items.Add(AppFilterComboBox.Text);
                }
            }
            AppFilterComboBox.IsDropDownOpen = true;
        }

        private void AppFilterComboDel_Click(object sender, RoutedEventArgs e)
        {
            if (AppFilterComboBox.SelectedIndex >= 0)
            {
                AppFilterComboBox.Items.RemoveAt(AppFilterComboBox.SelectedIndex);
            }
            AppFilterComboBox.IsDropDownOpen = true;
        }

        private void AppFilterComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AppFilterComboBox.IsDropDownOpen = true;
        }

        private void AppFilterComboBox_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AppFilterComboBox.IsDropDownOpen = true;
        }

        private void AppFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppFilterComboBox.IsDropDownOpen = true;
        }

        private void AppFilterComboBox_TextInput(object sender, TextCompositionEventArgs e)
        {
            AppFilterComboBox.IsDropDownOpen = true;
        }

        private void AppFilterComboBox_MouseEnter(object sender, MouseEventArgs e)
        {
            AppFilterComboBox.IsDropDownOpen = true;
        }
    }
}
