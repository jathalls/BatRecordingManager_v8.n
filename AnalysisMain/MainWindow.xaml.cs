using BatPassAnalysisFW;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace AnalysisMain
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public BitmapImage defaultBmpi { get; set; }

        public decimal thresholdFactor { get; set; } = 1.5m;

        //AnalysisTableData tableData { get; set; } = new AnalysisTableData();

        public MainWindow()
        {
            
            InitializeComponent();
            this.DataContext = this;
            string[] args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1)
            {
                AnalysisMain.CommandLineArgs(args);
            }
        }

        

        

        

        public static BitmapImage loadBitmap(System.Drawing.Bitmap source)
        {
            
            BitmapImage bmpi = null;
            try
            {
                MemoryStream ms = new MemoryStream();
                source.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                bmpi = new BitmapImage();
                bmpi.BeginInit();
                bmpi.StreamSource = ms;
                bmpi.EndInit();
                
               
            }catch(Exception ex)
            {
                Debug.WriteLine("Error in Load Bitmap:-" + ex.Message);
            }

            return bmpi;
        }
        
    }
    
}

