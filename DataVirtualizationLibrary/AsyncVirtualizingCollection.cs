/*
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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace DataVirtualizationLibrary
{
    /// <summary>
    ///     Derived VirtualizatingCollection, performing loading asychronously.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public class AsyncVirtualizingCollection<T> : VirtualizingCollection<T>, INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="AsyncVirtualizingCollection&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider)
            : base(itemsProvider)
        {
            SynchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AsyncVirtualizingCollection&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
            : base(itemsProvider, pageSize)
        {
            SynchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AsyncVirtualizingCollection&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize, int pageTimeout)
            : base(itemsProvider, pageSize, pageTimeout)
        {
            SynchronizationContext = SynchronizationContext.Current;
        }

        #endregion

        #region SynchronizationContext

        /// <summary>
        ///     Gets the synchronization context used for UI-related operations. This is obtained as
        ///     the current SynchronizationContext when the AsyncVirtualizingCollection is created.
        /// </summary>
        /// <value>The synchronization context.</value>
        protected SynchronizationContext SynchronizationContext { get; }

        #endregion

        #region INotifyCollectionChanged

        /// <summary>
        ///     Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     Raises the <see cref="E:CollectionChanged" /> event.
        /// </summary>
        /// <param name="e">
        ///     The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing
        ///     the event data.
        /// </param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var h = CollectionChanged;
            if (h != null)
                h(this, e);
        }

        /// <summary>
        ///     Fires the collection reset event.
        /// </summary>
        private void FireCollectionReset()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(e);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        ///     Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Raises the <see cref="E:PropertyChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var h = PropertyChanged;
            if (h != null)
                h(this, e);
        }

        /// <summary>
        ///     Fires the property changed event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void FirePropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
        }

        #endregion

        #region IsLoading

        private bool _isLoading;

        /// <summary>
        ///     Gets or sets a value indicating whether the collection is loading.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this collection is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value != _isLoading) _isLoading = value;
                FirePropertyChanged(nameof(IsLoading));
            }
        }

        #endregion

        #region Load overrides

        /// <summary>
        ///     Asynchronously loads the count of items.
        /// </summary>
        protected override void LoadCount()
        {
            Count = 0;
            IsLoading = true;
            //Debug.WriteLine("LoadCount()");
            ThreadPool.QueueUserWorkItem(LoadCountWork);
        }

        /// <summary>
        ///     Performed on background thread.
        /// </summary>
        /// <param name="args">None required.</param>
        private void LoadCountWork(object args)
        {
            var count = FetchCount();
            //Debug.WriteLine("LoadCountWork()=" + count);
            SynchronizationContext.Send(LoadCountCompleted, count);
        }

        /// <summary>
        ///     Performed on UI-thread after LoadCountWork.
        /// </summary>
        /// <param name="args">Number of items returned.</param>
        private void LoadCountCompleted(object args)
        {
            Count = (int) args;
            IsLoading = false;
            //Debug.WriteLine("LoadCountCompleted()=" + Count);

            FireCollectionReset();
        }

        /// <summary>
        ///     Asynchronously loads the page.
        /// </summary>
        /// <param name="index">The index.</param>
        protected override void LoadPage(int index)
        {
            IsLoading = true;
            //Debug.WriteLine("LoadPage() page="+index);
            ThreadPool.QueueUserWorkItem(LoadPageWork, index);
        }

        /// <summary>
        ///     Performed on background thread.
        /// </summary>
        /// <param name="args">Index of the page to load.</param>
        private void LoadPageWork(object args)
        {
            var pageIndex = (int) args;
            //Debug.WriteLine("LoadPageWork() at " + pageIndex);
            var page = FetchPage(pageIndex);
            //Debug.WriteLine("LoadPageWork() at " + pageIndex);
            SynchronizationContext.Send(LoadPageCompleted, new object[] {pageIndex, page});
        }

        /// <summary>
        ///     Performed on UI-thread after LoadPageWork.
        /// </summary>
        /// <param name="args">object[] { int pageIndex, IList(T) page }</param>
        private void LoadPageCompleted(object args)
        {
            var pageIndex = (int) ((object[])args)[0];
            var page = (IList<T>) ((object[])args)[1];
            //Debug.WriteLine("LoadPageCompleted() at " + pageIndex);
            PopulatePage(pageIndex, page);
            IsLoading = false;
            //Debug.WriteLine("PagePopulated...");
            FireCollectionReset();
        }

        #endregion
    }
}