using Microsoft.VisualStudio.Language.Intellisense;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private StoredImage generateSpectrogram(LabelledSegment seg)
        {
            if (seg.StartOffset.TotalMilliseconds == 0 && seg.EndOffset == seg.StartOffset) return (null);
            StoredImage si = DBAccess.GetSpectrogramForSegment(seg);
            if (si == null)
            {
                int FFTOrder = 10;
                List<float> data = GetData(seg, FFTOrder, out int sampleRate);
                Debug.WriteLine($"gen spectrogram-> data {data.Count} lasting {data.Count / (double)sampleRate}s");
                List<Spectrum> spectra = DeepAnalysis.GetSpectrum(data, sampleRate, FFTOrder, 0.5f, out int FFTAdvance, out double advanceMS, out double[] FFTQuiet);
                var bmp = DeepAnalysis.GetBmpFromSpectra(spectra, FFTOrder, 0.5f, sampleRate);
                si = new StoredImage(Tools.ToBitmapSource(bmp),
                    $"{seg.Recording.RecordingName} {seg.StartOffset.TotalSeconds} - {seg.EndOffset.TotalSeconds}",
                    $"FFTSize=1024\n{seg.Comment}",
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
    }
}