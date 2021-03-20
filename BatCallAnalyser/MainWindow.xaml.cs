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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UniversalToolkit;

namespace BatCallAnalyser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ReferenceCall cursors = new ReferenceCall();
            cursors.bandwidth_Max = 30.0;
            cursors.bandwidth_Min = 10.0;
            cursors.interval_Min = 70.0;
            cursors.interval_Max = 120.0;
            cursors.CallType = "fm_qCF";
            cursors.duration_Max = 4.5;
            cursors.duration_Min = 2.5;
            cursors.fEnd_Max = 46.0;
            cursors.fEnd_Min = 43.0;
            cursors.fHeel_Max = 0;
            cursors.fHeel_Min = 0;
            cursors.fKnee_Max = 0;
            cursors.fKnee_Min = 0;
            cursors.fPeak_Max = 48.0;
            cursors.fPeak_Min = 45.0;
            cursors.fStart_Max = 65.0;
            cursors.fStart_Min = 55.0;

            // cursors = chartGrid.callForm.getCallParameters();
            //chartGrid.showCharts(cursors);
        }
    }
}