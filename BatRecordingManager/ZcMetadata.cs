using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BatRecordingManager
{
    internal class ZcMetadata
    {
        public ZcMetadata(string zcFileName)
        {
            FileName = "";
            isDataCached = false;
            if (!string.IsNullOrWhiteSpace(zcFileName) && zcFileName.EndsWith(".zc", StringComparison.OrdinalIgnoreCase) && File.Exists(zcFileName))
            {
                FileName = zcFileName;
            }
        }

        public bool hasGpsLocation { get; private set; }
        public string m_DateString { get; private set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(15);
        public string FileName { get; set; } = "";
        public string GuanoData { get; private set; }
        public double Latitude { get; private set; }
        public string Location { get; private set; }
        public double Longitude { get; private set; }
        public string Note { get; private set; }
        public string Note1 { get; private set; }
        public string Spec { get; private set; }
        public string Species { get; private set; }
        public DateTime StartDateTime { get; set; } = DateTime.Now;

        public string Tape { get; private set; }

        internal DateTime GetTimeAndDuration(out TimeSpan duration, out string textHeader)
        {
            duration = TimeSpan.FromSeconds(15);
            DateTime result = DateTime.Now;
            textHeader = "";

            if (!isDataCached)
            {
                if (ReadData())
                {
                    duration = Duration;
                    textHeader = m_textHeader;
                    return (StartDateTime);
                }
            }
            else
            {
                duration = Duration;
                textHeader = m_textHeader;
                return (StartDateTime);
            }
            return (result);
        }

        internal bool ReadData()
        {
            bool result = false;
            if (!string.IsNullOrWhiteSpace(FileName) && FileName.EndsWith(".zc", StringComparison.OrdinalIgnoreCase) && File.Exists(FileName))
            {
                using (var fs = new FileStream(FileName, FileMode.Open))
                {
                    if (fs == null)
                    {
                        isDataCached = false;
                        return (false);
                    }
                    using (var breader = new BinaryReader(fs))
                    {
                        try
                        {
                            while (breader.BaseStream.Position < breader.BaseStream.Length)
                            {
                                var ptrptr = breader.ReadUInt16();
                                _ = breader.ReadByte(); // skip 1 byte of 00
                                var FileType = breader.ReadByte();
                                _ = breader.ReadUInt16(); // skip two bytes of 00

                                var TextHeader = breader.ReadBytes(275);
                                if (TextHeader.Length > 0)
                                {
                                    decodeTextHeader(TextHeader);
                                }

                                _ = breader.ReadByte();

                                var ptrData = breader.ReadUInt16(); // reads offset to start of data section

                                var Res1 = breader.ReadUInt16(); // number of counts in 25ms
                                var divRatio = breader.ReadByte(); // division ratio in recording
                                var vRes = breader.ReadByte(); // specifies scale for graph

                                var year = breader.ReadUInt16();
                                var month = breader.ReadByte();
                                var day = breader.ReadByte();
                                var hour = breader.ReadByte();
                                var minute = breader.ReadByte();
                                var second = breader.ReadByte();
                                var hundredths = breader.ReadByte();
                                var microseconds = breader.ReadUInt16();
                                long ticks = hundredths * 100L;
                                ticks += microseconds * 10L;
                                TimeSpan tsTicks = new TimeSpan(ticks);

                                int milliseconds = ((int)hundredths * 10) + (int)Math.Floor((double)microseconds / 1000.0d);
                                StartDateTime = new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second);
                                StartDateTime += tsTicks;

                                _ = breader.ReadBytes(6);  // skip past 6-byte ID code

                                var gpsData = breader.ReadBytes(32);
                                decodeGPSData(gpsData);

                                UInt16 guanoSize = (UInt16)((long)ptrData - breader.BaseStream.Position); // find length from here to data block
                                if (guanoSize > 6)
                                {
                                    var guanoData = breader.ReadBytes(guanoSize);
                                    decodeGuanoData(guanoData);
                                }

                                isDataCached = true;
                                return (true);

                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Unable to read ZC metadata; " + ex.Message);
                            isDataCached = false;
                            return (result);
                        }
                        finally
                        {
                            breader.Close();
                        }
                    }
                    fs.Close();
                }
            }
            return (false);
        }

        private bool isDataCached { get; set; } = false;

        private string m_textHeader { get; set; } = "";

        private void decodeGPSData(byte[] gpsData)
        {
            if (gpsData != null && gpsData.Length == 32)
            {
                if (char.IsDigit((char)gpsData[10]))
                {
                    // UTM data not decoded here
                    Longitude = 200.0d;
                    Latitude = 200.0d;
                    hasGpsLocation = false;
                }
                else
                {
                    hasGpsLocation = true;
                    string strLatDeg = Encoding.UTF8.GetString(gpsData.Skip(11).Take(2).ToArray()).Trim();

                    string strLatFract = Encoding.UTF8.GetString(gpsData.Skip(13).Take(5).ToArray()).Trim();
                    strLatDeg = $"{strLatDeg}.{strLatFract}";
                    double lat = 200.0d;
                    if (double.TryParse(strLatDeg, out lat)) Latitude = lat;

                    string strLongDeg = Encoding.UTF8.GetString(gpsData.Skip(20).Take(3).ToArray()).Trim();
                    string strLongFract = Encoding.UTF8.GetString(gpsData.Skip(23).Take(5).ToArray()).Trim();
                    if (double.TryParse($"{strLongDeg}.{strLongFract}", out double longit)) Longitude = longit;
                }
            }
            hasGpsLocation = GpxHandler.IsValidLocation(Latitude, Longitude);
        }

        private void decodeGuanoData(byte[] guanoData)
        {
            if (guanoData == null || guanoData.Length <= 0) return;
            GuanoData = Encoding.UTF8.GetString(guanoData).Trim();
            if (string.IsNullOrWhiteSpace(GuanoData)) return;
        }

        /// <summary>
        /// given an array of 275 bytes of text header data, decodes the textual fields that exist
        /// </summary>
        /// <param name="textHeader"></param>
        private void decodeTextHeader(byte[] textHeader)
        {
            if (textHeader != null && textHeader.Length == 275)
            {
                Tape = Encoding.UTF8.GetString(textHeader.Take(8).ToArray());
                Tape = Tape.Replace('\0', ' ').Trim();
                m_DateString = Encoding.UTF8.GetString(textHeader.Skip(8).Take(8).ToArray());
                m_DateString = m_DateString.Replace('\0', ' ').Trim();
                Location = Encoding.UTF8.GetString(textHeader.Skip(16).Take(40).ToArray());
                Location = Location.Replace('\0', ' ').Trim();
                Species = Encoding.UTF8.GetString(textHeader.Skip(56).Take(50).ToArray());
                Species = Species.Replace('\0', ' ').Trim();
                Spec = Encoding.UTF8.GetString(textHeader.Skip(106).Take(16).ToArray());
                Spec = Spec.Replace('\0', ' ').Trim();
                Note = Encoding.UTF8.GetString(textHeader.Skip(122).Take(73).ToArray());
                Note = Note.Replace('\0', ' ').Trim();
                Note1 = Encoding.UTF8.GetString(textHeader.Skip(195).Take(80).ToArray());
                Note1 = Note1.Replace('\0', ' ').Trim();
                m_textHeader = Encoding.UTF8.GetString(textHeader);
                m_textHeader = m_textHeader.Replace('\0', ' ').Trim();
            }
        }
    }
}