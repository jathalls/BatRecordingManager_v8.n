using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalToolkit
{
    /// <summary>
    /// Class About - describes the contents of the library
    /// </summary>
    public class About
    {
        public About() { }

        public static string Details()
        {
            return (@" Universal Toolkit
by Justin A. T. Halls
(C)2020

A general library of useful classes, functions and controls primarily for use
with Bat Recording Manager and Pulse Train Anlaysis, but also available for
more generic use.  Replaces the XCeed Extended WPF Toolkit which has gone over
to licensing which prohibits even voluntary contributions in return for
distributed copies of software.
");
        }
    }

    /// <summary>
    /// Generic static class of generically useful static functions
    /// </summary>
    public static class Tools
    {
        public static void WriteArrayToFile<T>(string fullyQualifiedFile, T[] data)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedFile) || data == null) return;

            if (File.Exists(fullyQualifiedFile))
            {
                string bakFile = Path.ChangeExtension(fullyQualifiedFile, "bak");
                if (File.Exists(bakFile)) File.Delete(bakFile);
                File.Move(fullyQualifiedFile, bakFile);
            }

            using (var sw = new StreamWriter(File.OpenWrite(fullyQualifiedFile)))
            {
                foreach (T val in data)
                {
                    sw.WriteLine(val);
                }
            }
        }

    }
}
