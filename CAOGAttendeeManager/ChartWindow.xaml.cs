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
using System.Windows.Controls.DataVisualization.Charting;
using System.Data;
using System.Text.RegularExpressions;

namespace CAOGAttendeeManager

{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        public ChartWindow(ref ModelDb db)
        {
            InitializeComponent();

            m_db = db;

        }


        private ModelDb m_db;
        private DateTime m_StartDateSelected;
        private DateTime m_EndDateSelected;
        private List<DateTime> m_lstValidSundays = new List<DateTime> { };
       
        private bool m_StartDateIsValid = false;
        private bool m_EndDateIsValid = false;

      

        private List<List<KeyValuePair<string, int>>> PrepareChartData(DateTime startDate, DateTime endDate)
        {

            List<KeyValuePair<string, int>> kvpAttended = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> kvpFollowUp = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> kvpResponded = new List<KeyValuePair<string, int>>();
            var ChartData = new List<List<KeyValuePair<string, int>>>();

            // query all Attendees by Attended, Responded, FollowUp
            //var dates = new List<DateTime>();






         

            foreach (var d in m_lstValidSundays)
                {

                    string date = d.ToString("MM-dd-yyyy");

                    var queryAllAttendeesAttended_Per_Date = (from AttendanceAttendedRec in m_db.Attendance_Info
                                                              where AttendanceAttendedRec.Date == d
                                                             && AttendanceAttendedRec.Status == "Attended"
                                                              select AttendanceAttendedRec).ToArray();

                    var queryAllAttendeesFollowUp_Per_Date = (from AttendanceFollowUpRecs in m_db.Attendance_Info
                                                              where AttendanceFollowUpRecs.Date == d
                                                              && AttendanceFollowUpRecs.Status == "Follow-Up"
                                                              select AttendanceFollowUpRecs).ToArray();

                    var queryAllAttendeesResponded_Per_Date = (from AttendanceRespondedRecs in m_db.Attendance_Info
                                                               where AttendanceRespondedRecs.Date == d
                                                               && AttendanceRespondedRecs.Status == "Responded"
                                                               select AttendanceRespondedRecs).ToArray();


                    int AttendedSum = (queryAllAttendeesAttended_Per_Date.Length != 0) ?
                        queryAllAttendeesAttended_Per_Date.Count() : 0;

                    int FollowUpSum = (queryAllAttendeesFollowUp_Per_Date.Length != 0) ?
                        queryAllAttendeesFollowUp_Per_Date.Count() : 0;

                    int RespondedSum = (queryAllAttendeesResponded_Per_Date.Length != 0) ?
                        queryAllAttendeesResponded_Per_Date.Count() : 0;

                    kvpAttended.Add(new KeyValuePair<string, int>(date, AttendedSum));
                    kvpFollowUp.Add(new KeyValuePair<string, int>(date, FollowUpSum));
                    kvpResponded.Add(new KeyValuePair<string, int>(date, RespondedSum));

                    
                }

                ChartData.Add(kvpAttended);
                ChartData.Add(kvpFollowUp);
                ChartData.Add(kvpResponded);
           
            return ChartData;
   }

        private void showColumnChart()
        {

          
                
                AttendeeChart.DataContext = PrepareChartData(m_StartDateSelected, m_EndDateSelected);
            
        }






        private void RibbonApplicationMenuItem_Click_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
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




        private void DatesRangeCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            var calendar = sender as Calendar;
            var displayDate = calendar.DisplayDate;

            Add_Blackout_Dates(ref calendar);


                if (displayDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    m_StartDateIsValid = true;
                    m_StartDateSelected = displayDate;
                    txtStartDate.Text = displayDate.ToString("MM-dd-yyyy");
                    m_lstValidSundays.Add(displayDate);
                }
            
            
            else
            {
                m_StartDateIsValid = false;
               
            }
       

        }

        private void DatesRangeCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            var calendar = sender as Calendar;

          
            Add_Blackout_Dates(ref calendar);



        }
       

        private void DatesRangeCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;
          
            if (calender.SelectedDate.HasValue)
            {
               var SelectedDates = calender.SelectedDates;

           
                 
                if (SelectedDates.Count > 1)
                {
                   IEnumerable<DateTime> orderedDates = SelectedDates.OrderBy(d => d.Date);
                    m_lstValidSundays.Clear();
                    foreach (var d in orderedDates)
                    {
                        m_lstValidSundays.Add(d);
                    }
                    
                    m_StartDateSelected = orderedDates.FirstOrDefault().Date;
                    m_EndDateSelected = orderedDates.LastOrDefault().Date;

                    txtStartDate.Text = orderedDates.FirstOrDefault().Date.ToString("MM-dd-yyyy");
                    txtEndDate.Text = orderedDates.LastOrDefault().Date.ToString("MM-dd-yyyy");
                        

                    m_StartDateIsValid = true;
                    m_EndDateIsValid = true;
                    btnPlot.IsEnabled = true;
                }
                else
                {
                        m_StartDateSelected = SelectedDates.First().Date;

                    IEnumerable<DateTime> orderedDates = SelectedDates.OrderBy(d => d.Date);
                    m_lstValidSundays.Clear();
                    foreach (var d in orderedDates)
                    {
                        m_lstValidSundays.Add(d);
                    }

                    txtStartDate.Text = SelectedDates.First().Date.ToString("MM-dd-yyyy");
                        m_StartDateIsValid = true;
                        btnPlot.IsEnabled = true;
                        txtEndDate.Text = "";


                }





            }
        }
    

        private void btnPlot_Click(object sender, RoutedEventArgs e)
        {

            Cursor = Cursors.Wait;

            if (m_StartDateIsValid && !m_EndDateIsValid)
            {
                m_EndDateSelected = m_StartDateSelected;

            }
            else if (!m_StartDateIsValid && m_EndDateIsValid)
            {
                m_StartDateSelected = m_EndDateSelected;

            }
            else if (m_StartDateIsValid && m_EndDateIsValid)
            {
                if (m_StartDateSelected > m_EndDateSelected)
                {
                    MessageBoxResult mr = MessageBox.Show("Start date cannot be greater than end date.", "Date range error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return;
                }
                TimeSpan tspan = m_EndDateSelected - m_StartDateSelected;

                if (tspan.Days >= 365)
                {
                    MessageBoxResult mr = MessageBox.Show("End date too far into the future. Maximum timespan is 1 year", "Date range error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return;
                }

            }
                        
            showColumnChart();
            Cursor = Cursors.Arrow;
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (m_StartDateIsValid)
            {
                btnPlot.IsEnabled = true;
                txtStartDate.Text = m_StartDateSelected.ToString("MM-dd-yyyy");
                
            }
            else
            {
                btnPlot.IsEnabled = false;
            
            }

            

        }
 


     
        private void txtStartDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtStartDate.Text == "" && txtEndDate.Text == "")
            {
                btnPlot.IsEnabled = false;

            }

            else
            {
                btnPlot.IsEnabled = true;

            }
        }

        private void txtEndDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtStartDate.Text == "" && txtEndDate.Text == "")
            {
                btnPlot.IsEnabled = false;

            }

            else
            {
                btnPlot.IsEnabled = true;

            }
        }


        //private void txtEndDate_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (txtEndDate.Text == "Select or type date.")
        //    {
        //        txtEndDate.Text = "";
        //    }
        //}

        //private void txtStartDate_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (txtStartDate.Text == "Select or type date.")
        //    {
        //        txtStartDate.Text = "";
        //    }
        //}
    }
}