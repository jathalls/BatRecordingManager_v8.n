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

namespace BatRecordingManager
{
    /// <summary>
    ///     A class to represent a Guano metadata chunk included in a .wav file.
    ///     The guano data should be extracted with Tools.Get_WAV_MetaData(wavfilename)
    ///     and the string can be parsed with ParseGuanoChunk(string chunk) which will
    ///     return an instance of this class.
    ///     The class copes with a number of known fields for GUANO data according to
    ///     specification 1.0 but is intended to work with GUANO data embedded by
    ///     the Android App BatRecorder.
    /// </summary>
    public class GuanoDeprecated
    {
        private string _deviceVersion;

        private TimeSpan? _duration;

        private int? _expansion;

        private double? _filterHp;


        private double? _filterLp;

        private string _firmwareVersion;

        private string _hardwareVersion;

        private string _hostDevice;

        private string _hostOs;

        private double? _humidity;

        private double? _length;
        private string[] _lines;

        private Tuple<double, double> _location;

        private double? _locationAccuracy;

        private double? _locationElevation;

        private string _make;

        private string _model;

        private string _note;

        private int? _samplerate;

        private string _speciesAutoID;

        private string _speciesManualID;

        private string[] _tags;

        private double? _temperature;

        private DateTime? _timestamp;

        private double? _version;
        private WAMD_Data _wamdData;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string RawText;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member


        /// <summary>
        ///     Constructor for the Guano class.  May be initialized with either the name
        ///     and path of a .wav file which has a Guano metadata section, or with the text of a
        ///     Guano Metadata chunk as returned by Tools.Get_WAV_MetaData(wavfile)
        /// </summary>
        /// <param name="guanoText"></param>
        public GuanoDeprecated(string guanoText)
        {
            _wamdData = null;
            RawText = "";
        }

        public TimeSpan? duration
        {
            get
            {
                if (_duration == null)
                    if (_lines != null && _lines.Length > 0)
                        foreach (var line in _lines)
                            if (line.StartsWith("Duration:"))
                            {
                                var parts = line.Split(' ');
                                if (parts != null && parts.Length > 1)
                                    if (TimeSpan.TryParse(parts[1].Trim(), out var ts))
                                        _duration = ts;
                            }


                return _duration;
            }
            set => _duration = value;
        }

        /// <summary>
        ///     MAKER|Version:
        /// </summary>
        public double? version
        {
            get
            {
                var found = false;
                if (_version == null)
                {
                    if (_lines != null && _lines.Length > 0)
                        foreach (var line in _lines)
                            if (line.StartsWith("GUANO|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var v))
                                    {
                                        _version = v;
                                        found = true;
                                    }
                            }
                }
                else
                {
                    found = true;
                }

                if (!found && _wamdData != null) _version = _wamdData.versionAsDouble;
                return _version;
            }
        }

        /// <summary>
        ///     Timestamp:
        /// </summary>
        public DateTime? timestamp
        {
            get
            {
                if (_timestamp == null)
                    if (_lines != null && _lines.Length > 0)
                        foreach (var line in _lines)
                            if (line.Contains("Timestamp:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 0)
                                {
                                    var dt = new DateTime();
                                    DateTime.TryParse(parts[1], out dt);
                                    if (dt.Year > 1950) _timestamp = dt;
                                }
                            }

                return _timestamp;
            }
        }

        /// <summary>
        ///     Tags: a,b,c
        /// </summary>
        public string[] tags
        {
            get
            {
                if (_tags == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.Contains("Tags:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _tags = parts[1].Trim().Split(',');
                            }

                return _tags;
            }
        }

        /// <summary>
        ///     Note: text/nsecond line/nthird line
        /// </summary>
        public string note
        {
            get
            {
                if (_note == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Note:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _note = parts[1];
                            }

                return _note;
            }
        }

        /// <summary>
        ///     Samplerate:
        /// </summary>
        public int? samplerate
        {
            get
            {
                if (_samplerate == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Samplerate:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (int.TryParse(parts[1].Trim(), out var s))
                                        _samplerate = s;
                            }

                return _samplerate;
            }
        }

        /// <summary>
        ///     Filter HP: (in kHz)
        /// </summary>
        public double? filterHP
        {
            get
            {
                if (_filterHp == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Filter HP:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var f))
                                        _filterHp = f;
                            }

                return _filterHp;
            }
        }

        /// <summary>
        ///     Filter LP: (in kHz)
        /// </summary>
        public double? filterLP
        {
            get
            {
                if (_filterLp == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("GUANO|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var f))
                                        _filterLp = f;
                            }

                return _filterLp;
            }
        }

        /// <summary>
        ///     MAKER|Version:
        /// </summary>
        public string firmwareVersion
        {
            get
            {
                if (_firmwareVersion == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("GUANO|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _firmwareVersion = parts[1];
                            }

                return _firmwareVersion;
            }
        }

        /// <summary>
        ///     MAKER|Version:
        /// </summary>
        public string hardwareVersion
        {
            get
            {
                if (_hardwareVersion == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.Contains("|Version:") && !line.Trim().StartsWith("GUANO"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _hardwareVersion = parts[1];
                            }

                return _hardwareVersion;
            }
        }

        /// <summary>
        ///     Humidity: (0.0 - 100.0)
        /// </summary>
        public double? humidity
        {
            get
            {
                if (_humidity == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Humidity:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var v))
                                        _humidity = v;
                            }

                return _humidity;
            }
        }

        /// <summary>
        ///     Length: (in secs)
        /// </summary>
        public double? length
        {
            get
            {
                if (_length == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Length:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var v))
                                        _length = v;
                            }

                return _length;
            }
        }

