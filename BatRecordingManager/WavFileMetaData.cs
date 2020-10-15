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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BatRecordingManager
{
    /// <summary>
    ///     Handler for metadata embedded in a .wav file in either WAMD or GUANO format.
    ///     Provides getters for all the commonly used parameters and populates them using
    ///     data from whichever source is available.
    /// </summary>
    internal class WavFileMetaData
    {
        /// <summary>
        ///     Constructor for WavFileMetaData.  Given a path to a .wav file reads the WAMD or
        ///     GUANO metdata from that file and uses it and other file information to populate the
        ///     appropriate data fields.
        /// </summary>
        /// <param name="filename"></param>
        public WavFileMetaData(string filename)
        {
            // set a default start date as the file creation or modified date
            try
            {
                if (m_Start == null)
                    if (File.Exists(filename) && (new FileInfo(filename).Length > 0L))
                    {
                        var duration = Tools.GetFileDatesAndTimes(filename, out string wavfile, out DateTime fileStart, out DateTime fileEnd);

                        m_Start = fileStart;
                        m_Created = fileStart;
                        m_End = fileEnd;

                        /*if (DBAccess.GetDateTimeFromFilename(filename, out var dt))
                        {
                            m_Start = dt;
                        }
                        else
                        {
                            m_Start = File.GetCreationTime(filename);
                            m_Created = m_Start;
                            if (m_Start == null || m_Start.Value.Year < 1960) m_Start = File.GetLastWriteTime(filename);
                        }*/ //replaced with the four lines above 23/7/2020
                    }

                Read_MetaData(filename);
                if (metadata != null) success = true;

                if (m_Start != null && m_Duration != null && m_End == null) m_End = m_Start + m_Duration;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Error in WavFileMetaData:- " + ex.Message);
                m_Note = "Metadata not read\n";
                success = false;
            }
        }

        /// <summary>
        ///     The results of any Auto-identification as a string
        /// </summary>
        public string m_AutoID { get; private set; }

        /// <summary>
        ///     The name of the recording device
        /// </summary>
        public string m_Device { get; private set; }

        /// <summary>
        ///     The duration of the .wav file calculated from the file header information
        /// </summary>
        public TimeSpan? m_Duration { get; private set; }

        /// <summary>
        ///     The end date and time for the file extracted from the metadata or by taking the start
        ///     Date and time and adding the file duration
        /// </summary>
        public DateTime? m_End { get; private set; }

        /// <summary>
        ///     the fully qualified name of the file from which the data was extracted
        /// </summary>
        public string m_FileName { get; private set; }

        /// <summary>
        ///     The location for the recording as GPS latitude and longitude as a pair of doubles
        /// </summary>
        public GPSLocation m_Location { get; private set; }

        /// <summary>
        ///     The string containing the manual identification from the Metadata
        /// </summary>
        public string m_ManualID { get; private set; }

        /// <summary>
        ///     The type of microphone used for the recording
        /// </summary>
        public string m_Microphone { get; private set; }

        /// <summary>
        ///     The contents of the notes field of the metadata
        /// </summary>
        public string m_Note { get; private set; }

        /// <summary>
        ///     The software used for the analysis
        /// </summary>
        public string m_Software { get; private set; }

        /// <summary>
        ///     The start time and date for the wavfile extracted from the filename or metadata, or if that
        ///     is not available from the filename, or failing that the file creation date and time
        ///     or the file modified date and time
        /// </summary>
        public DateTime? m_Start { get; private set; }

        /// <summary>
        ///     The temperature at the time of the recording
        /// </summary>
        public string m_Temperature { get; private set; }

        public bool success { get; set; } = false;

        internal string FormattedText()
        {
            var text = "";
            if (!string.IsNullOrWhiteSpace(m_source))
                text += "[ " + m_source + " metadata:-";
            else
                text += "[ Metadata:-";
            if (m_FileName != null) text += "\n" + m_FileName;
            if (m_Start != null)
                text += "\n" + m_Start.Value.ToShortDateString() + " " + m_Start.Value.ToLongTimeString();
            if (m_End != null) text += " - " + m_End.Value.ToShortDateString() + " " + m_End.Value.ToLongTimeString();
            if (m_Duration != null) text += "\nFile Duration = " + m_Duration.Value.TotalSeconds + " s";

            if (m_Location != null && !string.IsNullOrWhiteSpace(m_Location.m_Name))
            {
                text += "\n" + m_Location.m_Name;
                if (m_Location != null && !string.IsNullOrWhiteSpace(m_Location.m_ID))
                    text += " (" + m_Location.m_ID + ")";
            }

            if (m_Location != null && m_Location.m_Latitude != null && m_Location.m_Longitude != null)
                text += "\nGPS:- " + m_Location.m_Latitude + ", " + m_Location.m_Longitude;
            if (m_Device != null) text += "\nDevice:- " + m_Device;
            if (m_Microphone != null) text += "\nMic:- " + m_Microphone;
            if (m_Temperature != null) text += "\nTemp:- " + m_Temperature;
            if (m_Software != null) text += "\nAnalysed with:- " + m_Software;
            if (m_Note != null) text += "\n    " + m_Note;
            text += "\n]\n";

            return text.Replace("\n\n", "\n").Trim();
        }

        /// <summary>
        ///     file creation date and time.  Start date is the earlier of this and the embedded metadata
        ///     date and time as that might be the analysed date rather than the collected date
        /// </summary>
        private DateTime? m_Created { get; }

        private DateTime? m_MetaDate { get; set; }
        private string m_Notes { get; set; } = "";
        private string m_source { get; set; }
        private byte[] metadata { get; set; }

        /// <summary>
        ///     Given a 'chunk' of metadata from a wav file wamd chunk
        ///     which is everything after the wamd header and size attribute,
        ///     Returns true if any wamd data field is found and decoded
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        private bool decode_wamd_data(byte[] metadata)
        {
            var result = false;
            var entries = new Dictionary<short, string>();

            var bReader = new BinaryReader(new MemoryStream(metadata));

            while (bReader.BaseStream.Position < bReader.BaseStream.Length)
            {
                var type = bReader.ReadInt16(); // 01 00
                var size = bReader.ReadInt32(); // 03 00 00 00
                var bData = bReader.ReadBytes(size);
                if (type > 0)
                    try
                    {
                        var data = Encoding.UTF8.GetString(bData);
                        entries.Add(type, data);
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        Debug.WriteLine(ex);
                    }
            }

            foreach (var entry in entries)
                switch (entry.Key)
                {
                    case 0x000A:
                        m_Note = entry.Value;
                        m_Note = m_Note.Replace(@"\n", "");
                        result = true;
                        break;

                    case 0x0005:
                        var dt = new DateTime();
                        if (DateTime.TryParse(entry.Value, out dt))
                        {
                            m_MetaDate = dt;
                            m_Start = m_Start != null ? dt < m_Start.Value && dt.Year > 1960 ? dt : m_Start : dt;

                            if (m_Duration != null) m_End = m_Start + m_Duration;
                            result = true;
                        }

                        break;

                    case 0x000C:
                        if (string.IsNullOrWhiteSpace(m_ManualID)) m_ManualID = "";
                        if (!m_ManualID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                        {
                            m_ManualID = (m_ManualID + " " + entry.Value).Trim();
                        }
                        result = true;
                        break;

                    case 0x000B:
                        if (string.IsNullOrWhiteSpace(m_AutoID)) m_AutoID = "";
                        if (!m_AutoID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                        {
                            m_AutoID = "(" + (entry.Value + " " + m_AutoID).Trim() + ")";
                        }
                        result = true;
                        break;

                    case 0x000E: //AUTO_ID_STATS
                        if (string.IsNullOrWhiteSpace(m_AutoID)) m_AutoID = "";
                        if (m_AutoID.EndsWith(")")) m_AutoID = m_AutoID.Substring(0, m_AutoID.Length - 1);
                        m_AutoID = m_AutoID + ": " + entry.Value + ")";
                        result = true;
                        break;

                    case 0x0006: // GPS_FIRST
                        if (m_Location == null || (m_Location.m_Latitude == 0.0d && m_Location.m_Longitude == 0.0d) ||
                            Math.Abs(m_Location.m_Latitude) > 90.0d || Math.Abs(m_Location.m_Longitude) > 180.0d)
                        {
                            m_Location = new GPSLocation(entry.Value);
                        }
                        result = true;
                        break;

                    case 0x0001:
                        m_Device = entry.Value;
                        result = true;
                        if (entries.ContainsKey(0x0002)) m_Device = m_Device + " " + entries[0x0002];
                        if (entries.ContainsKey(0x0003)) m_Device = m_Device + " " + entries[0x0003];
                        break;

                    case 0x0012:
                        m_Microphone = entry.Value;
                        result = true;
                        if (entries.ContainsKey(0x0013)) m_Microphone = m_Microphone + " " + entries[0x0013];
                        break;

                    case 0x0015: //TEMP_INT
                        if (!string.IsNullOrWhiteSpace(m_Temperature)) m_Temperature = entry.Value;
                        result = true;
                        break;

                    case 0x0016: //TEMP_EXT
                        m_Temperature = entry.Value;
                        result = true;
                        break;

                    case 0x0008: //SOFTWARE
                        m_Software = entry.Value;
                        result = true;
                        break;
                }

            if (result)
            {
                if (string.IsNullOrWhiteSpace(m_source))
                    m_source = "WAMD";
                else
                    m_source += " and WAMD";
            }

            return result;
        }

        /// <summary>
        ///     Given the GUANO data chunk from a .wav file, converted into a UTF-8 string,
        ///     parses that string for Guano data fields and uses them to populate the data
        ///     section of the class.
        /// </summary>
        /// <param name="metadataString"></param>
        /// <returns></returns>
        private bool DecodeGuanoData(string metadataString)
        {
            var result = false;
            var entries = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(metadataString))
            {
                var lines = metadataString.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1) entries.Add(parts[0], parts[1]);
                }

                //
                // Codes taken from https://github.com/riggsd/guano-spec/blob/master/guano_specification.md
                //
                foreach (var entry in entries)
                    switch (entry.Key)
                    {
                        case "Note":
                            m_Note = entry.Value;
                            m_Note = m_Note.Replace(@"\n", "");
                            result = true;
                            break;

                        case "Timestamp":
                            if (DateTime.TryParse(entry.Value, out var dt))
                            {
                                m_MetaDate = dt;
                                m_Start = m_Start != null ? dt < m_Start.Value && dt.Year > 1960 ? dt : m_Start : dt;

                                if (m_Duration != null) m_End = m_Start + m_Duration;
                                result = true;
                            }

                            break;

                        case "Species Manual ID":
                            if (string.IsNullOrWhiteSpace(m_ManualID)) m_ManualID = "";
                            if (!m_ManualID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                            {
                                m_ManualID = (m_ManualID + " " + entry.Value).Trim();
                            }

                            result = true;
                            break;

                        case "Species Auto ID":
                            if (string.IsNullOrWhiteSpace(m_AutoID)) m_AutoID = "";
                            if (!m_AutoID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                            {
                                m_AutoID = "(" + (m_AutoID + " " + entry.Value).Trim() + ")";
                            }
                            result = true;
                            break;

                        case "Loc Position":
                            if (!GpxHandler.IsValidLocation(m_Location))
                            {
                                var lat = 200.0f;
                                var longit = 200.0f;
                                var sections = entry.Value.Trim().Split(' ');
                                if (sections.Length > 1)
                                {
                                    float.TryParse(sections[0], out lat);
                                    float.TryParse(sections[1], out longit);
                                    if (lat < 200.0f && longit < 200.0f)
                                    {
                                        m_Location = new GPSLocation(lat, longit);
                                        result = true;
                                    }
                                }
                            }
                            else result = true;

                            break;

                        case "Make":
                            m_Device = entry.Value;
                            if (entries.ContainsKey("Model")) m_Device = m_Device + " " + entries["Model"];
                            result = true;
                            break;

                        case "Original Filename":
                            m_FileName = entry.Value;
                            result = true;
                            break;

                        case "Temperature Ext":
                            m_Temperature = entry.Value;
                            result = true;
                            break;

                        case "Temperature Int":
                            if (string.IsNullOrWhiteSpace(m_Temperature)) m_Temperature = entry.Value;
                            result = true;
                            break;

                        case "WA|Kaleidoscope|Version":
                            m_Software = "Kaleidoscope v" + entry.Value;
                            result = true;
                            break;
                    }
            }

            if (result)
            {
                if (string.IsNullOrWhiteSpace(m_source))
                    m_source = "GUANO";
                else
                    m_source += " and GUANO";
            }

            return result;
        }

        /// <summary>
        ///     Retrieves the metadata sections from a .wav file for either WAMD or GUANO formatted data.
        ///     The file from which to extract the data is wavFilename and the metadata chunk itself is returned as
        ///     a byte[] called metadata.  Formatted versions of the data are returned in the out parameters wamd_data
        ///     and guano_data.  If not present in that format the classes will be returned empty.
        ///     The function returns a string comprising the metadate note section followed by a ; followed by the manual
        ///     species identification string and an optional auto-identification string in brackets.
        ///     the data out parameters will be null if not found.
        ///     Accepts both .wav and .zc files
        /// </summary>
        /// <param name="wavFilename"></param>
        /// <param name="metadata"></param>
        /// <param name="wamd_data"></param>
        /// <param name="guano_data"></param>
        /// <returns></returns>
        private bool Read_MetaData(string wavFilename)
        {
            var result = false;
            metadata = null;

            if (string.IsNullOrWhiteSpace(wavFilename)) return result;
            if (!wavFilename.Trim().EndsWith(".WAV", StringComparison.OrdinalIgnoreCase)) return (readZcMetadata(wavFilename));
            if (!File.Exists(wavFilename) || (new FileInfo(wavFilename).Length <= 0L)) return result;
            try
            {
                using (var fs = File.Open(wavFilename, FileMode.Open))
                {
                    var reader = new BinaryReader(fs);

                    // chunk 0
                    var chunkID = reader.ReadInt32(); //RIFF
                    var fileSize = reader.ReadInt32(); // 4 bytes of size
                    var riffType = reader.ReadInt32(); //WAVE

                    // chunk 1
                    var fmtID = reader.ReadInt32(); //fmt_
                    var fmtSize = reader.ReadInt32(); // bytes for this chunk typically 16
                    int fmtCode = reader.ReadInt16(); // typically 1
                    int channels = reader.ReadInt16(); // 1 or 2
                    var sampleRate = reader.ReadInt32(); //
                    var byteRate = reader.ReadInt32();
                    int fmtBlockAlign = reader.ReadInt16(); // 4
                    int bitDepth = reader.ReadInt16(); //16

                    if (fmtSize == 18) // not expected for .wav files
                    {
                        // Read any extra values
                        int fmtExtraSize = reader.ReadInt16();
                        reader.ReadBytes(fmtExtraSize);
                    }

                    var header = new byte[4];
                    byte[] data = null;
                    var dataBytes = 0;
                    // WAMD_Data wamd_data = new WAMD_Data();

                    try
                    {
                        metadata = null;

                        do
                        {
                            try
                            {
                                header = reader.ReadBytes(4);
                                if (header == null || header.Length != 4) break;
                                var size = reader.ReadInt32();
                                try
                                {
                                    data = reader.ReadBytes(size);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Tried to read to much data - {size}:-{ex}");
                                }

                                try
                                {
                                    if ((size & 0x0001) != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
                                        // we have an odd number of bytes for size, so read the xtra null byte of padding
                                        reader.ReadByte();
                                }
                                catch (Exception) // just in case it overflows the data file
                                {
                                }

                                var strHeader = Encoding.UTF8.GetString(header);
                                if (strHeader == "data") dataBytes = size;
                                if (strHeader == "wamd" && data != null)
                                {
                                    Debug.WriteLine("WAMD data:-" + header + "/" + size + "/" + data.Length);
                                    metadata = data;
                                    result = decode_wamd_data(metadata);
                                }

                                if (strHeader == "guan" && data != null)
                                {
                                    Debug.WriteLine("GUANO data:-" + header + "/" + size + "/" + data.Length);
                                    metadata = data;
                                    var metadataString = Encoding.UTF8.GetString(data);
                                    result = DecodeGuanoData(metadataString);
                                }
                            }
                            catch (Exception)
                            {
                                Debug.WriteLine("Overflowed the data file - " + reader.BaseStream.Position + "/" +
                                                reader.BaseStream.Length);
                                break;
                            }
                        } while (reader.BaseStream.Position < reader.BaseStream.Length);
                    }
                    catch (IOException iox)
                    {
                        Tools.ErrorLog(iox.Message);
                        Debug.WriteLine("Error reading wav file:- " + iox.Message);
                    }

                    var durationInSecs = 0.0d;
                    if (byteRate > 0 && channels > 0 && dataBytes > 0)
                    {
                        durationInSecs = (double)dataBytes / byteRate;
                        m_Duration = TimeSpan.FromSeconds(durationInSecs);
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }

            return result;
        }

        private bool readZcMetadata(string fileName)
        {
            var result = false;
            try
            {
                ZcMetadata zcMetadata = new ZcMetadata(fileName);

                result = zcMetadata.ReadData();

                m_FileName = fileName;
                if (zcMetadata.hasGpsLocation)
                {
                    m_Location = new GPSLocation(zcMetadata.m_latitude, zcMetadata.m_longitude);
                    m_Location.m_Name = zcMetadata.m_Location;
                }

                m_ManualID = zcMetadata.m_Species;
                m_Device = zcMetadata.m_Tape + " " + zcMetadata.m_Spec;

                m_Note = zcMetadata.m_Note + "\n" + zcMetadata.m_Note1;

                if (!String.IsNullOrWhiteSpace(zcMetadata.m_GuanoData))
                {
                    DecodeGuanoData(zcMetadata.m_GuanoData);
                }
                metadata = new byte[10];
                if (m_Start == null || DateTime.Now - m_Start.Value < new TimeSpan(0, 5, 0))
                {
                    m_Start = zcMetadata.m_startDateTime;
                }
                if (m_Duration == null) m_Duration = zcMetadata.m_duration;
                result = true;
            }
            catch (Exception)
            {
                metadata = null;
                result = false;
            }

            return (result);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///

    #region GPSLocation

    /// <summary>
    ///     A class to hold details of a particular location
    /// </summary>
    public class GPSLocation
    {
        /// <summary>
        ///     Constructor for a Location class.  Paraeters are GPS co-ordinates for
        ///     Latitude and Longitude as doubles, and an optional name and 3 or 4 letter identification code
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="name"></param>
        /// <param name="ID"></param>
        public GPSLocation(double latitude, double longitude, string name = "", string ID = "")
        {
            m_Latitude = latitude;
            m_Longitude = longitude;
            m_GridRef = ConvertGPStoGridRef(latitude, longitude);
            m_Name = name;
            m_ID = ID;
        }

        /// <summary>
        ///     Alternative constructoer for a Location class object.
        ///     The parameters are a strig defining the WGS84 location, and
        ///     an optional name and 3 or 4 letter identification code.
        ///     The string should be in the format:-
        ///     nn.nnnnn,N,mmm.mmmmm,W[,alt]
        /// </summary>
        /// <param name="WGS84AsciiLocation"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        public GPSLocation(string WGS84AsciiLocation, string name = "", string id = "")
        {
            if (ConvertToLatLong(WGS84AsciiLocation, out var latitude, out var longitude))
            {
                m_Latitude = latitude;
                m_Longitude = longitude;
                m_GridRef = ConvertGPStoGridRef(latitude, longitude);
                m_Name = name;
                m_ID = id;
            }
        }

        /// <summary>
        ///     The UK grid reference for the location if possible, calculated from the
        ///     latitude and longitude fields
        /// </summary>
        public string m_GridRef { get; }

        /// <summary>
        ///     A three or four letter ID for the location.  May be null or empty
        /// </summary>
        public string m_ID { get; }

        /// <summary>
        ///     GPS latitude as a double
        /// </summary>
        public double m_Latitude { get; }

        /// <summary>
        ///     GPS longitude as a double
        /// </summary>
        public double m_Longitude { get; }

        /// <summary>
        ///     The common name for the location.  May be null or empty.
        /// </summary>
        public string m_Name { get; set; }

        /// <summary>
        ///     Converts a GPS position in the form of latitude and longitude into a UK grid reference.
        ///     May not be precise because altitude is not take into account in the conversion, but is
        ///     generally close enough.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public static string ConvertGPStoGridRef(double latitude, double longitude)
        {
            var result = "";

            var nmea2OSG = new NMEA2OSG();
            if (latitude >= -90.0d && latitude <= 90.0d && longitude >= -180.0d && longitude <= 180.0d)
                // we have valid latitudes and longitudes
                // for now just assume they are in the OS grid ref acceptable area

                if (nmea2OSG.Transform(latitude, longitude, 0.0d))
                    result = nmea2OSG.ngr;

            return result;
        }

        /// <summary>
        ///     Converts a string in the format "blah nn.nnnnn,N,mmm.mmmmm,W[,alt]
        ///     into a latitude and longitude pair in the form of doubles
        /// </summary>
        /// <param name="wGS84AsciiLocation"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        private bool ConvertToLatLong(string WGS84AsciiLocation, out double latitude, out double longitude)
        {
            var result = false;
            latitude = 200.0d;
            longitude = 200.0d;
            var pattern = @"WGS84,([0-9.-]*),?([NS]?),([0-9.-]*),?([WE]?)"; // e.g. WGS84,51.74607,N,0.26183,W
            var match = Regex.Match(WGS84AsciiLocation, pattern);
            if (match.Success && match.Groups.Count >= 5)
            {
                if (double.TryParse(match.Groups[1].Value, out var dd)) latitude = dd;
                dd = -1.0d;
                if (double.TryParse(match.Groups[3].Value, out dd)) longitude = dd;
                if (match.Groups[2].Value.Contains("S")) latitude = 0.0d - latitude;
                if (match.Groups[4].Value.Contains("W")) longitude = 0.0d - longitude;
            }

            if (latitude < 200.0d && longitude < 200.0d) result = true;
            return result;
        }
    }

    #endregion GPSLocation
}