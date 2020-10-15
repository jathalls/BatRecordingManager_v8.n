using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace BatRecordingManager
{
    internal class ZcMetadata
    {
        public ZcMetadata(string zcFileName)
        {
            m_fileName = "";
            isDataCached = false;
            if (!string.IsNullOrWhiteSpace(zcFileName) && zcFileName.EndsWith(".zc", StringComparison.OrdinalIgnoreCase) && File.Exists(zcFileName))
            {
                m_fileName = zcFileName;
            }
        }

        public bool hasGpsLocation { get; private set; }
        public string m_DateString { get; private set; }
        public TimeSpan m_duration { get; set; } = TimeSpan.FromSeconds(15);
        public string m_fileName { get; set; } = "";
        public string m_GuanoData { get; private set; }
        public double m_latitude { get; private set; }
        public string m_Location { get; private set; }
        public double m_longitude { get; private set; }
        public string m_Note { get; private set; }
        public string m_Note1 { get; private set; }
        public string m_Spec { get; private set; }
        public string m_Species { get; private set; }
        public DateTime m_startDateTime { get; set; } = DateTime.Now;

        public string m_Tape { get; private set; }

        internal DateTime GetTimeAndDuration(out TimeSpan duration, out string textHeader)
        {
            duration = TimeSpan.FromSeconds(15);
            DateTime result = DateTime.Now;
            textHeader = "";

            if (!isDataCached)
            {
                if (ReadData())
                {
                    duration = m_duration;
                    textHeader = m_textHeader;
                    return (m_startDateTime);
                }
            }
            else
            {
                duration = m_duration;
                textHeader = m_textHeader;
                return (m_startDateTime);
            }
            return (result);
        }

        internal bool ReadData()
        {
            bool result = false;
            if (!string.IsNullOrWhiteSpace(m_fileName) && m_fileName.EndsWith(".zc", StringComparison.OrdinalIgnoreCase) && File.Exists(m_fileName))
            {
                using (var fs = new FileStream(m_fileName, FileMode.Open))
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
                                m_startDateTime = new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second);
                                m_startDateTime += tsTicks;

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
                            Debug.WriteLine("Unable to read ZC metadata");
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
                    m_longitude = 200.0d;
                    m_latitude = 200.0d;
                    hasGpsLocation = false;
                }
                else
                {
                    hasGpsLocation = true;
                    string strLatDeg = Encoding.UTF8.GetString(gpsData.Skip(11).Take(2).ToArray()).Trim();

                    string strLatFract = Encoding.UTF8.GetString(gpsData.Skip(13).Take(5).ToArray()).Trim();
                    strLatDeg = $"{strLatDeg}.{strLatFract}";
                    double lat = 200.0d;
                    if (double.TryParse(strLatDeg, out lat)) m_latitude = lat;

                    string strLongDeg = Encoding.UTF8.GetString(gpsData.Skip(20).Take(3).ToArray()).Trim();
                    string strLongFract = Encoding.UTF8.GetString(gpsData.Skip(23).Take(5).ToArray()).Trim();
                    if (double.TryParse($"{strLongDeg}.{strLongFract}", out double longit)) m_longitude = longit;
                }
            }
            hasGpsLocation = GpxHandler.IsValidLocation(m_latitude, m_longitude);
        }

        private void decodeGuanoData(byte[] guanoData)
        {
            if (guanoData == null || guanoData.Length <= 0) return;
            m_GuanoData = Encoding.UTF8.GetString(guanoData).Trim();
            if (string.IsNullOrWhiteSpace(m_GuanoData)) return;
        }

        /// <summary>
        /// given an array of 275 bytes of text header data, decodes the textual fields that exist
        /// </summary>
        /// <param name="textHeader"></param>
        private void decodeTextHeader(byte[] textHeader)
        {
            if (textHeader != null && textHeader.Length == 275)
            {
                m_Tape = Encoding.UTF8.GetString(textHeader.Take(8).ToArray());
                m_Tape = m_Tape.Replace('\0', ' ').Trim();
                m_DateString = Encoding.UTF8.GetString(textHeader.Skip(8).Take(8).ToArray());
                m_DateString = m_DateString.Replace('\0', ' ').Trim();
                m_Location = Encoding.UTF8.GetString(textHeader.Skip(16).Take(40).ToArray());
                m_Location = m_Location.Replace('\0', ' ').Trim();
                m_Species = Encoding.UTF8.GetString(textHeader.Skip(56).Take(50).ToArray());
                m_Species = m_Species.Replace('\0', ' ').Trim();
                m_Spec = Encoding.UTF8.GetString(textHeader.Skip(106).Take(16).ToArray());
                m_Spec = m_Spec.Replace('\0', ' ').Trim();
                m_Note = Encoding.UTF8.GetString(textHeader.Skip(122).Take(73).ToArray());
                m_Note = m_Note.Replace('\0', ' ').Trim();
                m_Note1 = Encoding.UTF8.GetString(textHeader.Skip(195).Take(80).ToArray());
                m_Note1 = m_Note1.Replace('\0', ' ').Trim();
                m_textHeader = Encoding.UTF8.GetString(textHeader);
                m_textHeader = m_textHeader.Replace('\0', ' ').Trim();
            }
        }
    }
}