using System.Reflection;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for AboutScreen.xaml
    /// </summary>
    public partial class AboutScreen : Window
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AboutScreen" /> class.
        /// </summary>
        public AboutScreen()
        {
            var build = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            AssemblyVersion = "Build " + build;
            InitializeComponent();
            DataContext = this;
            Version.Content = "v 6.2 (" + build + ")";
            DbVer.Content = "    Database Version " + DBAccess.GetDatabaseVersion() + " named:- " +
                            DBAccess.GetWorkingDatabaseName(DBAccess.GetWorkingDatabaseLocation());
        }

        #region AssemblyVersion

        /// <summary>
        ///     AssemblyVersion Dependency Property
        /// </summary>
        public static readonly DependencyProperty AssemblyVersionProperty =
            DependencyProperty.Register("AssemblyVersion", typeof(string), typeof(AboutScreen),
                new FrameworkPropertyMetadata(""));

        /// <summary>
        ///     Gets or sets the AssemblyVersion property.  This dependency property
        ///     indicates ....
        /// </summary>
        public string AssemblyVersion
        {
            get => (string) GetValue(AssemblyVersionProperty);
            set => SetValue(AssemblyVersionProperty, value);
        }

        #endregion AssemblyVersion
    }
}