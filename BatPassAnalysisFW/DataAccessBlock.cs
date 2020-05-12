using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Class to hold an AudioFileReader, a starting position in the file and a block length in samples
    /// </summary>
    public class DataAccessBlock
    {
        //public AudioFileReader audioFileReader { get; set; }
        public string fileName { get; set; }

        public long startLocation { get; set; }

        public long length { get; set; }

        public long segLength { get; set; }

        public DataAccessBlock(string filename, long start, long length,long segLength)
        {
            fileName = filename;
            startLocation = start;
            this.length = length;
            this.segLength = segLength;
        }

        public float[] getData()
        {
            float[] data = new float[length];
            using (AudioFileReader audioFileReader = new AudioFileReader(fileName))
            {
                
                audioFileReader.Position = startLocation * 4;
                audioFileReader.Read(data, 0, data.Length);
            }
            return (data);
            
        }

        public float[] getData(int start,int Length)
        {
            float[] data = new float[Length];
            
            return (data);
        }


    }
}
