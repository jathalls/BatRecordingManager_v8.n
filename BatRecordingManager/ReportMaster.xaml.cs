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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.Language.Intellisense;
using Mm.ExportableDataGrid;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ReportMaster.xaml
    ///     This class is the base class for specific report types.
    /// </summary>
    public abstract partial class ReportMaster : UserControl
    {
        /// <summary>
        ///     default constructor
        /// </summary>
        protected ReportMaster()
        {
            InitializeComponent();
            ReportDataGrid.EnableColumnVirtualization = true;
            ReportDataGrid.EnableRowVirtualization = true;
        }

        /// <summary>
        ///     abstract generic list for the specific type of report data
        /// </summary>
        public abstract string tabHeader { get; }

        /// <summary>
        ///     Generic SetData for each tabbed instance of ReportMaster
        /// </summary>
        /// <param name="reportBatStatsList"></param>
        /// <param name="reportSessionList"></param>
        /// <param name="reportRecordingList"></param>
        public abstract void SetData(BulkObservableCollection<BatStatistics> reportBatStatsList,
            BulkObservableCollection<RecordingSession> reportSessionList,
            BulkObservableCollection<Recording> reportRecordingList);

        internal void Export(CsvExporter exporter, string fileName)
        {
            ReportDataGrid.ExportUsingRefection(exporter, fileName);
        }

        /// <summary>
        ///     Generic column creation for versatile DataGrids
        /// </summary>
        /// <param name="header"></param>
        /// <param name="bindingSource"></param>
        /// <param name="visibility"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        protected DataGridTextColumn CreateColumn(string header, string bindingSource, Visibility visibility,
            string converter)
        {
            var column = new DataGridTextColumn {Header = header};
            var bind = new Binding(bindingSource);
            if (!string.IsNullOrWhiteSpace(converter))
            {
                if (converter == "ShortTime_Converter")
                    bind.Converter = new ShortTimeConverter();
                else if (converter == "ShortDate_Converter")
                    bind.Converter = new ShortDateConverter();
            }

            column.Binding = bind;
            column.Visibility = visibility;
            return column;
        }

        /// <summary>
        ///     Sets the header text into the headerTextBox and returns it to be put into the
        ///     DataGrid.  Adds bat passes summary for the session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected string SetHeaderText(RecordingSession session)
        {
            var footnote = @"
* NB Grid references marked with * are session locations, others are for the start of the recording
";
            var result = session.SessionTag + "\n" + session.SessionNotes.Replace(',', ';') + "\n";

            var summary = Tools.GetSessionSummary(session);
            foreach (var item in summary) result += item.Replace(',', ';') + "\n";
            HeaderTextBox.Text += result + footnote + "***************************************\n";

            return result;
        }

        //protected BulkObservableCollection<BatStatistics> ReportBatStatsList { get; set; } = new BulkObservableCollection<BatStatistics>();
        //protected BulkObservableCollection<RecordingSession> ReportSessionList { get; set; } = new BulkObservableCollection<RecordingSession>();
        //protected BulkObservableCollection<Recording> ReportRecordingList { get; set; } = new BulkObservableCollection<Recording>();
        private void ReportDataGrid_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
        }
    }
}