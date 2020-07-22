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


using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using System;
using System.Diagnostics;
using System.IO;

namespace BatRecordingManager
{
    /// <summary>
    ///     a class to carry the implementation od CScore audio components
    /// </summary>
    internal class CscoreWrapper
    {
        private readonly MMDevice _device;
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;


        public CscoreWrapper()
        {
            using (var mmdeviceEnumerator = new MMDeviceEnumerator())
            {
                using (var mmdeviceCollection =
                    mmdeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                {
                    foreach (var dev in mmdeviceCollection)
                    {
                        Debug.WriteLine(dev.DeviceID + ":-" + dev.FriendlyName);
                        if (_device == null) _device = dev;
                    }
                }
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                if (_soundOut != null)
                    return _soundOut.PlaybackState;
                return PlaybackState.Stopped;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.GetPosition();
                return TimeSpan.Zero;
            }
            set => _waveSource?.SetPosition(value);
        }

        public TimeSpan Length
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.GetLength();
                return TimeSpan.Zero;
            }
        }

        public int Volume
        {
            get
            {
                if (_soundOut != null)
                    return Math.Min(100, Math.Max((int) (_soundOut.Volume * 100), 0));
                return 100;
            }
            set
            {
                if (_soundOut != null) _soundOut.Volume = Math.Min(1.0f, Math.Max(value / 100f, 0f));
            }
        }

        public event EventHandler<PlaybackStoppedEventArgs> e_PlaybackStopped;

        public void Play(PlayListItem itemToPlay, double speed)
        {
            if (itemToPlay == null) return;
            if (string.IsNullOrWhiteSpace(itemToPlay.filename)) return;
            if (!File.Exists(itemToPlay.filename) || (new FileInfo(itemToPlay.filename).Length<=0L)) return;

            Open(itemToPlay, _device);
            _waveSource.SetPosition(itemToPlay.startOffset);
            _waveSource.ChangeSampleRate(_waveSource.WaveFormat.SampleRate / 10);
            var end = itemToPlay.startOffset + itemToPlay.playLength;
            Debug.WriteLine("Play from " + itemToPlay.startOffset + " to " + end);
            Debug.WriteLine("Starting at:-" + DateTime.Now.TimeOfDay);

            _soundOut = new WasapiOut {Latency = 100, Device = _device};
            _soundOut.Initialize(_waveSource);
            if (e_PlaybackStopped != null) _soundOut.Stopped += e_PlaybackStopped;
            Play();
            while (PlaybackState != PlaybackState.Stopped)
            {
                var pos = Position;
                if (pos > end)
                {
                    Stop();
                    Debug.WriteLine("Stopped at end:-" + DateTime.Now.TimeOfDay + " position=" + pos);
                }
            }

            Debug.WriteLine("Play ended:-" + DateTime.Now.TimeOfDay);
        }

        public void Open(PlayListItem itemToPlay, MMDevice device)
        {
            CleanupPlayback();

            var codec = CodecFactory.Instance.GetCodec(itemToPlay.filename);
            var source = codec.ToSampleSource();

            var mono = source.ToMono();
            _waveSource = mono.ToWaveSource();
            using (var ms = new MemoryStream())
            {
                _waveSource.WriteToStream(ms);
                var wf = _waveSource.WaveFormat;
                var wi = new WaveIn(new WaveFormat(wf.SampleRate / 10, wf.BitsPerSample, wf.Channels));
            }


            //_soundOut = new WasapiOut() { Latency = 100, Device = device };
            // _soundOut.Initialize(_waveSource);
            // if (PlaybackStopped != null) _soundOut.Stopped += PlaybackStopped;
        }

        public void Play()
        {
            _soundOut?.Play();
        }

        public void Pause()
        {
            _soundOut?.Pause();
        }

        public void Stop()
        {
            _soundOut?.Stop();
        }

        private void CleanupPlayback()
        {
            if (_soundOut != null)
            {
                _soundOut.Dispose();
                _soundOut = null;
            }

            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        protected void Dispose(bool disposing)
        {
            //base.Dispose(disposing);
            CleanupPlayback();
        }
    }
}