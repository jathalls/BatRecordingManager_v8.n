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
using System.Linq;
using System.Text.RegularExpressions;

namespace BatRecordingManager
{
    /// <summary>
    ///     A class to hold a single call parameter in the form of a Mean and
    ///     variation .  The mean is the avergae of the maximum and minimum values
    ///     and the variation is the difference ebtween the mean and the max.
    /// </summary>
    internal class Parameter
    {
        public double Mean;
        public double Variation;

        public double Max => Mean + Variation;

        public double Min => Mean - Variation;

        /// <summary>
        ///     Given a range in terms of maximum and minimum values, stores them in the
        ///     Parameter class as Mean and Variation and returns Tuple of Doubles containing
        ///     the resultant Mean and Variation.
        /// </summary>
        /// <param name="max">the maximum value in the range</param>
        /// <param name="min">the minimum value in the range</param>
        /// <returns>A Tuple containing the Mean and Variation</returns>
        public Tuple<double, double> SetMaxMin(double max, double min)
        {
            Mean = (max + min) / 2.0d;
            Variation = max - Mean;
            return new Tuple<double, double>(Mean, Variation);
        }

        /// <summary>
        ///     Given a string, starting with a digit, extract the single or paired
        ///     numeric parameters as an instance of the Parameter class
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal static Parameter GetNumericParameterFromString(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            if (!char.IsDigit(v[0])) return null;
            if (v.Contains("+/-"))
                return GetNumericMeanAndVariationFromString(v);
            if (v.Contains("-"))
                return GetNumericRangeFromString(v);
            return GetSingleNumericFromString(v);
        }

        /// <summary>
        ///     Creates a parameter instance containing meand and variation
        ///     from a string in the form 'mean +/- variation'
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private static Parameter GetNumericMeanAndVariationFromString(string v)
        {
            Parameter result = null;
            var parts = v.Split('/');
            if (parts.Length > 1)
            {
                parts[0] = parts[0].Substring(0, parts[0].Length - 1).Trim();
                parts[1] = parts[1].Substring(1).Trim();
                double.TryParse(parts[0], out var mean);
                double.TryParse(parts[1], out var variation);
                result = new Parameter { Mean = mean, Variation = variation };
            }

            return result;
        }

        /// <summary>
        ///     creates a Parameter Instance containing min and max from a string
        ///     in the format 'min - max'
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private static Parameter GetNumericRangeFromString(string v)
        {
            Parameter result = null;
            var parts = v.Split('-');
            if (parts.Length > 1)
            {
                double.TryParse(parts[0].Trim(), out var min);
                double.TryParse(parts[1].Trim(), out var max);
                result = new Parameter();
                result.SetMaxMin(max, min);
            }

            return result;
        }

        /// <summary>
        ///     Creates a parameter instance containing a single mean value from a string
        ///     in the form 'value'
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private static Parameter GetSingleNumericFromString(string v)
        {
            Parameter result = null;
            if (double.TryParse(v.Trim(), out var value))
            {
                result = new Parameter { Mean = value, Variation = 0.0 };
            }

            return result;
        }
    }

    /// <summary>
    ///     A class to hold a full set of call prarameters for start, end and
    ///     peak frequency in kHz and the duration and interval in ms.
    /// </summary>
    internal class ParameterSet
    {
        public Parameter Duration = new Parameter();
        public Parameter EndFrequency = new Parameter();
        public Parameter Interval = new Parameter();
        public string Notes = "";
        public Parameter PeakFrequency = new Parameter();
        public Parameter StartFrequency = new Parameter();

        /// <summary>
        ///     Parameter set can be initialised with a comment string from a
        ///     LabelledSegment which may or may not contain a set of parameters
        ///     enclosed in curly braces.
        /// </summary>
        /// <param name="label"></param>
        public ParameterSet(string label)
        {
            call = null;
            HasCallParameters = false;
            if (!string.IsNullOrWhiteSpace(label))
            {
                label = label.Trim();
                if (label.Contains("{") && label.Contains("}"))
                    call = InsertParamsFromLabel(label);
                if (label.Contains("?") || label.Contains("Confidence L") || label.EndsWith("L") || label.EndsWith("L}"))
                {
                    if (call == null)
                    {
                        call = new Call() { CallType = "LC", CallNotes = "" };
                    }
                    else
                    {
                        call.CallType = (call.CallType + " LC").Trim();
                        HasCallParameters = true;
                    }
                }
            }
        }

        public Call call
        {
            get
            {
                if (HasCallParameters) return _call;
                return null;
            }
            set
            {
                _call = value;
                if (_call != null && _call.Validate())
                    HasCallParameters = true;
                else
                    HasCallParameters = false;
            }
        }

        public bool HasCallParameters { get; set; }
        private Call _call;

