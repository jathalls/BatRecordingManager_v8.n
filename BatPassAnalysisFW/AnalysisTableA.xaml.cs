﻿using Acr.Settings;
using DspSharp.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Mm.ExportableDataGrid;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using System.Threading.Tasks;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Interaction logic for AnalysisTableA.xaml
    /// </summary>
    public partial class AnalysisTableA : UserControl
    {
        //public bpaRecording recording { get; set; } = new bpaRecording(null);


            /// <summary>
            /// Repository of all the displayable data for the analysis
            /// </summary>
        public AnalysisTableData tableData { get; set; } = new AnalysisTableData();

        
        //public AnalysisTableData tableData = new AnalysisTableData();

        public AnalysisTableA()
        {
            
            try
            {
                InitializeComponent();
                DataContext = tableData;
                headerImage.Source = tableData.FrequencyHeader;
                //Debug.WriteLine($"Set header source {tableData.FrequencyHeader.Width}x{tableData.FrequencyHeader.Height}");
            }catch(Exception ex)
            {
                AnalysisMainControl.ErrorLog($"failed initializing AnalysisTableA {ex.Message}");
            }

        }

        public void setTableData(AnalysisTableData data)
        {
            try
            {
                tableData = data;
                this.DataContext = tableData;
            }catch(Exception ex)
            {
                AnalysisMainControl.ErrorLog($"Error copying data to tableData:{ex.Message}");
            }
            
        }

        public void viewRecordings(bool selectRecordings)
        {
            if (selectRecordings)
            {
                //segmentDataGrid.Visibility = Visibility.Hidden;
                //recordingsDataGrid.Visibility = Visibility.Visible;
                recNumberColumn.Header = "Rec";
                PassRecColumn.Header = "Rec";
            }
            else
            {
                //recordingsDataGrid.Visibility = Visibility.Hidden;
                //segmentDataGrid.Visibility = Visibility.Visible;
                recNumberColumn.Header = "Seg";
                PassRecColumn.Header = "Seg";
            }
        }

        

        

        /// <summary>
        /// if the selection of the segment changes then update the itemssources for the details grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void segmentDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            if (segmentDataGrid?.SelectedItems == null || segmentDataGrid?.SelectedItems.Count == 0)
            {
                tableData.segmentDataGrid_SelectionChanged(segmentDataGrid?.Items);
            }
            else
            {
                tableData.segmentDataGrid_SelectionChanged(segmentDataGrid?.SelectedItems);
            }*/
            tableData.segmentDataGrid_SelectionChanged(null);
            
        }

        /// <summary>
        /// if the selection in the pass datagrid changes then update the items in the pulse datagrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pulseDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(pulseDataGrid.SelectedItems==null || pulseDataGrid.SelectedItems.Count == 0)
            {
                tableData.pulseDataGrid_SelectionChange(pulseDataGrid.Items);
            }
            else
            {
                tableData.pulseDataGrid_SelectionChange(pulseDataGrid.SelectedItems);
            }

            
            
        }

        internal void ReProcessFile()
        {
            RecalcButton.IsEnabled = false;
            if (tableData != null)
            {
                if(tableData.combinedRecordingList!=null && tableData.combinedRecordingList.Count > 0)
                {
                    ProcessFile(tableData.combinedRecordingList[0].FQfilename);
                    
                    
                }
            }
            
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


                    result=await ProcessSingleFileAsync(selectedFQ_FileName);
                }
                else
                {

                    // There is no .txt file so we assume that we are dealing with a folder full
                    // of short files which have been analysed using Kaleidoscope
                    // and we analyse all of the files in the folder.
                    // This may take a long time if there are hundreds of files

                    //AnalysisTable.setTableData(AnalysisTable.tableData);

                    result=await ProcessFileGroupAsync(selectedFQ_FileName);

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

        private async Task<bool> ProcessFileGroupAsync(string selectedFQ_FileName)
        {
            string folderPath = System.IO.Path.GetDirectoryName(selectedFQ_FileName);
            if (Directory.Exists(folderPath))
            {
                try
                {
                    ProcessWavFileFolder(folderPath);
                }catch(Exception ex)
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

    private void ProcessWavFileFolder(string folderPath)
    {
        
        

        var allFQWavFiles = Directory.EnumerateFiles(folderPath, "*.wav");
            if (allFQWavFiles != null && allFQWavFiles.Count() > 0)
            {
                List<bpaRecording> allRecordings = new List<bpaRecording>();
                int recNumber = 1;
                long totSize = 0l;
                bpaRecording recording;
                foreach (var FQwavFile in allFQWavFiles)
                {
                    if (PTA_DBAccess.RecordingExists(FQwavFile))
                    {
                        recording = PTA_DBAccess.getBPARecordingAndDescendants(FQwavFile);
                    }
                    else
                    {
                        var size = new FileInfo(FQwavFile).Length;
                        totSize += size;
                        if (totSize > 1000000000L)
                        {
                            MessageBox.Show("Total size of files too large, process truncated!");
                            break;
                        }
                        recording = new bpaRecording(recNumber++, FQwavFile);
                        recording.CreateSegments(tableData.thresholdFactor, tableData.spectrumFactor);
                        PTA_DBAccess.SaveRecording(recording);
                    }
                    allRecordings.Add(recording);
                }
                tableData.SetRecordings(allRecordings);
                RecalcButton.IsEnabled = true;
                viewRecordings(true);
            }
        }

        private void ProcessSingleWavFile(string file)
        {
            bpaRecording recording = null;
            long size = new FileInfo(file).Length;
            if (size > 1000000000L)
            {
                MessageBox.Show("File too large to process");
                return;
            }

            if (PTA_DBAccess.RecordingExists(file))
            {
                recording = PTA_DBAccess.getBPARecordingAndDescendants(file);
            }
            else {
                recording = new bpaRecording(recNumber: 1, file);

                try
                {
                    recording.CreateSegments(tableData.thresholdFactor, tableData.spectrumFactor);
                } catch (Exception ex)
                {
                    AnalysisMainControl.ErrorLog($"Error creating segments:{ex.Message}");
                }
                PTA_DBAccess.SaveRecording(recording);
            }
            tableData.SetRecording(recording);
            
            viewRecordings(false);
            RecalcButton.IsEnabled = true;

        }

        internal void ClearTabledata()
        {
            tableData.Clear();
        }

        private void passDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new WaitCursor())
            {
                if (passDataGrid.SelectedItems == null || passDataGrid.SelectedItems.Count <= 0)
                {
                    tableData.passDataGrid_SelectionChanged(passDataGrid.Items);
                }
                else
                {
                    tableData.passDataGrid_SelectionChanged(passDataGrid.SelectedItems);
                }
            }
            
            
        }

       

        

        private void recordingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            if(recordingsDataGrid.SelectedItems==null || recordingsDataGrid.SelectedItems.Count == 0)
            {
                tableData.recordingsDataGrid_SelectionChanged(recordingsDataGrid.Items);
            }
            else
            {
                tableData.recordingsDataGrid_SelectionChanged(recordingsDataGrid.SelectedItems);
            }*/
            tableData.recordingsDataGrid_SelectionChanged(null);
                
            
            
            
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

        private void EnvelopeThresholdUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
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

        private void RecalcButton_Click(object sender, RoutedEventArgs e)
        {
            ReProcessFile();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            CrossSettings.Current.Set("EnvelopeThresholdFactor", tableData.thresholdFactor);
            CrossSettings.Current.Set("SpectrumThresholdFactor", tableData.spectrumFactor);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            string filename = @"C:\ExportedBatData\Spectra.csv";
            if (File.Exists(filename))
            {
                string bakfile=Path.ChangeExtension(filename, ".bak");
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
            spectralPeakDataGrid.ExportUsingRefection(exporter,filename );
            passDataGrid.Visibility = Visibility.Visible;
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
            this.Dispatcher.Invoke( ( ) => { 
            spectralPeakDataGrid.Visibility = Visibility.Hidden;
            

            
                passDataGrid.Visibility = Visibility.Visible;
           
            passDataGrid.ExportUsingRefection(exporter, filename);
            spectralPeakDataGrid.Visibility = Visibility.Visible;
            });
        }

        private void RemoveOutliersButton_Click(object sender, RoutedEventArgs e)
        {
            bpaPass passToFilter = null;
            if(passDataGrid.SelectedItems!=null && passDataGrid.SelectedItems.Count > 0)
            {
                passToFilter = passDataGrid.SelectedItems[0] as bpaPass;
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
                using (new WaitCursor())
                {
                    passToFilter.RemoveOutliers();
                    tableData.segmentDataGrid_SelectionChanged(tableData.combinedSegmentList);
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
    }

    


}
