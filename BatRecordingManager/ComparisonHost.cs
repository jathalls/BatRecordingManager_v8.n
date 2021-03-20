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
using System.Diagnostics;

namespace BatRecordingManager
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public sealed class ComparisonHost
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        static ComparisonHost()
        {
        }

        public static ComparisonHost Instance
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return ComparisonHostInstance; }
        }

        public void AddImage(StoredImage image)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (image != null)
            {
                Debug.WriteLine("Added Image <" + image.caption + ">...<" + image.description + ">");
                if (_comparisonWindow == null)
                {
                    _comparisonWindow = new ComparisonWindow();

                    _comparisonWindow.Show();
                }

                _comparisonWindow.AddImage(image);
            }
        }

        public void AddImageRange(BulkObservableCollection<StoredImage> images)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (!images.IsNullOrEmpty())
            {
                Debug.WriteLine("Added Image <" + images[0].caption + ">...<" + images[0].description + ">");
                if (_comparisonWindow == null)
                {
                    _comparisonWindow = new ComparisonWindow();
                    _comparisonWindow.Show();
                }

                _comparisonWindow.AddImageRange(images);
            }
        }

        /// <summary>
        ///     Returns the currently selected image from the list of images in the comparison window,
        ///     or the first image in the list if none are specifically selected.  If there is no current instance
        ///     of the comparison window, or if the list of images is empty, then returns null.
        /// </summary>
        /// <returns></returns>
        public StoredImage GetSelectedImage()
        {
            StoredImage result = null;
            if (_comparisonWindow != null) result = _comparisonWindow.GetSelectedImage();

            return result;
        }

        internal void Close()
        {
            if (_comparisonWindow != null)
            {
                _comparisonWindow.Close();
                _comparisonWindow = null;
            }
        }

        private static readonly ComparisonHost ComparisonHostInstance = new ComparisonHost();

        private ComparisonWindow _comparisonWindow;

        private ComparisonHost()
        {
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    }
}