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

using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatPassSummaryControl.xaml
    /// </summary>
    public partial class BatPassSummaryControl : UserControl
    {
        private BatStats _passSummary;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatPassSummaryControl" /> class.
        /// </summary>
        public BatPassSummaryControl()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Gets or sets the PassSummary property. This dependency property indicates ....
        /// </summary>
        public BatStats PassSummary
        {
            get => _passSummary;
            set
            {
                _passSummary = value;

                SummaryStackpanel.Children.Clear();

                var statstring = Tools.GetFormattedBatStats(value, false);
                var statLabel = new Label {Content = statstring};
                SummaryStackpanel.Children.Add(statLabel);
                InvalidateArrange();
                UpdateLayout();
            }
        }
    }
}