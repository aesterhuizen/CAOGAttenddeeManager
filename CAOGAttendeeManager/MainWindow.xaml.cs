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
            Cursor = Cursors.Wait;
            InitializeComponent();

            this.Closing += new System.ComponentModel.CancelEventHandler(ClosingApp);

            //open file with database credentials


            var executingPath = Directory.GetCurrentDirectory();





            try
            {


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

                    MessageBox.Show("Cannot connect to Database, credential file does not exist!", "File does not exist.", MessageBoxButton.OK, MessageBoxImage.Error);
                    m_NoCredFile = true;
                    this.Close();
                }
                m_db = new ModelDb(m_constr);
                m_mySqlConnection = new SqlConnection(m_constr);

                 //GenerateDBFollowUps();
                 InitDataSet();
                 //CreateDatabase_FromXLSX();

                //clear_attendee_backlog();

                  Display_DefaultTable_in_Grid();


            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nException occurred when performing database initialization {ex}!\n", ex);

            }


            Cursor = Cursors.Arrow;










        }




        private ModelDb m_db;
        private bool m_NoCredFile = false;
        private DataSet m_DataSet = new DataSet();
        private bool m_DataSetChangesSaved = false;
        private SqlConnection m_mySqlConnection = null;
        private string m_constr = "";

       
        private DateTime m_DateSelected;
        private bool m_isAttendedChecked = false;
        private bool m_isFollowupChecked = false;
        private bool m_isRespondedChecked = false;
        private bool m_dateIsValid = false;
        private bool m_modeIsInListView = false;
        private bool m_modelIsInFollowUpView = true;
        private bool m_filterByDate = false;
        private int m_NewAttendeeId = 0;


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




        private void GenerateDBFollowUps()
        {
            //problem
            // Generate Follow-Ups for each Attendee that missed 28 days of church

            //Solution
            //1)look and last date attended for each AttendeeId and see if attendee attended 4 weeks (28 days) in the past from now
            //2)if attendee did not attend 4 weeks in the past, generate a new database entry with status follow-up for corresponding AttendeeId
            //3) if attendee has 3 follow-ups in a row, flag attendee and don't consider him for another follow-up entry. 


            bool bHasChanges = false;

            // get current time
            DateTime curdate = DateTime.Now;
            TimeSpan timespanSinceDate;

            ////get previous
            //while (curdate.DayOfWeek != DayOfWeek.Sunday)
            //{
            //    curdate = curdate.AddDays(-1);
            //}






            var queryAttendees = from AttendeeRec in m_db.Attendees
                                 select AttendeeRec;


            foreach (var AttendeeRec in queryAttendees)
            {

                if (AttendeeRec.HasThreeConsequitiveFollowUps != 1)
                {


                    var lstDateRecs = (from DateRec in AttendeeRec.AttendanceList
                                       orderby DateRec.Date ascending
                                       select DateRec).ToList();


                    timespanSinceDate = curdate - lstDateRecs[lstDateRecs.Count - 1].Date;

                    if ((lstDateRecs[lstDateRecs.Count - 1].Status == "Follow-Up" || lstDateRecs[lstDateRecs.Count - 1].Status == "Attended") && timespanSinceDate.Minutes <= 28)
                    {

                        // do nothing
                        //Attendee already have a followUp sent so do not generate another followup unil 28 days has
                        //lapsed since the last followUp        


                    }
                    else
                    {
                        //generate follow-up
                        Attendance_Info newfollowUpRecord = new Attendance_Info { };
                        newfollowUpRecord.AttendeeId = AttendeeRec.AttendeeId;
                        newfollowUpRecord.Date = curdate;
                        newfollowUpRecord.Last_Attended = lstDateRecs[lstDateRecs.Count - 1].Last_Attended;
                        newfollowUpRecord.Status = "Follow-Up";

                        m_db.Attendance_Info.Add(newfollowUpRecord);
                        bHasChanges = true;

                    }


                    //// flag attendee if havinging 3 consequitive follow-ups


                    for (int idx = 0; idx < lstDateRecs.Count() - 1; idx++)
                    {
                        if (lstDateRecs[idx].Status == "Follow-Up" &&
                            lstDateRecs[idx + 1].Status == "Follow-Up" &&
                            lstDateRecs[idx + 2].Status == "Follow-Up")
                        {
                            AttendeeRec.HasThreeConsequitiveFollowUps = 1;

                        }
                        else
                            AttendeeRec.HasThreeConsequitiveFollowUps = 0;

                    }

                } // end Has Three follow ups



            } //end foreach

            if (bHasChanges)
            {
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
                    int att_infoID = 0, attID = 0, attLst_Idx = 0, triad_counter = 0, HasThreeFollowUps = 0;

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
                                    DateTime date = new DateTime(int.Parse(year), int.Parse(md[0]), int.Parse(md[1]));
                                    string[] arylstAttDate = oleDataReader[2].ToString().Split('.');
                                    string lstyear = "20" + arylstAttDate[2];
                                    DateTime lstAttendedDate = new DateTime(int.Parse(lstyear), int.Parse(arylstAttDate[0]), int.Parse(arylstAttDate[1]));

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
                                            HasThreeFollowUps = 0;
                                            break;

                                        }
                                    case 2: //FollowUp
                                        {
                                            if (oleDataReader[col_index + 1].ToString() == "1")
                                            {
                                                churchAttendee.AttendanceList[attLst_Idx].Status = "Follow-Up";
                                                HasThreeFollowUps++;
                                                attLst_Idx++;

                                                if (HasThreeFollowUps == 3)
                                                {
                                                    churchAttendee.HasThreeConsequitiveFollowUps = 1;
                                                }
                                            }
                                            break;
                                        }
                                    case 3: //Responded
                                        {
                                            if (oleDataReader[col_index + 2].ToString() == "1")
                                            {
                                                churchAttendee.AttendanceList[attLst_Idx].Status = "Responded";
                                                //churchAttendee.AttendanceList.Add(Attendee_Status);
                                                HasThreeFollowUps = 0;
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

        private void Display_AttendeeListTable_in_Grid()
        {

            dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];
            (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";

            dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
            dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName

        } // end  private void Display_Database_in_Grid()

        //----Display Data in Grid------------------------------------------------------------------------------------------------------------------------   
        private void Display_DefaultTable_in_Grid()
        {

            if (m_DataSet.Tables.Contains("DefaultTable"))
            {
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];


                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName

                (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
            }




        } // end  private void Display_Database_in_Grid()


        private void InitDataSet()
        {

            //--------------------- Make DEFAULT TABLE---------------------------------------------------------------------------

            DataTable Default_Data_Table = new DataTable("DefaultTable");


            try
            {

                var queryAttendees = from AttendeeRec in m_db.Attendees
                                     select AttendeeRec;


                Default_Data_Table.Columns.Add(new DataColumn("AttendeeId"));
                Default_Data_Table.Columns.Add(new DataColumn("3follow-Ups", typeof(int) ));
                Default_Data_Table.Columns.Add(new DataColumn("FirstLastName"));
                Default_Data_Table.Columns.Add(new DataColumn("First Name"));
                Default_Data_Table.Columns.Add(new DataColumn("Last Name"));
                Default_Data_Table.Columns.Add(new DataColumn("Date Last Attended"));
                Default_Data_Table.Columns.Add(new DataColumn("Status"));

                Default_Data_Table.Columns["AttendeeId"].Unique = true;

                DataColumn[] primaryKeyCol = new DataColumn[1];
                primaryKeyCol[0] = Default_Data_Table.Columns[0];
                Default_Data_Table.PrimaryKey = primaryKeyCol;

                DateTime dateLA;
                string statusLA = "";

                foreach (var AttendeeRec in queryAttendees)
                {

                    var queryLastDateAttended = (from DateRec in AttendeeRec.AttendanceList
                                                 orderby DateRec.Last_Attended ascending
                                                 select DateRec).ToList().LastOrDefault();


                    dateLA = queryLastDateAttended.Last_Attended;
                    statusLA = queryLastDateAttended.Status;



                    m_NewAttendeeId = AttendeeRec.AttendeeId;
                    DataRow dr = Default_Data_Table.NewRow();

                    dr["AttendeeId"] = AttendeeRec.AttendeeId;
                    dr["3follow-Ups"] = AttendeeRec.HasThreeConsequitiveFollowUps;
                    dr["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;

                    dr["First Name"] = AttendeeRec.FirstName;
                    dr["Last Name"] = AttendeeRec.LastName;


                    dr["Date Last Attended"] = dateLA.ToString("MM-dd-yyyy");
                    dr["Status"] = statusLA;


                    Default_Data_Table.Rows.Add(dr);
                }


                Default_Data_Table.AcceptChanges();
                m_DataSet.Tables.Add(Default_Data_Table);
                m_NewAttendeeId += 1;

                //-------------------------------Make AttendeeList Table-------------------------------------------------------------------

                DataTable AttendeeListTable = new DataTable("AttendeeListTable");
                //AttendeeListTable.RowChanging += new DataRowChangeEventHandler(RowChanged);

                string date;



                if (m_filterByDate && m_dateIsValid)
                    date = m_DateSelected.ToString("MM-dd-yyyy");
                else
                    date = "Date Not Valid";


                AttendeeListTable.Columns.Add(new DataColumn("AttendeeId"));
                AttendeeListTable.Columns.Add(new DataColumn("FirstLastName"));
                AttendeeListTable.Columns.Add(new DataColumn("First Name"));
                AttendeeListTable.Columns.Add(new DataColumn("Last Name"));
                AttendeeListTable.Columns.Add(new DataColumn("Date"));
                AttendeeListTable.Columns.Add(new DataColumn("Attended", typeof(bool)));



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


            if (m_filterByDate && m_dateIsValid)
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'";
            }
            else
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Responded'";
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


            if (m_filterByDate && m_dateIsValid)
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attence_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'";

            }
            else
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Follow-Up'";

            }
            UpdateDataGrid(query);

        }

        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
            //generate list of all attended attendees


            string query = "";
            chkResponded.IsChecked = false;
            chkFollowup.IsChecked = false;

            m_isAttendedChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");



            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'";

            }
            else
            {

                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended'";
            }

            UpdateDataGrid(query);

        }





        private void Disable_Filters()
        {
            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;
            chkDateFilter.IsEnabled = false;
            cmbDate.IsEnabled = false;
            //m_isAllFiltersDisabled = true;

        }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if in followUp view, use query database else if in model list view filter table



            if (txtSearch.Text == "")
            {

                (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;

                //----------------------Textbox search has text-----------------------------------------------------------------------------------
            }
            else
            {

                (dataGrid.DataContext as DataTable).DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";

            }


        }

        private void UpdateDataGrid(string query)
        {
            //string date;

            //if (m_filterByDate && m_dateIsValid)
            //    date = m_DateSelected.ToString("MM-dd-yyyy");
            //else
            //    date = "Date Not Valid";

            if (query == "ShowDefaultTable")
            {
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                // (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
                dataGrid.Columns[0].Visibility = Visibility.Hidden;
                dataGrid.Columns[1].Visibility = Visibility.Hidden;
            }
            else if (query == "")
            {
                // do nothing

            }
            else
            {
                SqlDataAdapter da = new SqlDataAdapter(query, m_mySqlConnection);
                DataTable dt = new DataTable();


                da.Fill(dt);



                DataTable queryTable = new DataTable();

                queryTable.Columns.Add(new DataColumn("FirstLastName"));
                queryTable.Columns.Add(new DataColumn("First Name"));
                queryTable.Columns.Add(new DataColumn("Last Name"));
                queryTable.Columns.Add(new DataColumn("Date"));
                queryTable.Columns.Add(new DataColumn("Status"));

                
                foreach (DataRow dr in dt.Rows)
                {

                    DataRow newrow = queryTable.NewRow();

                    DateTime date = (DateTime)dr["Date"];


                    newrow["FirstLastName"] = dr["FirstName"].ToString() + " " + dr["LastName"].ToString();
                    newrow["First Name"] = dr["FirstName"];
                    newrow["Last Name"] = dr["LastName"];
                    newrow["Date"] = date.ToString("MM-dd-yyyy");
                    newrow["Status"] = dr["Status"];


                    queryTable.Rows.Add(newrow);
                }

                //Swap the columns back because the queries have these columns returned swapped

                queryTable.Columns["FirstLastName"].SetOrdinal(0);
                queryTable.Columns["Date"].SetOrdinal(3);
                queryTable.Columns["Status"].SetOrdinal(4);

                queryTable.Columns[1].ColumnName = "First Name";
                queryTable.Columns[2].ColumnName = "Last Name";



                dataGrid.DataContext = queryTable;
                dataGrid.Columns[0].Visibility = Visibility.Hidden;
                dataGrid.ColumnWidth = 100;
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

            m_isAttendedChecked = false;


            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "'";


            }
            else
            {
                query = "ShowDefaultTable";

            }

            UpdateDataGrid(query);

        }

        private void chkFollowup_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_isFollowupChecked = false;

            string date = m_DateSelected.ToString("MM-dd-yyyy");



            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "'";

            }
            else
            {
                query = "ShowDefaultTable";

            }

            UpdateDataGrid(query);

        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_isRespondedChecked = false;

            string date = m_DateSelected.ToString("MM-dd-yyyy");


            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Date='" + date + "'";


            }
            else
            {
                query = "ShowDefaultTable";

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




            query =
            "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
            "FROM Attendees " +
            "INNER JOIN Attendance_Info " +
            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
            "WHERE Attendance_Info.Date='" + date + "'";

            if (m_isAttendedChecked)
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'";

            }
            else if (m_isFollowupChecked)
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'";

            }
            else if (m_isRespondedChecked)
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'";
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

            m_dateIsValid = false;
            cmbDate.Text = "Select or type date";


            Add_Blackout_Dates(ref calendar);

        }


        //private void btnAttendeeList_Click(object sender, RoutedEventArgs e)
        //{
        //   if (m_modelIsInFollowUpView)
        //    {
        //        // return datagrid's datacontext to show all records
        //        (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;
        //        //(dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";



        //        m_modeIsInListView = true;
        //        m_modelIsInFollowUpView = false;

        //        btnFollowUp.IsChecked = false;
        //        btnAttendeeList.IsChecked = true;





        //        if (txtSearch.Text != "")
        //            txtSearch.Text = "";

        //        //UpdateAttendeeListTableWithDateFilter();
        //        cmbDate.Text = "Select or type date";
        //        m_dateIsValid = false;
        //        // m_DataSet.AcceptChanges();



        //        //if (m_DataSet.HasChanges())
        //        //    btnApplyChanges.IsEnabled = true;
        //        //else
        //        //    btnApplyChanges.IsEnabled = false; FIXME

        //        Uncheck_All_Filters_Except_Date();
        //        Disable_All_Filters_Except_Date();


        //        dataGrid.CanUserAddRows = true;
        //        dataGrid.CanUserDeleteRows = false; 
        //        dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];
        //        (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";

        //        dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
        //        dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName


        //    }

        //}


        private void Uncheck_All_Filters_Except_Date()
        {



            chkFollowup.IsChecked = false;
            chkResponded.IsChecked = false;
            chkAttended.IsChecked = false;

        }

        private void Disable_All_Filters_Except_Date()
        {
            chkAttended.IsEnabled = false;
            chkResponded.IsEnabled = false;
            chkFollowup.IsEnabled = false;

        }
        private void btnApplyChanges_Click(object sender, RoutedEventArgs e)
        {


            Cursor = Cursors.Wait;

            bool haschanges = false;

            if (m_dateIsValid)
            {

                // add all attendee status and date to database


                // save data grid edits to dataset
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);



                foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                {

                    if (dr.RowState == DataRowState.Added)
                    {

                        // if attendee already exist then exit
                        //dr["FirstLastName"] = dr["First Name"].ToString() + dr["Last Name"].ToString();
                        string firstname = dr["First Name"].ToString();
                        string lastname = dr["Last Name"].ToString();

                        var queryAtt = from AttRec in m_db.Attendance_Info
                                       where AttRec.Attendee.FirstName == firstname && AttRec.Attendee.LastName == lastname
                                       select AttRec;
                        if (queryAtt.Any())
                        {
                            MessageBox.Show("A Record with the same name already exist. Please select a unique name.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                            Cursor = Cursors.Arrow;
                            return;
                        }
                        else
                        {
                            // Add a new Attendee to the database
                            Attendee newAttendeeRec = new Attendee();
                            Attendance_Info newAttInfoRec = new Attendance_Info();



                            newAttendeeRec.AttendeeId = m_NewAttendeeId;
                            newAttendeeRec.FirstName = dr["First Name"].ToString().Trim();
                            newAttendeeRec.LastName = dr["Last Name"].ToString().Trim();
                            newAttendeeRec.HasThreeConsequitiveFollowUps = 0;



                            newAttInfoRec.AttendeeId = m_NewAttendeeId;
                            newAttInfoRec.Date = m_DateSelected;
                            newAttInfoRec.Last_Attended = m_DateSelected;

                            if (dr.ItemArray[5].ToString() == "True")
                                newAttInfoRec.Status = "Attended";


                            newAttendeeRec.AttendanceList.Add(newAttInfoRec);

                            m_db.Attendees.Add(newAttendeeRec);
                            m_db.Attendance_Info.Add(newAttInfoRec);
                            m_NewAttendeeId += 1;
                            haschanges = true;


                        }


                    }
                    // user checked the attended box next to an existing attendee already in the database
                    else if (dr.RowState == DataRowState.Modified)
                    {
                        if (dr.ItemArray[5].ToString() == "True")
                        {

                            Attendance_Info newRecord = new Attendance_Info { };

                            newRecord.AttendeeId = int.Parse(dr["AttendeeId"].ToString());
                            newRecord.Date = m_DateSelected;
                            newRecord.Last_Attended = m_DateSelected;
                            newRecord.Status = "Attended";

                            m_db.Attendance_Info.Add(newRecord);
                            haschanges = true;
                        }
                    }





                } // end foreach

            } // end if m_datValid

            else
            {
                MessageBoxResult mb = MessageBox.Show("Date not valid, select correct date", "Date Invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
            }


            if (haschanges)
            {


                m_db.SaveChanges();
                m_DataSetChangesSaved = true;
                //clear DataSet
                m_DataSet.Reset();
                InitDataSet();
                Display_AttendeeListTable_in_Grid();
                m_DataSet.AcceptChanges();



                txtSearch.Text = "";
                MessageBox.Show("Changes Saved...");


            }
            else
            {
                MessageBox.Show("No Changes to Save.");
            }






            Cursor = Cursors.Arrow;

        }


 


      

        private void btnFollowUp_Click(object sender, RoutedEventArgs e)
        {
            






        }

        private void UpdateAttendeeListTableWithDateFilter()
        {

            string date;

            if (m_dateIsValid)
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
            chkDateFilter.IsEnabled = true;
            cmbDate.IsEnabled = true;
            //m_isAllFiltersDisabled = false; 

        }

        private void Uncheck_All_Filters()
        {
            chkFollowup.IsChecked = false;
            chkResponded.IsChecked = false;
            chkAttended.IsChecked = false;
            chkDateFilter.IsChecked = false;
        }
        private void dataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            IList<DataGridCellInfo> CellV = grid.SelectedCells;

            if(CellV.Count !=0)
            {
                DataRowView RowView = (DataRowView)CellV[0].Item;
                // wout consecutive colunm first, lastname is 2,3
                //w/ consecutive column first,last name is 3,4
                string firstname = RowView.Row[3].ToString();
                string lastname = RowView.Row[4].ToString();

                WndAttendeeInfo AttendeeInfoWindow = new WndAttendeeInfo(firstname, lastname, m_mySqlConnection);
                AttendeeInfoWindow.Show();
            }
                
            
            
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
                    query =
                      "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Date='" + date + "'";
                                          
                }
                else
                {
                    return;// Do nothing, user will enter or select a date from dropdown box
                }


                if (m_isAttendedChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                          "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'";

                }
                else if (m_isFollowupChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'";
                }
                else if (m_isRespondedChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'";

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
               //do nothing
                // UpdateAttendeeListTableWithDateFilter();
               // return;
            }
            else if (m_modelIsInFollowUpView)
            {

                //dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                //dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                //dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                //return;



                if (m_isAttendedChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                          "WHERE Attendance_Info.Status='Attended'";


                }
                else if (m_isFollowupChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up'";
                    

                }
                else if (m_isRespondedChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Responded'";

                }
                else
                {
                    //Date filter unchecked
                    query = "ShowDefaultTable";
                    
                }

            }
           
           UpdateDataGrid(query);


        }

      

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           // var grid = sender as DataGrid;
           // IList<DataGridCellInfo> CellV = grid.SelectedCells;

           // DataRowView RowView = (DataRowView)CellV[0].Item;

            //if (m_modeIsInListView)
            //{
            //    bool haschange = false;

            //    foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            //    {
            //        if (dr.ItemArray[5].ToString() == "False")
            //        {
            //            haschange = false;
            //        }
            //        else
            //        {
            //            haschange = true;
            //            btnApplyChanges.IsEnabled = true;
            //            return;

            //        }

            //        if (!haschange) btnApplyChanges.IsEnabled = false;
            //    }
                //    foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                //    {

                //        dr.BeginEdit();

                //        if (dr.HasVersion(DataRowVersion.Proposed))
                //        {

                //            if (dr["Attended", DataRowVersion.Original].ToString() == "True" && dr["Attended",DataRowVersion.Proposed].ToString() == "False")
                //            {
                //            if (dr.HasVersion(DataRowVersion.Original)) { Console.WriteLine($"Original value = {dr["Attended", DataRowVersion.Original]}"); }
                //            if (dr.HasVersion(DataRowVersion.Proposed)) { Console.WriteLine($"Proposed value = {dr["Attended", DataRowVersion.Proposed]}"); }

                //            haschange = false;
                //                dr.CancelEdit();

                //            }
                //            else if (dr["Attended", DataRowVersion.Original].ToString() == "False" && dr["Attended", DataRowVersion.Proposed].ToString() == "True")
                //            {
                //                haschange = true;
                //                btnApplyChanges.IsEnabled = true;

                //                if (dr.HasVersion(DataRowVersion.Original)) { Console.WriteLine($"Original value = {dr["Attended", DataRowVersion.Original]}"); }
                //                if (dr.HasVersion(DataRowVersion.Proposed)) { Console.WriteLine($"Proposed value = {dr["Attended", DataRowVersion.Proposed]}"); }
                                
                //                m_DataSet.AcceptChanges();
                //                return;
                //            }

                    
                //        }
                    
                //      } // end foreach


              

