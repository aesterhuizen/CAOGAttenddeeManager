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
using System.Data.SqlClient;
using System.Data;

namespace CAOGAttendeeProject
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WndAttendeeInfo : Window
    {
        public WndAttendeeInfo(string fname,string lname, SqlConnection myConnection)
        {
            InitializeComponent();
            
            //Load data from AttendeeId selected in MainWindow Grid
            
            string query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                        "FROM Attendees INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendees.FirstName='" + fname + "'" + " AND " + "Attendees.LastName='" + lname + "'";


            SqlDataAdapter myAdapter = new SqlDataAdapter(query, myConnection);

            DataSet ds = new DataSet();
            myAdapter.Fill(ds);

            GrdAttendeeInfo.DataContext = ds.Tables[0];


        }

     
    }
}
