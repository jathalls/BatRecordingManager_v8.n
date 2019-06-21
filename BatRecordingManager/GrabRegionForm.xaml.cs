/*
 *  Copyright 2016 Justin A T Halls

        Licensed under the Apache License, Version 2.0 (the "License");
        you may not use this file except in compliance with the License.
        You may obtain a copy of the License at

            http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS,
        WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        See the License for the specific language governing permissions and
        limitations under the License.

 */

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