using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkSkyApi;
using DarkSkyApi.Models;

namespace BatRecordingManager
{
    class Weather
    {
        public Weather()
        {

        }


        public static async Task<string> getWeatherAsync()
        {

            //var it = GetWeatherHistoryAsync(51, -.1, DateTime.Now);
            
            string result = "";
            string key = APIKeys.DarkSkyApiKey;
            var client = new DarkSkyService(key);

            Forecast fr =
                await client.GetTimeMachineWeatherAsync(51.899066d, -0.178946d, new DateTime(2015, 8, 15, 20, 30, 00),
                    Unit.UK2);
            float temp = fr.Currently.Temperature;
            Debug.WriteLine(temp);
            return (result);
        }

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


        public async Task<string> GetWeatherHistoryAsync(double Latitude, double Longitude, DateTime when)
        {
            string result = "";
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
                    return (result);
                }

                if (client != null)
                {
                    try
                    {
                        Forecast forecast =
                            await client.GetTimeMachineWeatherAsync(Latitude, Longitude, when, Unit.UK2);
                        if (forecast != null)
                        {
                            
                            result =
                                $"Provided By DarkSky:- {forecast.Currently.Summary}, t={forecast.Currently.Temperature}C, Cloud={forecast.Currently.CloudCover}, Wind={forecast.Currently.WindSpeed} mph";
                            OnWeatherReceived(new weatherEventArgs(result));
                        }
                    }
                    catch (Exception)
                    {
                        
                        return result;
                    }

                }
            }


            return (result);
        }

        public event EventHandler<weatherEventArgs> weatherReceived;

        protected virtual void OnWeatherReceived(weatherEventArgs e)
        {
            if (weatherReceived != null)
            {
                weatherReceived(this, e);
            }
        }


    }

    public class weatherEventArgs : EventArgs
    {
        public readonly string summary;

        public weatherEventArgs(string summary)
        {
            this.summary = summary;
        }
    }

}



