using System;
using System.IO;

//using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

            InitActivityTreeView();




#if (DEBUG)
            this.Title = "CAOG Attendee Manager (Debug)";
#endif


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

                    m_dbContext = new ModelDb(m_constr);

                    // correctDBerrors();

                    if (m_dbContext.Attendees.Count() == 0)
                    {
                        //CreateDatabase_FromXLSX();

                        InitDataSet();
                        Display_DefaultTable_in_Grid();
                    }
                    else
                    {

                        InitDataSet();
                      
                       
                        Display_DefaultTable_in_Grid();
                    }

                }
                else
                {

                    MessageBox.Show("Cannot connect to database, credential file does not exist!", "File does not exist.", MessageBoxButton.OK, MessageBoxImage.Error);
                    m_NoCredFile = true;
                    this.Close();
                }


                //  SetTimer();


            }
            catch (Exception ex)
            {

                MessageBox.Show($"Exception occurred when performing database initialization { ex}!\n");
            }












        }




        private ModelDb m_dbContext;
       
        private DateTime m_DateSelected;
        private DateTime m_alistDateSelected;
        private DateTime? m_ActivityDateSelected;
        private DataSet m_DataSet = new DataSet();

       
       
        
        // current selected row in the data tables
        private ActivityTableRow m_activity_row_selected;
        private DefaultTableRow m_default_row_selected;
        private AttendanceTableRow m_attendance_row_selected;
      
        // pointer to datagrid within RowDetailsTemplate
        DataGrid m_AttendeeInfo_grid = null;
        DataGrid m_Activity_grid = null;

        //List of query rows
        private List<DefaultTableRow> m_lstQueryTableRows = new List<DefaultTableRow>() { };
        //list of activity Rows
        private List<ActivityTableRow> m_lstactivityTableRows = new List<ActivityTableRow>() { };
        //List of default Table rows
        private List<DefaultTableRow> m_lstdefaultTableRows = new List<DefaultTableRow>() { };

        private List<AttendanceTableRow> m_lstattendanceTableRows = new List<AttendanceTableRow>() { };
        //list of Activities
        private List<ActivityGroup> m_lstActivities = new List<ActivityGroup> { };
        private List<ActivityTask> m_lstActivityTask = new List<ActivityTask> { };

        private TabState m_TabState = new TabState();
        //Activity control
        string m_ActivityName = "";
        int m_old_ActivityId = 0;
        ActivityTask m_currentSelected_ActivityTask = null;
        ActivityTask m_previousSelected_ActivityTask = null;
        int m_child_taskId = 0;
        int m_parent_taskId = 0;

        // the current selected activity Pair
        ActivityPair m_currentSelected_ActivityPair = null;
        ActivityPair m_previousSelected_ActivityPair = null;
        private Timer aTimer;

        private bool m_NoCredFile = false;
        private int m_activitychecked_count = 0;

        //filter state
        private bool m_isActivityfilterByDateChecked = false;
        private bool m_isFilterByDateChecked = false;
        private bool m_isChurchStatusFilterChecked = false;
        private bool m_isAttendedChecked = false;
        private bool m_isFollowupChecked = false;
        private bool m_isRespondedChecked = false;
        private bool m_isActivityFilterChecked = false;
        private bool m_isActivityChecked = false;
        private bool m_isQueryTableShown = false;
        // view state
        private bool m_alistView = false;
        private bool m_AttendanceView = false;
        private bool m_activityView = false;
        //panel state
        private bool m_IsActivePanelView = false;
        private bool m_IsPanelProspectView = false;
        private bool m_IsActivityPanelView = false;
        
        private string[] m_ary_ActivityStatus = new string[10];
        private string m_old_attendeeId = "";
        private bool m_dateIsValid = false;
        private bool m_alistdateIsValid = false;




        private string m_constr = "";

        // private List<string> m_lstdataGridHeadersClicked = new List<string> { };
        private int m_NewAttendeeId = 0;

        private void Set_btnAddActivityState()
        {
            if (m_activitychecked_count == 1 && m_ActivityDateSelected !=null)
            {
                btnPanelAddActivity.IsEnabled = true;
            }
            else
            {
                btnPanelAddActivity.IsEnabled = false;
            }
        }
        delegate void setbtn_state();

        

        private void SetTimer()
        {
            aTimer = new Timer(100);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(Set_btnAddActivityState);
            



           }

        private void Create_new_DB_with_AutoGenerated_AttendeeIds()
        {


            string newDB_constring = "Data Source=caogserver.database.windows.net;Initial Catalog=TestDb_2018-02-17_Copy;Integrated Security=False;User ID=sqladmin;Password=RFtgYH56&*;" +
                "MultipleActiveResultSets = True; Connect Timeout =60;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";


            string qstr1 = "SELECT * FROM Attendees";
            string qstr2 = "SELECT * FROM Attendance_Info";



            using (SqlConnection myconn = new SqlConnection(newDB_constring))
            {
                SqlCommand sqlcmd = new SqlCommand(qstr1, myconn);
                // SqlCommand sqlcmd2 = new SqlCommand(qstr2,myconn);

                myconn.Open();
                //SqlDataAdapter oldDb_Attendee_adapter = new SqlDataAdapter(sqlcmd, newDB_constring);

                //DataTable oldDB_Attendee_dt = new DataTable();
                //oldDb_Attendee_adapter.Fill(oldDB_Attendee_dt);

                //oldDb_Attendee_adapter.Dispose();

                SqlDataAdapter oldDB_AttInfo_adapter = new SqlDataAdapter(qstr2, myconn);
                DataTable oldDB_AttInfo_dt = new DataTable();

                oldDB_AttInfo_adapter.Fill(oldDB_AttInfo_dt);

                //oldDB_AttInfo_adapter.Dispose();

                SqlDataReader AttendeeReader = sqlcmd.ExecuteReader();
                // SqlDataReader AttInfoReader = sqlcmd2.ExecuteReader();

                int year = 0;
                int month = 0;
                int day = 0;
                string[] bitstr;
                string[] datestr;


                Attendee newAttendeeRec = null;
                Attendance_Info newAttInfoRec = null;


                for (int i = 0; i <= 2; i++)
                {

                    AttendeeReader.Read();
                    //foreach (DataRow AttendeeRec in oldDB_Attendee_dt.Rows)
                    //{

                    int attID = int.Parse(AttendeeReader["AttendeeId"].ToString());

                    newAttendeeRec = new Attendee
                    {
                        FirstName = AttendeeReader["FirstName"].ToString(),
                        LastName = AttendeeReader["LastName"].ToString()
                    };


                    foreach (DataRow Attendee_InfoRec in oldDB_AttInfo_dt.Rows)
                    {



                        if (int.Parse(Attendee_InfoRec["AttendeeId"].ToString()) == attID)
                        {

                            newAttInfoRec = new Attendance_Info();

                            bitstr = Attendee_InfoRec["Date"].ToString().Split(' ');

                            datestr = bitstr[0].Split('/');

                            year = int.Parse(datestr[2]);
                            month = int.Parse(datestr[0]);
                            day = int.Parse(datestr[1]);
                            DateTime datetime = new DateTime(year, month, day);

                            newAttInfoRec.Date = datetime;
                            newAttInfoRec.Status = Attendee_InfoRec["Status"].ToString();

                            newAttendeeRec.AttendanceList.Add(newAttInfoRec);
                            m_dbContext.Attendance_Info.Add(newAttInfoRec);

                        }


                    }
                    m_dbContext.Attendees.Add(newAttendeeRec);


                }




                m_dbContext.SaveChanges();
                Console.WriteLine("Database Successfully populated!");
            }
        }


        private void correctDBerrors()
        {
            DateTime latest_date = new DateTime(2017, 11, 26);

            var daterec = from d in m_dbContext.Attendance_Info
                          where d.Status == "Follow-Up"
                          select d;

            foreach (var rec in daterec)
            {
                string[] arydate = rec.Date.ToString().Split();
                string date = arydate[0];


                if (date == "11/26/2017")
                {
                    Console.WriteLine("Found date 11-05-2017");
                    //attInforec.Date = correcteddate;

                }

            }




            // m_dbContext.SaveChanges();

            //string header = String.Format("{0,-30}{1,-30}{2,10}{3,12}\n",
            //                            "LastName","FirstName","Date","Status");
            //Console.WriteLine(header);
            //foreach (var rec in daterec)
            //{
            //    string outputformat = String.Format("{0:-30} {1:30} {2:10} {3:-12}\n",
            //                                        rec.Attendee.LastName, rec.Attendee.FirstName, rec.Date.ToString("MM-dd-yyyy"), rec.Status,rec.Attendee.Prospect);
            //    Console.WriteLine(outputformat);

            //    m_dbContext.Attendance_Info.Remove(rec);
            //}

            //m_dbContext.SaveChanges();
            //var queryAttendees = from AttendeeRec in m_dbContext.Attendance_Info
            //                     where AttendeeRec.Status == "Follow-Up"
            //                     select AttendeeRec;

            //var lstInfoRecs = from InfoRec in m_dbContextContext.Attendance_Info
            //                  where InfoRec.Date == date && InfoRec.Status == "Responded"
            //                  select InfoRec;


            //foreach (var attInforec in queryAttendees)
            //{
            //    //var lstInfoRecs = (from InfoRec in attrec.AttendanceList
            //    //                   where InfoRec.Status == "Follow-Up"
            //    //                   select InfoRec).ToArray().LastOrDefault();



            //    string[] arydate = attInforec.Date.ToString().Split();
            //    string date = arydate[0];


            //    if (date == "11/05/2017")
            //    {
            //        Console.WriteLine("Found date 11-05-2017");
            //        //attInforec.Date = correcteddate;

            //    }
            //}

            ////int count = lstInfoRecs.Count();


            //if (count >=2)
            //{
            //    for (int idx = 0; idx <= count - 1; idx++)
            //    {
            //        if (lstInfoRecs[idx].Date == date && lstInfoRecs[idx].Status == "Responded")
            //        {
            //            TimeSpan tspan = lstInfoRecs[idx].Date - lstInfoRecs[idx - 1].Date;


            //            if (tspan.Days < 22 && lstInfoRecs[idx - 1].Status == "Responded")
            //            {
            //                lstInfoRecs[idx].Status = "Attended";

            //            }
            //        }
            //        //if (lstInfoRecs[idx].Date == date && lstInfoRecs[idx].Status == "Responded")
            //        //{


            //        //    if (lstInfoRecs[idx+1].Status=="Responded" && lstInfoRecs[idx + 1].Status == "Responded")
            //        //    {
            //        //        m_dbContextContext.Attendance_Info.Remove(lstInfoRecs[idx + 1]);

            //        //    }
            //        //}

            //    }
            //} 




            // m_dbContext.SaveChanges();


        }
        // public void PrintTree(BinaryTreeNode root, TreeTraversal treeTraversal)
        // {
        //     int count = 1;  
        //     BinTreeTraversal(root, ref count);

        // }
        // public void BinTreeTraversal(BinaryTreeNode root, ref int count)
        // {
        //     if (root != null)
        //     {



        //         if (count % 2 == 0)
        //         {
        //             CheckBox chkActivityHeader = new CheckBox();
        //             chkActivityHeader.Margin = new Thickness(5, 0, 0, 0);
        //             chkActivityHeader.Content = root.Name;
        //             spActivity.Children.Add(chkActivityHeader);


        //         }
        //         else
        //         {
        //             CheckBox chkActivityHeader = new CheckBox();
        //             chkActivityHeader.Margin = new Thickness(5, 0, 0, 0);
        //             chkActivityHeader.Content = root.Name;
        //             spActivity.Children.Add(chkActivityHeader);
        //             TextBlock txtblkActivityHeader = new TextBlock();
        //             txtblkActivityHeader.Name = $"txtblkActivityHeader_{count}";
        //             txtblkActivityHeader.Text = root.Name;
        //             spActivity.Children.Add(txtblkActivityHeader);
        //         }



        //         count =+ 2;
        //         BinTreeTraversal(root.Right, ref count);
        //         count =- 1;
        //         BinTreeTraversal(root.Left, ref count);

        //     }








        //}
        // public void InOrderTraversal(BinaryTreeNode root)
        // {
        //     if (root != null)
        //     {
        //         InOrderTraversal(root.Left);
        //         Console.WriteLine($"{ root.Name}");
        //         InOrderTraversal(root.Right);
        //     }

        // }
        // public void PostOrderTraversal(BinaryTreeNode root)
        // {
        //     if (root != null)
        //     {

        //         PostOrderTraversal(root.Left);
        //         PostOrderTraversal(root.Right);
        //         Console.WriteLine($"{root.Name}");
        //     }

        // }

        private void InitActivityTreeView()
        {



            ActivityGroup Caring = new ActivityGroup() { Parent="", ActivityName = "Caring" };
            ActivityGroup Central_Christian_Academy = new ActivityGroup() {Parent = "", ActivityName = "Central Christian Academy" };
            ActivityGroup Central_Kids_Elementry_K5th_Grade = new ActivityGroup() { Parent = "", ActivityName = "Central Kids: Elementry (K-5th Grade)" };
            ActivityGroup Central_Kids_Nursery_8WKS_2YRS = new ActivityGroup() { Parent = "", ActivityName = "Central Kids: Nursery (8WKS-2YRS)" };
            ActivityGroup Central_Kids_Preschool3_5YRS_old = new ActivityGroup() { Parent = "", ActivityName = "Central Kids: Preschool (3-5YRS old)" };
            ActivityGroup Central_Kids_Outreach_Events = new ActivityGroup() { Parent = "", ActivityName = "Central Kids: Outreach Events" };
            ActivityGroup Creative_Arts = new ActivityGroup() { Parent = "", ActivityName = "Creative Arts" };
            ActivityGroup Helps = new ActivityGroup() { Parent = "", ActivityName = "Helps" };
            ActivityGroup Hospitality = new ActivityGroup() { Parent = "", ActivityName = "Hospitality" };
            ActivityGroup Outreach_Local_Missions = new ActivityGroup() { Parent = "", ActivityName = "Outreach: Local Missions" };
            ActivityGroup Students_The_Rock6_12th_Grade = new ActivityGroup() { Parent = "", ActivityName = "Students: The Rock (6-12th Grade)" };


            Caring.lstActivityTasks.Add(new ActivityTask() { Parent="Caring", ActivityId = 1, TaskName = "Hospitality", Description = "Briefely visit(on a day of your choosing) and pray with individuals who are in local hospitals - Church office will supply admission information." });
            Caring.lstActivityTasks.Add(new ActivityTask() { Parent = "Caring", ActivityId = 2, TaskName = "Shut-In Visitation", Description = "Call to arrange convenient times fora home visit - Communion may also be served." });
            Caring.lstActivityTasks.Add(new ActivityTask() { Parent = "Caring", ActivityId = 3, TaskName = "Nursing Home Visitation", Description = "Visit local nursing homes during anassigned week each month to encourage those in need." });

            m_lstActivityTask.AddRange(Caring.lstActivityTasks);


            ActivityTask General_Volunteer = new ActivityTask {Parent= "Central_Christian_Academy", ActivityId = 4, TaskName = "General Volenteer" };
            General_Volunteer.lstsubTasks.Add(new ActivityTask() {Parent="General_Volunteer", ActivityId = 5, TaskName = "Box Tops Collector", Description = "Cut, collect and count Box Tops." });
            General_Volunteer.lstsubTasks.Add(new ActivityTask() { Parent = "General_Volunteer", ActivityId = 6, TaskName = "Library Helper", Description = "Proof read library books before we put them on the shelves." });
            General_Volunteer.lstsubTasks.Add(new ActivityTask() { Parent = "General_Volunteer", ActivityId = 7, TaskName = "Lunch Monitor", Description = "Help to supervise the children during lunchtime." });
            General_Volunteer.lstsubTasks.Add(new ActivityTask() { Parent = "General_Volunteer",ActivityId = 8, TaskName = "Office Helper", Description = "Help to prepare mass mailings for fundraisers." });

            m_lstActivityTask.Add(General_Volunteer);
            m_lstActivityTask.AddRange(General_Volunteer.lstsubTasks);

            Central_Christian_Academy.lstActivityTasks.Add(General_Volunteer);

            ActivityTask Sunday_mornings = new ActivityTask {Parent = "Central_Kids_Elementry_K5th_Grade", ActivityId = 9, TaskName = "Sunday Mornings: Super Church" };
            Sunday_mornings.lstsubTasks.Add(new ActivityTask() { Parent = "Sunday_mornings", ActivityId = 10, TaskName = "Leader/Teacher & Assistant", Description = "Facilitate the lessons, crafts & classroom management. Provide a godly example." });
            Sunday_mornings.lstsubTasks.Add(new ActivityTask() { Parent = "Sunday_mornings", ActivityId = 11, TaskName = "Check-in Greeter", Description = "Greet families while checking-in kids with our computerized system at the beginning of the service." });

            m_lstActivityTask.Add(Sunday_mornings);
            m_lstActivityTask.AddRange(Sunday_mornings.lstsubTasks);

            ActivityTask Wednesday_evenings = new ActivityTask { Parent = "Central_Kids_Elementry_K5th_Grade", ActivityId = 12, TaskName = "Wednesday Evenings: M-Pact Girls Club & Royal Rangers" };
            Wednesday_evenings.lstsubTasks.Add(new ActivityTask() { Parent = "Wednesday_evenings", ActivityId = 13, TaskName = "Leader/Teacher & Assistant", Description = "Facilitate the lessons, crafts & classroom management. Provide a godly example." });
            Wednesday_evenings.lstsubTasks.Add(new ActivityTask() { Parent = "Wednesday_evenings", ActivityId = 14, TaskName = "Check-in Greeter", Description = "Greet families while checking-in kids with our computerized system at the beginning of the service." });

            m_lstActivityTask.Add(Wednesday_evenings);
            m_lstActivityTask.AddRange(Wednesday_evenings.lstsubTasks);

            ActivityTask Sunday_Mornings_Wednesday_Evenings = new ActivityTask { Parent = "Central_Kids_Nursery_8WKS_2YRS", ActivityId = 15, TaskName = "Sunday Mornings & Wednesday Evenings" };
            Sunday_Mornings_Wednesday_Evenings.lstsubTasks.Add(new ActivityTask() { Parent = "Sunday_Mornings_Wednesday_Evenings", ActivityId = 16, TaskName = "Leader/Assistant", Description = "Minister to nursery age children and their needs." });
            Sunday_Mornings_Wednesday_Evenings.lstsubTasks.Add(new ActivityTask() { Parent = "Sunday_Mornings_Wednesday_Evenings", ActivityId = 17, TaskName = "Rocker", Description = "Hold and rock infants as you minister in the nursery." });
            Sunday_Mornings_Wednesday_Evenings.lstsubTasks.Add(new ActivityTask() { Parent = "Sunday_Mornings_Wednesday_Evenings", ActivityId = 18, TaskName = "Check-in Greeter", Description = "Greet families while checking in kids with our computerized system at the begining of  service." });

            m_lstActivityTask.Add(Sunday_Mornings_Wednesday_Evenings);
            m_lstActivityTask.AddRange(Sunday_Mornings_Wednesday_Evenings.lstsubTasks);

            ActivityTask Childrens_church = new ActivityTask { Parent = "Central_Kids_Preschool3_5YRS_old", ActivityId = 19, TaskName = "Children's Church (Sunday) & Rainbows Club (Wednesdays)" };
            Childrens_church.lstsubTasks.Add(new ActivityTask() { Parent = "Childrens_church", ActivityId = 20, TaskName = "Leader/Teacher & Assistant", Description = "Facilitate the lessons, crafts & classroom management. Provide a godly example." });

            m_lstActivityTask.Add(Childrens_church);
            m_lstActivityTask.AddRange(Childrens_church.lstsubTasks);

            Central_Kids_Preschool3_5YRS_old.lstActivityTasks.Add(Childrens_church);
            Central_Kids_Nursery_8WKS_2YRS.lstActivityTasks.Add(Sunday_Mornings_Wednesday_Evenings);

            Central_Kids_Outreach_Events.lstActivityTasks.Add(new ActivityTask() { Parent = "Central_Kids_Outreach_Events", ActivityId = 21, TaskName = "Concessions", Description = "Prepare and serve food to children." });
            Central_Kids_Outreach_Events.lstActivityTasks.Add(new ActivityTask() { Parent = "Central_Kids_Outreach_Events", ActivityId = 22, TaskName = "Leader & Assistant", Description = "Minister to children and their needs at these events." });
            Central_Kids_Outreach_Events.lstActivityTasks.Add(new ActivityTask() { Parent = "Central_Kids_Outreach_Events", ActivityId = 23, TaskName = "Recreation", Description = "Register children as they enter each day's event." });
            Central_Kids_Outreach_Events.lstActivityTasks.Add(new ActivityTask() { Parent = "Central_Kids_Outreach_Events", ActivityId = 24, TaskName = "Security", Description = "Monitoring and securing the grounds at each event." });

            
            m_lstActivityTask.AddRange(Central_Kids_Outreach_Events.lstActivityTasks);

            Central_Kids_Elementry_K5th_Grade.lstActivityTasks.Add(Sunday_mornings);
            Central_Kids_Elementry_K5th_Grade.lstActivityTasks.Add(Wednesday_evenings);

            Creative_Arts.lstActivityTasks.Add(new ActivityTask() { Parent = "Creative_Arts", ActivityId = 25, TaskName = "Workshop Team (Vocal & Instrumental", Description = "Use your talents to assist in leading the congregation into worship during our weekly services." });
            Creative_Arts.lstActivityTasks.Add(new ActivityTask() { Parent = "Creative_Arts", ActivityId = 26, TaskName = "Technical Multimedia Team", Description = "Apply your skills in the area of sound, videography, and computer technology during our weekly worship services." });

            m_lstActivityTask.AddRange(Creative_Arts.lstActivityTasks);

            ActivityTask GroundsTeam = new ActivityTask {Parent="Helps", ActivityId = 27, TaskName = "Grounds Team" };
            GroundsTeam.lstsubTasks.Add(new ActivityTask() { Parent = "GroundsTeam", ActivityId = 28, TaskName = "Construction", Description = "Use your skills to help with special projects on campus." });
            GroundsTeam.lstsubTasks.Add(new ActivityTask() { Parent = "GroundsTeam", ActivityId = 29, TaskName = "Housekeeping", Description = "Help keep our facilities clean by volunteering to clean in certain areas." });
            GroundsTeam.lstsubTasks.Add(new ActivityTask() { Parent = "GroundsTeam", ActivityId = 30, TaskName = "Lanscaping", Description = "Groom and care for landscaping and mulchbeds around church and parsonage." });
            GroundsTeam.lstsubTasks.Add(new ActivityTask() { Parent = "GroundsTeam", ActivityId = 31, TaskName = "Lawn Maintenance", Description = "Join a rotaion to keep the lawn on campus mowed." });

            m_lstActivityTask.Add(GroundsTeam);
            m_lstActivityTask.AddRange(GroundsTeam.lstsubTasks);

            ActivityTask kitchenHelp = new ActivityTask { Parent = "Helps", ActivityId = 32, TaskName = "Kitchen Help" };
            kitchenHelp.lstsubTasks.Add(new ActivityTask() { Parent = "kitchenHelp", ActivityId = 33, TaskName = "Funeral Dinners", Description = "Cook and serve dinners to the families and friends of those whoare grievingafter funeral services." });
            kitchenHelp.lstsubTasks.Add(new ActivityTask() { Parent = "kitchenHelp", ActivityId = 34, TaskName = "Special Events", Description = "Throughout the year their are events that will require assistance with cooking, food preparation and setup." });

            m_lstActivityTask.Add(kitchenHelp);
            m_lstActivityTask.AddRange(kitchenHelp.lstsubTasks);

            Helps.lstActivityTasks.Add(new ActivityTask() { Parent = "Helps", ActivityId = 35, TaskName = "Medical Ministry Team", Description = "Be a first responder in the event of a medical emergency on the church campus or at a church event (special training and certification required)." });

            ActivityTask Office_Volunteers = new ActivityTask { Parent = "", ActivityId = 36, TaskName = "Office Volunteers" };
            GroundsTeam.lstsubTasks.Add(new ActivityTask() { Parent = "Office_Volunteers", ActivityId = 37, TaskName = "General Volunteer", Description = "Support the office staff on a regular basis - answering phones, type & copy documents as needed, basic computer skills and a pleasant personality will be required." });
            GroundsTeam.lstsubTasks.Add(new ActivityTask() { Parent = "Office_Volunteers", ActivityId = 38, TaskName = "Special Projects", Description = "Help on an as-needed basis with a wide variety of tasks such as mailings, packet assembly, cutting, sorting, etc." });


            m_lstActivityTask.AddRange(Helps.lstActivityTasks);

            m_lstActivityTask.Add(Office_Volunteers);
            m_lstActivityTask.AddRange(GroundsTeam.lstsubTasks);


            Helps.lstActivityTasks.Add(GroundsTeam);
            Helps.lstActivityTasks.Add(kitchenHelp);
            Helps.lstActivityTasks.Add(new ActivityTask() { Parent = "Helps", ActivityId = 38, TaskName = "Security Team", Description = "Be a part of a ministry team to help ensure the safety of all who choose to worship at Central." });
            Helps.lstActivityTasks.Add(new ActivityTask() { Parent = "Helps", ActivityId = 39, TaskName = "Transportation", Description = "Drive school bus (CDL license required) or church vans for special events throughout the year for children, youth and adult groups." });

            m_lstActivityTask.Add(GroundsTeam);
            m_lstActivityTask.Add(kitchenHelp);
            m_lstActivityTask.AddRange(Helps.lstActivityTasks);

            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 40, TaskName = "Communion Team(set-up, Condense, or Clean-up)", Description = "Prayerfully prepare the communion elements to be served monthly - includes clean-up (gathering cups from the pews and emptying/cleaning communion trays." });
            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 41, TaskName = "Welcome Center Assistance", Description = "To welcome new people to Central by giving them a gift bag, offering a tour of the campus, and helping them finding their children's classes." });
            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 42, TaskName = "Doorkeeper", Description = "Open the doors for people as they arrive to make them fell welcome." });
            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 43, TaskName = "Greeter", Description = "Greeting people with your talents and abilities to connect with God and others in the church." });
            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 44, TaskName = "Information Center Assistant", Description = "Supply congregation with up-to-date information regarding events, visitor information, ficility directions, and money collection for various items." });
            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 45, TaskName = "Parking Lot Attendant", Description = "Direct traffic flow, assist anyone who needs help getting into the church, and ensure a great first impression for everyone." });
            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 46, TaskName = "Usher Misistry", Description = "Help seat guests, hand out bulletins, handle crowed control, answer questions, take offering and serve communion during our Sunday services and special events." });
            Hospitality.lstActivityTasks.Add(new ActivityTask() { Parent = "Hospitality", ActivityId = 47, TaskName = "Golf Cart Drivers", Description = "Shuttle people to and from their cars on Sunday mornings and special events." });

            m_lstActivityTask.AddRange(Hospitality.lstActivityTasks);

            Outreach_Local_Missions.lstActivityTasks.Add(new ActivityTask() {Parent= "Outreach_Local_Missions", ActivityId = 48, TaskName = "Special Projects", Description = "Help with local outreach projects including minor building repairs, painting, collecting and distriburting clothing & food, etc." });

            Students_The_Rock6_12th_Grade.lstActivityTasks.Add(new ActivityTask() { Parent = "Students_The_Rock6_12th_Grade", ActivityId = 49, TaskName = "Administrative", Description = "Guest follow-up, parent communication, mailing/emailing, Website." });
            Students_The_Rock6_12th_Grade.lstActivityTasks.Add(new ActivityTask() { Parent = "Students_The_Rock6_12th_Grade", ActivityId = 50, TaskName = "Adio/Technicl Team", Description = "Prepare, manage, maintain and operate the sound, video, computer and lighting systems to enhance services." });
            Students_The_Rock6_12th_Grade.lstActivityTasks.Add(new ActivityTask() { Parent = "Students_The_Rock6_12th_Grade", ActivityId = 51, TaskName = "Cafe Team", Description = "Serving food, snacks and beverage items to the students on Wednesday nights and special events." });
            Students_The_Rock6_12th_Grade.lstActivityTasks.Add(new ActivityTask() { Parent = "Students_The_Rock6_12th_Grade", ActivityId = 52, TaskName = "Junior High Ministry Team", Description = "Serve by teaching/assisting in our Sunday Morning Junior High Class with students in grades 6-8" });
            Students_The_Rock6_12th_Grade.lstActivityTasks.Add(new ActivityTask() { Parent = "Students_The_Rock6_12th_Grade", ActivityId = 53, TaskName = "Ministry Team Volunteer", Description = "Serve by assisting during Wednesday night meetings in the areas of check-in, leading activities, small group discussions, events, student discipleship and leader retreats." });
            Students_The_Rock6_12th_Grade.lstActivityTasks.Add(new ActivityTask() { Parent = "Students_The_Rock6_12th_Grade", ActivityId = 54, TaskName = "Evangelic Outreach", Description = "Shate the love of Christ with others by partnering with missionschurches' events, outreaches, VBS, etc." });

            m_lstActivityTask.AddRange(Outreach_Local_Missions.lstActivityTasks);
            m_lstActivityTask.AddRange(Students_The_Rock6_12th_Grade.lstActivityTasks);

            //  lstsubTasks = new System.Collections.ObjectModel.ObservableCollection<ActivityTask>().Add(subtask1)

            m_lstActivities.Add(Caring);
            m_lstActivities.Add(Central_Christian_Academy);
            m_lstActivities.Add(Central_Kids_Elementry_K5th_Grade);
            m_lstActivities.Add(Central_Kids_Nursery_8WKS_2YRS);
            m_lstActivities.Add(Central_Kids_Preschool3_5YRS_old);
            m_lstActivities.Add(Central_Kids_Outreach_Events);
            m_lstActivities.Add(Creative_Arts);
            m_lstActivities.Add(Helps);
            m_lstActivities.Add(Hospitality);
            m_lstActivities.Add(Outreach_Local_Missions);
            m_lstActivities.Add(Students_The_Rock6_12th_Grade);



            trvActivities.ItemsSource = m_lstActivities;



        }
        private void GenerateDBFollowUps()
        {
            //problem
            // Generate Follow-Ups for each Attendee that missed 28 days of church

            //Solution
            //1)look and last date attended for each AttendeeId and see if attendee attended 4 weeks (28 days) in the past from now
            //2)if attendee did not attend 4 weeks in the past, generate a new database entry with status follow-up for corresponding AttendeeId
            //3) if attendee has 3 follow-ups in a row, flag attendee and don't consider him for another follow-up entry. 


            // DateTime curdate = DateTime.Now;
            // get last date that was just entered
            // int addEntity = 0;
            List<DateTime> lstsundays = new List<DateTime>();

            TimeSpan timespanSinceDate = new TimeSpan();

            Cursor = Cursors.Wait;

            var latest_date_attened = (from d in m_dbContext.Attendance_Info.Local
                                       where (d.Status == "Attended" || d.Status == "Responded")
                                       orderby d.Date ascending
                                       select d).ToArray().LastOrDefault();



            var queryAttendees = from AttendeeRec in m_dbContext.Attendees.Local
                                 select AttendeeRec;


            foreach (var AttendeeRec in queryAttendees)
            {




                var lstDateRecs = (from DateRec in AttendeeRec.AttendanceList
                                   orderby DateRec.Date ascending
                                   select DateRec).ToArray().LastOrDefault();

                if (lstDateRecs != null)
                {
                    timespanSinceDate = latest_date_attened.Date - lstDateRecs.Date;

                    

                    if (timespanSinceDate.Days < 21)
                    {

                        // do nothing
                        //Attendee already have a followUp sent so do not generate another followup unil 21 days has
                        //lapsed since the last followUp        


                    }
                    else
                    {
                        //generate follow-up if attendee does not have 3 consecutive followups already
                        //search for sunday


                        for (DateTime date = lstDateRecs.Date; date <= latest_date_attened.Date; date = date.AddDays(1))
                        {
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                lstsundays.Add(date);
                            }
                        }

                        DateTime lastSunday = lstsundays.LastOrDefault();

                        if (lastSunday != null)
                        {
                            Attendance_Info newfollowUpRecord = new Attendance_Info { };
                            newfollowUpRecord.AttendeeId = AttendeeRec.AttendeeId;
                            newfollowUpRecord.Date = lastSunday.Date;
                            newfollowUpRecord.Status = "Follow-Up";
                            m_dbContext.Attendance_Info.Add(newfollowUpRecord);
                        }
                        //re-initialize table with new added information
                        InitDataSet();
                        Display_DefaultTable_in_Grid();
                    }
                    // }
                } //end if


            } //end foreach



            Cursor = Cursors.Arrow;

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
                        if (row >= 2)
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

        private void Display_ActivityTable_in_Grid()
        {
           

            //dataGrid_activity.DataContext = m_lstactivityTableRows;
           // dataGrid_activity.Items.Refresh();
        }
        private void Display_AttendeeListTable_in_Grid()
        {

          
            
            dataGrid_prospect.DataContext = m_lstattendanceTableRows.OrderBy(rec => rec.LastName);
            dataGrid_prospect.Items.Refresh();





        }





        private void Display_DefaultTable_in_Grid()
        {

            //  m_lstdefaultTableRows.Sort();
            //   dataGrid.ItemsSource = m_lstdefaultTableRows;
            
            dataGrid.DataContext = m_lstdefaultTableRows.OrderBy(rec => rec.LastName); 
            dataGrid.Items.Refresh();



        } // end  private void Display_Database_in_Grid()

     

        private void InitDataSet()
        {

            m_dbContext.Attendees.Load();
            m_dbContext.Attendance_Info.Load();
            m_dbContext.Activities.Load();


            //--------------------- Make DEFAULT TABLE---------------------------------------------------------------------------


            //DataTable Default_Data_Table = new DataTable("DefaultTable");
            //DataTable AttendeeListTable = new DataTable("AttendeeListTable");
           // DataTable ActivityTable = new DataTable("ActivityTable");

            string date = "Date Not Valid";




            try
            {


                string ldate = "";
                string lstatus = "";
                string adate = "";



                foreach (var AttendeeRec in m_dbContext.Attendees.Local)
                {

                  
                    var queryLastDate = (from DateRec in AttendeeRec.AttendanceList
                                         where DateRec.Status == "Attended" || DateRec.Status == "Responded"
                                         orderby DateRec.Date ascending
                                         select DateRec).ToList().LastOrDefault();

                    var queryActivityLastDate = (from ActivityDateRec in AttendeeRec.ActivityList
                                                 orderby ActivityDateRec.Date ascending
                                                 select ActivityDateRec).ToList().LastOrDefault();

                    //----Construct AttendeeLisTable-------------------------------------------------------------------------------------
                    
                    // fill Attendance table columns. Add to list for each row
                    m_lstattendanceTableRows.Add(new AttendanceTableRow()
                    {
                        AttendeeId = AttendeeRec.AttendeeId,
                        FirstLastName = AttendeeRec.FirstName.ToUpper() + " " + AttendeeRec.LastName.ToUpper(),
                        LastName = AttendeeRec.LastName,
                        FirstName = AttendeeRec.FirstName,
                        DateString = "Date Not Valid",
                        Attended = false
                    });


                    //------Active Attendee--//---Construct DefaultTableRow-------------------------------------------------------------
                    DefaultTableRow DefaultTabledr = new DefaultTableRow
                    {
                        AttendanceList = AttendeeRec.AttendanceList
                    };
                  
                    if (queryLastDate != null)
                    {
                        ldate = queryLastDate.Date.ToString("MM-dd-yyyy");
                        if (queryActivityLastDate != null)
                            adate = queryActivityLastDate.Date?.ToString("MM-dd-yyyy");

                        lstatus = queryLastDate.Status;


                      

                        m_NewAttendeeId = AttendeeRec.AttendeeId;


                        DefaultTabledr.AttendeeId = AttendeeRec.AttendeeId;
                        DefaultTabledr.FirstLastName = AttendeeRec.FirstName.ToUpper() + " " + AttendeeRec.LastName.ToUpper();

                        DefaultTabledr.FirstName = AttendeeRec.FirstName;
                        DefaultTabledr.LastName = AttendeeRec.LastName;

                        

                        DefaultTabledr.Church_Last_Attended = ldate;
                        DefaultTabledr.ChurchStatus = lstatus;
                        
                        if (queryActivityLastDate != null)
                        {
                            DefaultTabledr.Activity_Last_Attended = adate;
                            DefaultTabledr.ActivityList = AttendeeRec.ActivityList;
                            DefaultTabledr.Activity = queryActivityLastDate.ToString();

                        }
                        else
                        {
                            DefaultTabledr.Activity_Last_Attended = "n/a";
                            DefaultTabledr.Activity = "n/a";
                        }
                            

                        DefaultTabledr.Phone = AttendeeRec.Phone;
                        DefaultTabledr.Email = AttendeeRec.Email;

                        
                    }
                    else // There are no Attended status for attendee, look for any follow-up statuses
                    {
                        var queryLastDateFollowUp = (from DateRec in AttendeeRec.AttendanceList
                                                     where DateRec.Status == "Follow-Up"
                                                     orderby DateRec.Date ascending
                                                     select DateRec).ToList().LastOrDefault();

                        if (queryLastDateFollowUp != null)
                        {


                            lstatus = queryLastDateFollowUp.Status;

                          

                            m_NewAttendeeId = AttendeeRec.AttendeeId;


                            DefaultTabledr.AttendeeId = AttendeeRec.AttendeeId;
                            DefaultTabledr.FirstLastName = AttendeeRec.FirstName.ToUpper() + " " + AttendeeRec.LastName.ToUpper();

                            DefaultTabledr.FirstName = AttendeeRec.FirstName;
                            DefaultTabledr.LastName = AttendeeRec.LastName;

                            DefaultTabledr.Church_Last_Attended = "N/A";
                            DefaultTabledr.ChurchStatus = lstatus;

                            if (queryActivityLastDate != null)
                            {
                                DefaultTabledr.Activity_Last_Attended = adate;
                                DefaultTabledr.ActivityList = AttendeeRec.ActivityList;

                                DefaultTabledr.Activity = queryActivityLastDate.ToString();

                            }
                            else
                            {
                                DefaultTabledr.Activity_Last_Attended = "n/a";
                                DefaultTabledr.Activity = "n/a";
                            }

                            DefaultTabledr.Phone = AttendeeRec.Phone;
                            DefaultTabledr.Email = AttendeeRec.Email;


                           

                        }
                        
                    }
                    // Add DefaultTableRow to list
                    m_lstdefaultTableRows.Add(DefaultTabledr);
                 
                 

                } // end foreach
             
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred when performing database operation: {ex}");
            }




        }




        private void chkResponded_Checked(object sender, RoutedEventArgs e)
        {

            Cursor = Cursors.Wait;
            chkAttended.IsChecked = false;
            chkFollowup.IsChecked = false;

            m_isRespondedChecked = true;
            m_isQueryTableShown = true;

            BuildQuery_and_UpdateGrid();

            Cursor = Cursors.Arrow;

        }

        private void chkFollowup_Checked(object sender, RoutedEventArgs e)
        {

            Cursor = Cursors.Wait;

            chkAttended.IsChecked = false;
            chkResponded.IsChecked = false;
            m_isFollowupChecked = true;
            m_isQueryTableShown = true;

            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;
        }


        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
            //generate list of all attended attendees


            Cursor = Cursors.Wait;

            chkResponded.IsChecked = false;
            chkFollowup.IsChecked = false;

            m_isAttendedChecked = true;
            m_isQueryTableShown = true;

            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;
        }





        private void Disable_Filters()
        {
            CalendarExpander.IsEnabled = false;
            ChurchStatusExpander.IsEnabled = false;
            ActivityExpander.IsEnabled = false;
            //chkAttended.IsEnabled = false;
            //chkFollowup.IsEnabled = false;
            //chkResponded.IsEnabled = false;
            //chkChurchDateFilter.IsEnabled = false;
            //chkActivityFilter.IsEnabled = false;



        }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if in followUp view, use query database else if in model list view filter table



            if (txtSearch.Text == "")
            {
                Enable_Filters();
                btnDelete.IsEnabled = true;

                if (m_isFilterByDateChecked || m_isActivityfilterByDateChecked)
                    DateCalendar.IsEnabled = true;
                else
                    DateCalendar.IsEnabled = false;

                if (m_AttendanceView)
                {
                    if ((chkChurchDateFilter.IsChecked == true ||
                            chkActivityDateFilter.IsChecked == true ||
                            chkActivityFilter.IsChecked == true) ||
                            (m_isAttendedChecked || m_isFollowupChecked || m_isRespondedChecked) == true)
                    {
                        dataGrid.DataContext = m_lstQueryTableRows;
                    }
                    else
                        dataGrid.DataContext = m_lstdefaultTableRows;

                }
                else if (m_alistView)
                {
                    m_DataSet.Tables["AttendeeListTable"].DefaultView.RowFilter = String.Empty;

                }
                else if (m_activityView)
                {

                   // dataGrid_activity.DataContext = m_lstactivityTableRows;


                }


                //----------------------Textbox search has text-----------------------------------------------------------------------------------
            }
            else
            {
                Disable_Filters();
                DateCalendar.IsEnabled = false;
                btnDelete.IsEnabled = false;

                string text = txtSearch.Text.ToUpper();

                if (m_AttendanceView)
                {
                    if ((chkChurchDateFilter.IsChecked == true ||
                            chkActivityDateFilter.IsChecked == true ||
                            chkActivityFilter.IsChecked == true ) ||
                            (m_isAttendedChecked || m_isFollowupChecked || m_isRespondedChecked) == true)
                    {
                        var filteredQueryTable = m_lstQueryTableRows.Where(row => row.FirstLastName.Contains(text));
                        dataGrid.DataContext = filteredQueryTable;
                    }
                    else
                    {
                        var filteredDefaultTable = m_lstdefaultTableRows.Where(row => row.FirstLastName.Contains(text));
                        dataGrid.DataContext = filteredDefaultTable;
                    }

                }
                else if (m_alistView)
                {
                    m_DataSet.Tables["AttendeeListTable"].DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                }
                else if (m_activityView)
                {
                    //m_DataSet.Tables["ActivityTable"].DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                   //var filteredActivityList = m_lstactivityTableRows.Where(row => row.FirstLastName.Contains(text) );
                    //dataGrid_activity.DataContext = filteredActivityList;
                }



            }


        }


        private void UpdateDefaultTableIdAndStatus(string attendeeid, DateTime date, string status)
        {

            foreach (DataRow def_dr in m_DataSet.Tables["DefaultTable"].Rows)
            {
                if (def_dr["AttendeeId"].ToString() == attendeeid)
                {
                    def_dr["Date Last Attended"] = date.ToString("MM-dd-yyyy");
                    def_dr["Status"] = status;
                }


            }

        }
        private void UpdateDataGridWithQuery(IQueryable<AttRecord> linqquery, string query = "0")
        {




            if (query == "ShowDefaultTable")
            {
                //if (m_DataSet.Tables["DefaultTable"].Rows.Count > 1)
                //{
                Display_DefaultTable_in_Grid();
                if (dataGrid.Columns.Count > 1)
                {
                    dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                    dataGrid.Columns[1].Visibility = Visibility.Hidden; //LastFirstName
                }
            //}

                m_isQueryTableShown = false;

            }
            else if (query == "")
            {
                // do nothing

            }
            else if (query == "QueryTableShown")
            {
                if (txtSearch.Text != "")
                {
                    m_DataSet.Tables["QueryTable"].DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                    dataGrid.DataContext = m_DataSet.Tables["QueryTable"];
                    dataGrid.IsReadOnly = true;
                }
                else
                {
                    dataGrid.DataContext = m_DataSet.Tables["QueryTable"];
                    dataGrid.IsReadOnly = true;
                }
                m_isQueryTableShown = true;

            }
            else if (linqquery != null)
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
                queryTable.Columns.Add(new DataColumn("Activity"));
                queryTable.Columns.Add(new DataColumn("Activity Last Attended"));
                queryTable.Columns.Add(new DataColumn("Phone"));
                queryTable.Columns.Add(new DataColumn("Email"));


                foreach (var rec in linqquery)
                {

                    DataRow newrow = queryTable.NewRow();



                    newrow["AttendeeId"] = rec.id;
                    newrow["FirstLastName"] = rec.fname + " " + rec.lname;
                    newrow["First Name"] = rec.fname;
                    newrow["Last Name"] = rec.lname;
                    newrow["Date"] = rec.date?.ToString("MM-dd-yyyy");
                    newrow["Status"] = rec.status;
                    newrow["Activity"] = rec.activity.ToString();
                    newrow["Activity Last Attended"] = rec.activity_date;
                    newrow["Phone"] = rec.phone;
                    newrow["Email"] = rec.email;



                    queryTable.Rows.Add(newrow);
                }




                //Swap LastName and FirstName columns
                queryTable.Columns[2].SetOrdinal(3);
                queryTable.AcceptChanges();
                m_DataSet.Tables.Add(queryTable);

                dataGrid.CanUserDeleteRows = false;
                dataGrid.CanUserAddRows = false;

                dataGrid.IsReadOnly = true;
                dataGrid.ToolTip = "Select and right mouse click to see attendance history.";
                queryTable.DefaultView.Sort = "[Last Name] ASC";
                // show the table 
                dataGrid.DataContext = queryTable;

                if (dataGrid.Columns.Count > 1)
                {
                    dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                    dataGrid.Columns[1].Visibility = Visibility.Hidden; //LastFirstName
                }

                m_isQueryTableShown = true;


            }


        }


        private void chkAttended_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isAttendedChecked = false;
            m_isQueryTableShown = false;

            Cursor = Cursors.Wait;


            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;
        }

        private void chkFollowup_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isFollowupChecked = false;
            m_isQueryTableShown = false;


            Cursor = Cursors.Wait;

            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;

        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isRespondedChecked = false;
            m_isQueryTableShown = false;

            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;
        }


        private void DateCalendar_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as Calendar;


            if (calender != null)
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

                    }

                    else
                    {
                        m_alistdateIsValid = false;

                    }


                }
                // AttendanceView-------------------------------------------------------------------------------------------------------------
                else if (m_AttendanceView && !m_IsActivityPanelView)
                {
                    if (m_isFilterByDateChecked)
                    {
                        m_DateSelected = datec;
                    }
                    else if (m_isActivityfilterByDateChecked)
                    {
                        m_ActivityDateSelected = datec;
                    }



                    if (datec.DayOfWeek == DayOfWeek.Sunday)
                    {

                        m_dateIsValid = true;

                    }

                    else
                    {
                        m_dateIsValid = false;

                    }


                    BuildQuery_and_UpdateGrid();

                }
                // ActivityView-------------------------------------------------------------------------------------------------------------------
                else if (m_IsActivityPanelView && m_isActivityfilterByDateChecked)
                {
                    m_ActivityDateSelected = datec;



                    string date = m_ActivityDateSelected?.ToString("MM-dd-yyyy");

                    //if (m_activity_row_selected != null)
                    //{
                    //    if (m_activityPairs_selectedFromListView.Count != 0)
                    //    {

                    //        foreach (ActivityPair ap in m_activityPairs_selectedFromListView)
                    //        {
                    //            //Find activity in Attendee List
                    //            var qFindActivityInList = m_activity_row_selected.ActivityList.SingleOrDefault(apair => apair.AttendeeId == m_activity_row_selected.AttendeeId &&
                    //                                                                              apair.ActivityGroup == ap.ActivityGroup &&
                    //                                                                              apair.ParentTaskName == ap.ParentTaskName &&
                    //                                                                              apair.ChildTaskName == ap.ChildTaskName);

                    //            //Activity is in List, create new activity and add it to the activity list with updated date
                    //            if (qFindActivityInList != null)
                    //            {
                    //                ActivityPair nap = new ActivityPair() { };
                    //                nap.Date = m_ActivityDateSelected;
                    //                nap.AttendeeId = m_activity_row_selected.AttendeeId;
                    //                nap.ActivityGroup = ap.ActivityGroup;
                    //                nap.ParentTaskName = ap.ParentTaskName;
                    //                nap.ChildTaskName = ap.ChildTaskName;

                    //                m_activity_row_selected.ActivityList.Add(nap);
                    //                //reflect activity last attended of attendee in default table row
                    //                DefaultTableRow selectdefaultrow = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == m_activity_row_selected.AttendeeId);
                    //                //get last activity record
                    //                ActivityPair lastActivityRec = selectdefaultrow.ActivityList.OrderByDescending(rec => rec.Date).ToList().FirstOrDefault();
                    //                //display last activity in Default Table
                    //                selectdefaultrow.Activity = lastActivityRec.ToString();
                    //                selectdefaultrow.Activity_Last_Attended = lastActivityRec.DateString;

                    //            }






                                    

                       


                    //        }
                    //    }
                        
                    //}
                }


                Cursor = Cursors.Arrow;
            }
            else // calendar is null
            {

            }

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

        void Save_Changes(object sender, RoutedEventArgs e)
        {
            bool isAttendedStatusChecked = false;

            int i = -1;



            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            //foreach (DataRow dr in m_DataSet.Tables["AttendeeListTable"].Rows)
            //{
            //    if (dr.ItemArray[5].ToString() == "True")
            //    {
            //        isAttendedStatusChecked = true;

            //        break;

            //    }
            //    else
            //    {
            //        isAttendedStatusChecked = false;
            //    }
            //}

            if (isAttendedStatusChecked)
            {
                MessageBoxResult res = MessageBox.Show("There are checked attendees in the attendee checklist that has not yet been added to the active attendance list,  save changes already made to the active attendance list?", "Attendees not added yet", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (res == MessageBoxResult.OK)
                {
                    Cursor = Cursors.Wait;
                    SaveActiveList();
                  
                    Cursor = Cursors.Arrow;
                }
                else
                    return;


            }
            else
            {
                SaveActiveList();
          
            }
        }

        private bool isAttendeeListDirty()
        {
            bool isAttendedStatusChecked = false;


            // save dataGrid edits
            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
            {
                if (dr.Attended)
                {
                    isAttendedStatusChecked = true;

                    break;

                }
                else
                {
                    isAttendedStatusChecked = false;
                }
            }
         
            return isAttendedStatusChecked;
        }
    
        private void DeleteRecordInDefaultTable(System.Collections.IList row_select)
        {

            DefaultTableRow[] array_defaultTableRows = new DefaultTableRow[m_lstdefaultTableRows.Count];
        
            //int index = 0;
            m_lstdefaultTableRows.CopyTo(array_defaultTableRows);
        
            var DefaultTableCopy = new List<DefaultTableRow>(array_defaultTableRows);
        

            foreach (DefaultTableRow dtr in row_select)
            {
                int attid = dtr.AttendeeId;

                var Attrec = m_dbContext.Attendees.Local.SingleOrDefault(id => id.AttendeeId == attid);
               


                // remove attendee from the db context
                if (Attrec != null)
                {
                   m_dbContext.Attendees.Remove(Attrec);

                    if (Attrec.AttendanceList.Count() != 0)
                    {
                        for (int idx = 0; idx <= Attrec.AttendanceList.Count() - 1; idx++)
                        {
                            m_dbContext.Attendance_Info.Remove(Attrec.AttendanceList[idx]);
                        }
                    }

                    if (Attrec.ActivityList.Count() != 0)
                    {
                        for (int idx = 0; idx <= Attrec.AttendanceList.Count() - 1; idx++)
                        {
                            m_dbContext.Activities.Remove(Attrec.ActivityList[idx]);
                        }
                    }



                }

                //get index of row to remove in copyand remove it
                for (int i =0; i <= array_defaultTableRows.Count()-1;i++)
                {
                    if (array_defaultTableRows[i].AttendeeId == attid)
                    {
                        DefaultTableCopy.RemoveAt(i);
                      
                        break;
                    }
                }
 
            }
            
            // clear the list 
            m_lstdefaultTableRows.Clear();
          
            // copy the list with the deleted rows to the mail list that display the default attendance table
            m_lstdefaultTableRows.AddRange(DefaultTableCopy);
            //  m_lstactivityTableRows.AddRange(ActivityTableCopy);

        }

      
      
        private void DeleteRecordInAttendeeListTable(System.Collections.IList row_select)
        {

            AttendanceTableRow[] array_attendanceTableRows = new AttendanceTableRow[m_lstattendanceTableRows.Count];


            //int index = 0;
            m_lstattendanceTableRows.CopyTo(array_attendanceTableRows);
            

            var AttendanceTableCopy = new List<AttendanceTableRow>(array_attendanceTableRows);

            foreach (DefaultTableRow dr in row_select)
            {


                //get index of row to remove in copyand remove it
                for (int i = 0; i <= array_attendanceTableRows.Count() - 1; i++)
                {
                    if (array_attendanceTableRows[i].AttendeeId == dr.AttendeeId)
                    {
                        AttendanceTableCopy.RemoveAt(i);

                        break;
                    }
                }

            }



            // clear the list 
            m_lstattendanceTableRows.Clear();
          
            // copy the list with the deleted rows to the mail list that display the default attendance table
            m_lstattendanceTableRows.AddRange(AttendanceTableCopy);
          


        }

    private void DeleteRecord(object sender, RoutedEventArgs e)
        {
          
            System.Collections.IList selectedRows = dataGrid.SelectedItems;


           //var default_row_selected = selectedRows.Cast<DefaultTableRow>();
            



            if (selectedRows.Count != 0)
            {

                Cursor = Cursors.Wait;
                bool isDirty = isAttendeeListDirty();


                if (isDirty)
                {
                    MessageBoxResult res = MessageBox.Show("There are checked attendees in the attendee checklist that has not yet been added to the active attendance list.\n\n" +
                                                           "Add them first then delete attendees.\n\nDiscard checked attendees in the attendee checklist and delete record anyway?", "Attendees not added yet", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    if (res == MessageBoxResult.OK)
                    {
                        if (m_AttendanceView)
                        {

                           DeleteRecordInDefaultTable(selectedRows);
                           DeleteRecordInAttendeeListTable(selectedRows);


                        }



                    }

                    else // isDirty: user pressed the cancel button on the messagebox
                    {
                        Cursor = Cursors.Arrow;
                        return;
                    }
                        
                }
                else
                {
                    DeleteRecordInDefaultTable(selectedRows);
                    DeleteRecordInAttendeeListTable(selectedRows);
                }
            



            }

            else
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("At least one record must be selected.", "Select Record", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


            Cursor = Cursors.Arrow;

           Display_DefaultTable_in_Grid();




        }

        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
           
            var selectedItems = sender as DataGrid;
           

            if (selectedItems.SelectedItem != null)
            {
                m_default_row_selected = (DefaultTableRow)selectedItems.SelectedItem;
               
            }

          
                txtSearch.IsEnabled = true;

            if (!m_IsActivePanelView)
            {
                LoadActivePanelState();
                Show_activeview_Panel();
            }
          
           
        }
        private int check_date_bounds()
        {
            DateTime curdate = DateTime.Now;
            DateTime datelimit;
            List<DateTime> lstsundays = new List<DateTime>();
            int i = 0;
            if (curdate.DayOfWeek != DayOfWeek.Sunday)
            {

                for (DateTime sundate = curdate.Date; sundate >= curdate.AddDays(-7); sundate = sundate.AddDays(-1))
                {
                    if (sundate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        lstsundays.Add(sundate);
                        break;
                    }
                }
                datelimit = lstsundays.FirstOrDefault();

            }
            else
            {
                datelimit = curdate;
            }


            if (datelimit != null)
            {
                if (m_alistDateSelected > datelimit)
                {
                    MessageBox.Show($"Date limit is {datelimit.ToShortDateString()}.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return 1;
                }
            }

            return 0;

        }


        private void ImportRows_Click(object sender, RoutedEventArgs e)
        {


            Cursor = Cursors.Wait;

            bool haschanges = false;

            string date = m_alistDateSelected.ToString("MM-dd-yyyy");

            string firstname = "";
            string lastname = "";
            //DateTime? date_t;
            int dupID = 1;
            // add all attendee status and date to database



            // first pass through list and make sure everything looks good before making any changes to the db context

            if (m_alistdateIsValid)
            {

                //end all edits and update the datagrid with changes
                dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid_prospect.UpdateLayout();




                int ret_error = check_date_bounds();

                if (ret_error == 1)
                    return;






                // m_DataSet.Tables["AttendeeListTable"].DefaultView.RowFilter = "";
                foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
                {
                    if (dr.IsModifiedrow)
                    {
                        int attid = dr.AttendeeId;
                        var AttendeeIdisInLocalContext = m_dbContext.Attendees.Local.SingleOrDefault(rec => rec.AttendeeId == attid);

                        //attendeeId is in database
                        if (AttendeeIdisInLocalContext != null)
                        {
                            // attended = true, add attendee info record to attendee attendance list

                            bool berror = Row_error_checking(dr);
                            if (berror)
                                return;


                            Attendance_Info newRecord = new Attendance_Info { };



                            newRecord.AttendeeId = attid;
                            newRecord.Date = m_alistDateSelected;


                            var lastAttInfoRec = (from AttInfo in AttendeeIdisInLocalContext.AttendanceList
                                                  where AttInfo.AttendeeId == attid
                                                  orderby AttInfo.Date ascending
                                                  select AttInfo).ToArray().LastOrDefault();


                            string flname = AttendeeIdisInLocalContext.FirstName + " " + AttendeeIdisInLocalContext.LastName;

                            if (lastAttInfoRec != null)
                            {
                                // If the last record is a follow-up then this record is a responded Status
                                if (lastAttInfoRec.Status == "Follow-Up")
                                    newRecord.Status = "Responded";
                                else
                                    newRecord.Status = "Attended";


                            }
                            else
                            {
                                newRecord.Status = "Attended";

                            }


                            //Add attendee info record to attendance_Info context
                            // AttendeeIdisInLocalContext.AttendanceList.Add(newRecord);

                            // Update Default
                            var defaultTableRec = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == attid);

                            // Add new Record to AttendanceList of attendee
                            // This will automatically update the m_dbcontext.Attendance_Info structure!
                            if (defaultTableRec != null)
                            {
                                //adding to the AttendanceList will automatically update the local db context with the new attendance_Info structure object
                                defaultTableRec.AttendanceList.Add(newRecord);

                                //change 'Status_Last_Attended' and 'date last attended' column in default table row to reflect the 
                                //new record's status
                                defaultTableRec.ChurchStatus = newRecord.Status;
                                defaultTableRec.Church_Last_Attended = newRecord.Date.ToString("MM-dd-yyyy");

                            }

                            haschanges = true;


                        }
                       
                       

                            
                       

                    }
                    else if (dr.IsNewrow)
                    {
                        //This is a new Attendee, add it to the database-------------------------------------------------------------------------------------------------------

                        /* --Add new Attendee to default table row---
                         *   Add a new Attendee to context
                         */
                        bool berror = Row_error_checking(dr);
                        if (berror)
                        {
                            MessageBox.Show("Please correct errors for attendee, check if first name, last name, date or status is valid?", "Attendee Status", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        //check if database already contain an attendee with first name and last name, if so append lastname with _1
                        bool bcheckdup = Check_for_dup_Attendee_inDbase(dr);
                        if (bcheckdup)
                        {
                            MessageBox.Show("A record with the same attendee name already exist in the database. Add status to existing attendee or please select a unique name.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                            return;
                        }



                        //find unique AttendeeId that is not already taken
                        dupID = 1;
                        // if attendee ID present increment by one
                        while (dupID == 1)
                        {
                            var isAttID_present = m_dbContext.Attendees.Local.SingleOrDefault(rec => rec.AttendeeId == m_NewAttendeeId);
                            if (isAttID_present != null)
                                m_NewAttendeeId += 1;
                            else
                                dupID = 0;
                        }

                        Attendee newAttendeeRec = new Attendee();
                        Attendance_Info newAttInfoRec = new Attendance_Info();
                        // make new row in Default Table
                        //build default table row to import
                        DefaultTableRow Defaultdr = new DefaultTableRow();




                        // new Attendee
                        newAttendeeRec.AttendeeId = m_NewAttendeeId;
                        newAttendeeRec.FirstName = dr.FirstName.ToString().Trim();
                        newAttendeeRec.LastName = dr.LastName.ToString().Trim();
                        newAttendeeRec.Prospect = 0;
                        newAttendeeRec.Phone = "";
                        newAttendeeRec.Email = "";

                        string flname = newAttendeeRec.FirstName.ToUpper() + " " + newAttendeeRec.LastName.ToUpper();
                        string phone = "";
                        string email = "";

                        //new Attendee Info record
                        newAttInfoRec.AttendeeId = newAttendeeRec.AttendeeId; // m_NewAttendeeId;
                        newAttInfoRec.Date = m_alistDateSelected;
                        newAttInfoRec.Status = "Attended";

                        //build row in DefaultTableRow
                        Defaultdr.AttendeeId = newAttendeeRec.AttendeeId;
                        Defaultdr.FirstLastName = flname;
                        Defaultdr.FirstName = newAttendeeRec.FirstName;
                        Defaultdr.LastName = newAttendeeRec.LastName;
                        Defaultdr.AttendanceList.Add(newAttInfoRec);

                        Defaultdr.ChurchStatus = newAttInfoRec.Status;
                        Defaultdr.Church_Last_Attended = newAttInfoRec.Date.ToString("MM-dd-yyyy");
                        Defaultdr.Activity = "n/a";
                        Defaultdr.Activity_Last_Attended = "n/a";

                        // add new rows to the default tables
                        m_lstdefaultTableRows.Add(Defaultdr);
                        // m_lstactivityTableRows.Add(newActivitydr);
                        // add new attendee info to db context
                        m_dbContext.Attendance_Info.Add(newAttInfoRec);
                        m_dbContext.Attendees.Add(newAttendeeRec);
                        haschanges = true;
                    }

                }

                if (haschanges)
                {
                    ClearAttendeeListStatus();
                    Display_AttendeeListTable_in_Grid();
                    Cursor = Cursors.Arrow;
                    MessageBox.Show("Attendees succesfully updated.", "Record Updated", MessageBoxButton.OK, MessageBoxImage.None);

                }

                
            }
            else
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("Please select a valid date", "Select date", MessageBoxButton.OK, MessageBoxImage.Error);

            }


            Cursor = Cursors.Arrow;


        }
        private bool Check_for_dup_Attendee_inDbase(AttendanceTableRow atr)
        {
            var queryAtt = (from AttRec in m_dbContext.Attendees.Local
                            where AttRec.FirstName.ToUpper() == atr.FirstName.ToUpper() && AttRec.LastName.ToUpper() == atr.LastName.ToUpper()
                            select AttRec).ToList().FirstOrDefault();


            if (queryAtt != null)
            {

                dataGrid_prospect.Focus();
                int id = atr.AttendeeId;
                int gridrowIdx = 1;
                foreach (AttendanceTableRow gridrow in dataGrid_prospect.Items)
                {

                    if (gridrow.AttendeeId == id)
                    {
                        dataGrid_prospect.SelectedIndex = gridrowIdx;
                        break;
                    }
                    gridrowIdx++;
                }
                dataGrid_prospect.ScrollIntoView(dataGrid_prospect.Items[gridrowIdx]);
                Cursor = Cursors.Arrow;
               
                return true;


            }

            return false;
            
        }
        private bool Row_error_checking(AttendanceTableRow atr)
        {
                if (atr.LastName == "" || atr.FirstName == "" || atr.DateString == "" || atr.Attended == false)
                {
                    dataGrid_prospect.Focus();
                    int id = atr.AttendeeId;
                    int gridrowIdx = 1;
                    foreach (AttendanceTableRow gridrow in dataGrid_prospect.Items)
                    {
                        
                        if (gridrow.AttendeeId == id)
                        {
                            dataGrid_prospect.SelectedIndex = gridrowIdx;
                            break;
                        }
                        gridrowIdx++;
                    }
                    dataGrid_prospect.ScrollIntoView(dataGrid_prospect.Items[gridrowIdx]);
                    Cursor = Cursors.Arrow;
                    

                    return true;
                }
                

            return false;
        }

        private void UpdateDefaultTableWithActivityInfo(string attId, DataRow ActivityTabledr)
        {
            foreach (DataRow DefaultTable_dr in m_DataSet.Tables["DefaultTable"].Rows)
            {
                if (DefaultTable_dr["AttendeeId"].ToString() == attId)
                {
                    DefaultTable_dr["Activity"] = ActivityTabledr["Activity"].ToString();
                    DefaultTable_dr["Activity Last Attended"] = ActivityTabledr["Activity Last Attended"].ToString();
                    DefaultTable_dr["Phone"] = ActivityTabledr["Phone"].ToString();
                    DefaultTable_dr["Email"] = ActivityTabledr["Email"].ToString();
                    break;
                }

            }


        }

        private void ClearAttendeeListStatus()
        {



            foreach (AttendanceTableRow atr in m_lstattendanceTableRows)
            {
                
                atr.Attended = false;
                atr.IsModifiedrow = false;
                atr.IsNewrow = false;
            }

            Display_AttendeeListTable_in_Grid();
        }


        private void Check_for_only_followup_status(int AttendeeId)
        {
            string ldate = "";
            string lstatus = "";
            int idx = 0;


            var attid = m_dbContext.Attendees.Local.SingleOrDefault(id => id.AttendeeId == AttendeeId);

            var queryLastDateAttended = (from DateRec in attid.AttendanceList
                                         where DateRec.Status == "Attended" || DateRec.Status == "Responded"
                                         orderby DateRec.Date ascending
                                         select DateRec).ToList().LastOrDefault();

            if (queryLastDateAttended == null)
            {



                var queryLastDateFollowUp = (from DateRec in attid.AttendanceList
                                             where DateRec.Status == "Follow-Up"
                                             orderby DateRec.Date ascending
                                             select DateRec).ToList().LastOrDefault();

                if (queryLastDateFollowUp != null)
                {

                    lstatus = queryLastDateFollowUp.Status;

                    foreach (DataRow dr in m_DataSet.Tables["DefaultTable"].Rows)
                    {
                        if (int.Parse(dr["AttendeeId"].ToString()) == AttendeeId)
                        {
                            m_DataSet.Tables["DefaultTable"].Rows[idx]["Date Last Attended"] = "N/A";
                            m_DataSet.Tables["DefaultTable"].Rows[idx]["Status"] = "Follow-Up";
                            break;
                        }
                        idx++;
                    }


                }



            }

        }
        private void RedrawAttendeeTable(int AttendeeId)
        {
            if (m_DataSet.Tables.Contains("AttendeeListTable"))
            {
                m_DataSet.Tables["AttendeeListTable"].Clear();


                var queryAttendees = from AttendeeRec in m_dbContext.Attendees.Local
                                     select AttendeeRec;


                foreach (var AttendeeRec in queryAttendees)
                {

                    m_NewAttendeeId = AttendeeRec.AttendeeId;

                    DataRow nrow = m_DataSet.Tables["AttendeeListTable"].NewRow();

                    nrow["AttendeeId"] = AttendeeRec.AttendeeId;
                    nrow["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;
                    nrow["First Name"] = AttendeeRec.FirstName;
                    nrow["Last Name"] = AttendeeRec.LastName;
                    nrow["Date"] = (m_alistdateIsValid) ? m_alistDateSelected.ToString("MM-dd-yyyy") : "Date Not Valid.";
                    nrow["Attended"] = false;

                    m_DataSet.Tables["AttendeeListTable"].Rows.Add(nrow);





                } // end foreach
            } // end foreach data defaulttable row
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

                    var queryLastDateAttended = (from DateRec in AttendeeRec.AttendanceList
                                                 where DateRec.Status == "Attended" || DateRec.Status == "Responded"
                                                 orderby DateRec.Date ascending
                                                 select DateRec).ToList().LastOrDefault();

                    if (queryLastDateAttended != null)
                    {
                        ldate = queryLastDateAttended.Date.ToString("MM-dd-yyyy");
                        lstatus = queryLastDateAttended.Status;


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
                    else // There are no Attended or Responded status for attendee, look for any follow-up statuses
                    {
                        var queryLastDateFollowUp = (from DateRec in AttendeeRec.AttendanceList
                                                     where DateRec.Status == "Follow-Up"
                                                     orderby DateRec.Date ascending
                                                     select DateRec).ToList().LastOrDefault();

                        if (queryLastDateFollowUp != null)
                        {

                            lstatus = queryLastDateFollowUp.Status;


                            m_NewAttendeeId = AttendeeRec.AttendeeId;

                            DataRow nrow = m_DataSet.Tables["DefaultTable"].NewRow();

                            nrow["AttendeeId"] = AttendeeRec.AttendeeId;
                            nrow["FirstLastName"] = AttendeeRec.FirstName + " " + AttendeeRec.LastName;
                            nrow["First Name"] = AttendeeRec.FirstName;
                            nrow["Last Name"] = AttendeeRec.LastName;
                            nrow["Date Last Attended"] = "N/A";
                            nrow["Status"] = lstatus;

                            m_DataSet.Tables["DefaultTable"].Rows.Add(nrow);


                        }
                    }

                } // end foreach
            } // end foreach data defaulttable row

            m_DataSet.Tables["DefaultTable"].AcceptChanges();




        }


        private void UpdateAttendeeListTableWithDateFilter()
        {

            string date;

            if (m_alistdateIsValid)
                date = m_alistDateSelected.ToString("MM-dd-yyyy");
            else
                date = "Date Not Valid.";

            foreach (AttendanceTableRow drAttendeeListTable in m_lstattendanceTableRows)
            {
                if (drAttendeeListTable.DateString == date)
                {
                    break;
                }
                else
                {
                    drAttendeeListTable.DateString = date;
                }

            }
            dataGrid_prospect.Items.Refresh();
        }


        private void Enable_Filters()
        {

            CalendarExpander.IsEnabled = true;
            ChurchStatusExpander.IsEnabled = true;
            ActivityExpander.IsEnabled = true;
           


        }

        private void Uncheck_All_Filters()
        {
            chkFollowup.IsChecked = false;
            chkResponded.IsChecked = false;
            chkAttended.IsChecked = false;
            chkChurchDateFilter.IsChecked = false;
            chkActivityFilter.IsChecked = false;
            chkChurchStatusFilter.IsChecked = false;
            DateCalendar.IsEnabled = false;

            trvActivities.IsEnabled = false;
        }



        private void chkChurchDateFiler_Checked(object sender, RoutedEventArgs e)
        {


            m_isFilterByDateChecked = true;


            m_isActivityfilterByDateChecked = false;
            chkActivityDateFilter.IsChecked = false;
            DateCalendar.IsEnabled = true;

            //DateCalendar_SelectedDateChanged(null, null);



        }




        private void chkChurchDateFiler_Unchecked(object sender, RoutedEventArgs e)
        {



            m_isFilterByDateChecked = false;
            m_isQueryTableShown = false;

            if (!m_isActivityfilterByDateChecked)
                DateCalendar.IsEnabled = false;

            if (m_alistView)
                return;


            Cursor = Cursors.Wait;

            BuildQuery_and_UpdateGrid();

            Cursor = Cursors.Arrow;

        }




        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            m_alistView = false;
            m_isActivityfilterByDateChecked = false;
            m_AttendanceView = true;

            m_IsActivePanelView = true;
            m_IsActivityPanelView = false;
            m_IsPanelProspectView = false;

            btnPanelAddActivity.Visibility = Visibility.Hidden;
            btnNewRec.IsEnabled = false;
            btnImportRecords.IsEnabled = false;
            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;
           

            Uncheck_All_Filters();


            setbtn_state mydelegate = Set_btnAddActivityState;


            txtSearch.Text = "";
            m_dateIsValid = false;

            // commit datagrid edits and return DataContext to show all records
            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }






            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName

            }





            //System.Windows.Data.CollectionViewSource attendeeViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("attendeeViewSource")));
            // Load data by setting the CollectionViewSource.Source property:
            // attendeeViewSource.Source = [generic data source]
            //m_dbContext.Attendees.Load();
            //m_dbContext.Attendance_Info.Load();
            //System.Windows.Data.CollectionViewSource attendeeViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("attendeeViewSource")));

            ////dataGrid.ItemsSource = m_dbContext.Attendance_Info.Local;
            // attendeeViewSource.Source = m_dbContext.Attendees.Local;

            // Load data by setting the CollectionViewSource.Source property:
            // attendeeViewSource.Source = [generic data source]

            // Load data by setting the CollectionViewSource.Source property:
            // attendance_InfoViewSource.Source = [generic data source]
        }

        private void CopyDataGridtoClipboard(object sender, DataGridRowClipboardEventArgs e)
        {

            var selectedcells = sender as DataGrid;
            var currentCell = e.ClipboardRowContent[dataGrid.CurrentCell.Column.DisplayIndex - 1];
            e.ClipboardRowContent.Add(currentCell);
            if (e.ClipboardRowContent.Count > 3)
            {
                e.ClipboardRowContent.RemoveAt(4);
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

          //  IQueryable<AttRecord> querylinq;

            //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
            //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
            //            join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
            //            where attinfo.Date == m_DateSelected
            //            select new AttRecord
            //            {
            //                id = att.AttendeeId,
            //                fname = att.FirstName,
            //                lname = att.LastName,
            //                date = attinfo.Date,
            //                activity_date = activity.Date,
            //                activity = activity.Activity,
            //                phone = att.Phone,
            //                email = att.Email,
            //                status = attinfo.Status
            //            };

            // FIX ME
            //if (m_isAttendedChecked)
            //{
            //    querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
            //                join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
            //                where attinfo.Status == "Attended" && attinfo.Date == m_DateSelected
            //                select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };


            //}
            //else if (m_isFollowupChecked)
            //{
            //    querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
            //                join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
            //                where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected
            //                select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

            //}
            //else if (m_isRespondedChecked)
            //{
            //    querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
            //                join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
            //                where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected
            //                select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

            //}

            //UpdateDataGrid(querylinq, query);


        }


        private void btnNewRec_Click(object sender, RoutedEventArgs e)
        {




            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            dataGrid_prospect.UpdateLayout();

            //m_DataSet.Tables["AttendeeListTable"].DefaultView.Sort = String.Empty;
            //dataGrid_prospect.Focus();
            ////////then create a new cell info, with the item we wish to edit and the column number of the cell we want in edit mode

            //DataGridCell dgcell = new DataGridCell();
            //DataGridCellInfo cellInfo = new DataGridCellInfo(dgcell, dataGrid.Columns[2]);
            ////////set the cell to be the active one
            //dataGrid_prospect.CurrentCell = cellInfo;
            ////////scroll the item into view
            //dataGrid_prospect.ScrollIntoView(dataGrid_prospect.Items[dataGrid_prospect.Items.Count - 1]); //scroll to last
            //dataGrid_prospect.SelectedItem = dataGrid_prospect.Items.Count - 1;
            ////////begin the edit
            //dataGrid_prospect.BeginEdit();


            string strdate;

            if (m_alistdateIsValid)
                strdate = m_alistDateSelected.ToString("MM-dd-yyyy");
            else
                strdate = "Date Not Valid";


            //int last_rowindex = m_DataSet.Tables["AttendeeListTable"].Rows.Count - 1;
            //DataRow lastdr = m_DataSet.Tables["AttendeeListTable"].Rows[last_rowindex];

            //if (lastdr["Last Name"].ToString() == "" || lastdr["First Name"].ToString() == "")
            //{
            //    // do nothing
            //}
            //else // there is an AttendeeId
            //{
            //    DataRow newdr = m_DataSet.Tables["AttendeeListTable"].NewRow();
            //    //AttendeeId(int)-FirstLastName(string)-LastName(string)-FirstName(String)-Date(string)-Attended(bool)
            //    newdr.ItemArray = new object[] { "", "", "", "", strdate, false };
            //    m_DataSet.Tables["AttendeeListTable"].Rows.Add(newdr);


            //    dataGrid_prospect.DataContext = m_DataSet.Tables["AttendeeListTable"];
            //}
            int last_rowindex = m_lstattendanceTableRows.Count;

            if (m_lstattendanceTableRows.Last().FirstName != "" ||
                m_lstattendanceTableRows.Last().LastName != "")
            {
                AttendanceTableRow newrow = new AttendanceTableRow
                {
                    IsNewrow = true,
                    AttendeeId = 0,
                    FirstName = "",
                    LastName = "",
                    FirstLastName = "",
                    DateString = strdate,
                    Attended = false
                };

                
                m_lstattendanceTableRows.Insert(last_rowindex, newrow);
                dataGrid_prospect.DataContext = m_lstattendanceTableRows;
                dataGrid_prospect.Items.Refresh();
            }





        }

        private void NewMethod()
        {

        }

        private void Ribbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (tabAttendeeList.IsSelected)
            //{

            //    m_alistView = true;
            //    m_AttendanceView = false;
            //    DateCalendar.IsEnabled = true;

            //    FilterOptionsGroupbox.Header = "Date";
            //    DateStackPanel.Visibility = Visibility.Hidden;
            //    //ActivityExpander.Visibility = Visibility.Hidden;
            //    ChurchStatusExpander.Visibility = Visibility.Hidden;


            //    if (m_alistdateIsValid)
            //    {
            //        DateCalendar.SelectedDates.Clear();
            //      //  alisttxtDate.Text = m_alistDateSelected.ToString("MM-dd-yyyy");
            //        DateCalendar.DisplayDate = m_alistDateSelected;
            //        DateCalendar.SelectedDate = m_alistDateSelected;

            //        // DateCalendar_SelectedDateChanged(null, null);

            //    }
            //    else
            //    {
            //        DateCalendar.SelectedDates.Clear();
            //        // DateCalendar.SelectedDate = DateTime.Today;

            //    }


            //    Display_AttendeeListTable_in_Grid(); FIX ME




            // }
            //--------Home Tab -----------------------------------------------------------------------------------------
            // if (tabHome.IsSelected)
            // {

            m_alistView = false;
            m_activityView = false;
            m_AttendanceView = true;

            gbFilterOptions.Header = "Filter Options";
            DateStackPanel.Visibility = Visibility.Visible;
            // ActivityExpander.Visibility = Visibility.Visible;
            ChurchStatusExpander.Visibility = Visibility.Visible;
            // commit datagrid edits and return DataContext to show all records
            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            if (!(m_isFilterByDateChecked || m_isActivityfilterByDateChecked))
            {
                DateCalendar.IsEnabled = false;
            }

            if (m_dateIsValid && m_isFilterByDateChecked)
            {
                DateCalendar.SelectedDates.Clear();
                // txtDate.Text = m_DateSelected.ToString("MM-dd-yyyy"); //FIX ME
                DateCalendar.DisplayDate = m_DateSelected;
                DateCalendar.SelectedDate = m_DateSelected;

                // DateCalendar_SelectedDateChanged(null, null);

            }
            else if (m_isActivityfilterByDateChecked)
            {
                DateCalendar.SelectedDates.Clear();

                DateCalendar.DisplayDate = (DateTime)m_ActivityDateSelected;
                DateCalendar.SelectedDate = m_ActivityDateSelected;
            }
            else
            {
                DateCalendar.SelectedDates.Clear();
                // DateCalendar.SelectedDate = DateTime.Today;
            }



            ShowFiltered_Or_DefaultTable();

            //}

        }






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



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool isAttendedStatusChecked = false;

            if (m_NoCredFile)
            {
                e.Cancel = false;
            }
            else
            {
                dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid.UpdateLayout();
                dataGrid_prospect.UpdateLayout();

                isAttendedStatusChecked = isAttendeeListDirty();

                if (!m_dbContext.ChangeTracker.HasChanges() && isAttendedStatusChecked)
                {



                    MessageBoxResult res = MessageBox.Show("There are checked attendees in the attendee checklist that has not yet been added to the active attendance list.\n\nDiscard changes and exit anyway?", "Attendees not added yet", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    if (res == MessageBoxResult.OK)
                    {

                        //Discard_CheckListandSaveActiveList();
                        e.Cancel = false;

                    }
                    else
                        e.Cancel = true;







                }
                else if (m_dbContext.ChangeTracker.HasChanges() && !isAttendedStatusChecked)
                {

                    MessageBoxResult res = MessageBox.Show("Changes has been made but not saved to the database yet, save changes?", "Changes not saved", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    Cursor = Cursors.Wait;
                    if (res == MessageBoxResult.Yes)
                    {

                        SaveActiveList();
                        e.Cancel = false;

                    }
                    else if (res == MessageBoxResult.No)
                    {
                        e.Cancel = false;
                    }
                    else if (res == MessageBoxResult.Cancel)
                        e.Cancel = true;

                    Cursor = Cursors.Arrow;

                }
                else if (m_dbContext.ChangeTracker.HasChanges() && isAttendedStatusChecked)
                {



                    MessageBoxResult res = MessageBox.Show("Changes has been made but not saved to the database yet.\n\nThere are checked attendees in the attendee checklist that has not yet been added to the active attendance list.\n\nDiscard checklist changes and save active attendance changes to database?", "Save and discard checklist", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    if (res == MessageBoxResult.Yes)
                    {

                        SaveActiveList();
                        e.Cancel = false;

                    }
                    else if (res == MessageBoxResult.No)
                    {
                        e.Cancel = false;
                    }
                    else if (res == MessageBoxResult.Cancel)
                        e.Cancel = true;







                }

            }





        }
        private void ShowFiltered_Or_DefaultTable()
        {
            if (m_isFilterByDateChecked || m_isActivityfilterByDateChecked || m_isActivityFilterChecked ||
                m_isAttendedChecked || m_isFollowupChecked || m_isRespondedChecked)

            {
                if (txtSearch.Text != "")
                {
                    m_DataSet.Tables["QueryTable"].DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                    dataGrid.DataContext = m_DataSet.Tables["QueryTable"];
                    dataGrid.IsReadOnly = true;
                }
                else
                {
                    dataGrid.DataContext = m_DataSet.Tables["QueryTable"];
                    dataGrid.IsReadOnly = true;
                }

            }
            else
            {

                if (txtSearch.Text != "")
                {
                    m_DataSet.Tables["DefaultTable"].DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
                    dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
                    dataGrid.CanUserDeleteRows = false;
                    dataGrid.CanUserAddRows = false;
                    dataGrid.IsReadOnly = false;

                }
                else
                {
                    // (dataGrid.DataContext as DataTable).DefaultView.Sort = "[Last Name] ASC";
                    Display_DefaultTable_in_Grid();
                }

            }

            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
            }
        }
        private void SaveActiveList()
        {

            if (m_DataSet.HasChanges())
            {
               //FIX ME //m_DataSet.Tables["DefaultTable"].DefaultView.RowFilter = String.Empty;
                //foreach (DataRow dr in m_DataSet.Tables["DefaultTable"].Rows)
                //{

                //    // checkmodified record ------------------------------------------------------------------------------------------
                //    if (dr.RowState == DataRowState.Modified)
                //    {

                //        int AttendeeId = int.Parse(dr["AttendeeId"].ToString());

                //        var Attendee = m_dbContext.Attendees.Local.SingleOrDefault(att => att.AttendeeId == AttendeeId);

                //        if (Attendee != null)
                //        {
                //            Attendee.LastName = dr["Last Name"].ToString().Trim();
                //            Attendee.FirstName = dr["First Name"].ToString().Trim();


                //        }


                //    }



                //} // end foreach row
            }


            if (m_dbContext.ChangeTracker.HasChanges())
            {
                Cursor = Cursors.Wait;
                // save contents to database
                m_dbContext.SaveChanges();


                m_DataSet.AcceptChanges();
                //  ShowFiltered_Or_DefaultTable();


                MessageBox.Show("Changes were succesfully saved to the database.");


                Cursor = Cursors.Arrow;
            }
            else
            {
                MessageBox.Show("No changes to save.");
            }



        }

        private void btnGenerateFollowUps_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Make sure all the most recent attendees are added before generating follow-ups, Generate follow-ups anyway?", "Generate follow-ups", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel);
            if (res == MessageBoxResult.OK)
            {

                GenerateDBFollowUps();

                MessageBox.Show("Successfully generated follow-ups!");
            }

        }


        //private void DeleteRecordInAttendeeList(object sender, RoutedEventArgs e)
        //{
        //    var row_select = dataGrid.SelectedItems;



        //    if (row_select.Count != 0)
        //    {

        //        Cursor = Cursors.Wait;

        //                    DeleteRecordInAttendeeListTable(row_select);



        //    }

        //    else
        //    {
        //        MessageBox.Show("At least one record must be selected.", "Select Record", MessageBoxButton.OK, MessageBoxImage.Warning);
        //    }

        //    Cursor = Cursors.Arrow;
        //}

        private void chkActivityFilter_Checked(object sender, RoutedEventArgs e)
        {
            m_isActivityFilterChecked = true;
            trvActivities.IsEnabled = true;


        }

        private void chkActivityFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            m_isActivityFilterChecked = false;
            trvActivities.IsEnabled = false;
            foreach (ActivityGroup group in trvActivities.ItemsSource)
            {
                group.IsSelected = false;
            }

            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;
        }


        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {

            // DataGridCellClipboardEventArgs clip = new DataGridCellClipboardEventArgs()
            // CopyDataGridtoClipboard(dataGrid, );
        }

        private void ActivityTreeView_Checkbox_Checked(object sender, RoutedEventArgs e)
        {


            m_isActivityChecked = true;
            var checkbox = sender as CheckBox;
            m_ActivityName = checkbox.Content.ToString();


            if (m_AttendanceView && m_IsActivePanelView)
            {
                Do_treeview_ActiveView();
            }
           else if (m_IsActivityPanelView && m_AttendanceView)
            {
                Do_treeview_ActivityView();
            }


        }

        private void Do_treeview_ActivityView()
        {

            m_parent_taskId = 0;
            m_child_taskId = 0;

            //int[] array_parent_Ids = new int[] { 4, 9, 12, 15, 19, 27, 32, 36 };

            foreach (ActivityGroup activity_group in m_lstActivities)
            {
                foreach (ActivityTask task in activity_group.lstActivityTasks)
                {
                   



                    if (task.lstsubTasks.Count != 0) // task has children
                    {
                   
                        foreach (ActivityTask subtask in task.lstsubTasks)
                        {

                           
                            // if user selected child node
                            if ((m_ActivityName == subtask.TaskName) && subtask.IsSelected)
                            {
                                m_activitychecked_count++;
                               
                                ActivityPair selectedActivity = new ActivityPair
                                {
                                    ActivityGroup = activity_group.ActivityName,
                                    ActivityPairId = subtask.ActivityId,
                                    ParentTaskName = task.TaskName,
                                    ChildTaskName = subtask.TaskName
                                };
                               // ActivityTask mytask = new ActivityTask { ActivityId = task.ActivityId };
                                m_child_taskId = subtask.ActivityId;
                                m_parent_taskId = task.ActivityId;

                                m_currentSelected_ActivityPair = selectedActivity;
                                //m_currentSelected_ActivityTask = mytask;


                                if (m_activitychecked_count == 1)
                                {
                                    m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;
                                }

                                txtblkTaskDescription.Text = subtask.Description;
                                task.IsSelected = true; //check parent

                                break;
                            }
                           
                           
                           

                        }
                    }
                    else if (task.lstsubTasks.Count == 0) // task has no children
                    {

                  
                        if ((m_ActivityName == task.TaskName) && task.IsSelected)
                        {
                            m_activitychecked_count++;
                            ActivityPair selectedActivity = new ActivityPair
                            {
                                ActivityGroup = activity_group.ActivityName,
                                ActivityPairId = task.ActivityId,
                                ParentTaskName = task.TaskName,
                                ChildTaskName = "n/a"
                            };
                            m_parent_taskId = task.ActivityId;
                            
                            m_currentSelected_ActivityPair = selectedActivity;
                            ActivityTask mytask = new ActivityTask { ActivityId = task.ActivityId };
                            m_currentSelected_ActivityTask = mytask;

                            if (m_activitychecked_count == 1)
                            {
                                m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;

                            }

                            txtblkTaskDescription.Text = task.Description;
                            break;
                        }



                    }
                    else { }


                }
            }


            m_activitychecked_count = 1;
            ClearTreeViewCheckboxes_except_clickedOne(m_child_taskId,m_parent_taskId);
            m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;


        }
        private void ClearTreeViewCheckboxes_except_clickedOne(int childtaskId, int parenttaskId)
        {
            // if task ids are different
            // loop through all the tasks and deselect any task that is not the current one selected

            if (m_previousSelected_ActivityPair != m_currentSelected_ActivityPair)
            {
            
                foreach (ActivityTask task in m_lstActivityTask)
                {
                   
                        if ( (task.ActivityId == parenttaskId) || (task.ActivityId == childtaskId) )
                        {
                            //do nothing
                        }
                        else
                        {
                            task.IsSelected = false;
                        }   
                        
                        
                        

                   
                   
                    
                }

                
            }

           
            //m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;
       //     m_activitychecked_count = 1;



        }
        private void Do_treeview_ActiveView()
        {



            foreach (ActivityGroup activity_group in m_lstActivities)
            {
                foreach (ActivityTask task in activity_group.lstActivityTasks)
                {


                    if (task.lstsubTasks.Count != 0) // task has children
                    {
                      
                        foreach (ActivityTask subtask in task.lstsubTasks)
                        {
                         
                            if ((m_ActivityName == subtask.TaskName) && subtask.IsSelected)
                            {
                                ActivityPair selectedActivity = new ActivityPair
                                {
                                    ActivityGroup = activity_group.ActivityName,
                                    ActivityPairId = subtask.ActivityId,
                                    ParentTaskName = task.TaskName,
                                    ChildTaskName = subtask.TaskName
                                };

                                ActivityTask mytask = new ActivityTask { ActivityId = task.ActivityId };
                                m_child_taskId = subtask.ActivityId;
                                m_parent_taskId = task.ActivityId;

                                m_currentSelected_ActivityPair = selectedActivity;
                                m_currentSelected_ActivityTask = mytask;

                                if (m_activitychecked_count == 1)
                                {
                                    m_previousSelected_ActivityTask = m_currentSelected_ActivityTask;
                                }


                                txtblkTaskDescription.Text = subtask.Description;
                                task.IsSelected = true; //check parent

                                break;
                            }

                        }
                    }
                    else if (task.lstsubTasks.Count == 0) // task has no children
                    {
          

                        if ((m_ActivityName == task.TaskName) && task.IsSelected)
                        {
                            ActivityPair selectedActivity = new ActivityPair
                            {
                                ActivityGroup = activity_group.ActivityName,
                                ActivityPairId = task.ActivityId,
                                ParentTaskName = task.TaskName,
                                ChildTaskName = "n/a"
                            };
                            ActivityTask mytask = new ActivityTask { ActivityId = task.ActivityId };

                            m_parent_taskId = task.ActivityId;

                            m_currentSelected_ActivityPair = selectedActivity;
                            m_currentSelected_ActivityTask = mytask;

                            if (m_activitychecked_count == 1)
                            {
                                m_previousSelected_ActivityTask = m_currentSelected_ActivityTask;
                            }

                            txtblkTaskDescription.Text = task.Description;
                            break;
                        }

                            

                    }
                    else { }

                  
                }
            }
           
            m_activitychecked_count = 1;
            ClearTreeViewCheckboxes_except_clickedOne(m_child_taskId, m_parent_taskId);
            BuildQuery_and_UpdateGrid();
           
        }
        //private void Do_treeview_CheckedActivity()
        //{

        //    // check if we have a activity pair selected



           
        //        int attendeeId = m_activity_row_selected.AttendeeId;

        
        //        // if cell already contain activity that is being selected then do not update the table, this function was called from an indirect selection
        //        // only reflect activity status


        //        foreach (ActivityGroup activity_group in m_lstActivities)
        //        {
        //            foreach (ActivityTask task in activity_group.lstActivityTasks)
        //            {


        //                if (task.lstsubTasks.Count != 0) // task has children
        //                {
        //                    foreach (ActivityTask subtask in task.lstsubTasks)
        //                    {

        //                        if ((m_ActivityName == subtask.TaskName) && subtask.IsSelected)
        //                        {
        //                            task.IsSelected = true; //check parent
        //                        string activityName = activity_group.ActivityName;
        //                        string ParentName = task.TaskName;
        //                        string ChildName = subtask.TaskName;

        //                            m_activitychecked_count++;




        //                        /* Check if selected activity is already in the attendee list
        //                         * If already in list, display a message box telling user the activity is already in the list
        //                         * and he should select a date to update the activity
        //                         */

        //                        var qActivityIsInList = m_activity_row_selected.ActivityList.SingleOrDefault(apair => apair.AttendeeId == m_activity_row_selected.AttendeeId &&
        //                                                                                                   apair.ActivityGroup == activityName &&
        //                                                                                                   apair.ParentTaskName == ParentName &&
        //                                                                                                   apair.ChildTaskName == ChildName);
                                                         
                              

        //                        // activity not in list, add new activity to list
        //                        if (qActivityIsInList == null)
        //                        {
        //                            ActivityPair nap = new ActivityPair() { };
        //                            nap.AttendeeId = m_activity_row_selected.AttendeeId;
        //                            nap.ActivityGroup = activityName;
        //                            nap.ParentTaskName = ParentName;
        //                            nap.ChildTaskName = ChildName;

        //                            // add activity to activitylist for activityTableRow
        //                            m_activity_row_selected.ActivityList.Add(nap);
        //                            // Add activity to DefaultTableRow
        //                            DefaultTableRow selectdefaultrow = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == attendeeId);
        //                            selectdefaultrow.ActivityList.Add(nap);

        //                            //reflect activity last attended of attendee in default table row

        //                            if (selectdefaultrow.ActivityList.Any() )
        //                            {
        //                                ActivityPair lastActivityRec = selectdefaultrow.ActivityList.OrderByDescending(rec => rec.Date).ToList().FirstOrDefault();
        //                                selectdefaultrow.Activity = lastActivityRec.ToString();
                                        

        //                            }
                              
        //                        }
        //                        /*Activity in list*/
        //                        else
        //                        {
        //                            MessageBox.Show("Activity already in list, please select an activity date to update the activity", "Activity already exist", MessageBoxButton.OK, MessageBoxImage.Exclamation);    
        //                        }
                                

                             
                             
                             
                                    
                               
                               
        //                            txtblkTaskDescription.Text = subtask.Description;


        //                            break;
        //                        }

        //                    }
        //                }
        //                else if ((m_ActivityName == task.TaskName) && task.IsSelected)
        //                {
                            
        //                    task.IsSelected = true;
        //                        string activityName = activity_group.ActivityName;
        //                        string ParentName = task.TaskName;
                                

        //                     m_activitychecked_count++;

        //                /* Check if selected activity is already in the attendee list
        //                   * If already in list, only update date, if not in list create and add a new activity to the list
        //                   */

        //                var qActivityIsInList = m_activity_row_selected.ActivityList.SingleOrDefault(apair => apair.AttendeeId == m_activity_row_selected.AttendeeId &&
        //                                                                                           apair.ActivityGroup == activityName &&
        //                                                                                           apair.ParentTaskName == ParentName);



        //                // activity not in list, add new activity to list
        //                if (qActivityIsInList == null)
        //                {
        //                    ActivityPair nap = new ActivityPair() { };
        //                    nap.AttendeeId = m_activity_row_selected.AttendeeId;
        //                    nap.ActivityGroup = activityName;
        //                    nap.ParentTaskName = ParentName;


        //                    // activity automatically gets added to m_dbcontext.activities withth new activity!
        //                    m_activity_row_selected.ActivityList.Add(nap);
        //                    //reflect activity list of attendee in default table row
        //                    var selectdefaultrow = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == attendeeId);
        //                    selectdefaultrow.ActivityList = m_activity_row_selected.ActivityList;
        //                }
        //                /*Activity in list*/
        //                else
        //                {
        //                    MessageBox.Show("Activity already in list, please select an activity date to update the activity", "Activity already exist", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        //                }





        //                txtblkTaskDescription.Text = task.Description;

        //                    break;

        //                }
        //            }
        //        }
            
        //}

      
      
           

      

        private void RemoveActivity_from_ActivityTableRowAttendeeActivityList(ref ActivityPair ap)
        {

            List<int> idx = new List<int> { };

            int i = 0;
           


                foreach (ActivityPair activity in m_activity_row_selected.ActivityList)
                {
                    if (activity.AttendeeId == ap.AttendeeId &&
                        activity.ActivityGroup == ap.ActivityGroup &&
                        activity.ParentTaskName == ap.ParentTaskName &&
                        activity.ChildTaskName == ap.ChildTaskName)
                    {
                        idx.Add(m_activity_row_selected.ActivityList.IndexOf(activity) );
                        
                    }
                    
                }

                foreach (int idxx in idx)
                {
                    m_activity_row_selected.ActivityList.RemoveAt(idxx);
                }
                
           
        }


      


        private void BuildQuery_and_UpdateGrid()
        {





            string query = "0";

            IQueryable<DefaultTableRow> querylinq = null;
            string strActivity = "";
            Cursor = Cursors.Wait;

            if (m_AttendanceView)
            {
                
                if (m_currentSelected_ActivityTask != null)
                {
                    strActivity = m_currentSelected_ActivityPair.ToString();
                }
                    //Date, Attended, Activity
                    if (m_isFilterByDateChecked && m_isChurchStatusFilterChecked && m_isAttendedChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {

                      

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && attinfo.Date == m_DateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }


                    //Date, Followup, Activity
                    else if (m_isFilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isFollowupChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                      

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Date, Responded, Activity
                    else if (m_isFilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isRespondedChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                      

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity date, Atttended, Activity
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isAttendedChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                      

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && activity.Date == m_ActivityDateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity date, FollowUp, Activity
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isFollowupChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                      

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Follow-Up" && activity.Date == m_ActivityDateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity date, Responded, Activity
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isRespondedChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Responded" && attinfo.ActivityDate == m_ActivityDateSelected && attinfo.Activity == m_ActivityName.ToString()
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && activity.Date == m_ActivityDateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };

                }
                    //Activity date, Atttended
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isAttendedChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && attinfo.ActivityDate == m_ActivityDateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };
                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && activity.Date == m_ActivityDateSelected
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };

                }
                    //Activity date, FollowUp
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isFollowupChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Follow-Up" && attinfo.ActivityDate == m_ActivityDateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Follow-Up" && activity.Date == m_ActivityDateSelected
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };

                }
                    //Activity date, Responded
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isRespondedChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Responded" && attinfo.ActivityDate == m_ActivityDateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };
                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Responded" && activity.ToString().Contains(m_ActivityName)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };


                }
                    //Date, Atttended
                    else if (m_isFilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isAttendedChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && attinfo.Date == m_DateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && attinfo.Date == m_DateSelected
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Date, FollowUp
                    else if (m_isFilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isFollowupChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Follow-Up" && attinfo.Date == m_DateSelected
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Date, Responded
                    else if (m_isFilterByDateChecked && m_dateIsValid && m_isChurchStatusFilterChecked && m_isRespondedChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Responded" && attinfo.Date == m_DateSelected
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Date
                    else if (m_isFilterByDateChecked && !m_isChurchStatusFilterChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Date == m_DateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Date == m_DateSelected
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Date, activity
                    else if (m_isFilterByDateChecked && m_dateIsValid && !m_isChurchStatusFilterChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Date == m_DateSelected && attinfo.Activity == m_ActivityName
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Date == m_DateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity date
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && !m_isChurchStatusFilterChecked && !m_isActivityFilterChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.ActivityDate == m_ActivityDateSelected
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where activity.Date == m_ActivityDateSelected
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity date, activity
                    else if (m_isActivityfilterByDateChecked && m_dateIsValid && !m_isChurchStatusFilterChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.ActivityDate == m_ActivityDateSelected && attinfo.Activity == m_ActivityName
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where activity.Date == m_ActivityDateSelected && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity
                    else if (!m_isActivityfilterByDateChecked && !m_isFilterByDateChecked && !m_isChurchStatusFilterChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Activity == m_ActivityName
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity, Attended
                    else if (!m_isActivityfilterByDateChecked && !m_isFilterByDateChecked && m_isChurchStatusFilterChecked && m_isAttendedChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && attinfo.Activity == m_ActivityName
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where (attinfo.Status == "Attended" || attinfo.Status == "Responded") && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity, Follow-Up
                    else if (!m_isActivityfilterByDateChecked && !m_isFilterByDateChecked && m_isChurchStatusFilterChecked && m_isFollowupChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where (attinfo.Status == "Follow-Up") && attinfo.Activity == m_ActivityName
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };
                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where (attinfo.Status == "Follow-Up") && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Activity, Responded
                    else if (!m_isActivityfilterByDateChecked && !m_isFilterByDateChecked && m_isChurchStatusFilterChecked && m_isRespondedChecked && m_isActivityFilterChecked && m_isActivityChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Responded" && attinfo.Activity == m_ActivityName
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Responded" && activity.ToString().Contains(strActivity)
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Attended
                    else if (!m_isActivityfilterByDateChecked && !m_isFilterByDateChecked && !m_isActivityFilterChecked && m_isChurchStatusFilterChecked && m_isAttendedChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Attended"
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Attended"
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                }
                    //Responded
                    else if (!m_isActivityfilterByDateChecked && !m_isFilterByDateChecked && !m_isActivityFilterChecked && m_isChurchStatusFilterChecked && m_isRespondedChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Responded"
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };
                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Responded"
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };

                    }
                    //Follow-up
                    else if (!m_isActivityfilterByDateChecked && !m_isFilterByDateChecked && !m_isActivityFilterChecked && m_isChurchStatusFilterChecked && m_isFollowupChecked)
                    {
                        //querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                        //            join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                        //            where attinfo.Status == "Follow-Up"
                        //            select new AttRecord { id = att.AttendeeId, fname = att.FirstName, lname = att.LastName, date = attinfo.Date, activity_date = attinfo.ActivityDate, activity = attinfo.Activity, phone = attinfo.Phone, email = attinfo.Email, status = attinfo.Status };

                        querylinq = from att in m_dbContext.Attendees.Local.AsQueryable()
                                    join attinfo in m_dbContext.Attendance_Info.Local on att.AttendeeId equals attinfo.AttendeeId
                                    join activity in m_dbContext.Activities.Local on attinfo.AttendeeId equals activity.AttendeeId
                                    where attinfo.Status == "Follow-Up"
                                    select new DefaultTableRow
                                    {
                                        AttendeeId = att.AttendeeId,
                                        FirstName = att.FirstName,
                                        LastName = att.LastName,
                                        FirstLastName = att.FirstName.ToUpper() + " " + att.LastName.ToUpper(),
                                        Church_Last_Attended = attinfo.Date.ToString("MM-dd-yyyy"),
                                        Activity_Last_Attended = activity.DateString,
                                        Activity = activity.ToString(),
                                        ActivityList = att.ActivityList,
                                        AttendanceList = att.AttendanceList,
                                        Phone = att.Phone,
                                        Email = att.Email,
                                        ChurchStatus = attinfo.Status
                                    };
                    }
                    else
                    {
                       
                        querylinq = null;
                    }
               




            }
           
        
            if (querylinq != null)
            {
                dataGrid.DataContext = querylinq;
                m_lstQueryTableRows = querylinq.ToList();
            }
            else
                dataGrid.DataContext = m_lstdefaultTableRows;


            Cursor = Cursors.Arrow;
        }
        private void ActivityTreeView_Checkbox_UnChecked(object sender, RoutedEventArgs e)
        {
            //m_isActivityChecked = false;
            //var checkbox = sender as CheckBox;
            //m_ActivityName = checkbox.Content.ToString();



            //if (m_AttendanceView)
            //{
            //    foreach (ActivityGroup activity_group in m_lstActivities)
            //    {
            //        foreach (ActivityTask task in activity_group.lstActivityTasks)
            //        {


            //            if (task.lstsubTasks.Count != 0) // task has children
            //            {
            //                foreach (ActivityTask subtask in task.lstsubTasks)
            //                {


            //                    if ((m_ActivityName == subtask.TaskName) && subtask.IsSelected)
            //                    {

            //                        BuildQuery_and_UpdateGrid();

            //                        txtblkTaskDescription.Text = subtask.Description;
            //                        task.IsSelected = true; //check parent
            //                        break;
            //                    }

            //                }
            //            }
            //            else if ((m_ActivityName == task.TaskName) && task.IsSelected)
            //            {
            //                BuildQuery_and_UpdateGrid();
            //                txtblkTaskDescription.Text = task.Description;

            //                break;

            //            }
            //        }
            //    }
            //}
            //else if (m_activityView)
            //{
               // m_activity_row_select = (List<ActivityTableRow>)dataGrid_activity.SelectedItems;
               //FIX ME
                //if (m_activity_row_select.AttendeeId != m_old_activityRow_attendeeId)
                //{


                   


                    //if (m_activity_row_selected != null)
                    //{


                    //    int attendeeid = m_activity_row_selected.AttendeeId;


                    //    foreach (ActivityGroup activity_group in m_lstActivities)
                    //    {
                    //        foreach (ActivityTask task in activity_group.lstActivityTasks)
                    //        {


                    //            if (task.lstsubTasks.Count != 0) // task has children
                    //            {
                    //                foreach (ActivityTask subtask in task.lstsubTasks)
                    //                {

                    //                    if ((m_ActivityName == subtask.TaskName) && subtask.IsSelected == false)
                    //                    {
                    //                        task.IsSelected = false; //check parent
                    //                        int parentId = task.ActivityId;
                    //                        int childId = subtask.ActivityId;

                    //                            ActivityPair ap = new ActivityPair() { };
                    //                            ap.AttendeeId = attendeeid;
                    //                            ap.ActivityGroup = activity_group.ActivityName;
                    //                            ap.ChildTaskName = subtask.TaskName;
                    //                            ap.ParentTaskName = task.TaskName;

                    //                    RemoveActivity_from_ActivityTableRowAttendeeActivityList(ref ap);


                    //                        // UpdateActivityFromLookUpTable(attendeeId, task.TaskName, subtask.TaskName);

                    //                        txtblkTaskDescription.Text = subtask.Description;


                    //                        break;
                    //                    }

                    //                }
                    //            }
                    //            else if ((m_ActivityName == task.TaskName) && task.IsSelected == false)
                    //            {
                    //                int parentId = task.ActivityId;
                    //                int childId = task.ActivityId;

                    //                //  DeleteActivityFromLookUpTable(attendeeId, parentId, childId);

                    //                // UpdateActivityFromLookUpTable(attendeeId, task.TaskName, "");
                    //                txtblkTaskDescription.Text = task.Description;

                    //                break;

                    //            }
                    //        }
                    //        // }
                    //    }

                    //}
                    //else
                    //{
                    //    MessageBox.Show("Must select at least one row", "Must select row...", MessageBoxButton.OK, MessageBoxImage.Error);
                    //}

                //}
                //else
                //{
                //    // Do Nothing uncheck is evoked by user selecting a different row
                //}
          //  }

        }

   
        private void GridsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (dataGrid_prospect.Columns.Count > 1)
            {
                dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid_prospect.UpdateLayout();
            }

            // commit datagrid edits and return DataContext to show all records
            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid.UpdateLayout();
            }

            //if (dataGrid_activity.Columns.Count > 1)
            //{
            //    dataGrid_activity.CommitEdit(DataGridEditingUnit.Row, true);
            //    dataGrid_activity.UpdateLayout();
            //}

            if ((GridsTabControl.SelectedItem as TabItem).Name == "ActiveTab")
            {
               

                if (!m_AttendanceView) // prevent from executing twice when already on the same tab
                {

                    m_alistView = false;
                    m_AttendanceView = true;
                    m_activityView = false;

                    //delete last row if no new attendee firstname or lastname entered
                    if (m_lstattendanceTableRows.LastOrDefault().FirstName == "" ||
                        m_lstattendanceTableRows.LastOrDefault().LastName == "")
                    {
                        m_lstattendanceTableRows.RemoveAt(m_lstattendanceTableRows.Count - 1);
                    }





                    // save current state of side panel
                    if (m_AttendanceView)
                    {
                        SaveActivePanelState();

                    }
                    else if (m_alistView)
                    {
                        SaveProspectPanelState();
                    }
                    else if (m_activityView)
                    {

                        SaveActivityPanelState();
                    }

                   

                    // load ActiveTab state from TabState class
                    txtSearch.Text = "";
                    txtSearch.Text = m_TabState.txtSearchActiveState;

                    ClearTreeView();
                    LoadActivePanelState();



                    btnNewRec.IsEnabled = false;
                    btnDelete.IsEnabled = true;
                    btnImportRecords.IsEnabled = false;

                    BuildQuery_and_UpdateGrid();
                    if (!m_IsActivePanelView)
                    {
                        Show_activeview_Panel();
                    }
                    
                }
            }
            if ((GridsTabControl.SelectedItem as TabItem).Name == "ProspectListTab")
            {
             
                // only do this once, if the page is loaded already no need to run throught this code again
                if (!m_alistView)
                {
                    m_alistView = true;
                    m_AttendanceView = false;
                    m_activityView = false;

                    // save current state of side panel
                    if (m_AttendanceView)
                    {
                        SaveActivePanelState();

                    }
                    else if (m_alistView)
                    {
                        SaveProspectPanelState();
                    }
                    else if (m_activityView)
                    {
                        SaveActivityPanelState();

                    }




                    // load ProspectTab state from TabState class
                    LoadProspectPanelState();



                    btnImportRecords.IsEnabled = true;
                    btnImportRecords.Content = "Update Changes";

                    btnNewRec.IsEnabled = true;
                    btnDelete.IsEnabled = false;

                    Display_AttendeeListTable_in_Grid();
                    if (!m_IsPanelProspectView)
                    {
                        Show_prospectview_Panel();
                    }
                    
                }
            }
         


        }

        private void LoadActivityPanelState()
        {
            txtSearch.IsEnabled = false;
            chkActivityDateFilter.IsChecked = m_TabState.ActivityPanel_isActivityChecked;
            chkActivityDateFilter.IsChecked = m_TabState.ActivityPanel_isActivityDateChecked;
        }
        private void SaveActivityPanelState()
        {
            m_TabState.txtSearchActivityState = txtSearch.Text;
            m_TabState.ActivityPanel_isActivityDateChecked = chkActivityDateFilter.IsChecked;
            m_TabState.ActivityPanel_isActivityChecked = chkActivityFilter.IsChecked;
        }
        private void SaveProspectPanelState()
        {
            m_TabState.txtSearchProspectState = txtSearch.Text;
            m_TabState.ProspectPanel_isFilterbyDateChecked = chkChurchDateFilter.IsChecked;
        }
        private void SaveActivePanelState()
        {
            m_TabState.txtSearchActiveState = txtSearch.Text;
            m_TabState.ActivePanel_isAttendedChecked = chkAttended.IsChecked;
            m_TabState.ActivePanel_isFollowUpChecked = chkFollowup.IsChecked;
            m_TabState.ActivePanel_isRespondedChecked = chkResponded.IsChecked;

            m_TabState.ActivePanel_isChurchStatusChecked = chkChurchStatusFilter.IsChecked;

            m_TabState.ActivePanel_isFilterbyDateChecked = chkChurchDateFilter.IsChecked;
            m_TabState.ActivePanel_isFilterbyActivityDateChecked = chkActivityDateFilter.IsChecked;
            m_TabState.ActivePanel_isActivityChecked = chkActivityFilter.IsChecked;
        }
        private void LoadProspectPanelState()
        {
           
            txtSearch.Text = "";
            txtSearch.Text = m_TabState.txtSearchProspectState;
            chkChurchDateFilter.IsChecked = m_TabState.ProspectPanel_isFilterbyDateChecked;
        }
        private void LoadActivePanelState()
        {
           
            chkAttended.IsChecked = m_TabState.ActivePanel_isAttendedChecked;
            chkFollowup.IsChecked = m_TabState.ActivePanel_isFollowUpChecked;
            chkResponded.IsChecked = m_TabState.ActivePanel_isRespondedChecked;
            chkChurchDateFilter.IsChecked = m_TabState.ActivePanel_isFilterbyDateChecked;
            chkActivityDateFilter.IsChecked = m_TabState.ActivePanel_isFilterbyActivityDateChecked;
            chkActivityFilter.IsChecked = m_TabState.ActivePanel_isActivityChecked;
            chkChurchStatusFilter.IsChecked = m_TabState.ActivePanel_isChurchStatusChecked;
        }
        private void Show_activeview_Panel()
        {


           
                spFilterOptions.Children.Clear();

                gbFilterOptions.Header = "Filter Table by:";




                if (!DateStackPanel.Children.Contains(chkActivityDateFilter))
                {
                    DateStackPanel.Children.Add(chkActivityDateFilter);
                    //  btnAddActivity.Visibility = Visibility.Hidden;
                }



                if (!DateStackPanel.Children.Contains(chkChurchDateFilter))
                {

                    chkChurchDateFilter.Content = "Church Date";

                    DateStackPanel.Children.Insert(0, chkChurchDateFilter);
                }
                else
                {
                    chkChurchDateFilter.Content = "Church Date";

                }

                btnPanelAddActivity.Visibility = Visibility.Hidden;


                spFilterOptions.Children.Add(CalendarExpander);
                spFilterOptions.Children.Add(ChurchStatusExpander);
                spFilterOptions.Children.Add(ActivityExpander);

                //m_IsPanelActiveView = true;
                //m_IsActivityPanelView = false;
                //m_IsPanelProspectView = false;
                txtblkTaskDescription.Text = "";

                m_IsActivityPanelView = false;
                m_IsActivePanelView = true;
                m_IsPanelProspectView = false;

            


        }

        private void Show_prospectview_Panel()
        {
            spFilterOptions.Children.Clear();
            gbFilterOptions.Header = "Edit Table";

            m_IsPanelProspectView = true;
            m_IsActivityPanelView = false;
            m_IsActivePanelView = false;


            if (DateStackPanel.Children.Contains(chkActivityDateFilter))
                DateStackPanel.Children.Remove(chkActivityDateFilter);

            if (!DateStackPanel.Children.Contains(chkChurchDateFilter))
            {

                chkChurchDateFilter.Content = "Church Date";
                // chkChurchDateFilter.IsChecked = true;
                DateStackPanel.Children.Insert(0, chkChurchDateFilter);
            }
            else
            {
                chkChurchDateFilter.Content = "Church Date";
                //chkChurchDateFilter.IsChecked = true;
            }


            spFilterOptions.Children.Add(CalendarExpander);
            //m_IsPanelProspectView = true;
            //m_IsActivityPanelView = false;
            //m_IsPanelActiveView = false;
            txtblkTaskDescription.Text = "";

        }
    
        private void GridsTabControl_MouseUp(object sender, MouseButtonEventArgs e)
        {

            var tabctrl = sender as TabControl;

            if (tabctrl.SelectedIndex == 1)
            {
                if (dataGrid_prospect.Columns.Count > 1)
                {
                    dataGrid_prospect.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                    dataGrid_prospect.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                }
            }
            else if (tabctrl.SelectedIndex == 0)
            {
                if (dataGrid.Columns.Count > 1)
                {
                    dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                    dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
                }
            }
            //else if (tabctrl.SelectedIndex == 2)
            //{
            //    if (dataGrid_activity.Columns.Count > 1)
            //    {
            //        dataGrid_activity.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
            //        dataGrid_activity.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
            //    }
            //}

        }

        private void chkActivityDateFilter_Checked(object sender, RoutedEventArgs e)
        {

            m_isActivityfilterByDateChecked = true;
            m_isFilterByDateChecked = false;

            chkChurchDateFilter.IsChecked = false;
            DateCalendar.IsEnabled = true;
            // DateCalendar.SelectedDates.Clear();






        }

        private void chkActivityDateFilter_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isActivityfilterByDateChecked = false;
            if (!m_isFilterByDateChecked)
                DateCalendar.IsEnabled = false;

            if (m_alistView)
                return;
            else
            {

                Cursor = Cursors.Wait;
                BuildQuery_and_UpdateGrid();
                Cursor = Cursors.Arrow;
            }



        }

        private void chkChurchStatusFilter_Checked(object sender, RoutedEventArgs e)
        {
            m_isChurchStatusFilterChecked = true;
            m_isQueryTableShown = true;

            chkAttended.IsEnabled = true;
            chkFollowup.IsEnabled = true;
            chkResponded.IsEnabled = true;

            if (m_isAttendedChecked || m_isRespondedChecked || m_isFollowupChecked)
                BuildQuery_and_UpdateGrid();
            else
                m_isQueryTableShown = false;



        }

        private void chkChurchStatusFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            m_isChurchStatusFilterChecked = false;
            m_isQueryTableShown = false;

            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;


            Cursor = Cursors.Wait;
            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;

        }

        private void dataGrid_activity_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            
            var selectedItems = sender as DataGrid;

            if (selectedItems.SelectedItem != null)
            {
                m_activity_row_selected = (ActivityTableRow)selectedItems.SelectedItem;
              
            }


            ClearTreeView();
         
        }


        private void ClearTreeView()
        {




            m_activitychecked_count = 0;
            //loop through ActivityTree and update the checkboxes
            foreach (ActivityGroup activity_group in m_lstActivities)
            {
                foreach (ActivityTask task in activity_group.lstActivityTasks)
                {
                    //check the box
                    task.IsSelected = false;
                   
                    if (task.lstsubTasks.Count != 0) // task has children
                    {
                        foreach (ActivityTask subtask in task.lstsubTasks)
                        {
                            subtask.IsSelected = false;
                        }

                    }



                }
            }
        }
     

        private void GetActivityFromTreeView(int attendeeId, ref List<ActivityPair> aList)
        {
            foreach (ActivityGroup activity_group in m_lstActivities)
            {
                foreach (ActivityTask task in activity_group.lstActivityTasks)
                {


                    if (task.lstsubTasks.Count != 0) // task has children
                    {
                        foreach (ActivityTask subtask in task.lstsubTasks)
                        {


                            if ((m_ActivityName == subtask.TaskName) && subtask.IsSelected)
                            {

                                BuildQuery_and_UpdateGrid();

                                txtblkTaskDescription.Text = subtask.Description;
                                task.IsSelected = true; //check parent
                                break;
                            }

                        }
                    }
                    else if ((m_ActivityName == task.TaskName) && task.IsSelected)
                    {
                        BuildQuery_and_UpdateGrid();
                        txtblkTaskDescription.Text = task.Description;

                        break;

                    }
                }
            }
        }
      
       

      
    
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            IEnumerable<Attendance_Info> AttendanceInfoRow_select = m_AttendeeInfo_grid.SelectedItems.Cast<Attendance_Info>();
            IEnumerable<ActivityPair> ActivityRow_select = m_Activity_grid.SelectedItems.Cast<ActivityPair>();

            if (AttendanceInfoRow_select.Any() )
            {

                DeleteRecordFromAttendanceInfoTable(AttendanceInfoRow_select);


              

                MessageBox.Show("Attendance record removed successfully.\n\nChanges has not been saved to the database until the Save button is clicked.", "Records removed", MessageBoxButton.OK, MessageBoxImage.None);

            }
            else if (ActivityRow_select.Any() )
            {


                DeleteRecordFromActivitiesTable(ActivityRow_select);
           

             

                MessageBox.Show("Activity record removed successfully.\n\nChanges has not been saved to the database until the Save button is clicked.", "Records removed", MessageBoxButton.OK, MessageBoxImage.None);
            }
            else
            {
                MessageBox.Show("Must select at least one row", "Select one record", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Cursor = Cursors.Arrow;
        }
        private void DeleteRecordFromActivitiesTable(IEnumerable<ActivityPair> row_select)
        {
            List<ActivityPair> rowsToBeDeleted = new List<ActivityPair>(row_select);

            foreach (ActivityPair dr in rowsToBeDeleted)
            {

                var AttActivityInforec = m_dbContext.Activities.Local.SingleOrDefault(id => id.ActivityPairId == dr.ActivityPairId);
                if (AttActivityInforec != null)
                {
                    m_dbContext.Activities.Remove(AttActivityInforec);
                }

                m_default_row_selected.ActivityList.Remove(dr);
            }

            Display_ActivityList_in_Grid();

        }
        private void DeleteRecordFromAttendanceInfoTable(IEnumerable<Attendance_Info> row_select)
        {

           
          
            List<Attendance_Info> rowsToBeDeleted = new List<Attendance_Info>(row_select);

          
            
            foreach (Attendance_Info dr in rowsToBeDeleted)
            {
            
                var AttInforec = m_dbContext.Attendance_Info.Local.SingleOrDefault(id => id.Attendance_InfoId == dr.Attendance_InfoId);
                if (AttInforec != null)
                {
                    m_dbContext.Attendance_Info.Remove(AttInforec);
                }

                m_default_row_selected.AttendanceList.Remove(dr);

            }

            Display_AttendanceList_in_Grid();

        }

        private void Display_ActivityList_in_Grid()
        {
            if (m_Activity_grid != null)
            {
                m_Activity_grid.DataContext = m_default_row_selected.ActivityList;
                m_Activity_grid.Items.Refresh();
            }
            
        }

        private void Display_AttendanceList_in_Grid()
        {
            if (m_AttendeeInfo_grid != null)
            {
                m_AttendeeInfo_grid.DataContext = m_default_row_selected.AttendanceList;
                m_AttendeeInfo_grid.Items.Refresh();
            }
            
        }
        private void GrdAttendee_InfoList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

        private void GrdAttendee_InfoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void GrdAttendee_ActivityList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

     

        private void btnExpandHistory_Click(object sender, RoutedEventArgs e)
        {
                      

            if (dataGrid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed)
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
                Disable_Filters();
            }
            else
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
                Enable_Filters(); 
              
            }

            if (!m_IsActivePanelView)
            {
                Show_activeview_Panel();
            }
        }
     

        private void dataGrid_LoadingRowDetails(object sender, DataGridRowDetailsEventArgs e)
        {


          
            // get GrdAttendee_InfoList element within the DataTemplate
            m_AttendeeInfo_grid = e.DetailsElement.FindName("GrdAttendee_InfoList") as DataGrid;

            m_AttendeeInfo_grid.DataContext = m_default_row_selected.AttendanceList.OrderByDescending(rec => rec.Date);
            // get GrdAttendee_ActivityList element within the DataTemplate
            m_Activity_grid = e.DetailsElement.FindName("GrdAttendee_ActivityList") as DataGrid;

            m_Activity_grid.DataContext = m_default_row_selected.ActivityList.OrderByDescending(rec => rec.Date);

        
            //hide grid columns
            if (m_AttendeeInfo_grid.Columns.Count > 0)
            {
                m_AttendeeInfo_grid.Columns[0].Visibility = Visibility.Hidden; //AttedeeId
         
            }
            if (m_Activity_grid.Columns.Count > 0)
            {
                m_Activity_grid.Columns[0].Visibility = Visibility.Hidden; // AttendeeId
                m_Activity_grid.Columns[2].Width = 300; // Activity column

            }

        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var datagrid = sender as DataGrid;

            m_default_row_selected = (DefaultTableRow)dataGrid.SelectedItem;

            var queryAttRec = m_dbContext.Attendees.Local.SingleOrDefault(attrec => attrec.AttendeeId == m_default_row_selected.AttendeeId);

            var text = e.EditingElement as TextBox;

            if (e.Column.Header != null)
            {
                if (e.Column.Header.ToString() == "Email")
                {

                    if (queryAttRec != null)
                    {
                        queryAttRec.Email = text.Text;
                    }
                    m_default_row_selected.Email = text.Text;

                }
                else if (e.Column.Header.ToString() == "Phone")
                {


                    if (queryAttRec != null)
                    {
                        queryAttRec.Phone = text.Text;
                    }
                    m_default_row_selected.Phone = text.Text;
                }
                else if (e.Column.Header.ToString() == "First Name")
                {
                    if (queryAttRec != null)
                    {
                        queryAttRec.FirstName = text.Text;

                    }

                    m_default_row_selected.FirstName = text.Text;
                    m_default_row_selected.FirstLastName = text.Text.ToUpper() + " " + m_default_row_selected.LastName.ToUpper();
                }
                else if (e.Column.Header.ToString() == "Last Name")
                {
                    if (queryAttRec != null)
                    {
                        queryAttRec.LastName = text.Text;
                    }

                    m_default_row_selected.LastName = text.Text;
                    m_default_row_selected.FirstLastName = m_default_row_selected.FirstName.ToUpper() + " " + text.Text.ToUpper();
                }
            }
            


        }

        private void cmbAttendanceInfo_Checked(object sender, RoutedEventArgs e)
        {
            m_attendance_row_selected.Attended = true;
            if (m_attendance_row_selected.IsNewrow)
            {
                m_attendance_row_selected.IsModifiedrow = false;
            }
            else
            {
                m_attendance_row_selected.IsModifiedrow = true;
            }
            
        }

        private void dataGrid_prospect_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedItems = sender as DataGrid;

            if (selectedItems.SelectedItem != null)
            {
                m_attendance_row_selected = (AttendanceTableRow)selectedItems.SelectedItem;

            }
        }

        private void cmbAttendanceInfo_Unchecked(object sender, RoutedEventArgs e)
        {
            m_attendance_row_selected.Attended = false;
            m_attendance_row_selected.IsModifiedrow = false;
            
            
        }

        private void btnAddActivity_Click(object sender, RoutedEventArgs e)
        {
           
            SetTimer();


            SaveActivePanelState();
            LoadActivityPanelState();



               

                btnNewRec.IsEnabled = false;


                btnImportRecords.IsEnabled = false;
                btnDelete.IsEnabled = false;


        
            chkActivityFilter.IsChecked = true;




            if (!m_IsActivityPanelView)
            {
                Show_Activity_Panel();
            }
            
        }
        private void Show_Activity_Panel()
        {

          
          
                spFilterOptions.Children.Clear();

                gbFilterOptions.Header = "Add Activity";



                if (DateStackPanel.Children.Contains(chkChurchDateFilter))
                    DateStackPanel.Children.Remove(chkChurchDateFilter);



                if (!DateStackPanel.Children.Contains(chkActivityDateFilter))
                    DateStackPanel.Children.Add(chkActivityDateFilter);




                btnPanelAddActivity.Visibility = Visibility.Visible;
                btnPanelAddActivity.IsEnabled = false;

                spFilterOptions.Children.Add(CalendarExpander);
                spFilterOptions.Children.Add(ActivityExpander);

                txtblkTaskDescription.Text = "";

                m_IsActivePanelView = false;
                m_IsActivityPanelView = true;
                m_IsPanelProspectView = false;

                Enable_Filters();
          


        }


        private void BtnAddActivity_Checked(object sender, RoutedEventArgs e)
        {


            
                SetTimer();

            //SaveActiveList current state of side panel


                SaveActivePanelState();



                // save current state of side panel


                m_alistView = false;
                m_AttendanceView = true;
                m_activityView = true;
                btnNewRec.IsEnabled = false;


                btnImportRecords.IsEnabled = false;
                btnDelete.IsEnabled = false;


            // load Activity state from TabState class
            LoadActivityPanelState();




            // Display_ActivityTable_in_Grid();
            if (!m_IsActivityPanelView)
            {
                Show_Activity_Panel();
            }
            

        }

        private void btnExpandHistory_Checked(object sender, RoutedEventArgs e)
        {
            if (dataGrid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed)
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
            
            }

           // btnAddActivity_Unchecked(sender, null);
            Disable_Filters();
        }

        private void btnExpandHistory_Unchecked(object sender, RoutedEventArgs e)
        {
            if (dataGrid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.VisibleWhenSelected)
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
                

            }

          //  btnAddActivity_Unchecked(sender, null);
            if (m_IsActivePanelView)
            {
                Enable_Filters();
            }
        }

        private void btnAddActivity_Unchecked(object sender, RoutedEventArgs e)
        {
           if (!m_IsActivePanelView)
            {
                Show_activeview_Panel();
            }
        }

        private void btnPanelAddActivity_Click(object sender, RoutedEventArgs e)
        {
            m_currentSelected_ActivityPair.Date = m_ActivityDateSelected;
            m_default_row_selected.ActivityList.Add(m_currentSelected_ActivityPair);

            //reflect activity last attended of attendee in default table row
            DefaultTableRow selectdefaultrow = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == m_default_row_selected.AttendeeId);
            //get last activity record
            ActivityPair lastActivityRec = selectdefaultrow.ActivityList.OrderByDescending(rec => rec.Date).ToList().FirstOrDefault();
            //display last activity in Default Table
            selectdefaultrow.Activity = lastActivityRec.ToString();
            selectdefaultrow.Activity_Last_Attended = lastActivityRec.DateString;

            ClearTreeView();
            //DateCalendar.DisplayDate
            //    DateCalendar.SelectedDates.Clear();
            btnPanelAddActivity.IsEnabled = false;
            m_currentSelected_ActivityPair = null;
            m_activitychecked_count = 0;

            Display_ActivityList_in_Grid();
            Display_DefaultTable_in_Grid();
        }
    }

}





