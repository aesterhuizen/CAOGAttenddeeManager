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
    //public class CAOGAttendeeDbModel : DbContext
    //{
    //    // Your context has been configured to use a 'CAOGAttendeeDbModel' connection string from your application's 
    //    // configuration file (App.config or Web.config). By default, this connection string targets the 
    //    // 'WpfApplication2.CAOGAttendeeDbModel' database on your LocalDb instance. 
    //    // 
    //    // If you wish to target a different database and/or database provider, modify the 'CAOGAttendeeDbModel' 
    //    // connection string in the application configuration file.
    //    public CAOGAttendeeDbModel() : base("name=CAOGAttendeeDbModel")

    //    {
    //        Database.SetInitializer<CAOGAttendeeDbModel>(null);
    //    }

    //    // Add a DbSet for each entity type that you want to include in your model. For more information 
    //    // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

    //    public virtual DbSet<Attendee> Attendees { get; set; }
    //    public virtual DbSet<Attendance_Info> Attendance_Info { get; set; }
    //}

    //public class Attendee
    //{
    //    public int AttendeeId { get; set; }
    //    public string FirstName { get; set; }
    //    public string LastName { get; set; }
    //    public virtual List<Attendance_Info> AttendanceList { get; set; }
    //}

    //public class Attendance_Info
    //{

    //    public int Attendance_InfoId { get; set; }
    //    public int AttendeeId { get; set; }
    //    public DateTime Last_Attended { get; set; }
    //    public DateTime Date { get; set; }
    //    public string Status { get; set; }



    //}



    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            // Clear_DB_Tables();
            //CreateDatabase_FromXLSX();

            //GenerateDBFollowUps();
           Display_Database_in_Grid();

            
            

        }

        //private string m_AttendeeID = "";
        

        private int m_lastAttendeeID = 0;
        private int m_lastAttendanceInfoID = 0;
        private DataSet m_DataSet = null;
        private string m_constr = "";
        private string m_searchStr = "";
        private string m_FirstName = "";
        private string m_LastName = "";
        private string m_defaultSqlStr = "";
        private bool m_NonChecked = true;

        private SqlConnection m_mySqlConnection = null;

        

        private void Clear_DB_Tables()
        {
            string myConnectionString = "Data Source = (LocalDB)\\MSSQLLocalDB;AttachDbFilename =C:\\Program Files\\Microsoft SQL Server\\MSSQL13.SQLEXPRESS\\MSSQL\\DATA\\C1.mdf;Integrated Security = True; Connect Timeout = 30";


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
            //1)look and each AttendeeId and see if he\she attended 4 weeks (28 days) in the past from this week's Sunday
            //2)if he/she did attend, generate a new database entry with status follow-up for corresponding AttendeeId
            //3)get a list of sunday dates for the specific year


            //int[] aryAttId = new int[m_DataSet.Tables[0].Rows.Count * 12];
            int attInfoId = 0;


            //m_constr = "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=C:\\Program Files\\Microsoft SQL Server\\MSSQL13.SQLEXPRESS\\MSSQL\\DATA\\Database.mdf;Integrated Security=True;Connect Timeout=30";
            InitDataSet();

            attInfoId = m_lastAttendanceInfoID;

            
            

            using (var db = new ModelDb(m_constr))
            {

                DateTime curdate = DateTime.Now;
                

                
                var queryAttInfo = from AttInfo_record in db.Attendance_Info
                                   where AttInfo_record.Status == "Attended" && AttInfo_record.Date == AttInfo_record.Last_Attended
                                   select AttInfo_record;

                var queryAttendee = from AttRecord in db.Attendees select AttRecord;

                
                
                foreach (var attRecord in queryAttendee)
                {
                    foreach (var attInfoRecord in queryAttInfo)
                    {
                        
                        DateTime followUpDate = (DateTime)attInfoRecord.Last_Attended;

                        if (curdate == followUpDate.AddDays(28) )
                        {

                            
                            attInfoId++;
                            Attendee newChurchAttendee = new Attendee { };
                            Attendance_Info newChurchAttendeeInfo = new Attendance_Info { };

                            newChurchAttendee.FirstName = attRecord.FirstName;
                            newChurchAttendee.LastName = attRecord.LastName;
                            newChurchAttendee.AttendeeId = attRecord.AttendeeId;

                            newChurchAttendeeInfo.Attendance_InfoId = attInfoId;
                           // newChurchAttendeeInfo.Attendee_AttendeeId = attRecord.AttendeeId;
                            newChurchAttendeeInfo.Date = followUpDate;
                            newChurchAttendeeInfo.Status = "Follow-Up";
                            newChurchAttendeeInfo.Last_Attended = attInfoRecord.Last_Attended;
                            newChurchAttendee.AttendanceList.Add(newChurchAttendeeInfo);

                            db.Attendees.Add(newChurchAttendee);
                            db.Attendance_Info.Add(newChurchAttendeeInfo);
                        }
                     }
                }
                db.SaveChanges();

                //Console.WriteLine($"aryAttId = {aryAttId.Count()}");

                //SqlCommand sqlCom = new SqlCommand(query, m_mySqlConnection);

                ////SqlDataAdapter dbAdapterAttended = new SqlDataAdapter(query, m_mySqlConnection);
                //SqlDataReader sqlReader = sqlCom.ExecuteReader();

                //while (sqlReader.Read() )
                //{


                //}






                //string[] aryNewAttendeeInfo = txtbox_addAttendee.Text.Split(',');
                //string[] subDate = aryNewAttendeeInfo[1].Split('/');
                //string[] aryName = aryNewAttendeeInfo[0].Split(' ');
                //string[] lstDate = aryNewAttendeeInfo[3].Split('/');


                //string year = subDate[2].Trim();
                //string month = subDate[0].Trim();
                //string day = subDate[1].Trim();

                //string lstyear = lstDate[2].Trim();
                //string lstmonth = lstDate[0].Trim();
                //string lstday = lstDate[1].Trim();


                //string status = aryNewAttendeeInfo[2].Trim();




                //DateTime NewDate = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));
                //DateTime lstAttendedDate = new DateTime(Int32.Parse(lstyear), Int32.Parse(lstmonth), Int32.Parse(lstday));





            }
                       





        }
        private void CreateDatabase_FromXLSX()
        {
            // create Database from Excel Sheet
            //m_constr = "Data Source=caogserver.database.windows.net;Initial Catalog=caogattendeedb;User ID=sqladmin;Password=ASdfGH12#$";
            m_constr = "Server=tcp:caogserver.database.windows.net,1433;Initial Catalog=TestDb;Persist Security Info=False;User ID=sqladmin;Password=ASdfGH12#$;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";

            using (var db = new ModelDb(m_constr) )
            {

                
                Console.WriteLine("Openning Excel datasheet...");

                String connectionString = "Provider=Microsoft.ACE.OLEDB.16.0;" +
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
                                        //Attendee_Status.Attendee_AttendeeId = attID;
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
        private void Display_Database_in_Grid()
        {
            InitDataSet();

            dataGrid.DataContext = m_DataSet.Tables[2];




        } // end  private void Display_Database_in_Grid()

        private void InitDataSet()
        {
           // m_constr = "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\Database.mdf;Integrated Security=True;Trusted_Connection=True;Connect Timeout = 30";

            m_constr = "Server=tcp:caogserver.database.windows.net,1433;Initial Catalog=TestDb;Persist Security Info=False;User ID=sqladmin;Password=ASdfGH12#$;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";

            string mysqlstring = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                            "ORDER BY Date ASC";

            string sqlAttendees = "SELECT * FROM Attendees";
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

        }
       

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {

           
            if (e.Key == Key.Return)
            {

               btnSearch_Click(sender, e);
            }
            
        }

       

        private void chkResponded_Checked(object sender, RoutedEventArgs e)
        {

            chkAttended.IsChecked = false;
            chkContacted.IsChecked = false;
            string query = "";
            m_NonChecked = false;

            if (m_searchStr != "")
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendee_AttendeeId " +
                       "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
                       "Attendance_Info.Status='Responded' ORDER BY Date ASC";
            }
            else
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                            "FROM Attendees " +
                            "INNER JOIN Attendance_Info " +
                            "ON Attendees.AttendeeId=Attendee_AttendeeId " +
                            "WHERE Status='Responded' ORDER BY Date ASC";
            }


            UpdateDataGrid(query);
            
            

        
        }

        private void chkContacted_Checked(object sender, RoutedEventArgs e)
        {
            chkAttended.IsChecked = false;
            chkResponded.IsChecked = false;
            string query = "";
            m_NonChecked = false;

            if (m_searchStr != "")
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                        "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
                        "Attendance_Info.Status='Follow-Up' ORDER BY Date ASC";
            }
            else
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                        "WHERE Attendance_Info.Status='Follow-Up' ORDER BY Date ASC"; ;
            }



            UpdateDataGrid(query);

            
        }

        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
            //generate list of all attended attendees
            string query = "";

            chkResponded.IsChecked = false;
            chkContacted.IsChecked = false;
            m_NonChecked = false;

            if (m_searchStr != "")
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
                       "Attendance_Info.Status='Attended' ORDER BY Date ASC";
            }
            else
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Status='Attended' ORDER BY Date ASC";
            }






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

            UpdateDataGrid(query);

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

            WndAttendeeInfo AttendeeInfoWindow = new WndAttendeeInfo(m_FirstName,m_LastName, m_mySqlConnection);
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


                    Attendee newChurchAttendee = new Attendee {};
                    Attendance_Info newChurchAttendeeInfo = new Attendance_Info { };



                    newChurchAttendee.FirstName = aryName[0].Trim();
                    newChurchAttendee.LastName = aryName[1].Trim();
                    newChurchAttendee.AttendeeId = m_lastAttendeeID + 1;

                    newChurchAttendeeInfo.Attendance_InfoId = m_lastAttendanceInfoID + 1;
                    //newChurchAttendeeInfo.AttendeeNum = m_lastAttendeeID + 1;
                    newChurchAttendeeInfo.Date = NewDate;
                    newChurchAttendeeInfo.Status = status;
                    newChurchAttendeeInfo.Last_Attended = lstAttendedDate;

                    db.Attendees.Add(newChurchAttendee);
                    db.Attendance_Info.Add(newChurchAttendeeInfo);
                    db.SaveChanges();
                }
                chkAttended.IsChecked = false;
                chkContacted.IsChecked = false;
                chkResponded.IsChecked = false;

                UpdateDataGrid(m_defaultSqlStr);
                
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

        private void txtSearch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MouseButtonState.Pressed == e.LeftButton)
            {
                txtSearch.Text = "";
            }
            
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string[] aryFirstLastName = txtSearch.Text.Split(' ');
            m_FirstName = aryFirstLastName[0];
            m_LastName = aryFirstLastName[1];
            m_searchStr = txtSearch.Text;

            string query = "";
            if (chkAttended.IsChecked == true)
            {
                 query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                        "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
                        "Attendance_Info.Status='Attended' ORDER BY Date ASC";
            }
            else if (chkContacted.IsChecked == true)
            {

                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
                       "Attendance_Info.Status='Follow-Up' ORDER BY Date ASC";

            }
            else if (chkResponded.IsChecked == true)
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" + " AND " +
                       "Attendance_Info.Status='Responded' ORDER BY Date ASC";
            }
            else
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                       "FROM Attendees " +
                       "INNER JOIN Attendance_Info " +
                       "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                       "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "' ORDER BY Date ASC";
            }



            UpdateDataGrid(query);

        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch.Text == "")
            {
                btnSearch.IsEnabled = false;
                m_searchStr = "";
                if (m_NonChecked == true)
                {
                    dataGrid.DataContext = m_DataSet.Tables[2];
                }
                
            }
            else
            {
                btnSearch.IsEnabled = true;
            }
            Console.WriteLine($"NonChecked = {m_NonChecked}");
            Console.WriteLine($"txtSearch = {txtSearch.Text }");
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

            if (chkAttended.IsChecked == false && chkContacted.IsChecked == false && chkResponded.IsChecked == false && m_searchStr != "")
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                        "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" +
                        " ORDER BY Date ASC";

                UpdateDataGrid(query);
            }
            else
            {
                dataGrid.DataContext = m_DataSet.Tables[2];
            }
        }

        private void chkContacted_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_NonChecked = true;

            if (chkAttended.IsChecked == false && chkContacted.IsChecked == false && chkResponded.IsChecked == false && m_searchStr != "")
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                        "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" +
                        " ORDER BY Date ASC";

                UpdateDataGrid(query);
            }
            else
            {
                dataGrid.DataContext = m_DataSet.Tables[2];
            }
        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {
            string query = "";
            m_NonChecked = true;

            if (chkAttended.IsChecked == false && chkContacted.IsChecked == false && chkResponded.IsChecked == false && m_searchStr != "")
            {
                query = "SELECT Attendees.FirstName,Attendees.LastName, Attendance_Info.Last_Attended, Attendance_Info.Date, Attendance_Info.Status " +
                        "FROM Attendees " +
                        "INNER JOIN Attendance_Info " +
                        "ON Attendees.AttendeeId=Attendance_Info.Attendee_AttendeeId " +
                        "WHERE Attendees.FirstName='" + m_FirstName + "'" + " AND " + "Attendees.LastName='" + m_LastName + "'" +
                        " ORDER BY Date ASC";

                UpdateDataGrid(query);
            }
            else
            {
                dataGrid.DataContext = m_DataSet.Tables[2];
            }
        }

        
    } // end MainWindow
  }        



