using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for DeleteRecord.xaml
    /// </summary>
    public partial class DeleteRecordWindow : Window
    {
        bool m_delete_click = false;
        public bool getDeleteRecs
        {
            get
            {
                return m_delete_click;
            }
            set
            {
                if (m_delete_click != value)
                    m_delete_click = true;
            }

        }
        private DateTime? _dateToDelete = null;
        public DateTime? getDateToDelete
        {
            get
            {
                return _dateToDelete;
            }
            private set
            {
                if (_dateToDelete != value)
                {
                    _dateToDelete = value;
                }
            }

        }
       

        public DeleteRecordWindow(System.Collections.IList selectedRows)
        {
            InitializeComponent();
           
            if (selectedRows.Count !=0 )
            {
                btnDeleteSelectedRecords.IsEnabled = true;
            }
            else
            {
                btnDeleteSelectedRecords.IsEnabled = false;
            }
        }
      

       
        private void BtnDeleteSelectedRecords_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m_delete_click = true;
            Close();
        }

     

        private void BtnDeleteDateRecords_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            m_delete_click = true;
            
            Close();

            


        }

        private void DpChurchDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var dpChurchDateToBeDeleted = sender as DatePicker;

            if (dpChurchDateToBeDeleted.SelectedDate != null)
            {
                DateTime dateToBeDeleted = dpChurchDateToBeDeleted.SelectedDate.Value;
                int ret_error = check_date_bounds();

                if (ret_error == 1)
                {
                    dpChurchDate.Text = "";
                    return;

                }
                else
                {

                    _dateToDelete = dateToBeDeleted;
                    btnDeleteDateRecords.IsEnabled = true;
                }
                

            }
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

        private void Add_Blackout_Dates(ref DatePicker dp_cal)
        {
            var dates = new List<DateTime?>();
            DateTime? date = dp_cal.DisplayDate;

            DateTime? startDate = date?.AddMonths(-10);
            DateTime? endDate = date?.AddMonths(10);

            for (var dt = startDate; dt <= endDate; dt = dt?.AddDays(1))
            {

                if (dt?.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(dt);
                }


            }
            foreach (DateTime d in dates)
            {
                dp_cal.BlackoutDates.Add(new CalendarDateRange(d, d));
            }
        }
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Add_Blackout_Dates(ref dpChurchDate);

            m_delete_click = false;
            btnDeleteDateRecords.IsEnabled = false;
            
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (m_delete_click == true)
            {
                // do nothing
            }
            else
                m_delete_click = false;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            m_delete_click = false;
            Close();
        }
    }


}
