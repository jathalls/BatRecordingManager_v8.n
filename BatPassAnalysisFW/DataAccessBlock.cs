using NAudio.Wave;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Class to hold an AudioFileReader, a starting position in the file and a block length in samples
    /// </summary>
    public class DataAccessBlock
    {
        //public AudioFileReader audioFileReader { get; set; }

        /// <summary>
        /// Creates a new data access block for a specified file, a specified start point within the file and a specified length
        /// </summary>
        /// <param name="FQfilename"></param>
        /// <param name="StartPosInFileInSamples"></param>
        /// <param name="length"></param>
        /// <param name="caller"></param>
        /// <param name="linenumber"></param>
        public DataAccessBlock(string FQfilename, long StartPosInFileInSamples, long length, [CallerMemberName] string caller = null, [CallerLineNumber] int linenumber = 0)
        {
            if (length < 0)
            {
                Debug.WriteLine($"DAB Creation ERROR:- from {caller} at line {linenumber}");
            }
            FQfileName = FQfilename;
            BlockStartInFileInSamples = StartPosInFileInSamples;
            this.Length = length;
        }

        /// <summary>
        /// start location in the file of the data block
        /// </summary>
        public long BlockStartInFileInSamples { get; set; }

        /// <summary>
        /// Fully qualified file name containing the data
        /// </summary>
        public string FQfileName { get; set; }

        /// <summary>
        /// size of the datablock
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Returns data read from the file from the block's startpoint for the block's length
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="linenumber"></param>
        /// <returns></returns>
        public float[] getData([CallerMemberName] string caller = null, [CallerLineNumber] int linenumber = 0)
        {
            if (Length < 0)
            {
                Debug.WriteLine($"DAB getData ERROR:- from {caller} at line {linenumber}");
                throw new System.OverflowException();
            }
            float[] data = new float[Length];
            using (AudioFileReader audioFileReader = new AudioFileReader(FQfileName))
            {
                audioFileReader.Position = BlockStartInFileInSamples * 4; // to convert the start location in floats to location in bytes
                audioFileReader.Read(data, 0, data.Length);
            }

            //using (WaveFileReader wfr = new WaveFileReader(FQfileName))
            //{
            //    wfr.ToSampleProvider().Read(data, 0, data.Length);
            //}
            return (data);
        }

        /// <summary>
        /// Returns data read from the file from the specified start point of the specified length
        /// </summary>
        /// <param name="StartPosInFRecordingInSamples"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public float[] getData(int StartPosInFRecordingInSamples, int Length)
        {
            float[] data2 = new float[1];
            float[] data = new float[Length];
            using (AudioFileReader audioFileReader = new AudioFileReader(FQfileName))
            {
                audioFileReader.Position = BlockStartInFileInSamples * 4; // to convert the start location in floats to location in bytes
                audioFileReader.Read(data, 0, Length);
            }

            using (WaveFileReader wfr = new WaveFileReader(FQfileName))
            {
                var sp = wfr.ToSampleProvider();
                if (wfr.Length / 2 < Length) Length = (int)(wfr.Length / 2);
                data2 = new float[Length];
                sp.Read(data2, StartPosInFRecordingInSamples, Length);
            }
            return (data2);
        }
    }
}