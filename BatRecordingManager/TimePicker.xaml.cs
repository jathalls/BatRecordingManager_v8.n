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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for TimePicker.xaml
    /// </summary>
    public partial class TimePicker : UserControl
    {
        /// <summary>
        ///     The hours property
        /// </summary>
        public static readonly DependencyProperty HoursProperty =
            DependencyProperty.Register(nameof(Hours), typeof(int), typeof(TimePicker),
                new UIPropertyMetadata(0, OnTimeChanged));

        /// <summary>
        ///     The minutes property
        /// </summary>
        public static readonly DependencyProperty MinutesProperty =
            DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(TimePicker),
                new UIPropertyMetadata(0, OnTimeChanged));

        /// <summary>
        ///     The seconds property
        /// </summary>
        public static readonly DependencyProperty SecondsProperty =
            DependencyProperty.Register(nameof(Seconds), typeof(int), typeof(TimePicker),
                new UIPropertyMetadata(0, OnTimeChanged));

        /// <summary>
        ///     The value property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(TimeSpan), typeof(TimePicker),
                new UIPropertyMetadata(DateTime.Now.TimeOfDay, OnValueChanged));

        /// <summary>
        ///     Initializes a new instance of the <see cref="TimePicker" /> class.
        /// </summary>
        public TimePicker()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Gets or sets the hours.
        /// </summary>
        /// <value>
        ///     The hours.
        /// </value>
        public int Hours
        {
            get => (int) GetValue(HoursProperty);
            set => SetValue(HoursProperty, value);
        }

        /// <summary>
        ///     Gets or sets the minutes.
        /// </summary>
        /// <value>
        ///     The minutes.
        /// </value>
        public int Minutes
        {
            get => (int) GetValue(MinutesProperty);
            set => SetValue(MinutesProperty, value);
        }

        /// <summary>
        ///     Gets or sets the seconds.
        /// </summary>
        /// <value>
        ///     The seconds.
        /// </value>
        public int Seconds
        {
            get => (int) GetValue(SecondsProperty);
            set => SetValue(SecondsProperty, value);
        }

        /// <summary>
        ///     Gets or sets the value.
        /// </summary>
        /// <value>
        ///     The value.
        /// </value>
        public TimeSpan Value
        {
            get => (TimeSpan) GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        ///     Called when [time changed].
        /// </summary>
        /// <param name="obj">
        ///     The object.
        /// </param>
        /// <param name="e">
        ///     The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.
        /// </param>
        private static void OnTimeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = obj as TimePicker;
            control.Value = new TimeSpan(control.Hours, control.Minutes, control.Seconds);
        }

        /// <summary>
        ///     Called when [value changed].
        /// </summary>
        /// <param name="obj">
        ///     The object.
        /// </param>
        /// <param name="e">
        ///     The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.
        /// </param>
        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = obj as TimePicker;
            control.Hours = ((TimeSpan) e.NewValue).Hours;
            control.Minutes = ((TimeSpan) e.NewValue).Minutes;
            control.Seconds = ((TimeSpan) e.NewValue).Seconds;
        }

        /// <summary>
        ///     Downs the specified sender.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="args">
        ///     The <see cref="KeyEventArgs" /> instance containing the event data.
        /// </param>
        private void Down(object sender, KeyEventArgs args)
        {
            switch (((Grid) sender).Name)
            {
                case "sec":
                    if (args.Key == Key.Up)
                        Seconds++;
                    if (args.Key == Key.Down)
                        Seconds--;
                    break;

                case "min":
                    if (args.Key == Key.Up)
                        Minutes++;
                    if (args.Key == Key.Down)
                        Minutes--;
                    break;

                case "hour":
                    if (args.Key == Key.Up)
                        Hours++;
                    if (args.Key == Key.Down)
                        Hours--;
                    break;
            }
        }
    }
}