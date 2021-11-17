using BatCallAnalysisControlSet;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for CallDataDisplayWindow.xaml
    /// </summary>
    public partial class CallDataDisplayWindow : Window
    {
        public CallDataDisplayWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// call contains:-
        /// StartFrequency
        /// StartFrequencyVariation
        /// EndFrequency
        /// EndFrequencyVariation
        /// PeakFrequency
        /// PeakFrequencyVariation
        /// PulseDuration
        /// PulseDurationvariation
        /// PulseInterval
        /// PulseIntervalVariation
        /// CallType
        /// CallFunction
        /// CallNotes
        ///
        /// </summary>
        public ObservableCollection<Call> CallData { get; set; }

        public LabelledSegment displayedSegment
        {
            get { return (_displayedSegment); }
            set
            {
                _displayedSegment = DBAccess.GetLabelledSegment(value?.Id ?? -1);
            }
        }

        public void setSegmentToDisplay(LabelledSegment segment)
        {
            displayedSegment = segment;
            SegmentDetails?.setSegment(segment);
            CallData = new ObservableCollection<Call>();
            if (!displayedSegment.SegmentCalls.IsNullOrEmpty())
            {
                var calls = displayedSegment.SegmentCalls.Select(lnk => lnk.Call);
                if (!calls.IsNullOrEmpty())
                {
                    foreach (var call in calls) CallData.Add(call);
                }
            }

            //this.Refresh();
            this.callDataGrid.ItemsSource = null;
            Binding binding = new Binding();
            binding.Path = new PropertyPath(nameof(CallData));
            _ = this.callDataGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
        }

        private LabelledSegment _displayedSegment;
        private bool formIsSaved = false;
        private Window window = null;

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            CallData.Clear();
            if (displayedSegment != null)
            {
                setSegmentToDisplay(DBAccess.DeleteCallsInSegment(displayedSegment.Id));
            }
            this.Refresh();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DelButton_Click(object sender, RoutedEventArgs e)
        {
            if (displayedSegment != null)
            {
                var selected = callDataGrid.SelectedItem as Call;
                if (selected != null)
                {
                    var segment = DBAccess.DeleteSegmentCall(displayedSegment.Id, selected.Id);

                    setSegmentToDisplay(segment);
                }
            }
            this.Refresh();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (displayedSegment != null)
            {
                var selected = callDataGrid.SelectedItem as Call;

                selected = editCall(selected);

                var segment = DBAccess.UpdateLabelledSegment(displayedSegment, selected);
                setSegmentToDisplay(segment);
                this.Refresh();
            }
        }

        private Call editCall(Call selected)
        {
            if (selected == null) selected = new Call();
            var result = selected;
            CallDataForm form = new CallDataForm();
            form.saveClicked += Form_saveClicked;
            var convertedCall = selected.ToRefCall();
            form.setSetters(convertedCall, CallDataForm.setterMode.EDIT);
            formIsSaved = false;

            window = new Window();
            window.Content = form;
            window.ShowDialog();
            if (formIsSaved)
            {
                var refCall = (window.Content as CallDataForm)?.getCallParametersfromSetters();

                result.FromRefCall(refCall);
                displayedSegment = DBAccess.UpdateLabelledSegment(displayedSegment, result);
            }
            else
            {
                result = selected;
            }
            setSegmentToDisplay(displayedSegment);
            return (result);
        }

        private void Form_saveClicked(object sender, EventArgs e)
        {
            formIsSaved = true;
            window.Close();
        }
    }
}