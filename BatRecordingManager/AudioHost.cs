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

using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Host class for the AudioPlayer window
    /// </summary>
    public sealed class AudioHost
    {
        private AudioPlayer _audioPlayer;

        static AudioHost()
        {
        }

        private AudioHost()
        {
        }

        /// <summary>
        ///     public accessor to the AudioPlayer for this inatsance
        /// </summary>
        public AudioPlayer audioPlayer
        {
            get
            {
                if (_audioPlayer == null)
                {
                    _audioPlayer = new AudioPlayer();
                    _audioPlayer.Show();
                }

                if (_audioPlayer.WindowState == WindowState.Minimized) _audioPlayer.WindowState = WindowState.Normal;
                return _audioPlayer;
            }
        }

        /// <summary>
        ///     returns an instance of the AudioHost holding an AudioPlayer
        /// </summary>
        public static AudioHost Instance { get; } = new AudioHost();

        internal void Close()
        {
            if (_audioPlayer != null)
            {
                _audioPlayer.Close();
                _audioPlayer = null;
            }
        }
    }
}