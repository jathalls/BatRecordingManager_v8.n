using Acr.Settings;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// property changed notifier
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #region EnvelopeThreshold



        private decimal _envelopeThreshold;
        /// <summary>
        /// Gets or sets the EnvelopeThreshold property.  This dependency property 
        /// indicates ....
        /// </summary>
        public decimal EnvelopeThreshold
        {

            get
            {
                return _envelopeThreshold;
            }
            set
            {
                try
                {
                    _envelopeThreshold = value;
                    NotifyPropertyChanged(nameof(EnvelopeThreshold));
                }
                catch (Exception) { }
            }

        }

        #endregion

        #region EnvelopeLeadin

        /// <summary>
        /// EnvelopeLeadin Dependency Property
        /// </summary>
        public static readonly DependencyProperty EnvelopeLeadinProperty =
            DependencyProperty.Register("EnvelopeLeadin", typeof(decimal), typeof(Settings),
                new FrameworkPropertyMetadata(0.2m));

        /// <summary>
        /// Gets or sets the EnvelopeLeadin property.  This dependency property 
        /// indicates ....
        /// </summary>
        public decimal EnvelopeLeadin
        {
            get { return (decimal)GetValue(EnvelopeLeadinProperty); }
            set { SetValue(EnvelopeLeadinProperty, value); }
        }

        #endregion

        #region SpectrumThreshold

        /// <summary>
        /// SpectrumThreshold Dependency Property
        /// </summary>
        public static readonly DependencyProperty SpectrumThresholdProperty =
            DependencyProperty.Register("SpectrumThreshold", typeof(decimal), typeof(Settings),
                new FrameworkPropertyMetadata(1.5m));

        /// <summary>
        /// Gets or sets the SpectrumThreshold property.  This dependency property 
        /// indicates ....
        /// </summary>
        public decimal SpectrumThreshold
        {
            get { return (decimal)GetValue(SpectrumThresholdProperty); }
            set { SetValue(SpectrumThresholdProperty, value); }
        }

        #endregion

        #region EnvelopeLeadout

        /// <summary>
        /// EnvelopeLeadout Dependency Property
        /// </summary>
        public static readonly DependencyProperty EnvelopeLeadoutProperty =
            DependencyProperty.Register("EnvelopeLeadout", typeof(decimal), typeof(Settings),
                new FrameworkPropertyMetadata(1.0m));

        /// <summary>
        /// Gets or sets the EnvelopeLeadout property.  This dependency property 
        /// indicates ....
        /// </summary>
        public decimal EnvelopeLeadout
        {
            get { return (decimal)GetValue(EnvelopeLeadoutProperty); }
            set { SetValue(EnvelopeLeadoutProperty, value); }
        }

        #endregion

        #region SpectrumLeadin

        /// <summary>
        /// SpectrumLeadin Dependency Property
        /// </summary>
        public static readonly DependencyProperty SpectrumLeadinProperty =
            DependencyProperty.Register("SpectrumLeadin", typeof(decimal), typeof(Settings),
                new FrameworkPropertyMetadata(4.0m));

        /// <summary>
        /// Gets or sets the SpectrumLeadin property.  This dependency property 
        /// indicates ....
        /// </summary>
        public decimal SpectrumLeadin
        {
            get { return (decimal)GetValue(SpectrumLeadinProperty); }
            set { SetValue(SpectrumLeadinProperty, value); }
        }

        #endregion

        #region SpectrumLeadout

        /// <summary>
        /// SpectrumLeadout Dependency Property
        /// </summary>
        public static readonly DependencyProperty SpectrumLeadoutProperty =
            DependencyProperty.Register("SpectrumLeadout", typeof(decimal), typeof(Settings),
                new FrameworkPropertyMetadata(5.0m));

        /// <summary>
        /// Gets or sets the SpectrumLeadout property.  This dependency property 
        /// indicates ....
        /// </summary>
        public decimal SpectrumLeadout
        {
            get { return (decimal)GetValue(SpectrumLeadoutProperty); }
            set { SetValue(SpectrumLeadoutProperty, value); }
        }

        #endregion

        #region DirectoryPath

        /// <summary>
        /// DirectoryPath Dependency Property
        /// </summary>
        public static readonly DependencyProperty DirectoryPathProperty =
            DependencyProperty.Register("DirectoryPath", typeof(string), typeof(Settings),
                new FrameworkPropertyMetadata(""));

        /// <summary>
        /// Gets or sets the DirectoryPath property.  This dependency property 
        /// indicates ....
        /// </summary>
        public string DirectoryPath
        {
            get { return (string)GetValue(DirectoryPathProperty); }
            set { SetValue(DirectoryPathProperty, value); }
        }

        public static readonly DependencyProperty EnableFilterProperty =
            DependencyProperty.Register("EnableFilter", typeof(bool), typeof(Settings),
                new FrameworkPropertyMetadata(false));
        public bool EnableFilter
        {
            get { return ((bool)GetValue(EnableFilterProperty)); }
            set { SetValue(EnableFilterProperty, value); }
        }

        #endregion



        public int FftSize { get; set; }
        public Settings()
        {
            InitializeComponent();
            this.DataContext = this;
            EnvelopeThreshold = (decimal)CrossSettings.Current.Get<float>("EnvelopeThresholdFactor");
            SpectrumThreshold = (decimal)CrossSettings.Current.Get<float>("SpectrumThresholdFactor");
            EnvelopeLeadin = (decimal)CrossSettings.Current.Get<float>("EnvelopeLeadInMS");
            EnvelopeLeadout = (decimal)CrossSettings.Current.Get<float>("EnvelopeLeadOutMS");
            SpectrumLeadin = CrossSettings.Current.Get<int>("SpectrumLeadInSamples");
            SpectrumLeadout = CrossSettings.Current.Get<int>("SpectrumLeadOutSamples");
            FftSize = CrossSettings.Current.Get<int>("FFTSize");
            DirectoryPath = CrossSettings.Current.Get<string>("InitialDirectory");
            try
            {
                EnableFilter = CrossSettings.Current.Get<bool>("EnableFilter");
            }
            catch (Exception)
            {
                CrossSettings.Current.SetDefault<bool>("EnableFilter", false);
                CrossSettings.Current.Set<bool>("EnableFilter", true);
                EnableFilter = true;
            }

        }

        public static void SetDefaults()
        {
            CrossSettings.Current.SetDefault("EnvelopeThresholdFactor", 1.5f);
            CrossSettings.Current.SetDefault("SpectrumThresholdFactor", 1.5f);
            CrossSettings.Current.SetDefault("EnvelopeLeadInMS", 0.2f);
            CrossSettings.Current.SetDefault("EnvelopeLeadOutMS", 1.0f);
            CrossSettings.Current.SetDefault("SpectrumLeadInSamples", 4);
            CrossSettings.Current.SetDefault("SpectrumLeadOutSamples", 5);
            CrossSettings.Current.SetDefault("FftSize", 1024);
            CrossSettings.Current.SetDefault("InitialDirectory", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            CrossSettings.Current.SetDefault("CurrentVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            CrossSettings.Current.SetDefault("EnableFilter", true);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            EnvelopeThreshold = 1.5m;

            EnvelopeLeadin = 0.2m;
            EnvelopeLeadout = 1.0m;
            SpectrumThreshold = 1.5m;
            SpectrumLeadin = 4.0m;
            SpectrumLeadout = 5.0m;
            DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FftSize = 1024;
            EnableFilter = true;




        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            CrossSettings.Current.Set("EnvelopeThresholdFactor", (float)EnvelopeThreshold);
            CrossSettings.Current.Set("EnvelopeLeadInMS", (float)EnvelopeLeadin);
            CrossSettings.Current.Set("EnvelopeLeadOutMS", (float)EnvelopeLeadout);
            CrossSettings.Current.Set("SpectrumThresholdFactor", (float)SpectrumThreshold);
            CrossSettings.Current.Set("SpectrumLeadInSamples", (int)SpectrumLeadin);
            CrossSettings.Current.Set("SpectrumLeadOutSamples", (int)SpectrumLeadout);
            CrossSettings.Current.Set("FftSize", FftSize);
            CrossSettings.Current.Set<bool>("EnableFilter", EnableFilter);
            if (Directory.Exists(DirectoryPath))
            {
                CrossSettings.Current.Set("InitialDirectory", DirectoryPath);
            }
            this.DialogResult = true;
            this.Close();
        }

        private void FFTSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (FFTSizeComboBox.SelectedIndex)
            {
                case 0:
                    FftSize = 2048; break;
                case 1:
                    FftSize = 1024; break;
                case 2:
                    FftSize = 512; break;
                case 3:
                    FftSize = 256; break;
                case 4:
                    FftSize = 128; break;
                default:
                    FftSize = 1024; break;
            }
        }

        private void defaultDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            string folder = SelectFolder();
            if (!string.IsNullOrWhiteSpace(folder))
            {
                if (Directory.Exists(folder))
                {
                    DirectoryPath = folder;
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
            string FolderPath = "";

            string defaultFolder = CrossSettings.Current.Get<string>("InitialDirectory");
            if (!Directory.Exists(defaultFolder))
            {
                defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }


            //using (System.Windows.Forms.OpenFileDialog dialog = new OpenFileDialog())
            //using(Ookii.Dialogs.Wpf.VistaOpenFileDialog dialog=new VistaOpenFileDialog())
            //{
            using (var dialog = new OpenFileDialog
            {
                DefaultExt = ".wav",
                Filter = "Text files (*.txt)|*.txt|Wav files (*.wav)|*.wav|All Files (*.*)|*.*",
                FilterIndex = 3,
                InitialDirectory = defaultFolder,
                Title = "Select Folder or WAV file",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            })
            {
                //dialog.FileOk += Dialog_FileOk;


                //dialog.Description = "Select the folder containing the .wav and descriptive text files";
                //dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    //HeaderFileName = dialog.FileName;

                    FolderPath = System.IO.Path.GetDirectoryName(dialog.FileName);

                //FolderPath = Path.GetDirectoryName(dialog.FileName);
                else
                    return null;
            }
            return (FolderPath);
        }


    }
}
