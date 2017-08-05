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

namespace CAOGAttendeeProject

{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        public ChartWindow(ModelDb db)
        {
            InitializeComponent();

            m_db = db;

        }


        private ModelDb m_db;
        private DateTime m_StartDateSelected;
        private DateTime m_EndDateSelected;

        private bool m_StartDateIsValid = false;
        private bool m_EndDateIsValid = false;

        private List<List<KeyValuePair<string, int>>> PrepareChartData(DateTime startDate, DateTime endDate)
        {

            List<KeyValuePair<string, int>> kvpAttended = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> kvpFollowUp = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> kvpResponded = new List<KeyValuePair<string, int>>();
            var ChartData = new List<List<KeyValuePair<string, int>>>();

            // query all Attendees by Attended, Responded, FollowUp
            var dates = new List<DateTime>();


        

        

            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {

                if (dt.DayOfWeek == DayOfWeek.Sunday)
                {
                    dates.Add(dt);
                }


            }

           
                foreach (var d in dates)
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




        private void DateStartCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            var calendar = sender as Calendar;
            DateTime date = calendar.DisplayDate;

            if (m_StartDateIsValid)
            {
                calendar.DisplayDate = m_StartDateSelected.Date;
            }
            else
            {

               // m_StartDateSelected = date;


                    m_StartDateIsValid = false;
                    cmbStartDate.Text = "Select or type date";

                Add_Blackout_Dates(ref calendar);
            }


        }

        private void DateEndCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            var calendar = sender as Calendar;
            DateTime date = calendar.DisplayDate;

            if (m_EndDateIsValid)
            {
                calendar.DisplayDate = m_EndDateSelected.Date;
            }
            else
            {

               // m_EndDateSelected = date;


                m_EndDateIsValid = false;
                    cmbEndDate.Text = "Select or type date";
              
                Add_Blackout_Dates(ref calendar);
            }

        }



        private void DateEndCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            var calendar = sender as Calendar;

            Add_Blackout_Dates(ref calendar);



        }
        private void DateStartCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            var calendar = sender as Calendar;

            Add_Blackout_Dates(ref calendar);



        }

        private void DateStartCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;
            string query = "";

            if (calender.SelectedDate.HasValue)
            {
                DateTime date = calender.SelectedDate.Value;

                m_StartDateSelected = date;


                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    cmbStartDate.Text = date.ToString("MM-dd-yyyy");
                    m_StartDateIsValid = true;
                    btnPlot.IsEnabled = true;
                }

                else
                    m_StartDateIsValid = false;



            }
        }
        private void DateEndCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;
            string query = "";

            if (calender.SelectedDate.HasValue)
            {
                DateTime date = calender.SelectedDate.Value;

                m_EndDateSelected = date;


                if (date.DayOfWeek == DayOfWeek.Sunday)
                {

                    cmbEndDate.Text = date.ToString("MM-dd-yyyy");
                    m_EndDateIsValid = true;
                    btnPlot.IsEnabled = true;
                }

                else
                    m_EndDateIsValid = false;



            }
        }

        private void btnPlot_Click(object sender, RoutedEventArgs e)
        {

            Regex pattern = new Regex(@"^[0-9]{2}-[0-9]{2}-[0-9]{4}");

            if (cmbStartDate.Text != "Select or type date")
            {
                if (pattern.IsMatch(cmbStartDate.Text))
                {


                    string text = pattern.Match(cmbStartDate.Text).ToString();
                    string[] splitstr = text.Split('-');
                    string month = splitstr[0];
                    string day = splitstr[1];
                    string year = splitstr[2];



                    try
                    {
                        m_StartDateSelected = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                        m_StartDateIsValid = true;

                    }
                    catch (Exception ex)
                    {
                        btnPlot.IsEnabled = false;
                        m_StartDateIsValid = false;
                        MessageBox.Show("Invalid date format. Date format must be in the form (mm-dd-yyyy)", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;

                    }
                }
                else
                {
                    btnPlot.IsEnabled = false;
                    m_StartDateIsValid = false;
                    MessageBox.Show("Invalid date format. Date format must be in the form (mm-dd-yyyy)", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (cmbEndDate.Text != "Select or type date")
            {

                if (pattern.IsMatch(cmbEndDate.Text))
                {
                    string text = pattern.Match(cmbEndDate.Text).ToString();
                    string[] splitstr = text.Split('-');
                    string month = splitstr[0];
                    string day = splitstr[1];
                    string year = splitstr[2];




                    try
                    {
                        m_EndDateSelected = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                        m_EndDateIsValid = true;

                    }
                    catch (Exception ex)
                    {
                        btnPlot.IsEnabled = false;
                        m_EndDateIsValid = false;
                        MessageBox.Show("Invalid date format. Date format must be in the form (mm-dd-yyyy)", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;

                    }
                }
                else
                {
                    btnPlot.IsEnabled = false;
                    m_EndDateIsValid = false;
                    MessageBox.Show("Invalid date format. Date format must be in the form (mm-dd-yyyy)", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


            }

        
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
                    return;
                }
                TimeSpan tspan = m_EndDateSelected - m_StartDateSelected;

                if (tspan.Days > 365 )
                {
                    MessageBoxResult mr = MessageBox.Show("End date too far into the future. Maximum timespan is 1 year", "Date range error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

            }

            showColumnChart();

    }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            btnPlot.IsEnabled = false;
            cmbStartDate.Text = "Select or type date.";
            cmbEndDate.Text = "Select or type date.";

        }

        private void cmbStartDate_KeyUp(object sender, KeyEventArgs e)
        {

          

            if (cmbStartDate.Text == "")
               btnPlot.IsEnabled = false;
            else
               btnPlot.IsEnabled = true;


            if (e.Key == Key.Enter)
            {

                btnPlot_Click(sender, e);

            }



        }

  private void cmbEndDate_KeyUp(object sender, KeyEventArgs e)
  {
            if (cmbEndDate.Text == "")
                btnPlot.IsEnabled = false;
            else
                btnPlot.IsEnabled = true;

         
         if (e.Key == Key.Enter)
         {

                btnPlot_Click(sender, e);
        }
            
  
  }

        private void cmbStartDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (cmbStartDate.Text == "Select or type date")
                cmbStartDate.Text = "";
        }

        private void cmbStartDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbStartDate.Text == "")
                cmbStartDate.Text = "Select or type date";
        }
        private void cmbEndDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (cmbEndDate.Text == "Select or type date")
                cmbEndDate.Text = "";
        }

        private void cmbEndDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbEndDate.Text == "")
                cmbEndDate.Text = "Select or type date";
        }

        
    }
}