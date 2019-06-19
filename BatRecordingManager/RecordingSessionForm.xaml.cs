using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingSessionForm.xaml
    /// </summary>
    public partial class RecordingSessionForm : Window
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingSessionForm" /> class.
        /// </summary>
        public RecordingSessionForm()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Gets the recording session.
        /// </summary>
        /// <returns>
        /// </returns>
        public RecordingSession GetRecordingSession()
        {
            return RecordingSessionControl.recordingSession;
        }

        /// <summary>
        ///     Sets the recording session.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        public void SetRecordingSession(RecordingSession session)
        {
            RecordingSessionControl.recordingSession = session;
        }

        /// <summary>
        ///     Clears this instance.
        /// </summary>
        internal void Clear()
        {
            RecordingSessionControl.recordingSession = new RecordingSession();
            RecordingSessionControl.recordingSession.LocationGPSLongitude = null;
            RecordingSessionControl.recordingSession.LocationGPSLatitude = null;
        }

        private void AutoButton_Click(object sender, RoutedEventArgs e)
        {
            var err = RecordingSessionControl.VerifyFormContents();
            if (string.IsNullOrWhiteSpace(err))
            {
                DBAccess.UpdateRecordingSession(RecordingSessionControl.recordingSession);
            }
            else
            {
                MessageBox.Show(err, "Recording Session Validation failed");
                return;
            }

            var recordingForm = new RecordingForm();
            recordingForm.AutoFill(RecordingSessionControl.recordingSession);
            if (recordingForm.ShowDialog() ?? false)
            {
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        ///     Handles the Click event of the CancelButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        ///     Handles the Click event of the OKButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var err = RecordingSessionControl.VerifyFormContents();
            if (string.IsNullOrWhiteSpace(err))
            {
                DBAccess.UpdateRecordingSession(RecordingSessionControl.recordingSession);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(err, "Recording Session Validation failed");
            }
        }
    }
}