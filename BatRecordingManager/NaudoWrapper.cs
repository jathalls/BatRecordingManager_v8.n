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
using System.Diagnostics;
using System.IO;
using NAudio.Dsp;
using NAudio.Utils;
using NAudio.Wave;

namespace BatRecordingManager
{
    /// <summary>
    ///     A class to hold all interface functions between the application and the Naudio system
    /// </summary>
    public class NaudioWrapper : IDisposable
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///
        private readonly object _stoppedEventLock = new object();

        private WaveFormatConversionProvider _converter;
        private bool _doLoop;

        private bool _isDisposing;
        private MemoryStream _ms;
        private WaveOut _player;
        private WaveFileReader _reader;
        private MediaFoundationResampler _resampler;
        private EventHandler _stoppedEvent;
        private AudioFileReader _wave;

        private PlayListItem currentItem { get; set; }
        private decimal currentSpeed { get; set; }

        public decimal Frequency { get; set; } = 50.0m;

        public PlaybackState playBackState
        {
            get
            {
                if (_player != null)
                    return _player.PlaybackState;
                return PlaybackState.Stopped;
            }
        }

        /// <summary>
        ///     Tidy up before disposal
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposing)
            {
                _doLoop = false;
                _isDisposing = true;
                if (_player != null && _player.PlaybackState == PlaybackState.Playing)
                {
                    if (!Stop())
                    {
                        _player.Dispose();
                        _player = null;
                    }
                }
                else
                {
                    if (_reader != null)
                    {
                        _reader.Dispose();
                        _reader = null;
                    }

                    if (_ms != null)
                    {
                        _ms.Dispose();
                        _ms = null;
                    }
                }

                CleanUp();
            }
        }

        private void CleanUp()
        {
            if (_player != null)
            {
                _player.Dispose();
                _player = null;
            }

            if (_reader != null && !_doLoop)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_ms != null && !_doLoop)
            {
                _ms.Dispose();
                _ms = null;
            }

            if (_converter != null)
            {
                _converter.Dispose();
                _converter = null;
            }

            if (_wave != null)
            {
                _wave.Dispose();
                _wave = null;
            }

            if (_resampler != null)
            {
                _resampler.Dispose();
                _resampler = null;
            }
        }

        public void Loop(PlayListItem itemToPlay, decimal speedFactor)
        {
            Play(itemToPlay, speedFactor, true);
        }

        /// <summary>
        ///     Plays an item in heterodyned format.
        ///     If in debug mode and filename is specified also saves a copy of the output to a file.
        /// </summary>
        /// <param name="itemToPlay"></param>
        /// <param name="fileName"></param>
        public void Heterodyne(PlayListItem itemToPlay, string fileName = "")
        {
            CleanUp();
            currentItem = itemToPlay;
            currentSpeed = 0.0m;
            _doLoop = true;
            GetSampleReader(itemToPlay, 1.0m, true);
            if (_reader == null)
            {
                OnStopped(new EventArgs());
                return;
            }

            _reader.Position = 0;

            var outFormat = new WaveFormat(21000, _reader.WaveFormat.Channels);
            _resampler = new MediaFoundationResampler(_reader, outFormat);

            
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                try
                {
                    WaveFileWriter.CreateWaveFile(fileName, _resampler);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Write of audio file failed:- " + e.Message);
                }
                finally
                {
                    CleanUp();
                    OnStopped(new EventArgs());
                    
                }
            }
            else
            {


                _reader.Position = 0;
                _resampler.Reposition();

                _player = new WaveOut();
                _player.Volume = 1.0f;
                if (_player == null)
                {
                    CleanUp();
                    OnStopped(new EventArgs());
                    return;
                }

                _player.PlaybackStopped += Player_PlaybackStopped;

                //reader = new WaveFileReader(converter);
                //player.Init(converter);
                _player.Init(_resampler);
                _player.Play();
            }
        }

        public void Play(PlayListItem itemToPlay, decimal speedFactor, bool playInLoop,string filename="")
        {
            CleanUp();
            _doLoop = playInLoop;
            currentItem = itemToPlay;
            currentSpeed = speedFactor;
            GetSampleReader(itemToPlay, speedFactor);
            if (_reader == null)
            {
                CleanUp();
                OnStopped(new EventArgs());
                return;
            }

            if (!string.IsNullOrWhiteSpace(filename))
            {
                _doLoop = false;
                bool quit = false;
                try
                {
                    WaveFileWriter.CreateWaveFile("filename.wav", _reader);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(("Write Play file faile:- " + ex.Message));
                }
                finally
                {
                    CleanUp();
                    OnStopped(new EventArgs());
                    quit = true;
                }

                if (quit) return;
            }

            _player = new WaveOut();
            if (_player == null)
            {
                CleanUp();
                OnStopped(new EventArgs());
                return;
            }

            _player.PlaybackStopped += Player_PlaybackStopped;
            _player.Init(_reader);
            _player.Play();
        }

        private void GetSampleReader(PlayListItem itemToPlay, decimal speedFactor, bool doHeterodyne = false)
        {
            if (_reader != null && _ms != null && _doLoop && !doHeterodyne)
            {
                _ms.Position = 0;
                return;
            }

            if (_player != null && _player.PlaybackState != PlaybackState.Stopped) return;
            if (itemToPlay == null) return;
            if (string.IsNullOrWhiteSpace(itemToPlay.filename)) return;
            if (!File.Exists(itemToPlay.filename) || (new FileInfo(itemToPlay.filename).Length<=0L)) return;


            using (var afr = new AudioFileReader(itemToPlay.filename))
            {
                if (afr != null)
                {
                    afr.Skip((int) itemToPlay.startOffset.TotalSeconds);


                    if (itemToPlay.playLength.TotalSeconds < 1)
                    {
                        if (itemToPlay.playLength.TotalMilliseconds > 0)
                            itemToPlay.playLength = new TimeSpan(0, 0, 1);
                        else
                            itemToPlay.playLength = afr.TotalTime - itemToPlay.startOffset;
                    }

                    var takenb = afr.Take(itemToPlay.playLength);
                    var bufferLength = 0;
                    if (doHeterodyne) bufferLength = takenb.WaveFormat.SampleRate;
                    var sineBuffer = new float[bufferLength];
                    sineBuffer = Fill(sineBuffer, Frequency * 1000);

                    if (takenb != null)
                    {
                        //afr.Dispose();
                        var wf = new WaveFormat((int) (takenb.WaveFormat.SampleRate * speedFactor),
                            takenb.WaveFormat.BitsPerSample, takenb.WaveFormat.Channels);
                        wf = WaveFormat.CreateIeeeFloatWaveFormat((int) (takenb.WaveFormat.SampleRate * speedFactor),
                            takenb.WaveFormat.Channels);
                        _ms = new MemoryStream();
                        if (wf != null && _ms != null)
                            using (var wfw = new WaveFileWriter(new IgnoreDisposeStream(_ms), wf))
                            {
                                if (wfw != null)
                                {
                                    //byte[] bytes = new byte[takenb.WaveFormat.AverageBytesPerSecond];
                                    var floats = new float[takenb.WaveFormat.SampleRate];
                                    var read = -1;
                                    var filter = BiQuadFilter.LowPassFilter(wfw.WaveFormat.SampleRate, 5000, 2.0f);
                                    // 6s @ 384ksps = 2,304,000 = 4,608,000 bytes
                                    while ((read = takenb.Read(floats, 0, floats.Length)) > 0)
                                    {
                                        if (doHeterodyne)
                                        {
                                            for (var i = 0; i < read; i++)
                                            {
                                                floats[i] = floats[i] * sineBuffer[i];

                                                floats[i] = filter.Transform(floats[i]);
                                            }


                                            wfw.WriteSamples(floats, 0, read);
                                        }
                                        else
                                        {
                                            wfw.WriteSamples(floats, 0, read);
                                        }


                                        wfw.Flush();
                                    }


                                    wfw.Flush();


                                    afr.Dispose();
                                }
                            }
                    }
                }
            }

            if (_ms != null)
            {
                _ms.Position = 0;
                //player = new WaveOut();
                //player.PlaybackStopped += Player_PlaybackStopped;
                //if (doHeterodyne)
                //{
                //    MyBiQuadFilter filter = new MyBiQuadFilter(ms);
                //    filter.setValues(5000);
                //    reader = filter;
                //}
                //else
                //{

                _reader = new WaveFileReader(_ms);
                //}
            }
        }

        /// <summary>
        ///     Fills an array of floats with sinewaves at the specified frequency
        /// </summary>
        /// <param name="sineBuffer"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private float[] Fill(float[] sineBuffer, decimal frequency)
        {
            for (var i = 0; i < sineBuffer.Length; i++)
                sineBuffer[i] = (float) Math.Sin(2.0d * Math.PI * i * (double) frequency / sineBuffer.Length);
            return sineBuffer;
        }

        /// <summary>
        ///     stops the player if it is playing
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            _doLoop = false;
            if (_player != null && _player.PlaybackState != PlaybackState.Stopped)
            {
                _player.Stop();
                return true;
            }

            return false;
        }


        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            CleanUp();
            if (_doLoop)
            {
                if (currentSpeed == 0.0m)
                    Heterodyne(currentItem);
                else
                    Play(currentItem, currentSpeed, _doLoop);
            }
            else
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }

                if (_player != null)
                {
                    _player.Dispose();
                    _player = null;
                }

                if (_ms != null)
                {
                    _ms.Dispose();
                    _ms = null;
                }

                OnStopped(new EventArgs());
            }
        }

        /// <summary>
        ///     Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler e_Stopped
        {
            add
            {
                lock (_stoppedEventLock)
                {
                    _stoppedEvent += value;
                }
            }
            remove
            {
                lock (_stoppedEventLock)
                {
                    _stoppedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_Stopped" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnStopped(EventArgs e)
        {
            EventHandler handler = null;

            lock (_stoppedEventLock)
            {
                handler = _stoppedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }
    }

    public class MyBiQuadFilter : WaveFileReader
    {
        private int _channels;

        private int _cutOffFreq;
        private string _fileName = "";
        private BiQuadFilter[] _filters;
        private Stream _inputStream;


        /// <summary>
        ///     An implementation of a BiQuad low pass filter
        /// </summary>
        public MyBiQuadFilter(Stream inputStream) : base(inputStream)
        {
            _inputStream = inputStream;
        }

        public MyBiQuadFilter(string fileName) : base(fileName)
        {
            _fileName = fileName;
        }

        public void SetValues(int cutOffFreq)
        {
            _cutOffFreq = cutOffFreq;

            filter_LowPass();
        }

        private void filter_LowPass()
        {
            _channels = base.WaveFormat.Channels;
            _filters = new BiQuadFilter[_channels];

            for (var n = 0; n < _channels; n++)
                if (_filters[n] == null)
                    _filters[n] = BiQuadFilter.LowPassFilter(WaveFormat.SampleRate, _cutOffFreq, 1);
                else
                    _filters[n].SetLowPassFilter(WaveFormat.SampleRate, _cutOffFreq, 1);
        }


        public int Read(float[] buffer, int offset, int count)
        {
            var sampleProvider = this.ToSampleProvider();
            var samplesRead = sampleProvider.Read(buffer, offset, count);

            for (var i = 0; i < samplesRead; i++)
                buffer[offset + i] = _filters[i % _channels].Transform(buffer[offset + i]);

            return samplesRead;
        }
    }
}