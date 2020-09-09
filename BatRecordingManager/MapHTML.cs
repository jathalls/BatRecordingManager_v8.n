using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatRecordingManager
{
    /// <summary>
    /// Uses an HTML template file to create a new html file which will display a Bing map in the local browser
    /// (by executing an html file which this class creates) to display a surveyed route and points at which
    /// each recording was made accompanied by a list f bats encountered at that location.
    /// </summary>
    internal class MapHTML
    {
        public MapHTML()
        {
        }

        public string Create(RecordingSession session)
        {
            GpxHandler gpx = new GpxHandler(session.OriginalFilePath);

            string htmlFile = "";
            string template = ReadTemplateFile();
            if (!string.IsNullOrWhiteSpace(template))
            {
                template = AddCredentials(template);
                template = AddCentreLocation(session, template);
                template = AddHeaderText(session, template);
                if (gpx.gpxFileExists)
                {
                    var trackPointList = gpx.getAllTrackPoints(session.SessionDate, session.SessionEndTime);
                    template = AddTrack(trackPointList, template);
                }
                else
                {
                    template = template.Replace("$$$$Track", "");
                }
                template = AddPushPins(session, template);

                template = AddClustering(template);

                htmlFile = ExecuteTemplate(session.OriginalFilePath, template);
            }
            return (htmlFile);
        }

        private string AddCentreLocation(RecordingSession session, string template)
        {
            var latitude = (from rec in session.Recordings
                            where rec.HasGPS
                            select rec.LatitudeAsDouble).Average();
            var longitude = (from rec in session.Recordings
                             where rec.HasGPS
                             select rec.LongitudeAsDouble).Average();
            string parameter = $"{latitude},{longitude}";
            template = template.Replace("$$$$Centreloc", parameter);
            return (template);
        }

        /// <summary>
        /// Optionally adds a clustering layer with amalgamation of pushpin labels
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private string AddClustering(string template)
        {
            if (!string.IsNullOrWhiteSpace(template))
            {
                string clusterCode = "";
                clusterCode += @"
    var clusterLayer=new Microsoft.Maps.ClusterLayer(pushpins,{gridsize:100});";

                clusterCode += @"
    map.layers.insert(clusterLayer);";

                template = template.Replace(@"//$$$$Clusters", clusterCode);
            }

            return (template);
        }

        /// <summary>
        /// replaces $$$$Crecentials with the actual Bing license key
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private string AddCredentials(string template)
        {
            template = template.Replace("$$$$Credentials", APIKeys.BingMapsLicenseKey);
            return (template);
        }

        /// <summary>
        /// Places a descriptive header text aove the map
        /// </summary>
        /// <param name="session"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        private string AddHeaderText(RecordingSession session, string template)
        {
            string headerText = "";
            headerText += $"{session.SessionTag} - {session.Operator} {session.Location} {session.SessionDate.ToShortDateString()} " +
                $"{(session.SessionStartTime ?? new TimeSpan()).ToHMString()}-" +
                $"{(session.SessionEndTime ?? new TimeSpan()).ToHMString()}";

            template = template.Replace("$$$$Headertext", headerText);
            return (template);
        }

        private string AddPushPins(RecordingSession session, string template)
        {
            string addPinText = "var pushpins=[";
            foreach (var rec in session.Recordings)
            {
                if (rec.HasGPS)
                {
                    var batlist = rec.GetBatTags6AsArray();
                    string batStrings = "";
                    foreach (var bat in batlist)
                    {
                        batStrings += bat + ",";
                    }
                    string time = "";
                    if (rec.RecordingStartTime != null)
                    {
                        time = rec.RecordingStartTime.Value.ToHMString();
                    }
                    if (batStrings.EndsWith(",")) batStrings = batStrings.Substring(0, batStrings.Length - 1);
                    //addPinText += $"AddData({rec.RecordingGPSLatitude}, {rec.RecordingGPSLongitude}, \'{batStrings}\', \'{time}\');\n";
                    addPinText += $"\nnew Microsoft.Maps.Pushpin(new Microsoft.Maps.Location({rec.RecordingGPSLatitude},{rec.RecordingGPSLongitude}),";
                    addPinText += $"\n{{title:\'{batStrings}\', subTitle: \'{time}\',";
                    addPinText += "\nicon: '<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"8\" height=\"8\"><circle cx=\"4\" cy=\"4\" r=\"4\" stroke=\"white\" stroke-width=\"1\" fill=\"green\" /></svg>',";
                    addPinText += "anchor: new Microsoft.Maps.Point(4,4), textOffset: {x: 5,y: 0}}),";
                }
            }
            if (addPinText.EndsWith(","))
            {
                addPinText = addPinText.Substring(0, addPinText.Length - 1);
            }
            addPinText += "];\nmap.entities.push(pushpins);\n";
            template = template.Replace("$$$$Adddata", addPinText);
            return (template);
        }

        /// <summary>
        /// Uses a set of trackpoints to draw a polyline track on the map
        /// </summary>
        /// <param name="trackPointList"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        private string AddTrack(List<(decimal, decimal)> trackPointList, string template)
        {
            string code = "";

            if (trackPointList != null && trackPointList.Any())
            {
                code = "var coords=[";
                foreach (var trkpt in trackPointList)
                {
                    code += $"new Microsoft.Maps.Location({trkpt.Item1},{trkpt.Item2}),\n";
                }
                if (code.EndsWith(","))
                {
                    code = code.Substring(0, code.Length - 1);
                }
                code += "];\n";
                template = template.Replace("$$$$Trackarray", code);
                code = "";
                code += @"var line=new Microsoft.Maps.Polyline(coords,{strokeColor: 'red', strokeThickness: 3});";
                code += "\nmap.entities.push(line);\n";
            }
            else
            {
                template = template.Replace("$$$$Trackarray", "");
            }

            template = template.Replace("$$$$Track", code);
            return (template);
        }

        private string ExecuteTemplate(string folderPath, string template)
        {
            string fqFilename = "";
            if (Directory.Exists(folderPath))
            {
                fqFilename = Path.Combine(folderPath, "bingMap.html");
                if (File.Exists(fqFilename))
                {
                    File.Delete(fqFilename);
                }
                File.WriteAllText(fqFilename, template);

                var proc = new Process();
                proc.StartInfo.FileName = fqFilename;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                proc.Start();
            }
            return (fqFilename);
        }

        /// <summary>
        /// Reads the bare template file from the local directory
        /// </summary>
        /// <returns></returns>
        private string ReadTemplateFile()
        {
            if (File.Exists(@".\bing.html"))
            {
                string result = File.ReadAllText(@".\bing.html");
                return (result);
            }
            return ("");
        }
    }
}