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
using System.Linq;
using System.Text.RegularExpressions;

namespace BatRecordingManager
{
    /// <summary>
    /// </summary>
    public class BatStats
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatStats" /> class.
        /// </summary>
        public BatStats()
        {
            maxDuration = TimeSpan.MinValue;
            minDuration = TimeSpan.MaxValue;
            meanDuration = new TimeSpan();
            totalDuration = new TimeSpan();
            count = 0;
            segments = 0;
            passes = 0;
            batCommonName = "";
            batAutoID = "";
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatStats" /> class.
        /// </summary>
        /// <param name="duration">
        ///     The duration.
        /// </param>
        public BatStats(TimeSpan duration)
        {
            maxDuration = TimeSpan.MinValue;
            minDuration = TimeSpan.MaxValue;
            meanDuration = new TimeSpan();
            totalDuration = new TimeSpan();
            count = 0;
            segments = 0;
            passes = 0;
            batCommonName = "";
            batAutoID = "";

            Add(duration, "");
        }

        public string batAutoID { get; set; }

        /// <summary>
        ///     Gets or sets the name of the bat common.
        /// </summary>
        /// <value>
        ///     The name of the bat common.
        /// </value>
        public string batCommonName { get; set; }

        /// <summary>
        ///     Gets or sets the count.
        /// </summary>
        /// <value>
        ///     The count.
        /// </value>
        public int count { get; set; }

        /// <summary>
        ///     Gets or sets the maximum.
        /// </summary>
        /// <value>
        ///     The maximum.
        /// </value>
        public TimeSpan maxDuration { get; set; }

        /// <summary>
        ///     Gets or sets the mean.
        /// </summary>
        /// <value>
        ///     The mean.
        /// </value>
        public TimeSpan meanDuration { get; set; }

        /// <summary>
        ///     Gets or sets the minimum.
        /// </summary>
        /// <value>
        ///     The minimum.
        /// </value>
        public TimeSpan minDuration { get; set; }

        /// <summary>
        ///     Gets or sets the passes.
        /// </summary>
        /// <value>
        ///     The passes.
        /// </value>
        public int passes { get; set; }

        /// <summary>
        ///     Gets or sets the passes.
        /// </summary>
        /// <value>
        ///     The passes.
        /// </value>
        public int segments { get; set; }

        /// <summary>
        ///     Gets or sets the total.
        /// </summary>
        /// <value>
        ///     The total.
        /// </value>
        public TimeSpan totalDuration { get; set; }

        /// <summary>
        ///     Adds the specified duration.
        /// </summary>
        /// <param name="duration">
        ///     The duration.
        /// </param>
        public void Add(TimeSpan duration, string AutoID)
        {
            if (duration.Ticks < 0) duration = -duration;
            if (duration.Ticks > 0)
            {
                segments++;
                if (duration.TotalSeconds <= 7.5d)
                {
                    passes++;
                }
                else
                {
                    var realPasses = duration.TotalSeconds / 5.0d;
                    passes += (int)Math.Round(realPasses);
                }

                count++;
                totalDuration += duration;
                if (duration > maxDuration) maxDuration = duration;
                if (duration < minDuration) minDuration = duration;
                meanDuration = new TimeSpan(totalDuration.Ticks / count);
            }
            if (!string.IsNullOrWhiteSpace(AutoID))
            {
                if (!(batAutoID?.Contains(AutoID) ?? false))
                {
                    batAutoID = (batAutoID ?? "") + "; " + AutoID;
                }
            }
        }

        /// <summary>
        ///     Adds all the listed segments to the stat
        /// </summary>
        /// <param name="segList"></param>
        public void Add(IEnumerable<LabelledSegment> segList)
        {
            if (!segList.IsNullOrEmpty())
                foreach (var segment in segList.Distinct())
                {
                    Add(segment.EndOffset - segment.StartOffset, segment.AutoID);
                }
        }

        /// <summary>
        ///     Adds the specified new data.
        /// </summary>
        /// <param name="newData">
        ///     The new data.
        /// </param>
        public void Add(BatStats newData)
        {
            // if both old and new have the same name, OK if neither old nor new have name, OK if
            // the new has name but the old doesnt't, use the new name if both have names but they
            // are different, don't do the Add

            if (!string.IsNullOrWhiteSpace(newData.batCommonName))
            {
                if (string.IsNullOrWhiteSpace(batCommonName))
                {
                    batCommonName = newData.batCommonName;
                }
                else
                {
                    if (batCommonName != newData.batCommonName) return;
                }
            }

            if (!string.IsNullOrEmpty(newData.batAutoID))
            {
                string newAutoID = newData.batAutoID;
                if (newAutoID.StartsWith(";")) newAutoID = newAutoID.Substring(1).Trim();

                batAutoID = incrementListCount(batAutoID, newAutoID); // either adds the new string or incrments the counter adjacent to the string
            }

            if (newData != null && newData.count > 0)
            {
                if (newData.maxDuration > maxDuration) maxDuration = newData.maxDuration;
                if (newData.minDuration < minDuration) minDuration = newData.minDuration;
                count += newData.count;
                segments += newData.segments;
                passes += newData.passes;
                totalDuration += newData.totalDuration;
                meanDuration = new TimeSpan(totalDuration.Ticks / count);
            }
        }

        /// <summary>
        /// given a string of bat names, some followed by a count in brackets, checks to see if the new bat is in the list
        /// and if so either adds or incrments the count in brackets, otherwise adds the new string to the ; separated list.
        /// </summary>
        /// <param name="batAutoID"></param>
        /// <param name="newAutoID"></param>
        /// <returns></returns>
        private string incrementListCount(string batAutoID, string newAutoID)
        {
            if (string.IsNullOrWhiteSpace(batAutoID)) return (newAutoID + ";");
            if (!batAutoID.Contains(newAutoID))
            {
                return (batAutoID + " " + newAutoID + ";"); // get rid of the simple case
            }
            string pattern = $@"({newAutoID})\(?([0-9]*)?\)?";
            var match = Regex.Match(batAutoID, pattern);
            if (match.Success)
            {
                string replacement = newAutoID;
                if (match.Groups.Count >= 3)
                {
                    if (int.TryParse(match.Groups[2].Value, out int num))
                    {
                        num++;
                        replacement = $"{newAutoID}({num})";
                    }
                    else
                    {
                        if (match.Groups.Count >= 2)
                        {
                            replacement = $"{newAutoID}(2)";
                        }
                    }
                }
                batAutoID = Regex.Replace(batAutoID, pattern, replacement);
            }
            else
            {
                // should not really get here!
                Debug.WriteLine($"+++Regex failed to find {newAutoID} in {batAutoID}");
                return (batAutoID + " " + newAutoID);
            }
            return (batAutoID);
        }

        /*
        /// <summary>
        ///     Passeses this instance.
        /// </summary>
        /// <returns>
        ///     </returns>
        internal int Passes()
        {
            return (passes);
        }*/
    }
}