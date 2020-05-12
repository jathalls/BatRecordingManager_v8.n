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

using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for AboutScreen.xaml
    /// </summary>
    public partial class AboutScreen : Window
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AboutScreen" /> class.
        /// </summary>
        /// 
        
        public AboutScreen()
        {
            var build = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            AssemblyVersion = "Build " + build;
            InitializeComponent();
            DataContext = this;
            Version.Content = "v  " + build ;
            DbVer.Content = "    Database Version " + DBAccess.GetDatabaseVersion() + " named:- " +
                            DBAccess.GetWorkingDatabaseName(DBAccess.GetWorkingDatabaseLocation());
        }

        #region AssemblyVersion

        /// <summary>
        ///     AssemblyVersion Dependency Property
        /// </summary>
        public static readonly DependencyProperty AssemblyVersionProperty =
            DependencyProperty.Register(nameof(AssemblyVersion), typeof(string), typeof(AboutScreen),
                new FrameworkPropertyMetadata(""));

        /// <summary>
        ///     Gets or sets the AssemblyVersion property.  This dependency property
        ///     indicates ....
        /// </summary>
        public string AssemblyVersion
        {
            get => (string) GetValue(AssemblyVersionProperty);
            set => SetValue(AssemblyVersionProperty, value);
        }

        #endregion AssemblyVersion

        private void Weather_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("https://darksky.net/poweredby");
        }
    }
}