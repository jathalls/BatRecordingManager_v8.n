using BatCallAnalysisControlSet;
using System.Collections.Generic;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for CallAnalysisWindow.xaml
    /// </summary>
    public partial class CallAnalysisWindow : Window
    {
        public CallAnalysisWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// set discrete call data for display in a data agrid
        /// </summary>
        /// <param name="displayableData"></param>
        public void SetDisplayableCallData(List<CallData> displayableData)
        {
            AnalysisChartGrid.SetDisplayableCallData(displayableData);
        }
    }
}