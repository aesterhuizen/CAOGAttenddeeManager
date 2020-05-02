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
    /// Interaction logic for DeleteRecord.xaml
    /// </summary>
    public partial class DeleteRecordWindow : Window
    {
        public DateTime getDateToDelete {get; private set;}

        public DeleteRecordWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnDeleteSelectedRecords_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnDeleteDateRecords_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DpChurchDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var dpChurchDateToBeDeleted = sender as DatePicker;

            if (dpChurchDateToBeDeleted != null)
            {
                DateTime dateToBeDeleted = dpChurchDateToBeDeleted.SelectedDate.Value;
                int ret_error = check_date_bounds(); 
                
                if (ret_error == 1)
                    return;


            }
        }

        private void DpChurchDate_Loaded(object sender, RoutedEventArgs e)
        {
            var date = sender as DatePicker;

            Add_Blackout_Dates(ref dpChurchDate);
        }

        private int check_date_bounds()
        {

            DateTime curdate = DateTime.Now;
            DateTime datelimit;
            List<DateTime> lstsundays = new List<DateTime>();
            int i = 0;
            if (curdate.DayOfWeek != DayOfWeek.Sunday)
            {

                for (DateTime sundate = curdate.Date; sundate >= curdate.AddDays(-7); sundate = sundate.AddDays(-1))
                {
                    if (sundate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        lstsundays.Add(sundate);
                        break;
                    }
                }
                datelimit = lstsundays.FirstOrDefault();

            }
            else
            {
                datelimit = curdate;
            }


            if (datelimit != null)
            {
                if (dpChurchDate.SelectedDate > datelimit)
                {
                    
                    MessageBox.Show($"Date limit is {datelimit.ToShortDateString()}.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                   
                    return 1;
                }
            }

            return 0;

        }

        private void Add_Blackout_Dates(ref Calendar calendar)
        {
            var dates = new List<DateTime>();
            DateTime date = calendar.DisplayDate;

            DateTime startDate = date.AddMonths(-1);
            DateTime endDate = date.AddMonths(2);

            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {

                if (dt.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(dt);
                }


            }
            foreach (var d in dates)
            {
                calendar.BlackoutDates.Add(new CalendarDateRange(d, d));
            }
        }

    }

  
}
