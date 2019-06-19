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