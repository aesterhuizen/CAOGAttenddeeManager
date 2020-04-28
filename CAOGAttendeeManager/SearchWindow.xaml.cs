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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SeachWindow : Window
    {
        public SeachWindow()
        {
            InitializeComponent();
        }

        string _search_txt = "";
        public string SearchText
        {
            get
            {
                return _search_txt;
            }
            set
            {
                _search_txt = value;
            }
        }

        private void txtSearch_TextChanged_LName(object sender, TextChangedEventArgs e)
        {
            //if (txtHeaderLastName.Text == "")
            //{
            //    //Enable_Filters();


            //    //if (m_isFilterByDateChecked || m_isActivityfilterByDateChecked)
            //    //    DateCalendar.IsEnabled = true;
            //    //else
            //    //    DateCalendar.IsEnabled = false;

            //    //if (m_AttendanceView)
            //    //{
            //    //    if (m_isQueryTableShown)
            //    //    {
            //    //        dataGrid.DataContext = m_lstQueryTableRows;
            //    //        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            //    //    }
            //    //    else
            //    //    {
            //    //        dataGrid.DataContext = m_lstdefaultTableRows;
            //    //        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            //    //    }


            //    //}
            //    //else if (m_alistView)
            //    //{
            //    //    Display_AttendeeListTable_in_Grid();

            //    //}



            //    //----------------------Textbox search has text-----------------------------------------------------------------------------------
            //}
            //else if (txtHeaderLastName.Text != "Search for Lastname")
            //{

            //    Disable_Filters();
            //    //  DateCalendar.IsEnabled = false;


            //    string text = txtHeaderLastName.Text;

            //    if (m_AttendanceView)
            //    {
            //        if (m_isQueryTableShown)
            //        {
            //            var filterQueryTable = m_lstQueryTableRows.Where(row => row.LastName.Contains(text));
            //            dataGrid.DataContext = filterQueryTable;
            //            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            //        }
            //        else
            //        {
            //            var filteredDefaultTable = m_lstdefaultTableRows.Where(row => row.LastName.Contains(text));
            //            dataGrid.DataContext = filteredDefaultTable;
            //            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            //        }


            //    }
            //    else if (m_alistView)
            //    {
            //        var filteredAttendeeListTable = m_lstattendanceTableRows.Where(row => row.LastName.Contains(text));
            //        dataGrid_prospect.DataContext = filteredAttendeeListTable;
            //        lblAttendenceMetrics.Text = dataGrid_prospect.Items.Count.ToString();
            //    }




            //}
            //else
            //{ }
        }

        private void TxtHeaderLastName_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void TxtHeaderLastName_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void TxtHeaderLastName_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void TxtHeaderLastName_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //this.WindowStartupLocation = WindowStartupLocation.Manual;
            //this.Top = 100;
            //this.Left = 200;

        }
    }
}
