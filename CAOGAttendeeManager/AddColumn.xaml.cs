using System.Windows;

namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for AddColumn.xaml
    /// </summary>
    public partial class AddColumn : Window
    {
        public AddColumn()
        {
            InitializeComponent();

            btnAdd.IsEnabled = false;
            btnRemove.IsEnabled = false;
        }

        private void BtnAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           
        }

       
        private void BtnRemove_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void BtnOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
