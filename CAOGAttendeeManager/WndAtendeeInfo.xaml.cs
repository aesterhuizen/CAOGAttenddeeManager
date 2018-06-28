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
        public WndAttendeeInfo(string fname, string lname, ref ModelDb dbcontext)
        {
            InitializeComponent();
            IQueryable<AttRecord> querylinq;

            m_dbContext = dbcontext;


            querylinq = from att in dbcontext.Attendees.Local.AsQueryable()
                        join attinfo in dbcontext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        where att.FirstName == fname && att.LastName == lname
                        orderby attinfo.Date ascending
                        select new AttRecord { id = attinfo.Attendance_InfoId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };







            UpdateDataTable(querylinq);

        }

        private ModelDb m_dbContext;
        private string m_query = "0";

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            var row_select = GrdAttendeeInfo.SelectedItems;
            if (row_select.Count != 0)
            {


                foreach (DataRow dr in row_select)
                {
                    int AttendeeInfoId = int.Parse(dr["AttendeeInfoId"].ToString());
                    var queryAttendeeInfo = (from inforec in m_dbContext.Attendance_Info.Local
                                             where inforec.Attendance_InfoId == AttendeeInfoId
                                             select inforec).ToArray().FirstOrDefault();

                    if (queryAttendeeInfo != null)
                    {
                        m_dbContext.Attendance_Info.Local.Remove(queryAttendeeInfo);

                    }

                    //m_dbContext.SaveChanges();
                    Cursor = Cursors.Arrow;
                    // UpdateDataTable(,m_query);
                    MessageBox.Show("Attendee record removed successfully.", "Remove Record", MessageBoxButton.OK, MessageBoxImage.None);
                }
            }
                           
       }

        private void UpdateDataTable(IQueryable<AttRecord> linqquery)
        {
            //SqlDataAdapter myAdapter = new SqlDataAdapter(query,m_sqlconnnection);

            //DataTable dt = new DataTable();
           // myAdapter.Fill(dt);


            DataTable StatusTable = new DataTable();


            StatusTable.Columns.Add(new DataColumn("AttendeeInfoId"));
            StatusTable.Columns.Add(new DataColumn("First Name"));
            StatusTable.Columns.Add(new DataColumn("Last Name"));
           // StatusTable.Columns.Add(new DataColumn("Date Last Attended"));
            StatusTable.Columns.Add(new DataColumn("Date"));
            StatusTable.Columns.Add(new DataColumn("Status"));



            foreach (var rec in linqquery)
            {

                DataRow newrow = StatusTable.NewRow();

                newrow["AttendeeInfoId"] = rec.id;
                newrow["First Name"] = rec.fname;
                newrow["Last Name"] = rec.lname;
                newrow["Date"] = rec.date?.ToString("MM-dd-yyyy");
                newrow["Status"] = rec.status;


                StatusTable.Rows.Add(newrow);
            }



            // GrdAttendeeInfo.ItemsSource = linqquery;



            // StatusTable.Columns[2].ColumnName = "Date";
            //swap first name last name column
            StatusTable.Columns[1].SetOrdinal(2);
            GrdAttendeeInfo.DataContext = StatusTable;
            GrdAttendeeInfo.ColumnWidth = 100;
            
            


        }

        private void GrdAttendeeInfo_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (GrdAttendeeInfo.Columns.Count > 1)
            {
                GrdAttendeeInfo.Columns[0].Visibility = Visibility.Hidden;
            }
        }
    }

   

}
