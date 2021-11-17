using Acr.Settings;
using BatCallAnalysisControlSet;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using Binding = System.Windows.Data.Binding;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Interaction logic for AnalysisMainControl.xaml
    /// </summary>
    public partial class AnalysisMainControl : System.Windows.Controls.UserControl
    {
        //private AnalysisTableData tableData = new AnalysisTableData();

        //private decimal thresholdFactor = 1.5m;
        /// <summary>
        /// Constructor the AnalysisMain user control.
        /// Initializes the PTA_Database, the UI and creates a main window header
        /// Loads defaults from the settings and establishes UI bindings
        /// </summary>
        public AnalysisMainControl()
        {
            PTA_DBAccess.InitialiseDatabase();
            InitializeComponent();

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                AnalysisTable.tableData.Version = "Version:- " + version;
            }
            catch (Exception ex)
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
            }
            catch (Exception ex)
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
            }
            catch (Exception ex)
            {
                ErrorLog($"Unable to set data context:" + ex.Message);
            }

            ZoomFactor = 0.95d;

            var binding = new MultiBinding { Converter = new MultiscaleConverter2() };
            binding.Bindings.Add(new Binding("ActualWidth") { Source = this.ImageContainerPanel });
            binding.Bindings.Add(new Binding("ZoomFactor") { Source = this });
            EnvelopeImage.SetBinding(WidthProperty, binding);
            EnvelopeImage.Height = ImageContainerPanel.Height;

            AnalysisTable.callChanged += AnalysisTable_callChanged;
        }

        public double ZoomFactor { get; set; } = 1.5d;

        public static void ErrorLog(string message)
        {
            if (!Directory.Exists(@"C:\AMCErrors\"))
            {
                Directory.CreateDirectory(@"C:\AMCErrors\");
            }
            File.AppendAllText(@"C:\AMCErrors\Errors.log", DateTime.Now.ToString() + message + "\n");
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

        public void CommandLineArgs(string[] args)
        {
            if (args.Length > 1)
            {
                string entry = args[1];
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    if (File.Exists(entry) || Directory.Exists(entry))
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

        private void AnalysisTable_callChanged(object sender, EventArgs e)
        {
            CallAnalysisChart.showCharts((e as callEventArgs).call);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var window = FindParent<Window>(this);
            window?.Close();
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            string selectedFQ_FileName;
            string initialDirectory = CrossSettings.Current.Get<string>("InitialDirectory");
            if (!Directory.Exists(initialDirectory ?? ""))
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
                if (selectedFQ_FileName.EndsWith("Select Folder") || selectedFQ_FileName.Trim().EndsWith(@"\"))
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

        private void SetDefaultSettings()
        {
            Settings.SetDefaults();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();

            var result = settings.ShowDialog();
            if (result != null && result.Value)
            {
                AnalysisTable.tableData.thresholdFactor = (decimal)CrossSettings.Current.Get<float>("EnvelopeThresholdFactor");
                AnalysisTable.tableData.spectrumFactor = (decimal)CrossSettings.Current.Get<float>("SpectrumThresholdFactor");
                AnalysisTable.tableData.EnableFilter = CrossSettings.Current.Get<bool>("EnableFilter");
            }
            settings.Close();
        }
    }

    #region multiscaleConverter (ValueConverter)

    /// <summary>
    ///     Converter class for scaling height or width of an image
    /// </summary>
    public class MultiscaleConverter2 : IMultiValueConverter

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
                    var factor = 2.0d;

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

                    if (values[1] is double) factor = ((double?)values[1]).Value;

                    return height * factor;
                }

                return 100.0d;
            }
            catch
            {
                return 100.0d;
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

    #endregion multiscaleConverter (ValueConverter)
}