﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace UniversalToolkit
{
    /// <summary>
    /// Interaction logic for NumericSpinner.xaml
    /// </summary>
    public partial class NumericSpinner : UserControl
    {
        #region Fields

        public event EventHandler PropertyChanged;
        public event EventHandler ValueChanged;
        #endregion

        public NumericSpinner()
        {
            InitializeComponent();

            _=tb_main.SetBinding(TextBox.TextProperty, new Binding("Value")
            {
                ElementName = "root_numeric_spinner",
                Mode = BindingMode.TwoWay,
                StringFormat = "##0.0",
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
            DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(NumericSpinner)).AddValueChanged(this, ValueChanged);
            DependencyPropertyDescriptor.FromProperty(DecimalsProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
            DependencyPropertyDescriptor.FromProperty(MinValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
            DependencyPropertyDescriptor.FromProperty(MaxValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);

            PropertyChanged += (x, y) => validate();
        }

        #region ValueProperty

        public readonly static DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(new decimal(0)));

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set
            {
                decimal newVal = decimal.Round(value, Decimals);
                if (newVal != Value)
                {
                    if (newVal < MinValue)
                        newVal = MinValue;
                    if (newVal > MaxValue)
                        newVal = MaxValue;

                    SetValue(ValueProperty, newVal);
                    try
                    {
                        ValueChanged(this, new EventArgs());
                    }
                    catch (Exception) { }
                }
            }
        }


        #endregion

        #region StepProperty

        public readonly static DependencyProperty StepProperty = DependencyProperty.Register(
            "Step",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(new decimal(1.0)));

        public decimal Step
        {
            get { return (decimal)GetValue(StepProperty); }
            set
            {
                SetValue(StepProperty, value);
            }
        }

        #endregion

        #region DecimalsProperty

        public readonly static DependencyProperty DecimalsProperty = DependencyProperty.Register(
            "Decimals",
            typeof(int),
            typeof(NumericSpinner),
            new PropertyMetadata(2));

        public int Decimals
        {
            get { return (int)GetValue(DecimalsProperty); }
            set
            {
                SetValue(DecimalsProperty, value);
            }
        }

        #endregion

        #region MinValueProperty

        public readonly static DependencyProperty MinValueProperty = DependencyProperty.Register(
            "MinValue",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(decimal.MinValue));

        public decimal MinValue
        {
            get { return (decimal)GetValue(MinValueProperty); }
            set
            {
                if (value > MaxValue)
                    MaxValue = value;
                SetValue(MinValueProperty, value);
            }
        }

        #endregion

        #region MaxValueProperty

        public readonly static DependencyProperty MaxValueProperty = DependencyProperty.Register(
            "MaxValue",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(decimal.MaxValue));

        public decimal MaxValue
        {
            get { return (decimal)GetValue(MaxValueProperty); }
            set
            {
                if (value < MinValue)
                    value = MinValue;
                SetValue(MaxValueProperty, value);
            }
        }

        #endregion

        #region CaptionProperty

        public readonly static DependencyProperty CaptionProperty = DependencyProperty.Register(
            "Caption",
            typeof(string),
            typeof(NumericSpinner),
            new PropertyMetadata(""));

        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set
            {

                SetValue(CaptionProperty, value);
            }
        }

        #endregion

        /// <summary>
        /// Revalidate the object, whenever a value is changed...
        /// </summary>
        private void validate()
        {
            // Logically, This is not needed at all... as it's handled within other properties...
            if (MinValue > MaxValue) MinValue = MaxValue;
            if (MaxValue < MinValue) MaxValue = MinValue;
            if (Value < MinValue) Value = MinValue;
            if (Value > MaxValue) Value = MaxValue;

            
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            decimal factor = 1.0m;
            if (Keyboard.IsKeyDown(Key.LeftCtrl)) factor /= 10.0m;
            if(Keyboard.IsKeyDown(Key.LeftShift)) factor *= 10.0m;
            Value += Step*factor;
            if(Value>MaxValue) Value = MaxValue;
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("cmdDownClick");
            decimal factor = 1.0m;
            if (Keyboard.IsKeyDown(Key.LeftCtrl)) factor /= 10.0m;
            if (Keyboard.IsKeyDown(Key.LeftShift)) factor *= 10.0m;
            Value -= Step * factor;
            if (Value <MinValue) Value = MinValue;
        }

        private void tb_main_Loaded(object sender, RoutedEventArgs e)
        {
            ValueChanged(this, new EventArgs());
        }

        private void Path_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("LM Down Preview");
        }

        //private bool textChanged = false;

        private void tb_main_TextChanged(object sender, TextChangedEventArgs e)
        {
            //textChanged = true;
        }

        private void tb_main_LostFocus(object sender, RoutedEventArgs e)
        {/*
            if (textChanged)
            {
                if(decimal.TryParse(tb_main.Text, out decimal value))
                {
                    Value = value;
                }
                
                ValueChanged(this, EventArgs.Empty);
            }
            textChanged = false;*/
        }
    }
}