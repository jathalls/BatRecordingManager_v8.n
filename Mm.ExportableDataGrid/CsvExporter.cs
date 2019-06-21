/*
 *  Copyright 2015 Magnus Montin

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
using System.IO;
using System.Text;

namespace Mm.ExportableDataGrid
{
    public class CsvExporter : IExporter
    {
        private readonly string _delimiter;
        private readonly StringBuilder sb = new StringBuilder();

        public CsvExporter(char delimiter)
        {
            _delimiter = delimiter.ToString();
        }

        public char Delimiter => _delimiter[0];

        public void AddColumn(string value)
        {
            if (value.Contains(_delimiter)) value = "\"" + value + "\"";
            sb.Append(value);
            //sb.Append(value.Replace(_delimiter,
            //  string.Format("\"{0}\"", _delimiter)));
            sb.Append(_delimiter);
        }

        public void AddLineBreak()
        {
            sb.Remove(sb.Length - 1, 1); //remove trailing delimiter
            sb.AppendLine();
        }

        public string Export(string exportPath)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                var rnd = new Random();
                exportPath = string.Format("{0}.csv", rnd.Next());
            }
            else if (!Path.GetExtension(exportPath).ToLower().Equals(".csv"))
            {
                throw new ArgumentException("Invalid file extension.", "exportPath");
            }

            File.WriteAllText(exportPath, sb.ToString().Trim());
            sb.Clear();
            return exportPath;
        }
    }
}