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

using DataVirtualizationLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BatRecordingManager
{
    /// <summary>
    ///     ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    internal class BatSessionRecordingDataProvider : IItemsProvider<BatSessionRecordingData>
    {
        public BatSessionRecordingDataProvider(List<int> batIdList, List<int> sessionIdList, int count = -1)
        {
            //_count = DBAccess.GetBatSessionRecordingDataCount(batIdList, sessionIdList);

            _batIdList = batIdList;
            _sessionIdList = sessionIdList;
            if (count >= 0) _count = count;
            Trace.WriteLine("new Provider for BSRD " + _count + "recordings in total");
        }

        public BatSessionRecordingData Default()
        {
            return new BatSessionRecordingData();
        }

        public int FetchCount()
        {
            Trace.WriteLine("BSRDP FetchCount");
            return _count;
        }

        public IList<BatSessionRecordingData> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("BSRD FetchRange: " + startIndex + ", " + count);
            var sessionList = new List<BatSessionRecordingData>();
            try
            {
                var page = DBAccess.GetPagedBatSessionRecordingData(_batIdList, _sessionIdList, startIndex, count);

                if (page != null)
                {
                    var list = page.ToList();

                    sessionList.AddRange(list);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("BSRDP Error fetching BatSessionrecordingData " + startIndex + " to " +
                                (startIndex + count));
            }

            return sessionList;
        }

        public void RefreshCount()
        {
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }

        private readonly List<int> _batIdList;
        private readonly int _count;
        private readonly List<int> _sessionIdList;
        private string _sortColumn;
    }

    /// <summary>
    ///     ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    internal class RecordingProvider : IItemsProvider<Recording>
    {
        public RecordingProvider()
        {
            _count = DBAccess.GetRecordingListCount();
        }

        public Recording Default()
        {
            return new Recording();
        }

        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            return _count;
        }

        public IList<Recording> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("Rec FetchRange: " + startIndex + ", " + count);
            var recordingList = new List<Recording>();
            var page = DBAccess.GetPagedRecordingList(count, startIndex, _sortColumn);
            if (page != null) recordingList.AddRange(page.ToList());
            return recordingList;
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingListCount();
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }

        private int _count;
        private string _sortColumn;
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    internal class RecordingsDataProvider : IItemsProvider<Recording>
    {
        public RecordingsDataProvider(RecordingSession session)
        {
            this.session = session;
            _count = 0;
            if (session != null && !session.Recordings.IsNullOrEmpty())
            {
                _count = session.Recordings.Count;
            }
        }

        public Recording Default()
        {
            return new Recording();
        }

        public int FetchCount()
        {
            Trace.WriteLine($"FetchCount Recordings {_count}");
            return _count;
        }

        public IList<Recording> FetchRange(int startIndex, int numberRequested)
        {
            Trace.WriteLine("Recording FetchRange: " + startIndex + ", " + numberRequested);
            var RecsList = new List<Recording>();
            if (session != null && !session.Recordings.IsNullOrEmpty())
            {
                if (_count > startIndex + numberRequested) // we have plenty
                {
                    RecsList.AddRange(session.Recordings.Skip(startIndex).Take(numberRequested));
                }
                else if (_count > startIndex) // we have more than the start but not enough beyond that for the requested
                {
                    RecsList.AddRange(session.Recordings.Skip(startIndex));
                }
                //else if (_count > numberRequested) // we dont have as many as the startIndex, so return none
                //{
                //RecsList.AddRange(session.Recordings.Take(numberRequested));
                //}
                //else
                //{
                //    RecsList.AddRange(session.Recordings);
                //}
            }
            //else
            //{
            //RecsList.Add(new Recording());
            //}

            return RecsList;
        }

        public void RefreshCount()
        {
            if (session != null && !session.Recordings.IsNullOrEmpty())
            {
                _count = session.Recordings.Count;
            }
            else
            {
                _count = 0;
            }
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }

        private int _count;
        private string _sortColumn;
        private RecordingSession session = null;
    }

    /// <summary>
    ///     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    internal class RecordingSessionDataProvider : IItemsProvider<RecordingSessionData>
    {
        public RecordingSessionDataProvider()
        {
            _count = DBAccess.GetRecordingSessionDataCount();
        }

        public RecordingSessionData Default()
        {
            return new RecordingSessionData();
        }

        public int FetchCount()
        {
            Trace.WriteLine($"FetchCount SessionData {_count}");
            return _count;
        }

        public IList<RecordingSessionData> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("RSD FetchRange: " + startIndex + ", " + count);
            var sessionList = new List<RecordingSessionData>();
            var page = DBAccess.GetPagedRecordingSessionDataList(count, startIndex, _sortColumn);
            if (page != null) sessionList.AddRange(page.ToList());
            return sessionList;
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingSessionDataCount();
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }

        private int _count;
        private string _sortColumn;
    }

    internal class RecordingSessionProvider : IItemsProvider<RecordingSession>
    {
        public RecordingSessionProvider()
        {
            _count = DBAccess.GetRecordingSessionListCount();
        }

        public RecordingSession Default()
        {
            return new RecordingSession();
        }

        public int FetchCount()
        {
            Trace.WriteLine($"FetchCount Sessions {_count}");
            return _count;
        }

        public IList<RecordingSession> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("Sess FetchRange: " + startIndex + ", " + count);
            var sessionList = new List<RecordingSession>();
            var page = DBAccess.GetPagedRecordingSessionList(count, startIndex, _sortColumn);
            if (page != null) sessionList.AddRange(page.ToList());
            return sessionList;
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingSessionCount();
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }

        private int _count;
        private string _sortColumn;
    }
}