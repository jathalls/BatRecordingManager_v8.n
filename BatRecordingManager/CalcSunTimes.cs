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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

//////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  C# Singleton class and thread-safe class for calculating Sunrise and Sunset times.
//
// The algorithm was adapted from the JavaScript sample provided here:
//      http://home.att.net/~srschmitt/script_sun_rise_set.html
//
//  NOTICE: this code is provided "as-is", without any warrenty, obligations or liability for it.
//          You may use this code freely for any use.
//
//  Zacky Pickholz (zacky.pickholz@gmail.com)
//
/////////////////////////////////////////////////////////////////////////////////////////////////////
namespace BatRecordingManager
{
    internal sealed class SunTimes
    {
        /// <summary>
        ///     Calculate sunrise and sunset times. Returns false if time zone and longitude are incompatible.
        /// </summary>
        /// <param name="lat">Latitude coordinates.</param>
        /// <param name="lon">Longitude coordinates.</param>
        /// <param name="date">Date for which to calculate.</param>
        /// <param name="riseTime">Sunrise time (output)</param>
        /// <param name="setTime">Sunset time (output)</param>
        /// <param name="isSunrise">Whether or not the sun rises at that day</param>
        /// <param name="isSunset">Whether or not the sun sets at that day</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool CalculateSunRiseSetTimes(LatitudeCoords lat, LongitudeCoords lon, DateTime date,
            ref DateTime riseTime, ref DateTime setTime,
            ref bool isSunrise, ref bool isSunset)
        {
            return CalculateSunRiseSetTimes(lat.ToDouble(), lon.ToDouble(), date, ref riseTime, ref setTime,
                ref isSunrise,
                ref isSunset);
        }

