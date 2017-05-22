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

namespace CAOGAttendeeProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>



    public class Attendee
    {
        public int AttendeeId { get; set; }
        public string Last_Attended { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public virtual List<Attendance_Info> AttendanceList { get; set; }
    }

    public class Attendance_Info
    {

        public int Attendance_InfoId { get; set; }
        public int AttendeeId { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }



    }

    public class CAOGAttendeeDB : DbContext
    {
        public CAOGAttendeeDB() : base("COAGAttendeeDB")
        { }

        public DbSet<Attendee> AttendeeTbl { get; set; }
        public DbSet<Attendance_Info> Attendance_InfoTbl { get; set; }

    }
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            // CreateDatabase_FromXLSX();
            Display_Database_in_Grid();




        }

        private string m_AttendeeID = "";
        private int m_lastAttendeeID = 0;
        private int m_lastAttendanceInfoID = 0;
        private DataSet m_DataSet = null;


        private SqlConnection m_mySqlConnection = null;

        public static string AttendeeID { get; }

        private void CreateDatabase_FromXLSX()
        {
            // create Database from Excel Sheet

            using (var db = new CAOGAttendeeDB())
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
                        string year = "", last_attended = "";
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
                                    db.AttendeeTbl.Add(churchAttendee);
                                    db.Attendance_InfoTbl.Add(Attendee_Status);
                                }
                                // create new attendee
                                churchAttendee = new Attendee { AttendanceList = new List<Attendance_Info> { } };
                                attID++;

                                year = oleDataReader.GetName(1);
                                last_attended = oleDataReader[2].ToString();
                                FLname = oleDataReader[1].ToString().Split(' ');

                                churchAttendee.FirstName = FLname[0];
                                churchAttendee.LastName = FLname[1];
                                churchAttendee.Last_Attended = last_attended;
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

                                        Attendee_Status.Attendance_InfoId = att_infoID;
                                        Attendee_Status.AttendeeId = attID;
                                        Attendee_Status.Date = datetime;

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
                                                churchAttendee.AttendanceList[attLst_Idx].Status = (oleDataReader[col_index + 1].ToString() == "1") ? "Contacted" : "Not Attended";
                                                attLst_Idx++;
                                                break;
                                            }
                                        case 3: //Responded
                                            {
                                                churchAttendee.AttendanceList[attLst_Idx].Status = (oleDataReader[col_index + 2].ToString() == "1") ? "Responded" : "Not Attended";
                                                attLst_Idx++;
                                                break;
                                            }
                                        default:
                                            break;


                                    }
                                   

                                } // end col_index % 4
                            }    // end for col_index


                        } // end data reader read
                        db.AttendeeTbl.Add(churchAttendee);
                        db.Attendance_InfoTbl.Add(Attendee_Status);
                        db.SaveChanges();
                        oleDataReader.Close();
                    } // end try




                    catch (Exception ex)
                    {
                        Console.Write("{0}", ex);

                    }





                } // end using oleconnection


            } // end using db



        } // end sub

        private void Display_Database_in_Grid()
        {
            string myConnectionString = "Data Source=AESTERH-PC\\SQLEXPRESS;Initial Catalog=COAGAttendeeDB;Integrated Security=True;" +
                                        "Connect Timeout=15;Encrypt=False;TrustServerCertificate=True;" +
                                        "ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            string mysqlstring = "SELECT * FROM Attendees";
            string sqlAttendanceInfo = "SELECT * FROM Attendance_Info";

            SqlConnection myConnection = new SqlConnection(myConnectionString);

            m_mySqlConnection = myConnection;

            try
            {
                myConnection.Open();
                Console.WriteLine($"Database Successfully opened!");

                SqlDataAdapter myAdapter = new SqlDataAdapter(mysqlstring, myConnection);
                SqlDataAdapter myAdapter2 = new SqlDataAdapter(sqlAttendanceInfo, myConnection);

                DataSet ds = new DataSet();
                DataTable dt = new DataTable();

                myAdapter.Fill(ds);
                myAdapter2.Fill(dt);

                ds.Tables.Add(dt);

                m_DataSet = ds;

                dataGridQ1.DataContext = ds.Tables[0];



            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred! {ex}");
            }







        } // end  private void Display_Database_in_Grid()


        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "Hello";
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void txtAddUser_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void chkResponded_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void chkContacted_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void dataGridQ1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            var grid = sender as DataGrid;
            IList<DataGridCellInfo> CellV = grid.SelectedCells;

            DataRowView RowView = (DataRowView)CellV[0].Item;

            m_AttendeeID = RowView.Row[0].ToString();

            WndAttendeeInfo AttendeeInfoWindow = new WndAttendeeInfo(m_AttendeeID, m_mySqlConnection);
            AttendeeInfoWindow.Show();


        }

        private void mnuItemExit_Click(object sender, RoutedEventArgs e)
        {
            m_mySqlConnection.Close();
        }

        private void btn_AddAttendee_Click(object sender, RoutedEventArgs e)
        {


            Regex chkFormat = new Regex(@"^\s*\w+\s+\w+\s*,\s*[0-9]{2}/[0-9]{2}/[0-9]{4}\s*,\s*\w+$");

            if (chkFormat.IsMatch(txtbox_addAttendee.Text))
            {
                // get last AttendeeId in datagrid
                m_lastAttendeeID = m_DataSet.Tables[0].Rows.Count;
                m_lastAttendanceInfoID = m_DataSet.Tables[1].Rows.Count;


                string[] aryNewAttendeeInfo = txtbox_addAttendee.Text.Split(',');
                string[] subDate = aryNewAttendeeInfo[1].Split('/');
                string[] aryName = aryNewAttendeeInfo[0].Split(' ');

                string year = subDate[2].Trim();
                string month = subDate[0].Trim();
                string day = subDate[1].Trim();

            

                string status = aryNewAttendeeInfo[2].Trim();
               

    

                DateTime NewDate = new DateTime(Int32.Parse(year), Int32.Parse(month),Int32.Parse(day) );

                using (var db = new CAOGAttendeeDB() )
                {
                    Attendee newChurchAttendee = new Attendee { AttendanceList = new List<Attendance_Info> { } };
                    Attendance_Info newChurchAttendeeInfo = new Attendance_Info { };

                    

                    newChurchAttendee.FirstName = aryName[0].Trim();
                    newChurchAttendee.LastName = aryName[1].Trim();
                    newChurchAttendee.AttendeeId = m_lastAttendeeID + 1;

                    newChurchAttendeeInfo.Attendance_InfoId = m_lastAttendanceInfoID + 1;
                    newChurchAttendeeInfo.Date = NewDate;
                    newChurchAttendeeInfo.
                    


                }

            }
            else
            {
                MessageBox.Show("Input string format not correct");

            }
        }
    } // end MainWindow
  }        



