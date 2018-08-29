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
                                  select new AttRecord
                                  {
                                      id = attinfo.Attendance_InfoId,
                                      fname = att.FirstName,
                                      lname = att.LastName,
                                      date = attinfo.Date,
                                      status = attinfo.Status
                                  };


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
        private DataSet m_DataSet = new DataSet() { };

        private string m_query = "0";

        private void DeleteRecordFromActivitiesTable(System.Collections.IList row_select)
        {
            DataTable ActivityStatusTableCopy = m_DataSet.Tables["ActivityStatusTable"].Copy();


            foreach (DataRowView drv in row_select)
            {
                bool bconv = int.TryParse(drv.Row["ActivityPairId"].ToString(), out int result);

                if (result == 0)
                {
                    // convert fail because AttendeeId = "", this is a added row but user want to delete it
                }
                else
                {
                    var Activityrec = m_dbContext.Activities.Local.SingleOrDefault(id => id.ActivityPairId == result);



                    if (Activityrec != null)
                    {
                        m_dbContext.Activities.Local.Remove(Activityrec);
                    }

                    // get row index of datarow to remove from ActivityStatus DataTable
                    int rowindex = m_DataSet.Tables["ActivityStatusTable"].Rows.IndexOf(drv.Row);
                   
                 
                    
                            //delete the row at the index in the copied table
                            ActivityStatusTableCopy.Rows[rowindex].Delete();
                        



                  
                }
            }

            ActivityStatusTableCopy.AcceptChanges();

            m_DataSet.Tables["ActivityStatusTable"].Clear();
            for (int i = 0; i <= ActivityStatusTableCopy.Rows.Count - 1; i++)
            {
                m_DataSet.Tables["ActivityStatusTable"].ImportRow(ActivityStatusTableCopy.Rows[i]);

            }

            GrdAttendee_ActivityList.DataContext = m_DataSet.Tables["ActivityStatusTable"];

                

        }
        private void DeleteRecordFromAttendanceInfoTable(System.Collections.IList row_select)
        {
            DataTable StatusTableCopy = m_DataSet.Tables["StatusTable"].Copy();


            foreach (DataRowView drv in row_select)
            {
                bool bconv = int.TryParse(drv.Row["AttendeeInfoId"].ToString(), out int result);

                if (result == 0)
                {
                    // convert fail because AttendeeId = "", this is a added row but user want to delete it
                }
                else
                {
                    var AttInforec = m_dbContext.Attendance_Info.Local.SingleOrDefault(id => id.Attendance_InfoId == result);




                   if (AttInforec != null)
                   {
                        m_dbContext.Attendance_Info.Remove(AttInforec);
                   }
                        
                 

                }



                // get row index of datarow to remove from AttendeeList DataTable
                int rowindex = m_DataSet.Tables["StatusTable"].Rows.IndexOf(drv.Row);

                              
                        StatusTableCopy.Rows[rowindex].Delete();
              
            }

            StatusTableCopy.AcceptChanges();
          
            m_DataSet.Tables["StatusTable"].Clear();
            for (int i = 0; i <= StatusTableCopy.Rows.Count - 1; i++)
            {
                m_DataSet.Tables["StatusTable"].ImportRow(StatusTableCopy.Rows[i]);

            }

            GrdAttendee_InfoList.DataContext = m_DataSet.Tables["StatusTable"];



        }
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            var AttendanceInfoRow_select = GrdAttendee_InfoList.SelectedItems;
            var ActivityRow_select = GrdAttendee_ActivityList.SelectedItems;

            if (AttendanceInfoRow_select.Count != 0)
            {

                DeleteRecordFromAttendanceInfoTable(AttendanceInfoRow_select);

                //foreach (DataRow dr in AttendanceInfoRow_select)
                //{
                //    int AttendeeInfoId = int.Parse(dr["AttendeeInfoId"].ToString());
                //    var queryAttendeeInfo = (from inforec in m_dbContext.Attendance_Info.Local
                //                             where inforec.Attendance_InfoId == AttendeeInfoId
                //                             select inforec).ToArray().FirstOrDefault();

                //    if (queryAttendeeInfo != null)
                //    {
                //        m_dbContext.Attendance_Info.Local.Remove(queryAttendeeInfo);

                //    }


                Cursor = Cursors.Arrow;

                MessageBox.Show("Attendance record removed successfully.\n\nChanges has not been saved to the database until the Save button is clicked.", "Records removed", MessageBoxButton.OK, MessageBoxImage.None);

            }
            else if (ActivityRow_select.Count != 0)
            {


                DeleteRecordFromActivitiesTable(ActivityRow_select);
                //foreach (DataRow dr in ActivityRow_select)
                //{
                //    int ActivityId = int.Parse(dr["ActivityPairId"].ToString());
                //    var queryActivity = (from activityrec in m_dbContext.Activities.Local
                //                             where activityrec.ActivityPairId == ActivityId
                //                             select activityrec).ToArray().FirstOrDefault();

                //    if (queryActivity != null)
                //    {
                //        m_dbContext.Activities.Local.Remove(queryActivity);

                //    }


                Cursor = Cursors.Arrow;

                MessageBox.Show("Activity record removed successfully.\n\nChanges has not been saved to the database until the Save button is clicked.", "Records removed", MessageBoxButton.OK, MessageBoxImage.None);
            }
        }
    
                           
       

        private void UpdateDataTable(IQueryable<ActivityRecord> linqActivity, IQueryable<AttRecord> linqAttendance)
        {
           

            
            DataTable StatusTable = new DataTable("StatusTable");
            DataTable ActivityStatusTable = new DataTable("ActivityStatusTable");

            StatusTable.Columns.Add(new DataColumn("AttendeeInfoId"));
            StatusTable.Columns.Add(new DataColumn("First Name"));
            StatusTable.Columns.Add(new DataColumn("Last Name"));
            StatusTable.Columns.Add(new DataColumn("Date"));
            StatusTable.Columns.Add(new DataColumn("Status"));
            

            

            ActivityStatusTable.Columns.Add(new DataColumn("ActivityPairId"));
            ActivityStatusTable.Columns.Add(new DataColumn("First Name"));
            ActivityStatusTable.Columns.Add(new DataColumn("Last Name"));
            ActivityStatusTable.Columns.Add(new DataColumn("Date"));
            ActivityStatusTable.Columns.Add(new DataColumn("Activity"));

            foreach (var rec in linqAttendance)
            {

                DataRow newrow = StatusTable.NewRow();

                newrow["AttendeeInfoId"] = rec.id;
                newrow["First Name"] = rec.fname;
                newrow["Last Name"] = rec.lname;
                newrow["Date"] = rec.date?.ToString("MM-dd-yyyy");
                newrow["Status"] = rec.status;

                StatusTable.Rows.Add(newrow);
            }


            foreach (var rec in linqActivity)
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
                    newActivityRow["Date"] = rec.activity_date;
                }
                else
                    newActivityRow["Date"] = "n/a";



                ActivityStatusTable.Rows.Add(newActivityRow);
            }

            //GrdAttendeeInfo.ItemsSource = linqquery;

            //Add tables to dataset
            if (!m_DataSet.Tables.Contains("StatusTable"))
            {
                m_DataSet.Tables.Add(StatusTable);
            }
            else
            {
                m_DataSet.Tables.Remove("StatusTable");
                m_DataSet.Tables.Add(StatusTable);
            }
            
            if (!m_DataSet.Tables.Contains("ActivityStatusTable") )
            {
                m_DataSet.Tables.Add(ActivityStatusTable);
            }
            else
            {
                m_DataSet.Tables.Remove("ActivityStatusTable");
                m_DataSet.Tables.Add(ActivityStatusTable);
            }

            
            //swap first name last name column
            StatusTable.Columns[1].SetOrdinal(2);
            GrdAttendee_InfoList.DataContext = StatusTable;
            GrdAttendee_InfoList.ColumnWidth = 100;

            m_DataSet.Tables["StatusTable"].AcceptChanges();
            m_DataSet.Tables["ActivityStatusTable"].AcceptChanges();
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
