using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for FollowUpWindow.xaml
    /// </summary>
    public partial class FollowUpWindow : Window
    {
        public string GetFollowUpWeeks { get; private set; }

        private string _followUpWeeks;
        public FollowUpWindow(string FollowUpWeeks)
        {
            InitializeComponent();
            _followUpWeeks = FollowUpWeeks;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFollowUpWeeks.Text = _followUpWeeks;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {


            GetFollowUpWeeks = txtFollowUpWeeks.Text;
            int.TryParse(GetFollowUpWeeks, out int tryres);
            if (tryres > 0 && tryres <= 52)
            {
           
                Close();
            }
            else
            {
                MessageBox.Show("Number is in the wrong format try again. Enter number between 1-52.");
            }


        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            GetFollowUpWeeks = "0";
            Close();
        }
    }
}
