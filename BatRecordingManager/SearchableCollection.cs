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

using Microsoft.VisualStudio.Language.Intellisense;
using System;

namespace BatRecordingManager
{
    /// <summary>
    ///     The SearchableCollection class collates a set of strings which can be
    ///     searched using the Search dialog.  The data in the SearchableCollection
    ///     is in the form of an ObservableCollection of 3-Tuples(int,int,string).
    ///     Typically the collection can be used to assemble strings to be searched
    ///     from a set of recordings in which case the first integer is the index
    ///     of a recording in the list, the second integer is the index of LaberlledSegement
    ///     in the recording, and the string is the comment for the Labelled Segment.
    ///     If the segment index is -1 then the string could be the recording note.
    ///     Other schema within the bounds of an int,int,string collection are
    ///     permitted.
    /// </summary>
    internal class SearchableCollection
    {
        public SearchableCollection()
        {
            searchableCollection = new BulkObservableCollection<Tuple<int, int, string>>();
        }

        public BulkObservableCollection<Tuple<int, int, string>> searchableCollection { get; set; } =
            new BulkObservableCollection<Tuple<int, int, string>>();

        public void Add(int recordingIndex, int segmentIndex, string target)
        {
            searchableCollection.Add(new Tuple<int, int, string>(recordingIndex, segmentIndex, target));
        }

        public void AddRange(int recordingIndex, BulkObservableCollection<string> targetList)
        {
            if (targetList != null)
                for (var i = 0; i < targetList.Count; i++)
                    Add(recordingIndex, i, targetList[i]);
        }

        public void Clear()
        {
            for (var i = searchableCollection.Count - 1; i >= 0; i--) searchableCollection.RemoveAt(i);
        }

        public BulkObservableCollection<string> GetStringCollection()
        {
            var result = new BulkObservableCollection<string>();
            foreach (var item in searchableCollection) result.Add(item.Item3);
            return result;
        }
    }
}