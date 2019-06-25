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

using System;

namespace BatRecordingManager
{
    public class NMEA2OSG
    {
        private static readonly double deg2rad = Math.PI / 180;
        private static readonly double rad2deg = 180.0 / Math.PI;
        public double deciLat;
        public double deciLon;
        public string ngr { get; set; }


        // Processes WGS84 lat and lon in NMEA form 
        // 52°09.1461"N         002°33.3717"W
        public bool ParseNMEA(string nlat, string nlon, double height)
        {
            //grab the bit up to the °
            deciLat = Convert.ToDouble(nlat.Substring(0, nlat.IndexOf("°")));
            deciLon = Convert.ToDouble(nlon.Substring(0, nlon.IndexOf("°")));

            //remove that bit from the string now we've used it and the ° symbol
            nlat = nlat.Substring(nlat.IndexOf("°") + 1);
            nlon = nlon.Substring(nlon.IndexOf("°") + 1);

            //grab the bit up to the " - divide by 60 to convert to degrees and add it to our double value
            deciLat += Convert.ToDouble(nlat.Substring(0, nlat.IndexOf("\""))) / 60;
            deciLon += Convert.ToDouble(nlon.Substring(0, nlat.IndexOf("\""))) / 60;

            //ok remove that now and just leave the compass direction
            nlat = nlat.Substring(nlat.IndexOf("\"") + 1);
            nlon = nlon.Substring(nlon.IndexOf("\"") + 1);

            // check for negative directions
            if (nlat == "S") deciLat = 0 - deciLat;
            if (nlon == "W") deciLon = 0 - deciLon;

            //now we can parse them
            return Transform(deciLat, deciLon, height);
        }

        // Processes WGS84 lat and lon in decimal form with S and W as -ve
        public bool Transform(double WGlat, double wGlon, double height)
        {
            //first off convert to radians
            var radWGlat = WGlat * deg2rad;
            var radWGlon = wGlon * deg2rad;

            /* these calculations were derived from the work of
             * Roger Muggleton (http://www.carabus.co.uk/) */

            /* quoting Roger Muggleton :-
             * There are many ways to convert data from one system to another, the most accurate 
             * being the most complex! For this example I shall use a 7 parameter Helmert 
             * transformation.
             * The process is in three parts: 
             * (1) Convert latitude and longitude to Cartesian coordinates (these also include height 
             * data, and have three parameters, X, Y and Z). 
             * (2) Transform to the new system by applying the 7 parameters and using a little maths.
             * (3) Convert back to latitude and longitude.
             * For the example we shall transform a GRS80 location to Airy, e.g. a GPS reading to 
             * the Airy spheroid.
             * The following code converts latitude and longitude to Cartesian coordinates. The 
             * input parameters are: WGS84 latitude and longitude, axis is the GRS80/WGS84 major 
             * axis in metres, ecc is the eccentricity, and height is the height above the 
             *  ellipsoid.
             *  v = axis / (Math.sqrt (1 - ecc * (Math.pow (Math.sin(lat), 2))));
             *  x = (v + height) * Math.cos(lat) * Math.cos(lon);
             * y = (v + height) * Math.cos(lat) * Math.sin(lon);
             * z = ((1 - ecc) * v + height) * Math.sin(lat);
             * The transformation requires the 7 parameters: xp, yp and zp correct the coordinate 
             * origin, xr, yr and zr correct the orientation of the axes, and sf deals with the 
             * changing scale factors. */

            //these are the values for WGS86(GRS80) to OSGB36(Airy)
            double a = 6378137; // WGS84_AXIS
            var e = 0.00669438037928458; // WGS84_ECCENTRIC
            var h = height; // height above datum  (from GPS GGA sentence)
            var a2 = 6377563.396; //OSGB_AXIS
            var e2 = 0.0066705397616; // OSGB_ECCENTRIC 
            var xp = -446.448;
            var yp = 125.157;
            var zp = -542.06;
            var xr = -0.1502;
            var yr = -0.247;
            var zr = -0.8421;
            var s = 20.4894;

            // convert to cartesian; lat, lon are radians
            var sf = s * 0.000001;
            var v = a / Math.Sqrt(1 - e * (Math.Sin(radWGlat) * Math.Sin(radWGlat)));
            var x = (v + h) * Math.Cos(radWGlat) * Math.Cos(radWGlon);
            var y = (v + h) * Math.Cos(radWGlat) * Math.Sin(radWGlon);
            var z = ((1 - e) * v + h) * Math.Sin(radWGlat);

            // transform cartesian
            var xrot = xr / 3600 * deg2rad;
            var yrot = yr / 3600 * deg2rad;
            var zrot = zr / 3600 * deg2rad;
            var hx = x + x * sf - y * zrot + z * yrot + xp;
            var hy = x * zrot + y + y * sf - z * xrot + yp;
            var hz = -1 * x * yrot + y * xrot + z + z * sf + zp;

            // Convert back to lat, lon
            var newLon = Math.Atan(hy / hx);
            var p = Math.Sqrt(hx * hx + hy * hy);
            var newLat = Math.Atan(hz / (p * (1 - e2)));
            v = a2 / Math.Sqrt(1 - e2 * (Math.Sin(newLat) * Math.Sin(newLat)));
            var errvalue = 1.0;
            double lat0 = 0;
            while (errvalue > 0.001)
            {
                lat0 = Math.Atan((hz + e2 * v * Math.Sin(newLat)) / p);
                errvalue = Math.Abs(lat0 - newLat);
                newLat = lat0;
            }

            //convert back to degrees
            newLat = newLat * rad2deg;
            newLon = newLon * rad2deg;

            //convert lat and lon (OSGB36)  to OS 6 figure northing and easting
            return LLtoNE(newLat, newLon);
        }

