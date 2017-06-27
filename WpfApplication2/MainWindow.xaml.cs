﻿using System;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Data;
using System.Text.RegularExpressions;
using System.Configuration;

//using System.Windows.Forms;




namespace CAOGAttendeeProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            // InitDataSet();
            // Clear_DB_Tables();
            //open file with database credentials


            var executingPath = Directory.GetCurrentDirectory();


            if (File.Exists($"{executingPath}\\credentials.txt"))
            {

                var fs = new FileStream($"{executingPath}\\credentials.txt", FileMode.Open, FileAccess.Read);
                using (var sr = new StreamReader(fs, Encoding.ASCII))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        m_constr = line;
                    }

                }



            }
            else
            {

                Console.WriteLine("Credential file does not exist!");
                return;
            }


            try
            {
                m_db = new ModelDb(m_constr);
                m_mySqlConnection = new SqlConnection(m_constr);

                // CreateDatabase_FromXLSX();
                // GenerateDBFollowUps();
                //clear_attendee_backlog();
                // FlagAttendeeForBacklog();
                Display_Database_in_Grid();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nException occurred when performing database initialization {ex}!\n", ex);
            }













        }




        private ModelDb m_db;
        private DataSet m_DataSet = new DataSet();
        private SqlConnection m_mySqlConnection = null;
        private DataTable m_tempFollowUpModeTable;
        private string m_constr = "";
        private string m_FirstName = "";
        private string m_LastName = "";

        private bool m_NonChecked = true;
        private DateTime m_DateSelected;
        private bool m_isAttendedChecked = false;
        private bool m_isFollowupChecked = false;
        private bool m_isRespondedChecked = false;
        private bool m_dateIsValid = false;
        private bool m_modeIsInListView = false;
        private bool m_modelIsInFollowUpView = true;
        private bool m_filterByDate = false;

        private void Clear_DB_Tables()
        {
            string myConnectionString = m_constr;


            string sqlClear_Attendees = "DELETE FROM Attendees";
            string sqlClear_AttendanceInfo = "DELETE FROM Attendance_Info";

            SqlConnection myConnection = new SqlConnection(myConnectionString);

            SqlCommand cmd = new SqlCommand(sqlClear_Attendees, myConnection);
            SqlCommand cmd2 = new SqlCommand(sqlClear_AttendanceInfo, myConnection);

            try
            {
                myConnection.Open();
                cmd.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                myConnection.Close();

            }
            catch (Exception t)
            {
                Console.Write("{0}", t);
            }



        }

        private void clear_attendee_backlog()
        {



            var queryAttendees = from AttendeeRec in m_db.Attendees
                                 select AttendeeRec;

            foreach (var Attendee in queryAttendees)
            {
                Attendee.HasThreeConsequitiveFollowUps = 0;
            }
            m_db.SaveChanges();

        }
        private void FlagAttendeeForBacklog()
        {
            //problem
            // flag each attendee for backlog that missed 3 consequtive follow-ups (84 days)

            //solution






            var queryAttendees = from AttendeeRec in m_db.Attendees
                                 select AttendeeRec;


            foreach (var AttendeeRec in queryAttendees)
            {




                var queryAttendeeDates = (from DateRec in AttendeeRec.AttendanceList
                                          where DateRec.AttendeeId == AttendeeRec.AttendeeId
                                          orderby DateRec.Date ascending
                                          select DateRec).ToArray();




                for (int idx = 0; idx < queryAttendeeDates.Count() - 1; idx++)
                {
                    if (queryAttendeeDates[idx].Status == "Follow-Up" &&
                        queryAttendeeDates[idx + 1].Status == "Follow-Up" &&
                        queryAttendeeDates[idx + 2].Status == "Follow-Up")
                    {
                        AttendeeRec.HasThreeConsequitiveFollowUps = 1;

                    }
                    if (queryAttendeeDates[idx].Status == "Attended")
                    {
                        AttendeeRec.HasThreeConsequitiveFollowUps = 0;
                    }
                }
            }
            m_db.SaveChanges();

        }




        private void GenerateDBFollowUps()
        {
            //problem
            // Generate Follow-Ups for each Attendee that missed 28 days of church

            //Solution
            //1)look and last date attended for each AttendeeId and see if attendee attended 4 weeks (28 days) in the past from now
            //2)if attendee did not attend 4 weeks in the past, generate a new database entry with status follow-up for corresponding AttendeeId
            //3) if attendee has 3 follow-ups in a row, flag attendee and don't consider him for another follow-up entry. 





            //m_constr = "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=C:\\Program Files\\Microsoft SQL Server\\MSSQL13.SQLEXPRESS\\MSSQL\\DATA\\Database.mdf;Integrated Security=True;Connect Timeout=30";
            //InitDataSet();






            // get current time
            DateTime curdate = DateTime.Now;


            var queryAttendees = from AttendeeRec in m_db.Attendees
                                 select AttendeeRec;


            foreach (var AttendeeRec in queryAttendees)
            {

                if (AttendeeRec.HasThreeConsequitiveFollowUps != 1)
                {

                    var queryLastDateAttended = (from DateRec in AttendeeRec.AttendanceList
                                                 orderby DateRec.Last_Attended ascending
                                                 select DateRec).ToList().LastOrDefault();
                    
                    if (queryLastDateAttended != null)
                    {

                        string[] arydate = queryLastDateAttended.Last_Attended.Split('-');
                        string year = arydate[2];
                        string month = arydate[1];
                        string day = arydate[0];

                        DateTime LastAttendedDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));

                        TimeSpan timespanSinceLastAttended = curdate - LastAttendedDate;

                        if (timespanSinceLastAttended.Minutes >= 30 /*Only a test */)
                        {

                            Attendance_Info newfollowUpRecord = new Attendance_Info { };
                            newfollowUpRecord.AttendeeId = AttendeeRec.AttendeeId;
                            newfollowUpRecord.Date = curdate.ToString("MM-dd-yyyy");
                            newfollowUpRecord.Last_Attended = month + "-" + day + "-" + year;
                            newfollowUpRecord.Status = "Follow-Up";

                            m_db.Attendance_Info.Add(newfollowUpRecord);
                        }
                    }
                }

                m_db.SaveChanges();

            }






        }
        private void CreateDatabase_FromXLSX()
        {
            // create Database from Excel Sheet






            Console.WriteLine("Openning Excel datasheet...");

            String connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;" +
                                        "Data Source=C:\\Users\\aesterh\\Documents\\church_stuff\\Attendance_Tracking10-23-16_revised.xlsx;" +
                                        "Extended Properties='Excel 12.0;IMEX=1'";

            string sqlcmd = "SELECT * FROM [2016 Jan-Mar$]";


            using (OleDbConnection oleConnection = new OleDbConnection(connectionString))
            {
                // command to select all the data from the database Q1
                OleDbCommand oleCommand = new OleDbCommand(sqlcmd, oleConnection);


                try
                {
                    oleConnection.Open();
                    Console.WriteLine("Database successfully opened!");

                    // create data reader
                    OleDbDataReader oleDataReader = oleCommand.ExecuteReader();


                    Regex string_f = new Regex(@"^F");

                    string year = "";
                    string[] FLname = { "", "" };
                    string[] md;
                    int isValid_employee = 0;
                    int att_infoID = 0, attID = 0, attLst_Idx = 0, triad_counter = 0;

                    Attendance_Info Attendee_Status = null;
                    Attendee churchAttendee = null;

                    while (oleDataReader.Read())
                    {

                        if (attID == 500) { break; }
                        // if year found store it in year variable
                        // if last attended found populate variable
                        // if date 
                        //  1) populate object with date information and set count to 1, loop over array in buckets of 3 until and get attendee status (Attended,Contacted,Responded)
                        //   loop until end of record
                        // every 5th row is a name, there are 4 rows in between each name. so loop until you get a name then create a new attendee 
                        // and fill out the attendeeID info

                        // increase current row counter and attendee ID counter

                        //problem, not all the names is evently spaced.
                        //solution get row numbers of all the names that has a 1 in front of it and write in in an array


                        //----------set conditions for for loop------------------------------------


                        if (oleDataReader[0].ToString() == "1")
                        {
                            isValid_employee = 1;

                            // add previous church attendee to database and create a new church attendee 
                            if (churchAttendee != null)
                            {
                                // add attendee to database table
                                m_db.Attendees.Add(churchAttendee);
                                m_db.Attendance_Info.Add(Attendee_Status);
                            }
                            // create new attendee
                            churchAttendee = new Attendee();
                            attID++;

                            year = oleDataReader.GetName(1);
                            FLname = oleDataReader[1].ToString().Split(' ');

                            churchAttendee.FirstName = FLname[1];
                            churchAttendee.LastName = FLname[0];
                            churchAttendee.HasThreeConsequitiveFollowUps = 0;
                            //churchAttendee.AttendeeId = attID;
                            triad_counter = 0;

                        }
                        else
                        {
                            triad_counter++;
                            isValid_employee = 0;
                        }

                        //-----------------------do every record-------------------------

                        for (int col_index = 1; col_index < oleDataReader.FieldCount - 1; col_index++)
                        {
                            // date column
                            if ((col_index % 4 == 0))
                            {
                                if (string_f.IsMatch(oleDataReader.GetName(col_index)))
                                {
                                    attLst_Idx = 0;
                                    break;
                                }

                                if (isValid_employee == 1)
                                {
                                    Attendee_Status = new Attendance_Info { };
                                    //att_infoID++;

                                    md = oleDataReader.GetName(col_index).ToString().Split('/');
                                    string date = (new DateTime(int.Parse(year), int.Parse(md[0]), int.Parse(md[1])).ToString("MM-dd-yyyy"));
                                    string[] arylstAttDate = oleDataReader[2].ToString().Split('.');
                                    string lstyear = "20" + arylstAttDate[2];
                                    string lstAttendedDate = (new DateTime(int.Parse(lstyear), int.Parse(arylstAttDate[0]), int.Parse(arylstAttDate[1])).ToString("MM-dd-yyyy"));

                                    //Attendee_Status.Attendance_InfoId = att_infoID;
                                    Attendee_Status.AttendeeId = attID;
                                    Attendee_Status.Date = date;
                                    Attendee_Status.Last_Attended = lstAttendedDate;
                                    churchAttendee.AttendanceList.Add(Attendee_Status);
                                }

                                // add attended to churchAtendee attended
                                switch (triad_counter)
                                {
                                    case 1: //Attended
                                        {

                                            churchAttendee.AttendanceList[attLst_Idx].Status = (oleDataReader[col_index].ToString() == "1") ? "Attended" : "Not Attended";
                                            attLst_Idx++;
                                            break;

                                        }
                                    case 2: //Contacted
                                        {
                                            if (oleDataReader[col_index + 1].ToString() == "1")
                                            {
                                                churchAttendee.AttendanceList[attLst_Idx].Status = "Follow-Up";

                                                attLst_Idx++;
                                            }
                                            break;
                                        }
                                    case 3: //Responded
                                        {
                                            if (oleDataReader[col_index + 2].ToString() == "1")
                                            {
                                                churchAttendee.AttendanceList[attLst_Idx].Status = "Responded";
                                                //churchAttendee.AttendanceList.Add(Attendee_Status);
                                                attLst_Idx++;
                                            }



                                            break;
                                        }
                                    default:
                                        break;


                                }


                            } // end col_index % 4
                        }    // end for col_index


                    } // end data reader read
                    m_db.Attendees.Add(churchAttendee);
                    m_db.Attendance_Info.Add(Attendee_Status);
                    m_db.SaveChanges();
                    Console.WriteLine("\nDone!\n");
                    oleDataReader.Close();
                } // end try




                catch (Exception ex)
                {
                    Console.Write("{0}", ex);

                }





            } // end using oleconnection
        } // end sub
          //----Display Data in Grid------------------------------------------------------------------------------------------------------------------------   
        private void Display_Database_in_Grid()
        {
            InitDataSet();

            dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];

        } // end  private void Display_Database_in_Grid()


        private void InitDataSet()
        {

            // default dataview from database

            //string mysqlstring = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
            //                "FROM Attendees " +
            //                "INNER JOIN Attendance_Info " +
            //                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
            //                "ORDER BY Attendees.FirstName ASC";

            //string sqlAttendees = "SELECT Attendees.AttendeeId, Attendees.HasThreeConsequitiveFollowUps, Attendees.FirstName,Attendees.LastName FROM Attendees ORDER BY Attendees.FirstName ASC";
            //string sqlAttendee_Info = "SELECT * FROM Attendance_Info";

            m_mySqlConnection = new SqlConnection(m_constr);



            //--------------------- Make DEFAULT TABLE---------------------------------------------------------------------------

            DataTable Default_Data_Table = new DataTable("DefaultTable");

            try
            {

                var queryAttendees = from AttendeeRec in m_db.Attendees
                                     select AttendeeRec;

                Default_Data_Table.Columns.Add(new DataColumn("AttendeeId"));
                Default_Data_Table.Columns.Add(new DataColumn("FirstLastName"));
                Default_Data_Table.Columns.Add(new DataColumn("First Name"));
                Default_Data_Table.Columns.Add(new DataColumn("Last Name"));
                Default_Data_Table.Columns.Add(new DataColumn("Date Last Attended"));
                Default_Data_Table.Columns.Add(new DataColumn("Status"));


                foreach (var AttendeeRec in queryAttendees)
                {

                    DataRow dr = Default_Data_Table.NewRow();
                    dr["AttendeeId"] = AttendeeRec.AttendeeId;
                    dr["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;

                    dr["First Name"] = AttendeeRec.FirstName;
                    dr["Last Name"] = AttendeeRec.LastName;
                    

                    dr["Date Last Attended"] = AttendeeRec.AttendanceList[0].Last_Attended;
                    dr["Status"] = AttendeeRec.AttendanceList[0].Status;


                    Default_Data_Table.Rows.Add(dr);
                }


                Default_Data_Table.AcceptChanges();
                m_DataSet.Tables.Add(Default_Data_Table);


                //-------------------------------Make AttendeeList Table-------------------------------------------------------------------

                DataTable AttendeeListTable = new DataTable("AttendeeListTable");
                string date;
                // m_ListChkBox.Checked += new RoutedEventHandler(AttendeeListchkBox_CheckedChanged);


                if (m_filterByDate && m_dateIsValid)
                    date = DateCalendar.DisplayDate.ToString("MM-dd-yyyy");
                else
                    date = "Date Not Valid";


                AttendeeListTable.Columns.Add(new DataColumn("AttendeeId"));
                AttendeeListTable.Columns.Add(new DataColumn("FirstLastName"));
                AttendeeListTable.Columns.Add(new DataColumn("First Name"));
                AttendeeListTable.Columns.Add(new DataColumn("Last Name"));
                AttendeeListTable.Columns.Add(new DataColumn("Date"));
                AttendeeListTable.Columns.Add(new DataColumn("Attended", typeof(bool)));


                //DataGridCheckBoxColumn checkcol = new DataGridCheckBoxColumn();
                //dataGrid.Columns.Add(checkcol);
                //CheckBox chkbox = new CheckBox();
                //checkcol.Header = chkbox;


                //chkbox.Checked += new RoutedEventHandler(cb_Checked);
                //chkbox.Unchecked += new RoutedEventHandler(cb_Unchecked);

                foreach (DataRow dr in m_DataSet.Tables["DefaultTable"].Rows)
                {

                    DataRow drNewAttendeeListRec = AttendeeListTable.NewRow();


                    drNewAttendeeListRec["AttendeeId"] = dr["AttendeeId"];
                    drNewAttendeeListRec["FirstLastName"] = dr["FirstLastName"];

                    drNewAttendeeListRec["First Name"] = dr["First Name"];
                    drNewAttendeeListRec["Last Name"] = dr["Last Name"];
                    drNewAttendeeListRec["Date"] = date;
                    drNewAttendeeListRec["Attended"] = false;
                    AttendeeListTable.Rows.Add(drNewAttendeeListRec);
                }


                AttendeeListTable.AcceptChanges();
                m_DataSet.Tables.Add(AttendeeListTable);

                //---------------------Make Attendees Table-------------------------------------------------------------------------------------


                //DataTable AttendeesTable = new DataTable("AttendeesTable");

                //AttendeesTable.Columns.Add(new DataColumn("AttendeeId"));
                //AttendeesTable.Columns.Add(new DataColumn("First Name"));
                //AttendeesTable.Columns.Add(new DataColumn("Last Name"));
                //AttendeesTable.Columns.Add(new DataColumn("HasThreeConsequativeFollowUps"));




                //foreach (var AttendeesRec in queryAttendees)
                //{

                //    DataRow drNewAttendeeRec = AttendeesTable.NewRow();


                //    drNewAttendeeRec["AttendeeId"] = AttendeesRec.AttendeeId;
                //    drNewAttendeeRec["First Name"] = AttendeesRec.FirstName;
                //    drNewAttendeeRec["Last Name"] = AttendeesRec.LastName;
                //    drNewAttendeeRec["HasThreeConsequativeFollowUps"] = AttendeesRec.HasThreeConsequitiveFollowUps;

                //    AttendeesTable.Rows.Add(drNewAttendeeRec);
                //}


                //AttendeesTable.AcceptChanges();
                //m_DataSet.Tables.Add(AttendeesTable);


                //-Setup AttendanceInfo Table----------------------------------------------------------------------------------------------------------

                //var queryAttendanceInfo = from attInfo in m_db.Attendance_Info
                //                          select attInfo;

                //DataTable AttendanceInfoTable = new DataTable("AttendanceInfoTable");

                //AttendanceInfoTable.Columns.Add(new DataColumn("AttendeeId"));
                //AttendanceInfoTable.Columns.Add(new DataColumn("Date Last Attended", typeof(DateTime)));
                //AttendanceInfoTable.Columns.Add(new DataColumn("Date", typeof(DateTime)));
                //AttendanceInfoTable.Columns.Add(new DataColumn("Status"));



                //foreach (var AttendanceRec in queryAttendanceInfo)
                //{

                //    DataRow drNewAttendanceRec = AttendanceInfoTable.NewRow();


                //    drNewAttendanceRec["AttendeeId"] = AttendanceRec.AttendeeId;
                //    drNewAttendanceRec["Date Last Attended"] = AttendanceRec.Last_Attended;
                //    drNewAttendanceRec["Date"] = AttendanceRec.Date;
                //    drNewAttendanceRec["Status"] = AttendanceRec.Status;

                //    AttendanceInfoTable.Rows.Add(drNewAttendanceRec);
                //}


                //AttendanceInfoTable.AcceptChanges();
                //m_DataSet.Tables.Add(AttendanceInfoTable);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred when performing database operation: {ex}");
            }




        }

      
        private void chkResponded_Checked(object sender, RoutedEventArgs e)
        {
            //if (txtSearch.Text != "")
            //{
            //    MessageBox.Show("Cannot filter while a seach is in progress. Clear the search before choosing filter options", "Filter invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
            //    chkResponded.IsChecked = false;
            //    return;
            //}

            chkAttended.IsChecked = false;
            chkFollowup.IsChecked = false;
            m_isRespondedChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");
            string query = "";
            m_NonChecked = false;

            if (m_filterByDate && m_dateIsValid)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'" +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";


            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Responded'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Responded' " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

            }

            UpdateDataGrid(query);



        }

        private void chkFollowup_Checked(object sender, RoutedEventArgs e)
        {

            //if (txtSearch.Text != "")
            //{
            //    MessageBox.Show("Cannot filter while a seach is in progress. Clear the search before choosing filter options", "Filter invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
            //    chkFollowup.IsChecked = false;
            //    return;
            //}

            chkAttended.IsChecked = false;
            chkResponded.IsChecked = false;
            m_isFollowupChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");
            string query = "";
            m_NonChecked = false;

            if (m_filterByDate && m_dateIsValid)
            {
                     query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'" :

                      "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'" +
                      "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
                


            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Follow-Up'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Follow-Up' " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }
            UpdateDataGrid(query);

        }

        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
            //generate list of all attended attendees

            //cannot filter while a text search already in progress
            //if (txtSearch.Text != "")
            //{
            //    MessageBox.Show("Cannot filter while a seach is in progress. Clear the search before choosing filter options", "Filter invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
            //    chkAttended.IsChecked = false;
            //    return;
            //}
            string query = "";
            chkResponded.IsChecked = false;
            chkFollowup.IsChecked = false;
            m_NonChecked = false;
            m_isAttendedChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");



            if (m_filterByDate && m_dateIsValid)
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "' " +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";






            }
            else
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended' " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

           UpdateDataGrid(query);

        }



        //private void DisplayResultInNewTable(DataRow[] foundRows)
        //{
        //    DataTable ResultTable = new DataTable("ResultsTable");

        //    ResultTable.Columns.Add(new DataColumn("AttendeeId"));
        //    ResultTable.Columns.Add(new DataColumn("FirstName"));
        //    ResultTable.Columns.Add(new DataColumn("LastName"));
        //    ResultTable.Columns.Add(new DataColumn("Date"));
        //    ResultTable.Columns.Add(new DataColumn("Attended", typeof(bool) ));

        //    for (int i = 0; i < foundRows.Length; i++)
        //    {
        //        ResultTable.ImportRow(foundRows[i]);
        //    }
        //    dataGrid.DataContext = ResultTable;
        //    dataGrid.Columns[0].Visibility = Visibility.Hidden;
        //}

        //private void UpdateAttendeeListTableWithModifiedRows(DataTable table)
        //{

        //    DataTable dt = m_DataSet.Tables["AttendeeListTable"];
        //    string AttendeeId;
        //    int Idx= 0 , rowIdx = 0;
        //    string[] aryAttendeeIdsModified = new string[m_DataSet.Tables["AttendeeListTable"].Rows.Count]; 

        //    //populate lstUpdateIdx with attendeeId that need to be updated
        //    foreach (DataRow dr in table.Rows)
        //    {
        //        AttendeeId = dr["AttendeeId"].ToString() ;
        //        aryAttendeeIdsModified[Idx] = AttendeeId;
        //        Idx++;
        //    }

        //    Idx = 0;
        //    //update AttendeeListTable's AttendeeIds
        //    foreach (DataRow drAttendeeTable in m_DataSet.Tables["AttendeeListTable"].Rows)
        //    {

        //        if (drAttendeeTable.ItemArray[0].ToString() == aryAttendeeIdsModified[Idx])
        //        {
        //            m_DataSet.Tables["AttendeeListTable"].Rows[rowIdx]["Attended"] = true;
        //            Idx++;
        //        }
        //        rowIdx++;
        //    }

        //    m_DataSet.Tables["AttendeeListTable"].AcceptChanges();


        //}
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if in followUp view, use query database else if in model list view filter table
            string query = "";
            string date = m_DateSelected.ToString("MM-dd-yyyy");


            if (txtSearch.Text == "")
            {
                //if (m_modeIsInListView && m_filterByDate == false && m_isAttendedChecked == false && m_isFollowupChecked == false && m_isRespondedChecked == false)
                //{
                //    m_NonChecked = true;

                //    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = "[First Name] LIKE '*'";
                //   // dataGrid.Columns[0].Visibility = Visibility.Hidden;
                //    return;
                //}


                if (m_modeIsInListView)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    if (m_DataSet.HasChanges())
                    {
                        m_DataSet.Tables["AttendeeListTable"].AcceptChanges();

                    }


                    // filter all records which is which is the Default Table
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = "FirstLastName LIKE '*'";
                    //dataGrid.Columns[0].Visibility = Visibility.Hidden;
                    return;
                }
                else if (m_modelIsInFollowUpView)
                {
                    query = (m_filterByDate && m_dateIsValid) ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                            "WHERE Attendance_Info.Date='" + date + "'" :

                            "ShowDefaultTable";


                    if (m_isAttendedChecked)
                    {
                        query = (m_filterByDate && m_dateIsValid) ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'" :

                        "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status = 'Attended'";

                    }
                    else if (m_isFollowupChecked)
                    {
                        query = (m_filterByDate && m_dateIsValid) ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'" :

                        "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status = 'Follow-Up'";

                    }
                    else if (m_isRespondedChecked)
                    {
                        query = (m_filterByDate && m_dateIsValid) ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'" :

                        "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status='Responded'";

                    }
                }
                    
 //----------------------Textbox search has text-----------------------------------------------------------------------------------
            }
            else  
            {

                if (m_modeIsInListView)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    if (m_DataSet.HasChanges())
                    {

                        m_DataSet.Tables["AttendeeListTable"].AcceptChanges();
                    }
                    //Do normal row filtering if none of the above conditions are true
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                    return;
                }
                else if (m_modelIsInFollowUpView)
                {
                    if (m_filterByDate && m_dateIsValid)
                    {
                        query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                            "WHERE Attendance_Info.Date='" + date + "' " +
                            "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
                        
                       
                    }
                    else
                    {
                        //if no filter options do a regular search
                        query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                            "WHERE Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                    }

                    if (m_isAttendedChecked)
                    {
                        query = (m_filterByDate && m_dateIsValid) ?
                            "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                            "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "' " +
                            "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'" :

                            "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                            "WHERE Attendance_Info.Status = 'Attended' " +
                            "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";


                    }
                    else if (m_isFollowupChecked)
                    {
                        query = (m_filterByDate && m_dateIsValid) ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "' " +
                        "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'" :

                        "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status = 'Follow-Up' " +
                        "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                    }
                    else if (m_isRespondedChecked)
                    {
                        query = (m_filterByDate && m_dateIsValid) ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "' " +
                        "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'" :

                        "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                        "WHERE Attendance_Info.Status = 'Responded' " +
                        "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                    }

              
                }
               
              

            }    
            
            // all conditions end here except when a return condition is called above
           UpdateDataGrid(query);

        }

        private void UpdateDataGrid(string query)
        {

            if (query == "ShowDefaultTable")
            {
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                dataGrid.Columns[0].Visibility = Visibility.Hidden;
                dataGrid.Columns[1].Visibility = Visibility.Hidden;
            }
            else
            {
                SqlDataAdapter da = new SqlDataAdapter(query, m_mySqlConnection);
                DataTable dt = new DataTable();


                da.Fill(dt);


                //Swap the columns back because the queries have these columns returned swapped
                dt.Columns["Date"].SetOrdinal(2);
                dt.Columns["Status"].SetOrdinal(3);

                dt.Columns[0].ColumnName = "First Name";
                dt.Columns[1].ColumnName = "Last Name";



                dataGrid.DataContext = dt;
            }
            

        }

        //private void ColorGridText(DataGridCell celltxt)
        //{
        //    Regex regExp = new Regex($@"{txtSearch.Text}");

        //    foreach (Match match in regExp.Matches(celltxt.Item.ToString()))
        //    {
        //        celltxt.Item.ToString().Substring(match.Index, match.Length);
        //        celltxt.Item.ToString().Substring = Color.Blue;
        //    }
        //}
        private void chkAttended_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_NonChecked = true;
            m_isAttendedChecked = false;

            if (m_filterByDate == false && m_isAttendedChecked == false && m_isFollowupChecked == false && m_isRespondedChecked == false)
            {
                m_NonChecked = true;
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                dataGrid.Columns[0].Visibility = Visibility.Hidden;
                dataGrid.Columns[1].Visibility = Visibility.Hidden;
                return;
            }
            string date = m_DateSelected.ToString("MM-dd-yyyy");
            if (m_filterByDate)
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "' " +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";






            }
            else
            {
                query = (txtSearch.Text == "") ? "ShowDefaultTable" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            UpdateDataGrid(query);

        }

        private void chkFollowup_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_isFollowupChecked = false;
            m_NonChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (m_filterByDate == false && m_isAttendedChecked == false && m_isFollowupChecked == false && m_isRespondedChecked == false)
            {
 
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                dataGrid.Columns[0].Visibility = Visibility.Hidden;
                dataGrid.Columns[1].Visibility = Visibility.Hidden;
                return;
            }

            if (m_filterByDate)
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "' " +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";






            }
            else
            {
                query = (txtSearch.Text == "") ? "ShowDefaultTable" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            UpdateDataGrid(query);

        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_isRespondedChecked = false;
            m_NonChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (m_filterByDate == false && m_isAttendedChecked == false && m_isFollowupChecked == false && m_isRespondedChecked == false)
            {
               
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                return;
            }

            if (m_filterByDate)
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "' " +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";






            }
            else
            {
                query = (txtSearch.Text == "") ? "ShowDefaultTable" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            UpdateDataGrid(query);

        }


        private void DateCalendar_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;

            string query = "";

            DateTime datec = calender.SelectedDate.Value;
            m_DateSelected = datec;

            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (datec.DayOfWeek == DayOfWeek.Sunday)
            {

                cmbDate.Text = date;
                m_dateIsValid = true;
            }

            else
                m_dateIsValid = false;


            if (m_modeIsInListView)
            {
                UpdateAttendeeListTableWithDateFilter();
                dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];

                foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                {
                    if (dr.ItemArray[4].ToString() == "True")
                    {
                        btnApplyChanges.IsEnabled = true;
                        break;
                    }
                }
                m_DataSet.AcceptChanges();
                return;


            }
            else if (m_modelIsInFollowUpView)
            {
                query = (txtSearch.Text == "") ?
                "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Date='" + date + "'" :

                "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Date='" + date + "' " +
                "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                if (m_isAttendedChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'" :

                    "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "' " +
                    "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else if (m_isFollowupChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'" :

                    "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "' " +
                    "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else if (m_isRespondedChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'" :

                    "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "' " +
                    "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
            }
            UpdateDataGrid(query);
            cmbDate.Text = date;
        }









        private void DateCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            var calendar = sender as Calendar;
            Add_Blackout_Dates(ref calendar);

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
        private void DateCalendar_Loaded(object sender, RoutedEventArgs e)
        {

            var calendar = sender as Calendar;
            //DateTime date = calendar.DisplayDate;
            //m_DateSelected = date;


            //if (date.DayOfWeek == DayOfWeek.Sunday)
            //{
            //    m_dateIsValid = true;
            //    cmbDate.Text = date.ToString("MM-dd-yyyy");
            //}
            //else
            //{
                m_dateIsValid = false;
                cmbDate.Text = "Select date";
         //   }

            Add_Blackout_Dates(ref calendar);

        }

        private void cb_Checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Checked!");
        }

        private void cb_Unchecked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("UnChecked!");
        }
        private void btnAttendeeList_Click(object sender, RoutedEventArgs e)
        {
            // save current data table
            m_modeIsInListView = true;
            m_modelIsInFollowUpView = false;
            m_tempFollowUpModeTable = dataGrid.DataContext as DataTable;

            dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];
           

            UpdateAttendeeListTableWithDateFilter();

            m_DataSet.AcceptChanges();

            foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            {
                if (dr.ItemArray[4].ToString() == "True")
                {
                    btnApplyChanges.IsEnabled = true;
                    break;
                }
            }

            if (m_DataSet.HasChanges())
                btnApplyChanges.IsEnabled = true;


            

            btnFollowUp.IsChecked = false;
            btnAttendeeList.IsChecked = true;

     
            //Uncheck_All_Filters_Except_Date();
            Disable_Filters();






            dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
            dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
        }

        private void Disable_Filters()
        {
            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;
        }
        private void Uncheck_All_Filters_Except_Date()
        {
            m_modeIsInListView = true;
            chkAttended.IsChecked = false;

            chkFollowup.IsChecked = false;
            chkResponded.IsChecked = false;

        }

        private void btnApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            
            if (m_filterByDate && m_dateIsValid && m_modeIsInListView)
            {
                // add all attendee status and date to database
                btnApplyChanges.IsEnabled = false;
                string date = m_DateSelected.ToString("MM-dd-yyyy");
             
                // save data grid edits to dataset
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid.EndInit();
               
                //check if there is a record already with the same date
                var queryDateAlreadyExist = from AttendeeListRec in m_db.Attendance_Info
                                            where AttendeeListRec.Date == date
                                            select AttendeeListRec;

                if (queryDateAlreadyExist.Any() )
                {

                    MessageBoxResult mbRecExist = MessageBox.Show("A Record with the same date exist. Please select a unique date.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }


                foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                {

                    if (dr.RowState == DataRowState.Added)
                    {
                        
                        // if attendee already exist then exit
                        string firstname = dr["First Name"].ToString();
                        string lastname = dr["Last Name"].ToString();

                        var queryAtt = from AttRec in m_db.Attendance_Info
                                       where AttRec.Attendee.FirstName == firstname && AttRec.Attendee.LastName == lastname
                                       select AttRec;
                        if (queryAtt.Any())
                        {
                            MessageBoxResult mbAttExist = MessageBox.Show("A Record with the same name already exist. Please select a unique name.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                            return;
                        }
                        else
                        {
                            // Add a new Attendee to the database
                            Attendee newAttendeeRec = new Attendee();
                            newAttendeeRec.FirstName = dr["First Name"].ToString();
                            newAttendeeRec.LastName = dr["Last Name"].ToString();
                            newAttendeeRec.HasThreeConsequitiveFollowUps = 0;

                            newAttendeeRec.AttendanceList[0].AttendeeId = int.Parse(dr["AttendeeId"].ToString());

                            newAttendeeRec.AttendanceList[0].Date = dr["Date"].ToString();
                            newAttendeeRec.AttendanceList[0].Last_Attended = dr["Date"].ToString();

                            if (dr.ItemArray[4].ToString() == "True")
                                newAttendeeRec.AttendanceList[0].Status = "Attended";
                            else
                                newAttendeeRec.AttendanceList[0].Status = "Not Attended";

                            m_db.Attendees.Add(newAttendeeRec);
                        }
                        

                    }
                    // user checked the attended box next to an existing attendee already in the database
                    else if (dr.RowState == DataRowState.Modified)
                    {
                        if (dr.ItemArray[4].ToString() == "True")
                        {
                     
                            Attendance_Info newRecord = new Attendance_Info { };

                            newRecord.AttendeeId = int.Parse(dr["AttendeeId"].ToString());

                            newRecord.Date = dr["Date"].ToString();

                            var queryAttDateLastAttended = (from AttInfo in m_db.Attendance_Info
                                                   where AttInfo.AttendeeId == newRecord.AttendeeId
                                                   orderby AttInfo.Last_Attended ascending
                                                   select AttInfo).ToArray().LastOrDefault();

                            if (queryAttDateLastAttended != null)
                            {
                                newRecord.Last_Attended = queryAttDateLastAttended.Last_Attended;
                            }
                            else
                            {
                                newRecord.Last_Attended = newRecord.Date;
                            }

                            newRecord.Status = "Attended";

                            m_db.Attendance_Info.Add(newRecord);
                        }
                    }
                    
                }

                m_db.SaveChanges();
                MessageBox.Show("Changes Saved!");




            }
            else
            {
                MessageBoxResult mb = MessageBox.Show("Date not valid, select correct date", "Date Invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
            }


        }





        private void btnFollowUp_Click(object sender, RoutedEventArgs e)
        {

            // btnFollowUp.IsChecked = true;
            m_modelIsInFollowUpView = true;

            m_modeIsInListView = false;
            btnApplyChanges.IsEnabled = false;
            btnAttendeeList.IsChecked = false;

            Enable_Filters();
            // commit datagrid edits
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (txtSearch.Text == "")
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
            else
                dataGrid.DataContext = m_tempFollowUpModeTable;

            if (m_tempFollowUpModeTable.Columns.Contains("AttendeeId") )
            {
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
            }
            

        }

        private void UpdateAttendeeListTableWithDateFilter()
        {

            string date;

            if (m_filterByDate && m_dateIsValid)
                date = m_DateSelected.ToString("MM-dd-yyyy");
            else
                date = "Date Not Valid";

            foreach (DataRow drAttendeeListTable in m_DataSet.Tables["AttendeeListTable"].Rows)
            {
                if (drAttendeeListTable["Date"].ToString() == date)
                {
                    break;
                }
                else
                {
                    drAttendeeListTable["Date"] = date;
                }

            }
        }


        private void Enable_Filters()
        {
            chkAttended.IsEnabled = true;
            chkFollowup.IsEnabled = true;
            chkResponded.IsEnabled = true;
          

        }


        private void dataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            IList<DataGridCellInfo> CellV = grid.SelectedCells;


            DataRowView RowView = (DataRowView)CellV[0].Item;

            m_FirstName = RowView.Row[1].ToString();
            m_LastName = RowView.Row[2].ToString();

            WndAttendeeInfo AttendeeInfoWindow = new WndAttendeeInfo(m_FirstName, m_LastName, m_mySqlConnection);
            AttendeeInfoWindow.Show();
        }

        private void chkDateFiler_Checked(object sender, RoutedEventArgs e)
        {

            //if (txtSearch.Text != "")
            //{
            //    MessageBox.Show("Cannot filter while a seach is in progress. Clear the search before choosing filter options", "Filter invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
            //    chkDateFilter.IsChecked = false;
            //    return;
            //}

            m_filterByDate = true;
            cmbDate.IsEnabled = true;
            string query = "";

            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (m_filterByDate && m_modeIsInListView)
            {
                if (m_dateIsValid)
                {
                    UpdateAttendeeListTableWithDateFilter();
                    return;
                }
                else
                {
                    return; // Do nothing, user will enter or select a date from dropdown box
                }

            }
            else if (m_filterByDate && m_modelIsInFollowUpView)
            {
                if (m_dateIsValid)
                {
                    query = (txtSearch.Text == "") ?
                      "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Date='" + date + "'" :

                      "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Date='" + date + "' " +
                      "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else
                {
                    return;// Do nothing, user will enter or select a date from dropdown box
                }


                if (m_isAttendedChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                          "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'" :

                          "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                          "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "' " +
                          "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else if (m_isFollowupChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'" :

                      "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "' " +
                      "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else if (m_isRespondedChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'" :

                      "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "' " +
                      "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }

                UpdateDataGrid(query);
            }
        }




        private void chkDateFiler_Unchecked(object sender, RoutedEventArgs e)
        {
            m_filterByDate = false;
            
            cmbDate.IsEnabled = false;
            string query = "";

            if (m_modeIsInListView)
            {
                UpdateAttendeeListTableWithDateFilter();
                return;
            }
            else if (m_modelIsInFollowUpView)
            {

                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                return;
            }

           
            if (m_isAttendedChecked)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Attended'" :

                      "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Attended' " +
                      "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

            }
            else if (m_isFollowupChecked)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                  "FROM Attendees " +
                  "INNER JOIN Attendance_Info " +
                  "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                  "WHERE Attendance_Info.Status='Follow-Up'" :

                  "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                  "FROM Attendees " +
                  "INNER JOIN Attendance_Info " +
                  "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                  "WHERE Attendance_Info.Status='Follow-Up' " +
                  "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

            }
            else if (m_isRespondedChecked)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                  "FROM Attendees " +
                  "INNER JOIN Attendance_Info " +
                  "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                  "WHERE Attendance_Info.Status='Responded'" :

                  "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                  "FROM Attendees " +
                  "INNER JOIN Attendance_Info " +
                  "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                  "WHERE Attendance_Info.Status='Responded' " +
                  "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

            }
            else
            {
                //Date filter unchecked
                query = (txtSearch.Text == "") ? "ShowDefaultTable" :

                     "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                     "FROM Attendees " +
                     "INNER JOIN Attendance_Info " +
                     "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                     "WHERE Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }
            UpdateDataGrid(query);


        }

        private void Window_Closed(object sender, EventArgs e)
        {
            m_mySqlConnection.Close();
        }


        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (m_modeIsInListView)
            //{

              
            //    foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            //    {
                 
            //        dr.BeginEdit();
            //        if (dr.HasVersion(DataRowVersion.Proposed) )
            //        {
            //            btnApplyChanges.IsEnabled = true;
            //            Console.WriteLine($"Original value = {dr["Attended", DataRowVersion.Original]}");
            //            Console.WriteLine($"Proposed value = {dr["Attended", DataRowVersion.Proposed]}");

            //            if (dr["Attended",DataRowVersion.Original].ToString() == dr["Attended", DataRowVersion.Proposed].ToString() )
            //            {
            //                btnApplyChanges.IsEnabled = false;
            //            }
                    
            //            break;
            //        }

                    


            //    }


            //}


        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            btnFollowUp.IsChecked = true;

           
            btnApplyChanges.IsEnabled = false;
            btnAttendeeList.IsChecked = false;
            chkDateFilter.IsChecked = false;
            m_dateIsValid = false;
            cmbDate.Text = "Select or type date";

            cmbDate.IsEnabled = false;


            dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
            dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName


        }

        private void btnChart_Click(object sender, RoutedEventArgs e)
        {
            ChartWindow wndCharts = new ChartWindow(m_db);
            wndCharts.Show();

        }

  

        private void RibbonApplicationMenuItem_Click_Exit(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void cmbDate_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {


                Regex pattern = new Regex(@"^[0-9]{2}-[0-9]{2}-[0-9]{4}");

                if (pattern.IsMatch(cmbDate.Text))
                {
                    string text = pattern.Match(cmbDate.Text).ToString();
                    string[] splitstr = text.Split('-');
                    string month = splitstr[0];
                    string day = splitstr[1];
                    string year = splitstr[2];


                    try
                    {
                        m_DateSelected = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                        if (m_DateSelected.DayOfWeek == DayOfWeek.Sunday)
                        {
                            m_dateIsValid = true;
                            Update_Status();
                        }
                        else
                        {
                            m_dateIsValid = false;
                            cmbDate.Text = "Date is invalid";


                        }
                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show("Invalid date.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                        Console.WriteLine($"{ex}");

                    }
                }
                else
                {
                    MessageBox.Show("Date in wrong format.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Update_Status()
        {


            string query = "";
            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (m_modeIsInListView)
            {
                UpdateAttendeeListTableWithDateFilter();


                dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];
                m_DataSet.AcceptChanges();

                foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                {
                    if (dr.ItemArray[4].ToString() == "True")
                    {
                        btnApplyChanges.IsEnabled = true;
                        break;
                    }
                }
                return;


            }
            else if (m_modelIsInFollowUpView)
            {
                query = (txtSearch.Text == "") ?
                "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Date='" + date + "'" :

                "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Date='" + date + "' " +
                "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                if (m_isAttendedChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'" :

                    "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "' " +
                    "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else if (m_isFollowupChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'" :

                    "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "' " +
                    "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else if (m_isRespondedChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'" :

                    "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "' " +
                    "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
            }
            UpdateDataGrid(query);
        }
        
        private void cmbDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (cmbDate.Text != "")
                cmbDate.Text = "";

         

        }

        private void cmbDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbDate.Text == "")
                cmbDate.Text = "Select or type date";
        }

        private void cmbDate_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (cmbDate.Text != "")
                cmbDate.Text = "";

         
        }

        private void dataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

    } // end MainWindow
} 





