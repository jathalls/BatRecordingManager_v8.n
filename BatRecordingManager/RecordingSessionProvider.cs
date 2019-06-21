/*
 *  Copyright 2016 Justin A T Halls

        Licensed under the Apache License, Version 2.0 (the "License");
        you may not use this file except in compliance with the License.
        You may obtain a copy of the License at

            http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS,
        WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        See the License for the specific language governing permissions and
        limitations under the License.

 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataVirtualizationLibrary;

namespace BatRecordingManager
{
    internal class RecordingSessionProvider : IItemsProvider<RecordingSession>
    {
        private int _count;
        private string _sortColumn;


        public RecordingSessionProvider()
        {
            _count = DBAccess.GetRecordingSessionListCount();
        }

        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            return _count;
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingSessionCount();
        }

        public IList<RecordingSession> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("Sess FetchRange: " + startIndex + ", " + count);
            var sessionList = new List<RecordingSession>();
            var page = DBAccess.GetPagedRecordingSessionList(count, startIndex, _sortColumn);
            if (page != null) sessionList.AddRange(page.ToList());
            return sessionList;
        }

        public RecordingSession Default()
        {
            return new RecordingSession();
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }
    }

    /// <summary>
    ///     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    internal class RecordingSessionDataProvider : IItemsProvider<RecordingSessionData>
    {
        private int _count;
        private string _sortColumn;

        public RecordingSessionDataProvider()
        {
            _count = DBAccess.GetRecordingSessionDataCount();
        }

        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            return _count;
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingSessionDataCount();
        }

        public IList<RecordingSessionData> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("RSD FetchRange: " + startIndex + ", " + count);
            var sessionList = new List<RecordingSessionData>();
            var page = DBAccess.GetPagedRecordingSessionDataList(count, startIndex, _sortColumn);
            if (page != null) sessionList.AddRange(page.ToList());
            return sessionList;
        }

        public RecordingSessionData Default()
        {
            return new RecordingSessionData();
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }
    }

    /// <summary>
    ///     ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    internal class BatSessionRecordingDataProvider : IItemsProvider<BatSessionRecordingData>
    {
        private readonly List<int> _batIdList;
        private readonly int _count;
        private readonly List<int> _sessionIdList;
        private string _sortColumn;

        public BatSessionRecordingDataProvider(List<int> batIdList, List<int> sessionIdList, int count = -1)
        {
            //_count = DBAccess.GetBatSessionRecordingDataCount(batIdList, sessionIdList);

            _batIdList = batIdList;
            _sessionIdList = sessionIdList;
            if (count >= 0) _count = count;
            Trace.WriteLine("new Provider for BSRD " + _count + "recordings in total");
        }

        public int FetchCount()
        {
            Trace.WriteLine("BSRDP FetchCount");
            return _count;
        }

        public void RefreshCount()
        {
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
            catch (Exception ex)
            {
                Debug.WriteLine("BSRDP Error fetching BatSessionrecordingData " + startIndex + " to " +
                                (startIndex + count));
            }

            return sessionList;
        }

        public BatSessionRecordingData Default()
        {
            return new BatSessionRecordingData();
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }
    }

    /// <summary>
    ///     ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    internal class RecordingProvider : IItemsProvider<Recording>
    {
        private int _count;
        private string _sortColumn;

        public RecordingProvider()
        {
            _count = DBAccess.GetRecordingListCount();
        }

        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            return _count;
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingListCount();
        }

        public IList<Recording> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("Rec FetchRange: " + startIndex + ", " + count);
            var recordingList = new List<Recording>();
            var page = DBAccess.GetPagedRecordingList(count, startIndex, _sortColumn);
            if (page != null) recordingList.AddRange(page.ToList());
            return recordingList;
        }

        public Recording Default()
        {
            return new Recording();
        }

        public void SetSortColumn(string column)
        {
            _sortColumn = column;
        }
    }
}