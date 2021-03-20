using BatCallAnalysisControlSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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