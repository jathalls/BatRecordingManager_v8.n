using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for GrabRegionForm.xaml
    /// </summary>
    public partial class GrabRegionForm : Window
    {
        public GrabRegionForm()
        {
            InitializeComponent();
        }

        public Rectangle rect { get; set; }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
                if (e.ChangedButton == MouseButton.Left && Mouse.LeftButton == MouseButtonState.Pressed)
                    try
                    {
                        DragMove();
                        e.Handled = true;
                    }
                    catch (Exception)
                    {
                    }
        }

        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                if (e.ChangedButton == MouseButton.Right)
                {
                    rect = new Rectangle((int) Left, (int) Top, (int) Width, (int) Height);

                    Close();
                }
            }
        }
    }
}