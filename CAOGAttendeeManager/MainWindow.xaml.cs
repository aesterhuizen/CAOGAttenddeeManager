using System;
using System.IO;

//using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
           // Cursor = Cursors.Wait;
            InitializeComponent();
            
            this.Closing += new System.ComponentModel.CancelEventHandler(ClosingApp);

          
            


            
            //open file with database credentials
            SplashScreen splashScreen = new SplashScreen("Resources/splashscreen.png");
            splashScreen.Show(true);
            TimeSpan timespan = new TimeSpan(0, 0, 10); // 10 seconds timespan

            

            splashScreen.Close(timespan);


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

              //  ChangedbVal();

              
            InitDataSet();
            // CreateDatabase_FromXLSX();
               

               
            Display_DefaultTable_in_Grid();


            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nException occurred when performing database initialization {ex}!\n", ex);

            }


           // Cursor = Cursors.Arrow;










        }




        private ModelDb m_db;

        private DateTime m_DateSelected;
        private DateTime m_alistDateSelected;

        private DataSet m_DataSet = new DataSet();

        private SqlConnection m_mySqlConnection = null;

        private bool m_NoCredFile = false;
        
        private bool m_alistView = false;
        private bool m_AttendanceView = false;
        private bool m_isAttendedChecked = false;
        private bool m_isFollowupChecked = false;
        private bool m_isRespondedChecked = false;
        private bool m_dateIsValid = false;
        private bool m_alistdateIsValid = false;
        private bool m_filterByDate = false;


        private string m_constr = "";
       
        private List<string> m_lstdataGridHeadersClicked = new List<string> { };
        private int m_NewAttendeeId = 0;




        private void ChangedbVal()
        {



            var queryAttendees = from AttendeeRec in m_db.Attendees
                                 select AttendeeRec;

            foreach (var Attendee in queryAttendees)
            {
                 Attendee.Prospect = 0;
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


           
            // get current time
            DateTime curdate = DateTime.Now;
            TimeSpan timespanSinceDate = new TimeSpan();

           var queryAttendees = from AttendeeRec in m_db.Attendees
                                 select AttendeeRec;


            foreach (var AttendeeRec in queryAttendees)
            {




                var lstDateRecs = (from DateRec in AttendeeRec.AttendanceList
                                   orderby DateRec.Date ascending
                                   select DateRec).ToArray().LastOrDefault();

                if (lstDateRecs != null)
                {
                    timespanSinceDate = curdate - lstDateRecs.Date;



                    if ((lstDateRecs.Status == "Follow-Up" || lstDateRecs.Status == "Attended") && timespanSinceDate.Days <= 22)
                    {

                        // do nothing
                        //Attendee already have a followUp sent so do not generate another followup unil 28 days has
                        //lapsed since the last followUp        


                    }
                    else
                    {
                        //generate follow-up if attendee does not have 3 consecutive followups already
                      
                        Attendance_Info newfollowUpRecord = new Attendance_Info { };
                        newfollowUpRecord.AttendeeId = AttendeeRec.AttendeeId;
                        newfollowUpRecord.Date = curdate;
                        newfollowUpRecord.Status = "Follow-Up";
                        m_db.Attendance_Info.Add(newfollowUpRecord);

                      
                    }

                } //end if

            } //end foreach

           




        }
        
        private void CreateDatabase_FromXLSX()
        {
          
            // create Database from Excel Sheet






            Console.WriteLine("Openning Excel datasheet...");

            String connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;" +
                                        "Data Source=C:\\Users\\aesterh\\Documents\\church_stuff\\2017 Absent List - updated.xlsx;" +
                                        "Extended Properties='Excel 12.0;IMEX=1'";

            string sqlcmd = "SELECT * FROM [Sheet1$]";


            using (OleDbConnection oleConnection = new OleDbConnection(connectionString))
            {
                // command to select all the data from the database Q1
                OleDbCommand oleCommand = new OleDbCommand(sqlcmd, oleConnection);


                try
                {
                    oleConnection.Open();
                    Console.WriteLine("Database successfully opened!");

                    // create data reader
                    //OleDbDataReader oleDataReader = oleCommand.ExecuteReader();
                    OleDbDataAdapter da = new OleDbDataAdapter(oleCommand);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    Regex string_f = new Regex(@"^F");

                    string year = "";
                    string lstyear = "";
                    
                    string[] FLname = { "", "" };
                    string[] md;
                    string[] arylstAttDate = { };

                    int row = -1;

                   // DateTime lastAttendedDate = new DateTime();
                    

                    int isValid_employee = 0;
                    int att_infoID = 0, attID = 0, attLst_Idx = 0, offset = 0, HasThreeFollowUps = 0;
                    int NotAttendedCounter = 0;

                    Attendance_Info Attendee_Status = null;
                    Attendee churchAttendee = null;

             foreach (DataRow dr in dt.Rows)
             {

                        row++;
                        Console.WriteLine($"Row={row}");
                       
                            //Console.WriteLine($"Row={row}");
                            //break;
                       
                        if (row == 841)
                        {
                           break;
                        }
                       
                        // if (attID == 100) { break; }
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
                        if (row >=2)
                        {

                            //Active church Attendee
                            if (dr[1].ToString() == "1")
                            {


                                churchAttendee = new Attendee();
                                NotAttendedCounter = 0;

                                attID++;


                                FLname = dr[0].ToString().Trim().Split(' ');

                                if (FLname.Count() == 3)
                                {
                                    churchAttendee.FirstName = FLname[1] + " " + FLname[2];
                                    churchAttendee.LastName = FLname[0];
                                }
                                else
                                {

                                    churchAttendee.FirstName = FLname[1];
                                    churchAttendee.LastName = FLname[0];
                                }

                                churchAttendee.Prospect = 0;
                                
    //------for each column 1/3-4/30--------------------------------------------------------------------------------------------
                                for (int col_index = 3; col_index <= 3 + 22; col_index++)
                                {


                                    md = dt.Columns[col_index].ToString().Split('/');
                                    if (md.Count() == 2)
                                    {
                                        ///SetCurrentValue date attended
                                        DateTime date = new DateTime(2017, int.Parse(md[0]), int.Parse(md[1]));




                                        //attended
                                        if (dr[col_index].ToString() == "")
                                        {
                                            Attendee_Status = new Attendance_Info { };
                                            //lastAttendedDate = date;

                                            Attendee_Status.AttendeeId = attID;
                                            // Attendee_Status.Last_Attended = lastAttendedDate;
                                            Attendee_Status.Date = date;
                                            if (NotAttendedCounter == 3)
                                            {
                                                Attendee_Status.Status = "Responded";
                                                NotAttendedCounter = 0;

                                            }
                                            else
                                            {

                                                Attendee_Status.Status = "Attended";
                                                NotAttendedCounter = 0;
                                            }





                                            churchAttendee.AttendanceList.Add(Attendee_Status);
                                        }
                                        else if (dr[col_index].ToString() == "Start")
                                        {
                                            Attendee_Status = new Attendance_Info { };
                                            Attendee_Status.AttendeeId = attID;
                                            //Attendee_Status.Last_Attended = date;
                                            Attendee_Status.Date = date;
                                            Attendee_Status.Status = "Start";
                                            NotAttendedCounter = 0;
                                            churchAttendee.AttendanceList.Add(Attendee_Status);
                                        }
                                        else
                                        {
                                            //not attended==================================================================
                                            NotAttendedCounter++;
                                            if (NotAttendedCounter % 3 == 0)
                                            {
                                                //follow-up                                   
                                                Attendee_Status = new Attendance_Info { };
                                                Attendee_Status.AttendeeId = attID;
                                                //Attendee_Status.Last_Attended = lastAttendedDate;
                                                Attendee_Status.Date = date;
                                                Attendee_Status.Status = "Follow-Up";
                                                churchAttendee.AttendanceList.Add(Attendee_Status);
                                                NotAttendedCounter = 3;

                                            }
                                        }
                                    }


                                }    // end for col_index
//------end for column 1/7-8/6--------------------------------------------------------------------------------------------
                                for (int col_index = 24; col_index <= 24 + 17; col_index++)
                                {


                                    md = dt.Columns[col_index].ToString().Split('/');
                                    if (md.Count() == 2)
                                    {
                                        ///SetCurrentValue date attended
                                        DateTime date = new DateTime(2017, int.Parse(md[0]), int.Parse(md[1]));




                                        //attended
                                        if (dr[col_index].ToString() == "1")
                                        {
                                            Attendee_Status = new Attendance_Info { };
                                            //lastAttendedDate = date;

                                            Attendee_Status.AttendeeId = attID;
                                            // Attendee_Status.Last_Attended = lastAttendedDate;
                                            Attendee_Status.Date = date;
                                            if (NotAttendedCounter == 3)
                                            {
                                                Attendee_Status.Status = "Responded";
                                                NotAttendedCounter = 0;

                                            }
                                            else
                                            {
                                                
                                                Attendee_Status.Status = "Attended";
                                                NotAttendedCounter = 0;
                                            }
                                                


                                          

                                            churchAttendee.AttendanceList.Add(Attendee_Status);
                                        }
                                        else if (dr[col_index].ToString() == "Start")
                                        {
                                            Attendee_Status = new Attendance_Info { };
                                            Attendee_Status.AttendeeId = attID;
                                            //Attendee_Status.Last_Attended = date;
                                            Attendee_Status.Date = date;
                                            Attendee_Status.Status = "Start";
                                            NotAttendedCounter = 0;
                                            churchAttendee.AttendanceList.Add(Attendee_Status);
                                        }
                                        else
                                        {
                                            //not attended==================================================================
                                            NotAttendedCounter++;
                                            if (NotAttendedCounter % 3 == 0)
                                            {
                                                //follow-up                                   
                                                Attendee_Status = new Attendance_Info { };
                                                Attendee_Status.AttendeeId = attID;
                                                //Attendee_Status.Last_Attended = lastAttendedDate;
                                                Attendee_Status.Date = date;
                                                Attendee_Status.Status = "Follow-Up";
                                                churchAttendee.AttendanceList.Add(Attendee_Status);
                                                NotAttendedCounter = 3;
                                                
                                            }
                                        }
                                    }


                                }    // end for col_index

                                m_db.Attendees.Add(churchAttendee);
                                m_db.Attendance_Info.Add(Attendee_Status);

                            } // end if
                              //-Prospect list------------------------------------------------------------------------------------
                            else
                            {
                                churchAttendee = new Attendee();
                                
                                DateTime dateLA;
                                attID++;

                                
                                FLname = dr[0].ToString().Trim().Split(' ');
                                if (FLname.Count() == 3)
                                {
                                    churchAttendee.FirstName = FLname[1] + " " + FLname[2];
                                    churchAttendee.LastName = FLname[0];


                                }
                                else
                                {
                                    
                                    churchAttendee.FirstName = FLname[1];
                                    churchAttendee.LastName = FLname[0];

                                }
                                churchAttendee.Prospect = 1;

                                if (dr["Last attended"].ToString() != "")
                                {
                                    Attendee_Status = new Attendance_Info { };
                                    string[] arydateLA = dr["Last attended"].ToString().Split('.');
                                    dateLA = new DateTime(2017, int.Parse(arydateLA[0]), int.Parse(arydateLA[1]));


                                    if (FLname.Count() == 3)
                                    {
                                        churchAttendee.FirstName = FLname[1] + " " + FLname[2];
                                        churchAttendee.LastName = FLname[0];


                                    }
                                    else
                                    {

                                        churchAttendee.FirstName = FLname[1];
                                        churchAttendee.LastName = FLname[0];

                                    }
                                    churchAttendee.Prospect = 1;

                                    Attendee_Status.AttendeeId = attID;
                                    Attendee_Status.Date = dateLA;
                                    Attendee_Status.Status = "Attended";
                                    churchAttendee.AttendanceList.Add(Attendee_Status);
                                    
                                    m_db.Attendees.Add(churchAttendee);
                                    m_db.Attendance_Info.Add(Attendee_Status);
                                }
                                else
                                {
                                   

                                    
                                    m_db.Attendees.Add(churchAttendee);
                                   
                                }
                               
                            }
                        } // end if row==2
                    }
                    
                    
                    m_db.SaveChanges();
                    Console.WriteLine("\nDone!\n");
                   // oleDataReader.Close();
                } // end try




                catch (Exception ex)
               {
                    Console.Write("{0}", ex);

                }





            } // end using oleconnection
        
    } // end sub

        private void Display_AttendeeListTable_in_Grid()
        {

            if (m_DataSet.Tables.Contains("AttendeeListTable"))
            {
                dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];


                (dataGrid.DataContext as DataTable).DefaultView.Sort = "[Last Name] ASC";

                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                

            }


            
          
        } // end  private void Display_Database_in_Grid()

        //----Display Data in Grid------------------------------------------------------------------------------------------------------------------------   
        private void Display_DefaultTable_in_Grid()
        {

            if (m_DataSet.Tables.Contains("DefaultTable"))
            {
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                (dataGrid.DataContext as DataTable).DefaultView.Sort = "[Last Name] ASC";
                if (dataGrid.Columns.Count > 1)
                {
                    dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                    dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                }
            }

            

        } // end  private void Display_Database_in_Grid()



        private void InitDataSet()
        {

            //--------------------- Make DEFAULT TABLE---------------------------------------------------------------------------

            DataTable Default_Data_Table = new DataTable("DefaultTable");
            DataTable AttendeeListTable = new DataTable("AttendeeListTable");

            string date;



            if (m_filterByDate && m_dateIsValid)
                date = m_DateSelected.ToString("MM-dd-yyyy");
            else
                date = "Date Not Valid";

            try
            {

                var queryAttendees = from AttendeeRec in m_db.Attendees
                                     select AttendeeRec;

                
                Default_Data_Table.Columns.Add(new DataColumn("AttendeeId"));
                // Default_Data_Table.Columns.Add(new DataColumn("3follow-Ups", typeof(int) ));
                Default_Data_Table.Columns.Add(new DataColumn("FirstLastName"));
                Default_Data_Table.Columns.Add(new DataColumn("First Name"));
                Default_Data_Table.Columns.Add(new DataColumn("Last Name"));
                Default_Data_Table.Columns.Add(new DataColumn("Date Last Attended"));
                Default_Data_Table.Columns.Add(new DataColumn("Status"));
                //-------------------------------Make AttendeeList Table-------------------------------------------------------------------
                AttendeeListTable.Columns.Add(new DataColumn("AttendeeId"));
                AttendeeListTable.Columns.Add(new DataColumn("FirstLastName"));
                AttendeeListTable.Columns.Add(new DataColumn("First Name"));
                AttendeeListTable.Columns.Add(new DataColumn("Last Name"));
                AttendeeListTable.Columns.Add(new DataColumn("Date"));
                AttendeeListTable.Columns.Add(new DataColumn("Attended", typeof(bool)));


                Default_Data_Table.Columns["AttendeeId"].Unique = true;

                DataColumn[] primaryKeyCol = new DataColumn[1];
                primaryKeyCol[0] = Default_Data_Table.Columns[0];
                Default_Data_Table.PrimaryKey = primaryKeyCol;

                string ldate = "";
                string lstatus = "";


                foreach (var AttendeeRec in queryAttendees)
                {

                    var queryLastDate = (from DateRec in AttendeeRec.AttendanceList
                                         orderby DateRec.Date ascending
                                         select DateRec).ToList().LastOrDefault();

                   

                    if (AttendeeRec.Prospect == 0 || AttendeeRec.Prospect == 1)
                    {
                        //----Construct AttendeeLisTable-----------------------------------------
                        DataRow drNewAttendeeListRec = AttendeeListTable.NewRow();


                        drNewAttendeeListRec["AttendeeId"] = AttendeeRec.AttendeeId;
                        drNewAttendeeListRec["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;

                        drNewAttendeeListRec["First Name"] = AttendeeRec.FirstName;
                        drNewAttendeeListRec["Last Name"] = AttendeeRec.LastName;
                        drNewAttendeeListRec["Date"] = date;
                        drNewAttendeeListRec["Attended"] = false;

                        AttendeeListTable.Rows.Add(drNewAttendeeListRec);
                    }

                    //------Active Attendee--//---Construct DefaultTable-------------------------------------------------------------
                    if (AttendeeRec.Prospect == 0)
                    {

                        if (queryLastDate != null)
                        {
                            ldate = queryLastDate.Date.ToString("MM-dd-yyyy");
                            lstatus = queryLastDate.Status;
                            
                            DataRow dr = Default_Data_Table.NewRow();
                            m_NewAttendeeId = AttendeeRec.AttendeeId;


                            dr["AttendeeId"] = AttendeeRec.AttendeeId;
                            dr["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;

                            dr["First Name"] = AttendeeRec.FirstName;
                            dr["Last Name"] = AttendeeRec.LastName;


                            dr["Date Last Attended"] = ldate;
                            dr["Status"] = lstatus;
                            Default_Data_Table.Rows.Add(dr);
                        }
      
                    } 
               } // end foreach


                
                // Swap FirstName and LastName  columns
                Default_Data_Table.Columns[2].SetOrdinal(3);
                AttendeeListTable.Columns[2].SetOrdinal(3);
              

                Default_Data_Table.AcceptChanges();
                AttendeeListTable.AcceptChanges();

                m_DataSet.Tables.Add(Default_Data_Table);
                m_DataSet.Tables.Add(AttendeeListTable);

                m_NewAttendeeId += 1;

              

                lblAttendenceMetrics.Content = m_DataSet.Tables["DefaultTable"].Rows.Count;
                lblProspectsMetrics.Content = m_DataSet.Tables["AttendeeListTable"].Rows.Count;


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
            Cursor = Cursors.Wait;
            chkAttended.IsChecked = false;
            chkFollowup.IsChecked = false;
            m_isRespondedChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");
            string query = "";


            if (m_filterByDate && m_dateIsValid)
            {
                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'";
            }
            else
            {
                query = "SELECT  Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Responded'";
            }

            UpdateDataGrid(query);

            Cursor = Cursors.Arrow;

        }

        private void chkFollowup_Checked(object sender, RoutedEventArgs e)
        {

            Cursor = Cursors.Wait;

            chkAttended.IsChecked = false;
            chkResponded.IsChecked = false;
            m_isFollowupChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");
            string query = "";


            if (m_filterByDate && m_dateIsValid)
            {
                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Follow-Up'" +
                      "AND Attendance_Info.Date='" + date + "'";

            }
            else
            {
                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Follow-Up'";

            }
            UpdateDataGrid(query);
            Cursor = Cursors.Arrow;
        }

        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
            //generate list of all attended attendees

            Cursor = Cursors.Wait;
            string query = "";
            chkResponded.IsChecked = false;
            chkFollowup.IsChecked = false;

            m_isAttendedChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");



            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                       "WHERE Attendance_Info.Status='Attended'" +
                       "AND Attendance_Info.Date='" + date + "'";


            }
            else
            {

                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                           "FROM Attendees " +
                           "INNER JOIN Attendance_Info " +
                           "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                           "WHERE Attendance_Info.Status='Attended'";
            }

            UpdateDataGrid(query);
            Cursor = Cursors.Arrow;
        }





        private void Disable_Filters()
        {
            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;
            chkDateFilter.IsEnabled = false;
            // txtDate.IsEnabled = false;
            //m_isAllFiltersDisabled = true;

        }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if in followUp view, use query database else if in model list view filter table



            if (txtSearch.Text == "")
            {
                Enable_Filters();
                (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;

                //----------------------Textbox search has text-----------------------------------------------------------------------------------
            }
            else
            {
                Disable_Filters();
                (dataGrid.DataContext as DataTable).DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";

            }


        }

        private void UpdateDataGrid(string query)
        {



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

                queryTable.Columns.Add(new DataColumn("AttendeeId"));
                queryTable.Columns.Add(new DataColumn("FirstLastName"));
                queryTable.Columns.Add(new DataColumn("First Name"));
                queryTable.Columns.Add(new DataColumn("Last Name"));
                queryTable.Columns.Add(new DataColumn("Date"));
                queryTable.Columns.Add(new DataColumn("Status"));


                foreach (DataRow dr in dt.Rows)
                {

                    DataRow newrow = queryTable.NewRow();

                    DateTime date = (DateTime)dr["Date"];

                    newrow["AttendeeId"] = dr["AttendeeId"];
                    newrow["FirstLastName"] = dr["FirstName"].ToString() + " " + dr["LastName"].ToString();
                    newrow["First Name"] = dr["FirstName"];
                    newrow["Last Name"] = dr["LastName"];
                    newrow["Date"] = date.ToString("MM-dd-yyyy");
                    newrow["Status"] = dr["Status"];


                    queryTable.Rows.Add(newrow);
                }

                //Swap the columns back because the queries have these columns returned swapped

               // queryTable.Columns["FirstLastName"].SetOrdinal(1);


              //  queryTable.Columns[1].ColumnName = "First Name";
              //  queryTable.Columns[2].ColumnName = "Last Name";

                //Swap LastName and FirstName columns
                queryTable.Columns[2].SetOrdinal(3);
               // queryTable.Columns[1].SetOrdinal(2);



                dataGrid.DataContext = queryTable;
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; //LastFirstName

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

            Cursor = Cursors.Wait;
            string date = m_DateSelected.ToString("MM-dd-yyyy");

            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
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
            Cursor = Cursors.Arrow;
        }

        private void chkFollowup_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_isFollowupChecked = false;

            string date = m_DateSelected.ToString("MM-dd-yyyy");

            Cursor = Cursors.Wait;

            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
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
            Cursor = Cursors.Arrow;

        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_isRespondedChecked = false;

            string date = m_DateSelected.ToString("MM-dd-yyyy");


            if (m_filterByDate && m_dateIsValid)
            {

                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
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
            Cursor = Cursors.Arrow;
        }


        private void DateCalendar_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;

            string query = "";

            DateTime datec = calender.SelectedDate.Value;

            Cursor = Cursors.Wait;
            if (m_alistView)
            {
                m_alistDateSelected = datec;
                string date = m_alistDateSelected.ToString("MM-dd-yyyy");

                if (datec.DayOfWeek == DayOfWeek.Sunday)
                {
                    m_alistdateIsValid = true;

                    UpdateAttendeeListTableWithDateFilter();
                    dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];

               
                    alisttxtDate.Text = date;


                }

                else
                {
                    m_alistdateIsValid = false;
        
                }


            }
            else
            {
                m_DateSelected = datec;
                string date = m_DateSelected.ToString("MM-dd-yyyy");

                if (datec.DayOfWeek == DayOfWeek.Sunday)
                {

                    txtDate.Text = date;
           
                    m_dateIsValid = true;

                }

                else
                {
                    m_dateIsValid = false;
           
                }


                if (m_dateIsValid)
                {
                    query =
                "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "AND Attendance_Info.Date='" + date + "'";
                }


                if (m_isAttendedChecked)
                {
                    query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "AND Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'";

                }
                else if (m_isFollowupChecked)
                {
                    query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "AND Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'";

                }
                else if (m_isRespondedChecked)
                {
                    query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                    "FROM Attendees " +
                    "INNER JOIN Attendance_Info " +
                    "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                    "WHERE Attendees.HasThreeConsequitiveFollowUps='0' " +
                    "Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'";
                }
            }






            UpdateDataGrid(query);
            Cursor = Cursors.Arrow;
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
            //txtDate.Text = "Select or type date";


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
            string date = m_alistDateSelected.ToString("MM-dd-yyyy");
            string firstname = "";
            string lastname = "";



          



            if (m_alistdateIsValid)
            {

                // add all attendee status and date to database

                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                {

                    if (dr.RowState == DataRowState.Added)
                    {

                        firstname = dr["First Name"].ToString().ToUpper();
                        lastname = dr["Last Name"].ToString().ToUpper();

                        var queryAtt = (from AttRec in m_db.Attendance_Info
                                        where AttRec.Attendee.FirstName.ToUpper() == firstname && AttRec.Attendee.LastName.ToUpper() == lastname
                                        select AttRec).ToList().FirstOrDefault();

                        
                        if (queryAtt != null)
                        {


                            MessageBox.Show("A record with the same attendee or date already exist. Please select a unique name.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                            Cursor = Cursors.Arrow;
                        }
                        else
                        {

                            // Add a new Attendee to the database
                            Attendee newAttendeeRec = new Attendee();
                            Attendance_Info newAttInfoRec = new Attendance_Info();



                            newAttendeeRec.AttendeeId = m_NewAttendeeId;
                            newAttendeeRec.FirstName = dr["First Name"].ToString().Trim();
                            newAttendeeRec.LastName = dr["Last Name"].ToString().Trim();
                            //newAttendeeRec.HasThreeConsequitiveFollowUps = 0;



                            newAttInfoRec.AttendeeId = m_NewAttendeeId;
                            newAttInfoRec.Date = m_alistDateSelected;
                            //newAttInfoRec.Last_Attended = m_alistDateSelected;

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
                            haschanges = true;

                            int attid = int.Parse(dr["AttendeeId"].ToString());
                            newRecord.AttendeeId = attid;
                            newRecord.Date = m_alistDateSelected;
                          //  newRecord.Last_Attended = m_alistDateSelected;

                            var lastAttInfoRec = (from AttInfo in m_db.Attendance_Info
                                                  where AttInfo.AttendeeId == attid
                                                  orderby AttInfo.Date ascending
                                                  select AttInfo).ToArray().LastOrDefault();

                            var queryAttendee = (from att in m_db.Attendees
                                                where att.AttendeeId == attid
                                                select att).ToArray().FirstOrDefault();

                            if (lastAttInfoRec != null && queryAttendee != null)
                            {
                                // If the last record is a follow-up then this record is a responded Status
                                if (lastAttInfoRec.Status == "Follow-Up")
                                    newRecord.Status = "Responded";
                                else
                                    newRecord.Status = "Attended";

                                
                            }
                            else
                                newRecord.Status = "Attended";

                            queryAttendee.Prospect = 0;

                            m_db.Attendance_Info.Add(newRecord);
                        }

                    }
                }
            } // end if m_datValid

            else
            {
                MessageBoxResult mb = MessageBox.Show("Date not valid, please correct errors", "Date Invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
            }


            if (haschanges)
            {


                GenerateDBFollowUps();
                m_db.SaveChanges();
                
                //clear DataSet
                m_DataSet.Reset();
                InitDataSet();
                Display_AttendeeListTable_in_Grid();
             
                m_DataSet.AcceptChanges();


               
                alisttxtSearch.Text = "";
                MessageBox.Show("Changes Saved...");


            }
            else
            {
                MessageBox.Show("No changes to Save.");
            }






            Cursor = Cursors.Arrow;

        }


        //private void dataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    btnProspect.IsEnabled = false;
        //}

        private void UpdateAttendeeListTableWithDateFilter()
        {

            string date;

            if (m_alistdateIsValid)
                date = m_alistDateSelected.ToString("MM-dd-yyyy");
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
            // txtDate.IsEnabled = true;
            //m_isAllFiltersDisabled = false; 

        }

        private void Uncheck_All_Filters()
        {
            chkFollowup.IsChecked = false;
            chkResponded.IsChecked = false;
            chkAttended.IsChecked = false;
            chkDateFilter.IsChecked = false;
            DateCalendar.IsEnabled = false;
        }



        //private void dataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    var dgv = sender as DataGrid;

            
            
            
        //}
        private void dataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            IList<DataGridCellInfo> CellV = grid.SelectedCells;

            if (CellV.Count != 0)
            {
                DataRowView RowView = (DataRowView)CellV[0].Item;
                // wout consecutive colunm first, lastname is 2,3
                //w/ consecutive column first,last name is 3,4
                string firstname = RowView.Row[3].ToString();
                string lastname = RowView.Row[2].ToString();

                WndAttendeeInfo AttendeeInfoWindow = new WndAttendeeInfo(firstname, lastname, m_mySqlConnection);
                

                
                
                AttendeeInfoWindow.ShowDialog();
            }



        }

        private void chkDateFiler_Checked(object sender, RoutedEventArgs e)
        {


            m_filterByDate = true;
            txtDate.IsEnabled = true;
            DateCalendar.IsEnabled = true;



        }




        private void chkDateFiler_Unchecked(object sender, RoutedEventArgs e)
        {
            m_filterByDate = false;
            DateCalendar.IsEnabled = false;
            txtDate.IsEnabled = false;
            txtDate.Text = "Select or type date.";

            string query = "";
            Cursor = Cursors.Wait;


            if (m_isAttendedChecked)
            {
                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                      "FROM Attendees " +
                      "INNER JOIN Attendance_Info " +
                      "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                      "WHERE Attendance_Info.Status='Attended'";


            }
            else if (m_isFollowupChecked)
            {
                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                  "FROM Attendees " +
                  "INNER JOIN Attendance_Info " +
                  "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                  "WHERE Attendance_Info.Status='Follow-Up'";


            }
            else if (m_isRespondedChecked)
            {
                query = "SELECT Attendees.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                  "FROM Attendees " +
                  "INNER JOIN Attendance_Info " +
                  "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                  "WHERE Attendance_Info.Status='Responded'";

            }
            else
            {
                //all filters unchecked
                query = "ShowDefaultTable";

            }



            UpdateDataGrid(query);
            Cursor = Cursors.Arrow;

        }




        private void Window_Loaded(object sender, RoutedEventArgs e)
        {



            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                
            }

            dataGrid.CopyingRowClipboardContent += new EventHandler<DataGridRowClipboardEventArgs>(CopyDataGridtoClipboard);
          
            

        }

        private void dataGridHeader_Click(object sender, RoutedEventArgs e)
        {
            
            //var colHeader = sender as DataGridColumnHeader;

            //string colname = colHeader.Column.Header.ToString();

            //if (colHeader != null)
            //{
            //    if (m_lstdataGridHeadersClicked.Contains(colname) )
            //    {
            //        m_lstdataGridHeadersClicked.Remove(colname);
            //    }
            //    else
            //    {
            //        m_lstdataGridHeadersClicked.Add(colname);
            //    }


            //    dataGrid.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
            //    dataGrid.SelectedCells.Clear();
            //    foreach (var item in dataGrid.Items)
            //    {
            //        dataGrid.SelectedCells.Add(new DataGridCellInfo(item, dataGrid.Columns[0]));
            //    }

            //    foreach (DataRow row in m_DataSet.Tables["DefaultTable"].Rows)
            //    {

            //    }

           // }

            
                //var list = dataGrid.ItemsSource.Cast<object>();
            //    List<string> lstnames = new List<string> { };

                //if (list.Count() > 0)
                //{
                

            //        foreach (DataRow row in values)
            //        {
            //            lstnames.Add(row.ItemArray[index].ToString());

            //        }

            //    }


            //     }



       }

        private void CopyDataGridtoClipboard(object sender, DataGridRowClipboardEventArgs e)
        {
            //Console.WriteLine("Clipboard stuff");
            var selectedcells = sender as DataGrid;
           // IList<DataGridCellInfo> lstCells = selectedcells.SelectedCells;


            var currentCell = e.ClipboardRowContent[dataGrid.CurrentCell.Column.DisplayIndex];
            //e.ClipboardRowContent.Clear();
            e.ClipboardRowContent.Add(currentCell);
        }
        private void ClosingApp(Object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_NoCredFile)
            {
                e.Cancel = false;
            }
            else
            {

                if (dataGrid.DataContext != null)
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




                    if (haschanges)
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



        private void Update_Status()
        {



            string query = "";
            string date = m_DateSelected.ToString("MM-dd-yyyy");

            query = "SELECT Attendee.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
            "FROM Attendees " +
            "INNER JOIN Attendance_Info " +
            "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
            "WHERE Attendance_Info.Date='" + date + "'";

            if (m_isAttendedChecked)
            {
                query = "SELECT Attendee.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Status='Attended' AND Attendance_Info.Date='" + date + "'";

            }
            else if (m_isFollowupChecked)
            {
                query = "SELECT Attendee.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Status='Follow-Up' AND Attendance_Info.Date='" + date + "'";
            }
            else if (m_isRespondedChecked)
            {
                query = "SELECT Attendee.AttendeeId, Attendees.FirstName, Attendees.LastName, Attendance_Info.Date, Attendance_Info.Status " +
                "FROM Attendees " +
                "INNER JOIN Attendance_Info " +
                "ON Attendees.AttendeeId=Attendance_Info.AttendeeId " +
                "WHERE Attendance_Info.Status='Responded' AND Attendance_Info.Date='" + date + "'";

            }

            UpdateDataGrid(query);


        }


        private void btnNewRec_Click(object sender, RoutedEventArgs e)
        {


            ////first focus the grid
            dataGrid.Focus();
            ////then create a new cell info, with the item we wish to edit and the column number of the cell we want in edit mode
            //DataGridCell dgcell = new DataGridCell();

            //DataGridCellInfo cellInfo = new DataGridCellInfo(dgcell, dataGrid.Columns[1]);
            ////set the cell to be the active one
            //dataGrid.CurrentCell = cellInfo;
            ////scroll the item into view
            //dataGrid.ScrollIntoView(itemToSelect);
            ////begin the edit
            //dataGrid.BeginEdit();
            //dataGrid.SelectedItem = cellInfo;
            dataGrid.ScrollIntoView(dataGrid.Items[dataGrid.Items.Count - 1]); //scroll to last
            //dataGrid.UpdateLayout();
            //dataGrid.ScrollIntoView(dataGrid.SelectedItem);



        }




        private void Ribbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabAttendeeList.IsSelected)
            {

                // return datagrid's datacontext to show all records
                if (dataGrid.DataContext != null)
                {
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;

                }





                m_alistView = true;
                m_AttendanceView = false;
                if (DateCalendar.IsEnabled == false)
                {
                    DateCalendar.IsEnabled = true;
                }







                if (alisttxtSearch.Text != "")
                    txtSearch.Text = "";

                if (m_alistdateIsValid)
                {
                    alisttxtDate.Text = m_alistDateSelected.ToString("MM-dd-yyyy");
                }
                else
                {
                    alisttxtDate.Text = "";   
                    m_alistdateIsValid = false;
                }



                dataGrid.CanUserAddRows = true;
                dataGrid.CanUserDeleteRows = false;
                
                dataGrid.ToolTip = null;

                Display_AttendeeListTable_in_Grid();

               



            }
            //--------Home Tab -----------------------------------------------------------------------------------------
            else if (tabHome.IsSelected)
            {

                if (dataGrid.DataContext != null)
                {
                    // commit datagrid edits and return DataContext to show all records
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    //reset dataContext sow all records
                    (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;
                    (dataGrid.DataContext as DataTable).DefaultView.Sort = "[Last Name] ASC";
                }



                m_alistView = false;
                m_AttendanceView = true;


                Uncheck_All_Filters();
                Enable_Filters();
               // btnProspect.IsEnabled = false;

                if (txtSearch.Text != "")
                    txtSearch.Text = "";

                txtDate.IsEnabled = false;

                m_dateIsValid = false;
              //  btnProspect.IsEnabled = false;

                dataGrid.CanUserAddRows = false;
                dataGrid.CanUserDeleteRows = false;
                dataGrid.ToolTip="Left mouse click to select attendee.\nRight mouse click to see more in depth attendance for the selected attendee.";

                Display_DefaultTable_in_Grid();

              





            }
        }

      
        private void btnProspect_Click(object sender, RoutedEventArgs e)
        {
            var lstCell = dataGrid.SelectedCells;
           
            bool haschanges = false;

            if (lstCell.Count != 0)
            {
                DataRowView dataRow = (DataRowView)lstCell[0].Item;







                if (dataGrid.DataContext != null)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                    {
                        if (dr.ItemArray[5].ToString() == "True")
                        {
                            haschanges = true;
                            break;
                        }
                    }

                }


                if (haschanges )
                {
                    MessageBox.Show("There are changes to save in the Attendence Prospect list. Please save before adding attendee to prospect list", "Save Changes", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                else
                {
                    
                    int attID = int.Parse(dataRow.Row.ItemArray[0].ToString());

                    var queryAtt = (from AttRec in m_db.Attendees
                                    where AttRec.AttendeeId == attID
                                    select AttRec).FirstOrDefault();

                    MessageBoxResult res = MessageBox.Show($"Are you sure you want to add '{queryAtt.FirstName} {queryAtt.LastName}' to the prospect list?", "Add Prospect Attendee", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.OK)
                    {
                        queryAtt.Prospect = 1;
                        m_db.SaveChanges();
                        //m_DataSetChangesSaved = true;

                        // clear dataset
                        m_DataSet.Reset();
                        InitDataSet();
                        Display_DefaultTable_in_Grid();
                        MessageBox.Show($"Attendee '{queryAtt.FirstName} {queryAtt.LastName}' added to the prospect list.", "Add Prospect Attendee");

                    }
                    
                        
                    
                }


            }
            else
            {
                MessageBox.Show("Attendee Record must be selected.", "Select Record", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
        private void alisttxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (alisttxtSearch.Text == "")
            {

                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);



                // show all records
                (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;


                //----------------------Textbox search has text-----------------------------------------------------------------------------------
            }
            else
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                //Do normal row filtering if none of the above conditions are true
                (dataGrid.DataContext as DataTable).DefaultView.RowFilter = "FirstLastName LIKE '%" + alisttxtSearch.Text + "%'";


            }
        }

        private void alisttxtDate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {


                Regex pattern = new Regex(@"^[0-9]{2}-[0-9]{2}-[0-9]{4}");

                if (pattern.IsMatch(alisttxtDate.Text))
                {
                    string text = pattern.Match(alisttxtDate.Text).ToString();
                    string[] splitstr = text.Split('-');
                    string month = splitstr[0];
                    string day = splitstr[1];
                    string year = splitstr[2];


                    try
                    {
                        m_alistDateSelected = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                        if (m_alistDateSelected.DayOfWeek == DayOfWeek.Sunday)
                        {
                            m_alistdateIsValid = true;
                         
                            UpdateAttendeeListTableWithDateFilter();
                            dataGrid.DataContext = m_DataSet.Tables["AttendeeListTable"];
                            DateCalendar.DisplayDate = m_alistDateSelected;
                        }
                        else
                        {
                            m_alistdateIsValid = false;

                            MessageBox.Show("Date is not a Sunday.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);


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
                    MessageBox.Show("Date is in wrong format.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }
        //private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        //{
        //    //var grid = sender as DataGrid;
        //    //var cells = grid.SelectedCells;
           
           
           
        //    btnProspect.IsEnabled = true;
        //}
      
        //private void btnCopy_Click(object sender, RoutedEventArgs e)
        //{

        //    //CopyDataGridtoClipboard(dataGrid, e);


        //}

        ////private void dataGrid_KeyUp(object sender, MouseButtonEventArgs e)
        ////{

        ////}

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {

            //int count = 0;
            ////OpenFileDialog fileDialog = new OpenFileDialog();
            //PrintDialog pd = new PrintDialog();
            //pd.PageRangeSelection = PageRangeSelection.AllPages;

            ////fileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            ////fileDialog.InitialDirectory = Directory.GetCurrentDirectory();

            ////write formatted file to text file
            //string path_to_printFile = $"{Directory.GetCurrentDirectory()}\\AttendeeNames.txt";

            //DataTableReader tableReader = new DataTableReader(m_DataSet.Tables["DefaultTable"]);

            //using (StreamWriter writer = new StreamWriter(path_to_printFile, false))
            //{
            //    try
            //    {
            //        while (tableReader.Read())
            //        {
            //            count++;
            //            writer.Write(tableReader["FirstLastName"]);
            //            writer.Write("\t");
            //            if (count == 2)
            //            {
            //                writer.WriteLine();
            //                count = 0;
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show(ex.Message);
            //    }
            //    finally
            //    {
            //        writer.Close();
            //    }


            //}

            ////send data bytes to printer



            //bool? result = pd.ShowDialog();

            //if (result == true)
            //{
            //    //DocumentPaginator
                
            //    //FixedDocumentSequence fixedDocSeq = 
            //    //pd.PrintDocument()


            //}


        }

       
           
 

    
        private void txtDate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {


                Regex pattern = new Regex(@"^[0-9]{2}-[0-9]{2}-[0-9]{4}");

                if (pattern.IsMatch(txtDate.Text))
                {
                    string text = pattern.Match(txtDate.Text).ToString();
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
                          
                            DateCalendar.DisplayDate = m_DateSelected;
                            Update_Status();
                        }
                        else
                        {
                            m_dateIsValid = false;
                            MessageBox.Show("Date is not a Sunday.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);



                        }
                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show("Invalid date.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                       

                    }
                }
                else
                {
                    MessageBox.Show("Date is in wrong format.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    } // end MainWindow
} 





