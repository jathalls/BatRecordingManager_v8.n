using Microsoft.VisualStudio.Language.Intellisense;
using NAudio.Dsp;
using NAudio.Wave;
using Spectrogram;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalToolkit;

namespace BatRecordingManager
{
    internal class SegmentSonagrams
    {
        public void GenerateForSegments(List<LabelledSegment> segmentList)
        {
            if (segmentList != null && segmentList.Any())
            {
                foreach (var seg in segmentList)
                {
                    if (seg.BatSegmentLinks?.Any() ?? false)
                    {
                        BulkObservableCollection<StoredImage> imageList = new BulkObservableCollection<StoredImage>();
                        StoredImage spectrogram = generateSpectrogram(seg);
                        if (spectrogram != null)
                        {
                            imageList.Add(spectrogram);
                            DBAccess.UpdateSegmentImages(imageList, seg);
                        }
                    }
                }
            }
        }

        internal StoredImage GenerateForSegment(LabelledSegment sel, Parametrization param = null)
        {
            return (generateSpectrogram(sel, param));
        }

        internal void GenerateForSession(RecordingSession selectedSession)
        {
            List<LabelledSegment> segments = DBAccess.GetSessionSegments(selectedSession);
            GenerateForSegments(segments);
        }

        /// <summary>
        /// Generates a spectrogram of the given LabelledSegment.  If the segment already has an image
        /// of type SPCT then that is retrieved and the image of it is modified to the new spectrogram
        /// </summary>
        /// <param name="seg"></param>
        /// <returns></returns>
        private StoredImage generateSpectrogram(LabelledSegment seg, Parametrization param = null)
        {
            if (seg == null || (seg.StartOffset.TotalMilliseconds == 0 && seg.EndOffset == seg.StartOffset)) return (null);
            if (string.IsNullOrWhiteSpace(seg.Comment) || seg.Comment.Contains("No Bats")) return (null);
            if (seg.BatSegmentLinks == null || seg.BatSegmentLinks.Count <= 0) return (null);
            StoredImage si = DBAccess.GetSpectrogramForSegment(seg);
            if (si == null)
            {
                //int FFTOrder = 10;
                // List<float> data = GetData(seg, FFTOrder, out int sampleRate);

                var settings = Settings.getSettings();

                var data = GetDataSG(seg, settings.Spectrogram.FFTSize, out int sampleRate, (double)settings.Spectrogram.scale);
                if (data.audio == null) return (null);

                Debug.WriteLine($"gen spectrogram-> data {data.audio.Count()} lasting {data.audio.Count() / (double)sampleRate}s");
                int maxFrequency = sampleRate / 2;
                if (settings.Spectrogram.maxFrequency > 0)
                {
                    maxFrequency = settings.Spectrogram.maxFrequency;
                }
                System.Drawing.Bitmap bmp = null;

                var sg = new SpectrogramGenerator(sampleRate,
                    fftSize: settings.Spectrogram.FFTSize,
                    stepSize: settings.Spectrogram.FFTAdvance,
                    maxFreq: maxFrequency);
                sg.Add(data.audio);
                sg.SetColormap(Colormap.GrayscaleReversed);
                //sg.SaveImage(@"C:\BRMTestData\Test.png", dB: true, dBScale: 20, intensity: 5);
                Debug.WriteLine($"Scale={settings.Spectrogram.scale}");
                bmp = sg.GetBitmap(dB: true, dBScale: settings.Spectrogram.dBScale, intensity: settings.Spectrogram.intensity);

                bmp = DeepAnalysis.decorateBitmap(bmp, settings.Spectrogram.FFTSize, settings.Spectrogram.FFTAdvance, sampleRate, param);
                //List<Spectrum> spectra = DeepAnalysis.GetSpectrum(data, sampleRate, FFTOrder, 0.5f, out int FFTAdvance, out double advanceMS, out double[] FFTQuiet);
                //var bmp = DeepAnalysis.GetBmpFromSpectra(spectra, FFTOrder, 0.5f, sampleRate);
                si = new StoredImage(Tools.ToBitmapSource(bmp),
                    $"{seg.Recording.RecordingName} {seg.StartOffset.TotalSeconds} - {seg.EndOffset.TotalSeconds}",
                    $"FFTSize/Advance={settings.Spectrogram.FFTSize}/{settings.Spectrogram.FFTAdvance}\n{seg.Comment}",
                    -1, false, Tools.BlobType.SPCT);
            }
            return (si);
        }

