using Acr.Settings;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                        AnalysisTable.ClearTabledata();
                        AnalysisTable.ProcessFile(entry);
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

            string selectedFileName;
            string initialDirectory = CrossSettings.Current.Get<string>("InitialDirectory");
            if (!Directory.Exists(initialDirectory??""))
            {
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.DefaultExt = "*.*";
                dialog.Filter = "Audio Files (*.wav)|*.wav|All Files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.InitialDirectory = initialDirectory;
                dialog.Title = "Select Recording .WAV file";
                dialog.ValidateNames = true;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.FileName = "*.wav";

                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    //HeaderFileName = dialog.FileName;
                    //folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                    selectedFileName = dialog.FileName;
                else
                    return;
            }
            try
            {
                AnalysisTable.ClearTabledata();
            }catch(Exception ex)
            {
                AnalysisMainControl.ErrorLog($"Error clearing Tabledata in FileOpen:{ex.Message}");
            }

            try
            {
                AnalysisTable.ProcessFile(selectedFileName);

            }catch(Exception ex)
            {
                AnalysisMainControl.ErrorLog($"FileOpenError in processFile:{ex.Message}");
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
    }
}
