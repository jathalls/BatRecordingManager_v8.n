﻿using System;
using System.ComponentModel;

namespace BatRecordingManager
{
    /// <summary>
    ///     Specialised class to hold the data displayed by RecordingSessionListDetailControl.
    ///     Contains just the necessary items to display to allow for fast loading
    /// </summary>
    public class RecordingSessionData : INotifyPropertyChanged
    {
        private int _Id = -1;
        private string _location;
        private int? _numberOfRecordingImages;
        private int _numberOfRecordings;
        private DateTime _sessionStartDate;
        private string _sessionTag;
        private TimeSpan? _startTime;

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

        public event PropertyChangedEventHandler PropertyChanged;

        private void Pc(string property)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}