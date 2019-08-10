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
using System.Data.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager
{
    /// <summary>
    ///     Static class of Database interface functions
    /// </summary>
    public static class DBAccess
    {
        public enum BracketedText
        {
            INCLUDE,
            EXCLUDE
        }

        private static BatReferenceDBLinqDataContext _persistentbatReferenceDataContext;

        internal static void FixSessionAndRecordingTimes()
        {

            BatReferenceDBLinqDataContext dc = GetDataContext();
            foreach (var session in dc.RecordingSessions)
            {
                UpdateRecordingSession(session,dc);
            }
        }

        private static bool _isDataContextUpToDate;


        /// <summary>
        ///     Inserts a bat or updates it
        /// </summary>
        /// <param name="bat"></param>
        /// <param name="dataContext"></param>
        /// <param name="newBat"></param>
        /// <returns></returns>
        public static string MergeBat(Bat bat, BatReferenceDBLinqDataContext dataContext, out Bat newBat)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            newBat = bat;
            var result = bat != null ? bat.Validate() : "No bat to validate";
            if (!string.IsNullOrWhiteSpace(result)) return result; // bat is not suitable for merging or insertion

            var existingBat = GetMatchingBat(bat, dataContext);
            if (existingBat == null) return InsertBat(bat, dataContext, out newBat);

            MergeTags(existingBat, bat, dataContext);
            existingBat.Notes = bat.Notes;
            existingBat.Name = bat.Name;
            existingBat.Batgenus = bat.Batgenus;
            existingBat.BatSpecies = bat.BatSpecies;
            existingBat.SortIndex = bat.SortIndex;
            dataContext.SubmitChanges();
            newBat = existingBat;

            return result;
        }

        internal static void FixRecordingLocationData()
        {
            /// TODO
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Goes through all the orphan images trying to associate them with a
        ///     bat or a recording based on the text in the caption.  If the caption
        ///     contains a bat tag the image will be associated with that bat through
        ///     a bat image link.  If the caption contans a .wav filename then the
        ///     Recordings table will be searched for a matching recording.  The image
        ///     will be associated with a labelled segment at 0:0-0:0 offset within that
        ///     recording.  The description field should contain further details.
        /// </summary>
        internal static void ResolveOrphanImages()
        {
            var dc = GetDataContext();

            var orphans = from bd in dc.BinaryDatas
                where (bd.BatPictures == null || bd.BatPictures.Count == 0)
                      && (bd.CallPictures == null || bd.CallPictures.Count == 0)
                      && (bd.SegmentDatas == null || bd.SegmentDatas.Count == 0)
                select bd;
            if (!orphans.IsNullOrEmpty())
                foreach (var orphan in orphans)
                    ResolveOrphan(orphan, dc);
            LinkBatsToSegmentZeros(dc);
        }

        private static void ResolveOrphan(BinaryData orphan, BatReferenceDBLinqDataContext dc)
        {
            var filename = "";
            var startOffsetString = "";
            var endOffsetString = "";
            var start = new TimeSpan();
            var end = new TimeSpan();
            var si = StoredImage.CreateFromBinary(orphan);

            var pattern = @"(.*(\.wav|\.WAV))\s?([0-9.]*)?\s*-?\s*([0-9.]*)?";
            var match = Regex.Match(si.caption, pattern);
            if (match.Success)
            {
                if (match.Groups.Count > 0) filename = match.Groups[1].Value;
                if (match.Groups.Count > 2)
                {
                    startOffsetString = match.Groups[3].Value;
                    if (int.TryParse(startOffsetString, out var secs)) start = TimeSpan.FromSeconds(secs);
                }

                if (match.Groups.Count > 3)
                {
                    endOffsetString = match.Groups[4].Value;
                    if (int.TryParse(endOffsetString, out var secs)) end = TimeSpan.FromSeconds(secs);
                }
            }

            if (!string.IsNullOrWhiteSpace(filename))
            {
                var recording = GetRecordingForWavFile(filename, dc);
                if (recording != null)
                {
                    var segment = GetOrCreateLabelledSegment(recording, start, end, si.description, dc);
                    AddImage(segment, orphan);
                }
            }
            else
            {
                var batList = GetDescribedBats(si.caption, BracketedText.INCLUDE);
                if (!batList.IsNullOrEmpty()) batList[0].AddImage(orphan);
            }
        }

        /// <summary>
        ///     Adds an image to the specified labelled segment.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="orphan"></param>
        private static void AddImage(LabelledSegment segment, BinaryData orphan)
        {
            var dc = GetDataContext();
            if (segment.StartOffset == segment.EndOffset && segment.StartOffset.Milliseconds == 0)
            {
                // we are adding an image to the Recording via its time zero segment
                var imageDescription = orphan.Description.Contains("$")
                    ? orphan.Description.Substring(orphan.Description.IndexOf("$"))
                    : orphan.Description;
                imageDescription = imageDescription.Replace("$", " ").Trim();
                segment.Comment = Tools.AdjustBracketedText(segment.Comment + imageDescription) + ";";
            }

            if (orphan.Id <= 0)
            {
                dc.BinaryDatas.InsertOnSubmit(orphan);
                dc.SubmitChanges();
            }

            if (segment.Id <= 0)
            {
                dc.LabelledSegments.InsertOnSubmit(segment);
                dc.SubmitChanges();
            }

            var sd = new SegmentData {SegmentId = segment.Id, BinaryDataId = orphan.Id};
            dc.SegmentDatas.InsertOnSubmit(sd);
            dc.SubmitChanges();
        }

        /// <summary>
        ///     given a recording and start and end times for a Labelled segment, looks for an existing segment which is close to
        ///     those
        ///     offsets, and if one cannto be found then creates one using the given start and offset times an the description as a
        ///     comment.
        ///     Adds the segment tothe database and the eventual segment is returned.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private static LabelledSegment GetOrCreateLabelledSegment(Recording recording, TimeSpan start, TimeSpan end,
            string description, BatReferenceDBLinqDataContext dc)
        {
            var variances = new List<Tuple<LabelledSegment, double>>();
            if (recording != null)
            {
                if (!recording.LabelledSegments.IsNullOrEmpty())
                {
                    foreach (var segment in recording.LabelledSegments)
                    {
                        if (segment.StartOffset <= start && segment.EndOffset >= start)
                            return segment; // if start is inside the segment return it
                        if (segment.EndOffset >= end && segment.StartOffset <= end)
                            return segment; // if end is inside the segment return it
                        if (start < segment.StartOffset && end > segment.EndOffset)
                            return segment; // if the entire segment is between start and end return it
                        var variance =
                            Math.Sqrt(Math.Pow(segment.StartOffset.TotalMilliseconds - start.TotalMilliseconds, 2.0d) +
                                      Math.Pow(segment.EndOffset.TotalMilliseconds - end.TotalMilliseconds, 2.0d));
                        variances.Add(new Tuple<LabelledSegment, double>(segment, variance));
                    }

                    var bestFit = variances
                        .FirstOrDefault(var => var.Item2 == variances.Min(minvar => minvar.Item2));
                    if (bestFit.Item2 < 7000.0d) return bestFit.Item1;
                }

                if (start.TotalMilliseconds <= 10 && end.Milliseconds <= 10 || start == end)
                {
                    var res = recording.GetSegmentZero(dc);
                    // add or adjust curly braces as a corrective measure - should have been done at creation
                    res.Comment = Tools.AdjustBracketedText(res.Comment);

                    return res;
                }

                var result = new LabelledSegment {StartOffset = start, EndOffset = end};
                description = Tools.AdjustBracketedText(description);

                result.Comment = description;
                recording.AddLabelledSegment(result, dc);
                return result;
            }

            return null;
        }

        /// <summary>
        ///     Corrective method to ensure that SegmentZeros which have a batTag in the description are linked
        ///     to the bat as well as to an image.  New additions will do this automatically but old entries did not
        ///     do this and need tobe updated and image modifications may not automatically make the additional links.
        ///     This is a repair process invoked when orphan images are resolved.
        /// </summary>
        /// <param name="dc"></param>
        private static void LinkBatsToSegmentZeros(BatReferenceDBLinqDataContext dc)
        {
            //remove all duplicate batsegment links
            var duplicateBsLs = dc.BatSegmentLinks.Where(bsl => dc.BatSegmentLinks.Count(
                                                                    bsl2 => bsl.BatID == bsl2.BatID &&
                                                                            bsl.LabelledSegmentID ==
                                                                            bsl2.LabelledSegmentID) > 1).Distinct();
            if (!duplicateBsLs.IsNullOrEmpty())
            {
                dc.BatSegmentLinks.DeleteAllOnSubmit(duplicateBsLs);
                dc.SubmitChanges();
            }

            var eligibleSegments = from seg in dc.LabelledSegments
                where !seg.BatSegmentLinks.Any()
                select seg;

            if (!eligibleSegments.IsNullOrEmpty())
            {
                // we have a list of LabelledSegments which do not have any batTags defined
                // this includes segmentZeros but also any other labelled segment with no associated bat
                foreach (var seg in eligibleSegments)
                {
                    var referredToBats = GetDescribedBats(seg.Comment, BracketedText.INCLUDE);

                    foreach (var bat in referredToBats)
                    {
                        var existingLinks =
                            dc.BatSegmentLinks.Any(lnk => lnk.BatID == bat.Id && lnk.LabelledSegmentID == seg.Id);
                        if (!existingLinks)
                        {
                            var bsl = new BatSegmentLink {BatID = bat.Id, LabelledSegmentID = seg.Id};
                            dc.BatSegmentLinks.InsertOnSubmit(bsl);
                        }
                    }
                }

                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Extension method ob Bat.AddImage
        ///     Given an instance of a BinaryData holding an image, the image is linked to
        ///     the Bat by an entry in the BatPictures link table
        /// </summary>
        /// <param name="bat"></param>
        /// <param name="imageData"></param>
        public static void AddImage(this Bat bat, BinaryData imageData)
        {
            try
            {
                var dc = GetDataContext();

                var existingLink = dc.BatPictures.Where(bp => bp.BatId == bat.Id && bp.BinaryDataId == imageData.Id);
                if (existingLink.IsNullOrEmpty())
                    if (imageData != null && bat.Id >= 0 && imageData.Id >= 0)
                    {
                        var bpLink = new BatPicture {BatId = bat.Id, BinaryDataId = imageData.Id};
                        dc.BatPictures.InsertOnSubmit(bpLink);
                        dc.SubmitChanges();
                    }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error adding an image to a bat:- " + ex.Message);
            }
        }

        /// <summary>
        ///     Extension method on Call.AddImage
        ///     Given an instance of a batCall and a binary data, links the image to the
        ///     call through the CallPicture Table
        /// </summary>
        /// <param name="call"></param>
        /// <param name="imageData"></param>
        public static void AddImage(this Call call, BinaryData imageData)
        {
            try
            {
                var dc = GetDataContext();

                var existingLink = dc.CallPictures.Where(bp => bp.CallID == call.Id && bp.BinaryDataID == imageData.Id);
                if (existingLink.IsNullOrEmpty())
                    if (imageData != null && call.Id >= 0 && imageData.Id >= 0)
                    {
                        var bpLink = new CallPicture {CallID = call.Id, BinaryDataID = imageData.Id};
                        dc.CallPictures.InsertOnSubmit(bpLink);
                        dc.SubmitChanges();
                    }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error adding an image to a call:- " + ex.Message);
            }
        }

        /// <summary>
        ///     Extension method on Recording.AddImage(BinaryData)
        ///     Given an instance of a BinaryData holding an image
        ///     the image is linked to segmentzero of the recording.  If the
        ///     recording does not have a segmentzero (i.e. a labelled segment with start
        ///     and end offsets of zero) then one is created through the GetSegmentZero
        ///     extension method.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="imageData"></param>
        public static void AddImage(this Recording recording, BinaryData imageData)
        {
            try
            {
                var dc = GetFastDataContext();

                if (imageData != null)
                {
                    var segmentZero = recording.GetSegmentZero(dc);
                    if (segmentZero != null)
                    {
                        segmentZero.Comment =
                            (segmentZero.Comment + " " + Tools.AdjustBracketedText(imageData.Description) + ";").Trim();

                        dc.SubmitChanges();
                        var sdLink = new SegmentData {SegmentId = segmentZero.Id, BinaryDataId = imageData.Id};
                        dc.SegmentDatas.InsertOnSubmit(sdLink);
                        dc.SubmitChanges();
                        var referredToBats = GetDescribedBats(imageData.Description, BracketedText.INCLUDE);
                        foreach (var bat in referredToBats)
                        {
                            var bsl = new BatSegmentLink {BatID = bat.Id, LabelledSegmentID = segmentZero.Id};
                            var existingLinks = (from lnk in dc.BatSegmentLinks
                                where lnk.BatID == bat.Id && lnk.LabelledSegmentID == segmentZero.Id
                                select lnk).Any();
                            if (!existingLinks)
                                // there is no existing link identical to the one we are about to add
                                dc.BatSegmentLinks.InsertOnSubmit(bsl);
                        }

                        dc.SubmitChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Add Image Error:- " + ex.Message);
            }
        }

        /// <summary>
        ///     gets a segment which has a start and end offset of 0 - this will be the dummy segment usedfor
        ///     attaching images to a recording rather than a specific user selected segment.
        ///     If there are no segment zeros, then a new one will be inserted into the database.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="dc"></param>
        /// <returns></returns>
        public static LabelledSegment GetSegmentZero(this Recording recording, BatReferenceDBLinqDataContext dc)

        {
            if (dc == null) dc = GetDataContext();

            LabelledSegment segmentZero = null;
            var segmentZeroList = from seg in recording.LabelledSegments
                where seg.StartOffset.Ticks == 0L && seg.EndOffset.Ticks == 0L
                select seg;
            if (!segmentZeroList.IsNullOrEmpty())
            {
                segmentZero = segmentZeroList.First();
            }
            else
            {
                segmentZero = new LabelledSegment
                {
                    StartOffset = new TimeSpan(0L),
                    EndOffset = new TimeSpan(0L),
                    Comment = "Recording Images:-",
                    RecordingID = recording.Id
                };
                dc.LabelledSegments.InsertOnSubmit(segmentZero);
                dc.SubmitChanges();
            }

            return segmentZero;
        }

        /// <summary>
        ///     Updates the labelled segments. Given an entity set of LabelledSegments (from a
        ///     Recording instance) updates the full set in the database, inserting where necessary
        ///     and parsing the comments for bat names and Updating all BatSegment links as required.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        /// <param name="listOfSegmentImageLists"></param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        public static void UpdateLabelledSegments(this Recording recording,
            BulkObservableCollection<BulkObservableCollection<StoredImage>> listOfSegmentImageLists,
            BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = GetDataContext();

            // if no segments, delete all existing segments for this recording id
            DeleteAllSegmentsInRecording(recording, dc);
            if (recording == null || recording.LabelledSegments.IsNullOrEmpty())
            {
            }
            else
            {
                // we do have some segments to update
                var bats = GetSortedBatList();
                for (var i = 0; i < recording.LabelledSegments.Count; i++)
                {
                    var segBatList =
                        SegmentAndBatList.ProcessLabelledSegment(
                            Tools.FormattedSegmentLine(recording.LabelledSegments[i]), bats);
                    UpdateLabelledSegment(segBatList, recording.Id, listOfSegmentImageLists[i], dc);
                }
            }
        }

        /// <summary>
        ///     Extension method on StoredImage.delete removes the image from the database
        /// </summary>
        /// <param name="image"></param>
        internal static void Delete(this StoredImage image)
        {
            DeleteBinaryData(image.ImageID);
        }

        /// <summary>
        ///     Returns a BulkObservableCollection of StoredImage which contains all images
        ///     i.e. BinaryDatas that do not have any associated links to bats, calls or segments
        ///     and are therefore considered to be orphans.  If the caption contains a fully qualified
        ///     name for a .wav file that exists, then the uri for that image is set equal to the caption.
        /// </summary>
        /// <returns></returns>
        internal static BulkObservableCollection<StoredImage> GetOrphanImages(BatReferenceDBLinqDataContext dc)
        {
            var result = new BulkObservableCollection<StoredImage>();
            if (dc == null) dc = GetDataContext();
            var orphans = from bd in dc.BinaryDatas
                where (bd.BatPictures == null || bd.BatPictures.Count == 0)
                      && (bd.CallPictures == null || bd.CallPictures.Count == 0)
                      && (bd.SegmentDatas == null || bd.SegmentDatas.Count == 0)
                select bd;
            if (!orphans.IsNullOrEmpty())
                foreach (var blob in orphans)
                {
                    var si = StoredImage.CreateFromBinary(blob);
                    var filename = si.caption.ExtractFilename(".wav");
                    if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
                        si.Uri = filename;
                    else
                        si.Uri = "";
                    result.Add(si);
                }

            return result;
        }

        /// <summary>
        ///     Validates the bat. Checkes to see if the required fields exist and are valid in
        ///     format and if so returns an empty string. Otherwise returns a string identifying
        ///     which fields are missing or incorrect.
        /// </summary>
        /// <param name="newBat">
        ///     The new bat.
        /// </param>
        /// <returns>
        /// </returns>
        public static string Validate(this Bat newBat)
        {
            var message = "";
            if (string.IsNullOrWhiteSpace(newBat.Name)) message = message + "Common Name required\n";

            if (string.IsNullOrWhiteSpace(newBat.BatSpecies))
                message = message + "Bat species required; use \'sp.\' if not known\n";

            if (string.IsNullOrWhiteSpace(newBat.Batgenus))
                message = message + "Bat genus required; use \'unknown\' if not known\n";
            if (newBat.BatTags == null || !newBat.BatTags.Any()) message = message + "At least one tag is requuired";

            return message;
        }

        /// <summary>
        ///     Validates the recording.  Confirms that the recording structure
        ///     contains valid and complete data, or returns a suitable
        ///     and informative error message.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string Validate(this Recording recording)
        {
            var result = "";
            if (string.IsNullOrWhiteSpace(recording.RecordingName))
                return "Recording Name (.wav file name) is required";
            if (!recording.RecordingName.ToUpper().EndsWith(".WAV")) return "Recording file must be of type .wav";

            return result;
        }

        /// <summary>
        ///     Adds the tag.
        /// </summary>
        /// <param name="tagText">
        ///     The tag text.
        /// </param>
        /// <param name="batId">
        ///     The bat identifier.
        /// </param>
        /// <returns>
        /// </returns>
        internal static int AddTag(string tagText, int batId)
        {
            if (string.IsNullOrWhiteSpace(tagText))
            {
                Tools.ErrorLog("null tag not saved to database: {" + tagText == null ? "NULL" : tagText + "}");
                return -1;
            }

            var dc = GetDataContext();
            var newTag = new BatTag {SortIndex = 0, BatTag1 = tagText};
            Bat batForTag = null;
            try
            {
                batForTag = (from bat in dc.Bats
                    where bat.Id == batId
                    select bat).SingleOrDefault();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("AddTag Failed to find bat {" + batId + "}" + ex.Message);
            }

            if (batForTag != null)
                try
                {
                    batForTag.BatTags.Add(newTag);
                    dc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("AddTag - Error adding new tag to database:- " + ex.Message);
                }

            //int index = DBAccess.ResequenceTags(newTag, dc);
            //DBAccess.ResequenceBats();
            var index = newTag.Id;
            return index;
        }

        /// <summary>
        ///     If the provided session exists in the database, then retrieve it and update
        ///     just the sunset field using the sunset data in the provided instance.
        /// </summary>
        /// <param name="recordingSession"></param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void UpdateSunset(RecordingSession recordingSession)
        {
            if (recordingSession == null || recordingSession.Id <= 0) return;
            try
            {
                var dc = GetDataContext();
                var session = (from sess in dc.RecordingSessions
                    where sess.Id == recordingSession.Id
                    select sess).SingleOrDefault();
                if (session != null)
                {
                    session.Sunset = recordingSession.Sunset;
                    dc.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }

        internal static void CloseDatabase()
        {
            if (!string.IsNullOrWhiteSpace(App.dbFileLocation) && !string.IsNullOrWhiteSpace(App.dbFileName))
            {
            }

            var dc = GetDataContext();

            dc.Connection.Close();
            dc.Connection.Dispose();
            dc.Dispose();
            dc = null;
            App.dbFileLocation = "";
            App.dbFileName = "";
            _persistentbatReferenceDataContext = null;
        }

        /// <summary>
        ///     Creates the database. Given a fully qualified file name, which must end with .mdf and
        ///     SHOULD end with BatReferenceDB.mdf and will be modified to so do, creates an new
        ///     instance of the bat reference database. It will be populated with default bat
        ///     reference species but no other data.
        /// </summary>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static string CreateDatabase(string fileName)
        {
            var err = "";
            if (!(fileName.EndsWith(".mdf") && fileName.Contains("BatReferenceDB")))
                if (fileName.EndsWith(".mdf"))
                {
                    fileName = fileName.Substring(0, fileName.Length - 4);
                    if (!fileName.EndsWith("BatReferenceDB")) fileName = fileName + "BatReferenceDB";
                    fileName = fileName + ".mdf";
                }

            if (File.Exists(fileName)) return "Cannot create - database named <" + fileName + "> already exists";

            //BatReferenceDBLinqDataContext batReferenceDataContext = new BatReferenceDBLinqDataContext(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=fileName;Integrated Security=False;Connect Timeout=60");
            var batReferenceDataContext =
                new BatReferenceDBLinqDataContext(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + fileName +
                                                  @";Integrated Security=False;Connect Timeout=60");
            if (batReferenceDataContext == null) return "Unable to generate a data context for the new database";
            if (batReferenceDataContext.DatabaseExists())
                batReferenceDataContext.DeleteDatabase();
            //return ("Database with this name already exists:- " + fileName);

            try
            {
                batReferenceDataContext.CreateDatabase();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
                return ex.Message;
            }

            InitializeDatabase(batReferenceDataContext);
            batReferenceDataContext.Connection.Close();
            batReferenceDataContext.Connection.Dispose();
            batReferenceDataContext.Dispose();
            batReferenceDataContext = null;
            return err;
        }

        /// <summary>
        ///     Deletes the bat passed as a parameter, and re-indexxes the sort order
        /// </summary>
        /// <param name="selectedBat">
        ///     The selected bat.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void DeleteBat(Bat selectedBat)
        {
            var dc = GetDataContext();
            if (selectedBat == null) return;
            if (selectedBat.Id > 0)
                try
                {
                    var bat = (from b in dc.Bats
                        where b.Id == selectedBat.Id
                        select b).SingleOrDefault();

                    var tags = from t in dc.BatTags
                        where t.BatID == selectedBat.Id
                        select t;
                    var batcalls = from bc in dc.BatCalls
                        where bc.BatID == selectedBat.Id
                        select bc;

                    var batSegments = from bsl in dc.BatSegmentLinks
                        where bsl.BatID == selectedBat.Id
                        select bsl;
                    dc.BatSegmentLinks.DeleteAllOnSubmit(batSegments);

                    var batRecordings = from brl in dc.BatRecordingLinks
                        where brl.BatID == selectedBat.Id
                        select brl;
                    dc.BatRecordingLinks.DeleteAllOnSubmit(batRecordings);
                    Debug.WriteLine("Deleting bat " + selectedBat.Name + " and " + batRecordings.Count() + " BRLs");

                    var batSessions = from bsl in dc.BatSessionLinks
                        where bsl.BatID == selectedBat.Id
                        select bsl;
                    dc.BatSessionLinks.DeleteAllOnSubmit(batSessions);
                    dc.SubmitChanges();

                    if (batcalls != null)
                    {
                        var calls = dc.Calls.Where(call => batcalls.Any(batcall => batcall.CallID == call.Id));

                        if (!calls.IsNullOrEmpty())
                            foreach (var call in calls)
                                if (call.CallPictures != null)
                                    DeleteImagesForCall(call, dc);

                        dc.BatCalls.DeleteAllOnSubmit(batcalls);

                        dc.Calls.DeleteAllOnSubmit(calls);
                    }

                    if (selectedBat.BatPictures != null) DeleteImagesForBat(selectedBat, dc);
                    if (tags != null) dc.BatTags.DeleteAllOnSubmit(tags);
                    if (bat != null) dc.Bats.DeleteOnSubmit(bat);
                    dc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    Debug.WriteLine("Error deleting Bat:- " + ex.Message);
                }

            //DBAccess.ResequenceBats();
        }

        internal static void DeleteAllSegmentsForRecording(int id)
        {
            if (id > 0)
            {
                var dc = GetDataContext();
                var recording = dc.Recordings.SingleOrDefault(rec => rec.Id == id);
                if (recording != null) DeleteAllSegmentsInRecording(recording, dc);
            }
        }

        internal static void DeleteSegment(LabelledSegment segment)
        {
            var dc = GetDataContext();
            DeleteSegment(segment, dc);
        }

        /// <summary>
        ///     Deletes the segment provided as a parameter and identified by it's Id.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void DeleteSegment(LabelledSegment segment, BatReferenceDBLinqDataContext dc)
        {
            if (segment != null && segment.Id > 0)
            {
                LabelledSegment segmentToDelete;
                if (dc == null) dc = GetDataContext();
                DeleteLinksForSegmentId(segment.Id, dc);
                try
                {
                    segmentToDelete = (from seg in dc.LabelledSegments
                        where seg.Id == segment.Id
                        select seg).SingleOrDefault();
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    return;
                }

                if (segmentToDelete != null)
                {
                    dc.LabelledSegments.DeleteOnSubmit(segmentToDelete);
                    dc.SubmitChanges();
                }
            }
        }

        /// <summary>
        ///     Deletes the session provided as a parameter and identified by the Id. All related
        ///     recordings are also deleted.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static void DeleteSession(RecordingSession session)
        {
            if (session != null && session.Id >= 0)
                using (var dc = GetDataContext(false))
                {
                    //dc.DeferredLoadingEnabled = false;

                    //DBAccess.DeleteAllRecordingsInSession(session, dc);
                    var sessionsToDelete = (from sess in dc.RecordingSessions
                        where sess.Id == session.Id
                        select sess).SingleOrDefault();
                    if (sessionsToDelete != null)
                    {
                        DeleteBatSessionLinks(sessionsToDelete, dc);
                        DeleteAllRecordingsInSession(sessionsToDelete, dc);

                        dc.RecordingSessions.DeleteOnSubmit(sessionsToDelete);
                        dc.SubmitChanges();
                    }
                }
        }

        private static void DeleteBatSessionLinks(RecordingSession session, BatReferenceDBLinqDataContext dc)
        {
            if (session != null && session.Id >= 0)
            {
                var linksTodelete = from lnk in dc.BatSessionLinks
                    where lnk.SessionID == session.Id
                    select lnk;
                if (!linksTodelete.IsNullOrEmpty())
                {
                    dc.BatSessionLinks.DeleteAllOnSubmit(linksTodelete);
                    dc.SubmitChanges();
                }
            }
        }

        private static void DeleteBatRecordingLinks(Recording recording, BatReferenceDBLinqDataContext dc)
        {
            if (recording != null && recording.Id >= 0)
            {
                var linksToDelete = from lnk in dc.BatRecordingLinks
                    where lnk.RecordingID == recording.Id
                    select lnk;
                if (!linksToDelete.IsNullOrEmpty())
                {
                    dc.BatRecordingLinks.DeleteAllOnSubmit(linksToDelete);
                    dc.SubmitChanges();
                }
            }
        }

        private static void DeleteBatRecordingLinks(IQueryable<Recording> recordings, BatReferenceDBLinqDataContext dc)
        {
            if (!recordings.IsNullOrEmpty())
            {
                var linksTodelete = from lnk in dc.BatRecordingLinks
                    from rec in recordings
                    where lnk.RecordingID == rec.Id
                    select lnk;
                if (!linksTodelete.IsNullOrEmpty())
                {
                    dc.BatRecordingLinks.DeleteAllOnSubmit(linksTodelete);
                    dc.SubmitChanges();
                }
            }
        }

        private static void DeleteBatSegmentLinks(RecordingSession sessionsToDelete, BatReferenceDBLinqDataContext dc)
        {
            if (sessionsToDelete != null && sessionsToDelete.Id >= 0)
            {
                var linksToDelete = from lnk in dc.BatSessionLinks
                    where lnk.SessionID == sessionsToDelete.Id
                    select lnk;
                if (!linksToDelete.IsNullOrEmpty())
                {
                    dc.BatSessionLinks.DeleteAllOnSubmit(linksToDelete);
                    dc.SubmitChanges();
                }
            }
        }

        /// <summary>
        ///     Deletes the tag.
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        internal static void DeleteTag(BatTag tag)
        {
            var dc = GetDataContext();
            var tagsToDelete = from tg in dc.BatTags
                where tg.Id == tag.Id
                select tg;
            dc.BatTags.DeleteAllOnSubmit(tagsToDelete);
            dc.SubmitChanges();
            //DBAccess.ResequenceTags(tag, dc);
            //DBAccess.ResequenceBats();
        }

        /// <summary>
        ///     Gets the name of the bat latin for the given common name
        /// </summary>
        /// <param name="batCommonName">
        ///     Name of the bat common.
        /// </param>
        /// <returns>
        /// </returns>
        internal static string GetBatLatinName(string batCommonName)
        {
            var result = batCommonName;

            if (batCommonName.ToUpper().Contains("NO") && batCommonName.ToUpper().Contains("BATS")) return result;

            var dc = GetFastDataContext();
            var bat = GetNamedBat(batCommonName, dc);
            if (!string.IsNullOrWhiteSpace(bat.Batgenus))
            {
                if (string.IsNullOrWhiteSpace(bat.BatSpecies))
                    result = bat.Batgenus + " sp.";
                else
                    result = bat.Batgenus + " " + bat.BatSpecies;
            }

            return result;
        }

        /// <summary>
        ///     Givel an instance of a Labelled Segment, finds any linked Call Parameter
        ///     sets and returns the first one.  Normally there should only be one per
        ///     segment but this is not guaranteed.  If there are none, returns null.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        internal static Call GetSegmentCall(LabelledSegment segment)
        {
            if (segment == null || segment.Id <= 0) return null;
            if (!segment.SegmentCalls.IsNullOrEmpty())
            {
                var call = segment.SegmentCalls[0].Call;
                return call;
            }

            return null;
        }

        /// <summary>
        ///     Gets the bat statistics.  Returns an ObservableCollection of BatStatisitics
        ///     one for each of the known species of bat.
        /// </summary>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static BulkObservableCollection<BatStatistics> GetBatStatistics()
        {
            var result = new BulkObservableCollection<BatStatistics>();

            var dc = GetFastDataContext();

            //var allBats = DBAccess.GetSortedBatList();

            foreach (var bat in dc.Bats)
                if (!bat.BatSegmentLinks.IsNullOrEmpty())
                {
                    var thisBatStats = new BatStatistics(bat);
                    //thisBatStats = DBAccess.GetBatStatisticsForBat(bat);
                    result.Add(thisBatStats);
                }

            return result;
        }

        /*
        /// <summary>
        /// Return an instance of BatStatisitics for the specified bat
        /// </summary>
        /// <param name="bat"></param>
        /// <returns></returns>
        internal static BatStatistics GetBatStatisticsForBat(Bat bat)
        {
            BatStatistics thisBatStats = new BatStatistics();
            if (bat != null && bat.Id >= 0)
            {
                thisBatStats.Name = bat.Name;
                thisBatStats.Genus = bat.Batgenus;
                thisBatStats.Species = bat.BatSpecies;

                var recordings = (from lnk in bat.BatSegmentLinks
                                  select lnk.LabelledSegment.Recording).Distinct();

                var sessions = (from rec in recordings
                                select rec.RecordingSession).Distinct();

                thisBatStats.sessions.AddRange(sessions);
                thisBatStats.recordings.AddRange(recordings);

                thisBatStats.numRecordingImages = 0;
                thisBatStats.numRecordingImages = (from lnk in bat.BatSegmentLinks

                                                   select lnk.LabelledSegment.SegmentDatas.Count).Sum();

                thisBatStats.numRecordingImages += GetImportedSegmentDatasForBat(bat).Count;

                thisBatStats.stats = DBAccess.GetPassesForBat(bat);
                thisBatStats.bat = bat;
                thisBatStats.numBatImages = bat.BatPictures.Count;
            }
            return (thisBatStats);
        }*/

        /// <summary>
        ///     Gets a list of all the Segmentdatas link entries for imported images which are
        ///     associated with dummy labelled segments that are labelled "Recording Images"
        ///     and in which the image combined caption and description includes a mention of the specified bat.
        /// </summary>
        /// <param name="bat"></param>
        /// <returns></returns>
        internal static List<SegmentData> GetImportedSegmentDatasForBat(Bat bat)
        {
            var result = new List<SegmentData>();
            if (bat != null)
            {
                var dc = GetFastDataContext();

                var importedSegmentDatas = from sd in dc.SegmentDatas
                    from tag in bat.BatTags
                    where sd.LabelledSegment.Comment.Contains("Recording Images") &&
                          (
                              tag.BatTag1.ToUpper() == tag.BatTag1 && sd.BinaryData.Description.Replace('$', ' ')
                                  .ToUpper().Contains(tag.BatTag1) ||
                              tag.BatTag1.ToUpper() != tag.BatTag1 && sd.BinaryData.Description.ToUpper()
                                  .Replace('$', ' ').Contains(tag.BatTag1.ToUpper())
                          )
                    select sd;

                //if (!importedSegmentDatas.IsNullOrEmpty())
                //{
                //foreach (var sd in importedSegmentDatas)
                //{
                /*
                var batList = DBAccess.GetDescribedBats(sd.BinaryData.Description.Replace('$',' '));
                if (batList != null && batList.Count > 0)
                {
                    var match = from bt in batList
                                where bt.Id == bat.Id
                                select bt;
                    if (!match.IsNullOrEmpty())
                    {
                        result.Add(sd);
                    }
                }*/
                //if (!bat.BatTags.IsNullOrEmpty())
                //{
                //    foreach (var tag in bat.BatTags)
                //    {
                //        if (tag.BatTag1.ToUpper() == tag.BatTag1)
                //        {
                //            if (sd.BinaryData.Description.Contains(tag.BatTag1))
                //            {
                //                result.Add(sd);
                //                break;
                //            }
                //        }
                //        else
                //        {
                //            if (sd.BinaryData.Description.ToUpper().Contains(tag.BatTag1.ToUpper()))
                //            {
                //                result.Add(sd);
                //                break;
                //            }
                //        }
                //    }
                //}
                //}
                //}
            }

            return result;
        }

        /// <summary>
        ///     Overload of GetImportedSegmentDatasForBat which restricts the retrieval to images
        ///     for a specific recording
        /// </summary>
        /// <param name="bat"></param>
        /// <param name="recording"></param>
        /// <returns></returns>
        internal static List<SegmentData> GetImportedSegmentDatasForBat(Bat bat, Recording recording)
        {
            var result = new List<SegmentData>();
            if (bat != null)
            {
                var dc = GetFastDataContext();

                var importedSegmentDatas = from sd in dc.SegmentDatas
                    where sd.LabelledSegment.RecordingID == recording.Id &&
                          sd.LabelledSegment.Comment.Contains("Recording Images")
                    select sd;
                if (!importedSegmentDatas.IsNullOrEmpty())
                    foreach (var sd in importedSegmentDatas)
                        if (!bat.BatTags.IsNullOrEmpty())
                            foreach (var tag in bat.BatTags)
                            {
                                var tagUc = tag.BatTag1.ToUpper();
                                if (tagUc == tag.BatTag1)
                                {
                                    if (sd.BinaryData.Description.Contains(tag.BatTag1))
                                    {
                                        result.Add(sd);
                                        break;
                                    }
                                }
                                else
                                {
                                    if (sd.BinaryData.Description.ToUpper().Contains(tagUc))
                                    {
                                        result.Add(sd);
                                        break;
                                    }
                                }
                            }

                //if (!importedSegmentDatas.IsNullOrEmpty())
                //{
                //    result.AddRange(importedSegmentDatas);

                //}
            }

            return result;
        }

        /// <summary>
        ///     Gets the blank bat.
        /// </summary>
        /// <returns>
        /// </returns>
        internal static Bat GetBlankBat()
        {
            IEnumerable<Bat> batlist = Enumerable.Empty<Bat>().AsQueryable();
            var dc = GetFastDataContext();

            batlist = from bat in dc.Bats
                where bat.Name == "No Bats"
                select bat;
            if (batlist == null || !batlist.Any())
            {
                var noBat = new Bat {Name = "No Bats"};

                var tag = new BatTag {BatTag1 = "No Bats", SortIndex = 1};
                noBat.BatTags.Add(tag);
                noBat.BatSpecies = "sp.";
                noBat.Batgenus = "Unknown";
                noBat.SortIndex = int.MaxValue;
                dc.Bats.InsertOnSubmit(noBat);
                dc.SubmitChanges();
                return noBat;
            }

            return batlist.First();
        }

        internal static BulkObservableCollection<Call> GetCallParametersForSegment(LabelledSegment segment)
        {
            //BatReferenceDBLinqDataContext dc = DBAccess.GetFastDataContext();
            var result = new BulkObservableCollection<Call>();

            if (segment != null)
                //var seg = from ls in dc.LabelledSegments
                //  where ls.Id == id
                //  select ls;

                // if (!seg.IsNullOrEmpty())
                // {
                //  var segment = seg.First();
                if (!segment.SegmentCalls.IsNullOrEmpty())
                {
                    //List<Call> callList = new List<Call>();
                    //foreach (var link in segment.SegmentCalls)
                    //{
                    //    callList.Add(link.Call);
                    //}

                    var calls = segment.SegmentCalls.Select(link => link.Call);

                    result.AddRange(calls);
                    return result;
                }
            // }

            return null;
        }

        /// <summary>
        ///     Gets the calls for bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        /// </returns>
        internal static List<Call> GetCallsForBat(Bat bat)
        {
            if (bat != null && bat.Id > 0)
            {
                //BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                //var batCallList = from bc in bat.BatCalls

                //select bc.Call;

                var batCallList = bat.BatCalls.Select(bc => bc.Call);
                if (!batCallList.IsNullOrEmpty()) return batCallList.ToList();
            }

            return null;
        }

        /// <summary>
        ///     Returns a persistent database datacontext if one exists.  DO NOT USE for updating
        ///     or inserting to the database, but only for data retrieval operations.
        /// </summary>
        /// <returns></returns>
        internal static BatReferenceDBLinqDataContext GetFastDataContext()
        {
            if (_persistentbatReferenceDataContext != null) return _persistentbatReferenceDataContext;
            return GetDataContext();
        }

        /// <summary>
        ///     Gets the data context.
        /// </summary>
        /// <returns>
        /// </returns>
        internal static BatReferenceDBLinqDataContext GetDataContext()
        {
            return GetDataContext(null);
        }

        internal static BatReferenceDBLinqDataContext GetDataContext(bool? deferred)
        {
            BatReferenceDBLinqDataContext batReferenceDataContext = null;
            // DONT do this - persistence makes changes to entities happen on the next SubmitChanges()
            // which causes conflicts because they may not happen in the right order and may not need to
            // be reflected in the database at all.
            //if (PersistentbatReferenceDataContext != null) return (PersistentbatReferenceDataContext);
            var workingDatabaseLocation = GetWorkingDatabaseLocation();
            var workingDatabaseFilename = GetWorkingDatabaseName(workingDatabaseLocation);

            try
            {
                if (!File.Exists(workingDatabaseLocation + workingDatabaseFilename))
                {
                    Tools.InfoLog("No file at [" + workingDatabaseLocation + workingDatabaseFilename + "]");
                    App.dbFileLocation = "";
                    workingDatabaseLocation = GetWorkingDatabaseLocation();
                    workingDatabaseFilename = GetWorkingDatabaseName(workingDatabaseLocation);
                }

                if (!Directory.Exists(workingDatabaseLocation))
                {
                    workingDatabaseLocation = DBLocation;
                    Tools.InfoLog("switch to " + workingDatabaseLocation);
                    if (!Directory.Exists(workingDatabaseLocation))
                    {
                        Tools.InfoLog("...and create that directory");
                        Directory.CreateDirectory(workingDatabaseLocation);
                    }
                }

                if (!File.Exists(workingDatabaseLocation + workingDatabaseFilename))
                {
                    Tools.InfoLog("No file at [" + workingDatabaseLocation + workingDatabaseFilename + "]");
                    workingDatabaseFilename = DBFileName;
                    Tools.InfoLog("Try " + DBFileName);
                    if (!File.Exists(workingDatabaseFilename))
                    {
                        Tools.InfoLog("Creating database [" + workingDatabaseLocation + workingDatabaseFilename + "]");
                        CreateDatabase(workingDatabaseLocation + workingDatabaseFilename);
                    }
                }

                batReferenceDataContext = new BatReferenceDBLinqDataContext(
                    @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + workingDatabaseLocation +
                    workingDatabaseFilename + @";Integrated Security=False;Connect Timeout=60");
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex + "\n" + ex.Message);
            }
            finally
            {
                if (batReferenceDataContext == null) batReferenceDataContext = new BatReferenceDBLinqDataContext();
            }

            if (deferred != null) batReferenceDataContext.DeferredLoadingEnabled = deferred.Value;

            if (_isDataContextUpToDate) return batReferenceDataContext;

            var tables = batReferenceDataContext.Mapping.GetTables();
            var versionTableExists = false;
            foreach (var table in tables)
                if (table.TableName.Contains("Version"))
                    versionTableExists = true;
            if (!versionTableExists)
            {
                Tools.InfoLog("+@+@+@+@  Table in the database:-");
                foreach (var table in tables) Tools.InfoLog(table.TableName);
                try
                {
                    CreateVersionTable(batReferenceDataContext);
                    Debug.WriteLine("******* Created Version Table");
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("From GetDataContext, failed to create version table:- " + ex.Message);
                }

                try
                {
                    // only happens when a table has just been created so it will always be the first entry
                    var version = new Version {Version1 = 5.31m};
                    batReferenceDataContext.Versions.InsertOnSubmit(version);
                    batReferenceDataContext.SubmitChanges();
                    Debug.WriteLine("Added a new version number 5.31");
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("From GetDataContext, failed to insert version number:- " + ex.Message);
                }

                batReferenceDataContext = new BatReferenceDBLinqDataContext(
                    @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + workingDatabaseLocation +
                    workingDatabaseFilename + @";Integrated Security=False;Connect Timeout=60");
            }

            try
            {
                var version = from v in batReferenceDataContext.Versions
                    select v.Version1;
                if (version != null && version.Any()) versionTableExists = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Version Table Does Not Exist:-" + ex.Message);
                Tools.InfoLog("Version Table does not exist " + ex.Message);
                Tools.ErrorLog("No version table:- " + batReferenceDataContext.Connection.ConnectionString);
                Tools.InfoLog(batReferenceDataContext.DatabaseExists() ? "Database exists" : "Database does not exist");
            }

            var mappedTables = batReferenceDataContext.Mapping.GetTables();
            versionTableExists = batReferenceDataContext.Mapping.GetTables().Where(e => e.TableName == "dbo.Version")
                .Select(t => t.RowType.IsEntity).FirstOrDefault();
            if (!versionTableExists)
            {
                try
                {
                    CreateVersionTable(batReferenceDataContext);
                    Debug.WriteLine("******* Created Version Table");
                    Tools.InfoLog("Created new Version Table");
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("From GetDataContext, failed to create version table:- " + ex.Message);
                }

                try
                {
                    // only happens when a table has just been created so it will always be the first entry
                    var version = new Version {Version1 = 5.31m};
                    batReferenceDataContext.Versions.InsertOnSubmit(version);
                    batReferenceDataContext.SubmitChanges();
                    Debug.WriteLine("Added a new version number 5.31");
                    Tools.InfoLog("added a new Version Number - 5.31");
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("From GetDataContext, failed to insert version number:- " + ex.Message);
                }
            }

            var actualVersions = batReferenceDataContext.Versions;

            //see if we actually have any versions and if so if it is the current version
            if (!actualVersions.Any() || actualVersions.First().Version1 != DbVersionDec)
            {
                if (!actualVersions.Any())
                {
                    // if the table is empty add a version number entry
                    var version = new Version {Version1 = 5.31m};
                    batReferenceDataContext.Versions.InsertOnSubmit(version);
                    batReferenceDataContext.SubmitChanges();
                    Debug.WriteLine("Added Version entry 5.31 to empty Version Table");
                    Tools.InfoLog("Added Version 5.31 to empty version Table");
                } // else we have an entry which is not the current version

                try
                {
                    UpdateDataBase(batReferenceDataContext);
                    batReferenceDataContext.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("From GetDataContext, failed to update database:- " + ex.Message);
                    return batReferenceDataContext;
                }

                try
                {
                    var currentVersion = batReferenceDataContext.Versions.First();
                    currentVersion.Version1 = DbVersionDec;
                    batReferenceDataContext.SubmitChanges();
                    Tools.InfoLog("Set current version to " + DbVersionDec);
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("From getDataContext, failed to update VersionTable:- " + ex.Message);
                }

                batReferenceDataContext.SubmitChanges();
            }

            _persistentbatReferenceDataContext = null;
            _isDataContextUpToDate = true;
            return batReferenceDataContext;
        }

        /// <summary>
        ///     Checks to see if the file selected is a valid and up to date BRM database.
        ///     If it is a an earlier version of a vsalid database will return a message of
        ///     "old", if invalid "bad" or if good and up to date "ok"
        ///     Validity is based on the presence of tables called Recordingsession,
        ///     Recording and LabelledSegment and the Version stored in the
        ///     Versions table.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static string ValidateDatabase(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "bad";
            if (!fileName.ToUpper().EndsWith(".MDF")) return "bad";
            BatReferenceDBLinqDataContext batReferenceDataContext;
            try
            {
                using (batReferenceDataContext = new BatReferenceDBLinqDataContext(
                    @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + fileName +
                    @";Integrated Security=False;Connect Timeout=60"))
                {
                    if (batReferenceDataContext == null) return "bad";
                    var mappedTables = batReferenceDataContext.Mapping.GetTables();
                    var tableNames = (from tab in mappedTables select tab.TableName).ToList();
                    if (!(tableNames.Contains("dbo.RecordingSession") && tableNames.Contains("dbo.Recording") &&
                          tableNames.Contains("dbo.LabelledSegment")))
                    {
                        var error = "Attempting to open database withthe following tables:-\n";
                        foreach (var name in tableNames) error = error + name + "\n";
                        Tools.InfoLog(error);
                        return "bad";
                    }

                    var versionTableExists = batReferenceDataContext.Mapping.GetTables()
                        .Where(e => e.TableName == "dbo.Version").Select(t => t.RowType.IsEntity).FirstOrDefault();
                    if (!versionTableExists) return "old";

                    try
                    {
                        var actualVersions = batReferenceDataContext.Versions;

                        //see if we actually have any versions and if so if it is the current version
                        if (!actualVersions.Any() || actualVersions.First().Version1 != DbVersionDec) return "old";
                    }
                    catch (Exception)
                    {
                        return "bad";
                    }

                    return "ok";
                }
            }
            catch (Exception)
            {
                return "bad";
            }
        }

        /// <summary>
        ///     Special routine to update an out of date database. Database modifications
        ///     should only involve the addition of tables and columns not the modification
        ///     of any existing column or data type which could corrupt or destroy existing
        ///     data.
        /// </summary>
        /// <param name="batReferenceDataContext"></param>
        private static void UpdateDataBase(BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            var version = 0.0m;
            try
            {
                var versionSet = batReferenceDataContext.Versions;
                if (versionSet.Any()) version = versionSet.First().Version1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to read Versions.Version1");
                Tools.ErrorLog("Unable to read version when updating the database" + ex.Message);
                return;
            }

            if (version < 6.0m)
            {
                // Add RecordingSessions.EndDate
                // Add Recording.StartDate
                //
                //
                try
                {
                    batReferenceDataContext.ExecuteCommand(@"ALTER TABLE [dbo].[RecordingSession]
    ADD [EndDate] DATETIME NULL;");
                    Tools.InfoLog("Updated Database by addition of EndDate to RecordingSession");
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("Updating database:- " + ex.Message);
                }

                try
                {
                    batReferenceDataContext.ExecuteCommand(@"ALTER TABLE [dbo].[Recording]
    ADD [RecordingDate] DATETIME NULL;");
                    Tools.InfoLog("Updated Database by addition of RecordingDate to Recording");
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("Updating Database:- " + ex.Message);
                }
            }

            if (version < 6.1m)
                try
                {
                    batReferenceDataContext.ExecuteCommand(
                        @"ALTER TABLE [dbo].[BinaryData] ALTER COLUMN [Description] NVARCHAR(MAX) NULL;");
                    Tools.InfoLog("Updated Database to v6.1 by extending BinaryData.Description to NVARCHAR(MAX)");
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("Updating Database to v6.1:- " + ex.Message);
                }

            if (version < 6.2m)
            {
                try
                {
                    AddBatSessionLinkTable(batReferenceDataContext);
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("Error Updating Database to v6.2 when adding BatSessiontable:- " + ex.Message);
                }

                try
                {
                    AddBatRecordingTable(batReferenceDataContext);
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog("Error Updating Database to v6.2 when adding BatRecordingTable:- " + ex.Message);
                }
            }
        }

        private static void AddBatRecordingTable(BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            try
            {
                batReferenceDataContext.ExecuteCommand(@"CREATE TABLE [dbo].[BatRecordingLink] (
    [Id]     INT IDENTITY (1, 1) NOT NULL,
    [BatID]       INT DEFAULT ((-1)) NOT NULL,
    [RecordingID] INT DEFAULT ((-1)) NOT NULL,
    CONSTRAINT [Bat_BatRecordingLink] FOREIGN KEY ([BatID]) REFERENCES [dbo].[Bat] ([Id]),
    CONSTRAINT [Recording_BatRecordingLink] FOREIGN KEY ([RecordingID]) REFERENCES [dbo].[Recording] ([Id]),
    CONSTRAINT [PK_BatRecordingLink] PRIMARY KEY ([Id])
)");
                batReferenceDataContext.SubmitChanges();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error creating BatRecording link table");
                throw ex;
            }

            PopulateBatRecordingLinkTable(batReferenceDataContext);
        }

        private static void PopulateBatRecordingLinkTable(BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            try
            {
                var links = (from bat in batReferenceDataContext.Bats
                    from bsLnk in bat.BatSegmentLinks
                    select new {batLink = bat, recLink = bsLnk.LabelledSegment.Recording}).Distinct();
                if (!links.IsNullOrEmpty())
                {
                    foreach (var link in links)
                    {
                        var batRecordingLink = new BatRecordingLink
                        {
                            Id = -1, BatID = link.batLink.Id, RecordingID = link.recLink.Id
                        };
                        batReferenceDataContext.BatRecordingLinks.InsertOnSubmit(batRecordingLink);
                    }

                    batReferenceDataContext.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Failed to update BatRecordingLink table data:-" + ex.Message);
            }
        }

        private static void AddBatSessionLinkTable(BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            try
            {
                batReferenceDataContext.ExecuteCommand(@"CREATE TABLE [dbo].[BatSessionLink] (
    [Id]     INT IDENTITY (1, 1) NOT NULL,
    [SessionID] INT DEFAULT ((-1)) NOT NULL,
    [BatID]     INT DEFAULT ((-1)) NOT NULL,

    CONSTRAINT [Bat_BatSessionLink] FOREIGN KEY ([BatID]) REFERENCES [dbo].[Bat] ([Id]),
    CONSTRAINT [RecordingSession_BatSessionLink] FOREIGN KEY ([SessionID]) REFERENCES [dbo].[RecordingSession] ([Id]),
    CONSTRAINT [PK_BatSessionLink] PRIMARY KEY ([Id])
)");
                batReferenceDataContext.SubmitChanges();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error creating BatSession link table");
                throw ex;
            }

            PopulateBatSessionLinkTable(batReferenceDataContext);
        }

        private static void PopulateBatSessionLinkTable(BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            try
            {
                var links = (from bat in batReferenceDataContext.Bats
                        from bsLnk in bat.BatSegmentLinks
                        select new {batLink = bat.Id, sessLink = bsLnk.LabelledSegment.Recording.RecordingSession.Id})
                    .Distinct();
                if (!links.IsNullOrEmpty())
                {
                    foreach (var link in links)
                    {
                        var batSessionLink = new BatSessionLink
                        {
                            Id = -1, BatID = link.batLink, SessionID = link.sessLink
                        };
                        batReferenceDataContext.BatSessionLinks.InsertOnSubmit(batSessionLink);
                        batReferenceDataContext.SubmitChanges();
                    }

                    batReferenceDataContext.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Failed to Update BatSessionLink table:- " + ex.Message);
            }
        }

        private static void CreateVersionTable(BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            try
            {
                batReferenceDataContext.ExecuteCommand(
                    @"CREATE TABLE[dbo].[Version]([Id] INT NOT NULL PRIMARY KEY, [Version] DECIMAL(6, 2) NOT NULL DEFAULT 6.1)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to create Version Table:-" + ex.Message);
                Tools.ErrorLog("Unable to create Version Table:-" + ex.Message);
            }
        }

        /// <summary>
        ///     Gets the described bats.
        ///     Removes everything following the first { to eliminate call
        ///     parameters and the associated comments, and then uses the
        ///     tagMatcher class to identify bat tags in the remaining string.
        /// </summary>
        /// <param name="description">
        ///     The description.
        /// </param>
        /// <returns>
        /// </returns>
        internal static BulkObservableCollection<Bat> GetDescribedBats(string description,
            BracketedText extent = BracketedText.EXCLUDE)
        {
            var matchingBats = new BulkObservableCollection<Bat>();
            if (string.IsNullOrWhiteSpace(description))
            {
                var nobat = GetBlankBat();
                matchingBats.Add(nobat);
                return matchingBats;
            }

            var dc = GetFastDataContext();

            if (dc == null) return null;
            var tagList = dc.BatTags.ToList(); // get a list of all known tags
            foreach (var bat in dc.Bats)
            {
                var tag = new BatTag {Bat = bat, BatTag1 = bat.Name};
                tagList.Add(tag); // add a tag for the name of each known bat
            }

            if (extent == BracketedText.EXCLUDE)
            {
                var len = description.IndexOf('{');
                if (len >= 0)
                    description = description.Substring(0, len); // get the working part of the comment to scan
            }

            var tagMatcher = new TagMatcher(tagList); // find matches for the tags
            matchingBats.Clear();
            matchingBats.AddRange(tagMatcher.Match(description));

            return matchingBats;
        }

        internal static BulkObservableCollection<Bat> GetDescribedBats(string description, out string moddedDescription,
            BracketedText extent = BracketedText.EXCLUDE)
        {
            var matchingBats = new BulkObservableCollection<Bat>();
            var bracketed = "";
            moddedDescription = description;
            if (string.IsNullOrWhiteSpace(description))
            {
                var nobat = GetBlankBat();
                matchingBats.Add(nobat);
                return matchingBats;
            }

            var dc = GetFastDataContext();

            if (dc == null) return null;
            var tagList = dc.BatTags.ToList();
            foreach (var bat in dc.Bats)
            {
                var tag = new BatTag {Bat = bat, BatTag1 = bat.Name};
                tagList.Add(tag);
            }

            if (extent == BracketedText.EXCLUDE)
            {
                var len = description.IndexOf('{');
                if (len >= 0)
                {
                    bracketed = description.Substring(len).Trim();
                    description = description.Substring(0, len);
                }
            }

            var tagMatcher = new TagMatcher(tagList);
            matchingBats.Clear();
            matchingBats.AddRange(tagMatcher.Match(description));
            moddedDescription = tagMatcher.Substitute(description);
            moddedDescription = moddedDescription.Trim() + " " + bracketed;

            return matchingBats;
        }

        /// <summary>
        ///     Gets the equipment list.
        /// </summary>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static BulkObservableCollection<string> GetEquipmentList()
        {
            var returnVal = new BulkObservableCollection<string>();
            var dc = GetFastDataContext();

            var result = (from sess in dc.RecordingSessions
                where sess.Equipment != null && sess.Equipment != ""
                select sess.Equipment).Distinct();
            returnVal.AddRange(result);

            return returnVal;
        }

        /// <summary>
        ///     Gets the location list.
        /// </summary>
        /// <returns>
        /// </returns>
        internal static BulkObservableCollection<string> GetLocationList()
        {
            var result = new BulkObservableCollection<string>();
            var dc = GetFastDataContext();

            var locations = (from sess in dc.RecordingSessions
                where sess.Location != null && sess.Location != ""
                select sess.Location).Distinct();
            result.AddRange(locations);

            return result;
        }

        /// <summary>
        ///     Gets the microphone list .
        /// </summary>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static BulkObservableCollection<string> GetMicrophoneList()
        {
            var result = new BulkObservableCollection<string>();
            var dc = GetFastDataContext();

            var mics = (from sess in dc.RecordingSessions
                where sess.Microphone != null && sess.Microphone != ""
                select sess.Microphone).Distinct();
            if (mics != null) result.AddRange(mics);

            return result;
        }

        /// <summary>
        ///     Gets the operators.
        /// </summary>
        /// <returns>
        /// </returns>
        internal static BulkObservableCollection<string> GetOperators()
        {
            var dc = GetFastDataContext();
            var result = new BulkObservableCollection<string>();
            var operators = (from op in dc.RecordingSessions
                where op.Operator != null && op.Operator != ""
                select op.Operator).Distinct();
            if (operators != null) result.AddRange(operators);
            return result;
        }

        /// <summary>
        ///     Gets the recording with the specified Id
        /// </summary>
        /// <param name="id">
        ///     The identifier.
        /// </param>
        /// <returns>
        /// </returns>
        internal static Recording GetRecording(int id)
        {
            Recording recording = null;
            if (id <= 0) return null;
            var dc = GetFastDataContext();

            try
            {
                recording = dc.Recordings.FirstOrDefault(rec => rec.Id == id);
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
                return null;
            }

            return recording;
        }

        /// <summary>
        ///     Gets the recording session.
        /// </summary>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <returns>
        /// </returns>
        internal static RecordingSession GetRecordingSession(string sessionTag)
        {
            sessionTag = sessionTag.Truncate(120);
            var dc = GetFastDataContext();

            var session = new RecordingSession {LocationGPSLatitude = null, LocationGPSLongitude = null};
            var sessions = from rs in dc.RecordingSessions
                where rs.SessionTag == sessionTag
                select rs;
            if (!sessions.IsNullOrEmpty())
            {
                session = sessions.First();
                return session;
            }

            return null;
        }

        internal static RecordingSession GetRecordingSession(int Id)
        {
            var dc = GetFastDataContext();
            RecordingSession result = null;
            try
            {
                var results = from sess in dc.RecordingSessions
                    where sess.Id == Id
                    select sess;
                if (!results.IsNullOrEmpty()) result = results.First();
                return result;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        ///     Gets the stats for recording. Given an instance of a specific recording produces a list
        ///     with an element for each bat type that was present in the recording and the number of
        ///     passes and the min, max, mean durations of each pass or labelled segment.
        /// </summary>
        /// <param name="recording">
        ///     The recording identifier.
        /// </param>
        /// <returns>
        /// </returns>
        internal static BulkObservableCollection<BatStats> GetStats(this Recording recording)
        {
            var result = new BulkObservableCollection<BatStats>();

            var listOfBatsAndSegments = from seg in recording.LabelledSegments
                from lnk in seg.BatSegmentLinks
                select new {bat = lnk.Bat, segment = lnk.LabelledSegment};

            foreach (var bat in listOfBatsAndSegments.Select(item => item.bat).Distinct())
            {
                var stat = new BatStats {batCommonName = bat.Name};
                var segmentsForThisBat = from item in listOfBatsAndSegments
                    where item.bat.Id == bat.Id
                    select item.segment;

                foreach (var seg in segmentsForThisBat) stat.Add(seg.EndOffset - seg.StartOffset);
                result.Add(stat);
            }

            return result;
        }

        /// <summary>
        ///     ExtensionMethod on RecordingSession.GetStats()
        ///     For a given recording session produce List of BatStats each of which gives the number
        ///     of passes and segments for a single recording for a single bat. All bats encountered in the session
        ///     should be represented in the list.
        ///     NB the list may contain multiple instances of each bat.  Use Tools.CondenseStatsList() to reduce the list
        ///     to contain single instances of each bat with aggregated data.
        /// </summary>
        /// <param name="recordingSession">
        ///     The recording session.
        /// </param>
        /// <returns>
        /// </returns>
        internal static BulkObservableCollection<BatStats> GetStats(this RecordingSession recordingSession)
        {
            var result = new BulkObservableCollection<BatStats>();

            if (!recordingSession.Recordings.IsNullOrEmpty())
            {
                var batSegmentsinSession = from rec in recordingSession.Recordings
                    from seg in rec.LabelledSegments
                    from pass in seg.BatSegmentLinks
                    select pass;

                if (!batSegmentsinSession.IsNullOrEmpty())
                    foreach (var pass in batSegmentsinSession)
                    {
                        var stat = new BatStats {batCommonName = pass.Bat.Name};

                        stat.Add(pass.LabelledSegment.EndOffset - pass.LabelledSegment.StartOffset);
                        result.Add(stat);
                    }
            }

            return result;
        }

        /// <summary>
        ///     Gets the tag with the same TagText as the parameter or null if no such tag exists.
        /// </summary>
        /// <param name="tagText">
        ///     The tag text.
        /// </param>
        /// <returns>
        /// </returns>
        internal static BatTag GetTag(string tagText)
        {
            var dc = GetFastDataContext();

            var flatTagsList = dc.BatTags.ToList();
            var tags = from tg in flatTagsList
                where tg.BatTag1.ToUpper() == tg.BatTag1 || tagText.ToUpper() == tagText
                    ? tg.BatTag1 == tagText
                    : tg.BatTag1.ToUpper() == tagText.ToUpper()
                select tg;
            if (!tags.IsNullOrEmpty()) return tags.First();

            return null;
        }

        /// <summary>
        ///     Gets the working database location.
        /// </summary>
        /// <returns>
        /// </returns>
        internal static string GetWorkingDatabaseLocation()
        {
            var workingDatabaseLocation = "";
            if (!string.IsNullOrWhiteSpace(App.dbFileLocation) && Directory.Exists(App.dbFileLocation))
            {
                workingDatabaseLocation = App.dbFileLocation;
            }
            else
            {
#if DEBUG
                workingDatabaseLocation = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\Debug\");
#else
                workingDatabaseLocation = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\");
#endif
            }

            if (!Directory.Exists(workingDatabaseLocation)) Directory.CreateDirectory(workingDatabaseLocation);

            return workingDatabaseLocation;
        }

        internal static string GetWorkingDatabaseName(string dbLocation)
        {
            var workingDatabaseName = "";
            if (!string.IsNullOrWhiteSpace(App.dbFileName) && App.dbFileName.EndsWith(".mdf") &&
                App.dbFileName.Contains("BatReferenceDB"))
            {
                if (File.Exists(dbLocation + App.dbFileName))
                {
                    workingDatabaseName = App.dbFileName;
                }
                else
                {
                    App.dbFileName = "";
                    if (File.Exists(dbLocation + DBFileName)) workingDatabaseName = DBFileName;
                }
            }
            else
            {
                workingDatabaseName = DBFileName;
            }

            return workingDatabaseName;
        }

        /// <summary>
        ///     Initializes the database.
        ///     Thiss function is called whenever the program runs.  It first checks that there is a database and if not
        ///     it creates one from scratch.  If it does this it uses either a bat reference xml file (supplied by the
        ///     installer) or an existing editable bat reference file to update the new database.  It then renames the
        ///     reference file used as an editable reference .bak file which will not be used for future initialisations
        ///     unless it is renamed as an editable reference file.
        ///     If the database already exists, then if there is an editable reference file it is used to update the database
        ///     and is then renamed as .bak to prevent re-use.
        /// </summary>
        internal static void InitializeDatabase()
        {
            try
            {
                var workingDatabaseLocation = GetWorkingDatabaseLocation();

                var dbShortName = "BatReferenceDB";

                if (!File.Exists(workingDatabaseLocation + dbShortName + DbVersion + ".mdf"))
                {
                    try
                    {
                        CreateDatabase(workingDatabaseLocation + dbShortName + DbVersion + ".mdf");
                        App.dbFileLocation = workingDatabaseLocation;
                        App.dbFileName = dbShortName + DbVersion + ".mdf";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        Tools.ErrorLog(ex.Message);
                    }

                    //BatReferenceDBLinqDataContext batReferenceDataContext = DBAccess.GetDataContext();
                    if (!File.Exists(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml") &&
                        File.Exists(workingDatabaseLocation + "BatReferenceXMLFile.xml"))
                    {
                        File.Move(workingDatabaseLocation + "BatReferenceXMLFile.xml",
                            workingDatabaseLocation + "EditableBatReferenceXMLFile.xml");

                        UpdateReferenceData(workingDatabaseLocation);
                    }
                }
                else
                {
                    UpdateReferenceData(workingDatabaseLocation);
                }

                //DBAccess.ResequenceBats();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Tools.ErrorLog(ex.Message);
            }
        }

        private static void UpdateReferenceData(string workingDatabaseLocation)
        {
            var referenceFile = workingDatabaseLocation + "EditableBatReferenceXMLFile.xml";
            try
            {
                if (!File.Exists(referenceFile))
                {
                    referenceFile = workingDatabaseLocation + "BatReferenceXMLFile.xml";
                    if (!File.Exists(referenceFile)) referenceFile = @".\BatReferenceXMLFile.xaml";
                }

                if (File.Exists(referenceFile))
                {
                    CopyXmlDataToDatabase(referenceFile);
                    if (File.Exists(referenceFile + ".bak")) File.Delete(referenceFile + ".bak");
                    File.Move(referenceFile, referenceFile + ".bak");
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("UpdateReferenceData Error from:- ( " + referenceFile + ") " + ex.Message);
            }
        }

        /// <summary>
        ///     Initializes the database.
        ///     If we have an editable bat reference file, then use it to update the database and then make the
        ///     editable reference file a .bak file so that it doesn't get loaded again.  If the user wants to
        ///     make a new file they can rename it to the editable reference file again and it will get , but
        ///     existing reference data may be lost.
        ///     This function is called when the databse in use is changedd by the user.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void InitializeDatabase(BatReferenceDBLinqDataContext dc)
        {
            try
            {
                //String workingDatabaseLocation = DBAccess.GetWorkingDatabaseLocation();
                /*
                if (!File.Exists(DBLocation + "EditableBatReferenceXMLFile.xml") && File.Exists(
                    DBLocation + "EditableBatReferenceXMLFile.xml.bak"))
                {
                    File.Copy(DBLocation + "EditableBatReferenceXMLFile.xml.bak", DBLocation + "EditableBatReferenceXMLFile.xml");
                }
                */
                //BatReferenceDBLinqDataContext batReferenceDataContext = DBAccess.GetDataContext();

                UpdateReferenceData(DBLocation);

                //DBAccess.ResequenceBats();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }

        internal static Call InsertParamsFromComment(string comment, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = GetDataContext();
            CleanCallTable();
            Call newCall = null;
            var parameterSet = new ParameterSet(comment);
            if (parameterSet.HasCallParameters)
            {
                //newCall = DBAccess.InsertParamsFromLabel(comment);

                newCall = parameterSet.call;
                if (newCall != null && newCall.Validate())
                    try
                    {
                        //BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                        dc.Calls.InsertOnSubmit(newCall);
                        dc.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        Debug.WriteLine(newCall.Id + " - InsertCallFromComment - " + ex);
                    }
                else
                    return null;
            }
            else
            {
                return null;
            }

            return newCall;
        }

        internal static string SetDatabase(string fileName)
        {
            var err = "";
            try
            {
                CloseDatabase();
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    if (File.Exists(fileName))
                    {
                        var index = fileName.LastIndexOf(@"\");
                        App.dbFileLocation = fileName.Substring(0, index + 1);
                        App.dbFileName = fileName.Substring(index + 1);
                    }
                    else
                    {
                        err = "Specified database file does not exist:-" + fileName;
                    }
                }
                else
                {
                    err = CreateDatabase(fileName);
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        App.dbFileLocation = "";
                        App.dbFileName = "";
                        Tools.ErrorLog("Unable to find or create database [" + fileName + "]:-" + err);
                    }
                }

                _isDataContextUpToDate = false;
                var dc = GetDataContext();
                if (dc == null)
                    err = "Unable to re-open selected database";
                else
                    InitializeDatabase(dc);
            }
            catch (Exception ex)
            {
                err = ex.ToString();
                Tools.ErrorLog(ex.Message);
            }

            return err;
        }

        /// <summary>
        ///     Updates the bat.
        /// </summary>
        /// <param name="selectedBat">
        ///     The selected bat.
        /// </param>
        /// <param name="callList"></param>
        /// <param name="imageList"></param>
        /// <param name="listOfCallImageLists"></param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static void UpdateBat(Bat selectedBat, BulkObservableCollection<Call> callList,
            BulkObservableCollection<StoredImage> imageList,
            BulkObservableCollection<BulkObservableCollection<StoredImage>> listOfCallImageLists)
        {
            var dc = GetFastDataContext();

            try
            {
                var existingBat = (from bat in dc.Bats
                    where bat.Id == selectedBat.Id
                    select bat).SingleOrDefault();
                //Bat ExistingBat = null;
                //if(ExistingBats!=null && ExistingBats.Count() > 0)
                //{
                //   ExistingBat = ExistingBats.First();
                //}
                if (existingBat == null)
                {
                    // so create a new bat entity
                    existingBat = new Bat
                    {
                        Batgenus = selectedBat.Batgenus,
                        BatSpecies = selectedBat.BatSpecies,
                        Name = selectedBat.Name,
                        Notes = selectedBat.Notes,
                        SortIndex = selectedBat.SortIndex
                    };
                    dc.Bats.InsertOnSubmit(existingBat);
                    dc.SubmitChanges();
                }
                else
                {
                    existingBat.Batgenus = selectedBat.Batgenus;
                    existingBat.BatSpecies = selectedBat.BatSpecies;
                    existingBat.Name = selectedBat.Name;
                    existingBat.Notes = selectedBat.Notes;
                    existingBat.SortIndex = selectedBat.SortIndex;
                }

                try
                {
                    dc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    var dc2 = GetDataContext();
                    Debug.WriteLine("---Repairing CallPictures:-" + ex);
                    var invalidCps = from cp in dc2.CallPictures
                        where cp.CallID <= 0
                        select cp;
                    dc2.CallPictures.DeleteAllOnSubmit(invalidCps);
                    dc2.SubmitChanges();
                }

                MergeTags(existingBat, selectedBat, dc);
                dc.SubmitChanges();
                if (callList != null) UpdateCalls(callList, existingBat, dc);
                if (!listOfCallImageLists.IsNullOrEmpty())
                    UpdateCallImagesForBat(listOfCallImageLists, existingBat, dc);
                if (imageList != null) UpdateBatImages(imageList, existingBat, dc);
            }
            catch (NullReferenceException nex)
            {
                Tools.ErrorLog(nex.Message);
                Debug.WriteLine("NULL in UpdateBat:- " + nex.Message);
                Debug.Write(nex.StackTrace);
            }
        }

        /// <summary>
        ///     For a bat with a currently accurate list of call types, updates the list of images
        ///     for each of the call types.  The images are in a list of lists.  The outer list
        ///     corresponds to the list of call types and for each call type there is a list of
        ///     images which may be empty.  existing images may have been deleted or modified.
        /// </summary>
        /// <param name="listOfCallImageLists"></param>
        /// <param name="existingBat"></param>
        /// <param name="dc"></param>
        private static void UpdateCallImagesForBat(
            BulkObservableCollection<BulkObservableCollection<StoredImage>> listOfCallImageLists, Bat existingBat,
            BatReferenceDBLinqDataContext dc)
        {
            //TO DO
            var numberOfCalls = 0;
            IQueryable<Call> callsForBat = null;
            try
            {
                callsForBat = from cl in dc.BatCalls
                    where cl.BatID == existingBat.Id
                    select cl.Call;
                numberOfCalls = callsForBat.Count();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
            }

            if (numberOfCalls <= listOfCallImageLists.Count && callsForBat != null)
            {
                var cfbArray = callsForBat.ToArray();
                for (var i = 0; i < numberOfCalls; i++)
                    UpdateCallImages(listOfCallImageLists[i], cfbArray[i], dc); //Element At not supported
            }
        }

        private static void UpdateCallImages(BulkObservableCollection<StoredImage> listOfImages, Call call,
            BatReferenceDBLinqDataContext dc)
        {
            if (listOfImages == null || listOfImages.Count <= 0)
            {
                DeleteImagesForCall(call, dc);
                return;
            }

            if (call != null && dc != null)
            {
                //first delete any images linked to this call which are not in the list
                var allImagesForCall = from cp in dc.CallPictures
                    where cp.CallID == call.Id
                    select cp.BinaryData;
                var newImageList = from image in listOfImages select image;
                var imagesToDelete = allImagesForCall.AsEnumerable()
                    .Where(aic => newImageList.All(ni => ni.ImageID != aic.Id));
                if (!imagesToDelete.IsNullOrEmpty())
                {
                    foreach (var bd in imagesToDelete)
                    {
                        dc.CallPictures.DeleteAllOnSubmit(bd.CallPictures);
                        dc.BinaryDatas.DeleteOnSubmit(bd);
                    }

                    dc.SubmitChanges();
                }

                //then we modify existing images that might have changed
                //
                var matchingImages = newImageList.Where(newImage =>
                    allImagesForCall.Any(oldImage => oldImage.Id == newImage.ImageID));
                if (!matchingImages.IsNullOrEmpty())
                    foreach (var modifiedImage in matchingImages)
                    {
                        var existingBinaryData = (from cp in dc.CallPictures
                            where cp.BinaryDataID == modifiedImage.ImageID && cp.CallID == call.Id
                            select cp.BinaryData).FirstOrDefault();
                        if (existingBinaryData != null && existingBinaryData.Id >= 0)
                        {
                            existingBinaryData.Description = modifiedImage.GetCombinedText();
                            existingBinaryData.BinaryDataType = Tools.BlobType.PNG.ToString();
                            var bd = StoredImage.ConvertBitmapImageToPngBinary(modifiedImage.image,
                                modifiedImage.HorizontalGridlines, modifiedImage.VerticalGridLines);
                            existingBinaryData.BinaryData1 = bd ?? new Binary(new byte[0]);
                            dc.SubmitChanges();
                        }
                    }

                // Finally we add any new images
                //
                var imagesToAdd = newImageList.Where(newImage => newImage.ImageID < 0);
                if (!imagesToAdd.IsNullOrEmpty())
                    foreach (var image in imagesToAdd)
                    {
                        var newBinaryData = image.GetAsBinaryData();
                        dc.BinaryDatas.InsertOnSubmit(newBinaryData);
                        dc.SubmitChanges();
                        var newCallImageLink = new CallPicture {CallID = call.Id, BinaryDataID = newBinaryData.Id};
                        dc.CallPictures.InsertOnSubmit(newCallImageLink);
                        dc.SubmitChanges();
                    }
            }
        }

        /// <summary>
        ///     deletes all images in the database which are linked to the specified call and the
        ///     associated links in CallPictures
        /// </summary>
        /// <param name="call"></param>
        /// <param name="dc"></param>
        private static void DeleteImagesForCall(Call call, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = GetDataContext();
            if (call == null) return;
            var callPictureLinks = from cp in dc.CallPictures
                where cp.CallID == call.Id
                select cp;
            if (!callPictureLinks.IsNullOrEmpty())
                foreach (var cp in callPictureLinks)
                {
                    var links = 0;
                    links = links + cp.BinaryData.CallPictures.Count;
                    links = links + cp.BinaryData.BatPictures.Count;
                    links = links + cp.BinaryData.SegmentDatas.Count;
                    dc.CallPictures.DeleteOnSubmit(cp);
                    if (links <= 1) dc.BinaryDatas.DeleteOnSubmit(cp.BinaryData);
                    dc.SubmitChanges();
                }
        }

        private static void DeleteImagesForSegment(LabelledSegment segment, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = GetDataContext();
            if (segment == null || segment.Id < 0) return;
            var segmentImageLinks = from sd in dc.SegmentDatas
                where sd.SegmentId == segment.Id
                select sd;
            if (!segmentImageLinks.IsNullOrEmpty())
                foreach (var sd in segmentImageLinks)
                {
                    var links = 0;
                    links = links + sd.BinaryData.CallPictures.Count;
                    links = links + sd.BinaryData.BatPictures.Count;
                    links = links + sd.BinaryData.SegmentDatas.Count;
                    dc.SegmentDatas.DeleteOnSubmit(sd);
                    if (links <= 1) dc.BinaryDatas.DeleteOnSubmit(sd.BinaryData);
                    dc.SubmitChanges();
                }
        }

        private static void DeleteImagesForBat(Bat bat, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = GetDataContext();
            if (bat == null || bat.Id < 0) return;
            var batImageLinks = from sd in dc.SegmentDatas
                where sd.SegmentId == bat.Id
                select sd;
            if (!batImageLinks.IsNullOrEmpty())
                foreach (var sd in batImageLinks)
                {
                    var links = 0;
                    links = links + sd.BinaryData.CallPictures.Count;
                    links = links + sd.BinaryData.BatPictures.Count;
                    links = links + sd.BinaryData.SegmentDatas.Count;
                    dc.SegmentDatas.DeleteOnSubmit(sd);
                    if (links <= 1) dc.BinaryDatas.DeleteOnSubmit(sd.BinaryData);
                    dc.SubmitChanges();
                }
        }

        /// <summary>
        ///     Updates the list of bat images for the specified bat.  The images may or may
        ///     not already exist and are linked to the bat table through the BatImage link table.
        ///     The image entries are in the form of a list of StoredImage each element of which
        ///     contains the image, the caption and description and the ID of the bat to which the
        ///     image relates.  If the ID is -1 then the image is new to this bat and there is no
        ///     link existing.  Images may have been deleted and therefore, need to be removed from
        ///     the database if they exist their but ot in the list.
        /// </summary>
        /// <param name="imageList"></param>
        /// <param name="existingBat"></param>
        /// <param name="dc"></param>
        private static void UpdateBatImages(BulkObservableCollection<StoredImage> imageList, Bat existingBat,
            BatReferenceDBLinqDataContext dc)
        {
            if (imageList != null && existingBat != null && existingBat.Id >= 0 && dc != null)
            {
                // first delete any images linked to this bat which are not in the list
                // theoretically they should have been removed from the database when they were
                // deleted from the ImageScrollerControl, but in case they got deleted by another
                // route we ensure that the database correctly reflects the list passed as data.
                var allImagesForBat = from bp in dc.BatPictures
                    where bp.BatId == existingBat.Id
                    select bp.BinaryData;
                var newImageList = from image in imageList select image; // to provide the list in the correct type

                var imagesToDelete = allImagesForBat.AsEnumerable()
                    .Where(aib => newImageList.All(ni => ni.ImageID != aib.Id));

                if (!imagesToDelete.IsNullOrEmpty())
                {
                    foreach (var bd in imagesToDelete)
                    {
                        dc.BatPictures.DeleteAllOnSubmit(bd.BatPictures);
                        dc.BinaryDatas.DeleteOnSubmit(bd);
                    }

                    dc.SubmitChanges();
                }

                // then we modify existing images that might have changed

                var matchingImages = newImageList.Where(newImage =>
                    allImagesForBat.Any(oldImage => oldImage.Id == newImage.ImageID));
                if (!matchingImages.IsNullOrEmpty())
                    foreach (var modifiedImage in matchingImages)
                    {
                        var existingBinaryData = (from bp in dc.BatPictures
                            where bp.BinaryDataId == modifiedImage.ImageID && bp.BatId == existingBat.Id
                            select bp.BinaryData).FirstOrDefault();
                        if (existingBinaryData != null && existingBinaryData.Id >= 0)
                        {
                            existingBinaryData.Description = modifiedImage.GetCombinedText();

                            existingBinaryData.BinaryDataType = Tools.BlobType.PNG.ToString();
                            var bd = StoredImage.ConvertBitmapImageToPngBinary(modifiedImage.image,
                                modifiedImage.HorizontalGridlines, modifiedImage.VerticalGridLines);
                            existingBinaryData.BinaryData1 = bd ?? new Binary(new byte[0]);
                            dc.SubmitChanges();
                        }
                    }

                // finally we add any new images
                //

                var imagesToAdd = newImageList.Where(newImage => newImage.ImageID < 0);
                if (!imagesToAdd.IsNullOrEmpty())
                    foreach (var image in imagesToAdd)
                    {
                        var newBinaryData = image.GetAsBinaryData();
                        dc.BinaryDatas.InsertOnSubmit(newBinaryData);
                        dc.SubmitChanges();
                        var newBatImageLink = new BatPicture {BatId = existingBat.Id, BinaryDataId = newBinaryData.Id};
                        dc.BatPictures.InsertOnSubmit(newBatImageLink);
                        dc.SubmitChanges();
                    }
            }
        }

        /// <summary>
        ///     Updates the bat list.
        /// </summary>
        /// <param name="batList">
        ///     The bat list.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static void UpdateBatList(BulkObservableCollection<Bat> batList)
        {
            if (batList != null)
                foreach (var bat in batList)
                    UpdateBat(bat, null, null, null);
        }

        /// <summary>
        ///     Updates the recording. Adds it to the database if not already present or modifies the
        ///     existing entry to match if present. The measure of presence depends on the Name filed
        ///     which is the name of the wav file and should be unique in the database.  Includes a lists
        ///     of lists of images, one imagelist for each labelled segment.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="listOfSegmentAndBatLists"></param>
        /// <param name="listOfSegmentImageLists"></param>
        /// <returns></returns>
        internal static string UpdateRecording(Recording recording,
            BulkObservableCollection<SegmentAndBatList> listOfSegmentAndBatLists,
            BulkObservableCollection<BulkObservableCollection<StoredImage>> listOfSegmentImageLists)
        {
            string errmsg = null;

            Recording existingRecording = null;
            var dc = GetFastDataContext();

            errmsg = recording != null ? recording.Validate() : "No recording to validate";
            try
            {
                if (string.IsNullOrWhiteSpace(errmsg))
                {
                    var session = (from sess in dc.RecordingSessions
                        where sess.Id == recording.RecordingSessionId
                        select sess).SingleOrDefault();
                    if (session == null) return "Unable to Locate Session for this Recording";

                    // find existing recordings with matching ID or Name
                    IQueryable<Recording> existingRecordings = null;
                    if (recording.Id <= 0 && !string.IsNullOrWhiteSpace(recording.RecordingName))
                        existingRecordings = from rec in dc.Recordings
                            where rec.RecordingName == recording.RecordingName
                            select rec;
                    else if (recording.Id > 0)
                        existingRecordings = from rec in dc.Recordings
                            where rec.Id == recording.Id
                            select rec;

                    // ...and extract the first and hopefully only eaxample
                    if (!existingRecordings.IsNullOrEmpty()) existingRecording = existingRecordings.First();


                    if (existingRecording == null)
                    {
                        recording.RecordingSessionId = session.Id;
                        existingRecording = recording;
                        dc.Recordings.InsertOnSubmit(recording);
                    }
                    else
                    {
                        existingRecording.RecordingDate = recording.RecordingDate;
                        existingRecording.RecordingEndTime = recording.RecordingEndTime;
                        existingRecording.RecordingGPSLatitude = recording.RecordingGPSLatitude;
                        existingRecording.RecordingGPSLongitude = recording.RecordingGPSLongitude;
                        existingRecording.RecordingName = recording.RecordingName;
                        existingRecording.RecordingNotes = recording.RecordingNotes;
                        existingRecording.RecordingSessionId = session.Id;
                        existingRecording.RecordingStartTime = recording.RecordingStartTime;
                        //existingRecording.LabelledSegments.Clear();
                    }

                    // if we have an existing recording, update it, otherwise add it toe database
                    if (existingRecording.RecordingDate == null || existingRecording.RecordingDate.Value.Date >
                        (session.EndDate ?? DateTime.Now).Date)
                        // reported recording date is later than the end of the session and therefore could have been corrupted by
                        // file copying or moving, so we will reset it to correspond to the session date or a date included in the filename
                        existingRecording = NormalizeRecordingDateAndTimes(existingRecording, session);

                    /*           foreach(var bsl in listOfSegmentAndBatList)
                               {
                                   existingRecording.LabelledSegments.Add(bsl.segment);
                               }*/
                    dc.SubmitChanges();

                    // now we have a stored updated recording, update the labelled segments and their images
                    if (listOfSegmentAndBatLists != null)
                        UpdateLabelledSegments(listOfSegmentAndBatLists, existingRecording.Id, listOfSegmentImageLists,
                            dc);
                }

                return errmsg;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("UpdateRecording - " + ex.Message);
                return ex.Message;
            }
        }

        /// <summary>
        ///     If the recording date does not match the session period this function can be used to try to deduce a more
        ///     accurate date and time on the basis of the filename or the session dates and times.
        ///     1st choice - file creation date and time
        ///     2nd choice - date and time from the name of the file
        ///     3rd choice - date from the session start or end dates
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static Recording NormalizeRecordingDateAndTimes(Recording recording, RecordingSession session)
        {
            if (recording == null) return null;
            var fullyQualifiedFileName = recording.GetFileName(session);
            if (!string.IsNullOrWhiteSpace(fullyQualifiedFileName) && File.Exists(fullyQualifiedFileName))
            {
                var creationDate = File.GetCreationTime(fullyQualifiedFileName);
                if (creationDate.Date >= session.SessionDate.Date &&
                    creationDate.Date <= (session.EndDate ?? session.SessionDate).Date)
                {
                    recording.RecordingDate = creationDate.Date;
                    recording = NormalizeRecordingTimes(recording, session);
                }
                else
                {
                    var date = new DateTime();
                    if (GetDateTimeFromFilename(recording.RecordingName, out date))
                    {
                        recording.RecordingDate = date.Date;
                        var duration = (recording.RecordingEndTime ?? new TimeSpan()) -
                                       (recording.RecordingStartTime ?? new TimeSpan());
                        recording.RecordingStartTime = date.TimeOfDay.Seconds == 0
                            ? recording.RecordingStartTime ?? new TimeSpan()
                            : date.TimeOfDay;
                        if (recording.RecordingStartTime.HasValue && recording.RecordingStartTime.Value.Seconds == 0)
                        {
                            recording.RecordingStartTime = null;
                            recording.RecordingEndTime = null;
                        }

                        if (recording.RecordingStartTime != null && duration.Seconds > 0)
                            recording.RecordingEndTime = recording.RecordingStartTime + duration;
                        recording = NormalizeRecordingTimes(recording, session);
                        return recording;
                    }

                    if (recording.RecordingStartTime != null && recording.RecordingStartTime.Value.Hours < 12)
                        // recording is in the morning so use the session end date as a best guess
                        recording.RecordingDate = (session.EndDate ?? session.SessionDate).Date;
                    else
                        recording.RecordingDate = session.SessionDate.Date;
                    recording = NormalizeRecordingTimes(recording, session);
                    return recording;
                }
            }


            return recording;
        }

        /// <summary>
        ///     parses the recording filename to see if it contains sequences that correspond to a date
        ///     and/or time and if so returns those dates and times combined in a single dateTime parameter.
        ///     returns true if valid dates/times are established and false otherwise.
        /// </summary>
        /// <param name="fullyQualifiedFileName"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool GetDateTimeFromFilename(string fullyQualifiedFileName, out DateTime date)
        {
            date = new DateTime();
            var pattern =
                @"([12][09][0-9]{2})[-_]?([0-1][0-9])[-_]?([0-3][0-9])[-_\s]?([0-2][0-9])[-_:]?([0-5][0-9])[-_:]?([0-5][0-9])";
            var match = Regex.Match(fullyQualifiedFileName, pattern);
            if (match.Success)
                if (match.Groups.Count >= 4)
                {
                    var year = match.Groups[1].Value.Trim();
                    var month = match.Groups[2].Value.Trim();
                    var day = match.Groups[3].Value.Trim();
                    var hour = "00";
                    var minute = "00";
                    var second = "00";
                    if (match.Groups.Count >= 7)
                    {
                        hour = match.Groups[4].Value.Trim();
                        minute = match.Groups[5].Value.Trim();
                        second = match.Groups[6].Value.Trim();
                    }

                    var result = new DateTime();
                    var enGB = new CultureInfo("en-GB");
                    var extractedString = year + "/" + month + "/" + day + " " + hour + ":" + minute + ":" + second;

                    if (DateTime.TryParseExact(extractedString, "yyyy/MM/dd HH:mm:ss", null, DateTimeStyles.AssumeLocal,
                        out result))
                    {
                        Debug.WriteLine("Found date time of " + result + " in " + fullyQualifiedFileName);
                        date = result;
                        return true;
                    }
                }

            Debug.WriteLine("No datetime found in " + fullyQualifiedFileName);
            return false;
        }

        /// <summary>
        ///     if the recording date has been modified, check that the times are reasonable and adjust if necessary
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static Recording NormalizeRecordingTimes(Recording recording, RecordingSession session)
        {
            DateTime sessionStart = session.SessionDate.Date;
            sessionStart+= session.SessionStartTime??new TimeSpan();

            DateTime recordStart = (recording.RecordingDate ?? sessionStart).Date;
            recordStart += recording.RecordingStartTime ?? sessionStart.TimeOfDay;

            DateTime sessionEnd = (session.EndDate ?? sessionStart).Date;
            sessionEnd += session.SessionEndTime ?? sessionStart.TimeOfDay.Add(recordStart.TimeOfDay+new TimeSpan(1,0,0));

            
            DateTime recordEnd = recordStart.Date;
            if (recording.RecordingEndTime != null)
            {
                if (recordStart.TimeOfDay.TotalMinutes > (12.0d * 60.0d) &&
                    recording.RecordingEndTime.Value.TotalMinutes < (12.0d * 60.0d)) // starts after midday but ends before midday, so add 1 day to the date
                {
                    recordEnd = recordEnd.AddDays(1);
                }

                recordEnd += recording.RecordingEndTime.Value;
            }
            else
            {
                recordEnd += recordStart.TimeOfDay.Add(new TimeSpan(0,4,0));
            }

            if (sessionStart > sessionEnd)
            {
                DateTime temp = sessionStart;
                sessionStart = sessionEnd;
                sessionEnd = temp;
            }

            if (recordStart > recordEnd)
            {
                DateTime temp = recordStart;
                recordStart = recordEnd;
                recordEnd = temp;
            }

            if (recordStart < sessionStart)
            {
                recordStart = sessionStart;
            }

            if (recordEnd > sessionEnd)
            {
                recordEnd = sessionEnd;
            }


            recording.RecordingDate = recordStart.Date;
            recording.RecordingStartTime = recordStart.TimeOfDay;
            recording.RecordingEndTime = recordEnd - recordStart;

            

            return recording;
        }

        /// <summary>
        ///     Updates the recording session if it already exists in the database or adds it to the database
        /// </summary>
        /// <param name="sessionForFolder">
        ///     The session for folder.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        internal static void UpdateRecordingSession(RecordingSession sessionForFolder,BatReferenceDBLinqDataContext dc=null)
        {
            if (sessionForFolder == null)
            {
                Tools.ErrorLog("Attempt to update a null session");
                return;
            }

            try
            {

                if (dc == null) dc = GetDataContext();
                RecordingSession existingSession = null;

                var existingSessions = from sess in dc.RecordingSessions
                    where sess.Id == sessionForFolder.Id || sess.SessionTag == sessionForFolder.SessionTag
                    select sess;
                if (!existingSessions.IsNullOrEmpty()) existingSession = existingSessions.First();

                if (existingSession == null)
                {
                    sessionForFolder.SessionTag = sessionForFolder.SessionTag.Truncate(120);
                    sessionForFolder.Equipment = sessionForFolder.Equipment.Truncate(120);
                    sessionForFolder.Microphone = sessionForFolder.Microphone.Truncate(120);
                    sessionForFolder.Weather = sessionForFolder.Weather.Truncate(120);
                    sessionForFolder.Location = sessionForFolder.Location.Truncate(120);
                    sessionForFolder.Operator = sessionForFolder.Operator.Truncate(120);
                    NormalizeSessionDateTimes(ref sessionForFolder);
                    dc.RecordingSessions.InsertOnSubmit(sessionForFolder);
                }
                else
                {
                    existingSession.SessionTag = sessionForFolder.SessionTag.Truncate(120);
                    existingSession.Equipment = sessionForFolder.Equipment.Truncate(120);
                    existingSession.Microphone = sessionForFolder.Microphone.Truncate(120);
                    existingSession.Weather = sessionForFolder.Weather.Truncate(120);
                    existingSession.Location = sessionForFolder.Location.Truncate(120);
                    existingSession.SessionDate = sessionForFolder.SessionDate;
                    existingSession.SessionStartTime = sessionForFolder.SessionStartTime;
                    existingSession.SessionEndTime = sessionForFolder.SessionEndTime;
                    existingSession.EndDate = sessionForFolder.EndDate;
                    existingSession.Sunset = sessionForFolder.Sunset;
                    existingSession.SessionNotes = sessionForFolder.SessionNotes;
                    existingSession.Temp = sessionForFolder.Temp;
                    existingSession.Operator = sessionForFolder.Operator.Truncate(120);
                    existingSession.LocationGPSLatitude = sessionForFolder.LocationGPSLatitude;
                    existingSession.LocationGPSLongitude = sessionForFolder.LocationGPSLongitude;
                    existingSession.OriginalFilePath = sessionForFolder.OriginalFilePath;
                    NormalizeSessionDateTimes(ref existingSession);
                }

                dc.SubmitChanges();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Updating Session <" + sessionForFolder.SessionTag + "> - " + ex);
            }
        }

        /// <summary>
        /// Checks validity and ordering of session dates and times
        /// </summary>
        /// <param name="session"></param>
        private static void NormalizeSessionDateTimes(ref RecordingSession session)
        {
            if (session.EndDate == null) session.EndDate = session.SessionDate;
            if(session.SessionStartTime==null) session.SessionStartTime=new TimeSpan(18,0,0);
            if(session.SessionEndTime==null) session.SessionEndTime=new TimeSpan(23,59,59);
            DateTime start = session.SessionDate.Date + session.SessionStartTime.Value;
            DateTime end = session.EndDate.Value.Date + session.SessionEndTime.Value;
            if (start > end)
            {
                DateTime temp = start;
                start = end;
                end = temp;
            }

            session.SessionDate = start;
            session.EndDate = end;
            session.SessionStartTime = start.TimeOfDay;
            session.SessionEndTime = end.TimeOfDay;
        }

        /// <summary>
        ///     Updates the tag specified and returns the sortindex of the tag
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        /// <returns>
        /// </returns>
        internal static int UpdateTag(BatTag tag)
        {
            BatTag thisTag;
            var result = -1;
            var dc = GetFastDataContext();

            thisTag = (from tg in dc.BatTags
                where tg.Id == tag.Id
                select tg).SingleOrDefault();
            if (thisTag != null) thisTag.BatTag1 = tag.BatTag1;
            dc.SubmitChanges();
            result = ResequenceTags(thisTag, dc);

            return result;
        }

        private static void CleanCallTable()
        {
            var dc = GetFastDataContext();

            var orphans = dc.Calls.Except(dc.BatCalls.Select(bc => bc.Call));
            var toDelete = orphans.Except(dc.SegmentCalls.Select(sc => sc.Call));
            if (!toDelete.IsNullOrEmpty())
            {
                /*var cpLinks = from cp in dc.CallPictures
                              from td in toDelete
                              where td.Id == cp.CallID
                              select cp;*/
                var cpLinks = from td in toDelete
                    join cp in dc.CallPictures on td.Id equals cp.CallID
                    select cp;

                if (!cpLinks.IsNullOrEmpty())
                {
                    /*var picsToDelete = from pic in dc.BinaryDatas
                                       from cp in cpLinks
                                       where pic.Id == cp.BinaryDataID
                                       select pic;*/

                    var picsToDelete = from cp in cpLinks
                        join pic in dc.BinaryDatas on cp.BinaryDataID equals pic.Id
                        select pic;

                    if (!picsToDelete.IsNullOrEmpty()) dc.BinaryDatas.DeleteAllOnSubmit(picsToDelete);
                    dc.CallPictures.DeleteAllOnSubmit(cpLinks);
                }

                dc.Calls.DeleteAllOnSubmit(toDelete);
            }

            dc.SubmitChanges();
        }

        /// <summary>
        ///     Converts the XML bat.
        ///     Takes bat as an XElement from an XML file and extracts bat, tag and call details
        ///     from the XML, creating new instances of Bat, BatTag and BatCall classes in the process.
        ///     The bat is merged with any existing bat of the same name (or inserted if it does not exist)
        ///     If there was no existing bat then all BatCalls in the definition are added to the database and
        ///     linked to the new bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dc"></param>
        /// <returns>
        /// </returns>
        private static Bat ConvertXmlBat(XElement bat, BatReferenceDBLinqDataContext dc)
        {
            var newBat = new Bat();
            Bat insertedBat = null;
            try
            {
                newBat.Name = bat.Attribute("Name").Value;
                newBat.Batgenus = bat.Descendants("BatGenus").FirstOrDefault().Value;
                newBat.BatSpecies = bat.Descendants("BatSpecies").FirstOrDefault().Value;
                newBat.SortIndex = 2000;
                newBat.Notes = "";
                var parameters = "";

                var newTags = bat.Descendants("BatTag");
                if (!newTags.IsNullOrEmpty())
                {
                    short index = 0;
                    foreach (var tag in newTags)
                    {
                        var bt = new BatTag {BatTag1 = tag.Value, BatID = newBat.Id, SortIndex = index++};
                        newBat.BatTags.Add(bt);
                    }
                }

                var newCommonNames = bat.Descendants("BatCommonName");
                if (!newCommonNames.IsNullOrEmpty()) newBat.Name = newCommonNames.First().Value;
                if (dc == null) dc = GetDataContext();

                var existingBat = GetMatchingBat(newBat);

                MergeBat(newBat, dc, out insertedBat);
                if (insertedBat == null)
                {
                    Tools.ErrorLog("Failed to insert/Merge Bat from XML File");
                    return insertedBat;
                }

                if (existingBat == null)
                {
                    var callDefinitions = bat.Descendants("Call");
                    if (!callDefinitions.IsNullOrEmpty())
                        foreach (var call in callDefinitions)
                        {
                            var dbCall = GetXmlCallParameters(call, parameters);

                            dc.Calls.InsertOnSubmit(dbCall);
                            dc.SubmitChanges();
                            var bc = new BatCall {BatID = insertedBat.Id, CallID = dbCall.Id};
                            dc.BatCalls.InsertOnSubmit(bc);
                            dc.SubmitChanges();
                        }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
            }

            return insertedBat;
        }

        private static Call GetXmlCallParameters(XElement call, string parameters)
        {
            var dbCall = new Call();
            var mean = 0.0d;
            var variation = 0.0d;
            var xFstart = call.Descendants("fStart");
            if (!xFstart.IsNullOrEmpty())
            {
                parameters = xFstart.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(parameters))
                    if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                    {
                        dbCall.StartFrequency = mean;
                        dbCall.StartFrequencyVariation = variation;
                    }
            }

            mean = 0.0d;
            variation = 0.0d;
            var xFend = call.Descendants("fEnd");
            if (!xFend.IsNullOrEmpty())
            {
                parameters = xFend.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(parameters))
                    if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                    {
                        dbCall.EndFrequency = mean;
                        dbCall.EndFrequencyVariation = variation;
                    }
            }

            mean = 0.0d;
            variation = 0.0d;
            var xFpeak = call.Descendants("fPeak");
            if (!xFpeak.IsNullOrEmpty())
            {
                parameters = xFpeak.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(parameters))
                    if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                    {
                        dbCall.PeakFrequency = mean;
                        dbCall.PeakFrequencyVariation = variation;
                    }
            }

            mean = 0.0d;
            variation = 0.0d;
            var xInterval = call.Descendants("Interval");
            if (!xInterval.IsNullOrEmpty())
            {
                parameters = xInterval.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(parameters))
                    if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                    {
                        dbCall.PulseInterval = mean;
                        dbCall.PulseIntervalVariation = variation;
                    }
            }

            mean = 0.0d;
            variation = 0.0d;
            var xDuration = call.Descendants("Duration");
            if (!xDuration.IsNullOrEmpty())
            {
                parameters = xDuration.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(parameters))
                    if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                    {
                        dbCall.PulseDuration = mean;
                        dbCall.PulseDurationVariation = variation;
                    }
            }

            var xFunction = call.Descendants("Function");
            if (!xFunction.IsNullOrEmpty())
            {
                parameters = xFunction.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(parameters)) dbCall.CallFunction = parameters;
            }

            var xType = call.Descendants("Type");
            if (!xType.IsNullOrEmpty())
            {
                parameters = xType.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(parameters)) dbCall.CallType = parameters;
            }

            var xComments = call.Descendants("Comments");
            if (!xComments.IsNullOrEmpty())
            {
                parameters = xComments.FirstOrDefault().Value;

                if (!string.IsNullOrWhiteSpace(parameters)) dbCall.CallNotes = parameters;
            }

            return dbCall;
        }

        /// <summary>
        ///     Copies the XML data to database.
        /// </summary>
        /// <param name="xmlFile">
        ///     The XML file.
        /// </param>
        public static void CopyXmlDataToDatabase(string xmlFile)
        {
            try
            {
                var xmlBats = XElement.Load(xmlFile).Descendants("Bat");
                if (xmlBats != null)
                    foreach (var bat in xmlBats)
                        try
                        {
                            MergeXmlBatToDb(bat);
                        }
                        catch (Exception e1)
                        {
                            Tools.ErrorLog(e1.Message);
                            Debug.WriteLine("Error merging bat " + bat.Name + ":- " + e1.Message);
                        }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Error reading xml file " + xmlFile + ":- " + ex.Message);
            }
        }

        /// <summary>
        ///     Copies the XML data to database.
        /// </summary>
        /// <param name="xmlFile">
        ///     The XML file.
        /// </param>
        /// <param name="dc"></param>
        private static void CopyXmlDataToDatabase(string xmlFile, BatReferenceDBLinqDataContext dc)
        {
            try
            {
                var xmlBats = XElement.Load(xmlFile).Descendants("Bat");
                if (xmlBats != null)
                    foreach (var bat in xmlBats)
                        try
                        {
                            MergeXmlBatToDb(bat, dc);
                        }
                        catch (Exception e1)
                        {
                            Tools.ErrorLog(e1.Message);
                            Debug.WriteLine("Error merging bat " + bat.Name + ":- " + e1.Message);
                        }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Error reading xml file " + xmlFile + ":- " + ex.Message);
            }

            //short i = 0;
        }

        /// <summary>
        ///     Deletes all recordings in session and all Segments in all those recordings.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private static void DeleteAllRecordingsInSession(RecordingSession session, BatReferenceDBLinqDataContext dc)
        {
            //DBAccess.DeleteAllSegmentsInSession(session, dc);
            var recordingsToDelete = from rec in dc.Recordings
                where rec.RecordingSessionId == session.Id
                select rec;
            if (!recordingsToDelete.IsNullOrEmpty())
            {
                DeleteBatRecordingLinks(recordingsToDelete, dc);
                DeleteAllSegmentsInRecording(recordingsToDelete, dc);
                dc.Recordings.DeleteAllOnSubmit(recordingsToDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Deletes all segments in recording passes as a parameter, using the supplied DataContext.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private static void DeleteAllSegmentsInRecording(Recording recording, BatReferenceDBLinqDataContext dc)
        {
            if (recording != null)
            {
                var segmentsToDelete = from seg in dc.LabelledSegments
                    where seg.RecordingID == recording.Id
                    select seg;
                if (segmentsToDelete != null)
                {
                    foreach (var seg in segmentsToDelete) DeleteLinksForSegmentId(seg.Id, dc);

                    dc.LabelledSegments.DeleteAllOnSubmit(segmentsToDelete);
                    dc.SubmitChanges();
                }
            }
        }

        private static void DeleteAllSegmentsInRecording(IQueryable<Recording> recordings,
            BatReferenceDBLinqDataContext dc)
        {
            if (!recordings.IsNullOrEmpty())
            {
                var segmentsToDelete = (from seg in dc.LabelledSegments
                    from rec in recordings
                    where seg.RecordingID == rec.Id
                    select seg).Distinct();
                if (!segmentsToDelete.IsNullOrEmpty())
                {
                    foreach (var seg in segmentsToDelete) DeleteLinksForSegmentId(seg.Id, dc);
                    dc.LabelledSegments.DeleteAllOnSubmit(segmentsToDelete);
                    dc.SubmitChanges();
                }
            }
        }

        /// <summary>
        ///     Deletes all segments in session.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private static void DeleteAllSegmentsInSession(RecordingSession session, BatReferenceDBLinqDataContext dc)
        {
            var segmentsToDelete = from seg in dc.LabelledSegments
                where seg.Recording.RecordingSessionId == session.Id
                select seg;
            if (segmentsToDelete != null)
                foreach (var seg in segmentsToDelete)
                    DeleteLinksForSegmentId(seg.Id, dc);
            dc.LabelledSegments.DeleteAllOnSubmit(segmentsToDelete);
            dc.SubmitChanges();
        }

        /// <summary>
        ///     Deletes the links for segment identifier.
        /// </summary>
        /// <param name="id">
        ///     The identifier.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        private static void DeleteLinksForSegmentId(int id, BatReferenceDBLinqDataContext dc)
        {
            LabelledSegment segment = null;
            var segments = dc.LabelledSegments.Where(seg => seg.Id == id);
            if (!segments.IsNullOrEmpty()) segment = segments.First();
            if (segment != null)
            {
                if (dc == null) dc = GetDataContext();
                var linksToDelete = from lnk in dc.BatSegmentLinks
                    where lnk.LabelledSegmentID == id
                    select lnk;
                if (!linksToDelete.IsNullOrEmpty())
                {
                    foreach (var link in linksToDelete)
                    {
                        // is this the last segment in this recording that refers to this bat?
                        var otherBsLs = from batseglink in dc.BatSegmentLinks
                            where batseglink.BatID == link.BatID &&
                                  batseglink.LabelledSegment.RecordingID == segment.RecordingID
                            select batseglink;
                        if (otherBsLs != null && otherBsLs.Count() == 1)
                            // There is only one link and that is the one we are about to delete
                            // so we should delete the BatRecordingLink as well
                            DeleteBatRecordingLink(link.BatID, segment.RecordingID, dc);
                        // Deleting the recording link also deals with BatSessionLinks
                    }

                    dc.BatSegmentLinks.DeleteAllOnSubmit(linksToDelete);
                    dc.SubmitChanges();
                }

                var callLinks = from lnk in dc.SegmentCalls
                    where lnk.LabelledSegmentID == id
                    select lnk;
                if (!callLinks.IsNullOrEmpty())
                {
                    var callsToDelete = callLinks.Select(lnk => lnk.Call);
                    dc.SegmentCalls.DeleteAllOnSubmit(callLinks);
                    dc.SubmitChanges();

                    dc.Calls.DeleteAllOnSubmit(callsToDelete);
                    dc.SubmitChanges();
                }

                dc.SubmitChanges();

                var imageLinksToDelete = from lnk in dc.SegmentDatas
                    where lnk.SegmentId == id
                    select lnk;
                if (!imageLinksToDelete.IsNullOrEmpty())
                {
                    var imagesToDelete = imageLinksToDelete.Select(lnk => lnk.BinaryData).ToList();
                    dc.SegmentDatas.DeleteAllOnSubmit(imageLinksToDelete);
                    dc.SubmitChanges();
                    //DBAccess.DeleteOrphanedImages(imagesToDelete, dc); // in case an image is referenced by more than one link table
                }
            }
        }

        /// <summary>
        ///     deletes the BatRecordingLink for the given Bat and Recording.  Also checks that this does not leave the
        ///     BatSessionLinks in deficit
        /// </summary>
        /// <param name="bat"></param>
        /// <param name="recording"></param>
        /// <param name="dc"></param>
        private static void DeleteBatRecordingLink(int batId, int recordingId, BatReferenceDBLinqDataContext dc)
        {
            Recording recording = null;
            var recordings = dc.Recordings.Where(rec => rec.Id == recordingId);
            if (!recordings.IsNullOrEmpty()) recording = recordings.First();
            if (recording != null)
            {
                var otherBsLs = from batseglink in dc.BatSegmentLinks
                    where batseglink.BatID == batId && batseglink.LabelledSegment.Recording.RecordingSession.Id ==
                          recording.RecordingSessionId
                    select batseglink;
                // gives us all the BatSegmentLinks that link this bat to the session that the recording belongs to
                // if there is just the one, then the BatSessionLink should be removed as well as the BatRecording link
                if (otherBsLs != null && otherBsLs.Count() == 1)
                {
                    var linkToRemove = from bsl in dc.BatSessionLinks
                        where bsl.BatID == batId && bsl.SessionID == recording.RecordingSessionId
                        select bsl;
                    if (!linkToRemove.IsNullOrEmpty()) dc.BatSessionLinks.DeleteOnSubmit(linkToRemove.First());
                }

                var brlToRemove = from brl in dc.BatRecordingLinks
                    where brl.BatID == batId && brl.RecordingID == recordingId
                    select brl;
                if (!brlToRemove.IsNullOrEmpty())
                {
                    var link = brlToRemove.First();
                    Debug.WriteLine("Deleting BRL " + link.Bat.Name + "--" + link.Recording.RecordingName);
                    dc.BatRecordingLinks.DeleteOnSubmit(brlToRemove.First());
                }

                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Given a dataContext and a list of images, checks to see if any of the images in the list are no
        ///     longer referenced in any of the image link tables, namely SegmentData, CallPicture, BatPicture
        ///     and if not referenced then they are deleted.
        /// </summary>
        /// <param name="imagesToDelete"></param>
        /// <param name="dc"></param>
        private static void DeleteOrphanedImages(List<BinaryData> imagesToDelete, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = GetDataContext();

            if (!imagesToDelete.IsNullOrEmpty())
            {
                var binariesToDelete = from bd in dc.BinaryDatas
                    where imagesToDelete.Contains(bd) &&
                          bd.CallPictures.Count == 0 &&
                          bd.BatPictures.Count == 0 &&
                          bd.SegmentDatas.Count == 0
                    select bd;
                if (!binariesToDelete.IsNullOrEmpty())
                {
                    dc.BinaryDatas.DeleteAllOnSubmit(binariesToDelete);
                    dc.SubmitChanges();
                }
            }
            else
            {
                var binariesToDelete = from bd in dc.BinaryDatas
                    where
                        bd.CallPictures.Count == 0 &&
                        bd.BatPictures.Count == 0 &&
                        bd.SegmentDatas.Count == 0
                    select bd;

                if (!binariesToDelete.IsNullOrEmpty())
                {
                    dc.BinaryDatas.DeleteAllOnSubmit(binariesToDelete);
                    dc.SubmitChanges();
                }
            }
        }

        /// <summary>
        ///     Gets the matching bat. Returns a bat from the database which has the same genus and
        ///     species as the bat passes as a parameter or null if no matching bat is found. If more
        ///     than one matching bat is found (should not
        ///     happen) will return the one with the lowest sortIndex.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dataContext">
        ///     The data context.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        private static Bat GetMatchingBat(Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            try
            {
                if (bat == null) return null;
                var sortedMatchingBats = from b in dataContext.Bats
                    where bat.Id > 0 && b.Id == bat.Id ||
                          b.Batgenus == bat.Batgenus &&
                          b.BatSpecies == bat.BatSpecies
                    orderby b.SortIndex
                    select b;
                if (!sortedMatchingBats.IsNullOrEmpty()) return sortedMatchingBats.First();
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        ///     Gets the named bat.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="dataContext">
        ///     The data context.
        /// </param>
        /// <returns>
        /// </returns>
        private static Bat GetNamedBat(string name, BatReferenceDBLinqDataContext dataContext)
        {
            var namedBats = from b in dataContext.Bats
                where b.Name == name
                orderby b.SortIndex
                select b;
            if (namedBats.IsNullOrEmpty()) return null;
            return namedBats.First();
        }

        /// <summary>
        ///     Gets the passes for bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        /// </returns>
        public static BatStats GetPassesForBat(Bat bat)
        {
            var stats = new BatStats();
            if (bat != null)
            {
                var segments = (from link in bat.BatSegmentLinks
                    select link.LabelledSegment).Distinct();
                if (!segments.IsNullOrEmpty())
                    foreach (var seg in segments)
                        stats.Add(seg.EndOffset - seg.StartOffset);
            }

            return stats;
        }

        /// <summary>
        ///     Gets the recordings for bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        /// </returns>
        private static BulkObservableCollection<Recording> GetRecordingsForBat(Bat bat)
        {
            var result = new BulkObservableCollection<Recording>();
            if (bat?.BatSegmentLinks != null)
            {
                var recordings = (from link in bat.BatSegmentLinks
                    select link.LabelledSegment.Recording).Distinct();

                if (recordings != null) result.AddRange(recordings);
            }

            return result;
        }

        /// <summary>
        ///     Extension Method on Bat.GetSessions()
        ///     returns a collection of all recording sessions which had recordings of this bat
        /// </summary>
        /// <param name="bat"></param>
        /// <returns></returns>
        internal static BulkObservableCollection<RecordingSession> GetSessions(this Bat bat)
        {
            var result = new BulkObservableCollection<RecordingSession>();
            if (bat?.BatSegmentLinks != null)
            {
                var sessions = (from link in bat.BatSegmentLinks
                    select link.LabelledSegment.Recording.RecordingSession).Distinct();

                if (sessions != null) result.AddRange(sessions);
            }

            return result;
        }

        /// <summary>
        ///     Inserts the bat. Adds the supplied bat to the database. It is assumed that the bat
        ///     has been verified and that it does not already exist in the database
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dataContext">
        ///     The data context.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        private static string InsertBat(Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            Bat newBat = null;
            return InsertBat(bat, dataContext, out newBat);
        }

        private static string InsertBat(Bat bat, BatReferenceDBLinqDataContext dataContext, out Bat newBat)
        {
            newBat = bat;
            try
            {
                dataContext.Bats.InsertOnSubmit(bat);
                dataContext.SubmitChanges();
                newBat = bat;
                //DBAccess.ResequenceBats();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                return ex.Message;
            }

            return "";
        }

        /// <summary>
        ///     Merges the tags. First removes tags in the existing bat which do not exist in the new
        ///     bat, then adds any tags in the new bat which do not exist in the existing bat.
        /// </summary>
        /// <param name="existingBat">
        ///     The existing bat.
        /// </param>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dataContext">
        ///     The data context.
        /// </param>
        private static void MergeTags(Bat existingBat, Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            var tagsToDelete = existingBat.BatTags.Except(existingBat.BatTags.Where(
                ebtags => bat.BatTags.Any(btags => btags.BatTag1 == ebtags.BatTag1)));
            if (!tagsToDelete.IsNullOrEmpty()) dataContext.BatTags.DeleteAllOnSubmit(tagsToDelete);

            var tagsToAdd = bat.BatTags.Except(bat.BatTags.Where(
                btag => existingBat.BatTags.Any(ebtag => ebtag.BatTag1 == btag.BatTag1)));
            if (!tagsToAdd.IsNullOrEmpty()) existingBat.BatTags.AddRange(tagsToAdd);
            try
            {
                dataContext.SubmitChanges();
            }
            catch (Exception sqlException)
            {
                Tools.ErrorLog(sqlException.Message);
                Debug.WriteLine("&&&& - MergeTags() -" + sqlException.Message);
            }

            var existingTags = from tag in dataContext.BatTags
                where tag.BatID == existingBat.Id
                orderby tag.SortIndex
                select tag;
            short i = 0;
            foreach (var tag in existingTags) tag.SortIndex = i++;
            try
            {
                dataContext.SubmitChanges();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("    &&&& - MergeTags()(2) - " + ex.Message);
            }
        }

        /// <summary>
        ///     Merges the XML bat to database.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        private static void MergeXmlBatToDb(XElement bat)
        {
            var dc = GetDataContext();
            var batToMerge = ConvertXmlBat(bat, dc);
            //DBAccess.MergeBat(batToMerge);
        }

        /// <summary>
        ///     Merges the XML bat to database.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dc"></param>
        private static void MergeXmlBatToDb(XElement bat, BatReferenceDBLinqDataContext dc)
        {
            var batToMerge = ConvertXmlBat(bat, dc);
            //DBAccess.MergeBat(batToMerge, dc);
        }

        /// <summary>
        ///     Resequences the tags.
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <returns>
        /// </returns>
        private static int ResequenceTags(BatTag tag, BatReferenceDBLinqDataContext dc)
        {
            var result = 0;
            var tagsToSort = from tg in dc.BatTags
                where tg.BatID == tag.BatID
                orderby tg.SortIndex
                select tg;
            short index = 0;
            foreach (var tg in tagsToSort)
            {
                tg.SortIndex = index++;
                if (tg.BatTag1 == tag.BatTag1) result = index - 1;
            }

            dc.SubmitChanges();
            //DBAccess.ResequenceBats();
            return result;
        }

        /// <summary>
        ///     Updates the extended bat pass, or inserts it if it does not already exist in the database
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="segment">
        ///     the parent LabelledSegment that this pass belongs to
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        private static void UpdateBatSegmentLinks(Bat bat, LabelledSegment segment, BatReferenceDBLinqDataContext dc)
        {
            BatSegmentLink batSegmentLink = null;
            if (bat != null && segment != null)
            {
                var matchingPasses = from p in dc.BatSegmentLinks
                    where p.BatID == bat.Id && p.LabelledSegmentID == segment.Id
                    select p;
                if (!matchingPasses.IsNullOrEmpty())
                {
                    batSegmentLink = matchingPasses.First();

                    batSegmentLink.NumberOfPasses = Tools.GetNumberOfPassesForSegment(segment);
                }
                else
                {
                    batSegmentLink = new BatSegmentLink
                    {
                        LabelledSegmentID = segment.Id,
                        BatID = bat.Id,
                        NumberOfPasses = Tools.GetNumberOfPassesForSegment(segment)
                    };
                    dc.BatSegmentLinks.InsertOnSubmit(batSegmentLink);
                }

                try
                {
                    dc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    Debug.WriteLine("UpdatedExtendedBatPAss - " + ex.Message);
                }

                UpdateBatRecAndSessLinks(bat, segment, dc);
            }
            else
            {
                Debug.WriteLine("+++ UpdateBatSegmentLinks - null data supplied");
            }
        }

        private static void UpdateBatRecAndSessLinks(Bat bat, LabelledSegment segment, BatReferenceDBLinqDataContext dc)
        {
            var existingRecLink = from lnk in dc.BatRecordingLinks
                where lnk.BatID == bat.Id && lnk.RecordingID == segment.RecordingID
                select lnk;
            if (existingRecLink.IsNullOrEmpty())
            {
                var blnk = new BatRecordingLink {BatID = bat.Id, RecordingID = segment.RecordingID};
                dc.BatRecordingLinks.InsertOnSubmit(blnk);
                var existingSessionLink = from lnk in dc.BatSessionLinks
                    where lnk.BatID == bat.Id && lnk.SessionID == segment.Recording.RecordingSessionId
                    select lnk;
                if (segment.Recording.RecordingSessionId != null && existingSessionLink.IsNullOrEmpty())
                {
                    var bsesslnk = new BatSessionLink
                    {
                        BatID = bat.Id, SessionID = segment.Recording.RecordingSessionId.Value
                    };
                    dc.BatSessionLinks.InsertOnSubmit(bsesslnk);
                }

                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Updates the calls for the specified bat. The callList may contain new calls and may
        ///     have had calls removed. The function must first delete existing calls not in the new
        ///     list, then update the calls which already exist, and finally add the new calls which
        ///     did not previously exist.
        /// </summary>
        /// <param name="callList">
        ///     The call list.
        /// </param>
        /// <param name="bat"></param>
        /// <param name="dc"></param>
        private static void UpdateCalls(BulkObservableCollection<Call> callList, Bat bat,
            BatReferenceDBLinqDataContext dc)
        {
            CleanCallTable();
            if (bat != null && bat.Id >= 0)
            {
                var allCallsForBat = from bc in dc.BatCalls
                    where bc.BatID == bat.Id
                    select bc.Call;

                var newCallList = from call in callList
                    select call;

                var callsToDelete = allCallsForBat.AsEnumerable().Where(ac => newCallList.All(nc => nc.Id != ac.Id));
                if (!callsToDelete.IsNullOrEmpty())
                {
                    foreach (var call in callsToDelete)
                    {
                        dc.BatCalls.DeleteAllOnSubmit(call.BatCalls);
                        dc.Calls.DeleteOnSubmit(call);
                    }

                    dc.SubmitChanges();
                }

                //var matchingCalls = allCallsForBat.Where(oldcall => !newCallList.Contains(newcall => newcall.Id == oldcall.Id));
                var matchingCalls =
                    newCallList.Where(newcall => allCallsForBat.Any(oldcall => oldcall.Id == newcall.Id));
                if (!matchingCalls.IsNullOrEmpty())
                {
                    foreach (var mcall in matchingCalls)
                    {
                        var callToUpdate = allCallsForBat.SingleOrDefault(call => call.Id == mcall.Id);
                        var updatingCall = newCallList.SingleOrDefault(call => call.Id == mcall.Id);
                        if (callToUpdate != null && updatingCall != null)
                        {
                            callToUpdate.CallFunction = updatingCall.CallFunction;
                            callToUpdate.CallNotes = updatingCall.CallNotes;
                            callToUpdate.CallType = updatingCall.CallType;
                            callToUpdate.EndFrequency = updatingCall.EndFrequency;
                            callToUpdate.EndFrequencyVariation = updatingCall.EndFrequencyVariation;
                            callToUpdate.PeakFrequency = updatingCall.PeakFrequency;
                            callToUpdate.PeakFrequencyVariation = updatingCall.PeakFrequencyVariation;
                            callToUpdate.PulseDuration = updatingCall.PulseDuration;
                            callToUpdate.PulseDurationVariation = updatingCall.PulseDurationVariation;
                            callToUpdate.PulseInterval = updatingCall.PulseInterval;
                            callToUpdate.PulseIntervalVariation = updatingCall.PulseIntervalVariation;
                            callToUpdate.StartFrequency = updatingCall.StartFrequency;
                            callToUpdate.StartFrequencyVariation = updatingCall.StartFrequencyVariation;
                        }
                    }

                    dc.SubmitChanges();
                }

                var callsToAdd = newCallList.Where(newcall => newcall.Id <= 0);
                if (!callsToAdd.IsNullOrEmpty())
                {
                    var linkBat = (from b in dc.Bats
                        where b.Id == bat.Id
                        select bat).SingleOrDefault();
                    if (linkBat != null)
                    {
                        dc.Calls.InsertAllOnSubmit(callsToAdd);
                        foreach (var call in callsToAdd)
                        {
                            var newLink = new BatCall {Call = call, Bat = linkBat};
                            dc.BatCalls.InsertOnSubmit(newLink);
                            //dc.SubmitChanges();
                        }

                        dc.SubmitChanges();
                    }

                    //dc.SubmitChanges();// when a call is modified gets here and crashes trying to insert it
                }
            }
        }

        /// <summary>
        ///     Updates the labelled segments. using the data in the combinedSgementsAndPasses and
        ///     linked to the recording identified by the Id. Also adds data to the extendedBatPasses table.
        ///     Returns the updated or inserted segment
        /// </summary>
        /// <param name="segmentAndBatList">
        ///     The combined segments and passes.
        /// </param>
        /// <param name="recordingId">
        ///     The identifier.
        /// </param>
        /// <param name="listOfSegmentImages"></param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        public static void UpdateLabelledSegment(SegmentAndBatList segmentAndBatList, int recordingId,
            BulkObservableCollection<StoredImage> listOfSegmentImages, BatReferenceDBLinqDataContext dc)
        {
            var commentIsChanged = false;
            if (segmentAndBatList?.Segment == null) return;
            if (dc == null) dc = GetDataContext();
            LabelledSegment existingSegment = null;

            //BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            //dc.DeferredLoadingEnabled = false;

            // if the segment already exists, get it from the DB and modify it and save it back
            // sets a flag if the comment is new or modified
            try
            {
                existingSegment = GetNearestMatchingSegment(segmentAndBatList, recordingId, dc);

                if (existingSegment == null)
                {
                    existingSegment = new LabelledSegment
                    {
                        RecordingID = recordingId,
                        StartOffset = segmentAndBatList.Segment.StartOffset,
                        EndOffset = segmentAndBatList.Segment.EndOffset,
                        Comment = segmentAndBatList.Segment.Comment
                    };

                    commentIsChanged = true;

                    //segmentAndBatList.segment.RecordingID = recordingId;
                    dc.LabelledSegments.InsertOnSubmit(existingSegment);
                }
                else
                {
                    existingSegment.StartOffset = segmentAndBatList.Segment.StartOffset;
                    existingSegment.EndOffset = segmentAndBatList.Segment.EndOffset;
                    if (existingSegment.Comment != segmentAndBatList.Segment.Comment)
                    {
                        commentIsChanged = true;
                        existingSegment.Comment = segmentAndBatList.Segment.Comment;
                    }
                }

                dc.SubmitChanges();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("### UpdateLabelledSegment finding existing segment exception:- " + ex);
            }

            UpdateBatSegmentLinks(segmentAndBatList, existingSegment, dc);
            // Use the batList to establish all the relevant bat/segment links
            // If the comment is unchanged the batList must be unchange

            if (commentIsChanged)
                // Remove all existing segment/call links and generate new ones from the new comment
                UpdateSegmentCalls(existingSegment, dc);

            try
            {
                // even if ListOfSegmentImages is null or empty so that the existing links can be updated
                UpdateSegmentImages(listOfSegmentImages, existingSegment, dc);
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("#### UpdateLabelledSegment updating image links failed:- " + ex.Message);
            }
        }

        /// <summary>
        ///     Uses a SegmentAndBatList (contains a single Segment and a list of referredToBats) and a
        ///     corresponding ExistingSegment and a dataContext from which the ExistingSegment was derived.
        ///     Updates the links between the bats and the Existing segment, deleting unused links and
        ///     adding new links, and retaining exisiting correct links.
        /// </summary>
        /// <param name="segmentAndBatList"></param>
        /// <param name="existingSegment"></param>
        /// <param name="dc"></param>
        private static void UpdateBatSegmentLinks(SegmentAndBatList segmentAndBatList, LabelledSegment existingSegment,
            BatReferenceDBLinqDataContext dc)
        {
            //FAULTY leads to recursion and stack overflow!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            try
            {
                /*
                Operate on bat segment links
                    1) get existing links from existing segment
                    2) remove any links to bats not in the new bat list
                    3) add links for any bats not in the existing links

                so:-
                    1) create an enumerable list of bsLinks for all the new bats to the existing segment
                    2) fetch the enumerable/queryable of bslinks from the existingsegment
                    3) find the links to delete and delete them
                    4) find the links to add and add them
                */

                // 1 - create an enumerable list of new bslinks
                if (segmentAndBatList?.BatList == null || existingSegment == null)
                    return; // can't do anything in this case

                var newLinkList = (from bat in segmentAndBatList.BatList
                                      select new BatSegmentLink
                                          {BatID = bat.Id, LabelledSegmentID = existingSegment.Id}) ??
                                  Enumerable.Empty<BatSegmentLink>();

                // 2 - get existingBatSegment links

                var existingLinks = existingSegment.BatSegmentLinks.Select(lnk => lnk) ??
                                    Enumerable.Empty<BatSegmentLink>();

                // 3 - delete existing links not in new links
                if (!existingLinks.IsNullOrEmpty())
                {
                    var linksTodelete = existingLinks.Except(newLinkList);
                    if (!linksTodelete.IsNullOrEmpty()) dc.BatSegmentLinks.DeleteAllOnSubmit(linksTodelete);
                }

                dc.SubmitChanges();

                // 4 - insert new links not present in the existing segment
                var linksToInsert = newLinkList.Except(existingLinks);
                if (!linksToInsert.IsNullOrEmpty()) dc.BatSegmentLinks.InsertAllOnSubmit(linksToInsert);
                dc.SubmitChanges();

                foreach (var bat in segmentAndBatList.BatList) UpdateBatRecAndSessLinks(bat, existingSegment, dc);
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("### # UpdateLabelledSegment updating bat segment links: " + ex);
            }
        }

        /// <summary>
        ///     Checks that all links in the BatSessionsLink and BatRecordingLink tables are still valid
        ///     and removes any that are invalid - but does not check that segments still mention the bat.
        /// </summary>
        private static void ResolveBatAndRecLinks(BatReferenceDBLinqDataContext dc)
        {
            var noBsl = from brLink in dc.BatRecordingLinks
                where brLink.Bat.BatSegmentLinks.Count <= 0
                select brLink;
            dc.BatRecordingLinks.DeleteAllOnSubmit(noBsl); // remove all links where the bat has no labelled segments
            Debug.WriteLine("Deleting " + noBsl.Count() + " BRLS with no Bat.BatSegmentLinks");

            var noRsl = from brLink in dc.BatRecordingLinks
                where brLink.Recording.LabelledSegments.Count <= 0
                select brLink;
            Debug.WriteLine("Deleting " + noRsl.Count() + " BRLS with no recording.LabelledSegments");
            dc.BatRecordingLinks
                .DeleteAllOnSubmit(noRsl); // remove all links where the recording has no labelled segments

            var bRlinkstodelete = from brlnk in dc.BatRecordingLinks // from all the links...
                from batseglink in brlnk.Bat.BatSegmentLinks // get all the bat's segments for the bat in the link
                from recseg in
                    brlnk.Recording.LabelledSegments // get all the recording's segments for the recording in the link
                where batseglink.LabelledSegment.RecordingID != recseg.RecordingID //
                select brlnk;

            if (!bRlinkstodelete.IsNullOrEmpty())
            {
                Debug.WriteLine("Deleting " + bRlinkstodelete.Count() +
                                " BRLS where bat segments dont match recording segments");
                dc.BatRecordingLinks.DeleteAllOnSubmit(bRlinkstodelete);
            }
        }

        /// <summary>
        ///     Given an existingSegment with possibly updated contents, updates the segment/call links and
        ///     associated call definitions to match the current comment.  Makes the assumption that there is only a single
        ///     call parameter set associated with a segment.  Call images remain even if the call parameters are
        ///     modified to reflect changes in the comment field.
        /// </summary>
        /// <param name="existingSegment"></param>
        /// <param name="dc"></param>
        private static void UpdateSegmentCalls(LabelledSegment existingSegment, BatReferenceDBLinqDataContext dc)
        {
            try
            {
                /*if(existingSegment.SegmentCalls!=null && existingSegment.SegmentCalls.Count > 0)
                {
                    DBAccess.DeleteAllCallsForSegment(existingSegment);
                }*/
                var paramsInComment = new ParameterSet(existingSegment.Comment);
                var newCall = paramsInComment.call;
                if (newCall == null)
                {
                    // new comment has no call parameters so delete all from exisiting segment, job done
                    DeleteAllCallsForSegment(existingSegment);
                }
                else
                {
                    // we have a new set of parameters to be added or updated
                    var existingCall = new Call();

                    if (!existingSegment.SegmentCalls.IsNullOrEmpty())
                    {
                        // this segment has a call definition associated with it
                        // so we retrieve it and update it - Job Done
                        existingCall = existingSegment.SegmentCalls[0].Call;
                        existingCall.CallFunction = newCall.CallFunction;
                        existingCall.CallNotes = newCall.CallNotes;
                        existingCall.CallType = newCall.CallType;
                        existingCall.EndFrequency = newCall.EndFrequency;
                        existingCall.EndFrequencyVariation = newCall.EndFrequencyVariation;
                        existingCall.PeakFrequency = newCall.PeakFrequency;
                        existingCall.PeakFrequencyVariation = newCall.PeakFrequencyVariation;
                        existingCall.PulseDuration = newCall.PulseDuration;
                        existingCall.PulseDurationVariation = newCall.PulseDurationVariation;
                        existingCall.PulseInterval = newCall.PulseInterval;
                        existingCall.PulseIntervalVariation = newCall.PulseIntervalVariation;
                        existingCall.StartFrequency = newCall.StartFrequency;
                        existingCall.StartFrequencyVariation = newCall.StartFrequencyVariation;
                        dc.SubmitChanges();
                    }
                    else
                    {
                        // there is no existing call for the segment so we make one and addit
                        dc.Calls.InsertOnSubmit(newCall);
                        dc.SubmitChanges();
                        var link = new SegmentCall {LabelledSegmentID = existingSegment.Id, CallID = newCall.Id};
                        dc.SegmentCalls.InsertOnSubmit(link);
                        dc.SubmitChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("### ## UpdateLabelledSegment updating call and  links- " + ex.Message);
            }
        }

        /// <summary>
        ///     Given a segmentAndBatList returns the best matching segment from the
        ///     database.  If the passed segment exists based on its ID the that segment
        ///     is returned, otherwise a segment with a greater than 50% temporal
        ///     overlap is returned.  If no match is found then returns null.
        ///     A match is only considered found if EITHER the times or the comment are a
        ///     perfect match - if both have been changed then this is considered to be
        ///     a new segment not a modification of an existing one.
        /// </summary>
        /// <param name="segmentAndBatList"></param>
        /// <param name="recordingId"></param>
        /// <param name="dc"></param>
        /// <returns></returns>
        private static LabelledSegment GetNearestMatchingSegment(SegmentAndBatList segmentAndBatList, int recordingId,
            BatReferenceDBLinqDataContext dc)
        {
            LabelledSegment existingSegment = null;
            if (segmentAndBatList?.Segment != null && dc != null)
            {
                if (segmentAndBatList.Segment.Id > 0 && recordingId > 0)
                {
                    var segments = from seg in dc.LabelledSegments
                        where seg.Id == segmentAndBatList.Segment.Id && seg.RecordingID == recordingId
                        select seg;
                    if (!segments.IsNullOrEmpty())
                    {
                        existingSegment = segments.First();
                        existingSegment.StartOffset = segmentAndBatList.Segment.StartOffset;
                        existingSegment.EndOffset = segmentAndBatList.Segment.EndOffset;
                        existingSegment.Comment = segmentAndBatList.Segment.Comment;
                    }
                }

                if (existingSegment == null)
                {
                    var overlappingSegments = from seg in dc.LabelledSegments
                        where seg.RecordingID == recordingId &&
                              (segmentAndBatList.Segment.StartOffset >= seg.StartOffset &&
                               segmentAndBatList.Segment.StartOffset <= seg.EndOffset ||
                               segmentAndBatList.Segment.EndOffset >= seg.StartOffset &&
                               segmentAndBatList.Segment.EndOffset <= seg.EndOffset ||
                               segmentAndBatList.Segment.StartOffset <= seg.StartOffset &&
                               segmentAndBatList.Segment.EndOffset >= seg.EndOffset)
                        select seg;
                    var greatestOverlap = double.MinValue;
                    if (!overlappingSegments.IsNullOrEmpty())
                        foreach (var seg in overlappingSegments)
                        {
                            var overlap = Math.Min(seg.EndOffset.TotalMilliseconds,
                                              segmentAndBatList.Segment.EndOffset.TotalMilliseconds) -
                                          Math.Max(seg.StartOffset.TotalMilliseconds,
                                              segmentAndBatList.Segment.StartOffset.TotalMilliseconds);
                            if (overlap > greatestOverlap)
                            {
                                greatestOverlap = overlap;
                                existingSegment = seg;
                            }
                        }
                }

                if (existingSegment != null)
                    if (existingSegment.Comment == segmentAndBatList.Segment.Comment ||
                        existingSegment.StartOffset == segmentAndBatList.Segment.StartOffset &&
                        existingSegment.EndOffset == segmentAndBatList.Segment.EndOffset)
                        return existingSegment;
            }

            return null;
        }

        private static void DeleteAllCallsForSegment(LabelledSegment existingSegment)
        {
            if (existingSegment == null || existingSegment.Id < 0) return;

            var dc = GetDataContext();
            var links = from lnk in dc.SegmentCalls
                where lnk.LabelledSegmentID == existingSegment.Id
                select lnk;
            if (!links.IsNullOrEmpty())
            {
                dc.SegmentCalls.DeleteAllOnSubmit(links);
                dc.SubmitChanges();
                CleanCallTable();
            }
        }

        /// <summary>
        ///     Given a list of images (StoredImage) and an Existing segment derived from the given DataContext
        ///     removes any existing images no longer refferred to, updates images that are referred to and
        ///     add any new images that were not referred to before.  Images may be de novo or pre-exisiting
        ///     in the database.  If already exisiting then just a link is added, if new the image is added
        ///     as well. On deletion, if there are no other links to the image then it is deleted, otherwise just the
        ///     link is deleted.
        /// </summary>
        /// <param name="listOfImages"></param>
        /// <param name="segment"></param>
        /// <param name="dc"></param>
        private static void UpdateSegmentImages(BulkObservableCollection<StoredImage> listOfImages,
            LabelledSegment segment, BatReferenceDBLinqDataContext dc)
        {
            if (segment == null || dc == null) return;
            if (listOfImages == null || listOfImages.Count <= 0)
            {
                DeleteImagesForSegment(segment, dc);
                return;
            }

            if (segment != null && dc != null)
            {
                //first delete any images linked to this call which are not in the list

                var allImagesForSegment = from cp in dc.SegmentDatas
                    where cp.SegmentId == segment.Id
                    select cp.BinaryData;
                // gets all images for the segment

                var newImageList = from image in listOfImages select image;
                // and all images in the new list

                var imagesToDelete = allImagesForSegment.AsEnumerable()
                    .Where(aic => newImageList.All(ni => ni.ImageID != aic.Id));
                // then all images in segment but not in new list

                if (!imagesToDelete.IsNullOrEmpty())
                {
                    foreach (var bd in imagesToDelete)
                    {
                        dc.SegmentDatas.DeleteAllOnSubmit(bd.SegmentDatas);
                        if (!(bd.BatPictures.Count > 0 || bd.CallPictures.Count > 0 || bd.SegmentDatas.Count > 1))
                            dc.BinaryDatas.DeleteOnSubmit(bd);
                    }

                    dc.SubmitChanges();
                }

                //then we modify existing images that might have changed

                // get all images for the segment which are also in the new list
                var matchingImages = newImageList.Where(newImage =>
                    allImagesForSegment.Any(oldImage => oldImage.Id == newImage.ImageID));
                if (!matchingImages.IsNullOrEmpty())
                    foreach (var modifiedImage in matchingImages)
                    {
                        var existingBinaryData = (from cp in dc.SegmentDatas
                            where cp.BinaryDataId == modifiedImage.ImageID && cp.SegmentId == segment.Id
                            select cp.BinaryData).FirstOrDefault();
                        if (existingBinaryData != null && existingBinaryData.Id >= 0)
                        {
                            existingBinaryData.Description = modifiedImage.GetCombinedText();
                            existingBinaryData.BinaryDataType = Tools.BlobType.PNG.ToString();
                            var bd = StoredImage.ConvertBitmapImageToPngBinary(modifiedImage.image,
                                modifiedImage.HorizontalGridlines, modifiedImage.VerticalGridLines);
                            existingBinaryData.BinaryData1 = bd ?? new Binary(new byte[0]);
                            dc.SubmitChanges();
                        }
                    }

                // Finally we add any new images
                //
                var imagesToAdd = newImageList.Where(newImage => newImage.ImageID < 0);
                if (!imagesToAdd.IsNullOrEmpty())
                {
                    foreach (var image in imagesToAdd)
                    {
                        var newBinaryData = image.GetAsBinaryData();
                        dc.BinaryDatas.InsertOnSubmit(newBinaryData);
                        dc.SubmitChanges();
                        var newSegmentImageLink = new SegmentData
                        {
                            SegmentId = segment.Id, BinaryDataId = newBinaryData.Id
                        };
                        dc.SegmentDatas.InsertOnSubmit(newSegmentImageLink);
                        dc.SubmitChanges();
                    }
                }
                else
                {
                    imagesToAdd = newImageList.Where(newImage => newImage.ImageID >= 0);
                    // we have an existing image to link into this segment
                    if (!imagesToAdd.IsNullOrEmpty())
                        foreach (var image in imagesToAdd)
                            if (image.ImageID >= 0)
                            {
                                var existingLink = from xsd in dc.SegmentDatas
                                    where xsd.BinaryDataId == image.ImageID && xsd.SegmentId == segment.Id
                                    select xsd;
                                if (existingLink.IsNullOrEmpty())
                                {
                                    // only create and insert a new link if there is not one already
                                    var sd = new SegmentData {BinaryDataId = image.ImageID, SegmentId = segment.Id};

                                    dc.SegmentDatas.InsertOnSubmit(sd);
                                    dc.SubmitChanges();
                                    sd.BinaryData.Description = image.GetCombinedText();
                                    dc.SubmitChanges();
                                }
                            }
                }
            }
        }

        /// <summary>
        ///     Updates the labelled segments.
        /// </summary>
        /// <param name="listOfSegmentAndBatList">
        ///     The list of segment and bat list.
        /// </param>
        /// <param name="id">
        ///     The identifier.
        /// </param>
        /// <param name="listOfSegmentImageLists"></param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        private static void UpdateLabelledSegments(BulkObservableCollection<SegmentAndBatList> listOfSegmentAndBatList,
            int id,
            BulkObservableCollection<BulkObservableCollection<StoredImage>> listOfSegmentImageLists,
            BatReferenceDBLinqDataContext dc)
        {
            if (!listOfSegmentAndBatList.IsNullOrEmpty())
                for (var i = 0; i < listOfSegmentAndBatList.Count; i++)

                    try
                    {
                        if (listOfSegmentImageLists != null && listOfSegmentImageLists.Count > i)
                            UpdateLabelledSegment(listOfSegmentAndBatList[i], id, listOfSegmentImageLists[i], dc);
                        else
                            UpdateLabelledSegment(listOfSegmentAndBatList[i], id, null, dc);
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorLog(ex.Message);
                        Debug.WriteLine("### at ListOfSegmentAndBatList " + i + " Threw {" + ex + "}");
                    }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static List<LabelledSegment> GetLabelledSegmentsToDelete(
            BulkObservableCollection<SegmentAndBatList> listOfSegmentAndBatList, int id,
            BatReferenceDBLinqDataContext dc)
        {
            var result = new List<LabelledSegment>();
            var labelledSegmentsForThisRecording = (from rec in dc.Recordings
                where rec.Id == id
                select rec.LabelledSegments).SingleOrDefault();
            if (!labelledSegmentsForThisRecording.IsNullOrEmpty())
                foreach (var seg in labelledSegmentsForThisRecording)
                {
                    var sbl = from sabl in listOfSegmentAndBatList
                        where sabl.Segment.Id == seg.Id
                        select sabl;
                    if (!sbl.IsNullOrEmpty()) result.Add(seg);
                }

            return result;
        }

        /// <summary>
        ///     Validates the call.
        /// </summary>
        /// <param name="newCall">
        ///     The new call.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool Validate(this Call newCall)
        {
            if (newCall == null) return false;
            var result = true;
            if ((newCall.StartFrequency ?? 50.0) < 10.0)
            {
                result = false;
            }
            else
            {
                if ((newCall.StartFrequencyVariation ?? 1.0) < 0.0)
                    result = false;
            }

            if ((newCall.EndFrequency ?? 50.0) < 10.0)
            {
                result = false;
            }
            else
            {
                if ((newCall.EndFrequencyVariation ?? 1.0) < 0.0)
                    result = false;
            }

            if ((newCall.PeakFrequency ?? 50.0) < 10.0)
            {
                result = false;
            }
            else
            {
                if ((newCall.PeakFrequencyVariation ?? 1.0) < 0.0)
                    result = false;
            }

            if ((newCall.PulseDuration ?? 50.0) <= 0.0)
            {
                result = false;
            }
            else
            {
                if ((newCall.PulseDurationVariation ?? 1.0) < 0.0)
                    result = false;
            }

            if ((newCall.PulseInterval ?? 50.0) <= 1.0)
            {
                result = false;
            }
            else
            {
                if ((newCall.PulseIntervalVariation ?? 1.0) < 0.0)
                    result = false;
            }

            if (newCall.StartFrequency == null && newCall.EndFrequency == null && newCall.PeakFrequency == null &&
                newCall.PulseDuration == null && newCall.PulseInterval == null &&
                string.IsNullOrWhiteSpace(newCall.CallType) &&
                string.IsNullOrWhiteSpace(newCall.CallFunction))
                result = false;

            return result;
        }

        internal static IQueryable<Recording> GetPagedRecordingList(int count, int startIndex, string p)
        {
            var dc = GetFastDataContext();

            return dc.Recordings.Skip(startIndex).Take(count).AsQueryable();
        }

        internal static int GetRecordingListCount()
        {
            var dc = GetFastDataContext();

            return dc.Recordings.Count();
        }

        /// <summary>
        ///     Returns an Observable collection of recordings that were made within the specified date range
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        internal static BulkObservableCollection<Recording> GetRecordingsInRange(DateTime startDate, DateTime endDate)
        {
            var result = new BulkObservableCollection<Recording>();
            var dc = GetFastDataContext();

            var recordings = from rec in dc.Recordings
                where rec.RecordingDate >= startDate.Date && rec.RecordingDate <= endDate.Date
                select rec;

            if (recordings != null)
            {
                var boc = new BulkObservableCollection<Recording>();
                boc.AddRange(recordings);
                result = boc;
            }

            return result;
        }


        internal static StoredImage GetImage(StoredImage existingImage)
        {
            var result = existingImage;
            if (existingImage != null && existingImage.ImageID >= 0)
                try
                {
                    var dc = GetFastDataContext();

                    var dbBlob = from blob in dc.BinaryDatas
                        where blob.Id == existingImage.ImageID
                        select blob;
                    if (!dbBlob.IsNullOrEmpty())
                        //var thisBlob = dbBlob.Single();
                        result = StoredImage.CreateFromBinary(dbBlob.Single());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("No matching blob image found:- " + ex.Message);
                }

            return result;
        }

        /// <summary>
        ///     given an imageID, checks to see if this image is linked to any labelled segments.
        ///     if so, returns those segments in a list.
        ///     If a linked segment has a duration of zero, then all of the segments in the parent recording
        ///     are added to the list and finally all duplicates are removed.
        /// </summary>
        /// <param name="imageID"></param>
        /// <returns></returns>
        internal static List<LabelledSegment> GetSegmentsForImage(int imageID)
        {
            var result = new List<LabelledSegment>();
            var dc = GetFastDataContext();
            var linkedSegments = from link in dc.SegmentDatas
                where link.BinaryDataId == imageID
                select link.LabelledSegment;
            if (!linkedSegments.IsNullOrEmpty())
                foreach (var seg in linkedSegments)
                    if ((seg.Duration() ?? new TimeSpan()).Ticks == 0L)
                        foreach (var segment in seg.Recording.LabelledSegments)
                            result.Add((segment.Duration() ?? new TimeSpan()).Ticks > 0L ? segment : seg);
            if (result.Count > 0) return result;
            // if we get here we could not find any segments associated with the image, so try looking for
            // recordings associated with the image instead
            BinaryData imageData = null;
            var imageDatas = dc.BinaryDatas.Where(bd => bd.Id == imageID);
            if (imageDatas.IsNullOrEmpty()) return result;
            imageData = imageDatas.First();

            if (imageData.Description == null) imageData.Description = "";
            if (imageData.Description.ToUpper().Contains(".WAV"))
            {
                var filename = Tools.ExtractWavFilename(imageData.Description);
                var recordings = from rec in dc.Recordings
                    where rec.RecordingName.ToUpper().Contains(filename.ToUpper())
                    select rec;
                if (!recordings.IsNullOrEmpty())
                    foreach (var seg in recordings.First().LabelledSegments)
                        if ((seg.Duration() ?? new TimeSpan()).Ticks > 0L)
                            result.Add(seg);
            }

            return result;
        }

        /// <summary>
        ///     Overload of GetImage to return a stored image for the biven ID in BinaryData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static StoredImage GetImage(int id)
        {
            var result = new StoredImage(null, "", "", id);
            result = GetImage(result);
            return result;
        }

        /// <summary>
        ///     Returns the number of RecordingSessions in the database
        /// </summary>
        /// <returns></returns>
        internal static int GetRecordingSessionCount()
        {
            var dc = GetFastDataContext();
            var result = dc.RecordingSessions.Count();
            return result;
        }

        internal static string GetDatabaseVersion()
        {
            try
            {
                var dc = GetFastDataContext();

                var versions = from ver in dc.Versions
                    select ver;
                if (versions.IsNullOrEmpty()) return "Undefined";

                return versions?.First()?.Version1.ToString();
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("From GetDatabaseVersion, returned error:- " + ex.Message);
                return "Undefined";
            }
        }

        /// <summary>
        ///     Deletes the recording supplied as a parameter and all LabelledSegments related to
        ///     that recording.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        public static string DeleteRecording(Recording recording)
        {
            string result = null;
            if (recording != null && recording.Id > 0)
                try
                {
                    var dc = GetFastDataContext();

                    var recordingToDelete = (from rec in dc.Recordings
                        where rec.Id == recording.Id
                        select rec).SingleOrDefault();
                    DeleteAllSegmentsInRecording(recordingToDelete, dc);

                    dc.Recordings.DeleteOnSubmit(recordingToDelete);
                    dc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    result = "Error deleting recording:- " + ex.Message;
                }

            return result;
        }

        /// <summary>
        ///     Returns a BulkObservableCollection of stored images belonging to the segment
        ///     Extension method for LabelledSegment
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static BulkObservableCollection<StoredImage> GetImageList(this LabelledSegment segment)
        {
            var result = new BulkObservableCollection<StoredImage>();
            if (segment != null)
            {
                var blobs = segment.SegmentDatas.Select(sdLnk => sdLnk.BinaryData);
                if (!blobs.IsNullOrEmpty())
                    foreach (var blob in blobs)
                    {
                        StoredImage si;
                        si = StoredImage.CreateFromBinary(blob);
                        result.Add(si);
                    }
            }

            return result;
        }

        internal static bool SessionTagExists(string path)
        {
            var dc = GetFastDataContext();

            if (dc.RecordingSessions.Any(rs => rs.SessionTag == path))
                return true;
            return false;
        }

        /// <summary>
        ///     Given a bat returns data about all the sessions that feature that bat organised into a collection of
        ///     BatSessionData
        /// </summary>
        /// <param name="batID"></param>
        /// <returns></returns>
        internal static IEnumerable<BatSessionData> GetBatSessionData(int batID)
        {
            var dc = GetFastDataContext();

            var result = Enumerable.Empty<BatSessionData>();

            result = from bsLink in dc.BatSessionLinks
                where bsLink.BatID == batID
                select new BatSessionData(
                    bsLink.SessionID,
                    bsLink.RecordingSession.SessionTag,
                    bsLink.RecordingSession.Location,
                    bsLink.RecordingSession.SessionDate,
                    bsLink.RecordingSession.SessionStartTime,
                    bsLink.BatID,
                    bsLink.Bat.Name,
                    (from bsLnk in dc.BatSegmentLinks.Where(lnk => lnk.BatID == batID)
                        join sdLnk in dc.SegmentDatas.Where(sd =>
                            sd.LabelledSegment.Recording.RecordingSessionId == bsLink.SessionID) on bsLnk
                            .LabelledSegmentID equals sdLnk.SegmentId
                        where bsLnk.LabelledSegment.StartOffset != bsLnk.LabelledSegment.EndOffset
                        select sdLnk.LabelledSegment).Count()
                    +
                    (from sdLnk in dc.SegmentDatas.Where(lnk =>
                            lnk.LabelledSegment.Recording.RecordingSessionId == bsLink.SessionID)
                        where sdLnk.LabelledSegment.StartOffset == sdLnk.LabelledSegment.EndOffset
                        join brLnk in dc.BatRecordingLinks.Where(brl => brl.BatID == batID) on sdLnk.LabelledSegment
                            .RecordingID equals brLnk.RecordingID
                        select sdLnk).Count(),

                    //dc.SegmentDatas.Where(sdLnk => sdLnk.LabelledSegment.Recording.RecordingSession.Id == bsLink.SessionID).Count(),
                    dc.BatRecordingLinks.Count(brLink =>
                        brLink.Recording.RecordingSessionId == bsLink.SessionID && brLink.BatID == batID)
                );

            return result;
        }

        internal static int GetRecordingSessionListCount()
        {
            var dc = GetFastDataContext();
            return dc.RecordingSessions.Count();
        }

        internal static IQueryable<RecordingSession> GetPagedRecordingSessionList(int pageSize, int topOfScreen,
            string field)
        {
            var dc = GetFastDataContext();
            return GetPagedRecordingSessionList(pageSize, topOfScreen, field, dc);
        }

        /// <summary>
        ///     Gets an enumerable/queryable of recordingSessions of length pagesize and starting at record topOfScreen after
        ///     having sorted on
        ///     field
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="topOfScreen"></param>
        /// <param name="field"></param>
        /// <param name="dc"></param>
        /// <returns></returns>
        internal static IQueryable<RecordingSession> GetPagedRecordingSessionList(int pageSize, int topOfScreen,
            string field, BatReferenceDBLinqDataContext dc)
        {
            var result = Enumerable.Empty<RecordingSession>().AsQueryable();
            if (pageSize <= 0)
            {
                pageSize = dc.RecordingSessions.Count();
                topOfScreen = 0;
            }

            result = string.IsNullOrWhiteSpace(field)
                ? dc.RecordingSessions.Skip(topOfScreen).Take(pageSize).AsQueryable()
                : dc.RecordingSessions.OrderBy(field).Skip(topOfScreen).Take(pageSize);

            /*

            switch (field)
            {
                case "NONE":
                    result = (from sess in dc.RecordingSessions

                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "DATE^":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.SessionDate
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "DATEv":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.SessionDate descending
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "TAG^":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.SessionTag
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "TAGv":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.SessionTag descending
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "LOCATION^":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.Location
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "LOCATIONv":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.Location descending
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "RECORDINGS^":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.Recordings.Count
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                case "RECORDINGSv":
                    result = (from sess in dc.RecordingSessions
                              orderby sess.Recordings.Count descending
                              select sess).Skip(topOfScreen).Take(pageSize);
                    break;

                default:
                    result = dc.RecordingSessions.Skip(topOfScreen).Take(pageSize).AsQueryable();
                    break;
            }*/

            return result;
        }

        /// <summary>
        ///     Returns a collection of BatRecordings related to the given session and bat
        ///     NB image count should include images for segments with the named bat +
        ///     images for recordings (segment endoffset==startofset) where the recording has a
        ///     segment featuring the named bat
        /// </summary>
        /// <param name="SessionId"></param>
        /// <param name="batId"></param>
        /// <returns></returns>
        internal static IEnumerable<BatSessionRecordingData> GetRecordingDataForBatSession(int SessionId, int batId)
        {
            var dc = GetFastDataContext();
            var result = Enumerable.Empty<BatSessionRecordingData>();

            result = from brLink in dc.BatRecordingLinks
                where brLink.BatID == batId && brLink.Recording.RecordingSessionId == SessionId
                select new BatSessionRecordingData(
                    brLink.Recording.RecordingSessionId,
                    brLink.RecordingID,
                    batId,
                    brLink.Recording.RecordingName,
                    brLink.Recording.RecordingDate,
                    brLink.Recording.RecordingStartTime,
                    dc.BatSegmentLinks.Count(lnk =>
                        lnk.LabelledSegment.RecordingID == brLink.RecordingID && lnk.BatID == batId),
                    (from bsLnk in dc.BatSegmentLinks
                        join sdLink in dc.SegmentDatas.Where(sdl =>
                                sdl.LabelledSegment.RecordingID == brLink.RecordingID) on
                            bsLnk.LabelledSegmentID equals sdLink.SegmentId
                        where bsLnk.BatID == batId &&
                              bsLnk.LabelledSegment.StartOffset != bsLnk.LabelledSegment.EndOffset
                        select sdLink.LabelledSegment).Count() +
                    (from sdLnk in dc.SegmentDatas.Where(sdl => sdl.LabelledSegment.RecordingID == brLink.RecordingID)
                        where sdLnk.LabelledSegment.StartOffset == sdLnk.LabelledSegment.EndOffset
                        join brLnk in dc.BatRecordingLinks.Where(brl => brl.BatID == batId) on sdLnk.LabelledSegment
                            .RecordingID equals brLnk.RecordingID
                        select sdLnk).Count()
                );

            return result;
        }

        internal static int GetBatSessionRecordingDataCount(List<int> batIdList, List<int> sessionIdList)
        {
            var dc = GetFastDataContext();
            try
            {
                var result = (from brLink in dc.BatRecordingLinks
                    where batIdList.Contains(brLink.BatID) &&
                          sessionIdList.Contains(brLink.Recording.RecordingSessionId ?? -1)
                    select brLink).Count();
                return result;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        ///     Like GetRecordingDataForBatSession in a paged form based on the supplied list of session and bat ids
        /// </summary>
        /// <param name="batIdList"></param>
        /// <param name="sessionIdList"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        internal static IEnumerable<BatSessionRecordingData> GetPagedBatSessionRecordingData(List<int> batIdList,
            List<int> sessionIdList, int startIndex, int count)
        {
            var dc = GetDataContext(false);
            var result = Enumerable.Empty<BatSessionRecordingData>();
            if (batIdList == null || sessionIdList == null || batIdList.Count <= 0 || sessionIdList.Count <= 0 ||
                startIndex < 0 || count <= 0) return result;

            var linkList = Enumerable.Empty<BatRecordingLink>();

            foreach (var sid in sessionIdList)
                linkList = linkList.Concat(from brLink in dc.BatRecordingLinks
                    where brLink.Recording.RecordingSessionId == sid
                    select brLink);

            var linkList2 = Enumerable.Empty<BatRecordingLink>();


            foreach (var bid in batIdList) linkList2 = linkList2.Concat(linkList.Where(brl => brl.BatID == bid));

            if (linkList2.IsNullOrEmpty()) return result;
            if (linkList2.Count() > startIndex) linkList2 = linkList2.Skip(startIndex);
            if (linkList2.Count() > count) linkList2 = linkList2.Take(count);

            if (linkList2.IsNullOrEmpty()) return result;
            result = from brLink in linkList2
                from rec in dc.Recordings.Where(r => r.Id == brLink.RecordingID).DefaultIfEmpty()
                select new BatSessionRecordingData(
                    rec.RecordingSessionId,
                    brLink.RecordingID,
                    brLink.BatID,
                    rec.RecordingName,
                    rec.RecordingDate,
                    rec.RecordingStartTime,
                    (from bsl in dc.BatSegmentLinks
                        from seg in dc.LabelledSegments.Where(ls => ls.Id == bsl.LabelledSegmentID).DefaultIfEmpty()
                        where seg.RecordingID == brLink.RecordingID && bsl.BatID == brLink.BatID
                        select bsl).Count(),
                    (from seg in dc.LabelledSegments.Where(ls => ls.RecordingID == brLink.RecordingID).DefaultIfEmpty()
                        join bsl in dc.BatSegmentLinks on seg.Id equals bsl.LabelledSegmentID
                        where bsl.BatID == brLink.BatID
                        join sdl in dc.SegmentDatas on seg.Id equals sdl.SegmentId
                        where seg.Id == bsl.LabelledSegmentID ||
                              seg.StartOffset == seg.EndOffset
                        select sdl).Count()
                );


            return result;
        }

        /// <summary>
        ///     Gets a collection of StoredImage from the specified recording and relating to the specified bat
        /// </summary>
        /// <param name="recordingId"></param>
        /// <param name="batId"></param>
        /// <returns></returns>
        internal static BulkObservableCollection<StoredImage> GetRecordingImagesForBat(int? recordingId, int batId)
        {
            var dc = GetFastDataContext();
            var result = new BulkObservableCollection<StoredImage>();

            var segImages = from sdLnk in dc.SegmentDatas
                join bsLnk in dc.BatSegmentLinks.Where(bsl => bsl.BatID == batId) on sdLnk.SegmentId equals bsLnk
                    .LabelledSegmentID
                where bsLnk.LabelledSegment.RecordingID == recordingId &&
                      bsLnk.LabelledSegment.StartOffset != bsLnk.LabelledSegment.EndOffset
                select sdLnk.BinaryData;
            if (segImages != null)
            {
                var segImages2 = from sdLlnk in dc.SegmentDatas
                    where sdLlnk.LabelledSegment.RecordingID == recordingId
                          && sdLlnk.LabelledSegment.StartOffset == sdLlnk.LabelledSegment.EndOffset
                    select sdLlnk.BinaryData;
                if (segImages2 != null) segImages = segImages.Concat(segImages2);
            }
            else
            {
                segImages = from sdLlnk in dc.SegmentDatas
                    where sdLlnk.LabelledSegment.RecordingID == recordingId
                          && sdLlnk.LabelledSegment.StartOffset == sdLlnk.LabelledSegment.EndOffset
                    select sdLlnk.BinaryData;
            }

            if (!segImages.IsNullOrEmpty())
                foreach (var img in segImages)
                {
                    var si = StoredImage.CreateFromBinary(img);
                    result.Add(si);
                }

            return result;
        }


        internal static int GetRecordingSessionDataCount()
        {
            var dc = GetFastDataContext();
            return dc.RecordingSessions.Count();
        }

        /// <summary>
        ///     Gets a page full of Recording session data formatted as RecordingSesssion Data Items
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="topOfScreen"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        internal static IEnumerable<RecordingSessionData> GetPagedRecordingSessionDataList(int pageSize,
            int topOfScreen, string field)
        {
            var dc = GetFastDataContext();
            var result = Enumerable.Empty<RecordingSessionData>();

            var sessions = GetPagedRecordingSessionList(pageSize, topOfScreen, field, dc);
            if (!sessions.IsNullOrEmpty())
                try
                {
                    result = (from sess in sessions
                        select new
                            RecordingSessionData(
                                sess.Id,
                                sess.SessionTag,
                                sess.Location,
                                sess.SessionDate,
                                sess.SessionStartTime,
                                dc.SegmentDatas.Count(
                                    lnk => lnk.LabelledSegment.Recording.RecordingSessionId == sess.Id),
                                sess.Recordings.Count
                            )).AsEnumerable();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error reading data:- " + ex.Message);
                    Tools.ErrorLog(ex.Message);
                }

            return result;
        }

        /// <summary>
        ///     Returns the number of recordings that relate to this bat
        /// </summary>
        /// <param name="bat"></param>
        /// <returns></returns>
        internal static int GetNumberOfRecordingsForBat(Bat bat)
        {
            //int result = (from lnk in bat.BatSegmentLinks
            //              select lnk.LabelledSegment.Recording).Distinct().Count();
            var result = bat.BatRecordingLinks.Count;
            return result;
        }

        internal static int GetNumberOfSessionsForBat(Bat bat)
        {
            if (bat == null) return 0;
            //int result = bat.GetSessions().Count;
            var result = bat.BatSessionLinks.Count;
            return result;
        }

        /// <summary>
        ///     Returns a BOC(StoredImage) contaiing all bat images and all call images for the
        ///     specified species of bat.
        /// </summary>
        /// <param name="bat"></param>
        /// <returns></returns>
        internal static BulkObservableCollection<StoredImage> GetBatAndCallImagesForBat(Bat bat)
        {
            var result = new BulkObservableCollection<StoredImage>();
            try
            {
                if (bat != null) result.AddRange(bat.GetImageList());
                var callList = from link in bat.BatCalls
                    select link.Call;
                if (callList != null)
                    foreach (var call in callList)
                        result.AddRange(call.GetImageList());
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error getting bat and call images for " + bat.Name + ":- " + ex.Message);
            }

            return result;
        }

        /// <summary>
        ///     returns true if their is any RecordingSession with the specified Path as a folder name
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        internal static bool FolderExists(string folder)
        {
            if (!string.IsNullOrWhiteSpace(folder))
            {
                folder = folder.Replace('#', ' ').Trim();
                if (!folder.EndsWith(@"\")) folder = folder + @"\";

                var dc = GetFastDataContext();
                var matchingSessions = dc.RecordingSessions.Select(sess => sess.OriginalFilePath).Contains(folder);
                //var matchingSessions = (from sess in dc.RecordingSessions
                //                       where sess.OriginalFilePath == folder
                //                       select sess).Any();
                return matchingSessions;
            }

            return false;
        }

        internal static BulkObservableCollection<StoredImage> GetAllImagesForBat(Bat bat)
        {
            var result = new BulkObservableCollection<StoredImage>();
            try
            {
                if (bat != null) result.AddRange(GetBatAndCallImagesForBat(bat));
                if (bat.BatSegmentLinks != null)
                {
                    var segmentsForBat = from link in bat.BatSegmentLinks
                        where link.LabelledSegment.SegmentDatas.Count > 0
                        select link.LabelledSegment;
                    if (!segmentsForBat.IsNullOrEmpty())
                        foreach (var seg in segmentsForBat)
                            result.AddRange(seg.GetImageList());
                }

                //var importedSegmentsForBat = from link in DBAccess.GetImportedSegmentDatasForBat(bat)
                //                             select link.LabelledSegment;
                //if (!importedSegmentsForBat.IsNullOrEmpty())
                //{
                //    foreach(var seg in importedSegmentsForBat)
                //    {
                //        if (seg != null)
                //        {
                //           result.AddRange(seg.GetImageList());
                //       }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error getting all images for " + bat.Name + ":- " + ex.Message);
            }

            return result;
        }

        internal static int GetNumRecordingImagesForBat(int id)
        {
            var dc = GetFastDataContext();
            var result = (from bsLnk in dc.BatSegmentLinks
                join sdLnk in dc.SegmentDatas on bsLnk.LabelledSegmentID equals sdLnk.SegmentId
                where bsLnk.BatID == id && bsLnk.LabelledSegment.StartOffset != bsLnk.LabelledSegment.EndOffset
                select sdLnk.LabelledSegment).Count();

            var fullRecordImages = (from sdLnk in dc.SegmentDatas
                where sdLnk.LabelledSegment.StartOffset == sdLnk.LabelledSegment.EndOffset
                join brLnk in dc.BatRecordingLinks.Where(brl => brl.BatID == id) on sdLnk.LabelledSegment.RecordingID
                    equals brLnk.RecordingID
                select sdLnk).Count();

            return result + fullRecordImages;
        }

        /// <summary>
        ///     Returns the BinaryData blob for the given ID
        /// </summary>
        /// <param name="imageID"></param>
        /// <returns></returns>
        internal static BinaryData GetBlob(int imageID)
        {
            BinaryData result = null;
            var dc = GetFastDataContext();
            if (imageID >= 0)
            {
                var blobs = from blob in dc.BinaryDatas
                    where blob.Id == imageID
                    select blob;
                if (!blobs.IsNullOrEmpty()) result = blobs.First();
            }

            return result;
        }

        /// <summary>
        ///     Extension Method on Call.GetImageList()
        ///     returns a collection of StoredImage for the call.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public static BulkObservableCollection<StoredImage> GetImageList(this Call call)
        {
            var result = new BulkObservableCollection<StoredImage>();
            try
            {
                if (call == null) return null;

                var binaryCollection = call.CallPictures.Select(cp => cp.BinaryData);
                if (!binaryCollection.IsNullOrEmpty())
                    foreach (var bin in binaryCollection)
                    {
                        var si = StoredImage.CreateFromBinary(bin);

                        result.Add(si);
                    }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog("Error getting images for call:- " + ex.Message);
            }

            return result;
        }

        /// <summary>
        ///     Returns true if there is an image with the given ID in the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool ImageExists(int id)
        {
            var result = false;
            var dc = GetFastDataContext();
            var images = from img in dc.BinaryDatas
                where img.Id == id
                select img;
            if (!images.IsNullOrEmpty()) result = true;
            return result;
        }

        /// <summary>
        ///     Deletes the link between the deletedImage and the selected seement identified by their
        ///     IDs.  If the image has no remaining links then it is deleted completely.
        /// </summary>
        /// <param name="deletedImageID"></param>
        /// <param name="selectedSegmentId"></param>
        internal static void DeleteImageForSegment(int deletedImageID, int selectedSegmentId)
        {
            var dc = GetDataContext();
            var links = from link in dc.SegmentDatas
                where link.BinaryDataId == deletedImageID && link.SegmentId == selectedSegmentId
                select link;
            if (links.IsNullOrEmpty()) return;
            dc.SegmentDatas.DeleteOnSubmit(links.FirstOrDefault());
            dc.SubmitChanges();

            var images = from img in dc.BinaryDatas
                where img.Id == deletedImageID
                select img;
            if (images.IsNullOrEmpty()) return;
            var toDelete = images.FirstOrDefault();
            var numLinks = toDelete.BatPictures.Count + toDelete.CallPictures.Count + toDelete.SegmentDatas.Count;
            if (numLinks <= 0)
            {
                dc.BinaryDatas.DeleteOnSubmit(toDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Returns an Observable Collection if recording sessions encompassed by the (inclusive) range of dates
        ///     given
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        internal static BulkObservableCollection<RecordingSession> GetSessionsInDateRange(DateTime startDate,
            DateTime endDate)
        {
            var dc = GetFastDataContext();
            var sessions = from sess in dc.RecordingSessions
                where sess.SessionDate >= startDate && sess.SessionDate <= endDate ||
                      sess.EndDate >= startDate && sess.EndDate <= endDate
                select sess;
            if (sessions != null)
            {
                var boc = new BulkObservableCollection<RecordingSession>();
                boc.AddRange(sessions);

                return boc;
            }

            return new BulkObservableCollection<RecordingSession>();
        }

        /// <summary>
        ///     converts the recordings GPS co-ordinates in the form of strings into a pair of doubles
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        public static void GetGpSasDouble(this Recording recording, out double latitude, out double longitude)
        {
            latitude = 200.0d;
            longitude = 200.0d;
            if (recording == null || string.IsNullOrWhiteSpace(recording.RecordingGPSLatitude) ||
                string.IsNullOrWhiteSpace(recording.RecordingGPSLongitude)) return;
            double.TryParse(recording.RecordingGPSLatitude, out latitude);
            double.TryParse(recording.RecordingGPSLongitude, out longitude);
        }

        /// <summary>
        ///     Extension method on Recording.GetImageList(Bat)
        ///     returns a collection of StoredImage of all the images for all the segments
        ///     of the recording.  If Bat is not null then aonly includes images for segments
        ///     that tag the specifried bat, otherwise returns images for all segments.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="bat"></param>
        /// <returns></returns>
        public static BulkObservableCollection<StoredImage> GetImageList(this Recording recording, int batId)
        {
            var result = new BulkObservableCollection<StoredImage>();
            IEnumerable<BinaryData> imgs;

            var bat = GetBat(batId);
            if (bat != null)
                //imgs = from seg in recording.LabelledSegments
                //       from bsLink in seg.BatSegmentLinks
                //       where bsLink.BatID == bat.Id
                //       from sdLink in seg.SegmentDatas
                //       select sdLink.BinaryData;
                imgs = from bsLnk in bat.BatSegmentLinks
                    where bsLnk.LabelledSegment.RecordingID == recording.Id
                    from segData in bsLnk.LabelledSegment.SegmentDatas
                    select segData.BinaryData;
            else
                imgs = from seg in recording.LabelledSegments
                    from sdLink in seg.SegmentDatas
                    select sdLink.BinaryData;

            if (!imgs.IsNullOrEmpty())
                foreach (var img in imgs)
                {
                    var si = StoredImage.CreateFromBinary(img);

                    result.Add(si);
                }

            return result;
        }

        private static Bat GetBat(int batId)
        {
            var dc = GetFastDataContext();
            return dc.Bats.FirstOrDefault(bat => bat.Id == batId);
        }

        /// <summary>
        ///     Extension Method on Recording.GetImageCount(bat)
        ///     returns the number of images in every segment of the recording
        ///     in which the specified bat is tagged.  If bat is null returns the
        ///     image count for all segments in the recording.
        /// </summary>
        /// <param name="recording"></param>
        /// <param name="bat"></param>
        /// <param name="recordingHasBat"></param>
        /// <returns></returns>
        public static int GetImageCount(this Recording recording, Bat bat, out bool recordingHasBat)
        {
            recordingHasBat = false;
            var result = 0;
            try
            {
                if (bat != null)
                {
                    //  Stopwatch watch2 = Stopwatch.StartNew();
                    var segmentsWithBat = from seg in recording.LabelledSegments
                        from bsLink in seg.BatSegmentLinks
                        where bsLink.BatID == bat.Id
                        select bsLink.LabelledSegment;

                    if (!segmentsWithBat.IsNullOrEmpty())
                    {
                        recordingHasBat = true;
                        result = (from seg in segmentsWithBat
                            from sdLink in seg.SegmentDatas
                            select sdLink.BinaryData).Count();
                    }
                    else
                    {
                        result = 0;
                    }
                    //     watch2.Stop();

                    //     Debug.WriteLine("Times for GetImageCount are, "+ watch2.ElapsedMilliseconds );
                }
                else
                {
                    result = (from seg in recording.LabelledSegments
                        from sdLink in seg.SegmentDatas
                        select sdLink.BinaryData).Count();
                }

                //var importedSegmentDatas = DBAccess.GetImportedSegmentDatasForBat(bat, recording);
                //result += (from sd in importedSegmentDatas
                //          where sd.LabelledSegment.RecordingID == recording.Id
                //        select sd).Count();
                //result += importedSegmentDatas.Count;
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                Debug.WriteLine("Recording.GetImageCount error:- " + ex.Message);
            }

            return result;
        }

        /// <summary>
        ///     Extension method Bat.GetImageList()
        ///     returns a collection of images for the Bat.  If Bat is null
        ///     returns a null, otherwise returns a collection, empty if necessary.
        ///     It is assumed that all stored blobs in BinaryDatas are images.
        /// </summary>
        /// <param name="bat"></param>
        /// <returns></returns>
        public static BulkObservableCollection<StoredImage> GetImageList(this Bat bat)
        {
            var result = new BulkObservableCollection<StoredImage>();
            if (bat == null) return null;

            var imgs = from bpLink in bat.BatPictures
                select bpLink.BinaryData;

            if (!imgs.IsNullOrEmpty())
                foreach (var img in imgs)
                {
                    var si = StoredImage.CreateFromBinary(img);

                    result.Add(si);
                }

            return result;
        }

        /// <summary>
        ///     Deletes a BinaryData item and any associated links from link tables
        ///     SegmentData
        ///     BatPicture
        ///     CallPicture
        /// </summary>
        /// <param name="BlobID">The database ID of the binary data to be deleted</param>
        internal static void DeleteBinaryData(int BlobID)
        {
            var dc = GetDataContext();
            if (BlobID < 0) return;
            var segmentLinks = from sd in dc.SegmentDatas
                where sd.BinaryDataId == BlobID
                select sd;
            if (!segmentLinks.IsNullOrEmpty()) dc.SegmentDatas.DeleteAllOnSubmit(segmentLinks);
            var batLinks = from bl in dc.BatPictures
                where bl.BinaryDataId == BlobID
                select bl;
            if (!batLinks.IsNullOrEmpty()) dc.BatPictures.DeleteAllOnSubmit(batLinks);
            var callLinks = from cl in dc.CallPictures
                where cl.BinaryDataID == BlobID
                select cl;
            if (!callLinks.IsNullOrEmpty()) dc.CallPictures.DeleteAllOnSubmit(callLinks);
            dc.SubmitChanges();
            var blobs = from blob in dc.BinaryDatas
                where blob.Id == BlobID
                select blob;
            if (!blobs.IsNullOrEmpty()) dc.BinaryDatas.DeleteAllOnSubmit(blobs);
            dc.SubmitChanges();
        }

        /// <summary>
        ///     returns a list of all bats encountered in all of the listed recordingsessions.
        /// </summary>
        /// <param name="reportSessionList"></param>
        /// <returns></returns>
        internal static BulkObservableCollection<Bat> GetBatsForTheseSessions(
            BulkObservableCollection<RecordingSession> reportSessionList)
        {
            var result = new BulkObservableCollection<Bat>();
            var bats = Enumerable.Empty<Bat>();
            var batList = new List<Bat>();
            var dc = GetFastDataContext();
            if (!reportSessionList.IsNullOrEmpty())
                bats = (from sess in reportSessionList
                    join bsl in dc.BatSegmentLinks on sess.Id equals bsl.LabelledSegment.Recording.RecordingSessionId
                    select bsl.Bat).Distinct();

            result.AddRange(bats);
            return result;
        }

        /// <summary>
        ///     Gets the recording period in the form of a Tupe of DateTime for the start and end times,
        ///     using the time of the earliest recording and the time of the last recording rather than
        ///     the more loosely defined session start and end times.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        internal static Tuple<DateTime, DateTime> GetRecordingPeriod(RecordingSession session)
        {
            if (session == null || session.Recordings.IsNullOrEmpty())
                return new Tuple<DateTime, DateTime>(new DateTime(), new DateTime());
            //BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var firstRecording = (from rec in session.Recordings
                    select (rec.RecordingDate ?? session.SessionDate).Date + (rec.RecordingStartTime ?? new TimeSpan()))
                .Min();

            var lastRecording = (from rec in session.Recordings
                select (rec.RecordingDate ?? session.EndDate ?? session.SessionDate).Date +
                       (rec.RecordingEndTime ?? new TimeSpan())).Max();

            var result = new Tuple<DateTime, DateTime>(firstRecording, lastRecording);
            return result;
        }

        /// <summary>
        ///     Updates a single labelledSegment which already exists in the database.
        ///     The segment is identified by the ID in the passed parameter.
        ///     NB does not update BatSegmentLinks
        /// </summary>
        /// <param name="segment"></param>
        internal static void UpdateLabelledSegment(LabelledSegment segment)
        {
            LabelledSegment existingsegment = null;
            try
            {
                var dc = GetDataContext();
                var existingsegments = from seg in dc.LabelledSegments
                    where seg.Id == segment.Id
                    select seg;
                if (!existingsegments.IsNullOrEmpty()) existingsegment = existingsegments.Single();
                if (existingsegment != null)
                {
                    existingsegment.StartOffset = segment.StartOffset;
                    existingsegment.EndOffset = segment.EndOffset;
                    existingsegment.Comment = segment.Comment;
                    dc.SubmitChanges();
                }
            }
            catch (SqlException sqlex)
            {
                Tools.ErrorLog(sqlex.Message);
                Debug.WriteLine("#]#]#] --- " + sqlex);
                Debug.WriteLine("#]#]#] --- " + sqlex.StackTrace);
                Debug.WriteLine("#]#]#] --- " + existingsegment);
            }
        }

        /// <summary>
        ///     Returns the number of minutes in the specified time window which contain instances of
        ///     the specified bat for the specified session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="bat"></param>
        /// <param name="samplePeriod"></param>
        /// <returns></returns>
        internal static int GetOccurrencesInWindow(RecordingSession session, Bat bat, DateTime periodStart,
            int aggregationPeriod)
        {
            Debug.WriteLine("GetOccurrences in Window from " + periodStart + " For " + aggregationPeriod + " Minutes");
            var result = 0;
            if (session == null || bat == null || aggregationPeriod <= 0) return 0;


            var sampleStart = periodStart.TimeOfDay;
            var sampleEnd = sampleStart + new TimeSpan(0, aggregationPeriod, 0);
            if (sampleEnd.Days > 0)
            {
                var minsBeforeMidnight = aggregationPeriod;
                var minsAfterMidnight = 0;
                while ((sampleStart + new TimeSpan(0, minsBeforeMidnight, 0)).Days > 0)
                {
                    minsBeforeMidnight--;
                    minsAfterMidnight++;
                    if (minsBeforeMidnight < 0)
                    {
                        Debug.WriteLine("GetOccurrencesinWindow Decremented mins Before Midnight to less than 0");
                        return 0;
                    }
                }

                result = GetOccurrencesInWindow(session, bat, periodStart, minsBeforeMidnight);
                result += GetOccurrencesInWindow(session, bat, new DateTime(), minsAfterMidnight);
                return result;
            }


            var aggregationCells = aggregationPeriod;

            Debug.WriteLine("Period from " + sampleStart + " to " + sampleEnd + " in " + aggregationCells);

            var dc = GetDataContext();
            var tDay = new TimeSpan(12, 0, 0);

            var segs = from seg in dc.LabelledSegments
                from link in seg.BatSegmentLinks
                where seg.Recording.RecordingSessionId == session.Id &&
                      link.BatID == bat.Id && seg.Recording.RecordingStartTime != null &&
                      seg.Recording.RecordingEndTime != null &&
                      !(seg.Recording.RecordingStartTime.Value > sampleEnd) &&
                      !(seg.Recording.RecordingEndTime.Value < sampleStart)
                select seg;
            if (!segs.IsNullOrEmpty())
            {
                //Debug.WriteLine("GetOccurrencesInWindow found " + segs.Count() + " segments for " + bat.Name);

                var oneMinute = new TimeSpan(0, 1, 0);
                for (var i = 0; i < aggregationCells; i++, sampleStart = sampleStart + oneMinute)
                {
                    foreach (var seg in segs)
                    {
                        if (seg.Recording.RecordingStartTime != null && seg.Recording.RecordingStartTime.Value > sampleStart + oneMinute) continue;
                        if (seg.Recording.RecordingEndTime != null && seg.Recording.RecordingEndTime < sampleStart)
                            continue;
                        var realStartTime = seg.Recording.RecordingStartTime.Value + seg.StartOffset;
                        if (realStartTime >= sampleStart && realStartTime < sampleStart + oneMinute)
                        {
                            result++;
                            break; // foreach
                        }
                    }

                    if (result > 0)
                    {
                        //Debug.WriteLine("For period " + i + " result was " + Result);
                    }
                }
            }
            /*
            foreach(var seg in segs)// all the labelled segments featuring this bat in this session
            {
                Result += Tools.SegmentOverlap(seg, samplePeriod);
            }*/

            return result;

            // get the labelled segments for the session that overlap the period and include the bat
            // then sum the sizes of the overlaps in minutes.
        }

        /// <summary>
        ///     Given a fully qualified name of a .wav file, locates the corresponding
        ///     Recording based on the file name as a recording label or as the specified
        ///     filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal static Recording GetRecordingForWavFile(string filename)
        {
            var dc = GetFastDataContext();
            return GetRecordingForWavFile(filename, dc);
        }

        /// <summary>
        ///     Given a fully qualified name of a .wav file, locates the corresponding
        ///     Recording based on the file name as a recording label or as the specified
        ///     filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dc"></param>
        /// <returns></returns>
        internal static Recording GetRecordingForWavFile(string filename, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = GetFastDataContext();
            Recording result = null;

            if (!string.IsNullOrWhiteSpace(filename))
                if (filename.ToUpper().Contains(".WAV"))
                {
                    //if (filename.Contains(@"\"))
                    //{
                    //    var parts = filename.Split('\\');
                    //    filename = parts[parts.Count() - 1]; // strip down to unqualified file name
                    //}
                    filename = filename.ExtractFilename(@".wav");
                    /*
                    filename = @"\"+filename.Substring(0, filename.Length - 4)+".wav"; // strip off the .wav
                    // matches using contains so the name field may or may not contain the .wav part
                    var recordings = from rec in dc.Recordings
                                     where rec.RecordingName==filename
                                     select rec;
                                     */

                    var fnameParts = filename.Substring(0, filename.Length - 4)
                        .Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries);
                    if (fnameParts.Length == 1)
                    {
                        var recordings = from rec in dc.Recordings
                            where rec.RecordingName.Contains(fnameParts[0])
                            select rec;

                        if (!recordings.IsNullOrEmpty()) result = recordings.First();
                    }
                    else if (fnameParts.Length == 2)
                    {
                        var recordings = from rec in dc.Recordings
                            where rec.RecordingName.Contains(fnameParts[0]) &&
                                  rec.RecordingName.Contains(fnameParts[1])
                            select rec;

                        if (!recordings.IsNullOrEmpty()) result = recordings.First();
                    }
                    else if (fnameParts.Length >= 3)
                    {
                        var recordings = from rec in dc.Recordings
                            where rec.RecordingName.Contains(fnameParts[0])
                            select rec;

                        recordings = from rec in recordings
                            where rec.RecordingName.Contains(fnameParts[1])
                            select rec;

                        recordings = from rec in recordings
                            where rec.RecordingName.Contains(fnameParts[2])
                            select rec;

                        if (!recordings.IsNullOrEmpty()) result = recordings.First();
                    }
                }

            return result;
        }

        internal static string GetRecordingSessionNotes(int currentRecordingSessionId)
        {
            try
            {
                var dc = GetFastDataContext();
                return dc.RecordingSessions.Single(sess => sess.Id == currentRecordingSessionId).SessionNotes;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        ///     Updates an existing image in the database to match the caption and
        ///     dewscription in a supplied image.  The image field itself is not
        ///     modified and matching is on the basis of the imageID in the supplied
        ///     image.
        /// </summary>
        /// <param name="selectedImage"></param>
        internal static void UpdateImage(StoredImage selectedImage)
        {
            var dc = GetDataContext();
            var identical = Enumerable.Empty<BinaryData>().AsQueryable();
            var existingImages = Enumerable.Empty<BinaryData>().AsQueryable();
            if (selectedImage.ImageID < 0)
            {
                identical = from img in dc.BinaryDatas
                    where img.Description == selectedImage.GetCombinedText()
                    select img;
                if (identical.IsNullOrEmpty())
                {
                    selectedImage = InsertImage(selectedImage);
                    if (selectedImage == null) return;
                }
            }

            BinaryData existingImage = null;
            if (identical.IsNullOrEmpty())
                existingImages = from img in dc.BinaryDatas
                    where img.Id == selectedImage.ImageID
                    select img;
            else
                existingImages = identical;
            if (!existingImages.IsNullOrEmpty()) existingImage = existingImages.First();
            if (existingImage != null)
            {
                existingImage.BinaryData1 = selectedImage.GetAsBinaryData().BinaryData1;
                existingImage.Description = selectedImage.GetCombinedText();
                dc.SubmitChanges();
            }
            else
            {
                selectedImage.ImageID = -1;
                InsertImage(selectedImage);
            }
        }

        /// <summary>
        ///     Inserts a new StoredImage into the database without any links to other objects.
        ///     If the image already exists then calls UpdateImage(image) instead.
        /// </summary>
        /// <param name="image"></param>
        public static StoredImage InsertImage(StoredImage image)
        {
            if (image.ImageID >= 0)
            {
                UpdateImage(image);
            }
            else
            {
                var dc = GetDataContext();
                var binaryData = image.GetAsBinaryData();
                if (binaryData != null)
                {
                    dc.BinaryDatas.InsertOnSubmit(binaryData);
                    dc.SubmitChanges();
                    ResolveOrphan(binaryData, dc);
                    image.ImageID = binaryData.Id;
                    image = StoredImage.CreateFromBinary(binaryData);
                }
            }

            return image;
        }

        /// <summary>
        ///     Gets the matching bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        /// </returns>
        public static Bat GetMatchingBat(Bat bat)
        {
            var dataContext = GetFastDataContext();
            return GetMatchingBat(bat, dataContext);
        }

        /// <summary>
        ///     Gets the named bat. Returns the bat identified by a specified Name or null if the bat
        ///     does not exist in the database
        /// </summary>
        /// <param name="newBatName">
        ///     New name of the bat.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public static Bat GetNamedBat(string newBatName)
        {
            var dataContext = GetFastDataContext();
            return GetNamedBat(newBatName, dataContext);
        }

        /// <summary>
        ///     Returns a list of all known bats sorted on SortOrder
        /// </summary>
        /// <returns>
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static BulkObservableCollection<Bat> GetSortedBatList()
        {
            var dc = GetFastDataContext();
            var result = new BulkObservableCollection<Bat>();
            result.AddRange(dc.Bats.OrderBy(bat => bat.SortIndex));
            /* from bat in dc.Bats
                       orderby bat.SortIndex
                       select bat;*/
            return result;
        }

        /// <summary>
        ///     Inserts the bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        /// </returns>
        public static string InsertBat(Bat bat)
        {
            var dataContext = GetDataContext();
            return InsertBat(bat, dataContext);
        }

        /// <summary>
        ///     Extension Methid on RecordingSession.GetImageList()
        ///     returns a collection of all the images for all the segments
        ///     in all the recordings for the RecordingSession.
        ///     Returns at least an empty list.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static BulkObservableCollection<StoredImage> GetImageList(this RecordingSession session)
        {
            var result = new BulkObservableCollection<StoredImage>();

            var imgs = from rec in session.Recordings
                from seg in rec.LabelledSegments
                from sd in seg.SegmentDatas
                select sd.BinaryData;

            if (!imgs.IsNullOrEmpty())
                foreach (var img in imgs)
                {
                    var si = StoredImage.CreateFromBinary(img);

                    result.Add(si);
                }

            return result;
        }

        /// <summary>
        ///     Merges the bat. The supplied bat is either inserted into the database if it is not
        ///     already there, or, if a bat with the same genus and species is present then the data
        ///     in this bat is added to and merged with the data for the existing bat. Sort orders
        ///     are taken from the new bat and duplicate tags or common names are removed, otherwise
        ///     any tag or common name differing ins pelling or capitalization will be treated as a
        ///     new item. Existing tags or common names which do not exist in the new bat will be
        ///     removed. Notes from the new bat will replace notes in the existing bat. The bat
        ///     'name' will be updated to reflect the common name with the lowest sort index. Returns
        ///     a suitable error message if the process failed, or an empty string if the process was successful;
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dataContext"></param>
        /// <returns>
        /// </returns>
        public static string MergeBat(Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            return MergeBat(bat, dataContext, out var newBat);
        }

        private static readonly string DbVersion = "v5.31";

        private static readonly string DBFileName = "BatReferenceDBv5.31.mdf";

        private static readonly string DBLocation = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Echolocation\WinBLP\");

        /// <summary>
        ///     dbVersionDec is the decimal format version of the currently expected database
        ///     This is the database version, not the program version.
        ///     v5.31 Original base format
        ///     v6.0 :-
        ///     @"ALTER TABLE [dbo].[RecordingSession] ADD[EndDate] DATETIME NULL;"
        ///     @"ALTER TABLE [dbo].[Recording] ADD[RecordingDate] DATETIME NULL;"
        ///     v6.1 :-
        ///     @"ALTER TABLE [dbo].[BinaryData] ALTER COLUMN [Description] NVARCHAR(MAX) NULL;"
        ///     v6.2 :- add two new link tables and update the existing data
        ///     @"CREATE TABLE [dbo].[BatSession]  (
        ///     [Id] INT NOT NULL PRIMARY KEY,     [SessionID] INT NOT NULL DEFAULT -1,    [BatID] INT NOT NULL DEFAULT -1
        ///     CONSTRAINT[Bat_BatSession] FOREIGN KEY([BatID]) REFERENCES[dbo].[Bat] ([Id]),
        ///     CONSTRAINT[RecordingSession_BatSession] FOREIGN KEY([SessionID]) REFERENCES[dbo].[RecordingSession] ([Id])
        ///     )
        ///     CREATE TABLE[dbo].[BatRecording] (
        ///     [Id] INT NOT NULL PRIMARY KEY,     [BatID] INT NOT NULL DEFAULT -1,    [RecordingID] INT NOT NULL DEFAULT -1,
        ///     CONSTRAINT[Bat_BatRecording] FOREIGN KEY([BatID]) REFERENCES[dbo].[Bat] ([Id]),
        ///     CONSTRAINT[Recording_BatRecording] FOREIGN KEY([RecordingID]) REFERENCES[dbo].[Recording] ([Id])"
        /// </summary>
        private static readonly decimal DbVersionDec = 6.2m;
    } // end DBAccess

    //###########################################################################################################################################

    /// <summary>
    ///     Class to add functionality to the String class
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Truncates to the specified maximum length.
        /// </summary>
        /// <param name="s">
        ///     The s.
        /// </param>
        /// <param name="maxLength">
        ///     The maximum length.
        /// </param>
        /// <returns>
        /// </returns>
        public static string Truncate(this string s, int maxLength)
        {
            if (!string.IsNullOrWhiteSpace(s))
                if (s.Length > maxLength)
                    return s.Substring(0, maxLength - 1);
            return s;
        }

        /// <summary>
        ///     Extracts a filename (or other substring) from the calling string the extracted
        ///     portion ending with the specified portion (typically the expected extension)
        ///     and then removing everything prior to the last occuring backslash (if any) in order
        ///     to remove the path.
        ///     If the terminating string is not found then it returns an empty string.
        ///     The function is not case sensitivie
        /// </summary>
        /// <param name="s"></param>
        /// <param name="endingwith"></param>
        /// <returns></returns>
        public static string ExtractFilename(this string s, string endingwith)
        {
            var result = "";
            if (s.ToUpper().Contains(endingwith.ToUpper()))
            {
                result = !string.IsNullOrEmpty(endingwith)
                    ? s.Substring(0, s.ToUpper().IndexOf(endingwith.ToUpper()) + endingwith.Length)
                    : s;
                if (result.Contains(@"\")) result = result.Substring(result.LastIndexOf(@"\") + 1);
            }

            return result;
        }
    }
}