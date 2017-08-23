using System;
using System.IO;

//using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Timers;
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

            InitializeComponent();

#if (DEBUG)
            this.Title = "CAOG Attendee Manager (Debug)";
#endif

            this.Closing += new System.ComponentModel.CancelEventHandler(ClosingApp);
            dataGrid.CopyingRowClipboardContent += new EventHandler<DataGridRowClipboardEventArgs>(CopyDataGridtoClipboard);
            

           


            //open file with database credentials
            SplashScreen splashScreen = new SplashScreen("Resources/splashscreen.png");
            splashScreen.Show(true);
            TimeSpan timespan = new TimeSpan(0, 0, 1); // 1 seconds timespan

            

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

                    MessageBox.Show("Cannot connect to database, credential file does not exist!", "File does not exist.", MessageBoxButton.OK, MessageBoxImage.Error);
                    m_NoCredFile = true;
                    this.Close();
                }
                m_dbContext = new ModelDb(m_constr);
                

                //  ChangedbVal();


              // correctDBerrors();

                if (m_dbContext.Attendees.Count() == 0)
                {
                    CreateDatabase_FromXLSX();
                    InitDataSet();
                    Display_DefaultTable_in_Grid();
                }
                else
                {
                    InitDataSet();
                    Display_DefaultTable_in_Grid();
                }

                // SetTimer();


            }
            catch (Exception ex)
            {
                
                MessageBox.Show($"Exception occurred when performing database initialization { ex}!\n");
            }





          






        }




        private ModelDb m_dbContext;

        private DateTime m_DateSelected;
        private DateTime m_alistDateSelected;
        
        private DataSet m_DataSet = new DataSet();
        DataTable m_tempTable = new DataTable();
  
       // private Timer aTimer;

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
       
       // private List<string> m_lstdataGridHeadersClicked = new List<string> { };
        private int m_NewAttendeeId = 0;


        //private void SetTimer()
        //{
        //    aTimer = new Timer(1000);
        //    // Hook up the Elapsed event for the timer. 
        //    aTimer.Elapsed += OnTimedEvent;
        //    aTimer.AutoReset = true;
        //    aTimer.Enabled = true;
        //}

        //private void OnTimedEvent(Object source, ElapsedEventArgs e)
        //{
        //    //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke((Action)(() =>
        //    //{
        //    //    if (m_DataSet.HasChanges())
        //    //    {
        //    //        btnSaveUpdate.IsEnabled = false;
        //    //    }
        //    //}));
            
            
            
        //}

        private void ChangedbVal()
        {



            var queryAttendees = from AttendeeRec in m_dbContext.Attendees
                                 select AttendeeRec;

            foreach (var Attendee in queryAttendees)
            {
                 Attendee.Prospect = 0;
            }
            m_dbContext.SaveChanges();

        }

        private void correctDBerrors()
        {
            DateTime date = new DateTime(2017, 8, 13);

            var queryAttendees = from AttendeeRec in m_dbContext.Attendees
                                 select AttendeeRec;

            //var lstInfoRecs = from InfoRec in m_dbContextContext.Attendance_Info
            //                  where InfoRec.Date == date && InfoRec.Status == "Responded"
            //                  select InfoRec;

            foreach (var attrec in queryAttendees)
            {
                var lstInfoRecs = (from InfoRec in attrec.AttendanceList
                                   select InfoRec).ToArray();

                
                int count = lstInfoRecs.Count();
                

                if (count >=2)
                {
                    for (int idx = 0; idx <= count - 1; idx++)
                    {
                        if (lstInfoRecs[idx].Date == date && lstInfoRecs[idx].Status == "Responded")
                        {
                            TimeSpan tspan = lstInfoRecs[idx].Date - lstInfoRecs[idx - 1].Date;


                            if (tspan.Days < 22 && lstInfoRecs[idx - 1].Status == "Responded")
                            {
                                lstInfoRecs[idx].Status = "Attended";

                            }
                        }
                        //if (lstInfoRecs[idx].Date == date && lstInfoRecs[idx].Status == "Responded")
                        //{


                        //    if (lstInfoRecs[idx+1].Status=="Responded" && lstInfoRecs[idx + 1].Status == "Responded")
                        //    {
                        //        m_dbContextContext.Attendance_Info.Remove(lstInfoRecs[idx + 1]);

                        //    }
                        //}

                    }
                } 
                
            }


            m_dbContext.SaveChanges();
            Console.WriteLine("Done!");

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
            List<DateTime> lstsundays = new List<DateTime>();

            TimeSpan timespanSinceDate = new TimeSpan();

           var queryAttendees = from AttendeeRec in m_dbContext.Attendees.Local
                                 select AttendeeRec;


            foreach (var AttendeeRec in queryAttendees)
            {




                var lstDateRecs = (from DateRec in AttendeeRec.AttendanceList
                                   orderby DateRec.Date ascending
                                   select DateRec).ToArray().LastOrDefault();

                if (lstDateRecs != null)
                {
                    timespanSinceDate = curdate - lstDateRecs.Date;



                    if (timespanSinceDate.Days <= 22)
                    {

                        // do nothing
                        //Attendee already have a followUp sent so do not generate another followup unil 22 days has
                        //lapsed since the last followUp        


                    }
                    else
                    {
                        //generate follow-up if attendee does not have 3 consecutive followups already
                        //search for sunday
                        
                       
                      for (DateTime date=lstDateRecs.Date; date <=curdate;date = date.AddDays(1) )
                        {
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                lstsundays.Add(date);
                            }
                        }
                      
                        DateTime lastSunday = lstsundays.LastOrDefault();

                        if (lastSunday !=null )
                        {
                            Attendance_Info newfollowUpRecord = new Attendance_Info { };
                            newfollowUpRecord.AttendeeId = AttendeeRec.AttendeeId;
                            newfollowUpRecord.Date = lastSunday.Date;
                            newfollowUpRecord.Status = "Follow-Up";
                            m_dbContext.Attendance_Info.Add(newfollowUpRecord);
                        }
                        

                      
                    }

                } //end if

            } //end foreach

           




        }
        
        private void CreateDatabase_FromXLSX()
        {
          
            // create Database from Excel Sheet


            Console.WriteLine("Openning Excel datasheet...");

            String connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;" +
                                        $"Data Source={Directory.GetCurrentDirectory()}\\2017 Absent List - updated.xlsx;" +
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
                       
                        if (row == 100)
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

                                m_dbContext.Attendees.Add(churchAttendee);
                                m_dbContext.Attendance_Info.Add(Attendee_Status);

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
                                    
                                    m_dbContext.Attendees.Add(churchAttendee);
                                    m_dbContext.Attendance_Info.Add(Attendee_Status);
                                }
                                else
                                {
                                   

                                    
                                    m_dbContext.Attendees.Add(churchAttendee);
                                   
                                }
                               
                            }
                        } // end if row==2
                    }
                    
                    
                    m_dbContext.SaveChanges();
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


                //(dataGrid.DataContext as DataTable).DefaultView.Sort = "[Last Name] ASC";
                if (dataGrid.Columns.Count > 1)
                {
                    dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                    dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                }

            }


            
          
        } // end  private void Display_Database_in_Grid()

        private void dataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if (dataGrid.Columns.Count > 1)
            //{
            //    dataGrid.Columns[0].Visibility = Visibility.Hidden; // FirstLastName
            //    dataGrid.Columns[1].Visibility = Visibility.Hidden; // AttendeeId

            //}
        }
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
                date = "Date Not Valid.";

            try
            {

                var queryAttendees = (from AttendeeRec in m_dbContext.Attendees
                                     select AttendeeRec).ToArray();

                
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
                                         where DateRec.Status == "Attended" || DateRec.Status == "Responded"
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
                            
                            DataRow DefaultTabledr = Default_Data_Table.NewRow();
                            m_NewAttendeeId = AttendeeRec.AttendeeId;


                            DefaultTabledr["AttendeeId"] = AttendeeRec.AttendeeId;
                            DefaultTabledr["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;

                            DefaultTabledr["First Name"] = AttendeeRec.FirstName;
                            DefaultTabledr["Last Name"] = AttendeeRec.LastName;


                            DefaultTabledr["Date Last Attended"] = ldate;
                            DefaultTabledr["Status"] = lstatus;

                            Default_Data_Table.Rows.Add(DefaultTabledr);
                        }
      
                    } 
               } // end foreach


                
                // Swap FirstName and LastName  columns
                Default_Data_Table.Columns[2].SetOrdinal(3);
                AttendeeListTable.Columns[2].SetOrdinal(3);

                AttendeeListTable.AcceptChanges();
                Default_Data_Table.AcceptChanges();
             

                m_DataSet.Tables.Add(Default_Data_Table);
                m_DataSet.Tables.Add(AttendeeListTable);

                m_NewAttendeeId += 1;

              

            
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
            string query = "0";

            IQueryable<AttRecord> querylinq;

            if (m_filterByDate && m_dateIsValid)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

                
            }
            else
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Responded"
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

            }

            UpdateDataGrid(querylinq, query);

            Cursor = Cursors.Arrow;

        }

        private void chkFollowup_Checked(object sender, RoutedEventArgs e)
        {

            Cursor = Cursors.Wait;

            chkAttended.IsChecked = false;
            chkResponded.IsChecked = false;
            m_isFollowupChecked = true;
            string date = m_DateSelected.ToString("MM-dd-yyyy");
            string query = "0";

            IQueryable<AttRecord> querylinq;

            if (m_filterByDate && m_dateIsValid)
            {

                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

              

            }
            else
            {

                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Follow-Up"
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };
                
           
            }
            UpdateDataGrid(querylinq,query);
            Cursor = Cursors.Arrow;
        }

        
        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
            //generate list of all attended attendees
        

            Cursor = Cursors.Wait;
            string query = "0";
            chkResponded.IsChecked = false;
            chkFollowup.IsChecked = false;

            m_isAttendedChecked = true;
            //string date = m_DateSelected.ToString("MM-dd-yyyy");

            IQueryable<AttRecord> querylinq;

            if (m_filterByDate && m_dateIsValid)
            {


                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                where attinfo.Status == "Attended" && attinfo.Date == m_DateSelected
                                select new AttRecord {id = att.AttendeeId, fname=att.FirstName, lname=att.LastName, date=attinfo.Date, status=attinfo.Status};

              

            }
            else
            {

                querylinq =     from att in m_dbContext.Attendees.Local.AsQueryable()
                                join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                where attinfo.Status == "Attended"
                                select new AttRecord{id= att.AttendeeId, fname=att.FirstName, lname=att.LastName, date=attinfo.Date, status=attinfo.Status };


   
              
            }

            UpdateDataGrid(querylinq, query);
            Cursor = Cursors.Arrow;
        }

   



        private void Disable_Filters()
        {
            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;
            chkDateFilter.IsEnabled = false;
     

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

        private void UpdateDataGrid(IQueryable<AttRecord> linqquery,string query = "0")
        {




            if (query == "ShowDefaultTable")
            {
                dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                // (dataGrid.DataContext as DataTable).DefaultView.Sort = "FirstLastName ASC";
                dataGrid.Columns[0].Visibility = Visibility.Hidden;
                dataGrid.Columns[1].Visibility = Visibility.Hidden;
                //lblAttendenceMetrics.Content = m_DataSet.Tables["DefaultTable"].Rows.Count;

            }
            else if (query == "")
            {
                // do nothing

            }
            else if (linqquery != null )
            {
              

                if (m_DataSet.Tables.Contains("QueryTable"))
                {
                    m_DataSet.Tables.Remove("QueryTable");
                }

                DataTable queryTable = new DataTable("QueryTable");

                queryTable.Columns.Add(new DataColumn("AttendeeId"));
                queryTable.Columns.Add(new DataColumn("FirstLastName"));
                queryTable.Columns.Add(new DataColumn("First Name"));
                queryTable.Columns.Add(new DataColumn("Last Name"));
                queryTable.Columns.Add(new DataColumn("Date"));
                queryTable.Columns.Add(new DataColumn("Status"));


             
                    foreach (var rec in linqquery)
                    {

                        DataRow newrow = queryTable.NewRow();

                        

                        newrow["AttendeeId"] = rec.id;
                        newrow["FirstLastName"] = rec.fname + " " + rec.lname;
                        newrow["First Name"] = rec.fname;
                        newrow["Last Name"] = rec.lname;
                        newrow["Date"] = rec.date.ToString("MM-dd-yyyy");
                        newrow["Status"] = rec.status;


                        queryTable.Rows.Add(newrow);
                    }
                



                //Swap LastName and FirstName columns
               // queryTable.Columns[2].SetOrdinal(3);
                queryTable.AcceptChanges();
                m_DataSet.Tables.Add("QueryTable");


                dataGrid.DataContext = queryTable;
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; //LastFirstName



              
            }

        }


        private void chkAttended_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "0";

            m_isAttendedChecked = false;

            Cursor = Cursors.Wait;


            IQueryable<AttRecord> querylinq; 

            if (m_filterByDate && m_dateIsValid)
            {

                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

             
            }
            else
            {
                query = "ShowDefaultTable";
                querylinq = null;
            }

            UpdateDataGrid(querylinq,query);
            Cursor = Cursors.Arrow;
        }

        private void chkFollowup_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "0";
            m_isFollowupChecked = false;

            string date = m_DateSelected.ToString("MM-dd-yyyy");

            Cursor = Cursors.Wait;

            IQueryable<AttRecord> querylinq;

            if (m_filterByDate && m_dateIsValid)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

           

            }
            else
            {
                query = "ShowDefaultTable";
                querylinq = null;

            }

            UpdateDataGrid(querylinq,query);
            Cursor = Cursors.Arrow;

        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "0";
            m_isRespondedChecked = false;

            string date = m_DateSelected.ToString("MM-dd-yyyy");

            IQueryable<AttRecord> querylinq;
            if (m_filterByDate && m_dateIsValid)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

            }
            else
            {
                query = "ShowDefaultTable";
                querylinq = null;
            }

          UpdateDataGrid(querylinq,query);
            Cursor = Cursors.Arrow;
        }


        private void DateCalendar_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;

            Console.WriteLine($"{e.Source}");

            IQueryable<AttRecord> querylinq;

           

            string query = "0";

            if (calender == null)
            {
                if (m_dateIsValid)
                {


                    querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                where attinfo.Date == m_DateSelected
                                select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };


                    if (m_isAttendedChecked)
                    {
                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    where attinfo.Status == "Attended" && attinfo.Date == m_DateSelected
                                    select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

                    }
                    else if (m_isFollowupChecked)
                    {
                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected
                                    select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

                    }
                    else if (m_isRespondedChecked)
                    {
                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected
                                    select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };



                    }

                    UpdateDataGrid(querylinq, query);
                }

            }
            else
            {
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


                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    where attinfo.Date == m_DateSelected
                                    select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };


                        if (m_isAttendedChecked)
                        {
                            querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                        join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                        where attinfo.Status == "Attended" && attinfo.Date == m_DateSelected
                                        select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

                        }
                        else if (m_isFollowupChecked)
                        {
                            querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                        join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                        where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected
                                        select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

                        }
                        else if (m_isRespondedChecked)
                        {
                            querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                        join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                        where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected
                                        select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };



                        }

                        UpdateDataGrid(querylinq, query);
                    }
                }

            }
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
        


            Add_Blackout_Dates(ref calendar);

        }


      
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

        private void Save_Changes(object sender, RoutedEventArgs e)
        {
            
            int i = -1;

            bool haschanges = false;
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (m_DataSet.HasChanges())
            {

                Cursor = Cursors.Wait;
                foreach (DataRow dr in m_DataSet.Tables["DefaultTable"].Rows)
                {
                    i++;
                    // modified record ------------------------------------------------------------------------------------------
                    if (dr.RowState == DataRowState.Modified)
                    {
                        haschanges = true;
                        int AttendeeId = int.Parse(dr["AttendeeId"].ToString());

                        var Attendee = m_dbContext.Attendees.Local.Single(att => att.AttendeeId == AttendeeId);
                                       
                        if (Attendee !=null)
                        {
                            Attendee.LastName = dr["Last Name"].ToString().Trim();
                            Attendee.FirstName = dr["First Name"].ToString().Trim();
                        }               

                        
                            
                            

                        


                    }
                    // deleted rec ------------------------------------------------------------------------------------------
                    else if (dr.RowState == DataRowState.Deleted)
                    {
                        haschanges = true;
                        int attid = int.Parse(m_DataSet.Tables["DefaultTable"].Rows[i]["AttendeeId", DataRowVersion.Original].ToString());


                        var Attrec = m_dbContext.Attendees.Local.SingleOrDefault(id => id.AttendeeId == attid);

                        var queryAttendeeInfo = (from inforec in m_dbContext.Attendance_Info.Local
                                                 where inforec.AttendeeId == attid
                                                 select inforec).ToArray();

                        if (queryAttendeeInfo.Any())
                        {


                            for (int idx = 0; idx <= queryAttendeeInfo.Count() - 1; idx++)
                            {
                                m_dbContext.Attendance_Info.Local.Remove(queryAttendeeInfo[idx]);
                            }

                        }

                        m_dbContext.Attendees.Local.Remove(Attrec);


                    }
                    

                } // end foreach row

                                   
                    GenerateDBFollowUps();
                    m_dbContext.SaveChanges();
                    //m_DataSet.Reset();
                    m_DataSet.Tables["DefaultTable"].AcceptChanges();

                    //InitDataSet();
                    //Display_DefaultTable_in_Grid();
                    MessageBox.Show("Changes were saved succesfully.");
               

                Cursor = Cursors.Arrow;
            }
            else
            {
                MessageBox.Show("No changes to save.");
            }

        }

        private void DeleteRecord(object sender, RoutedEventArgs e )
        {
            var row_select = dataGrid.SelectedItems;


            if (dataGrid.DataContext == m_DataSet.Tables["DefaultTable"])
            {
               
                if (row_select.Count != 0)
                {
                  
                    Cursor = Cursors.Wait;
            

                    foreach (DataRowView drv in row_select)
                    {

                        int AttendeeId = int.Parse(drv.Row["AttendeeId"].ToString());
                     
                        var Attrec = m_dbContext.Attendees.Local.SingleOrDefault(id => id.AttendeeId == AttendeeId);

                        var queryAttendeeInfo = (from inforec in m_dbContext.Attendance_Info.Local
                                                where inforec.AttendeeId == AttendeeId
                                                select inforec).ToArray();

                            for (int idx=0;idx <=queryAttendeeInfo.Count()-1;idx++)
                            {
                                m_dbContext.Attendance_Info.Remove(queryAttendeeInfo[idx]);
                            }
                            
                            

                        m_dbContext.Attendees.Remove(Attrec);

                        
                    }
            
                    //m_dbContext.SaveChanges();
                    //InitDataSet();
                    //Display_DefaultTable_in_Grid();

                    MessageBox.Show("Attendee record(s) removed successfully.\nChanges has not been saved to the database until Save changes has been clicked.", "Remove Record", MessageBoxButton.OK, MessageBoxImage.None);
                }
                else
                {
                    MessageBox.Show("At least one record must be selected.", "Select Record", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                Cursor = Cursors.Arrow;
            }
        }

        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            {
                if (dr.RowState == DataRowState.Added)
                {

                }
            }
        }

        private void dataGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
               

            }
        }
        private void ImportRows_Click(object sender, RoutedEventArgs e)
        {


            Cursor = Cursors.Wait;

            bool haschanges = false;
            string date = m_alistDateSelected.ToString("MM-dd-yyyy");
            string firstname = "";
            string lastname = "";

            // add all attendee status and date to database

            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            // first pass through list and make sure everything looks good before making any changes to the db context

            foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            {

                  
                if (dr.RowState == DataRowState.Added)
                {
                    if (dr.ItemArray[2].ToString() == "" || dr.ItemArray[3].ToString() == "" || dr.ItemArray[4].ToString() == "" || dr.ItemArray[5].ToString() != "True")
                    {
                        MessageBox.Show("Please correct errors for attendee, check first name, last name, date and status is valid?", "Attendee Status", MessageBoxButton.OK, MessageBoxImage.Error);
                        dataGrid.Focus();
                        dataGrid.SelectedIndex = m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr);
                        dataGrid.ScrollIntoView(dataGrid.Items[m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr)]);

                        Cursor = Cursors.Arrow;
                        return;

                    }
                    else
                    {
                        firstname = dr["First Name"].ToString().ToUpper();
                        lastname = dr["Last Name"].ToString().ToUpper();

                        var queryAtt = (from AttRec in m_dbContext.Attendance_Info.Local
                                        where AttRec.Attendee.FirstName.ToUpper() == firstname && AttRec.Attendee.LastName.ToUpper() == lastname
                                        && AttRec.Date == m_alistDateSelected
                                        select AttRec).ToList().FirstOrDefault();


                        if (queryAtt != null)
                        {


                            MessageBox.Show("A record with the same attendee and date already exist. Please select a unique name or date.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                            dataGrid.Focus();
                            dataGrid.SelectedIndex = m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr);
                            dataGrid.ScrollIntoView(dataGrid.Items[m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr)]);
                            Cursor = Cursors.Arrow;
                            return;
                        }
                    }
           
                }
                else if (dr.RowState == DataRowState.Modified)
                {
                        if (dr.ItemArray[5].ToString() == "True")
                        {
                            if (!m_alistdateIsValid)
                            {
                                MessageBox.Show("Attendee date is not valid.", "Date invalid", MessageBoxButton.OK, MessageBoxImage.Stop);
                                dataGrid.Focus();
                                dataGrid.SelectedIndex = m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr);
                                dataGrid.ScrollIntoView(dataGrid.Items[m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr)]);
                                Cursor = Cursors.Arrow;
                                return;
                            }
                            else
                            {
                                firstname = dr["First Name"].ToString().ToUpper();
                                lastname = dr["Last Name"].ToString().ToUpper();
                            var queryAtt = (from AttRec in m_dbContext.Attendance_Info.Local
                                            where AttRec.Attendee.FirstName.ToUpper() == firstname && AttRec.Attendee.LastName.ToUpper() == lastname
                                            && AttRec.Date == m_alistDateSelected
                                            select AttRec).ToList().FirstOrDefault();


                                    if (queryAtt != null)
                                    {


                                        MessageBox.Show("A record with the same attendee and date already exist. Please select a unique name or date.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                                        dataGrid.Focus();
                                        dataGrid.SelectedIndex = m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr);
                                        dataGrid.ScrollIntoView(dataGrid.Items[m_DataSet.Tables["AttendeeListTable"].Rows.IndexOf(dr)]);
                                        Cursor = Cursors.Arrow;
                                        return;
                                    }
                            }

                        }
                 } //end modified



            }       



                foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                {

                    if (dr.RowState == DataRowState.Added)
                    {
                       
                  
                                // Add a new Attendee to context
                                Attendee newAttendeeRec = new Attendee();
                                Attendance_Info newAttInfoRec = new Attendance_Info();



                                newAttendeeRec.AttendeeId = m_NewAttendeeId;
                                newAttendeeRec.FirstName = dr["First Name"].ToString().Trim();
                                newAttendeeRec.LastName = dr["Last Name"].ToString().Trim();

                                string flname = newAttendeeRec.FirstName + " " + newAttendeeRec.LastName;

                            

                                newAttInfoRec.AttendeeId = m_NewAttendeeId;
                                newAttInfoRec.Date = m_alistDateSelected;
                                //newAttInfoRec.Last_Attended = m_alistDateSelected;

                                newAttInfoRec.Status = "Attended";

                                newAttendeeRec.AttendanceList.Add(newAttInfoRec);




                            m_NewAttendeeId += 1;
                                

                            // make new row in Default Table
                            //build row to import

                            DataRow newdr = m_DataSet.Tables["DefaultTable"].NewRow();
                            newdr.ItemArray = new object[] {   newAttendeeRec.AttendeeId,
                                                                   flname,
                                                                    dr["First Name"],
                                                                    dr["Last Name"],
                                                                    dr["Date"],
                                                                    newAttInfoRec.Status
                                                                };


                            m_DataSet.Tables["DefaultTable"].Rows.Add(newdr);

                            m_dbContext.Attendees.Add(newAttendeeRec);
                            m_dbContext.Attendance_Info.Add(newAttInfoRec);

                    haschanges = true;




                }
                    //======================= user checked the attended box next to an existing attendee already in the database
                    else if (dr.RowState == DataRowState.Modified)
                    {
                        if (dr.ItemArray[5].ToString() == "True")
                        {
                            int attid = int.Parse(dr["AttendeeId"].ToString());
                        Attendance_Info newRecord = new Attendance_Info { };

                            var queryAttendee = (from att in m_dbContext.Attendees.Local
                                                 where att.AttendeeId == attid
                                                 select att).ToArray().FirstOrDefault();


                            
                            newRecord.AttendeeId = attid;
                            newRecord.Date = m_alistDateSelected;
                          //  newRecord.Last_Attended = m_alistDateSelected;

                            var lastAttInfoRec = (from AttInfo in m_dbContext.Attendance_Info.Local
                                                  where AttInfo.AttendeeId == attid
                                                  orderby AttInfo.Date ascending
                                                  select AttInfo).ToArray().LastOrDefault();

                           

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
                
                     
                                m_dbContext.Attendance_Info.Add(newRecord);
                      


                        haschanges = true;
                     


                    }
                  

                    }
                }
             // end if m_datValid



            if (haschanges)
            {


               

                foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
                {
                    dr.ItemArray[5] = false;
                }
                m_DataSet.Tables["AttendeeListTable"].AcceptChanges();

                InitDefaultTable();
               // Display_DefaultTable_in_Grid();

                alisttxtSearch.Text = "";
                MessageBox.Show("Records succesfully added to active attendence list.");

            }
            else
            {
                MessageBox.Show("No changes to records, no records exported to active attendee list.");
            }






            Cursor = Cursors.Arrow;

        }

        private void InitDefaultTable()
        {
            string ldate = "";
            string lstatus = "";
           


            if (m_DataSet.Tables.Contains("DefaultTable"))
            {
               m_DataSet.Tables["DefaultTable"].Clear();


                var queryAttendees = from AttendeeRec in m_dbContext.Attendees.Local
                                     select AttendeeRec;


                    foreach (var AttendeeRec in queryAttendees)
                    {
                       
                        var queryLastDate = (from DateRec in AttendeeRec.AttendanceList
                                             where DateRec.Status == "Attended" || DateRec.Status == "Responded"
                                             orderby DateRec.Date ascending
                                             select DateRec).ToList().LastOrDefault();
                    
                            if (AttendeeRec.Prospect == 0)
                            {

                                    if (queryLastDate != null)
                                    {
                                        ldate = queryLastDate.Date.ToString("MM-dd-yyyy");
                                        lstatus = queryLastDate.Status;


                                        m_NewAttendeeId = AttendeeRec.AttendeeId;

                                        DataRow nrow = m_DataSet.Tables["DefaultTable"].NewRow();

                                        nrow["AttendeeId"] = AttendeeRec.AttendeeId;
                                        nrow["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;
                                        nrow["First Name"] = AttendeeRec.FirstName;
                                        nrow["Last Name"] = AttendeeRec.LastName;
                                        nrow["Date Last Attended"] = ldate;
                                        nrow["Status"] = lstatus;

                                         m_DataSet.Tables["DefaultTable"].Rows.Add(nrow);
                         

                                    }

                            }
                } // end foreach
            } // end foreach data defaulttable row

            //  m_DataSet.Tables["DefaultTable"].AcceptChanges();
            m_DataSet.Tables["DefaultTable"].Columns[2].SetOrdinal(3);

                lblProspectsMetrics.Content = m_DataSet.Tables["AttendeeListTable"].Rows.Count;

         
            
        }

              
        private void UpdateAttendeeListTableWithDateFilter()
        {

            string date;

            if (m_alistdateIsValid)
                date = m_alistDateSelected.ToString("MM-dd-yyyy");
            else
                date = "Date Not Valid.";

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
          

        }

        private void Uncheck_All_Filters()
        {
            chkFollowup.IsChecked = false;
            chkResponded.IsChecked = false;
            chkAttended.IsChecked = false;
            chkDateFilter.IsChecked = false;
            DateCalendar.IsEnabled = false;
        }



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

           
                     
                WndAttendeeInfo AttendeeInfoWindow = new WndAttendeeInfo(firstname, lastname, m_dbContext);
                

                
                
                AttendeeInfoWindow.ShowDialog();
            }



        }

        private void chkDateFiler_Checked(object sender, RoutedEventArgs e)
        {


            m_filterByDate = true;
            if (m_dateIsValid)
            {
               // DateCalendar_SelectedDateChanged(null, SelectionChangedEventArgs);
                txtDate.Text = m_DateSelected.ToString("MM-dd-yyyy");

            }
            txtDate.IsEnabled = true;
            DateCalendar.IsEnabled = true;



        }




        private void chkDateFiler_Unchecked(object sender, RoutedEventArgs e)
        {
            m_filterByDate = false;
            DateCalendar.IsEnabled = false;
            txtDate.IsEnabled = false;
            txtDate.Text = "Check date filter to select date.";

            string query = "0";
            Cursor = Cursors.Wait;
            IQueryable<AttRecord> querylinq;



            if (m_isAttendedChecked)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Attended"
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

              

            }
            else if (m_isFollowupChecked)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Follow-Up"
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };



            }
            else if (m_isRespondedChecked)
            {

                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Responded"
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

             
            }
            else
            {
                //all filters unchecked
                query = "ShowDefaultTable";
                querylinq = null;

            }



          UpdateDataGrid(querylinq, query);
            Cursor = Cursors.Arrow;

        }

     


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            m_alistView = false;
            m_AttendanceView = true;

            Uncheck_All_Filters();
            Enable_Filters();

          


            txtSearch.Text = "";
             txtDate.IsEnabled = false;
            m_dateIsValid = false;

            // commit datagrid edits and return DataContext to show all records
            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            //reset dataContext sow all records
            //(dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;
            //(dataGrid.DataContext as DataTable).DefaultView.Sort = "[Last Name] ASC";


            dataGrid.CanUserAddRows = false;
            //dataGrid.CanUserDeleteRows = false;
            dataGrid.ToolTip = "Left mouse click to select attendee.\nRight mouse click to see more in depth attendance for the selected attendee.";

            // Display_DefaultTable_in_Grid();


           
            lblProspectsMetrics.Content = m_DataSet.Tables["AttendeeListTable"].Rows.Count;


            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName

            }





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
            
            var selectedcells = sender as DataGrid;
            var currentCell = e.ClipboardRowContent[dataGrid.CurrentCell.Column.DisplayIndex];
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

             
                    if (m_DataSet.HasChanges())
                    {



                        MessageBoxResult result = MessageBox.Show("Changes has been made but not saved yet, exit anyway?", "Save Changes...", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                        {
                            e.Cancel = true;

                        }
                        else
                        {
                          

                            e.Cancel = false;

                        }
                    }
                

            }
        }
        private void btnChart_Click(object sender, RoutedEventArgs e)
        {
            ChartWindow wndCharts = new ChartWindow(m_dbContext);
            wndCharts.Show();

        }

        private void RibbonApplicationMenuItem_Click_About(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.ShowDialog();
            
        }

        private void RibbonApplicationMenuItem_Click_Exit(object sender, RoutedEventArgs e)
        {

            this.Close();
        }



        private void Update_Status()
        {



            string query = "0";
            //string date = m_DateSelected.ToString("MM-dd-yyyy");

            IQueryable<AttRecord> querylinq;

            querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        where attinfo.Date == m_DateSelected
                        select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

         
            if (m_isAttendedChecked)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Attended" && attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };


            }
            else if (m_isFollowupChecked)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

            }
            else if (m_isRespondedChecked)
            {
                querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                            where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected
                            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, status = attinfo.Status };

            }

            UpdateDataGrid(querylinq, query);


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


                
                (dataGrid.DataContext as DataTable).DefaultView.RowFilter = String.Empty;
                m_tempTable = (DataTable)dataGrid.DataContext;
                

                m_alistView = true;
                m_AttendanceView = false;
                DateCalendar.IsEnabled = true;

                // alisttxtSearch.Text = "";

                if (m_alistdateIsValid)
                {
                    UpdateAttendeeListTableWithDateFilter();
                }
                // alisttxtDate.Text = "";
                //  m_alistdateIsValid = false;
                dataGrid.CanUserAddRows = true;
                dataGrid.CanUserDeleteRows = false;

                dataGrid.ToolTip = null;

                Display_AttendeeListTable_in_Grid();

                lblProspectsMetrics.Content = m_DataSet.Tables["AttendeeListTable"].Rows.Count;







            }
            //--------Home Tab -----------------------------------------------------------------------------------------
            else if (tabHome.IsSelected)
            {
                // commit datagrid edits and return DataContext to show all records
                if (dataGrid.Columns.Count > 1)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                }

                  


                m_alistView = false;
                m_AttendanceView = true;

                if (m_dateIsValid || chkDateFilter.IsChecked == true || chkAttended.IsChecked == true || chkFollowup.IsChecked == true || chkResponded.IsChecked == true)
                {
                    if (txtSearch.Text != "")
                    {
                        m_tempTable.DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                        dataGrid.DataContext = m_tempTable;
                    }
                    else
                    {
                        dataGrid.DataContext = m_tempTable;
                    }
                    
                }
                else
                {
                   
                    if (txtSearch.Text != "")
                    {
                        m_tempTable.DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                        dataGrid.DataContext = m_tempTable;
                    }
                    else
                    {
                        Display_DefaultTable_in_Grid();
                    }
                    
                    
                }

                if (dataGrid.Columns.Count > 1)
                {
                   dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                   dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                }

                dataGrid.CanUserAddRows = false;
               // dataGrid.CanUserDeleteRows = false;
                dataGrid.ToolTip = "Left mouse click to select attendee.\nRight mouse click to see more in depth attendance for the selected attendee.";
                lblProspectsMetrics.Content = m_DataSet.Tables["AttendeeListTable"].Rows.Count;

                

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

                    var queryAtt = (from AttRec in m_dbContext.Attendees
                                    where AttRec.AttendeeId == attID
                                    select AttRec).FirstOrDefault();

                    MessageBoxResult res = MessageBox.Show($"Are you sure you want to add '{queryAtt.FirstName} {queryAtt.LastName}' to the prospect list?", "Add Prospect Attendee", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.OK)
                    {
                        queryAtt.Prospect = 1;
                        m_dbContext.SaveChanges();
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