        /// <summary>
        ///     Calculate sunrise and sunset times. Returns false if time zone and longitude are incompatible.
        /// </summary>
        /// <param name="lat">Latitude in decimal notation.</param>
        /// <param name="lon">Longitude in decimal notation.</param>
        /// <param name="date">Date for which to calculate.</param>
        /// <param name="riseTime">Sunrise time (output)</param>
        /// <param name="setTime">Sunset time (output)</param>
        /// <param name="isSunrise">Whether or not the sun rises at that day</param>
        /// <param name="isSunset">Whether or not the sun sets at that day</param>
        public bool CalculateSunRiseSetTimes(double lat, double lon, DateTime date,
            ref DateTime riseTime, ref DateTime setTime,
            ref bool isSunrise, ref bool isSunset)
        {
            lock (_mLock) // lock for thread safety
            {
                double zone = -(int)Math.Round(TimeZone.CurrentTimeZone.GetUtcOffset(date).TotalSeconds / 3600);
                var jd = GetJulianDay(date) - 2451545; // Julian day relative to Jan 1.5, 2000

                if (Sign(zone) == Sign(lon) && zone != 0)
                    Debug.Print("WARNING: time zone and longitude are incompatible!");
                //return false;

                lon = lon / 360;
                var tz = zone / 24;
                var ct = jd / 36525 + 1; // centuries since 1900.0
                var t0 = LocalSiderealTimeForTimeZone(lon, jd, tz); // local sidereal time

                // get sun position at start of day
                jd += tz;
                CalculateSunPosition(jd, ct);
                var ra0 = _mSunPositionInSkyArr[0];
                var dec0 = _mSunPositionInSkyArr[1];

                // get sun position at end of day
                jd += 1;
                CalculateSunPosition(jd, ct);
                var ra1 = _mSunPositionInSkyArr[0];
                var dec1 = _mSunPositionInSkyArr[1];

                // make continuous
                if (ra1 < ra0)
                    ra1 += 2 * Math.PI;

                // initialize
                _mIsSunrise = false;
                _mIsSunset = false;

                _mRightAscentionArr[0] = ra0;
                _mDecensionArr[0] = dec0;

                // check each hour of this day
                for (var k = 0; k < 24; k++)
                {
                    _mRightAscentionArr[2] = ra0 + (k + 1) * (ra1 - ra0) / 24;
                    _mDecensionArr[2] = dec0 + (k + 1) * (dec1 - dec0) / 24;
                    _mVHzArr[2] = TestHour(k, zone, t0, lat);

                    // advance to next hour
                    _mRightAscentionArr[0] = _mRightAscentionArr[2];
                    _mDecensionArr[0] = _mDecensionArr[2];
                    _mVHzArr[0] = _mVHzArr[2];
                }

                riseTime = new DateTime(date.Year, date.Month, date.Day, _mRiseTimeArr[0], _mRiseTimeArr[1], 0);
                setTime = new DateTime(date.Year, date.Month, date.Day, _mSetTimeArr[0], _mSetTimeArr[1], 0);

                isSunset = true;
                isSunrise = true;

                // neither sunrise nor sunset
                if (!_mIsSunrise && !_mIsSunset)
                {
                    if (_mVHzArr[2] < 0)
                        isSunrise = false; // Sun down all day
                    else
                        isSunset = false; // Sun up all day
                }
                // sunrise or sunset
                else
                {
                    if (!_mIsSunrise)
                        // No sunrise this date
                        isSunrise = false;
                    else if (!_mIsSunset)
                        // No sunset this date
                        isSunset = false;
                }

                return true;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        public class LatitudeCoords : Coords
        {
            public enum Direction
            {
                NORTH,
                SOUTH
            }

            protected internal Direction MDirection = Direction.NORTH;

            public LatitudeCoords(int degrees, int minutes, int seconds, Direction direction)
            {
                MDegrees = degrees;
                MMinutes = minutes;
                MSeconds = seconds;
                MDirection = direction;
            }

            protected internal override int Sign()
            {
                return MDirection == Direction.NORTH ? 1 : -1;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        public class LongitudeCoords : Coords
        {
            public enum Direction
            {
                EAST,
                WEST
            }

            protected internal Direction MDirection = Direction.EAST;

            public LongitudeCoords(int degrees, int minutes, int seconds, Direction direction)
            {
                MDegrees = degrees;
                MMinutes = minutes;
                MSeconds = seconds;
                MDirection = direction;
            }

            protected internal override int Sign()
            {
                return MDirection == Direction.EAST ? 1 : -1;
            }
        }

        internal abstract class Coords
        {
            protected internal int MDegrees;
            protected internal int MMinutes;
            protected internal int MSeconds;

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public double ToDouble()
            {
                return Sign() * (MDegrees + (double)MMinutes / 60 + (double)MSeconds / 3600);
            }

            protected internal abstract int Sign();
        }

        #region Private Data Members

        private const double MDr = Math.PI / 180;
        private const double MK1 = 15 * MDr * 1.0027379;
        private readonly double[] _mDecensionArr = new double[3] { 0.0, 0.0, 0.0 };
        private bool _mIsSunrise;
        private bool _mIsSunset;
        private readonly object _mLock = new object();
        private readonly double[] _mRightAscentionArr = new double[3] { 0.0, 0.0, 0.0 };
        private readonly int[] _mRiseTimeArr = new int[2] { 0, 0 };

        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private double _mRizeAzimuth;

        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private double _mSetAzimuth;

        private readonly int[] _mSetTimeArr = new int[2] { 0, 0 };
        private readonly double[] _mSunPositionInSkyArr = new double[2] { 0.0, 0.0 };
        private readonly double[] _mVHzArr = new double[3] { 0.0, 0.0, 0.0 };

        #endregion Private Data Members

        #region Singleton

        public static SunTimes Instance { get; } = new SunTimes();

        private SunTimes()
        {
        }

        #endregion Singleton

        #region Private Methods

        // determine Julian day from calendar date
        // (Jean Meeus, "Astronomical Algorithms", Willmann-Bell, 1991)
        private static double GetJulianDay(DateTime date)
        {
            var month = date.Month;
            var day = date.Day;
            var year = date.Year;

            var gregorian = year >= 1583;

            if (month == 1 || month == 2)
            {
                year = year - 1;
                month = month + 12;
            }

            var a = Math.Floor((double)year / 100);
            double b = 0;

            if (gregorian)
                b = 2 - a + Math.Floor(a / 4);
            else
                b = 0.0;

            var jd = Math.Floor(365.25 * (year + 4716))
                     + Math.Floor(30.6001 * (month + 1))
                     + day + b - 1524.5;

            return jd;
        }

        // Local Sidereal Time for zone
        private static double LocalSiderealTimeForTimeZone(double lon, double jd, double z)
        {
            var s = 24110.5 + 8640184.812999999 * jd / 36525 + 86636.6 * z + 86400 * lon;
            s = s / 86400;
            s = s - Math.Floor(s);
            return s * 360 * MDr;
        }

        private static int Sign(double value)
        {
            var rv = 0;

            if (value > 0.0) rv = 1;
            else if (value < 0.0) rv = -1;
            else rv = 0;

            return rv;
        }

        // sun's position using fundamental arguments
        // (Van Flandern & Pulkkinen, 1979)
        private void CalculateSunPosition(double jd, double ct)
        {
            double g, lo, s, u, v, w;

            lo = 0.779072 + 0.00273790931 * jd;
            lo = lo - Math.Floor(lo);
            lo = lo * 2 * Math.PI;

            g = 0.993126 + 0.0027377785 * jd;
            g = g - Math.Floor(g);
            g = g * 2 * Math.PI;

            v = 0.39785 * Math.Sin(lo);
            v = v - 0.01 * Math.Sin(lo - g);
            v = v + 0.00333 * Math.Sin(lo + g);
            v = v - 0.00021 * ct * Math.Sin(lo);

            u = 1 - 0.03349 * Math.Cos(g);
            u = u - 0.00014 * Math.Cos(2 * lo);
            u = u + 0.00008 * Math.Cos(lo);

            w = -0.0001 - 0.04129 * Math.Sin(2 * lo);
            w = w + 0.03211 * Math.Sin(g);
            w = w + 0.00104 * Math.Sin(2 * lo - g);
            w = w - 0.00035 * Math.Sin(2 * lo + g);
            w = w - 0.00008 * ct * Math.Sin(g);

            // compute sun's right ascension
            s = w / Math.Sqrt(u - v * v);
            _mSunPositionInSkyArr[0] = lo + Math.Atan(s / Math.Sqrt(1 - s * s));

            // ...and declination
            s = v / Math.Sqrt(u);
            _mSunPositionInSkyArr[1] = Math.Atan(s / Math.Sqrt(1 - s * s));
        }

        // test an hour for an event
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "zone")]
        private double TestHour(int k, double zone, double t0, double lat)
        {
            var ha = new double[3];
            double a, b, c, d, e, s, z;
            double time;
            int hr, min;
            double az, dz, hz, nz;

            ha[0] = t0 - _mRightAscentionArr[0] + k * MK1;
            ha[2] = t0 - _mRightAscentionArr[2] + k * MK1 + MK1;

            ha[1] = (ha[2] + ha[0]) / 2; // hour angle at half hour
            _mDecensionArr[1] = (_mDecensionArr[2] + _mDecensionArr[0]) / 2; // declination at half hour

            s = Math.Sin(lat * MDr);
            c = Math.Cos(lat * MDr);
            z = Math.Cos(90.833 * MDr); // refraction + sun semidiameter at horizon

            if (k <= 0)
                _mVHzArr[0] = s * Math.Sin(_mDecensionArr[0]) + c * Math.Cos(_mDecensionArr[0]) * Math.Cos(ha[0]) - z;

            _mVHzArr[2] = s * Math.Sin(_mDecensionArr[2]) + c * Math.Cos(_mDecensionArr[2]) * Math.Cos(ha[2]) - z;

            if (Sign(_mVHzArr[0]) == Sign(_mVHzArr[2]))
                return _mVHzArr[2]; // no event this hour

            _mVHzArr[1] = s * Math.Sin(_mDecensionArr[1]) + c * Math.Cos(_mDecensionArr[1]) * Math.Cos(ha[1]) - z;

            a = 2 * _mVHzArr[0] - 4 * _mVHzArr[1] + 2 * _mVHzArr[2];
            b = -3 * _mVHzArr[0] + 4 * _mVHzArr[1] - _mVHzArr[2];
            d = b * b - 4 * a * _mVHzArr[0];

            if (d < 0)
                return _mVHzArr[2]; // no event this hour

            d = Math.Sqrt(d);
            e = (-b + d) / (2 * a);

            if (e > 1 || e < 0)
                e = (-b - d) / (2 * a);

            time = k + e + 1 / (double)120; // time of an event

            hr = (int)Math.Floor(time);
            min = (int)Math.Floor((time - hr) * 60);

            hz = ha[0] + e * (ha[2] - ha[0]); // azimuth of the sun at the event
            nz = -Math.Cos(_mDecensionArr[1]) * Math.Sin(hz);
            dz = c * Math.Sin(_mDecensionArr[1]) - s * Math.Cos(_mDecensionArr[1]) * Math.Cos(hz);
            az = Math.Atan2(nz, dz) / MDr;
            if (az < 0) az = az + 360;

            if (_mVHzArr[0] < 0 && _mVHzArr[2] > 0)
            {
                _mRiseTimeArr[0] = hr;
                _mRiseTimeArr[1] = min;
                _mRizeAzimuth = az;
                _mIsSunrise = true;
            }

            if (_mVHzArr[0] > 0 && _mVHzArr[2] < 0)
            {
                _mSetTimeArr[0] = hr;
                _mSetTimeArr[1] = min;
                _mSetAzimuth = az;
                _mIsSunset = true;
            }

            return _mVHzArr[2];
        }

        #endregion Private Methods
    }
}