        private List<float> GetData(LabelledSegment segment, int FFTOrder, out int sampleRate)
        {
            List<float> result = new List<float>();
            sampleRate = 384000;
            int FFTSize = (int)Math.Pow(2, FFTOrder);

            string file = segment.Recording.GetFileName();
            //string file = Path.Combine(sel.Recording.RecordingSession.OriginalFilePath,sel.Recording.RecordingName);
            if (!File.Exists(file)) return result;

            using (var wfr = new WaveFileReader(file))
            {
                var sp = wfr.ToSampleProvider();
                sampleRate = wfr.WaveFormat.SampleRate;
                TimeSpan leadin = new TimeSpan();
                var requestedDuration = segment.EndOffset - segment.StartOffset;
                if (requestedDuration.TotalSeconds > 15)
                {
                    leadin = TimeSpan.FromSeconds(((requestedDuration.TotalSeconds - 15.0d) / 2.0d));
                    requestedDuration = TimeSpan.FromSeconds(15.0d);
                }
                var data = sp.Skip(segment.StartOffset + leadin).Take(requestedDuration);
                float[] faData = new float[FFTSize];
                result = new List<float>();
                int samplesRead;
                while ((samplesRead = data.Read(faData, 0, FFTSize)) > 0)
                {
                    result.AddRange(faData.Take(samplesRead));
                }

                var filter = BiQuadFilter.HighPassFilter(sampleRate, 15000, 1);
                for (int i = 0; i < result.Count; i++)
                {
                    result[i] = filter.Transform(result[i]);
                }
            }
            return (result);
        }

        private (double[] audio, int sampleRate) GetDataSG(LabelledSegment segment, int FFTSize, out int sampleRate, double scale = 16000.0d)
        {
            List<double> result;

            sampleRate = 384000;
            //int FFTSize = (int)Math.Pow(2, FFTOrder);

            string file = segment.Recording.GetFileName();
            //string file = Path.Combine(sel.Recording.RecordingSession.OriginalFilePath,sel.Recording.RecordingName);
            if (!File.Exists(file)) return (null, sampleRate);
            var audio = new double[1];
            using (var wfr = new AudioFileReader(file))
            {
                var sp = wfr.ToSampleProvider();
                sampleRate = wfr.WaveFormat.SampleRate;
                TimeSpan leadin = new TimeSpan();
                var requestedDuration = segment.EndOffset - segment.StartOffset;
                if (requestedDuration.TotalSeconds > 15)
                {
                    leadin = TimeSpan.FromSeconds(((requestedDuration.TotalSeconds - 15.0d) / 2.0d));
                    requestedDuration = TimeSpan.FromSeconds(15.0d);
                }
                var data = sp.Skip(segment.StartOffset + leadin).Take(requestedDuration);
                var sampleCount = (int)(requestedDuration.TotalSeconds * sampleRate);
                float[] faData = new float[FFTSize];
                result = new List<double>();
                int samplesRead;
                //double scale = 16_000.0d;

                while ((samplesRead = data.Read(faData, 0, FFTSize)) > 0)
                {
                    result.AddRange(faData.Take(samplesRead).Select(x => x * scale));
                }
                /*
                var filter = BiQuadFilter.HighPassFilter(sampleRate, 15000, 1);
                audio = new double[result.Count];
                var maxVal = result.Max();
                var scale = 1.0f / maxVal;
                for (int i = 0; i < result.Count; i++)
                {
                    result[i] = result[i] * scale * 1000.0f;
                    audio[i] = (double)filter.Transform(result[i]);
                }*/
            }
            return (result.ToArray(), sampleRate);
        }
    }
}