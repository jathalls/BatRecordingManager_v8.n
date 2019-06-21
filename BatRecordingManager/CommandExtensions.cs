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

using System.Windows;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    /// </summary>
    public class CommandExtensions : DependencyObject
    {
        /// <summary>
        ///     The command property
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(CommandExtensions),
                new UIPropertyMetadata(null));

        /// <summary>
        ///     Gets the command.
        /// </summary>
        /// <param name="obj">
        ///     The object.
        /// </param>
        /// <returns>
        /// </returns>
        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand) obj.GetValue(CommandProperty);
        }

        /// <summary>
        ///     Sets the command.
        /// </summary>
        /// <param name="obj">
        ///     The object.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }
    }
}