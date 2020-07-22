using Acr.Settings;
using System.Windows.Forms;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Interaction logic for AnalysisMainControl.xaml
    /// </summary>
    public partial class AnalysisMainControl : System.Windows.Controls.UserControl
    {
        //private AnalysisTableData tableData = new AnalysisTableData();

        //private decimal thresholdFactor = 1.5m;

        

        public AnalysisMainControl()
        {
            PTA_DBAccess.InitialiseDatabase();
            InitializeComponent();
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                AnalysisTable.tableData.Version = "Version:- " + version;
            }catch(Exception ex)
            {
                ErrorLog($"Unable to get Version:{ex.Message}");
            }

            try
            {
                string CurrentVersion = CrossSettings.Current.Get<string>("CurrentVersion");
                if (string.IsNullOrWhiteSpace(CurrentVersion))
                {
                    SetDefaultSettings();
                }
            }catch(Exception ex)
            {
                ErrorLog($"Unabble to set default settings:" + ex.Message);
            }


            try
            {
                AnalysisTable.tableData.thresholdFactor = CrossSettings.Current.Get<decimal>("EnvelopeThresholdFactor");
                AnalysisTable.tableData.spectrumFactor = CrossSettings.Current.Get<decimal>("SpectrumThresholdFactor");
                AnalysisTable.tableData.EnableFilter = CrossSettings.Current.Get<bool>("EnableFilter");


                //tableData.bmpiCreated += TableData_bmpiCreated;
                this.DataContext = AnalysisTable.tableData;
            }catch(Exception ex)
            {
                ErrorLog($"Unable to set data context:" + ex.Message);
            }
        }

        public static void ErrorLog(string message)
        {
            if (!Directory.Exists(@"C:\AMCErrors\"))
            {
                Directory.CreateDirectory(@"C:\AMCErrors\");
            }
            File.AppendAllText( @"C:\AMCErrors\Errors.log", DateTime.Now.ToString() + message + "\n");
        }

        public void CommandLineArgs(string[] args)
        {
            if (args.Length > 1)
            {
                string entry = args[1];
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    if(File.Exists(entry) || Directory.Exists(entry))
                    {
                        using (new WaitCursor())
                        {
                            AnalysisTable.ClearTabledata();
                            miSaveToDB.IsEnabled = false;
                            try
                            {
                                AnalysisTable.ProcessFile(entry);
                                miSaveToDB.IsEnabled = true;
                            }
                            catch (Exception ex)
                            {
                                ErrorLog("Process File Failed:- " + ex.Message);
                            }
                        }
                        
                    }
                }
            }
        }

        private void SetDefaultSettings()
        {

            Settings.SetDefaults();
            
        }

        

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {

            string selectedFQ_FileName;
            string initialDirectory = CrossSettings.Current.Get<string>("InitialDirectory");
            if (!Directory.Exists(initialDirectory??""))
            {
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.DefaultExt = ".wav";
                dialog.Filter = "Text files (*.txt)|*.txt|Wav files (*.wav)|*.wav|All Files (*.*)|*.*";
                dialog.FilterIndex = 3;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Title = "Select Folder or WAV file";
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Select Folder";

                selectedFQ_FileName = "A";

                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == DialogResult.OK)
                    //HeaderFileName = dialog.FileName;
                    //folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                    selectedFQ_FileName = dialog.FileName;
                if (selectedFQ_FileName.EndsWith("Select Folder")||selectedFQ_FileName.Trim().EndsWith(@"\"))
                {
                    string path = System.IO.Path.GetDirectoryName(selectedFQ_FileName);
                    if (Directory.Exists(path))
                    {
                        //CreateLabelFilesForFolder(path);
                        


                    }
                    else
                    {
                        selectedFQ_FileName = "";
                        
                    }
                }
                
            }
            using (new WaitCursor())
            {
                try
                {
                    AnalysisTable.ClearTabledata();
                    miSaveToDB.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"Error clearing Tabledata in FileOpen:{ex.Message}");
                }

                try
                {
                    using (new WaitCursor())
                    {
                        AnalysisTable.ProcessFile(selectedFQ_FileName);
                        miSaveToDB.IsEnabled = true;
                    }

                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"FileOpenError in processFile:{ex.Message}");
                    miSaveToDB.IsEnabled = false;
                }
            }




        }

        

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();

            var result = settings.ShowDialog();
            if(result!=null && result.Value )
            {
                AnalysisTable.tableData.thresholdFactor = (decimal)CrossSettings.Current.Get<float>("EnvelopeThresholdFactor");
                AnalysisTable.tableData.spectrumFactor = (decimal)CrossSettings.Current.Get<float>("SpectrumThresholdFactor");
                AnalysisTable.tableData.EnableFilter = (bool)CrossSettings.Current.Get<bool>("EnableFilter");
            }
            settings.Close();



        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var window = FindParent<Window>(this);
            window?.Close();
        }

        /// <summary>
        /// <see langword="static"/>function to find a parent in th tree of a specified kind
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="child"></param>
        /// <returns></returns>
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

        private void HelpHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpfile = @"Bat Pulse Analyser.chm";
            if (File.Exists(helpfile)) Help.ShowHelp(null, helpfile);
        }

        /// <summary>
        /// Saves the displayed data set to the local database, updating or creating records
        /// as necessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miSaveToDB_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                AnalysisTable.tableData.SaveToDatabase();
            }
        }
    }
}
