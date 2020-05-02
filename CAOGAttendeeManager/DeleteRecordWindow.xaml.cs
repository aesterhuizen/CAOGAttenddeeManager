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

        //private void DeleteSelectedRecs()
        private void BtnDeleteSelectedRecords_Click(object sender, RoutedEventArgs e)
        {
            //System.Collections.IList selectedRows = dataGrid.SelectedItems;


            //var default_row_selected = selectedRows.Cast<DefaultTableRow>();




            //if (selectedRows.Count != 0)
            //{

            //    Cursor = Cursors.Wait;
            //    bool isDirty = isAttendeeModified();


            //    if (isDirty)
            //    {
            //        MessageBoxResult res = MessageBox.Show("There are checked attendees in the attendee checklist that has not yet been added to the active attendance list.\n\n" +
            //                                               "Add them first then delete attendees.\n\nDiscard checked attendees in the attendee checklist and delete record anyway?", "Attendees not added yet", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            //        if (res == MessageBoxResult.OK)
            //        {
            //            if (m_AttendanceView)
            //            {

            //                //DeleteRecordInDefaultTable(selectedRows);
            //                //DeleteRecordInAttendeeListTable(selectedRows);


            //            }



            //        }

            //        else // isDirty: user pressed the cancel button on the messagebox
            //        {
            //            Cursor = Cursors.Arrow;
            //            return;
            //        }

            //    }
            //    else
            //    {
            //        DeleteRecordInDefaultTable(selectedRows);
            //        DeleteRecordInAttendeeListTable(selectedRows);
            //    }




            //}

            //else
            //{
            //    Cursor = Cursors.Arrow;
            //    MessageBox.Show("At least one record must be selected.", "Select Record", MessageBoxButton.OK, MessageBoxImage.Warning);
            //}


            //Cursor = Cursors.Arrow;

           // Display_DefaultTable_in_Grid();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Add_Blackout_Dates(ref dpChurchDate);
        }
    }

  
}
