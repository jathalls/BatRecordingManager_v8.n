using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Class to contain reference details for a particular bat species
    /// </summary>
    public class Bats
    {
        private readonly XElement batRef;

        /// <summary>
        /// default constructor for the Bat class
        /// </summary>
        public Bats()
        {
            try
            {
                batRef = XElement.Load(@"..\..\..\BatrecordingManager\bin\Debug\BatReferenceXMLFile.xml");
            }
            catch (Exception)
            {
                Debug.WriteLine($"Unable to find xml from {Path.GetDirectoryName(@".\")}");
            }
        }

        /// <summary>
        /// Returns a list of all the call definitions from the bat reference XML file loaded during
        /// class construction.
        /// </summary>
        /// <returns>List of BatCall with call and label for each defined bat call as a List</returns>
        public List<BatCall> getAllCalls()
        {
            var callList = from bats in batRef.Elements("Bat")
                           from call in bats.Elements("Call")
                           from label in bats.Elements("Label")
                           select new BatCall(call, label);
            return (callList.ToList());
        }
    }
}
