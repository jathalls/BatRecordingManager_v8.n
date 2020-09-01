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
    ///     Interaction logic for LabelledSegmentForm.xaml
    /// </summary>
    public partial class LabelledSegmentForm : Window
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LabelledSegmentForm" /> class.
        /// </summary>
        public LabelledSegmentForm()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        #region labelledSegment

        /// <summary>
        ///     labelledSegment Dependency Property
        /// </summary>
        public static readonly DependencyProperty labelledSegmentProperty =
            DependencyProperty.Register(nameof(labelledSegment), typeof(LabelledSegment), typeof(LabelledSegmentForm),
                new FrameworkPropertyMetadata(new LabelledSegment()));

        /// <summary>
        ///     Gets or sets the labelledSegment property. This dependency property indicates ....
        /// </summary>
        public LabelledSegment labelledSegment
        {
            get
            {
                var result = (LabelledSegment)GetValue(labelledSegmentProperty);
                result.StartOffset = Tools.ConvertDoubleToTimeSpan((double)StartOffsetDoubleUpDown.Value);
                result.EndOffset = Tools.ConvertDoubleToTimeSpan((double)EndOffsetDoubleUpDown.Value);
                result.Comment = CommentTextBox.Text;
                return result;
            }
            set
            {
                SetValue(labelledSegmentProperty, value);
                StartOffsetDoubleUpDown.Value = (decimal)value.StartOffset.TotalSeconds;
                EndOffsetDoubleUpDown.Value = (decimal)value.EndOffset.TotalSeconds;
                CommentTextBox.Text = value.Comment;
            }
        }

        #endregion labelledSegment
    }
}