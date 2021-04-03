/*##################################################
 * This file contains miscellaneous converter classes, which used to be scattered about or in the Tools.cs file.
 * Having them here is tidier, but not all may have been transferred.
 *
 * The Converters are then tagged for use as static resources in the file
 *
 *      BatStyleDictionary.xaml
 *
 * */

using System;
using System.Windows.Data;

namespace BatRecordingManager
{
    #region CallDisplayEnabledConverter (ValueConverter)

    public class CallDisplayEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                if (value != null && value is LabelledSegment)
                {
                    var ls = value as LabelledSegment;
                    if (ls != null && !ls.SegmentCalls.IsNullOrEmpty())
                    {
                        return (true);
                    }
                }
                return (false);
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion CallDisplayEnabledConverter (ValueConverter)
}