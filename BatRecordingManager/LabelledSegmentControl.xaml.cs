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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for LabelledSegmentControl.xaml
    /// </summary>
    public partial class LabelledSegmentControl : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LabelledSegmentControl" /> class.
        /// </summary>
        public LabelledSegmentControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region labelledSegment

        /// <summary>
        ///     labelledSegment Dependency Property
        /// </summary>
        public static readonly DependencyProperty labelledSegmentProperty =
            DependencyProperty.Register(nameof(labelledSegment), typeof(LabelledSegment), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata(new LabelledSegment()));

        /// <summary>
        ///     Gets or sets the labelledSegment property. This dependency property indicates ....
        /// </summary>
        public LabelledSegment labelledSegment
        {
            get => (LabelledSegment) GetValue(labelledSegmentProperty);
            set
            {
                SetValue(labelledSegmentProperty, value);
                startTime = value.StartOffset;
                endTime = value.EndOffset;
                duration = endTime - startTime;
                comment = value.Comment;
                if (value.SegmentCalls != null && value.SegmentCalls.Count > 0)
                    CallParametersLabel.Visibility = Visibility.Hidden;
                else
                    CallParametersLabel.Visibility = Visibility.Visible;
            }
        }

        #endregion labelledSegment

        #region startTime

        /// <summary>
        ///     startTime Dependency Property
        /// </summary>
        public static readonly DependencyProperty startTimeProperty =
            DependencyProperty.Register(nameof(startTime), typeof(TimeSpan), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata(new TimeSpan()));

        /// <summary>
        ///     Gets or sets the startTime property. This dependency property indicates ....
        /// </summary>
        public TimeSpan startTime
        {
            get => (TimeSpan) GetValue(startTimeProperty);
            set => SetValue(startTimeProperty, value);
        }

        #endregion startTime

        #region endTime

        /// <summary>
        ///     endTime Dependency Property
        /// </summary>
        public static readonly DependencyProperty endTimeProperty =
            DependencyProperty.Register(nameof(endTime), typeof(TimeSpan), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata(new TimeSpan()));

        /// <summary>
        ///     Gets or sets the endTime property. This dependency property indicates ....
        /// </summary>
        public TimeSpan endTime
        {
            get => (TimeSpan) GetValue(endTimeProperty);
            set => SetValue(endTimeProperty, value);
        }

        #endregion endTime

        #region duration

        /// <summary>
        ///     duration Dependency Property
        /// </summary>
        public static readonly DependencyProperty durationProperty =
            DependencyProperty.Register(nameof(duration), typeof(TimeSpan), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata(new TimeSpan()));

        /// <summary>
        ///     Gets or sets the duration property. This dependency property indicates ....
        /// </summary>
        public TimeSpan duration
        {
            get => (TimeSpan) GetValue(durationProperty);
            set => SetValue(durationProperty, value);
        }

        #endregion duration

        #region comment

        /// <summary>
        ///     comment Dependency Property
        /// </summary>
        public static readonly DependencyProperty commentProperty =
            DependencyProperty.Register(nameof(comment), typeof(string), typeof(LabelledSegmentControl),
                new FrameworkPropertyMetadata(""));

        /// <summary>
        ///     Gets or sets the comment property. This dependency property indicates ....
        /// </summary>
        public string comment
        {
            get => (string) GetValue(commentProperty);
            set => SetValue(commentProperty, value);
        }

        #endregion comment
    }

    #region TimeSpanConverter (ValueConverter)

    /// <summary>
    ///     Converter class for displaying a timespan object as a string
    /// </summary>
    public class TimeSpanConverter : IValueConverter
    {
        /// <summary>
        ///     Converts a Timespan object into a formatted string
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (!(value is TimeSpan ts)) return "";
                var result = Tools.FormattedTimeSpan(ts);

                return result;
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion TimeSpanConverter (ValueConverter)

    #region CallParametersConverter (ValueConverter)

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class CallParametersConverter : IValueConverter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                var summary = "";
                var segment = value as LabelledSegment;
                if (segment.SegmentCalls != null && segment.SegmentCalls.Count > 0)
                    foreach (var call in segment.SegmentCalls)
                    {
                        if (!string.IsNullOrWhiteSpace(summary))
                            summary = summary + @"
";
                        else
                            summary = "";
                        summary = summary +
                                  $"{call.Call.StartFrequency,5:##0.0},{call.Call.EndFrequency,5:##0.0},{call.Call.PeakFrequency,5:##0.0}kHz {call.Call.PulseDuration,5:##0.0},{call.Call.PulseInterval,5:##0.0}mS";
                    }

                return summary;
            }
            catch
            {
                return value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Not implemented
            return null;
        }
    }

    #endregion CallParametersConverter (ValueConverter)
}