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
            //open file with database cridentials

            
            var executingPath = Directory.GetCurrentDirectory();


            if (File.Exists($"{executingPath}\\credentials.txt") )
            {
                
                var fs = new FileStream($"{executingPath}\\credentials.txt", FileMode.Open, FileAccess.Read);
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    string line;
                    while ( (line = sr.ReadLine() ) != null)
                    {
                        m_credentials = line;
                    }

                }
                

                    
            }
            else
            {
                
                Console.WriteLine("Credential file does not exist!");
                return;
            }
           //CreateDatabase_FromXLSX();
           GenerateDBFollowUps();
            Display_Database_in_Grid();

               
               
           




        }

        //private string m_AttendeeID = "";


        private int m_lastAttendeeID = 0;
        private int m_lastAttendanceInfoID = 0;
        private DataSet m_DataSet = null;
        private string m_constr = "";
        private string m_FirstName = "";
        private string m_LastName = "";
        private string m_defaultSqlStr = "";
        private bool m_NonChecked = true;
        DateTime m_DateSelected;
        private bool m_isAttendedChecked = false;
        private bool m_isFollowupChecked = false;
        private bool m_isRespondedChecked = false;
        private bool m_dateIsValid = false;
        private string m_credentials = "";
        private SqlConnection m_mySqlConnection = null;



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



            m_constr = m_credentials;


            using (var db = new ModelDb(m_constr))
            {
                
                DateTime curdate = DateTime.Now;


                var queryAttendees = from AttendeeRec in db.Attendees
                                     select AttendeeRec;




                //var queryLastDateAttended = (from AttendanceRec in queryAttendees
                //                             where AttendanceRec.Status == "Attended" && AttendanceRec.AttendeeId == 
                //                             orderby AttendanceRec.Date ascending
                //                             select AttendanceRec).ToList().Last().Date;

               // DateTime queryLastDateAttended;

                foreach (var AttendeeRec in queryAttendees)
                {



                    var queryLastDateAttended = (from DateRec in AttendeeRec.AttendanceList
                                                                    where DateRec.Status == "Attended" && DateRec.AttendeeId == AttendeeRec.AttendeeId
                                                                    orderby DateRec.Date ascending
                                                                    select DateRec).ToList().LastOrDefault();

                    

                    

                    if (queryLastDateAttended != null)
                    {
                        TimeSpan timespanSinceLastAttended = curdate - queryLastDateAttended.Date ;


                        if (timespanSinceLastAttended.Minutes >= 5)
                        {

                            Attendance_Info newfollowUpRecord = new Attendance_Info { };
                            newfollowUpRecord.AttendeeId += AttendeeRec.AttendeeId;
                            newfollowUpRecord.Date = curdate;
                            newfollowUpRecord.Last_Attended = curdate;
                            newfollowUpRecord.Status = "Follow-Up";

                            db.Attendance_Info.Add(newfollowUpRecord);
                        }
                    }

                }
                db.SaveChanges();

            }






        }
        private void CreateDatabase_FromXLSX()
        {
            // create Database from Excel Sheet
            //m_constr = "Data Source=caogserver.database.windows.net;Initial Catalog=caogattendeedb;User ID=sqladmin;Password=ASdfGH12#$";
            m_constr = "Server=tcp:caogserver.database.windows.net,1433;Initial Catalog=TestDb2;Persist Security Info=False;User ID=sqladmin;Password=ASdfGH12#$;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";

            using (var db = new ModelDb(m_constr))
            {


                Console.WriteLine("Openning Excel datasheet...");

                String connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;" +
                                            "Data Source=C:\\Users\\Anton Esterhuizen\\Documents\\Visual Studio 2017\\vs_projects\\CAOGAttendeeManager\\Attendance Tracking & Followup 10-23-16_revised.xlsx;" +
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
                            
                            if (attID == 10) {break; }
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
                                    db.Attendees.Add(churchAttendee);
                                    db.Attendance_Info.Add(Attendee_Status);
                                }
                                // create new attendee
                                churchAttendee = new Attendee();
                                attID++;

                                year = oleDataReader.GetName(1);
                                FLname = oleDataReader[1].ToString().Split(' ');

                                churchAttendee.FirstName = FLname[1];
                                churchAttendee.LastName = FLname[0];

                                churchAttendee.AttendeeId = attID;
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
                                        att_infoID++;

                                        md = oleDataReader.GetName(col_index).ToString().Split('/');
                                        DateTime datetime = new DateTime(Int32.Parse(year), Int32.Parse(md[0]), Int32.Parse(md[1]));
                                        string[] arylstAttDate = oleDataReader[2].ToString().Split('.');
                                        string lstyear = "20" + arylstAttDate[2];
                                        DateTime lstAttendedDate = new DateTime(Int32.Parse(lstyear), Int32.Parse(arylstAttDate[0]), Int32.Parse(arylstAttDate[1]));

                                        Attendee_Status.Attendance_InfoId = att_infoID;
                                        Attendee_Status.AttendeeId = attID;
                                        //Attendee_Status.Attendee = churchAttendee;
                                        Attendee_Status.Date = datetime;
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
                        db.Attendees.Add(churchAttendee);
                        db.Attendance_Info.Add(Attendee_Status);
                        db.SaveChanges();
                        Console.WriteLine("\nDone!\n");
                        oleDataReader.Close();
                    } // end try




                    catch (Exception ex)
                    {
                        Console.Write("{0}", ex);

                    }





                } // end using oleconnection


            } // end using db



        } // end sub
          //----Display Data in Grid------------------------------------------------------------------------------------------------------------------------   
        private bool Display_Database_in_Grid()
        {
            bool bSuccess = InitDataSet();

            
            dataGrid.DataContext = m_DataSet.Tables[0];


            return bSuccess;


        } // end  private void Display_Database_in_Grid()

        private bool InitDataSet()
        {
            // m_constr = "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\Database.mdf;Integrated Security=True;Trusted_Connection=True;Connect Timeout = 30";
            bool r = false;
            m_constr = m_credentials;

            string mysqlstring = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                            "ORDER BY Attendees.FirstName ASC";

            string sqlAttendees = "SELECT Attendees.FirstName,Attendees.LastName FROM Attendees ORDER BY Attendees.FirstName ASC";
            string sqlAttendee_Info = "SELECT * FROM Attendance_Info";

            SqlConnection myConnection = new SqlConnection(m_constr);

            m_mySqlConnection = myConnection;
            m_defaultSqlStr = mysqlstring;

            try
            {
                myConnection.Open();
                Console.WriteLine($"Database Successfully opened!");

                SqlDataAdapter myAdapter1 = new SqlDataAdapter(sqlAttendees, myConnection);
                SqlDataAdapter myAdapter2 = new SqlDataAdapter(sqlAttendee_Info, myConnection);
                SqlDataAdapter myAdapter3 = new SqlDataAdapter(mysqlstring, myConnection);
                DataTable dt1 = new DataTable("AttendeesTbl");
                DataTable dt2 = new DataTable("AttendanceTbl");
                DataTable dt3 = new DataTable("DefaultTbl");

                DataSet ds = new DataSet();

                myAdapter1.Fill(dt1);
                myAdapter2.Fill(dt2);
                myAdapter3.Fill(dt3);

                ds.Tables.Add(dt1);
                ds.Tables.Add(dt2);
                ds.Tables.Add(dt3);

                m_DataSet = ds;



                myConnection.Close();


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred! {ex}");
            }

            //if (m_DataSet.Tables[0].Rows.Count > 0) { r = true; }

            return r;

        }


        



        private void chkResponded_Checked(object sender, RoutedEventArgs e)
        {

            chkAttended.IsChecked = false;
            chkFollowup.IsChecked = false;
            m_isRespondedChecked = true;

            string query = "";
            m_NonChecked = false;

            if (m_dateIsValid)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + m_DateSelected + "'" +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                
            }
            else
            {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                               "FROM Attendees " +
                               "INNER JOIN Attendance_Info " +
                               "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                               "WHERE Attendance_Info.Status='Responded'" :

                               "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                               "FROM Attendees " +
                               "INNER JOIN Attendance_Info " +
                               "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                               "WHERE Attendance_Info.Status='Responded' " +
                               "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
                
            }

            UpdateDataGrid(query);



        }

        private void chkFollowup_Checked(object sender, RoutedEventArgs e)
        {
            chkAttended.IsChecked = false;
            chkResponded.IsChecked = false;
            m_isFollowupChecked = true;

            string query = "";
            m_NonChecked = false;

            if (m_dateIsValid)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + m_DateSelected + "'" +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                
            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Status='Follow-Up'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Status='Follow-Up' " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }
            UpdateDataGrid(query);
       
        }

        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
            //generate list of all attended attendees

            string query = "";
            chkResponded.IsChecked = false;
            chkFollowup.IsChecked = false;
            m_NonChecked = false;
            m_isAttendedChecked = true;

            if (m_dateIsValid)
            {
                
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + m_DateSelected + "' " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";


                
                


            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended' " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            UpdateDataGrid(query);





            //SqlCommand com = new SqlCommand(query, m_mySqlConnection);

            //SqlDataAdapter myAdapter = new SqlDataAdapter(query, m_mySqlConnection);
            //DataTable tblTemp = new DataTable();
            //myAdapter.Fill(tblTemp);

            //DataTable tblFinal = new DataTable();
            //DataColumn idcolumn = tblFinal.Columns.Add("ID", typeof(int));
            //tblFinal.PrimaryKey = new DataColumn[] { idcolumn };


            //int rIdx = 0, Idx = 0;
            //string prevDate = "", ColName = "", AttendeeName = "";



            //string[] aryColName = tblTemp.Columns[3].ToString().Split(' ');
            //ColName = aryColName[0];
            //AttendeeName = $"{tblTemp.Columns[0].ToString()} {tblTemp.Columns[1].ToString()}";

            ////tblFinal.Columns.Add(ColName, typeof(string));
            ////DataRow dr = tblFinal.NewRow();
            //// dr[ColName] = AttendeeName;
            //// tblFinal.Rows.Add(dr);


            //DataTable table = new DataTable("childTable");
            //DataColumn column;
            //DataRow row;

            //// Create first column and add to the DataTable.
            //column = new DataColumn();
            //column.DataType = System.Type.GetType("System.String");
            //column.ColumnName = "1/3";
            //column.ReadOnly = true;


            //// Add the column to the DataColumnCollection.
            //table.Columns.Add(column);

            //// Create second column.
            //column = new DataColumn();
            //column.DataType = System.Type.GetType("System.String");
            //column.ColumnName = "1/7";
            //column.AutoIncrement = false;
            //column.Caption = "ChildItem";
            //column.ReadOnly = false;

            //table.Columns.Add(column);



            //for (int i = 0; i < 4; i++)
            //{
            //    row = table.NewRow();
            //    row["1/3"] = "Hello";
            //    table.Rows.Add(row);
            //}



            //for (int i = 5; i < 7; i++)
            //{
            //    row = table.NewRow();
            //    row["1/7"] = "Hello";
            //    table.Rows.Add(row);
            //}

            //UpdateDataGrid(query);

            // create new table with Date as columns
            // add Attendees in different date buckets


            // for (int i = 0; i < tblTemp.Rows.Count - 1; i++)
            // {

            //         string[] aryColName = tblTemp.Columns[3].ToString().Split(' ');
            //         ColNames = aryColName[0];
            //         AttendeeName = $"{tblTemp.Columns[0].ToString()} {tblTemp.Columns[1].ToString()}";




            // }




            // if (rIdx == 0) { tblFinal.Columns.Add(ColName, typeof(string)); }

            // if (ColName == prevDate)
            // {

            //     DataRow dr = tblFinal.NewRow();
            //     dr[ColName] = AttendeeName;
            //     tblFinal.Rows.Add(dr);
            // }
            // else
            // {
            //     
            // }



            // //tblFinal.Columns.Add(ColName, typeof(string));


            // prevDate = ColName;




            // rIdx++;
            //if (rIdx == 5)
            // {
            //     break;
            // }





            // ListView lstViewData
            //lstViewData.View = View.Details;
            //listView1.GridLines = true;

            //// for (int i = 0; i < tblTemp.Rows.Count; i++)
            //// {
            //DataRow dr = tblTemp.Rows[i];
            //ListViewItem listitem = new ListViewItem(dr["FirstName"].ToString());
            //listitem.SubItems.Add(dr["LastName"].ToString());
            //listitem.SubItems.Add(dr["Status"].ToString());

            //listView1.Items.Add(listitem);


            //}








            //    //m_DataSet.Tables.Add(tblTemp);


        }

    

        private void dataGridQ1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            var grid = sender as DataGrid;
            IList<DataGridCellInfo> CellV = grid.SelectedCells;

            DataRowView RowView = (DataRowView)CellV[0].Item;

            m_FirstName = RowView.Row[0].ToString();
            m_LastName = RowView.Row[1].ToString();

            WndAttendeeInfo AttendeeInfoWindow = new WndAttendeeInfo(m_FirstName, m_LastName, m_mySqlConnection);
            AttendeeInfoWindow.Show();


        }

        private void mnuItemExit_Click(object sender, RoutedEventArgs e)
        {
            m_mySqlConnection.Close();

        }
        //--------------ADD Attendee-------------------------------------------------------------------------------------------------------
        private void btn_AddAttendee_Click(object sender, RoutedEventArgs e)
        {


            Regex chkFormat = new Regex(@"^\s*\w+\s+\w+\s*,\s*[0-9]+/[0-9]+/[0-9]{4}\s*,\s*\w+\s*,\s*[0-9]+/[0-9]+/[0-9]{4}$");

            if (chkFormat.IsMatch(txtbox_addAttendee.Text))
            {
                // get last AttendeeId in datagrid
                m_lastAttendeeID = m_DataSet.Tables[0].Rows.Count;
                m_lastAttendanceInfoID = m_DataSet.Tables[1].Rows.Count;

                using (var db = new ModelDb(m_constr))
                {



                    string[] aryNewAttendeeInfo = txtbox_addAttendee.Text.Split(',');
                    string[] subDate = aryNewAttendeeInfo[1].Split('/');
                    string[] aryName = aryNewAttendeeInfo[0].Split(' ');
                    string[] lstDate = aryNewAttendeeInfo[3].Split('/');


                    string year = subDate[2].Trim();
                    string month = subDate[0].Trim();
                    string day = subDate[1].Trim();

                    string lstyear = lstDate[2].Trim();
                    string lstmonth = lstDate[0].Trim();
                    string lstday = lstDate[1].Trim();


                    string status = aryNewAttendeeInfo[2].Trim();




                    DateTime NewDate = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));
                    DateTime lstAttendedDate = new DateTime(Int32.Parse(lstyear), Int32.Parse(lstmonth), Int32.Parse(lstday));


                    Attendee newChurchAttendee = new Attendee { };
                    Attendance_Info newChurchAttendeeInfo = new Attendance_Info { };



                    newChurchAttendee.FirstName = aryName[0].Trim();
                    newChurchAttendee.LastName = aryName[1].Trim();
                    //newChurchAttendee.AttendeeId = m_lastAttendeeID + 1;

                    //newChurchAttendeeInfo.Attendance_InfoId = m_lastAttendanceInfoID + 1;
                    //newChurchAttendeeInfo.AttendeeNum = m_lastAttendeeID + 1;
                    newChurchAttendeeInfo.Date = NewDate;
                    newChurchAttendeeInfo.Status = status;
                    newChurchAttendeeInfo.Last_Attended = lstAttendedDate;

                    db.Attendees.Add(newChurchAttendee);
                    db.Attendance_Info.Add(newChurchAttendeeInfo);
                    db.SaveChanges();
                }
                chkAttended.IsChecked = false;
                chkFollowup.IsChecked = false;
                chkResponded.IsChecked = false;

                InitDataSet();
                dataGrid.DataContext = m_DataSet.Tables[0];

            }
            else
            {
                MessageBox.Show("Input string format not correct");

            }
        }
        //----End Add Attendee--------------------------------------------------------------------------------------------------------------------------------
        private void mnuItemCharts_Click(object sender, RoutedEventArgs e)
        {
            AttendeeChartForm ChartForm = new AttendeeChartForm();


            ChartForm.Show();

        }

        private void txtbox_addAttendee_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtbox_addAttendee.Text == "")
            {
                btn_AddAttendee.IsEnabled = false;
            }
            else
            {
                btn_AddAttendee.IsEnabled = true;
            }

        }

        

        //private void btnSearch_Click(object sender, RoutedEventArgs e)
        //{
        //    string[] aryFirstLastName = txtSearch.Text.Split(' ');
        //    m_FirstName = aryFirstLastName[0];
        //    m_LastName = aryFirstLastName[1];
        //    m_searchStr = txtSearch.Text;

        //    string query = "";
        //    if (chkAttended.IsChecked == true)
        //    {
        //        query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
        //               "FROM Attendees " +
        //               "INNER JOIN Attendance_Info " +
        //               "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
        //               "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
        //               "Attendance_Info.Status='Attended' ORDER BY Date ASC";
        //    }
        //    else if (chkContacted.IsChecked == true)
        //    {

        //        query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
        //               "FROM Attendees " +
        //               "INNER JOIN Attendance_Info " +
        //               "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
        //               "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
        //               "Attendance_Info.Status='Follow-Up' ORDER BY Date ASC";

        //    }
        //    else if (chkResponded.IsChecked == true)
        //    {
        //        query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
        //               "FROM Attendees " +
        //               "INNER JOIN Attendance_Info " +
        //               "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
        //               "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
        //               "Attendance_Info.Status='Responded' ORDER BY Date ASC";
        //    }
        //    else
        //    {
        //        query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
        //               "FROM Attendees " +
        //               "INNER JOIN Attendance_Info " +
        //               "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
        //               "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "' ORDER BY Date ASC";
        //    }



        //    UpdateDataGrid(query);

        //}
        
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = "";

            if (m_dateIsValid)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                         "FROM Attendees " +
                         "INNER JOIN Attendance_Info " +
                         "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                         "WHERE Attendance_Info.Date='" + m_DateSelected + "' " :

                         "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                         "FROM Attendees " +
                         "INNER JOIN Attendance_Info " +
                         "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                         "WHERE Attendance_Info.Date='" + m_DateSelected + "' " +
                         "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
                
            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                     "FROM Attendees " +
                     "INNER JOIN Attendance_Info " +
                     "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " :

                     "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                     "FROM Attendees " +
                     "INNER JOIN Attendance_Info " +
                     "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                     "WHERE Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            if (!m_NonChecked)
            {
                if (m_isAttendedChecked)
                {

                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                          "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " + 
                         "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + m_DateSelected + "' " +
                          "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";



                }
                else if (m_isFollowupChecked)
                {
                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                     "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + m_DateSelected + "' " +
                      "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                }
                else if (m_isRespondedChecked)
                {

                    query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                      "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                     "WHERE Attendance_Info.Status='Responded' " +
                      "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
                }

               

            }

            UpdateDataGrid(query);
            
            

           
        }

        private void UpdateDataGrid(string query)
        {
            SqlDataAdapter da = new SqlDataAdapter(query, m_mySqlConnection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dataGrid.DataContext = dt;
        }
        private void chkAttended_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_NonChecked = true;
            m_isAttendedChecked = false;

            if (m_dateIsValid)
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Date='" + m_DateSelected + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Date='" + m_DateSelected + "' " +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";






            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            UpdateDataGrid(query);
            //if (chkAttended.IsChecked == false && chkContacted.IsChecked == false && chkResponded.IsChecked == false && m_searchStr != "")
            //{
            //    query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
            //            "FROM Attendees " +
            //            "INNER JOIN Attendance_Info " +
            //            "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
            //            "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" +
            //            " ORDER BY Date ASC";

            //    UpdateDataGrid(query);
            //}
            //else
            //{
            //    dataGrid.DataContext = m_DataSet.Tables[2];
            //}
        }

        private void chkFollowup_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_NonChecked = true;
            m_isFollowupChecked = false;

            if (m_dateIsValid)
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Date='" + m_DateSelected + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Date='" + m_DateSelected + "' " +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";






            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            UpdateDataGrid(query);
            
        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_NonChecked = true;
            m_isRespondedChecked = false;
            if (m_dateIsValid)
            {

                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Date='" + m_DateSelected + "'" :

                       "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendance_Info.Date='" + m_DateSelected + "' " +
                       "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";






            }
            else
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";
            }

            UpdateDataGrid(query);
            //if (chkAttended.IsChecked == false && chkContacted.IsChecked == false && chkResponded.IsChecked == false && m_searchStr != "")
            //{
            //    query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
            //            "FROM Attendees " +
            //            "INNER JOIN Attendance_Info " +
            //            "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
            //            "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" +
            //            " ORDER BY Date ASC";

            //    UpdateDataGrid(query);
            //}
            //else
            //{
            //    dataGrid.DataContext = m_DataSet.Tables[2];
            //}
        }

      
        private void DatePick_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;
            string query = "";

            if (calender.SelectedDate.HasValue)
            {
                DateTime date = calender.SelectedDate.Value;
                m_DateSelected = date;
                if (date.DayOfWeek == DayOfWeek.Sunday) { m_dateIsValid = true; }
            }

            if (m_dateIsValid)
            {
                query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Date='" + m_DateSelected + "'" :

                           "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                           "WHERE Attendance_Info.Date='" + m_DateSelected + "' " +
                           "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                if (!m_NonChecked)
                {
                    if (m_isAttendedChecked)
                    {
                        query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                              "FROM Attendees " +
                              "INNER JOIN Attendance_Info " +
                              "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                              "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                              "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                              "FROM Attendees " +
                              "INNER JOIN Attendance_Info " +
                              "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                              "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + m_DateSelected + "' " +
                              "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                    }
                    else if (m_isFollowupChecked)
                    {
                        query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                          "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                          "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                          "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + m_DateSelected + "' " +
                          "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                    }
                    else if (m_isRespondedChecked)
                    {
                        query = (txtSearch.Text == "") ? "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                          "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + m_DateSelected + "'" :

                          "SELECT Attendees.FirstName, Attendees.LastName, Attendance_Info.Status,Attendance_Info.Date " +
                          "FROM Attendees " +
                          "INNER JOIN Attendance_Info " +
                          "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                          "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + m_DateSelected + "' " +
                          "AND Attendees.FirstName LIKE '%" + txtSearch.Text + "%'" + " OR " + "Attendees.LastName LIKE '%" + txtSearch.Text + "%'";

                    }
                }
                
            }
            UpdateDataGrid(query);


        }

       

       

        private void DatePick_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            var calendar = sender as Calendar;
            DateTime date = calendar.DisplayDate;
            

            var dates = new List<DateTime>();


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

            if (date.DayOfWeek == DayOfWeek.Sunday) { m_dateIsValid = true; }

        }

        private void DatePick_Loaded(object sender, RoutedEventArgs e)
        {

            var calendar = sender as Calendar;
            DateTime date = calendar.DisplayDate;
            m_DateSelected = date;
            

            var dates = new List<DateTime>();


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

            if (date.DayOfWeek == DayOfWeek.Sunday) { m_dateIsValid = true; }

        }
    } // end MainWindow
}



