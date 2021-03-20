using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BatCallAnalysisControlSet
{
    /// <summary>
    /// Interaction logic for ParameterSetter.xaml
    /// </summary>
    public partial class ParameterSetter : UserControl, INotifyPropertyChanged
    {
        public ParameterSetter()
        {
            InitializeComponent();
            DataContext = this;
            MaxPermitted = 210.0d;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public double MaxPermitted { get; set; }

        public string Units { get; set; }

        public double Value_max
        {
            get { return (_value_max); }
            set
            {
                _value_max = value;
                OnPropertyChanged(nameof(Value_max));
            }
        }

        public double Value_mean
        {
            get { return (_value_mean); }
            set
            {
                _value_mean = value;
                OnPropertyChanged(nameof(Value_mean));
            }
        }

        public double Value_min
        {
            get { return (_value_min); }
            set
            {
                _value_min = value;
                OnPropertyChanged(nameof(Value_min));
            }
        }

        public double Value_range
        {
            get { return (_value_range); }
            set
            {
                _value_range = value;
                OnPropertyChanged(nameof(Value_range));
            }
        }

        public (double min, double mean, double max) Value_Set
        {
            get
            {
                if (Value_min == 0.0d)
                {
                    if (Value_range == 0.0d)
                        Value_min = Value_mean;
                    else
                    {
                        Value_min = Value_mean - Value_range;
                        if (Value_min <= 0.0d) Value_min = 0.001d;
                    }
                }

                if (Value_max == 0.0d)
                {
                    if (Value_range == 0.0d)
                        Value_max = Value_mean;
                    else
                    {
                        Value_max = Value_mean + Value_range;
                    }
                }

                return (((double)Value_min, (double)Value_mean, (double)Value_max));
            }

            set
            {
                isChanging = true;
                Value_mean = value.mean;
                Value_min = value.min;

                Value_max = value.max;
                Value_range = (Value_mean - Value_min);
                isChanging = false;
            }
        }

        internal void SetAll((double min, double mean, double max) set)
        {
            Value_Set = set;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _value_max;
        private double _value_mean;
        private double _value_min;
        private double _value_range;
        private bool isChanging = false;

        private enum Changed { MIN, MEAN, MAX, RANGE };

        /// <summary>
        /// checks to see if the values are in the correct relationships
        /// </summary>
        /// <returns></returns>
        private bool isRational()
        {
            if (Value_max == 0.0d || Value_min == 0.0d || Value_mean == 0.0d || Value_range == 0.0d) return (false);
            if (Value_max < Value_mean) return (false);
            if (Value_min > Value_mean) return (false);
            if (Value_range > Value_mean - Value_min) return (false);
            if (Value_mean - Value_min != Value_max - Value_mean) return (false);
            if (Value_range != Value_mean - Value_min) return (false);

            return (true);
        }

        /// <summary>
        /// given a set of min,mean,max,range values, ensures that they are in the proper relationships
        /// </summary>
        private void RationalizeValues(Changed changed)
        {
            if (Value_mean == 0.0d)
            {
                if (Value_max > 0.0d && Value_min > 0.0d)
                {
                    Value_mean = (Value_max - Value_min) / 0.0d;
                    Value_range = Value_mean - Value_min;
                    return;
                }
                return; // we either have just the range, or just min or max set, so cannot compute
            }
            // from here we have a mean value and all else revolves around this
            if (Value_max < Value_mean) Value_max = Value_mean;
            if (Value_min > Value_mean || Value_min == 0.0d) Value_min = Value_mean;
            if (changed == Changed.RANGE)
            {
                Value_min = Value_mean - Value_range;
                Value_max = Value_mean + Value_range;
                return;
            }
            if (changed == Changed.MIN || changed == Changed.MAX)
            {
                Value_mean = (Value_min + Value_max) / 2.0d;
                Value_range = Value_mean - Value_min;
                return;
            }
            if (changed == Changed.MEAN)
            {
                if (Value_range > 0.0d)
                {
                    Value_min = Value_mean - Value_range;
                    Value_max = Value_mean + Value_range;
                    return;
                }
                else
                {
                    Value_range = Value_mean - Value_min;
                    Value_max = Value_mean + Value_range;
                    return;
                }
            }
        }

        private void vMax_ValueChanged(object sender, EventArgs e)
        {
            if (!isChanging && !isRational())
            {
                isChanging = true;
                RationalizeValues(Changed.MAX);
                isChanging = false;
            }
        }

        private void vMean_ValueChanged(object sender, EventArgs e)
        {
            if (!isChanging && !isRational())
            {
                isChanging = true;

                RationalizeValues(Changed.MEAN);

                isChanging = false;
            }
        }

        private void vMin_ValueChanged(object sender, EventArgs e)
        {
            if (!isChanging && !isRational())
            {
                isChanging = true;
                RationalizeValues(Changed.MIN);
                isChanging = false;
            }
        }

        private void vRange_ValueChanged(object sender, EventArgs e)
        {
            if (!isChanging && !isRational())
            {
                isChanging = true;
                RationalizeValues(Changed.RANGE);

                isChanging = false;
            }
        }
    }
}