        //converts lat and lon (OSGB36)  to OS 6 figure northing and easting
        private bool LLtoNE(double lat, double lon)
        {
            var phi = lat * deg2rad; // convert latitude to radians
            var lam = lon * deg2rad; // convert longitude to radians
            var a = 6377563.396; // OSGB semi-major axis
            var b = 6356256.91; // OSGB semi-minor axis
            double e0 = 400000; // easting of false origin
            double n0 = -100000; // northing of false origin
            var f0 = 0.9996012717; // OSGB scale factor on central meridian
            var e2 = 0.0066705397616; // OSGB eccentricity squared
            var lam0 = -0.034906585039886591; // OSGB false east
            var phi0 = 0.85521133347722145; // OSGB false north
            var af0 = a * f0;
            var bf0 = b * f0;

            // easting
            var slat2 = Math.Sin(phi) * Math.Sin(phi);
            var nu = af0 / Math.Sqrt(1 - e2 * slat2);
            var rho = nu * (1 - e2) / (1 - e2 * slat2);
            var eta2 = nu / rho - 1;
            var p = lam - lam0;
            var IV = nu * Math.Cos(phi);
            var clat3 = Math.Pow(Math.Cos(phi), 3);
            var tlat2 = Math.Tan(phi) * Math.Tan(phi);
            var V = nu / 6 * clat3 * (nu / rho - tlat2);
            var clat5 = Math.Pow(Math.Cos(phi), 5);
            var tlat4 = Math.Pow(Math.Tan(phi), 4);
            var VI = nu / 120 * clat5 * (5 - 18 * tlat2 + tlat4 + 14 * eta2 - 58 * tlat2 * eta2);
            var east = e0 + p * IV + Math.Pow(p, 3) * V + Math.Pow(p, 5) * VI;

            // northing
            var n = (af0 - bf0) / (af0 + bf0);
            var M = Marc(bf0, n, phi0, phi);
            var I = M + n0;
            var II = nu / 2 * Math.Sin(phi) * Math.Cos(phi);
            var III = nu / 24 * Math.Sin(phi) * Math.Pow(Math.Cos(phi), 3) *
                      (5 - Math.Pow(Math.Tan(phi), 2) + 9 * eta2);
            var IIIA = nu / 720 * Math.Sin(phi) * clat5 * (61 - 58 * tlat2 + tlat4);
            var north = I + p * p * II + Math.Pow(p, 4) * III + Math.Pow(p, 6) * IIIA;

            // make whole number values
            east = Math.Round(east); // round to whole number
            north = Math.Round(north); // round to whole number

            // Notify the calling application of the change
            NorthingEastingReceived?.Invoke(north, east);

            // convert to nat grid ref
            return NE2NGR(east, north);
        }

        // a function used in LLtoNE  - that's all I know about it
        private double Marc(double bf0, double n, double phi0, double phi)
        {
            return bf0 * ((1 + n + 5 / 4 * (n * n) + 5 / 4 * (n * n * n)) * (phi - phi0)
                          - (3 * n + 3 * (n * n) + 21 / 8 * (n * n * n)) * Math.Sin(phi - phi0) * Math.Cos(phi + phi0)
                          + (15 / 8 * (n * n) + 15 / 8 * (n * n * n)) * Math.Sin(2 * (phi - phi0)) *
                          Math.Cos(2 * (phi + phi0))
                          - 35 / 24 * (n * n * n) * Math.Sin(3 * (phi - phi0)) * Math.Cos(3 * (phi + phi0)));
        }

        //convert 12 (6e & 6n) figure numeric to letter and number grid system
        private bool NE2NGR(double east, double north)
        {
            var eX = east / 500000;
            var nX = north / 500000;
            var tmp = Math.Floor(eX) - 5.0 * Math.Floor(nX) +
                      17.0; //Math.Floor Returns the largest integer less than or equal to the specified number.
            nX = 5 * (nX - Math.Floor(nX));
            eX = 20 - 5.0 * Math.Floor(nX) + Math.Floor(5.0 * (eX - Math.Floor(eX)));
            if (eX > 7.5) eX = eX + 1;
            if (tmp > 7.5) tmp = tmp + 1;
            var eing = Convert.ToString(east);
            var ning = Convert.ToString(north);
            var lnth = eing.Length;
            eing = eing.Substring(lnth - 5);
            lnth = ning.Length;
            ning = ning.Substring(lnth - 5);
            ngr = Convert.ToString((char) (tmp + 65)) + Convert.ToString((char) (eX + 65)) + " " + eing + " " + ning;
            if (!string.IsNullOrWhiteSpace(ngr)) return true;
            return false;
            // Notify the calling application of the change
            //if (NatGridRefReceived != null) NatGridRefReceived(ngr);
        }

        #region Delegates

        public delegate void NorthingEastingReceivedEventHandler(double northing, double easting);

        public delegate void NatGridRefReceivedEventHandler(string ngr);

        #endregion

        #region Events

        public event NorthingEastingReceivedEventHandler NorthingEastingReceived;
        public event NatGridRefReceivedEventHandler NatGridRefReceived;

        #endregion
    }
}