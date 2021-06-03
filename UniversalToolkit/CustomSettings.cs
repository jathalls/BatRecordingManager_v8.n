using Invisionware.Settings;
using Invisionware.Settings.Sinks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalToolkit
{
    public static class Settings
    {
        public static CustomSettings getSettings()
        {
            string settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\");
            settingsPath = Path.Combine(settingsPath, "customSettings.json");
            var settingsConfig = new SettingsConfiguration().WriteTo.JsonNet(settingsPath).ReadFrom.JsonNet(settingsPath);
            var SettingsMgr = settingsConfig.CreateSettingsMgr<ISettingsObjectMgr>();
            CustomSettings settings = new CustomSettings();
            try
            {
                settings = SettingsMgr.ReadSettings<CustomSettings>();
            }
            catch (Exception)
            {
                settings = new CustomSettings();
                SettingsMgr.WriteSettings<CustomSettings>(settings);
            }
            Debug.WriteLine($"Read settings with scale={settings.Spectrogram.scale}");

            return (settings);
        }

        public static void setSettings(CustomSettings settings)
        {
            string settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\");
            settingsPath = Path.Combine(settingsPath, "customSettings.json");
            var settingsConfig = new SettingsConfiguration().WriteTo.JsonNet(settingsPath).ReadFrom.JsonNet(settingsPath);
            var SettingsMgr = settingsConfig.CreateSettingsMgr<ISettingsObjectMgr>();
            Debug.WriteLine($"Write Settings scale={settings.Spectrogram.scale}");
            SettingsMgr.WriteSettings<CustomSettings>(settings);
        }
    }

    public class CustomSettings
    {
        public ReferenceCall call { get; set; }
        public (double min, double mean, double max) fBandwidth { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fEnd { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fHeel { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fKnee { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fPeak { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) fStart { get; set; } = (0, 0, 0);
        public CustomSettingsSpectrogram Spectrogram { get; set; } = new CustomSettingsSpectrogram();
        public (double min, double mean, double max) tDuration { get; set; } = (0, 0, 0);
        public (double min, double mean, double max) tInterval { get; set; } = (0, 0, 0);
    }

    public class CustomSettingsSpectrogram
    {
        public int dBScale { get; set; } = 10;
        public int FFTAdvance { get; set; } = 512;
        public int FFTSize { get; set; } = 1024;
        public int intensity { get; set; } = 5;

        public int maxFrequency { get; set; } = 120000;
        public double scale { get; set; } = 8000.0d;
    }
}