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

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string _dbFileLocation;

        private static string _dbFileName;

        public static bool ShowDatabase { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static string dbFileLocation
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return _dbFileLocation; }
            set { _dbFileLocation = value; }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static string dbFileName
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return _dbFileName; }
            set { _dbFileName = value; }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            foreach (var arg in e.Args)
                if (arg.Contains("debug"))
                    ShowDatabase = true;
#if DEBUG
            ShowDatabase = true;
#endif
            foreach (var arg in e.Args)
                if (arg.Contains("nodebug") || arg.Contains("undebug"))
                    ShowDatabase = false;
            base.OnStartup(e);
        }
    }
}