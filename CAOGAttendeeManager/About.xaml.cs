using System.Windows;

namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow(string versionString)
        {
            InitializeComponent();
            lblVersionStr.Content = versionString;
        }
    }
}
