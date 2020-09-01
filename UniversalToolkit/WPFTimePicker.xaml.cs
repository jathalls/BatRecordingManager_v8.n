using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UniversalToolkit
{
    /// <summary>
    /// Interaction logic for WPFTimePicker.xaml
    /// </summary>
    public partial class WPFTimePicker : UserControl
    {
        public WPFTimePicker()
        {
            InitializeComponent();
        }
        public TimeSpan Value
        {
            get { return (TimeSpan)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(TimeSpan), typeof(WPFTimePicker),
        new UIPropertyMetadata(DateTime.Now.TimeOfDay, new PropertyChangedCallback(OnValueChanged)));

        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            WPFTimePicker control = obj as WPFTimePicker;
            control.Hours = ((TimeSpan)e.NewValue).Hours;
            control.Minutes = ((TimeSpan)e.NewValue).Minutes;
            control.Seconds = ((TimeSpan)e.NewValue).Seconds;
        }

        public int Hours
        {
            get { return (int)GetValue(HoursProperty); }
            set { SetValue(HoursProperty, value); }
        }
        public static readonly DependencyProperty HoursProperty =
        DependencyProperty.Register("Hours", typeof(int), typeof(WPFTimePicker),
        new UIPropertyMetadata(0, new PropertyChangedCallback(OnTimeChanged)));

        public int Minutes
        {
            get { return (int)GetValue(MinutesProperty); }
            set { SetValue(MinutesProperty, value); }
        }
        public static readonly DependencyProperty MinutesProperty =
        DependencyProperty.Register("Minutes", typeof(int), typeof(WPFTimePicker),
        new UIPropertyMetadata(0, new PropertyChangedCallback(OnTimeChanged)));

        public int Seconds
        {
            get { return (int)GetValue(SecondsProperty); }
            set { SetValue(SecondsProperty, value); }
        }

        public static readonly DependencyProperty SecondsProperty =
        DependencyProperty.Register("Seconds", typeof(int), typeof(WPFTimePicker),
        new UIPropertyMetadata(0, new PropertyChangedCallback(OnTimeChanged)));

        public bool IsReadOnly
        {
            get { return ((bool)GetValue(IsReadOnlyProperty)); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(WPFTimePicker),
                new UIPropertyMetadata(false));



        private static void OnTimeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            WPFTimePicker control = obj as WPFTimePicker;
            control.Value = new TimeSpan(control.Hours, control.Minutes, control.Seconds);
        }

        private void Down(object sender, KeyEventArgs args)
        {
            if (!IsReadOnly)
            {
                switch (((Grid)sender).Name)
                {
                    case "sec":
                        if (args.Key == Key.Up)
                            this.Seconds++;
                        if (args.Key == Key.Down)
                            this.Seconds--;
                        break;

                    case "min":
                        if (args.Key == Key.Up)
                            this.Minutes++;
                        if (args.Key == Key.Down)
                            this.Minutes--;
                        break;

                    case "hour":
                        if (args.Key == Key.Up)
                            this.Hours++;
                        if (args.Key == Key.Down)
                            this.Hours--;
                        break;
                }
            }
        }

    }
}
