using Invisionware.Settings;
using Invisionware.Settings.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalToolkit
{
    public class ParameterGroup
    {
        public ParameterGroup(int groupSize)
        {
            value_min = new double[groupSize];
            value_max = new double[groupSize];
            value_mean = new double[groupSize];
            batLabel = new string[groupSize];
            for (int i = 0; i < groupSize; i++) batLabel[i] = "";
        }

        public string batCommonName { get; set; }
        public string[] batLabel { get; set; }
        public string batLatinName { get; set; }
        public double[] value_max { get; set; }
        public double[] value_mean { get; set; }
        public double[] value_min { get; set; }

        public void setValue(int index, double min, double max)
        {
            if (index < value_min.Length)
            {
                value_min[index] = min;
                value_max[index] = max;
                value_mean[index] = (min + max) / 2.0d;
            }
        }
    }

    public class ReferenceCall
    {
        public enum CallTypes { fm, cf, fm_qCF, qCF, fm_rsig, fm2, fm2_2_5, qCF_fm, fm1, cf_fm, fm_cf_fm, fm1_2, fm1_1 }

        public double bandwidth_Max { get; set; }

        public double bandwidth_Mean
        {
            get
            {
                if (_bandwidth_mean <= 0.0d)
                {
                    return ((bandwidth_Max + bandwidth_Min) / 2.0d);
                }
                else return (_bandwidth_mean);
            }
        }

        public double bandwidth_Min { get; set; }

        public string BatCommonName { get; set; }
        public string BatLabel { get; set; }
        public string BatLatinName { get; set; }
        public string CallType { get; set; }
        public double duration_Max { get; set; }

        public double duration_Mean
        {
            get
            {
                if (_duration_mean <= 0.0d)
                {
                    return ((duration_Max + duration_Min) / 2.0d);
                }
                else return (_duration_mean);
            }
        }

        public double duration_Min { get; set; }
        public double fEnd_Max { get; set; }

        public double fEnd_Mean
        {
            get
            {
                if (_fEnd_mean <= 0.0d)
                {
                    return ((fEnd_Max + fEnd_Min) / 2.0d);
                }
                else return (_fEnd_mean);
            }
        }

        public double fEnd_Min { get; set; }
        public double fHeel_Max { get; set; }

        public double fHeel_Mean
        {
            get
            {
                if (_fHeel_mean <= 0.0d)
                {
                    return ((fHeel_Max + fHeel_Min) / 2.0d);
                }
                else return (_fHeel_mean);
            }
        }

        public double fHeel_Min { get; set; }
        public double fKnee_Max { get; set; }

        public double fKnee_Mean
        {
            get
            {
                if (_fKnee_mean <= 0.0d)
                {
                    return ((fKnee_Max + fKnee_Min) / 2.0d);
                }
                else return (_fKnee_mean);
            }
        }

        public double fKnee_Min { get; set; }
        public double fPeak_Max { get; set; }

        public double fPeak_Mean
        {
            get
            {
                if (_fPeak_mean <= 0.0d)
                {
                    return ((fPeak_Max + fPeak_Min) / 2.0d);
                }
                else return (_fPeak_mean);
            }
        }

        public double fPeak_Min { get; set; }
        public double fStart_Max { get; set; }

        public double fStart_Mean
        {
            get
            {
                if (_fStart_mean <= 0.0d)
                {
                    return ((fStart_Max + fStart_Min) / 2.0d);
                }
                else return (_fStart_mean);
            }
        }

        public double fStart_Min { get; set; }

        public (double min, double mean, double max) getBandwidthSet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = bandwidth_Max;
                result.mean = bandwidth_Mean;
                result.min = bandwidth_Min;
                return (result);
            }
        }

        public (double min, double mean, double max) getDurationSet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = duration_Max;
                result.mean = duration_Mean;
                result.min = duration_Min;
                return (result);
            }
        }

        public (double min, double mean, double max) getEndFrequencySet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = fEnd_Max;
                result.mean = fEnd_Mean;
                result.min = fEnd_Min;
                return (result);
            }
        }

        public (double min, double mean, double max) getHeelFrequencySet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = fHeel_Max;
                result.mean = fHeel_Mean;
                result.min = fHeel_Min;
                return (result);
            }
        }

        public (double min, double mean, double max) getIntervalSet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = interval_Max;
                result.mean = interval_Mean;
                result.min = interval_Min;
                return (result);
            }
        }

        public (double min, double mean, double max) getKneeFrequencySet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = fKnee_Max;
                result.mean = fKnee_Mean;
                result.min = fKnee_Min;
                return (result);
            }
        }

        public (double min, double mean, double max) getPeakFrequencySet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = fPeak_Max;
                result.mean = fPeak_Mean;
                result.min = fPeak_Min;
                return (result);
            }
        }

        public (double min, double mean, double max) getStartFrequencySet
        {
            get
            {
                (double min, double mean, double max) result;
                result.max = fStart_Max;
                result.mean = fStart_Mean;
                result.min = fStart_Min;
                return (result);
            }
        }

        public double interval_Max { get; set; }

        public double interval_Mean
        {
            get
            {
                if (_interval_mean <= 0.0d)
                {
                    return ((interval_Max + interval_Min) / 2.0d);
                }
                else return (_interval_mean);
            }
        }

        public double interval_Min { get; set; }

        public static ReferenceCall getFromSettings()
        {
            ReferenceCall call;
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
            if (settings != null)
            {
                call = settings.call;
            }
            else
            {
                call = new ReferenceCall();
            }
            return (call);
        }

        public void setBandwidth((double min, double mean, double max) value_Set)
        {
            bandwidth_Min = value_Set.min;

            bandwidth_Max = value_Set.max;
        }

        public void setDuration((double min, double mean, double max) value_Set)
        {
            duration_Min = value_Set.min;

            duration_Max = value_Set.max;
        }

        public void setEndFrequency((double min, double mean, double max) value_Set)
        {
            fEnd_Min = value_Set.min;

            fEnd_Max = value_Set.max;
        }

        public void setHeelFrequency((double min, double mean, double max) value_Set)
        {
            fHeel_Min = value_Set.min;

            fHeel_Max = value_Set.max;
        }

        public void setInterval((double min, double mean, double max) value_Set)
        {
            interval_Min = value_Set.min;

            interval_Max = value_Set.max;
        }

        public void setKneeFrequency((double min, double mean, double max) value_Set)
        {
            fKnee_Min = value_Set.min;

            fKnee_Max = value_Set.max;
        }

        public void setPeakFrequency((double min, double mean, double max) value_Set)
        {
            fPeak_Min = value_Set.min;

            fPeak_Max = value_Set.max;
        }

        public void setStartFrequency((double min, double mean, double max) value_Set)
        {
            fStart_Min = value_Set.min;

            fStart_Max = value_Set.max;
        }

        public void setToSettings()
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
            settings.call = this;
            SettingsMgr.WriteSettings<CustomSettings>(settings);
        }

        private double _bandwidth_mean = 0.0;
        private double _duration_mean = 0.0;
        private double _fEnd_mean = 0.0d;
        private double _fHeel_mean = 0.0d;
        private double _fKnee_mean = 0.0d;
        private double _fPeak_mean = 0.0d;
        private double _fStart_mean = 0.0d;
        private double _interval_mean = 0.0d;
    }
}