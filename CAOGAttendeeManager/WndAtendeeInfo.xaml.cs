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
                        "WHERE Attendees.FirstName='" + fname + "'" + " AND " + "Attendees.LastName='" + lname + "'" +
                        "ORDER BY Date ASC";

          

            SqlDataAdapter myAdapter = new SqlDataAdapter(query, myConnection);

            DataTable dt = new DataTable();
            myAdapter.Fill(dt);


            DataTable StatusTable = new DataTable();


            StatusTable.Columns.Add(new DataColumn("First Name"));
            StatusTable.Columns.Add(new DataColumn("Last Name"));
            StatusTable.Columns.Add(new DataColumn("Date Last Attended"));
            StatusTable.Columns.Add(new DataColumn("Date"));
            StatusTable.Columns.Add(new DataColumn("Status"));



            string datefmt = "";




            foreach (DataRow dr in dt.Rows)
            {

                DataRow newrow = StatusTable.NewRow();

                DateTime date = (DateTime)dr["Date"];



                newrow["First Name"] = dr["FirstName"];
                newrow["Last Name"] = dr["LastName"];
                newrow["Date"] = date.ToString("MM-dd-yyyy");

                DateTime ldate = (DateTime)dr["Last_Attended"];

                newrow["Date Last Attended"] = ldate.ToString("MM-dd-yyyy");
                newrow["Status"] = dr["Status"];


                StatusTable.Rows.Add(newrow);
            }






            StatusTable.Columns[2].ColumnName = "Last Attended";

           
            GrdAttendeeInfo.DataContext = StatusTable;
            GrdAttendeeInfo.ColumnWidth = 100;
            

        }


    }

   

}
