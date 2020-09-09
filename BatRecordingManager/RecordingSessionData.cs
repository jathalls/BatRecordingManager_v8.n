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
using System.ComponentModel;

namespace BatRecordingManager
{
    /// <summary>
    ///     Specialised class to hold the data displayed by RecordingSessionListDetailControl.
    ///     Contains just the necessary items to display to allow for fast loading
    /// </summary>
    public class RecordingSessionData : INotifyPropertyChanged
    {
        public RecordingSessionData(int ID, string tag, string loc, DateTime startDate, TimeSpan? startTime,
            int numImages, int numRecordings)
        {
            Id = ID;
            SessionTag = tag;
            Location = loc;
            SessionStartDate = startDate;
            StartTime = startTime;
            NumberOfRecordingImages = numImages;
            NumberOfRecordings = numRecordings;
        }

        public RecordingSessionData()
        {
            Id = -1;
            SessionTag = "";
            Location = "";
            SessionStartDate = new DateTime();
            StartTime = null;
            NumberOfRecordingImages = 0;
            NumberOfRecordings = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Id
        {
            get => _Id;
            set
            {
                _Id = value;
                Pc("Id");
            }
        }

        /// <summary>
        ///     Location of the session
        /// </summary>
        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                Pc("Location");
            }
        }

        public bool multiDaySession
        {
            get
            {
                if (_multiDaySession == null)
                {
                    RecordingSession session = DBAccess.GetRecordingSession(Id);
                    if (session != null)
                    {
                        if (session.EndDate == null)
                        {
                            _multiDaySession = false;
                            Pc(nameof(multiDaySession));
                            return (_multiDaySession ?? false);
                        }
                        if (session.SessionDate.Date == session.EndDate.Value.Date)
                        {
                            _multiDaySession = false;
                            Pc(nameof(multiDaySession));
                            return (_multiDaySession ?? false);
                        }
                        if (session.SessionDate.Date.AddDays(1) == session.EndDate.Value.Date && session.SessionDate.TimeOfDay.Hours >= 12 &&
                            (session.SessionEndTime ?? new TimeSpan()).Hours < 12)
                        {
                            _multiDaySession = false;
                            Pc(nameof(multiDaySession));
                            return (_multiDaySession ?? false);
                        }

                        // by this point we have eliminated all single day sessions and sessions from one evening to the next morning
                        _multiDaySession = true;
                        Pc(nameof(multiDaySession));
                    }
                }
                return (_multiDaySession ?? false);
            }
        }

        /// <summary>
        ///     The number of images associated with recordings of this session
        /// </summary>
        public int? NumberOfRecordingImages
        {
            get => _numberOfRecordingImages;
            set
            {
                _numberOfRecordingImages = value;
                Pc("NumberOfRecordingImages");
            }
        }

        /// <summary>
        ///     The number of recordings that are part of this session
        /// </summary>
        public int NumberOfRecordings
        {
            get => _numberOfRecordings;
            set
            {
                _numberOfRecordings = value;
                Pc("NumberOfRecordings");
            }
        }

        /// <summary>
        ///     Date and time at which the session started
        /// </summary>
        public DateTime SessionStartDate
        {
            get => _sessionStartDate;
            set
            {
                _sessionStartDate = value;
                Pc("SessionStartDate");
            }
        }

        /// <summary>
        ///     Tag from the RecordingSession
        /// </summary>
        public string SessionTag
        {
            get => _sessionTag;
            set
            {
                _sessionTag = value;
                Pc("SessionTag");
            }
        }

        /// <summary>
        ///     The optional time of the start of the session
        /// </summary>
        public TimeSpan? StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                Pc("StartTime");
            }
        }

        private int _Id = -1;
        private string _location;
        private bool? _multiDaySession = null;
        private int? _numberOfRecordingImages;
        private int _numberOfRecordings;
        private DateTime _sessionStartDate;
        private string _sessionTag;
        private TimeSpan? _startTime;

        private void Pc(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}