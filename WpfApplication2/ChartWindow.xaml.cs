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

                kvpAttended.Add(new KeyValuePair<string, int>(d.Date.ToString("MM-dd-yyyy"), AttendedSum));
                kvpFollowUp.Add(new KeyValuePair<string, int>(d.Date.ToString("MM-dd-yyyy"), FollowUpSum));
                kvpResponded.Add(new KeyValuePair<string, int>(d.Date.ToString("MM-dd-yyyy"), RespondedSum));


            }

            ChartData.Add(kvpAttended);
            ChartData.Add(kvpFollowUp);
            ChartData.Add(kvpResponded);

            return ChartData;
        }


        private List<List<KeyValuePair<string, int>>> PrepareChartData(DateTime startDate)
        {
            List<KeyValuePair<string, int>> kvpAttended = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> kvpFollowUp = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> kvpResponded = new List<KeyValuePair<string, int>>();
            var ChartData = new List<List<KeyValuePair<string, int>>>();

            // query all Attendees by Attended, Responded, FollowUp
            var dates = new List<DateTime>();




            for (var dt = startDate; dt <= startDate; dt = dt.AddDays(1))
            {

                if (dt.DayOfWeek == DayOfWeek.Sunday)
                {
                    dates.Add(dt);
                }


            }


            foreach (var d in dates)
            {



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

                kvpAttended.Add(new KeyValuePair<string, int>(d.Date.ToString("MM-dd-yyyy"), AttendedSum));
                kvpFollowUp.Add(new KeyValuePair<string, int>(d.Date.ToString("MM-dd-yyyy"), FollowUpSum));
                kvpResponded.Add(new KeyValuePair<string, int>(d.Date.ToString("MM-dd-yyyy"), RespondedSum));


            }

            ChartData.Add(kvpAttended);
            ChartData.Add(kvpFollowUp);
            ChartData.Add(kvpResponded);

            return ChartData;
        }
        private void showColumnChart()
        {
            DateTime startDate = m_StartDateSelected.Date;
            DateTime endDate = m_EndDateSelected.Date;

            if (m_StartDateIsValid && m_EndDateIsValid)
            {
                //Setting data for column chart
                AttendeeChart.DataContext = PrepareChartData(startDate, endDate);

            }
            else if (m_StartDateIsValid && !m_EndDateIsValid)
            {
                AttendeeChart.DataContext = PrepareChartData(startDate);
            }
            else if (!m_StartDateIsValid && m_EndDateIsValid)
            {
                AttendeeChart.DataContext = PrepareChartData(endDate = startDate);
            }


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

                m_StartDateSelected = date;


                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    m_StartDateIsValid = true;
                    cmbStartDate.Text = date.ToString("MM-dd-yyyy");
                }
                else
                {
                    m_StartDateIsValid = false;
                    cmbStartDate.Text = "Date Not Valid";
                }
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

                m_EndDateSelected = date;


                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    m_EndDateIsValid = true;
                    cmbEndDate.Text = date.ToString("MM-dd-yyyy");
                }
                else
                {
                    m_EndDateIsValid = false;
                    cmbEndDate.Text = "Date Not Valid";
                }
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
                    m_StartDateIsValid = true;
                    cmbStartDate.Text = date.ToString("MM-dd-yyyy");
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
                }

                else
                    m_EndDateIsValid = false;



            }
        }

        private void btnPlot_Click(object sender, RoutedEventArgs e)
        {
            if (m_StartDateSelected > m_EndDateSelected)
            {
                MessageBoxResult mr = MessageBox.Show("Start date cannot be greater than end date.", "Date range error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                showColumnChart();
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}