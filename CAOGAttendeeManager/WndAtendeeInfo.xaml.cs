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
            IQueryable<ActivityRecord> queryActivityList;
            IQueryable<AttRecord> queryAttendanceList;

            m_dbContext = dbcontext;


            queryAttendanceList = from att in dbcontext.Attendees.Local.AsQueryable()
                        join attinfo in dbcontext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        where att.FirstName == fname && att.LastName == lname
                        orderby attinfo.Date ascending
                        select new AttRecord {
                            id = attinfo.Attendance_InfoId,
                            fname = att.FirstName,
                            lname = att.LastName,
                            date = attinfo.Date,
                            status = attinfo.Status };


            queryActivityList = from att in m_dbContext.Attendees.Local.AsQueryable()
                        join activity in m_dbContext.Activities.Local on att.AttendeeId equals activity.AttendeeId
                        where att.FirstName == fname && att.LastName == lname
                        orderby activity.Date ascending
                        select new ActivityRecord
                        {
                            id = activity.ActivityPairId,
                            fname = att.FirstName,
                            lname = att.LastName,
                            activity_date = activity.DateString,
                            activity = activity.ToString()
                           
                        };




            UpdateDataTable(queryActivityList, queryAttendanceList);
          

        }

        private ModelDb m_dbContext;
       
        private string m_query = "0";

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            var row_select = GrdAttendee_InfoList.SelectedItems;
            row_select = GrdAttendee_ActivityList.SelectedItems;

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

        private void UpdateDataTable(IQueryable<ActivityRecord> linqquery, IQueryable<AttRecord> linqquery2)
        {
            //SqlDataAdapter myAdapter = new SqlDataAdapter(query,m_sqlconnnection);

            //DataTable dt = new DataTable();
           // myAdapter.Fill(dt);


            DataTable StatusTable = new DataTable();


            StatusTable.Columns.Add(new DataColumn("AttendeeInfoId"));
            StatusTable.Columns.Add(new DataColumn("First Name"));
            StatusTable.Columns.Add(new DataColumn("Last Name"));
            StatusTable.Columns.Add(new DataColumn("Church Last Attended"));
            StatusTable.Columns.Add(new DataColumn("Status"));
            

            DataTable ActivityStatusTable = new DataTable();

            ActivityStatusTable.Columns.Add(new DataColumn("ActivityPairId"));
            ActivityStatusTable.Columns.Add(new DataColumn("First Name"));
            ActivityStatusTable.Columns.Add(new DataColumn("Last Name"));
            ActivityStatusTable.Columns.Add(new DataColumn("Activity Last Attended"));
            ActivityStatusTable.Columns.Add(new DataColumn("Activity"));

            foreach (var rec in linqquery2)
            {

                DataRow newrow = StatusTable.NewRow();

                newrow["AttendeeInfoId"] = rec.id;
                newrow["First Name"] = rec.fname;
                newrow["Last Name"] = rec.lname;
                newrow["Church Last Attended"] = rec.date?.ToString("MM-dd-yyyy");
                newrow["Status"] = rec.status;

                StatusTable.Rows.Add(newrow);
            }


            foreach (var rec in linqquery)
            {
                DataRow newActivityRow = ActivityStatusTable.NewRow();

                newActivityRow["ActivityPairId"] = rec.id;
                newActivityRow["First Name"] = rec.fname;
                newActivityRow["Last Name"] = rec.lname;
                if (rec.activity != null)
                {
                    newActivityRow["Activity"] = rec.activity.ToString();
                }
                else
                    newActivityRow["Activity"] = "n/a";

                if (rec.activity_date != null)
                {
                    newActivityRow["Activity Last Attended"] = rec.activity_date;
                }
                else
                    newActivityRow["Activity Last Attended"] = "n/a";



                ActivityStatusTable.Rows.Add(newActivityRow);
            }

            //GrdAttendeeInfo.ItemsSource = linqquery;



            
            //swap first name last name column
            StatusTable.Columns[1].SetOrdinal(2);
            GrdAttendee_InfoList.DataContext = StatusTable;
            GrdAttendee_InfoList.ColumnWidth = 100;
          
                

            //swap first name last name column
            ActivityStatusTable.Columns[1].SetOrdinal(2);
            
            GrdAttendee_ActivityList.DataContext = ActivityStatusTable;
            GrdAttendee_ActivityList.ColumnWidth = 100;




        }

        private void GrdAttendeeInfo_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (GrdAttendee_InfoList.Columns.Count >= 1 || GrdAttendee_ActivityList.Columns.Count >= 1)
            {
                GrdAttendee_InfoList.Columns[0].Visibility = Visibility.Hidden;
                GrdAttendee_ActivityList.Columns[0].Visibility = Visibility.Hidden;
                
                    GrdAttendee_ActivityList.Columns[3].Width = 130;
                    GrdAttendee_ActivityList.Columns[4].Width = 500;
                
            }

            GrdAttendee_InfoList.Height = grd_AttInfoLists.RowDefinitions[0].ActualHeight - tbarDeleteRec.ActualHeight - txtblkAttendanceList.ActualHeight;
            GrdAttendee_ActivityList.Height = AttInfoWindow.ActualHeight - txtblkActivities.ActualHeight - grd_AttInfoLists.RowDefinitions[0].ActualHeight;
        }

        private void GrdAttendee_ActivityList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

        private void GrdAttendeeInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GrdAttendee_InfoList.Height = grd_AttInfoLists.RowDefinitions[0].ActualHeight - tbarDeleteRec.ActualHeight - txtblkAttendanceList.ActualHeight;
            GrdAttendee_ActivityList.Height = AttInfoWindow.ActualHeight - txtblkActivities.ActualHeight - grd_AttInfoLists.RowDefinitions[0].ActualHeight;

        }

        private void Gsplitter_lists_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            GrdAttendee_InfoList.Height = grd_AttInfoLists.RowDefinitions[0].ActualHeight - tbarDeleteRec.ActualHeight - txtblkAttendanceList.ActualHeight;
            GrdAttendee_ActivityList.Height = AttInfoWindow.ActualHeight - txtblkActivities.ActualHeight - grd_AttInfoLists.RowDefinitions[0].ActualHeight;
        }
    }

   

}
