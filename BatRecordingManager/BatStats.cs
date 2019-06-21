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
using System.Linq;

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

            Add(duration);
        }

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
        public void Add(TimeSpan duration)
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
                    passes += (int) Math.Round(realPasses);
                }

                count++;
                totalDuration += duration;
                if (duration > maxDuration) maxDuration = duration;
                if (duration < minDuration) minDuration = duration;
                meanDuration = new TimeSpan(totalDuration.Ticks / count);
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
                    Add(segment.EndOffset - segment.StartOffset);
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