        /// <summary>
        ///     Loc Accuracy:
        /// </summary>
        public double? locationAccuracy
        {
            get
            {
                if (_locationAccuracy == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Loc Accuracy:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var v))
                                        _locationAccuracy = v;
                            }

                return _locationAccuracy;
            }
        }

        /// <summary>
        ///     Loc Elevation:
        /// </summary>
        public double? locationElevation
        {
            get
            {
                if (_locationElevation == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Loc Elevation:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var v))
                                        _locationElevation = v;
                            }

                return _locationElevation;
            }
        }

        /// <summary>
        ///     Loc Position: (32.1878016 -86.1057312)
        /// </summary>
        public Tuple<double, double> location
        {
            get
            {
                if (_location == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Loc Position:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                {
                                    var locarray = parts[1].Trim().Split(' ');
                                    if (locarray != null && locarray.Length > 1)
                                        if (double.TryParse(locarray[0].Trim(), out var lat))
                                            if (double.TryParse(locarray[1].Trim(), out var longit))
                                                _location = new Tuple<double, double>(lat, longit);
                                }
                            }

                return _location;
            }
        }

        /// <summary>
        ///     MAKER|Make:
        /// </summary>
        public string make
        {
            get
            {
                if (_make == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.Contains("|Make:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _make = parts[1].Trim();
                            }

                return _make;
            }
        }

        /// <summary>
        ///     MAKER|Model:
        /// </summary>
        public string model
        {
            get
            {
                if (_model == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.Contains("|model:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _model = parts[1].Trim();
                            }

                return _model;
            }
        }

        /// <summary>
        ///     Species Auto ID: a,b,c
        /// </summary>
        public string speciesAutoID
        {
            get
            {
                if (_speciesAutoID == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Species Auto ID:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _speciesAutoID = parts[1].Trim();
                            }

                return _speciesAutoID;
            }
        }

        /// <summary>
        ///     Species Manual ID: a,b,c
        /// </summary>
        public string speciesManualID
        {
            get
            {
                if (_speciesManualID == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Species Manual ID:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _speciesManualID = parts[1].Trim();
                            }

                return _speciesManualID;
            }
        }

        /// <summary>
        ///     TE:
        /// </summary>
        public int? expansion
        {
            get
            {
                if (_expansion == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("TE:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (int.TryParse(parts[1].Trim(), out var v))
                                        _expansion = v;
                            }

                return _expansion;
            }
        }

        /// <summary>
        ///     Temperature Ext:
        ///     Temperature Int:
        /// </summary>
        public double? temperature
        {
            get
            {
                if (_temperature == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("Temperature Ext:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1)
                                    if (double.TryParse(parts[1].Trim(), out var v))
                                        _temperature = v;
                            }

                return _temperature;
            }
        }

        /// <summary>
        ///     BATREC|Version:
        /// </summary>
        public string deviceVersion
        {
            get
            {
                if (_deviceVersion == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("BATREC|Version:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _deviceVersion = parts[1];
                            }

                return _deviceVersion;
            }
        }

        /// <summary>
        ///     BATREC|Host Device:
        /// </summary>
        public string hostDevice
        {
            get
            {
                if (_hostDevice == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("BATREC|Host Device:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _hostDevice = parts[1];
                            }

                return _hostDevice;
            }
        }

        /// <summary>
        ///     BATREC|Host OS:
        /// </summary>
        public string hostOS
        {
            get
            {
                if (_hostOs == null)
                    if (!_lines.IsNullOrEmpty())
                        foreach (var line in _lines)
                            if (line.StartsWith("BATREC|Host OS:"))
                            {
                                var parts = line.Split(':');
                                if (parts != null && parts.Length > 1) _hostOs = parts[1];
                            }

                return _hostOs;
            }
        }

        /// <summary>
        ///     reads and loads the metadata from a string
        /// </summary>
        /// <param name="guanoText"></param>
        public void SetMetaData(string guanoText)
        {
            byte[] metadata = null;
            _wamdData = null;
            RawText = "";
            if (guanoText.Trim().ToUpper().EndsWith(".WAV"))
            {
                Get_WAVFile_MetaData(guanoText, out metadata, out var wamdData);
                _wamdData = wamdData;
            }
            else
            {
                RawText = guanoText;
            }

            if (!string.IsNullOrWhiteSpace(RawText)) _lines = RawText.Split('\n');
        }

        /// <summary>
        ///     Reads the wamd metadata chunk from a .wav file and converts it into the
        ///     equivalent of an array of lines read from an Audacity comment file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="wamd_data"></param>
        /// <returns></returns>
        internal string[] ReadMetadata(string fileName, out WAMD_Data wamdData)
        {
            byte[] metadata = null;
            //WAMD_Data wamd_data = null;
            var comment = "start - end " + Get_WAVFile_MetaData(fileName, out metadata, out wamdData);
            var result = new string[1];
            result[0] = comment;

            return result;
        }

        /// <summary>
        ///     Retrieves the metadata sections from a .wav file for either WAMD or GUANO formatted data.
        ///     The file from which to extract the data is wavFilename and the metadata chunk itself is returned as
        ///     a byte[] called metdata.  Formatted versions of the data are returned in the out parameters wamd_data
        ///     and guano_data.  If not present in that format the classes will be returned empty.
        ///     The function returns a string comprising the metadate note section followed by a ; followed by the manual
        ///     species identification string and an optional auto-identification string in brackets.
        ///     the data out parameters will be null if not found.
        /// </summary>
        /// <param name="wavFilename"></param>
        /// <param name="metadata"></param>
        /// <param name="wamd_data"></param>
        /// <param name="guano_data"></param>
        /// <returns></returns>
        public string Get_WAVFile_MetaData(string wavFilename, out byte[] metadata, out WAMD_Data wamdData)
        {
            metadata = null;
            var result = "";
            wamdData = new WAMD_Data();

            if (string.IsNullOrWhiteSpace(wavFilename)) return result;
            if (!wavFilename.Trim().ToUpper().EndsWith(".WAV")) return result;
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
                    byte[] data;
                    var dataBytes = 0;
                    // WAMD_Data wamd_data = new WAMD_Data();
                    result = "";
                    try
                    {
                        metadata = null;
                        wamdData = null;

                        do
                        {
                            header = reader.ReadBytes(4);
                            if (header == null || header.Length != 4) break;
                            var size = reader.ReadInt32();
                            data = reader.ReadBytes(size);
                            var strHeader = Encoding.UTF8.GetString(header);
                            if (strHeader == "data") dataBytes = size;
                            if (strHeader == "wamd")
                            {
                                metadata = data;
                                wamdData = decode_wamd_data(metadata);
                                result = (wamdData.note + "; " + wamdData.identification).Trim();
                                break;
                            }

                            if (strHeader == "guan" && data != null)
                            {
                                metadata = data;
                                result = Encoding.UTF8.GetString(data);
                                //decodeGuanoData(result);
                                result = note.Trim() + "; " + speciesManualID.Trim() +
                                         (string.IsNullOrWhiteSpace(speciesAutoID)
                                             ? "(" + speciesAutoID.Trim() + ")"
                                             : "");
                                break;
                            }
                        } while (reader.BaseStream.Position != reader.BaseStream.Length);
                    }
                    catch (IOException iox)
                    {
                        Tools.ErrorLog(iox.Message);
                        Debug.WriteLine("Error reading wav file:- " + iox.Message);
                    }

                    var durationInSecs = 0.0d;
                    if (byteRate > 0 && channels > 0 && dataBytes > 0)
                    {
                        durationInSecs = (double) dataBytes / byteRate;
                        duration = TimeSpan.FromSeconds(durationInSecs);
                        if (wamdData != null) wamdData.duration = durationInSecs;
                        result = result + @"
Duration: " + new TimeSpan((long) (durationInSecs * 10000000L)).ToString(@"hh\:mm\:ss\.ff");
                    }
                }
            }
            catch (Exception ex)
            {
                if(ex.Message.Contains("used by another process"))
                {
                    
                }
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }

            return result;
        }

        /// <summary>
        ///     Given a 'chunk' of metadata from a wav file wamd chunk
        ///     which is everything after the wamd header and size attribute,
        ///     extracts the Name and Note fields and assembles them into a
        ///     'pseudo'Audacity comment label field using start and end for
        ///     the time parameters.
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        private static WAMD_Data decode_wamd_data(byte[] metadata)
        {
            var entries = new List<Tuple<short, string>>();
            var result = new WAMD_Data();
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
                        entries.Add(new Tuple<short, string>(type, data));
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        Debug.WriteLine(ex);
                    }
            }

            var wamdData = new WAMD_Data();

            foreach (var entry in entries) wamdData.item = entry;

            result = wamdData;

            return result;
        }

        internal string GetGuanoData(int currentRecordingSessionId, string wavfile)
        {
            if (currentRecordingSessionId < 0) return "";
            if (string.IsNullOrWhiteSpace(wavfile)) return "";
            var sessionNotes = DBAccess.GetRecordingSessionNotes(currentRecordingSessionId);
            if (string.IsNullOrWhiteSpace(sessionNotes)) return "";
            if (!sessionNotes.Contains("[GUANO]")) return "";
            if (!File.Exists(wavfile) || (new FileInfo(wavfile).Length<=0L)) return "";
            byte[] metadata = null;
            WAMD_Data wamdData = null;
            var guanoData = Get_WAVFile_MetaData(wavfile, out metadata, out wamdData);
            return guanoData;
        }
    }
}