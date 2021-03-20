using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BatCallAnalysisControlSet
{
    /// <summary>
    /// This class contains additional information for a series, including
    /// title, cursor data, and color
    /// </summary>
    internal class SeriesMetadata
    {
        public SeriesMetadata(string Title, Color SeriesColor, double Cursor_Min, double Cursor_Max, double Cursor_Mean = -1.0d)
        {
            if (Cursor_Mean < 0.0d) Cursor_Mean = (Cursor_Min + Cursor_Max) / 2.0d;
            this.Title = Title;
            cursors = new CallCursor(Cursor_Min, Cursor_Mean, Cursor_Max);

            this.SeriesColor = SeriesColor;
        }

        public CallCursor cursors { get; set; }

        public Color SeriesColor { get; set; }
        public string Title { get; set; }
    }
}