//            }



      }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

          
                    
            if (dataGrid.Columns.Count > 1 )
            {
               // dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
               // dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
            }
           
           
           
           

        }

        private void ClosingApp(Object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_NoCredFile)
            {
                e.Cancel = false;
            }
            else
            {

                if (dataGrid.DataContext !=null)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    bool haschanges = false;
                    foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                    {
                        if (dr.ItemArray[5].ToString() == "True")
                        {
                            haschanges = true;
                            break;
                        }
                    }




                    if (haschanges && !m_DataSetChangesSaved)
                    {



                        MessageBoxResult result = MessageBox.Show("Attendee List has been modified but changes has not been saved, exit anyway?", "Save Changes...", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;

                        }
                        else
                        {
                            m_mySqlConnection.Close();

                            e.Cancel = false;

                        }
                    }
                }
                
            }
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

                //foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                //{
                //    if (dr.ItemArray[5].ToString() == "True")
                //    {
                //        btnApplyChanges.IsEnabled = true;
                //        break;
                //    }
                //}
                return;


            }
            else if (m_modelIsInFollowUpView)
            {
                query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Date='" + date + "'";

                if (m_isAttendedChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'";

                }
                else if (m_isFollowupChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'";
                }
                else if (m_isRespondedChecked)
                {
                    query = "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'";

                }
            }
                UpdateDataGrid(query);
           

        }
        
        private void cmbDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (cmbDate.Text == "Select or type date")
                cmbDate.Text = "";

         

        }

        private void cmbDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbDate.Text == "")
                cmbDate.Text = "Select or type date";
        }

      
        private void dataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine("DataContext Changed!");
        }

        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            DataGridCellInfo current_cell = grid.CurrentCell;


            //if (current_cell.Column.ToString() == "Date")
            //{
            //        // current_cell. = m_DateSelected.ToString("MM-dd-yyyy");

            //}
        }

        private void dataGrid_CurrentCellChanged(object sender, EventArgs e)
        {

           


            //if (CellV.Count != 0)
            //{


            //DataRowView RowView = (DataRowView)CellV[0].Item;

            //    // RowView.EndEdit();
            //    bool haschange = false;
            //    foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            //    {
            //        if (dr.ItemArray[5].ToString() == "True")
            //        {
            //            haschange = true;

            //            btnApplyChanges.IsEnabled = true;
            //        }
            //        else
            //        {
            //            haschange = false;



            //        }

            //        if (!haschange)
            //        {
            //            btnApplyChanges.IsEnabled = false;
            //            return;
            //        }
            //    }

            // }



        }

        private void dataGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnApplyChanges_Click(sender, e);
               
            }
        }

        private void btnNewRec_Click(object sender, RoutedEventArgs e)
        {
           
            if (m_modeIsInListView)
            {
                ////first focus the grid
                //dataGrid.Focus();
                ////then create a new cell info, with the item we wish to edit and the column number of the cell we want in edit mode
                //DataGridCell dgcell = new DataGridCell();
                //dgcell.
                //DataGridCellInfo cellInfo = new DataGridCellInfo(string, dataGrid.Columns[1]);
                ////set the cell to be the active one
                //dataGrid.CurrentCell = cellInfo;
                ////scroll the item into view
                //dataGrid.ScrollIntoView(itemToSelect);
                ////begin the edit
                //dataGrid.BeginEdit();
               // dataGrid.SelectedItem = 
               dataGrid.ScrollIntoView(dataGrid.Items[dataGrid.Items.Count - 1]); //scroll to last
                //dataGrid.UpdateLayout();
                //dataGrid.ScrollIntoView(dataGrid.SelectedItem);

            }
                
        }


        private void dataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //var grid = sender as DataGrid;
            //IList<DataGridCellInfo> CellV = grid.SelectedCells;

            //dataGrid.CommitEdit();


            //DataRowView RowView = (DataRowView)CellV[0].Item;

            //if (dataGrid.CurrentColumn == dataGrid.Columns[5])
            //{
            //    RowView.Row.EndEdit();
            //}

            //bool haschange = false;


            //foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            //{
            //    if (dr.ItemArray[5].ToString() == "True")
            //    {
            //        haschange = true;

            //        btnApplyChanges.IsEnabled = true;
            //    }
            //    else
            //    {
            //        haschange = false;



            //    }

            //    if (!haschange)
            //    {
            //        btnApplyChanges.IsEnabled = false;
            //        return;
            //    }

            //}
        }

        private void Ribbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabAttendeeList.IsSelected)
            {

                // return datagrid's datacontext to show all records
                if (dataGrid.DataContext != null)
                {
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;
                    //(dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
                }




                m_modeIsInListView = true;
                    m_modelIsInFollowUpView = false;

                  //  btnFollowUp.IsChecked = false;
                    // btnAttendeeList.IsChecked = true;





                    if (alisttxtSearch.Text != "")
                        txtSearch.Text = "";

                    //UpdateAttendeeListTableWithDateFilter();
                    alistcmbDate.Text = "Select or type date";
                    m_dateIsValid = false;
                    // m_DataSet.AcceptChanges();



                    //if (m_DataSet.HasChanges())
                    //    btnApplyChanges.IsEnabled = true;
                    //else
                    //    btnApplyChanges.IsEnabled = false; FIXME

                  

                    dataGrid.CanUserAddRows = true;
                    dataGrid.CanUserDeleteRows = false;

                if (m_DataSet.Tables.Contains("AttendeeListTable"))
                {
                    dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];
                    (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
                    dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                    dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName

                }


               
            }
            else if (tabHome.IsSelected)
            {

                if (dataGrid.DataContext != null)
                {
                    // commit datagrid edits and return DataContext to show all records
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    //reset dataContext sow all records
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;
                    (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
                }

                    // btnFollowUp.IsChecked = true;
                    m_modelIsInFollowUpView = true;
                    m_modeIsInListView = false;

                    // btnApplyChanges.IsEnabled = false; FIXME
                    //btnAttendeeList.IsChecked = false;


                    Uncheck_All_Filters();
                    Enable_Filters();


                    if (txtSearch.Text != "")
                        txtSearch.Text = "";

                    cmbDate.Text = "Select or type date";
                    cmbDate.IsEnabled = false;
                    m_dateIsValid = false;

                    dataGrid.CanUserAddRows = false;
                    dataGrid.CanUserDeleteRows = false;

                if(m_DataSet.Tables.Contains("DefaultTable"))
                {
                    dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                    (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
                    if (dataGrid.Columns.Count > 1)
                    {
                        dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                        dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                    }
                }
                    

                    

               
            }
        }

        private void alisttxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            
            if (alisttxtSearch.Text == "")
            {
                
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                
                    //if (m_DataSet.HasChanges())
                    //{
                    //    m_DataSet.Tables["AttendeeListTable"].AcceptChanges();

                    //}
                    // show all records
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;
                

                //----------------------Textbox search has text-----------------------------------------------------------------------------------
            }
            else
            {
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    //if (m_DataSet.HasChanges())
                    //{

                    //    m_DataSet.Tables["AttendeeListTable"].AcceptChanges();
                    //}
                    //Do normal row filtering if none of the above conditions are true
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = "FirstLastName LIKE '%" + alisttxtSearch.Text + "%'";
                    //(dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
          
            }
        }

        private void alistcmbDate_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void alistcmbDate_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void alistcmbDate_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void alistDateCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            var calendar = sender as Calendar;

            m_dateIsValid = false;
            alistcmbDate.Text = "Select or type date";


            Add_Blackout_Dates(ref calendar);
        }

        private void alistDateCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            var calendar = sender as Calendar;
            Add_Blackout_Dates(ref calendar);
        }

        private void alistDateCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;
            

            DateTime datec = calender.SelectedDate.Value;
            m_DateSelected = datec;

            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (datec.DayOfWeek == DayOfWeek.Sunday)
            {
                
                m_dateIsValid = true;

                UpdateAttendeeListTableWithDateFilter();
                dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];
              
               
            }
            else
                m_dateIsValid = false;




           
            alistcmbDate.Text = date;




        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {

        }
    } // end MainWindow
} 