        /// <summary>
        ///     Inserts the parameters from comment. Creates a new Call and populates it from the
        ///     comment string provided as {start,end,peak,duration,interval,type,function,notes}
        ///     Incidentally clears out any Call items in the database which are not also present in
        ///     a link table.
        ///     parameters may be in the form {s=start,e=end,p=peak ...
        ///     in which case they may come in any order and are no longer position dependant
        /// </summary>
        /// <param name="label"></param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <returns>
        /// </returns>
        /// <summary>
        ///     Given a string containing a named call parameter in the form
        ///     'sf=values' extracts the parameters and adds them to the passed
        ///     Call instance, returning it.
        ///     Identifiers are:-
        ///     s* = start frequency
        ///     e*=end frpequency
        ///     p*=peak frequency
        ///     i*=pulse interval
        ///     d*=pulse duration
        ///     Notes should not be labelled and should always be the last field in the parameters block
        /// </summary>
        /// <param name="newCall"></param>
        /// <param name="i"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private Call GetNamedParamFromString(Call newCall, string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return newCall;
            var parts = v.Split('=');
            if (parts.Length > 1)
            {
                var val = Parameter.GetNumericParameterFromString(parts[1].Trim());
                if (val != null)
                    switch (parts[0].ToUpper()[0]) // the first char of the uppercased string parts[0]
                    {
                        case 'S':
                            newCall.StartFrequency = val.Mean;
                            newCall.StartFrequencyVariation = val.Variation;
                            break;

                        case 'E':
                            newCall.EndFrequency = val.Mean;
                            newCall.EndFrequencyVariation = val.Variation;
                            break;

                        case 'P':
                            newCall.PeakFrequency = val.Mean;
                            newCall.PeakFrequencyVariation = val.Variation;
                            break;

                        case 'D':
                            newCall.PulseDuration = val.Mean;
                            newCall.PulseDurationVariation = val.Variation;
                            break;

                        case 'I':
                            newCall.PulseInterval = val.Mean;
                            newCall.PulseIntervalVariation = val.Variation;
                            break;
                    }
            }

            return newCall;
        }

        /// <summary>
        ///     Gets a comme delimited segment from a calls parameter section of a
        ///     labelled comment.  The parameter may be in the form 'param=value' or
        ///     may be defined by its position in the sequence
        /// </summary>
        /// <param name="newCall"></param>
        /// <param name="i"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private Call GetParamFromString(Call newCall, int i, string v)
        {
            if (newCall == null) newCall = new Call();
            if (string.IsNullOrWhiteSpace(v)) return newCall;
            if (v.Contains("=")) return GetNamedParamFromString(newCall, v);

            if (char.IsDigit(v[0]))
            {
                var val = Parameter.GetNumericParameterFromString(v);
                if (val != null)
                    switch (i)
                    {
                        case 0:
                            newCall.StartFrequency = val.Mean;
                            newCall.StartFrequencyVariation = val.Variation;
                            break;

                        case 1:
                            newCall.EndFrequency = val.Mean;
                            newCall.EndFrequencyVariation = val.Variation;
                            break;

                        case 2:
                            newCall.PeakFrequency = val.Mean;
                            newCall.PeakFrequencyVariation = val.Variation;
                            break;

                        case 3:
                            newCall.PulseDuration = val.Mean;
                            newCall.PulseDurationVariation = val.Variation;
                            break;

                        case 4:
                            newCall.PulseInterval = val.Mean;
                            newCall.PulseIntervalVariation = val.Variation;
                            break;

                        case 5:
                            if (newCall.CallType == null) newCall.CallType = "";
                            newCall.CallType = newCall.CallType + v;
                            break;

                        case 6:
                            if (newCall.CallFunction == null) newCall.CallFunction = "";
                            newCall.CallFunction = newCall.CallFunction + v;
                            break;

                        default:
                            if (newCall.CallNotes == null) newCall.CallNotes = "";
                            newCall.CallNotes = newCall.CallNotes + v + " ";
                            break;
                    }
            }
            else // char[0] is not digit
            {
                // plain parameter field does not start with a digit, could be type, function or notes
                // function still defined by position - last item is assumed to be a note
                if (MatchesExistingCallType(v))
                {
                    newCall.CallType = v;
                }
                else if (MatchesExistingCallFunction(v))
                {
                    newCall.CallFunction = v;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(newCall.CallNotes)) newCall.CallNotes = "";
                    newCall.CallNotes = newCall.CallNotes.Trim() + v + " ";
                }
            }

            return newCall;
        }

        private Call InsertParamsFromLabel(string label)
        {
            Call newCall = null;
            var callParamRegex = new Regex("\\{.+\\}");
            if (!string.IsNullOrWhiteSpace(label))
            {
                var result = callParamRegex.Match(label);
                if (result.Success) newCall = InsertParamsFromParameterSection(result.Value);
            }

            return newCall;
        }

        private Call InsertParamsFromParameterSection(string value)
        {
            Call newCall = null;
            value = value.Replace('{', ' ');
            value = value.Replace('}', ' ').Trim();
            var allParams = value.Split(',');
            for (var i = 0; i < allParams.Length; i++) newCall = GetParamFromString(newCall, i, allParams[i].Trim());
            if (string.IsNullOrWhiteSpace(newCall.CallNotes))
            {
                var lastparam = allParams[allParams.Length - 1];
                if (!char.IsDigit(lastparam[0]) && !lastparam.Contains("="))
                    newCall.CallNotes = allParams[allParams.Length - 1];
            }

            return newCall;
        }

        /// <summary>
        ///     Given a string checks to see if that string has previously been used as a
        ///     call function type and if so return True
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool MatchesExistingCallFunction(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return false;
            var dc = DBAccess.GetFastDataContext();

            var x = (from cp in dc.Calls
                     where cp.CallFunction.Trim().ToUpper() == v.Trim().ToUpper()
                     select cp).Count();
            if (x > 0) return true;

            return false;
        }

        /// <summary>
        ///     Given a string, checks to see if that string has peviously been used as a
        ///     call type and if so return true
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool MatchesExistingCallType(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return false;
            var dc = DBAccess.GetFastDataContext();

            var x = (from cp in dc.Calls
                     where cp.CallType.Trim().ToUpper() == v.Trim().ToUpper()
                     select cp).Count();
            if (x > 0) return true;

            return false;
        }
    }
}