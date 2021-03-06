﻿// *  Copyright 2016 Justin A T Halls
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
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatCallControl.xaml
    /// </summary>
    public partial class BatCallControl : UserControl
    {
        private readonly object _showImageButtonPressedEventLock = new object();
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public readonly BulkObservableCollection<StoredImage> CallImageList =
            new BulkObservableCollection<StoredImage>();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private Brush _mDefaultBrush = Brushes.Cornsilk;
        private EventHandler<EventArgs> _showImageButtonPressedEvent;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public BatCallControl()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();
            DataContext = BatCall;
            SetReadOnly(true);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatCallControl" /> class.
        /// </summary>
        /// <param name="isReadOnly">
        ///     if set to <c>true</c> [is read only].
        /// </param>
        public BatCallControl(bool isReadOnly)
        {
            if (BatCall == null) BatCall = new Call();
            InitializeComponent();
            DataContext = BatCall;
            SetReadOnly(isReadOnly);
        }

        /// <summary>
        ///     Sets the read only.
        /// </summary>
        /// <param name="isReadOnly">
        ///     if set to <c>true</c> [is read only].
        /// </param>
        public void SetReadOnly(bool isReadOnly)
        {
            StartFreqUpDown.IsReadOnly = isReadOnly;
            StartFreqVariationTextBox.IsReadOnly = isReadOnly;
            EndFreqTextBox.IsReadOnly = isReadOnly;
            EndFreqVariationTextBox.IsReadOnly = isReadOnly;
            PeakFreqTextBox.IsReadOnly = isReadOnly;
            PeakFreqVariationTextBox.IsReadOnly = isReadOnly;
            PulseDurationTextBox.IsReadOnly = isReadOnly;
            PulseDurationVariationTextBox.IsReadOnly = isReadOnly;
            PulseIntervalTextBox.IsReadOnly = isReadOnly;
            PulseIntervalVariationTextBox.IsReadOnly = isReadOnly;
            CallTypeTextBox.IsReadOnly = isReadOnly;
            CallFunctionTextBox.IsReadOnly = isReadOnly;
            CallTypeNotesBox.IsReadOnly = isReadOnly;
        }

        private void ShowImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button)) return;
            var button = sender as Button;
            if (button.Background != Brushes.Coral)
            {
                _mDefaultBrush = button.Background;
                button.Background = Brushes.Coral;
            }
            else
            {
                button.Background = _mDefaultBrush;
            }

            OnShowImageButtonPressed(new EventArgs());
        }

        /// <summary>
        ///     toggles the state of the showimage button
        /// </summary>
        internal void Reset()
        {
            if (ShowImageButton.Background == Brushes.Coral)
                ShowImageButton_Click(ShowImageButton, new RoutedEventArgs());
        }

        private void StartFrequencySeparatorLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Label)) return;
            var call = BatCall;
            var sl = sender as Label;
            sl.Content = sl.Content as string == "+/-" ? " - " : "+/-";
            BatCall = call;
        }

        /// <summary>
        ///     Event raised when the Image button is pressed to tell a parent class to display
        ///     the list of images for this call in its own ImageScrollerControl.
        /// </summary>
        public event EventHandler<EventArgs> e_ShowImageButtonPressed
        {
            add
            {
                lock (_showImageButtonPressedEventLock)
                {
                    _showImageButtonPressedEvent += value;
                }
            }
            remove
            {
                lock (_showImageButtonPressedEventLock)
                {
                    _showImageButtonPressedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_ShowImageButtonPressed" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnShowImageButtonPressed(EventArgs e)
        {
            EventHandler<EventArgs> handler = null;

            lock (_showImageButtonPressedEventLock)
            {
                handler = _showImageButtonPressedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        #region BatCall

        /// <summary>
        ///     BatCall Dependency Property
        /// </summary>
        public static readonly DependencyProperty BatCallProperty =
            DependencyProperty.Register(nameof(BatCall), typeof(Call), typeof(BatCallControl),
                new FrameworkPropertyMetadata(new Call()));

        /// <summary>
        ///     Gets or sets the BatCall property. This dependency property indicates ....
        /// </summary>
        public Call BatCall
        {
            get
            {
                var result = (Call) GetValue(BatCallProperty);

                result.CallPictures.Clear();
                if (CallImageList != null)
                    foreach (var storedImage in CallImageList)
                    {
                        var callPicture = new CallPicture {BinaryData = storedImage.GetAsBinaryData()};
                        result.CallPictures.Add(callPicture);
                    }

                if (result != null)
                {
                    result.CallFunction = CallFunctionTextBox.Text;
                    result.CallNotes = CallTypeNotesBox.Text;
                    result.CallType = CallTypeTextBox.Text;
                    if (StartFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        result.StartFrequency = StartFreqUpDown.Value;
                        result.StartFrequencyVariation = StartFreqVariationTextBox.Value;
                    }
                    else
                    {
                        var hi = StartFreqUpDown.Value ?? 0.0;
                        var lo = StartFreqVariationTextBox.Value ?? 0.0;

                        result.StartFrequency = (hi + lo) / 2.0d;
                        result.StartFrequencyVariation = Math.Abs((hi - lo) / 2.0d);
                    }

                    if (EndFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        result.EndFrequency = EndFreqTextBox.Value;
                        result.EndFrequencyVariation = EndFreqVariationTextBox.Value;
                    }
                    else
                    {
                        var hi = EndFreqTextBox.Value ?? 0.0;
                        var lo = EndFreqVariationTextBox.Value ?? 0.0;
                        result.EndFrequency = (hi + lo) / 2.0d;
                        result.EndFrequencyVariation = Math.Abs((hi - lo) / 2.0d);
                    }

                    if (PeakFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        result.PeakFrequency = PeakFreqTextBox.Value;
                        result.PeakFrequencyVariation = PeakFreqVariationTextBox.Value;
                    }
                    else
                    {
                        var hi = PeakFreqTextBox.Value ?? 0.0;
                        var lo = PeakFreqVariationTextBox.Value ?? 0.0;
                        result.PeakFrequency = (hi + lo) / 2.0d;
                        result.PeakFrequencyVariation = Math.Abs((hi - lo) / 2.0d);
                    }

                    if (DurationSeparatorLabel.Content as string == "+/-")
                    {
                        result.PulseDuration = PulseDurationTextBox.Value;
                        result.PulseDurationVariation = PulseDurationVariationTextBox.Value;
                    }
                    else
                    {
                        var hi = PulseDurationTextBox.Value ?? 0.0;
                        var lo = PulseDurationVariationTextBox.Value ?? 0.0;
                        result.PulseDuration = (hi + lo) / 2.0d;
                        result.PulseDurationVariation = Math.Abs((hi - lo) / 2.0d);
                    }

                    if (IntervalSeparatorLabel.Content as string == "+/-")
                    {
                        result.PulseInterval = PulseIntervalTextBox.Value;
                        result.PulseIntervalVariation = PulseIntervalVariationTextBox.Value;
                    }
                    else
                    {
                        var hi = PulseIntervalTextBox.Value ?? 0.0;
                        var lo = PulseIntervalVariationTextBox.Value ?? 0.0;
                        result.PulseInterval = (hi + lo) / 2.0d;
                        result.PulseIntervalVariation = Math.Abs((hi - lo) / 2.0d);
                    }
                }

                return result;
            }
            set
            {
                SetValue(BatCallProperty, value);
                if (value != null)
                {
                    ShowImageButton.IsEnabled = true;
                    CallImageList.Clear();
                    if (!value.CallPictures.IsNullOrEmpty())
                        foreach (var callPicture in value.CallPictures)
                        {
                            var callImage = new StoredImage(null, "", "", -1);
                            if (callPicture.BinaryData.BinaryDataType == "BMPS" ||
                                callPicture.BinaryData.BinaryDataType == "BMP" ||
                                callPicture.BinaryData.BinaryDataType.Trim() == "PNG")
                            {
                                callImage.SetBinaryData(callPicture.BinaryData);
                                CallImageList.Add(callImage);
                            }
                        }

                    if (StartFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        StartFreqUpDown.Value = value.StartFrequency;
                        StartFreqVariationTextBox.Value = value.StartFrequencyVariation;
                    }
                    else
                    {
                        var mid = value.StartFrequency ?? 0.0;
                        var var = value.StartFrequencyVariation ?? 0.0;
                        StartFreqUpDown.Value = mid + var;
                        StartFreqVariationTextBox.Value = mid - var;
                    }

                    if (EndFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        EndFreqTextBox.Value = value.EndFrequency;
                        EndFreqVariationTextBox.Value = value.EndFrequencyVariation;
                    }
                    else
                    {
                        var mid = value.EndFrequency ?? 0.0;
                        var var = value.EndFrequencyVariation ?? 0.0;
                        EndFreqTextBox.Value = mid + var;
                        EndFreqVariationTextBox.Value = mid - var;
                    }

                    if (PeakFrequencySeparatorLabel.Content as string == "+/-")
                    {
                        PeakFreqTextBox.Value = value.PeakFrequency;
                        PeakFreqVariationTextBox.Value = value.PeakFrequencyVariation;
                    }
                    else
                    {
                        var mid = value.PeakFrequency ?? 0.0;
                        var var = value.PeakFrequencyVariation ?? 0.0;
                        PeakFreqTextBox.Value = mid + var;
                        PeakFreqVariationTextBox.Value = mid - var;
                    }

                    if (DurationSeparatorLabel.Content as string == "+/-")
                    {
                        PulseDurationTextBox.Value = value.PulseDuration;
                        PulseDurationVariationTextBox.Value = value.PulseDurationVariation;
                    }
                    else
                    {
                        var mid = value.PulseDuration ?? 0.0;
                        var var = value.PulseDurationVariation ?? 0.0;
                        PulseDurationTextBox.Value = mid + var;
                        PulseDurationVariationTextBox.Value = mid - var;
                    }

                    if (IntervalSeparatorLabel.Content as string == "+/-")
                    {
                        PulseIntervalTextBox.Value = value.PulseInterval;
                        PulseIntervalVariationTextBox.Value = value.PulseIntervalVariation;
                    }
                    else
                    {
                        var mid = value.PulseInterval ?? 0.0;
                        var var = value.PulseIntervalVariation ?? 0.0;
                        PulseIntervalTextBox.Value = mid + var;
                        PulseIntervalVariationTextBox.Value = mid - var;
                    }

                    CallTypeNotesBox.Text = value.CallNotes;
                    CallTypeTextBox.Text = value.CallType;
                    CallFunctionTextBox.Text = value.CallFunction;
                }
                else
                {
                    SetReadOnly(true);
                }
            }
        }

        #endregion BatCall
    }
}