using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Collection of static functions for accessing the PTA database and doing direct manipulations on its contents.
    /// </summary>
    public static class PTA_DBAccess
    {
#if DEBUG
        public static string databaseName = @"C:\Echolocation\PTADebugDatabase.mdf";
        //public static string databaseName = @"C:\Echolocation\PTADatabase.mdf";
#else
        public static string databaseName = @"C:\Echolocation\PTADatabase.mdf";
#endif

        public static void ForEach<T>(
            this IEnumerable<T> source, Action<T> func)
        {
            if (source != null)
            {
                foreach (var item in source)
                {
                    func(item);
                }
            }
        }

        public static void InitialiseDatabase()
        {
            if (!File.Exists(databaseName))
            {
                File.Copy($@".\PulseTrainAnalysisDB.mdf", databaseName);
                var dc = new PulseTrainAnalysisDBMLDataContext($@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={databaseName};Integrated Security=True");
                //string script = $@"DROP DATABASE IF EXISTS '{databaseName}'";
                //dc.ExecuteCommand(script);

                dc.DeleteDatabase();
                //dc.SubmitChanges();

                dc.CreateDatabase();



            }
        }

        internal static PulseTrainAnalysisDBMLDataContext getDataContext()
        {
            return (new PulseTrainAnalysisDBMLDataContext($@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={databaseName};Integrated Security=True"));
        }

        /// <summary>
        /// Given a bparecording, inserts or updates the recording into the database
        /// recursing through subsidiary elements as necessary
        /// </summary>
        /// <param name="srcRecording"></param>
        internal static void SaveRecording(bpaRecording srcRecording)
        {
            var dc = getDataContext();

            if (srcRecording != null && !string.IsNullOrWhiteSpace(srcRecording.FQfilename))
            {
                PTARecording rec = GetRecording(srcRecording.FQfilename, dc);
                if (rec != null)
                {
                    UpdateRecording(rec, srcRecording, dc);
                }
                else
                {
                    InsertRecording(srcRecording, dc);
                }


            }
        }

        /// <summary>
        /// Returns an instance of the database record for the recording filename or null if there is
        /// no such entry
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dc"></param>
        private static PTARecording GetRecording(string filename, PulseTrainAnalysisDBMLDataContext dc = null)
        {
            if (dc == null) dc = getDataContext();
            string bareName = Path.GetFileName(filename);
            string path = Path.GetDirectoryName(filename);
            var result = (from rec in dc.PTARecordings
                          where rec.FileName == bareName && rec.FilePath == path
                          select rec)?.FirstOrDefault();
            if (result == null || result.PTARecordingID < 1)
            {
                return (null);
            }
            return (result);
        }

        /// <summary>
        /// Updates an existing database recording from the source recording, where the
        /// PTARecording has been retrieved using the supplied dataContext
        /// if you have no dataContext use SaveRecording instead.
        /// </summary>
        /// <param name="destRecording"></param>
        /// <param name="srcRecording"></param>
        /// <param name="dc"></param>
        private static void UpdateRecording(PTARecording destRecording, bpaRecording srcRecording, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) return;
            if (srcRecording == null || destRecording == null) return;
            destRecording.FileName = Path.GetFileName(srcRecording.FQfilename);
            destRecording.FilePath = Path.GetDirectoryName(srcRecording.FQfilename);
            destRecording.RecordingNumber = srcRecording.recNumber;
            destRecording.SampleRate = srcRecording.SampleRate;
            dc.SubmitChanges();
            foreach (var seg in srcRecording.getSegmentList())
            {
                SaveSegment(srcRecording.FQfilename, seg, dc, destRecording.PTARecordingID);
            }
        }

        private static void InsertRecording(bpaRecording srcRecording, PulseTrainAnalysisDBMLDataContext dc = null)
        {
            if (dc == null) dc = getDataContext();
            if (srcRecording == null) return;
            PTARecording destRecording = new PTARecording();
            destRecording.FileName = Path.GetFileName(srcRecording.FQfilename);
            destRecording.FilePath = Path.GetDirectoryName(srcRecording.FQfilename);
            destRecording.RecordingNumber = srcRecording.recNumber;
            destRecording.SampleRate = srcRecording.SampleRate;
            dc.PTARecordings.InsertOnSubmit(destRecording);
            dc.SubmitChanges();
            foreach (var seg in srcRecording.getSegmentList())
            {
                SaveSegment(srcRecording.FQfilename, seg, dc, destRecording.PTARecordingID);
            }
        }


        /// <summary>
        /// Inserts or updates the given segment as appropriate.  The segment is linked to the recording identified by the 
        /// recordingID or by the filename if the ID is not given.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="srcSegment"></param>
        /// <param name="dc"></param>
        /// <param name="ptaRecordingID"></param>
        public static void SaveSegment(string filename, bpaSegment srcSegment, PulseTrainAnalysisDBMLDataContext dc = null, int ptaRecordingID = -1)
        {
            if (dc == null) dc = getDataContext();
            if (ptaRecordingID < 0)
            {
                var rec = GetRecording(filename, dc);
                if (rec == null) return;
                ptaRecordingID = rec.PTARecordingID;
            }
            var destSegment = GetSegment(ptaRecordingID, srcSegment.No);
            if (destSegment == null)
            {
                InsertSegment(srcSegment, ptaRecordingID, dc);
            }
            else
            {
                UpdateSegment(srcSegment, destSegment, dc);
            }



        }

        /// <summary>
        /// Updates an existing segment in the database
        /// </summary>
        /// <param name="srcSegment"></param>
        /// <param name="destSegment"></param>
        /// <param name="dc"></param>
        private static void UpdateSegment(bpaSegment srcSegment, PTASegment destSegment, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) dc = getDataContext();
            if (destSegment == null || srcSegment == null) return;
            destSegment.Comment = srcSegment.Comment;
            destSegment.Duration = srcSegment.GetDuration();
            destSegment.SegmentNumber = (short)srcSegment.No;
            destSegment.StartTimeInRec = srcSegment.GetOffsetInRecording();
            dc.SubmitChanges();
            foreach (var pass in srcSegment.getPassList())
            {
                SavePass(pass, dc, destSegment.PTASegmentID);
            }
        }

        /// <summary>
        /// Inserts a new segment in the database linked to the recording with the specified ID
        /// </summary>
        /// <param name="srcSegment"></param>
        /// <param name="ptaRecordingID"></param>
        /// <param name="dc"></param>
        private static void InsertSegment(bpaSegment srcSegment, int ptaRecordingID, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) dc = getDataContext();
            var destSegment = new PTASegment();
            destSegment.Recording = ptaRecordingID;
            if (destSegment == null || srcSegment == null) return;
            destSegment.Comment = srcSegment.Comment;
            destSegment.Duration = srcSegment.GetDuration();
            destSegment.SegmentNumber = (short)srcSegment.No;
            destSegment.StartTimeInRec = srcSegment.GetOffsetInRecording();
            dc.PTASegments.InsertOnSubmit(destSegment);
            dc.SubmitChanges();
            foreach (var pass in srcSegment.getPassList())
            {
                SavePass(pass, dc, destSegment.PTASegmentID);
            }
        }

        private static PTASegment GetSegment(int recID, int segNumber, PulseTrainAnalysisDBMLDataContext dc = null)
        {
            if (dc == null) dc = getDataContext();

            var result = (from seg in dc.PTASegments
                          where seg.Recording == recID && seg.SegmentNumber == segNumber
                          select seg)?.FirstOrDefault();
            if (result == null || result.PTASegmentID < 1)
            {
                return (null);
            }
            return (result);

        }

        private static PTAPass GetPass(int PassNumber, int SegmentID, PulseTrainAnalysisDBMLDataContext dc = null)
        {
            if (dc == null) dc = getDataContext();
            var destPass = (from pass in dc.PTAPasses
                            where pass.PTASegment.PTASegmentID == SegmentID && pass.PassNumber == PassNumber
                            select pass)?.FirstOrDefault();
            if (destPass == null || destPass.PTAPassID < 1)
            {
                return (null);
            }
            else
            {
                return (destPass);
            }

        }

        public static void SavePass(bpaPass srcPass, PulseTrainAnalysisDBMLDataContext dc, int SegmentID)
        {
            if (dc == null || SegmentID < 1) return;
            var destPass = GetPass(srcPass.Pass_Number, SegmentID);
            if (destPass == null || destPass.PTAPassID < 1)
            {
                destPass = InsertPass(srcPass, SegmentID, dc);
            }
            else
            {
                UpdatePass(srcPass, destPass, dc);
            }
            var pulseList = srcPass.getPulseList();
            if (pulseList != null)
            {
                foreach (var pulse in pulseList)
                {
                    SavePulse(pulse, destPass.PTAPassID, dc);
                }

            }
        }

        private static void UpdatePass(bpaPass srcPass, PTAPass destPass, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) return;
            CopyPassBPA2PTA(srcPass, ref destPass);
            dc.SubmitChanges();
        }

        private static PTAPass InsertPass(bpaPass srcPass, int SegmentID, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) dc = getDataContext();
            var destPass = new PTAPass();
            destPass.Segment = SegmentID;
            CopyPassBPA2PTA(srcPass, ref destPass);
            dc.PTAPasses.InsertOnSubmit(destPass);
            dc.SubmitChanges();
            return (destPass);
        }

        private static void CopyPassBPA2PTA(bpaPass srcPass, ref PTAPass destPass)
        {
            destPass.OffsetInSegmentInSamples = srcPass.getOffsetInSegmentInSamples();
            destPass.PassLengthInSamples = srcPass.getPassLengthInSamples();
            destPass.PassNumber = (short)srcPass.Pass_Number;
            destPass.EnvelopeThresholdFactor = (float)srcPass.thresholdFactor;
            destPass.SpectrumThresholdFactor = (float)srcPass.spectrumfactor;


        }

        /// <summary>
        /// Saves a pulse and all related information to the database, inserting or updating as appropriate
        /// </summary>
        /// <param name="srcPulse"></param>
        /// <param name="PassID"></param>
        /// <param name="dc"></param>
        public static void SavePulse(Pulse srcPulse, int PassID, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) dc = getDataContext();
            var destPulse = GetPulse(srcPulse, PassID, dc);
            if (destPulse == null)
            {
                InsertPulse(srcPulse, PassID, dc);

            }
            else
            {
                UpdatePulse(srcPulse, ref destPulse, dc);
            }

        }

        private static PTAPulse GetPulse(Pulse srcPulse, int PassID, PulseTrainAnalysisDBMLDataContext dc)
        {
            var result = (from dbPulse in dc.PTAPulses
                          where dbPulse.Pass == PassID && dbPulse.PulseNumber == srcPulse.Pulse_Number
                          select dbPulse)?.FirstOrDefault();
            if (result == null || result.PTAPulseID < 1)
            {
                return (null);
            }
            else
            {
                return (result);
            }
        }

        private static void InsertPulse(Pulse srcPulse, int PassID, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) dc = getDataContext();
            var destPulse = new PTAPulse();
            destPulse.Pass = PassID;
            CopyPulseBPA2PTA(srcPulse, ref destPulse);
            dc.PTAPulses.InsertOnSubmit(destPulse);
            dc.SubmitChanges();
        }

        private static void UpdatePulse(Pulse srcPulse, ref PTAPulse destPulse, PulseTrainAnalysisDBMLDataContext dc)
        {
            if (dc == null) return;
            CopyPulseBPA2PTA(srcPulse, ref destPulse);
            dc.SubmitChanges();
        }

        private static void CopyPulseBPA2PTA(Pulse srcPulse, ref PTAPulse destPulse)
        {
            destPulse.AbsoluteThreshold = srcPulse.getPeak().AbsoluteThreshold;
            var spectrumdetails = srcPulse?.GetSpectrumDetails();
            if (spectrumdetails != null && spectrumdetails.spectralPeakList != null && spectrumdetails.spectralPeakList.Any())
            {
                var spectralPeak = spectrumdetails.spectralPeakList?.First() as SpectralPeak;
                if (spectralPeak != null)
                {
                    destPulse.AutoCorrelationWidth = spectralPeak?.AutoCorrelationWidth;
                    destPulse.HalfHeightHighFrequency = spectralPeak.halfHeightHighFrequency;
                    destPulse.HalfHeightLowFrequency = spectralPeak.halfHeightLowFrequency;
                    destPulse.HalfHeightWidth = spectralPeak.halfHeightWidthHz;
                    destPulse.HighFrequency = spectralPeak.highFrequency;
                    destPulse.LowFrequency = spectralPeak.lowFrequency;
                    destPulse.MaxVal = spectralPeak.GetMaxVal();
                    destPulse.PeakFrequency = spectralPeak.peakFrequency;
                    destPulse.PrevIntervalSamples = srcPulse.getPeak().GetPrevIntervalSamples();

                }
                destPulse.FFTSize = spectrumdetails.getSpectrum().getFFTSize();
            }
            destPulse.DurationInSamples = srcPulse.getPeak()?.getPeakWidthSamples() ?? 0;
            destPulse.OffsetInPassInSamples = srcPulse.getPeak().getStartAsSampleInPass();
            destPulse.PeakArea = srcPulse.getPeak().GetPeakArea();
            destPulse.PulseNumber = (short)srcPulse.Pulse_Number;
        }

        public static bool RecordingExists(string FQFileName)
        {
            PulseTrainAnalysisDBMLDataContext dc = getDataContext();
            bool result = false;
            string recPath = Path.GetDirectoryName(FQFileName);
            string recFile = Path.GetFileName(FQFileName);
            var recordings = from rec in dc.PTARecordings
                             where rec.FilePath == recPath && rec.FileName == recFile
                             select rec;
            if (recordings != null && recordings.Any())
            {
                result = true;
            }
            return (result);
        }

        internal static bpaRecording getBPARecordingAndDescendants(string FQFileName)
        {
            PulseTrainAnalysisDBMLDataContext dc = getDataContext();
            string recPath = Path.GetDirectoryName(FQFileName);
            string recFile = Path.GetFileName(FQFileName);

            PTARecording srcRecording = (from rec in dc.PTARecordings
                                         where rec.FilePath == recPath && rec.FileName == recFile
                                         select rec)?.FirstOrDefault();
            if (srcRecording == null || srcRecording.PTARecordingID < 1) return (null); // didn't find a valid record

            bpaRecording destRecording = CopyRecordingPTA2BPA(srcRecording);

            foreach (var srcSegment in srcRecording.PTASegments)
            {
                bpaSegment destSegment = CopySegmentPTA2BPA(srcSegment);
                destRecording.AddSegment(destSegment);
            }

            return (destRecording);
        }


        /// <summary>
        /// Copies all the relevant segment data from the database segment record
        /// to the bpaSegment that is returned, and also populates the Pass list
        /// from the database Passes records
        /// </summary>
        /// <param name="srcSegment"></param>
        /// <returns></returns>
        private static bpaSegment CopySegmentPTA2BPA(PTASegment srcSegment)
        {
            bpaSegment destSegment = new bpaSegment(srcSegment.PTARecording.RecordingNumber,
                srcSegment.SegmentNumber,
                (int)(srcSegment.StartTimeInRec.TotalSeconds * srcSegment.PTARecording.SampleRate),
                new DataAccessBlock(Path.Combine(srcSegment.PTARecording.FilePath, srcSegment.PTARecording.FileName),
                                    (long)(srcSegment.StartTimeInRec.TotalSeconds * srcSegment.PTARecording.SampleRate),
                                    (long)(srcSegment.Duration.TotalSeconds * srcSegment.PTARecording.SampleRate)
                                    ),
                srcSegment.PTARecording.SampleRate ?? 384000,
                srcSegment.Comment);

            foreach (var srcPass in srcSegment.PTAPasses)
            {
                bpaPass destPass = CopyPassPTA2BPA(srcPass);
                destSegment.AddPass(destPass);
            }

            return (destSegment);
        }

        /// <summary>
        /// Copies information from the database Pass record into a new bpaPass class
        /// and returns it.  Also populates the PulseList in the generated Pass from the database
        /// information in PTAPulses
        /// </summary>
        /// <param name="srcPass"></param>
        /// <returns></returns>
        private static bpaPass CopyPassPTA2BPA(PTAPass srcPass)
        {
            DataAccessBlock passDab = new DataAccessBlock(Path.Combine(srcPass.PTASegment.PTARecording.FilePath, srcPass.PTASegment.PTARecording.FileName),
                                                            srcPass.OffsetInSegmentInSamples + (long)(srcPass.PTASegment.StartTimeInRec.TotalSeconds * srcPass.PTASegment.PTARecording.SampleRate),
                                                            srcPass.PassLengthInSamples
                                                            );
            if (passDab.Length <= 0)
            {
                Debug.WriteLine("Created invalid DAB");
            }
            bpaPass destPass = new bpaPass(srcPass.PTASegment.PTARecording.RecordingNumber,
                                        srcPass.PTASegment.SegmentNumber,
                                        srcPass.PassNumber,
                                        srcPass.OffsetInSegmentInSamples,
                                        passDab,
                                        srcPass.PTASegment.PTARecording.SampleRate ?? 384000,
                                        srcPass.PTASegment.Comment,
                                        (float)(srcPass.PTASegment.Duration.TotalSeconds),
                                        srcPass.PTASegment.StartTimeInRec
                                        );
            destPass.thresholdFactor = (decimal)(srcPass.EnvelopeThresholdFactor ?? 1.5f);
            destPass.spectrumfactor = (decimal)(srcPass.SpectrumThresholdFactor ?? 1.5f);


            //Debug.WriteLine($"Pass {destPass.Pass_Number} at {destPass.getOffsetInSegmentInSamples()} in segment {destPass.segmentNumber}");
            foreach (var srcPulse in srcPass.PTAPulses)
            {
                Peak peak = new Peak(peakNumber: srcPulse.PulseNumber,
                                    rate: srcPulse.PTAPass.PTASegment.PTARecording.SampleRate ?? 384000,
                                    startOfPassInSegment: srcPulse.PTAPass.OffsetInSegmentInSamples,
                                    startOfPeakInPass: srcPulse.OffsetInPassInSamples,
                                    peakWidth: srcPulse.DurationInSamples,
                                    peakArea: srcPulse.PeakArea ?? 0.0d,
                                    peakMaxHeight: (float)(srcPulse.MaxVal ?? 0.0d),
                                    interval: srcPulse.PrevIntervalSamples ?? 0,
                                    RecordingNumber: srcPulse.PTAPass.PTASegment.PTARecording.RecordingNumber,
                                    AbsoluteThreshold: (float)srcPulse.AbsoluteThreshold);
                SpectrumDetails destSpectrumDetails = CopySpectrumDetailsPTA2BPA(srcPulse, peak);
                Pulse destPulse = CopyPulsePTA2BPA(srcPulse, passDab, destSpectrumDetails, peak, destPass.spectrumfactor);


                destPulse.spectralDetails = destSpectrumDetails;
                destPass.AddPulse(destPulse);
            }
            destPass.CalculateMeanInterval();
            return (destPass);
        }

        private static SpectrumDetails CopySpectrumDetailsPTA2BPA(PTAPulse srcPulse, Peak peak)
        {
            Spectrum destSpectrum = new Spectrum(srcPulse.PTAPass.PTASegment.PTARecording.SampleRate ?? 384000,
                                                srcPulse.FFTSize ?? 1024,
                                                srcPulse.PulseNumber);
            SpectrumDetails destSpectrumDetails = new SpectrumDetails(destSpectrum);
            destSpectrumDetails.pfMeanOfPeakFrequenciesInSpectralPeaksList = (float)(srcPulse.PeakFrequency ?? 0.0f);
            destSpectrumDetails.pfStart = (float)(srcPulse.HighFrequency ?? 0.0f);
            destSpectrumDetails.pfEnd = (float)(srcPulse.LowFrequency ?? 0.0f);
            SpectralPeak spectralPeak = CopySpectralPeakPTA2BPA(srcPulse, peak);
            destSpectrumDetails.AddSpectralPeak(spectralPeak);
            return (destSpectrumDetails);
        }

        private static SpectralPeak CopySpectralPeakPTA2BPA(PTAPulse srcPulse, Peak peak)
        {
            var sampleRate = srcPulse.PTAPass.PTASegment.PTARecording.SampleRate;
            var fftSize = srcPulse.FFTSize;
            var HzPerSample = (sampleRate ?? 384000) / (fftSize ?? 1024);
            var result = SpectralPeak.Create(1,// int
                peakStart: (srcPulse.LowFrequency / HzPerSample) ?? 0,// int
                peakCount: 1,//int
                peakArea: srcPulse.PeakArea ?? 0.0d, // int
                maxHeight: (float)(srcPulse.MaxVal ?? 0.0d),// float
                interval: srcPulse.PrevIntervalSamples ?? 0,// int
                sampleRate: sampleRate ?? 384000,// int
                autoCorrelationWidth: (float)(srcPulse.AutoCorrelationWidth ?? 0.0d),// float
                parentPeak: peak,// Peak
                isValidPulse: true, // default value for all retrieved data, only reset by a reCalc
                startOffset: srcPulse.PTAPass.PTASegment.StartTimeInRec.TotalSeconds,// double - start of segment in secs
                                                                                     //ref data: null,// float[]=null
                HzPerSample: HzPerSample,// int=1
                PassNumber: srcPulse.PTAPass.PassNumber,// int =1
                RecordingNumber: srcPulse.PTAPass.PTASegment.PTARecording.RecordingNumber,//int =1
                AbsoluteThreshold: 0.0f// float = 0.0f
                );
            result.peakFrequency = srcPulse.PeakFrequency ?? 0;
            result.highFrequency = srcPulse.HighFrequency ?? 0;
            result.lowFrequency = srcPulse.LowFrequency ?? 0;
            result.halfHeightHighFrequency = srcPulse.HalfHeightHighFrequency ?? 0;
            result.halfHeightLowFrequency = srcPulse.HalfHeightLowFrequency ?? 0;
            result.halfHeightWidthHz = srcPulse.HalfHeightWidth ?? 0;
            return (result);
        }

        /// <summary>
        /// Copies data from a database Pulse record into a BPA Pulse record.  Does not restore the SpectrumDetails
        /// which should be handled separately
        /// </summary>
        /// <param name="srcPulse"></param>
        /// <returns></returns>
        private static Pulse CopyPulsePTA2BPA(PTAPulse srcPulse, DataAccessBlock PassDab, SpectrumDetails spectralDetails, Peak peak, decimal SpectrumThresholdFactor)
        {
            Pulse destPulse;
            //DataAccessBlock PassDab = new DataAccessBlock(FQFileName,
            //                            (long)srcPulse.PTAPass.OffsetInSegmentInSamples,
            //                            (long)srcPulse.PTAPass.PassLengthInSamples,
            //                            (long)(srcPulse.PTAPass.PTASegment.Duration.TotalSeconds * (double)(srcPulse.PTAPass.PTASegment.PTARecording.SampleRate ?? 384000)));

            //Debug.WriteLine($"    Pulse {srcPulse.PulseNumber} at {peak.getStartAsSampleInPass()} in pass, {peak.GetStartAsSampleInSeg()}");
            try
            {
                destPulse = new Pulse(
                            PassDab,
                            srcPulse.PTAPass.OffsetInSegmentInSamples,
                            peak,
                            srcPulse.PTAPass.PassNumber,
                            srcPulse.QuietStart ?? 0,
                            SpectrumThresholdFactor,
                            spectralDetails
                            );
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return (destPulse);
        }

        /// <summary>
        /// copies the stored database recording data in a new bpaRecording, but does not
        /// populate the segmentlist
        /// </summary>
        /// <param name="srcRecording"></param>
        /// <returns></returns>
        private static bpaRecording CopyRecordingPTA2BPA(PTARecording srcRecording)
        {
            bpaRecording destRecording = new bpaRecording(srcRecording.RecordingNumber, Path.Combine(srcRecording.FilePath, srcRecording.FileName));
            destRecording.SampleRate = srcRecording.SampleRate ?? -1;
            return (destRecording);

        }
    }


}
