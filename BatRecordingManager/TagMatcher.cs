﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BatRecordingManager
{
    /// <summary>
    ///     A class to manage the complex process of finding all th bats which have tags contained
    ///     ina given description, bearing in mind that some tags may be substrings of other tags
    /// </summary>
    internal class TagMatcher
    {
        private string _moddedDescription = "";

        /// <summary>
        ///     The tag list of all tags in the database and their accompanying bats Derived from
        ///     the BatTags table and converted to a simple List of Bat.
        /// </summary>
        private readonly List<BatTag> _tagList;

        public TagMatcher(List<BatTag> tagList)
        {
            this._tagList = tagList;
        }

        /// <summary>
        ///     Matches the specified descrition against the list of tags provided within the
        ///     constructor. It returns a simple a lit of Bat of all bats that have tags contained
        ///     within the string provided bearing in mind that some bats have tags which are
        ///     substrings of other bats. Matches the tags to the description from longest tag to
        ///     shortest tag so that substrings can only be matched after the strings that contain
        ///     them. As each match is made the matching part of the descritpion is removed so that
        ///     it cannot ever match more than a single bat
        /// </summary>
        /// <param name="description">
        ///     The description.
        /// </param>
        /// <returns>
        /// </returns>
        internal List<Bat> Match(string description)
        {
            var batList = new List<Bat>();
            _moddedDescription = description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var containedTags = GetAllTags(description);
                if (containedTags != null && containedTags.Count > 0)
                    foreach (var tag in containedTags)
                    {
                        var position =
                            MatchTag(description, tag.BatTag1); // gives the position of the tag in the description
                        if (position >= 0)
                        {
                            // we had a match
                            description = description.Remove(position,
                                Math.Min(tag.BatTag1.Length, description.Length));
                            _moddedDescription = _moddedDescription.Replace(tag.BatTag1, tag.Bat.Name);
                            batList.Add(tag.Bat);
                            if (string.IsNullOrWhiteSpace(description))
                                // nothing more in the description to match
                                return batList.Distinct().ToList();
                        }
                    }
            }

            return batList;
        }

        /// <summary>
        ///     The match function replaced all occurrences of a tag with the corresponding bat
        ///     common name.  That modded decription is returned if it exists and the local copy of the modded
        ///     description is removed to prevent multiple calls or calls to obsolete descriptions.
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        internal string Substitute(string description)
        {
            if (!string.IsNullOrWhiteSpace(_moddedDescription))
            {
                var md = _moddedDescription;
                _moddedDescription = "";
                return md;
            }

            return description;
        }

        /// <summary>
        ///     Gets all tags which are contained within the String description, regardless of case
        ///     except where the tag is all uppercase in which case the description matching must
        ///     also be upper case.
        /// </summary>
        /// <param name="description">
        ///     The description.
        /// </param>
        /// <returns>
        /// </returns>
        private List<BatTag> GetAllTags(string description)
        {
            var tags = (from tg in _tagList
                where tg.BatTag1.ToUpper() == tg.BatTag1
                    ? description.Contains(tg.BatTag1)
                    : description.ToUpper().Contains(tg.BatTag1.ToUpper())
                orderby tg.BatTag1.Length descending
                select tg).Distinct();
            if (tags != null) return tags.ToList();
            return null;
        }

        /// <summary>
        ///     Matches the tag to the description, with regard to case and returns the location in
        ///     the description at which the tag occurs. If there is no match returns -1.
        /// </summary>
        /// <param name="description">
        ///     The description.
        /// </param>
        /// <param name="batTag1">
        ///     The bat tag1.
        /// </param>
        /// <returns>
        /// </returns>
        private int MatchTag(string description, string batTag1)
        {
            var position = -1;
            if (!string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(batTag1))
            {
                if (batTag1.ToUpper() != batTag1)
                {
                    // we do not have an all uppercase Tag
                    description = description.ToUpper();
                    batTag1 = batTag1.ToUpper();
                }

                if (description.Contains(batTag1)) position = description.IndexOf(batTag1);
            }

            return position;
        }

        /*      /// <summary>
              /// Gets the substring tags. Given a list of BatTags returns a list of all the tags
              /// which are substrings of tags for other bats. </summary> <param
              /// name="containedTags">The contained tags.</param> <returns> a List of BatTags or null</returns>
              private List<TagPair> GetSubstringTags(List<BatTag> containedTags)
              {
                  List<TagPair> result = new List<TagPair>();
                  var tags = (from ct in containedTags
                              from ct1 in containedTags
                              where ((ct1.BatTag1.ToUpper() == ct1.BatTag1) ?
                                    ct.BatTag1.Contains(ct1.BatTag1) :
                                    ct.BatTag1.ToUpper().Contains(ct1.BatTag1.ToUpper()))
                                    &&
                                    (ct.BatID!=ct1.BatID)
                              select new TagPair(ct1, ct));
                  if (tags != null && tags.Count() > 0)
                  {
                      return (tags.ToList());
                  }
                  return (null);
              }*/
    }

    /*
    public class TagPair
    {
        public BatTag shortTag { get; set; }
        public BatTag longTag { get; set; }

        public TagPair(BatTag shortTag, BatTag longTag)
        {
            this.shortTag = shortTag;
            this.longTag = longTag;
        }
    }*/
}