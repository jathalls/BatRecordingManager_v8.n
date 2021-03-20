using Acr.Settings;
using BatCallAnalysisControlSet;
using Mm.ExportableDataGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UniversalToolkit;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Interaction logic for AnalysisTableA.xaml
    /// </summary>
    public partial class AnalysisTableA : UserControl
    {
        //public bpaRecording recording { get; set; } = new bpaRecording(null);

        public AnalysisTableA()
        {
            try
            {
                InitializeComponent();
                DataContext = tableData;
                headerImage.Source = tableData.FrequencyHeader;
                //Debug.WriteLine($"Set header source {tableData.FrequencyHeader.Width}x{tableData.FrequencyHeader.Height}");
            }
            catch (Exception ex)
            {
                AnalysisMainControl.ErrorLog($"failed initializing AnalysisTableA {ex.Message}");
            }
        }

        public event EventHandler callChanged;

        /// <summary>
        /// Repository of all the displayable data for the analysis
        /// </summary>
        public AnalysisTableData tableData { get; set; } = new AnalysisTableData();

        //public AnalysisTableData tableData = new AnalysisTableData();
        public void setTableData(AnalysisTableData data)
        {
            try
            {
                tableData = data;
                this.DataContext = tableData;
            }
            catch (Exception ex)
            {
                AnalysisMainControl.ErrorLog($"Error copying data to tableData:{ex.Message}");
            }
        }

        internal void ClearTabledata()
        {
            tableData.Clear();
        }

        internal async void ProcessFile(string selectedFQ_FileName)
        {
            bool result = false;
            using (new WaitCursor())
            {
                //var x=CrossSettings.Current.Get<int>("EnvelopeLeadIn");
                //CrossSettings.Current.Set("EnvelopeLeadIn", 20);
                //x=CrossSettings.Current.Get<int>("EnvelopeLeadIn");
                RecalcButton.IsEnabled = false;
                string textFQ_FileName = System.IO.Path.ChangeExtension(selectedFQ_FileName, ".txt");

                if (File.Exists(textFQ_FileName))
                {
                    result = await ProcessSingleFileAsync(selectedFQ_FileName);
                }
                else
                {
                    // There is no .txt file so we assume that we are dealing with a folder full
                    // of short files which have been analysed using Kaleidoscope
                    // and we analyse all of the files in the folder.
                    // This may take a long time if there are hundreds of files

                    //AnalysisTable.setTableData(AnalysisTable.tableData);

                    result = await ProcessFileGroupAsync(selectedFQ_FileName);
                }
                var previous = passDataGrid.Columns[11].ActualWidth;
                passDataGrid.Columns[11].Width = 20;
                tableData.RefreshFrequencyheader();
                headerImage.Source = tableData.FrequencyHeader;
                passDataGrid.UpdateLayout();
                passDataGrid.Columns[11].Width = previous;
                passDataGrid.UpdateLayout();
            }
        }

        internal void ReProcessFile(string file)
        {
            RecalcButton.IsEnabled = false;
            tableData.ReProcessFile(file, EnvelopeThresholdUpDown.Value, SpectrumThresholdUpDown.Value);
            RecalcButton.IsEnabled = true;
        }

        internal void ReProcessFile()
        {
            RecalcButton.IsEnabled = false;
            if (tableData != null)
            {
                if (tableData.combinedRecordingList != null && tableData.combinedRecordingList.Count > 0)
                {
                    foreach (var recording in tableData.combinedRecordingList)
                    {
                        tableData.ReProcessFile(recording.FQfilename, EnvelopeThresholdUpDown.Value, SpectrumThresholdUpDown.Value);
                    }
                }
            }

            RecalcButton.IsEnabled = true;
        }

        /// <summary>
        /// if the selection of the segment changes then update the itemssources for the details grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void segmentDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tableData.segmentDataGrid_SelectionChanged(null);
        }

        protected virtual void OnCallChanged(callEventArgs e) => callChanged?.Invoke(this, e);

        private bool skipDatabaseCheck = true;

        private void AutoClassifyButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                Classifier classify = new Classifier();
                if (passDataGrid.SelectedItems != null && passDataGrid.SelectedItems.Count > 0)
                {
                    foreach (var item in passDataGrid.SelectedItems)
                    {
                        bpaPass pass = item as bpaPass;
                        string comment = classify.Classify(pass);

                        var root = (from rec in tableData.combinedRecordingList
                                    where rec.recNumber == pass.recordingNumber

                                    select rec).FirstOrDefault();
                        if (root != null && root.recNumber > 0)
                        {
                            root.appendCommentForPass(pass, "AC=" + comment);
                        }
                        pass.Comment += "AC=" + comment;
                    }
                    var selected = passDataGrid.SelectedItems;
                    tableData.segmentDataGrid_SelectionChanged(null);

                    tableData.passDataGrid_SelectionChanged(passDataGrid.SelectedItems);
                }
            }
        }

        /// <summary>
        /// Collects call sequence data, saves it to settings and opens the chartGrid tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmiCallAnalysis_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                List<bpaPass> SelectedPasses = new List<bpaPass>();
                if (passDataGrid.SelectedItems != null && passDataGrid.SelectedItems.Count > 0)
                {
                    ReferenceCall call = new ReferenceCall();
                    SelectedPasses.Add(passDataGrid.SelectedItems[0] as bpaPass);
                    var start = SelectedPasses[0].startDetails;
                    call.fStart_Max = start.Mean + start.SD;

                    call.fStart_Min = start.Mean - start.SD;

                    var end = SelectedPasses[0].endDetails;
                    call.fEnd_Max = end.Mean + end.SD;
                    call.fEnd_Min = end.Mean - end.SD;

                    var peak = SelectedPasses[0].peakDetails;
                    call.fPeak_Max = peak.Mean + peak.SD;
                    call.fPeak_Min = peak.Mean - peak.SD;

                    var dur = SelectedPasses[0].durationDetails;
                    call.duration_Max = dur.Mean + dur.SD;
                    call.duration_Min = dur.Mean - dur.SD;

                    var interval = SelectedPasses[0].intervalDetails;
                    call.interval_Max = interval.Mean + interval.SD;
                    call.interval_Min = interval.Mean - interval.SD;

                    OnCallChanged(new callEventArgs(call));
                }
            }
        }

        private void cmiEnvelope_Click(object sender, RoutedEventArgs e)
        {
            if (passDataGrid.SelectedItem != null)
            {
                using (new WaitCursor())
                {
                    bpaPass selectedPass = passDataGrid.SelectedItem as bpaPass;
                    tableData.DisplayEnvelope(selectedPass);
                    tableData.EnvelopeEnabled = true;
                }
            }
        }

        private void cmiPulseEnvelope_Click(object sender, RoutedEventArgs e)
        {
            if (spectralPeakDataGrid.SelectedItem != null)
            {
                using (new WaitCursor())
                {
                    SpectralPeak selectedSpectrum = spectralPeakDataGrid.SelectedItem as SpectralPeak;
                    tableData.DisplayPulseEnvelope(selectedSpectrum);
                    tableData.PulseEnvelopeEnabled = true;
                }
            }
        }

        private void EnvelopeThresholdUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (tableData != null)
            {
                Debug.WriteLine($"NUD={EnvelopeThresholdUpDown.Value} SLID={EnvelopeThresholdSlider.Value} var={tableData?.thresholdFactor}");
            }
            else
            {
                Debug.WriteLine("No Table Data Yet");
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                string filename = @"C:\ExportedBatData\Spectra.csv";
                if (File.Exists(filename))
                {
                    string bakfile = Path.ChangeExtension(filename, ".bak");
                    if (File.Exists(bakfile)) File.Delete(bakfile);
                    File.Copy(filename, bakfile);
                    File.Delete(filename);
                }
                //spectralPeakDataGrid.Visibility = Visibility.Hidden;
                var exporter = new CsvExporter(',');
                exporter.ExportCompleted += Exporter_ExportCompleted;

                //passDataGrid.ExportUsingRefection(exporter,@"C:\ExportedBatData\Passes.csv");
                spectralPeakDataGrid.Visibility = Visibility.Visible;
                passDataGrid.Visibility = Visibility.Hidden;
                spectralPeakDataGrid.ExportUsingRefection(exporter, filename);
                passDataGrid.Visibility = Visibility.Visible;
            }
        }

        private void Exporter_ExportCompleted(object sender, EventArgs e)
        {
            string filename = @"C:\ExportedBatData\Passes.csv";
            if (File.Exists(filename))
            {
                string bakfile = Path.ChangeExtension(filename, ".bak");
                if (File.Exists(bakfile)) File.Delete(bakfile);
                File.Copy(filename, bakfile);
                File.Delete(filename);
            }
            var exporter = new CsvExporter(',');

            //passDataGrid.ExportUsingRefection(exporter,@"C:\ExportedBatData\Passes.csv");
            this.Dispatcher.Invoke(() =>
            {
                spectralPeakDataGrid.Visibility = Visibility.Hidden;

                passDataGrid.Visibility = Visibility.Visible;

                passDataGrid.ExportUsingRefection(exporter, filename);
                spectralPeakDataGrid.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Akin to Recording.GenerateLabelFile but applies to multiple recordings and only the
        /// passes of those recordings which are in the provided list of passes
        /// </summary>
        /// <param name="selectedPasses"></param>
        private void GenerateLabelFiles(List<bpaPass> selectedPasses)
        {
            var recordingsForThesePasses = (from pass in selectedPasses
                                            from rec in tableData.combinedRecordingList
                                            where rec.recNumber == pass.recordingNumber
                                            select rec).Distinct();

            foreach (var rec in recordingsForThesePasses)
            {
                var passesForThisrecording = (from pass in tableData.combinedPassList
                                              where pass.recordingNumber == rec.recNumber
                                              select pass);
                rec.setPassList(passesForThisrecording.ToList());
                rec.GenerateLabelFile();
            }
        }

        private void passDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor())
            {
                if (passDataGrid.SelectedItems == null || passDataGrid.SelectedItems.Count <= 0)
                {
                    tableData.passDataGrid_SelectionChanged(passDataGrid.Items);
                    AutoClassifyButton.IsEnabled = false;
                }
                else
                {
                    tableData.passDataGrid_SelectionChanged(passDataGrid.SelectedItems);
                    AutoClassifyButton.IsEnabled = true;
                }
            }
        }

        private async Task<bool> ProcessFileGroupAsync(string selectedFQ_FileName)
        {
            string folderPath = System.IO.Path.GetDirectoryName(selectedFQ_FileName);
            if (Directory.Exists(folderPath))
            {
                try
                {
                    ProcessWavFileFolder(folderPath);
                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"Process multiple files error:- {ex.Message}");
                    return (false);
                }
            }
            return (true);
        }

        private async Task<bool> ProcessSingleFileAsync(string selectedFQ_FileName)
        {
            // The selected .wav file has a corresponding .txt file
            // Therefore we assume it is an Audacity analysed long file
            // with multiple segments and analyse just that file

            if (File.Exists(selectedFQ_FileName))
            {
                try
                {
                    ProcessSingleWavFile(selectedFQ_FileName);
                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"ProcessFile error single file:" + ex.Message);
                    return (false);
                }
            }
            return (true);
        }

        private bpaRecording ProcessSingleWavFile(string file)
        {
            bpaRecording recording = null;
            long size = new FileInfo(file).Length;
            if (size > 1000000000L)
            {
                MessageBox.Show("File too large to process");
                return null;
            }

            if (!skipDatabaseCheck && PTA_DBAccess.RecordingExists(file))
            {
                recording = PTA_DBAccess.getBPARecordingAndDescendants(file);
                tableData.thresholdFactor = recording.getThresholdFactor();
                tableData.spectrumFactor = recording.getSpectrumThresholdFactor();
            }
            else
            {
                recording = new bpaRecording(recNumber: 1, file);

                try
                {
                    recording.CreateSegments(tableData.thresholdFactor, tableData.spectrumFactor);
                }
                catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"Error creating segments:{ex.Message}");
                }
                if (!skipDatabaseCheck)
                {
                    PTA_DBAccess.SaveRecording(recording);
                }
            }
            tableData.SetRecording(recording);

            RecalcButton.IsEnabled = true;
            return (recording);
        }

        private void ProcessWavFileFolder(string folderPath)
        {
            var allFQWavFiles = Directory.EnumerateFiles(folderPath, "*.wav");
            if (allFQWavFiles != null && allFQWavFiles.Count() > 0)
            {
                List<bpaRecording> allRecordings = new List<bpaRecording>();
                int recNumber = 1;
                long totSize = 0l;
                bpaRecording recording = null;
                foreach (var FQwavFile in allFQWavFiles)
                {
                    var textFileName = System.IO.Path.ChangeExtension(FQwavFile, ".txt");
                    if (File.Exists(textFileName))
                    {
                        recording = ProcessSingleWavFile(FQwavFile);
                    }
                    else
                    {
                        if (!skipDatabaseCheck && PTA_DBAccess.RecordingExists(FQwavFile))
                        {
                            recording = PTA_DBAccess.getBPARecordingAndDescendants(FQwavFile);

                            tableData.thresholdFactor = recording.getThresholdFactor();

                            tableData.spectrumFactor = recording.getSpectrumThresholdFactor();
                        }
                        else
                        {
                            var size = new FileInfo(FQwavFile).Length;
                            totSize += size;
                            if (totSize > 4000000000L)
                            {
                                MessageBox.Show("Total size of files too large, process truncated!");
                                break;
                            }
                            recording = new bpaRecording(recNumber++, FQwavFile);
                            recording.CreateSegments(tableData.thresholdFactor, tableData.spectrumFactor);
                            recording.GenerateLabelFile();
                            PTA_DBAccess.SaveRecording(recording);
                        }
                    }
                    if (recording != null)
                    {
                        allRecordings.Add(recording);
                    }
                }
                tableData.SetRecordings(allRecordings);
                RecalcButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// if the selection in the pass datagrid changes then update the items in the pulse datagrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pulseDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pulseDataGrid.SelectedItems == null || pulseDataGrid.SelectedItems.Count == 0)
            {
                tableData.pulseDataGrid_SelectionChange(pulseDataGrid.Items);
            }
            else
            {
                tableData.pulseDataGrid_SelectionChange(pulseDataGrid.SelectedItems);
            }
        }

        private void RecalcButton_Click(object sender, RoutedEventArgs e)
        {
            skipDatabaseCheck = true;
            using (new WaitCursor())
            {
                if (passDataGrid.SelectedItems != null && passDataGrid.SelectedItems.Count > 0)
                {
                    List<bpaPass> selectedPasses = new List<bpaPass>();
                    foreach (var item in passDataGrid.SelectedItems)
                    {
                        selectedPasses.Add(item as bpaPass);
                    }

                    foreach (var pass in selectedPasses)
                    {
                        var selection = passDataGrid.SelectedItems;
                        tableData.UpdatePass(pass);
                        tableData.passDataGrid_SelectionChanged(selection);

                        //pass.CreatePass(tableData.thresholdFactor, tableData.spectrumFactor);
                    }

                    /* var selecteddRecordings = (from pass in selectedPasses
                                                from rec in tableData.combinedRecordingList
                                                where pass.recordingNumber == rec.recNumber
                                                select rec.FQfilename).Distinct<string>().ToList<string>();

                     foreach (var file in selecteddRecordings)
                     {
                         ReProcessFile(file);
                     }*/
                }
                else
                {
                    ReProcessFile();
                }
            }
            //skipDatabaseCheck = false;
            tableData.passDataGrid_SelectionChanged(passDataGrid.SelectedItems);
            if (tableData.envelopeImage != null) tableData.EnvelopeEnabled = true;
        }

        private void recordingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tableData.recordingsDataGrid_SelectionChanged(null);
        }

        private void RemoveOutliersButton_Click(object sender, RoutedEventArgs e)
        {
            int oldSelection = passDataGrid.SelectedIndex;
            using (new WaitCursor())
            {
                bpaPass passToFilter = null;
                if (passDataGrid.SelectedItems != null && passDataGrid.SelectedItems.Count > 0)
                {
                    foreach (var item in passDataGrid.SelectedItems)
                    {
                        var pass = item as bpaPass;
                        pass.RemoveOutliers();
                    }
                }
                else
                {
                    if (passDataGrid.SelectedItem != null)
                    {
                        passToFilter = passDataGrid.SelectedItem as bpaPass;
                    }
                }
                if (passToFilter != null)
                {
                    passToFilter.RemoveOutliers();
                }
                tableData.recordingsDataGrid_SelectionChanged(tableData.combinedRecordingList);
                tableData.segmentDataGrid_SelectionChanged(tableData.combinedSegmentList);
            }
            passDataGrid.SelectedIndex = oldSelection;
        }

        private void ReWriteLabelsButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                if (passDataGrid.SelectedItems != null && passDataGrid.SelectedItems.Count > 0)
                {
                    List<bpaPass> selectedPasses = new List<bpaPass>();
                    foreach (var item in passDataGrid.SelectedItems)
                    {
                        selectedPasses.Add(item as bpaPass);
                    }
                    GenerateLabelFiles(selectedPasses);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                CrossSettings.Current.Set("EnvelopeThresholdFactor", tableData.thresholdFactor);
                CrossSettings.Current.Set("SpectrumThresholdFactor", tableData.spectrumFactor);
            }
        }

        private void spectralPeakDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor())
            {
                if (spectralPeakDataGrid.SelectedItems == null || spectralPeakDataGrid.SelectedItems.Count == 0)
                {
                    tableData.spectralPeakDataGrid_SelectionChanged(spectralPeakDataGrid.Items);
                }
                else
                {
                    tableData.spectralPeakDataGrid_SelectionChanged(spectralPeakDataGrid.SelectedItems);
                }
            }
        }

        /// <summary>
        /// Tidies up the data displayed.
        /// 1) performs Remove Outliers where the peakFreq SD>10
        /// 2) deletes any pulse with peak, start or end freq outside pass mean+/-2SD
        /// 3) hides all passes with zero pulses
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TidyButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                List<bpaPass> SelectedPasses = new List<bpaPass>();
                if (passDataGrid.SelectedItems != null && passDataGrid.SelectedItems.Count > 0)
                {
                    foreach (var item in passDataGrid.SelectedItems) SelectedPasses.Add(item as bpaPass);
                }

                tableData.AutoDeleteExtremePulses();
                tableData.passDataGrid_SelectionChanged(null);

                tableData.AutoRemoveOutliers();
                tableData.passDataGrid_SelectionChanged(null);

                tableData.AutoDeleteBlankPasses();
                tableData.passDataGrid_SelectionChanged(null);

                tableData.recordingsDataGrid_SelectionChanged(tableData.combinedRecordingList);
            }
        }
    }
}