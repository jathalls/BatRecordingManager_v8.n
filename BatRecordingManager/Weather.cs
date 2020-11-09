using DarkSkyApi;
using DarkSkyApi.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BatRecordingManager
{
    public class weatherEventArgs : EventArgs
    {
        public readonly string summary;

        public weatherEventArgs(string summary)
        {
            this.summary = summary;
        }
    }

    internal class Weather
    {
        public Weather()
        {
        }

        public event EventHandler<weatherEventArgs> weatherReceived;

        public string GetWeatherHistory(double lat, double longit, DateTime when)
        {
            try
            {
                GetWeatherHistoryAsync(lat, longit, when);
            }
            catch (Exception ex)
            {
                return (ex.Message);
            }

            return ("");
        }

        public async void GetWeatherHistoryAsync(double Latitude, double Longitude, DateTime when)
        {
            string key = APIKeys.DarkSkyApiKey;
            if (!string.IsNullOrWhiteSpace(key))
            {
                DarkSkyService client;
                try
                {
                    client = new DarkSkyService(key);
                }
                catch (Exception)
                {
                    return;
                }

                if (client != null)
                {
                    try
                    {
                        Forecast forecast =
                            await client.GetTimeMachineWeatherAsync(Latitude, Longitude, when, Unit.UK2);
                        if (forecast != null)
                        {
                            var result =
                                $"Provided By DarkSky:- {forecast.Currently.Summary}, t={forecast.Currently.Temperature}C, Cloud={forecast.Currently.CloudCover}, Wind={forecast.Currently.WindSpeed} mph at {when.TimeOfDay.ToString()} on {when.Date.ToShortDateString()}";
                            OnWeatherReceived(new weatherEventArgs(result));
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }

            return;
        }

        protected virtual void OnWeatherReceived(weatherEventArgs e)
        {
            if (weatherReceived != null)
            {
                weatherReceived(this, e);
            }
        }
    }
}