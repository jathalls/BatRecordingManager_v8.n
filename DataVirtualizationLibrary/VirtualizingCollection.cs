﻿/*
 *  Copyright 2013 Paul McClean

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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataVirtualizationLibrary
{
    /// <summary>
    ///     Specialized list implementation that provides data virtualization. The collection is divided up into pages,
    ///     and pages are dynamically fetched from the IItemsProvider when required. Stale pages are removed after a
    ///     configurable period of time.
    ///     Intended for use with large collections on a network or disk resource that cannot be instantiated locally
    ///     due to memory consumption or fetch latency.
    /// </summary>
    /// <remarks>
    ///     The IList implmentation is not fully complete, but should be sufficient for use as read only collection
    ///     data bound to a suitable ItemsControl.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class VirtualizingCollection<T> : IList<T>, IList
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingCollection&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize, int pageTimeout)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;
            PageTimeout = pageTimeout;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingCollection&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingCollection&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        public VirtualizingCollection(IItemsProvider<T> itemsProvider)
        {
            ItemsProvider = itemsProvider;
        }

        public VirtualizingCollection()
        {
        }

        public string sortColumn
        {
            set
            {
                if (ItemsProvider != null) ItemsProvider.SetSortColumn(value);
            }
        }

        #endregion Constructors

        #region ItemsProvider

        /// <summary>
        ///     Gets the items provider.
        /// </summary>
        /// <value>The items provider.</value>
        public IItemsProvider<T> ItemsProvider { get; }

        #endregion ItemsProvider

        #region PageSize

        /// <summary>
        ///     Gets the size of the page.
        /// </summary>
        /// <value>The size of the page.</value>
        public int PageSize { get; } = 100;

        #endregion PageSize

        #region PageTimeout

        /// <summary>
        ///     Gets the page timeout.
        /// </summary>
        /// <value>The page timeout.</value>
        public long PageTimeout { get; } = 10000;

        #endregion PageTimeout

        #region IList<T>, IList

        #region Count

        /// <summary>
        ///     Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        ///     The first time this property is accessed, it will fetch the count from the IItemsProvider.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        public virtual int Count
        {
            get
            {
                if (_count == -1) LoadCount();
                return _count;
            }
            protected set => _count = value;
        }

        public virtual bool IsLoading
        {
            get => false;
            set { }
        }

        private int _count = -1;

        #endregion Count

        #region Indexer

        /// <summary>
        ///     Gets the item at the specified index. This property will fetch
        ///     the corresponding page from the IItemsProvider if required.
        /// </summary>
        /// <value></value>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    Debug.WriteLine("request for item at " + index + " out of " + Count);
                    return ItemsProvider.Default();
                }

                // determine which page and offset within page
                var pageIndex = index / PageSize;
                var pageOffset = index % PageSize;
                try
                {
                    // request primary page
                    RequestPage(pageIndex);

                    // if accessing upper 50% then request next page
                    if (pageOffset > PageSize / 2 && pageIndex < Count / PageSize)
                        RequestPage(pageIndex + 1);

                    // if accessing lower 50% then request prev page
                    if (pageOffset < PageSize / 2 && pageIndex > 0)
                        RequestPage(pageIndex - 1);

                    // remove stale pages
                    CleanUpPages();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("exception in VC T this[int]:- " + ex.Message);
                }

                // return requested item
                try
                {
                    if (_pages == null) return ItemsProvider.Default();
                    if (_pages.ContainsKey(pageIndex))
                    {
                        if (_pages[pageIndex] == null)
                            //Debug.WriteLine("Null data returned");
                            return ItemsProvider.Default();
                        return _pages[pageIndex][pageOffset];
                    }

                    return ItemsProvider.Default();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"###### From VirtualizingCollection this<T>[{index}]");
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.Source);
                    //Debug.WriteLine("no pages in list at " + index);
                    return ItemsProvider.Default();
                }
            }
            set => throw (new NotImplementedException());
        }

        object IList.this[int index]
        {
            get => this[index];
            set => throw (new NotImplementedException());
        }

        public void Refresh()
        {
            _pages.Clear();
            _pageTouchTimes.Clear();
            ItemsProvider.RefreshCount();

            Count = ItemsProvider.FetchCount();
            RequestPage(0);
        }

        public void Refresh(int index)
        {
            if (index >= 0 && index < this.Count)
            {
                //this[index] = ItemsProvider.Refresh(this[index]);
            }
        }

        #endregion Indexer

        #region IEnumerator<T>, IEnumerator

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <remarks>
        ///     This method should be avoided on large collections due to poor performance.
        /// </remarks>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++) yield return this[i];
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerator<T>, IEnumerator

        #region Add

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </exception>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        #endregion Add

        #region Contains

        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///     Always false.
        /// </returns>
        public bool Contains(T item)
        {
            return false;
        }

        #endregion Contains

        #region Clear

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </exception>
        public void Clear()
        {
            //throw new NotSupportedException();
        }

        #endregion Clear

        #region IndexOf

        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        /// <summary>
        ///     Not supported
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>
        ///     Always -1.
        /// </returns>
        public int IndexOf(T item)
        {
            return -1;
        }

        #endregion IndexOf

        #region Insert

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.
        /// </exception>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        #endregion Insert

        #region Remove

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///     true if <paramref name="item" /> was successfully removed from the
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if
        ///     <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </exception>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.
        /// </exception>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        #endregion Remove

        #region CopyTo

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied
        ///     from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have
        ///     zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="array" /> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="arrayIndex" /> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="array" /> is multidimensional.
        ///     -or-
        ///     <paramref name="arrayIndex" /> is equal to or greater than the length of <paramref name="array" />.
        ///     -or-
        ///     The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the
        ///     available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.
        ///     -or-
        ///     Type <paramref name="T" /> cannot be cast automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        #endregion CopyTo

        #region Misc

        /// <summary>
        ///     Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     Always false.
        /// </returns>
        public bool IsFixedSize => false;

        /// <summary>
        ///     Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     Always true.
        /// </returns>
        public bool IsReadOnly => true;

        /// <summary>
        ///     Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized
        ///     (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     Always false.
        /// </returns>
        public bool IsSynchronized => false;

        /// <summary>
        ///     Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </returns>
        public object SyncRoot => this;

        #endregion Misc

        #endregion IList<T>, IList

        #region Paging

        /// <summary>
        ///     Cleans up any stale pages that have not been accessed in the period dictated by PageTimeout.
        /// </summary>
        public void CleanUpPages()
        {
            try
            {
                // Debug.WriteLine(_pages.Count + " in memory last touched at:-");
                // foreach (var time in _pageTouchTimes)
                // {
                //     Debug.WriteLine("Created " + time.Value.ToString() + " = " + (DateTime.Now - time.Value).TotalMilliseconds + " ms");
                // }
                if (_pages.Count > 5)
                {
                    var keys = new List<int>(_pageTouchTimes.Keys);
                    foreach (var key in keys)
                        // page 0 is a special case, since WPF ItemsControl access the first item frequently
                        if (key != 0 && _pages.ContainsKey(key) &&
                            (DateTime.Now - _pageTouchTimes[key]).TotalMilliseconds > PageTimeout * 100)
                        {
                            _pages.Remove(key);
                            _pageTouchTimes.Remove(key);
                            Trace.WriteLine("Removed Page: " + key);
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("VC CleanupPages Error:- " + ex.Message + ex.StackTrace);
                Debug.WriteLine(_pages.Count + " in memory last touched at:-");
                foreach (var time in _pageTouchTimes)
                    Debug.WriteLine("Created " + time.Value + " = " + (DateTime.Now - time.Value).TotalMilliseconds +
                                    " ms");
            }
        }

        /// <summary>
        ///     Populates the page within the dictionary.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <param name="page">The page.</param>
        protected virtual void PopulatePage(int pageIndex, IList<T> page)
        {
            try
            {
                Trace.WriteLine("Page populated: " + pageIndex);
                if (_pages.ContainsKey(pageIndex))
                    _pages[pageIndex] = page;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("======= PopulatePage " + pageIndex + " :-" + ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        ///     Makes a request for the specified page, creating the necessary slots in the dictionary,
        ///     and updating the page touch time.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        protected virtual void RequestPage(int pageIndex)
        {
            try
            {
                if (!_pages.ContainsKey(pageIndex))
                {
                    _pages.Add(pageIndex, null);
                    _pageTouchTimes.Add(pageIndex, DateTime.Now);
                    Trace.WriteLine("Added page: " + pageIndex);
                    LoadPage(pageIndex);
                }
                else
                {
                    _pageTouchTimes[pageIndex] = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("+++++ RequestPage " + pageIndex + ":- " + ex.Message + ex.StackTrace);
            }
        }

        private readonly Dictionary<int, IList<T>> _pages = new Dictionary<int, IList<T>>();
        private readonly Dictionary<int, DateTime> _pageTouchTimes = new Dictionary<int, DateTime>();

        #endregion Paging

        #region Load methods

        /// <summary>
        ///     Loads the count of items.
        /// </summary>
        protected virtual void LoadCount()
        {
            Count = FetchCount();
        }

        /// <summary>
        ///     Loads the page of items.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        protected virtual void LoadPage(int pageIndex)
        {
            PopulatePage(pageIndex, FetchPage(pageIndex));
        }

        #endregion Load methods

        #region Fetch methods

        /// <summary>
        ///     Fetches the count of itmes from the IItemsProvider.
        /// </summary>
        /// <returns></returns>
        protected int FetchCount()
        {
            if (ItemsProvider != null) return ItemsProvider.FetchCount();
            return 0;
        }

        /// <summary>
        ///     Fetches the requested page from the IItemsProvider.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        protected IList<T> FetchPage(int pageIndex)
        {
            Debug.WriteLine("FetchPage() at " + pageIndex);
            if (ItemsProvider != null) return ItemsProvider.FetchRange(pageIndex * PageSize, PageSize);
            return new List<T>();
        }

        #endregion Fetch methods
    }
}