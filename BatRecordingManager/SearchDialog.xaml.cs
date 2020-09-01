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
using System.Text.RegularExpressions;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for SearchDialog.xaml
    ///     This dialog permits the user to specify and implement searches on a
    ///     collection of strings.  The user may select the type of search,
    ///     with or withou case matching and may request to use a regular expression
    ///     which will be directly used as a pattern inn Regex.
    ///     Search results and moves are reported back through an event handler.
    /// </summary>
    public partial class SearchDialog : Window
    {
        //-------------------------------------------------------------------------------------------------------
        private readonly object _searchedEventLock = new object();

        private int _currentIndex = -1;

        private EventHandler _searchedEvent;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public SearchDialog()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public BulkObservableCollection<string> targetStrings
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get;
            set;
        }

        /// <summary>
        ///     Triggered by clicking the Find button, saves the search string and
        ///     performs an initial search of the collection of target strings.  When a
        ///     match is found triggers the event handler to report it.
        ///     If a match is found, enables the Next button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindNextButton.IsEnabled = false;
            FindPrevButton.IsEnabled = false;
            if (targetStrings == null || targetStrings.Count <= 0 ||
                string.IsNullOrWhiteSpace(SimpleSearchTextBox.Text)) return;

            _currentIndex = 0;
            FindNextButton_Click(sender, e);
        }

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        ///     Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler e_Searched
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            add
            {
                lock (_searchedEventLock)
                {
                    _searchedEvent += value;
                }
            }
            remove
            {
                lock (_searchedEventLock)
                {
                    _searchedEvent -= value;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="e_Searched" /> event.
        /// </summary>
        /// <param name="e"><see cref="SearchedEventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnSearched(SearchedEventArgs e)
        {
            EventHandler handler = null;

            lock (_searchedEventLock)
            {
                handler = _searchedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        /// <summary>
        ///     Either triggered by clicking the Next button, or is called after
        ///     initialization by the Find button.  Advances forwards through the list looking
        ///     for a match to the defined search pattern and if one is found, makes that the
        ///     current position and triggers a 'searched' event.  If no match is found
        ///     then triggers a 'searched' event with a null 'foundItem' and a -1 index..
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (targetStrings == null || targetStrings.Count <= 0 || string.IsNullOrWhiteSpace(SimpleSearchTextBox.Text)
            ) return; // do nothing if there are no strings to search, or no pattern
            var result = RegexCheckBox.IsChecked ?? false ? RegexSearch() : SimpleSearch();
            FindNextButton.IsEnabled = _currentIndex < targetStrings.Count - 1;
            FindPrevButton.IsEnabled = _currentIndex > 0;
        }

        /// <summary>
        ///     Searches through the collection of target strings performing a Regex on each using the
        ///     pattern supplied in the search text box.  Triggers a searched event with the result.
        /// </summary>
        /// <returns></returns>
        private bool RegexSearch()
        {
            _currentIndex++; // so we don't re-search the last found string - was -1 if no preior search
            // caseCheckBox is irrelevant in a Regex
            var regex = new Regex(SimpleSearchTextBox.Text);
            while (targetStrings != null && _currentIndex < targetStrings.Count)
            {
                if (!string.IsNullOrWhiteSpace(targetStrings[_currentIndex]))
                {
                    var match = regex.Match(targetStrings[_currentIndex]);
                    if (match.Success)
                    {
                        MatchFound(SimpleSearchTextBox.Text, targetStrings[_currentIndex], _currentIndex);
                        return true;
                    }
                }

                _currentIndex++;
            }

            MatchFound(SimpleSearchTextBox.Text, null, -1);
            return false;
        }

        /// <summary>
        ///     Performs a simple search through the collection of strings untils match is found,
        ///     then triggers a searched event
        /// </summary>
        private bool SimpleSearch()
        {
            _currentIndex++;
            var searchFor = SimpleSearchTextBox.Text.Trim();
            if (!(CaseCheckBox.IsChecked ?? false)) searchFor = searchFor.ToUpper();
            if (_currentIndex == targetStrings.Count) _currentIndex = 0;
            while (targetStrings != null && _currentIndex < targetStrings.Count)
            {
                if (!string.IsNullOrWhiteSpace(targetStrings[_currentIndex]))
                {
                    if (!(CaseCheckBox.IsChecked ?? false))
                    {
                        if (targetStrings[_currentIndex].ToUpper().Contains(searchFor))
                        {
                            MatchFound(searchFor, targetStrings[_currentIndex], _currentIndex);
                            return true;
                        }
                    }
                    else
                    {
                        if (targetStrings[_currentIndex].Contains(searchFor))
                        {
                            MatchFound(searchFor, targetStrings[_currentIndex], _currentIndex);
                            return true;
                        }
                    }
                }

                _currentIndex++;
            }

            MatchFound(searchFor, null, -1);
            return false;
        }

        /// <summary>
        ///     when a search match is found triggers the searched event handler with the supplied
        ///     result string and index
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <param name="v"></param>
        private void MatchFound(string pattern, string result, int index)
        {
            var seArgs = new SearchedEventArgs(index, pattern, result);
            OnSearched(seArgs);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
        }
    }

    /// <summary>
    ///     Provides arguments containing the results of the latest search, next or prev
    ///     request
    /// </summary>
    [Serializable]
    public class SearchedEventArgs : EventArgs
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public new static readonly SearchedEventArgs Empty = new SearchedEventArgs(-1, "", "");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member


        #region Constructors

        /// <summary>
        ///     Constructs a new instance of the <see cref="SearchedEventArgs" /> class.
        /// </summary>
        public SearchedEventArgs(int index, string pattern, string result)
        {
            IndexOfFoundItem = index;
            SearchPattern = pattern;
            FoundItem = result;
        }

        #endregion Constructors

        #region Public Properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string FoundItem;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string SearchPattern;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public int IndexOfFoundItem;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion Public Properties
    }

    /// <summary>
    /// general purpose EventArgs which can pass back a boolean flag
    /// </summary>
    public class BoolEventArgs : EventArgs
    {
        /// <summary>
        /// boolean flag, defaults to false
        /// </summary>
        public bool state { get; set; } = false;

        /// <summary>
        /// Constructor for an event args which carries a boolean flag.
        /// The flag defaults to false and is false if no parameter is supplied.
        /// </summary>
        /// <param name="state"></param>
        public BoolEventArgs(bool state = false)
        {
            this.state = state;
        }
    }
}