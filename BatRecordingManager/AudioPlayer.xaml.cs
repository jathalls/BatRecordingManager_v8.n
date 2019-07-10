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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Language.Intellisense;
using NAudio.Wave;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for AudioPlayerIK.xaml
    /// </summary>
    public partial class AudioPlayer : Window
    {
        private NaudioWrapper _wrapper;

        /// <summary>
        ///     Constructor for the AudioPlayer
        /// </summary>
        public AudioPlayer()
        {
            InitializeComponent();
            DataContext = this;

            Closing += AudioPlayer_Closing;
            SetButtonVisibility();
            //PlayListItem pli = PlayListItem.Create(@"X:\BatRecordings\2018\Knebworth-KNB18-2_20180816\KNB18-2p_20180816\KNB18-2p_20180816_212555.wav", TimeSpan.FromSeconds(218), TimeSpan.FromSeconds(8), "Comment line");
            //PlayList.Add(pli);
        }


        /// <summary>
        ///     List of items that can be played if selected
        /// </summary>
        public BulkObservableCollection<PlayListItem> PlayList { get; set; } =
            new BulkObservableCollection<PlayListItem>();

        /// <summary>
        ///     Adds a specific labelled segment to the playlist
        /// </summary>
        /// <param name="segmentToAdd"></param>
        public int AddToList(LabelledSegment segmentToAdd)
        {
            if (PlayList == null) PlayList = new BulkObservableCollection<PlayListItem>();
            var filename = segmentToAdd.Recording.GetFileName();
            if (string.IsNullOrWhiteSpace(filename))
            {
                MessageBox.Show("No file found on this computer for this segment");
                return PlayList.Count;
            }

            var start = segmentToAdd.StartOffset;
            var duration = segmentToAdd.Duration() ?? new TimeSpan();
            var comment = segmentToAdd.Comment;
            var pli = PlayListItem.Create(filename, start, duration, comment);
            AddToPlayList(pli);


            return PlayList.Count;
        }

        private void SetButtonVisibility()
        {
            if (PlayList == null || PlayList.Count <= 0)
            {
                PlayButton.IsEnabled = false;
                PlayLoopedButton.IsEnabled = false;
            }
            else
            {
                PlayButton.IsEnabled = true;
                PlayLoopedButton.IsEnabled = true;
            }
        }

        /// <summary>
        ///     event handler triggered when the window is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AudioPlayer_Closing(object sender, CancelEventArgs e)
        {
            if (_wrapper != null)
            {
                _wrapper.Dispose();
                var i = 0;
                while (_wrapper != null && _wrapper.playBackState != PlaybackState.Stopped)
                {
                    Thread.Sleep(100);
                    if (i++ > 10) _wrapper = null;
                }
            }
        }

        /// <summary>
        ///     constructs a playlistitem and adds it to the playlist
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="start"></param>
        /// <param name="duration"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public bool AddToPlayList(string filename, TimeSpan start, TimeSpan duration, string label)
        {
            var pli = PlayListItem.Create(filename, start, duration, label);
            if (pli != null) return AddToPlayList(pli);
            return false;
        }

        /// <summary>
        ///     Adds a pre-constructed playlistitem to the playlist
        /// </summary>
        /// <param name="pli"></param>
        /// <returns></returns>
        public bool AddToPlayList(PlayListItem pli)
        {
            if (pli == null) return false;
            PlayList.Add(pli);
            SetButtonVisibility();
            return true;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            string filename = "";
            var looped = (sender as Button).Content as string == "LOOP";
            if ((sender as Button).Content as String == "SAVE")
            {
                filename = Tools.GetFileToWriteTo("", ".wav");
            }
            if (PlayButton.Content as string == "PLAY")
            {
                PlayListItem itemToPlay = GetItemToPlay();

                if (itemToPlay != null)
                {
                    PlayItem(itemToPlay, looped,filename);
                    if (string.IsNullOrWhiteSpace(filename))
                    {
                        PlayButton.Content = "STOP";
                    }
                    else
                    {
                        StopPlaying();
                    }
                }
            }
            else
            {
                StopPlaying();
            }
        }

        private void StopPlaying()
        {
            if (_wrapper != null)
            {
                _wrapper.Stop();
                if (_wrapper.playBackState == PlaybackState.Stopped)
                {
                    _wrapper.Dispose();
                    _wrapper = null;
                    PlayButton.Content = "PLAY";
                }
            }
        }

        private PlayListItem GetItemToPlay()
        {
            PlayListItem item = null;
            if (!PlayList.IsNullOrEmpty())
            {
                if (PlayListDatagrid.SelectedItem != null)
                    item = PlayListDatagrid.SelectedItem as PlayListItem;
                else
                    item = PlayList.First();
            }

            return (item);
        }

        private void PlayItem(PlayListItem itemToPlay, bool playLooped,string filename)
        {
            _wrapper = new NaudioWrapper {Frequency = (decimal) Frequency};
            _wrapper.e_Stopped += Wrapper_Stopped;
            if (!TunedButton.IsChecked ?? false)
            {
                var rate = 1.0m;

                if (TenthButton.IsChecked ?? false) rate = 0.1m;
                if (FifthButton.IsChecked ?? false) rate = 0.2m;
                if (TwentiethButton.IsChecked ?? false) rate = 0.05m;
                
                _wrapper.Play(itemToPlay, rate, playLooped,filename);
            }
            else
            {
                _wrapper.Heterodyne(itemToPlay, filename);
            }
        }

        private void Wrapper_Stopped(object sender, EventArgs e)
        {
            if (_wrapper != null)
            {
                _wrapper.Dispose();
                _wrapper = null;
            }

            PlayButton.Content = "PLAY";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_wrapper != null)
            {
                _wrapper.Dispose();
                _wrapper = null;
            }

            PlayButton.Content = "PLAY";
            Close();
        }

        private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_wrapper != null) _wrapper.Frequency = (decimal) (sender as Slider).Value;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_wrapper != null)
            {
                _wrapper.Dispose();
                _wrapper = null;
            }

            PlayList?.Clear();
            ShowInTaskbar = true;
            WindowState = WindowState.Minimized;
            PlayButton.Content = "PLAY";
            e.Cancel = true;
        }

        internal void Stop()
        {
            _wrapper?.Stop();
        }

        #region

        /// <summary>
        ///     Frequency Dependency Property
        /// </summary>
        public static readonly DependencyProperty FrequencyProperty =
            DependencyProperty.Register("Frequency", typeof(double), typeof(AudioPlayer),
                new FrameworkPropertyMetadata(50.0d));

        /// <summary>
        ///     Gets or sets the Frequency property.  This dependency property
        ///     indicates ....
        /// </summary>
        public double Frequency
        {
            get => (double) GetValue(FrequencyProperty);
            set
            {
                if (_wrapper != null) _wrapper.Frequency = (decimal) value;
                SetValue(FrequencyProperty, value);
            }
        }

        #endregion

       
    }

    /// <summary>
    ///     A class to hold items to be displayed in the AudioPlayer playlist
    /// </summary>
    public class PlayListItem
    {
        /// <summary>
        ///     fully qualified name of the source .wav file
        /// </summary>
        public string filename { get; set; }

        /// <summary>
        ///     offset in the file for the start of the region to be played
        /// </summary>
        public TimeSpan startOffset { get; set; }

        /// <summary>
        ///     duration of the segment to be played
        /// </summary>
        public TimeSpan playLength { get; set; }

        /// <summary>
        ///     label of the original labelled segment or other comment for the playlist display
        /// </summary>
        public string label { get; set; }

        /// <summary>
        ///     Constructor for playlist elements
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="start"></param>
        /// <param name="duration"></param>
        /// <param name="label"></param>
        public static PlayListItem Create(string filename, TimeSpan start, TimeSpan duration, string label)
        {
            if (string.IsNullOrWhiteSpace(filename)) return null;
            if (!File.Exists(filename)) return null;
            var result = new PlayListItem
            {
                filename = filename, startOffset = start, playLength = duration, label = label
            };


            return result;
        }
    }
}