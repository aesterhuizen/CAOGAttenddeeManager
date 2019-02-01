using System;
using System.IO;

//using System.Runtime.InteropServices;
using System.Timers;
using System.Collections.Generic;

using System.Linq;
using System.Linq.Dynamic;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Data.Entity;
using System.Data.OleDb;
using System.Data;
using System.Text.RegularExpressions;

//using System.Windows.Forms;




namespace CAOGAttendeeManager
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


#if (init_db)

                    if (m_dbContext == null)
                    {
                        m_dbContext = new ModelDb(m_constr);
                        m_dbContext.Configuration.ProxyCreationEnabled = false;
                        //load db context
                        m_dbContext.Attendees.Load();
                        m_dbContext.Attendance_Info.Load();
                        m_dbContext.Activities.Load();
                    }

#if (DEBUG)
                    correctDBerrors();
#endif



                    InitDataSet();
#endif


                    



                    Display_DefaultTable_in_Grid();


                }
                else
                {

                    MessageBox.Show("Cannot connect to database, credential file does not exist!", "File does not exist.", MessageBoxButton.OK, MessageBoxImage.Error);
                    m_NoCredFile = true;
                    this.Close();
                }


               


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

        private DefaultTableRow m_default_row_selected;
        private AttendanceTableRow m_attendance_row_selected;

        // pointer to datagrid within RowDetailsTemplate
        private DataGrid m_AttendeeInfo_grid = null;
        private DataGrid m_Activity_grid = null;

        //List of query rows
        private List<DefaultTableRow> m_lstQueryTableRows = new List<DefaultTableRow>() { };
        private List<DefaultTableRow> m_lstdefaultTableRowsCopy;
        //List of default Table rows
        private List<DefaultTableRow> m_lstdefaultTableRows = new List<DefaultTableRow>() { };

        private List<AttendanceTableRow> m_lstattendanceTableRows = new List<AttendanceTableRow>() { };
        //list of Activities
        private List<ActivityGroup> m_lstActivities = new List<ActivityGroup> { };
        
        
        private List<ActivityTask> m_lstActivityTasks = new List<ActivityTask> { };

        private TabState m_TabState = new TabState();
        //Activity control
        private string m_ActivityName = "";
        
    
       
        private int m_child_taskId = 0;
        int m_parent_taskId = 0;
        private int m_lstActivitiesCount = 0;
        private int m_newlstActivitiesCount = 0;
        // the current selected activity Pair
        private ActivityPair m_currentSelected_ActivityPair = null;
        private ActivityTask m_currentSelect_ActivityTask = null;

        private ActivityPair m_previousSelected_ActivityPair = null;
        private Timer aTimer = null;

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
        private bool m_LoadFromActiveState = false;
        private string[] m_ary_ActivityStatus = new string[10];
        private string m_old_attendeeId = "";
        private bool m_dateIsValid = false;
        private bool m_alistdateIsValid = false;




        private string m_constr = "";


        private int m_NewAttendeeId = 0;

        private void Set_btnAddActivityState()
        {
            if (m_activitychecked_count == 1 && (m_ActivityDateSelected != null && m_dateIsValid))
            {
                btnPanelAddActivity.IsEnabled = true;
            }
            else
            {
                btnPanelAddActivity.IsEnabled = false;
            }
        }


        private void StopTimer()
        {
            if (aTimer != null)
            {
                aTimer.Enabled = false;
            }

        }
        private void SetTimer()
        {
            aTimer = new Timer(100);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {

            Dispatcher.Invoke(() =>
           {
               if (m_currentSelected_ActivityPair !=null && m_currentSelect_ActivityTask != null)
               {
                   if (m_IsActivityPanelView &&
                    m_activitychecked_count == 1 &&
                    m_ActivityDateSelected != null &&

                    /*parent tasks with child tasks cannot be added to the activity list*/
                    m_currentSelect_ActivityTask.lstsubTasks.Count == 0
                   )

                   {
                       btnPanelAddActivity.IsEnabled = true;
                   }
                   else
                   {
                       btnPanelAddActivity.IsEnabled = false;
                   }
               }
               


           });

            Dispatcher.Invoke(() =>
            {

                var selected_group = trvActivities.SelectedItem;

                if (m_currentSelected_ActivityPair != null && m_currentSelect_ActivityTask != null)
                {
                    if (selected_group != null ||
                        (m_currentSelected_ActivityPair != null &&
                         (m_currentSelect_ActivityTask.lstsubTasks.Count == 0 ||
                         m_currentSelect_ActivityTask.lstsubTasks.Count != 0)))
                    {
                        btnPanelNewActivity.IsEnabled = true;
                    }
                    else
                    {
                        btnPanelNewActivity.IsEnabled = false;
                    }

                }
            });




        }

       

        private void correctDBerrors()
        {

            //m_dbContext.Attendance_Info.Load();
            //m_dbContext.Attendees.Load();

             DateTime find_date = new DateTime(2019, 1, 6);
            List<Attendance_Info> lstAtt = new List<Attendance_Info>() { };

            foreach (var attendee in m_dbContext.Attendees)
            {
                var querylatestDate = (from rec in attendee.AttendanceList
                                       where rec.Date == find_date
                                       select rec).ToList().LastOrDefault();

                if (querylatestDate != null)
                {
                    lstAtt.Add(querylatestDate);
                }
                
            }

            m_dbContext.Attendance_Info.RemoveRange(lstAtt);



            m_dbContext.SaveChanges();


        }

        private void InitActivityTreeView()
        {

            Load_ChurchActivities_From_XMLFile();

            

            trvActivities.ItemsSource = m_lstActivities;



        }

        private void Save_ChurchActivities_To_XMLFile()
        {
            
            List<XNode> lstdocNodes = new List<XNode>() { };
            var doc_root = new XElement("XmlDocument");

            int intId = 0;
            Cursor = Cursors.Wait;

            foreach (ActivityGroup Agroup in m_lstActivities)
            {
                XElement ActivityGroupElement = new XElement("ActivityGroup", new XAttribute("ActivityName", Agroup.ActivityName));

                //ActivityTask Element
                foreach (ActivityTask task in Agroup.lstActivityTasks)
                {
                    List<XAttribute> lstAttributes = new List<XAttribute>();

                    XAttribute attId = new XAttribute("Id", intId++);
                    lstAttributes.Add(attId);
                    XAttribute attTaskName = new XAttribute("TaskName", task.TaskName);
                    lstAttributes.Add(attTaskName);
                    if (task.Description != null)
                    {
                        XAttribute attDescription = new XAttribute("Description", task.Description);
                        lstAttributes.Add(attDescription);

                    }
                    XElement ActivityTaskElement = new XElement("ActivityTask", lstAttributes);

                    
                    //task has subtasks
                    if (task.lstsubTasks.Count != 0)
                    {

                        // ActivitySubTask Element
                        foreach (ActivityTask subTask in task.lstsubTasks)
                        {
                            List<XAttribute> lstsubAttributes = new List<XAttribute>();

                            XAttribute subattId = new XAttribute("Id", intId++);
                            lstsubAttributes.Add(subattId);
                            XAttribute subattTaskName = new XAttribute("TaskName", subTask.TaskName);
                            lstsubAttributes.Add(subattTaskName);
                            if (subTask.Description != null)
                            {
                                XAttribute subattDescription = new XAttribute("Description", subTask.Description);
                                lstsubAttributes.Add(subattDescription);
                            }
                            XElement ActivitySubTaskElement = new XElement("ActivitySubTask", lstsubAttributes);
                            //add sub element to the ActivityTask elelemnt
                            ActivityTaskElement.Add(ActivitySubTaskElement);
                        }
                        // add activityTask element to ActivityGroup element
                        ActivityGroupElement.Add(ActivityTaskElement);
                    }
                    else
                    {
                        //ActivityTask has no subtasks so add ActivityTask to ActivityGroup
                        ActivityGroupElement.Add(ActivityTaskElement);
                    }

                }

                // add ActivityGroup elements to doc_root Element of the DOM document
                lstdocNodes.Add(ActivityGroupElement);

            }
            //add root node list of element nodes
            doc_root.Add(lstdocNodes);
            // Create DOM with lst of nodes 'lstdocNodes'
            XDocument DOMdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), doc_root);
            var executingPath = Directory.GetCurrentDirectory();

            try
            {


                if (File.Exists($"{executingPath}\\ChurchActivities.xml"))
                {

                    var fsXML = new FileStream($"{executingPath}\\ChurchActivities.xml", FileMode.Create, FileAccess.ReadWrite);

                    DOMdoc.Save(fsXML);
                }

            }
            catch (Exception ex)
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("No Activities file found!");
            }

            Cursor = Cursors.Arrow;

        }

        private void Load_ChurchActivities_From_XMLFile()
        {

            XmlReaderSettings reader_settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true
            };

            try
            {

                var executingPath = Directory.GetCurrentDirectory();
                if (File.Exists($"{executingPath}\\ChurchActivities.xml"))
                {

                    using (XmlReader xreader = XmlReader.Create("ChurchActivities.xml", reader_settings))
                    {
                        xreader.ReadStartElement("XmlDocument");
                        while (xreader.Name == "ActivityGroup")
                        {
                            XElement ActivityGroupElement = (XElement)XNode.ReadFrom(xreader);

                            string xmlAttName = (string)ActivityGroupElement.Attribute("ActivityName");
                            ActivityGroup trv_activityGroup = new ActivityGroup { Parent = "", ActivityName = xmlAttName };
                            m_lstActivitiesCount++; // increments activity list count to later compare if the list has changed

                            foreach (XElement ActivityTaskElement in ActivityGroupElement.Elements())
                            {
                                int id = (int)ActivityTaskElement.Attribute("Id");
                                string name = (string)ActivityTaskElement.Attribute("TaskName");
                                string description = (string)ActivityTaskElement.Attribute("Description");

                                ActivityTask trv_activityTask = new ActivityTask { Parent = "", ActivityId = id, TaskName = name, Description = description };
                                m_lstActivitiesCount++;

                                if (ActivityTaskElement.HasElements)
                                {
                                    foreach (XElement subActivity in ActivityTaskElement.Elements())
                                    {
                                        int subtaskId = (int)subActivity.Attribute("Id");
                                        string subtaskName = (string)subActivity.Attribute("TaskName");
                                        string subtaskdescription = (string)subActivity.Attribute("Description");

                                        ActivityTask trv_activitysubTask = new ActivityTask { Parent = "", ActivityId = subtaskId, TaskName = subtaskName, Description = subtaskdescription };
                                        //add subtask to lstActivityTask
                                        trv_activityTask.lstsubTasks.Add(trv_activitysubTask);
                                        m_lstActivityTasks.Add(trv_activitysubTask);
                                        m_lstActivitiesCount++; // increments activity list count to later compare if the list has changed

                                    }

                                }

                                //add activity tasks to activity group
                                trv_activityGroup.lstActivityTasks.Add(trv_activityTask);
                                m_lstActivityTasks.Add(trv_activityTask);

                            }

                            //add groups to lstActivities list

                            m_lstActivities.Add(trv_activityGroup);



                        }
                        xreader.ReadEndElement();
                    }
                }
                else // activities file does not exist
                {
                    Cursor = Cursors.Arrow;
                    MessageBox.Show("No Activities file found! 'ChurchActivities.xml'");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }
           
           
          
          


        }
        private bool GenerateDBFollowUps()
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
            bool generate_one = false;
            TimeSpan timespanSinceDate = new TimeSpan();
            List<Attendance_Info> lstAttendanceInfo = new List<Attendance_Info>() { };

            DateTime greatest_date = new DateTime(2000,1,1);
         
            //get latest and greatest attended date 
            for (int i=0; i < m_lstdefaultTableRows.Count; i++)
            {
                var latest_date_attened = (from d in m_lstdefaultTableRows[i].AttendanceList
                                           orderby d.Date ascending
                                           select d).ToArray().LastOrDefault();
                

                if (i == 0 )
                {
                    greatest_date = latest_date_attened.Date;
                }

                if (latest_date_attened.Date > greatest_date)
                {
                    greatest_date = latest_date_attened.Date;
                   
                }
              

               
            }

            //foreach (var AttendeeRec in m_dbContext.Attendees.Local)
            //{


            //var lstDateRecs = (from DateRec in AttendeeRec.AttendanceList
            //                   orderby DateRec.Date ascending
            //                   select DateRec).ToArray().LastOrDefault();

            for (int i = 0; i < m_lstdefaultTableRows.Count -1; i++)
            {
                var lstDateRec = (from rec in m_lstdefaultTableRows[i].AttendanceList
                                   orderby rec
                                   select rec).ToList().LastOrDefault();


                if (lstDateRec != null)
                {
                    timespanSinceDate = greatest_date - lstDateRec.Date;



                    if (timespanSinceDate.Days < 21)
                    {
                      //  Console.WriteLine($"No Follow-Ups to generate for {lstDateRec.Attendee.LastName} {lstDateRec.Attendee.FirstName} AttendeeId={m_lstdefaultTableRows[i].AttendeeId}");
                        // do nothing
                        //Attendee already have a followUp sent so do not generate another followup unil 21 days has
                        //lapsed since the last followUp        


                    }
                    else
                    {
                        //generate follow-up if attendee does not have 3 consecutive followups already
                        //search for sunday
                       // Console.WriteLine($"Follow-Up written for: {lstDateRec.Attendee.LastName} {lstDateRec.Attendee.FirstName} AttendeeId={m_lstdefaultTableRows[i].AttendeeId}");

                        //for (DateTime date = lstDateRec.Date; date <= greatest_date; date = date.AddDays(1))
                        //{
                        //    if (date.DayOfWeek == DayOfWeek.Sunday)
                        //    {
                        //        lstsundays.Add(date);
                        //    }
                        //}

                        //DateTime lastSunday = lstsundays.LastOrDefault();
                        //lstsundays.Clear();
                        //if (lastSunday != null)
                        //{
                            Attendance_Info newfollowUpRecord = new Attendance_Info { };
                            newfollowUpRecord.AttendeeId = m_lstdefaultTableRows[i].AttendeeId;
                            newfollowUpRecord.Date = greatest_date;
                            newfollowUpRecord.Status = "Follow-Up";

                            lstAttendanceInfo.Add(newfollowUpRecord);


                            generate_one = true;

                        
                    }





                } //end if
            }

            m_dbContext.Attendance_Info.AddRange(lstAttendanceInfo);
         


            if (generate_one)
            {
                InitDataSet();
                Display_DefaultTable_in_Grid();

            }


            return generate_one;


        }


        private void Display_AttendeeListTable_in_Grid()
        {

          
            
            dataGrid_prospect.DataContext = m_lstattendanceTableRows.OrderBy(rec => rec.LastName).ToList();
            dataGrid_prospect.Items.Refresh();
            lblAttendenceMetrics.Text = dataGrid_prospect.Items.Count.ToString();





        }





        private void Display_DefaultTable_in_Grid()
        {

           
            dataGrid.DataContext = m_lstdefaultTableRows.OrderBy(rec => rec.LastName).ToList(); 
           dataGrid.Items.Refresh();
            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            m_isQueryTableShown = false;

            btnDelete.IsEnabled = true;
            btnGenerateFollowUps.IsEnabled = true;
            btnExecQuery.IsEnabled = true;

            dataGrid.IsReadOnly = false;
            txtSearch.IsEnabled = true;


        } 

     

        private void InitDataSet()
        {

          






            if (m_lstdefaultTableRows.Count > 0)
            {
                m_lstdefaultTableRows.Clear();

            }
            if (m_lstattendanceTableRows.Count > 0)
            {
                m_lstattendanceTableRows.Clear();
            }
            string date = "Date Not Valid";

          

            try
            {


                string ldate = "";
                string lstatus = "";
                string adate = "";

                int i = 0;

              
                    foreach (var AttendeeRec in m_dbContext.Attendees.Local )
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

                        if (DefaultTabledr.AttendeeId != 0)
                            m_lstdefaultTableRows.Add(DefaultTabledr);

#if (DEBUG)
                    //i++;
                    //if (i == 10)
                    //    break;
#endif



                }
                // make a copy of m_lstdefaultTableRows

                //DefaultTableRow[] array_defaultTableRows = new DefaultTableRow[m_lstdefaultTableRows.Count];
                //m_lstdefaultTableRows.CopyTo(array_defaultTableRows);

                //m_lstdefaultTableRowsCopy = new List<DefaultTableRow>(array_defaultTableRows);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred when performing database operation: {ex}");
            }




        }




        private void chkResponded_Checked(object sender, RoutedEventArgs e)
        {

           
            chkAttended.IsChecked = false;
            chkFollowup.IsChecked = false;

            m_isRespondedChecked = true;
      
           

           

        }

        private void chkFollowup_Checked(object sender, RoutedEventArgs e)
        {

          

            chkAttended.IsChecked = false;
            chkResponded.IsChecked = false;
            m_isFollowupChecked = true;
          

          
          
        }


        private void chkAttended_Checked(object sender, RoutedEventArgs e)
        {
         


         
            chkResponded.IsChecked = false;
            chkFollowup.IsChecked = false;

            m_isAttendedChecked = true;
        

           
         
        }





        private void Disable_Filters()
        {
            CalendarExpander.IsEnabled = false;
            ChurchStatusExpander.IsEnabled = false;
            ActivityExpander.IsEnabled = false;
          
         



        }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if in followUp view, use query database else if in model list view filter table



            if (txtSearch.Text == "")
            {
                Enable_Filters();
                

                if (m_isFilterByDateChecked || m_isActivityfilterByDateChecked)
                    DateCalendar.IsEnabled = true;
                else
                    DateCalendar.IsEnabled = false;

                if (m_AttendanceView)
                {
                    if (m_isQueryTableShown)
                    {
                        dataGrid.DataContext = m_lstQueryTableRows;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
                    }
                    else
                    {
                        dataGrid.DataContext = m_lstdefaultTableRows;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
                    }
               

                }
                else if (m_alistView)
                {
                    Display_AttendeeListTable_in_Grid();

                }
             


                //----------------------Textbox search has text-----------------------------------------------------------------------------------
            }
            else
            {
           
                Disable_Filters();
                DateCalendar.IsEnabled = false;
               

                string text = txtSearch.Text.ToUpper();

                if (m_AttendanceView)
                {
                    if (m_isQueryTableShown )
                    { 
                        var filteredQueryTable = m_lstQueryTableRows.Where(row => row.FirstLastName.Contains(text));
                        dataGrid.DataContext = filteredQueryTable;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
                    }
                    else
                    {
                        var filteredDefaultTable = m_lstdefaultTableRows.Where(row => row.FirstLastName.Contains(text));
                        dataGrid.DataContext = filteredDefaultTable;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
                    }

                }
                else if (m_alistView)
                {
                    var filteredAttendeeListTable = m_lstattendanceTableRows.Where(row => row.FirstLastName.Contains(text));
                    dataGrid_prospect.DataContext = filteredAttendeeListTable;
                   lblAttendenceMetrics.Text = dataGrid_prospect.Items.Count.ToString();
                }
              



            }


        }

        private void chkAttended_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isAttendedChecked = false;
         

          
        }

        private void chkFollowup_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isFollowupChecked = false;
          

        }

        private void chkResponded_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isRespondedChecked = false;
       

          
            
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

                    int ret_error = check_date_bounds();

                    if (ret_error == 1)
                        return;


                    string date = m_alistDateSelected.ToString("MM-dd-yyyy");

                    if (datec.DayOfWeek == DayOfWeek.Sunday)
                    {
                        m_alistdateIsValid = true;

                        UpdateAttendeeListTableWithDateFilter();
                   

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


                    // BuildQuery_and_UpdateGrid();

                }
                // ActivityView-------------------------------------------------------------------------------------------------------------------
                else if (m_IsActivityPanelView && m_isActivityfilterByDateChecked)
                {
                    m_ActivityDateSelected = datec;

                    int ret_error = check_date_bounds();

                    if (ret_error == 1)
                        return;
                    else
                        m_dateIsValid = true;
                   

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
                Cursor = Cursors.Wait;
                SaveActiveList();
                Cursor = Cursors.Arrow;
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

                var Attrec = m_dbContext.Attendees.SingleOrDefault(id => id.AttendeeId == attid);
               


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
          
            // copy the list with the deleted rows to the main list that display the default attendance table
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
         
            
            var grid = sender as DataGrid;

          
            if (grid.SelectedItem != null)
            {
                m_default_row_selected = (DefaultTableRow)grid.SelectedItem;
               
            }

    
            if (!txtSearch.IsEnabled)
                txtSearch.IsEnabled = true;

            if (!btnDelete.IsEnabled && dataGrid.RowDetailsVisibilityMode != DataGridRowDetailsVisibilityMode.Visible)
                btnDelete.IsEnabled = true;

            if (!m_IsActivePanelView)
            {
                LoadActivePanelState();
                Show_activeview_Panel();
                StopTimer();
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
                if ( (m_ActivityDateSelected > datelimit) || (m_alistDateSelected > datelimit) )
                {
                    m_dateIsValid = false;
                    MessageBox.Show($"Date limit is {datelimit.ToShortDateString()}.", "Invalid date", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return 1;
                }
            }

            return 0;

        }

        private bool Check_for_dup_AttendeeInfo_inDbase(AttendanceTableRow atr)
        {


            var defaultTableRec = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == atr.AttendeeId);
            if (defaultTableRec != null)
            {
                var lastAttInfoRec = defaultTableRec.AttendanceList.SingleOrDefault(rec => rec.Date == m_alistDateSelected && rec.Status == "Attended");

                if (lastAttInfoRec != null)
                {
                    dataGrid_prospect.Focus();
                    int aid = atr.AttendeeId;
                    int gridrowIdx = 0;
                    foreach (AttendanceTableRow gridrow in dataGrid_prospect.Items)
                    {

                        if (gridrow.AttendeeId == aid)
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
            }
                
          


            return false;

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

            List<Attendee> attendeeList = new List<Attendee>() { };
            List<Attendance_Info> attendanceList = new List<Attendance_Info>() { };

            // first pass through list and make sure everything looks good before making any changes to the db context

            if (m_alistdateIsValid)
            {

                //end all edits and update the datagrid with changes
                dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid_prospect.UpdateLayout();


                // m_DataSet.Tables["AttendeeListTable"].DefaultView.RowFilter = "";
                foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
                {
                    if (dr.IsModifiedrow)
                    {
                        int attid = dr.AttendeeId;

                       
                        //check for duplicate attendance record
                        bool bcheckdupInfo = Check_for_dup_AttendeeInfo_inDbase(dr);
                        if (bcheckdupInfo)
                        {
                            MessageBox.Show("A record with the same first name, last name and date already exist in the database, choose a difference date, first name or last name.", "Duplicate record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                            return;
                        }
                        else
                        {
                            // attended = true, add attendee info record to attendee attendance list

                            bool berror = Row_error_checking(dr);
                            if (berror)
                                return;


                            Attendance_Info newRecord = new Attendance_Info { };

                           

                            newRecord.AttendeeId = attid;
                            newRecord.Date = m_alistDateSelected;

                            //var AttendeeIdisInLocalContext = m_dbContext.Attendees.Local.SingleOrDefault(rec => rec.AttendeeId == attid);


                            var defaultTableRec = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == attid);
                            if (defaultTableRec == null)
                            {
                                Console.WriteLine("defaultTableRec = NULL");
                            }
                            if (defaultTableRec !=null)
                            {
                                var lastAttInfoRec = defaultTableRec.AttendanceList.SingleOrDefault(rec => rec.Date == m_alistDateSelected);

                                string flname = defaultTableRec.FirstName + " " + defaultTableRec.LastName;

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


                                // Update Default


                                // Add new Record to AttendanceList of attendee
                                // This will automatically update the m_dbcontext.Attendance_Info structure!
                                if (defaultTableRec != null)
                                {
                                    //adding to the AttendanceList will automatically update the local db context with the new attendance_Info structure object

                                    m_dbContext.Attendance_Info.Add(newRecord);
                                    //attendanceList.Add(newRecord);
                                    defaultTableRec.AttendanceList.Add(newRecord);

                                    //change 'Status_Last_Attended' and 'date last attended' column in default table row to reflect the 
                                    //new record's status
                                    defaultTableRec.ChurchStatus = newRecord.Status;
                                    defaultTableRec.Church_Last_Attended = newRecord.Date.ToString("MM-dd-yyyy");
                                    haschanges = true;

                                }
                            }
                           

                           
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
                            var isAttID_present = m_dbContext.Attendees.AsNoTracking().SingleOrDefault(rec => rec.AttendeeId == m_NewAttendeeId);
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
                        newAttendeeRec.Phone = "";
                        newAttendeeRec.Email = "";
                        
                        string flname = newAttendeeRec.FirstName.ToUpper() + " " + newAttendeeRec.LastName.ToUpper();
                        string phone = "";
                        string email = "";

                        //new Attendee Info record
                        newAttInfoRec.AttendeeId = newAttendeeRec.AttendeeId; // m_NewAttendeeId;
                        newAttInfoRec.Date = m_alistDateSelected;
                        newAttInfoRec.Status = "Attended";
                        //newAttInfoRec.Attendee = newAttendeeRec;
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

                        // add new attendee info to db context
                        //m_dbContext.Attendance_Info.Add(newAttInfoRec);
                        attendanceList.Add(newAttInfoRec);
                        //m_dbContext.Attendees.Add(newAttendeeRec);
                        attendeeList.Add(newAttendeeRec);
                        haschanges = true;
                    }

                }

                if (haschanges)
                {
                    if (attendeeList.Any())
                    {
                        m_dbContext.Attendees.AddRange(attendeeList);
                    }
                    if (attendanceList.Any())
                    {

                        m_dbContext.Attendance_Info.AddRange(attendanceList);
                    }
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
            //var queryAtt = m_dbContext.Attendance_Info.AsNoTracking().SingleOrDefault(
            //                                                   AttInfoRec => AttInfoRec.Attendee.FirstName.ToUpper() == atr.FirstName.ToUpper() &&
            //                                                   AttInfoRec.Attendee.LastName.ToUpper() == atr.LastName.ToUpper() );


            var queryAtt = m_lstdefaultTableRows.SingleOrDefault(rec => rec.FirstName.ToUpper() == atr.FirstName.ToUpper()
                                                                 && rec.LastName.ToUpper() == atr.LastName.ToUpper());

            if (queryAtt != null)
            {

                dataGrid_prospect.Focus();
                int id = atr.AttendeeId;
                int gridrowIdx = 0;
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

       


        }




        private void chkChurchDateFiler_Unchecked(object sender, RoutedEventArgs e)
        {



            m_isFilterByDateChecked = false;
         
            if (!m_isActivityfilterByDateChecked)
                DateCalendar.IsEnabled = false;

            if (m_alistView)
                return;

           

        }




        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            m_alistView = false;
            m_isActivityfilterByDateChecked = false;
            m_AttendanceView = true;

            m_IsActivePanelView = true;
            m_IsActivityPanelView = false;
            m_IsPanelProspectView = false;

          


          


            btnNewRec.IsEnabled = false;
            btnImportRecords.IsEnabled = false;
            btnGenerateFollowUps.IsEnabled = true;
            btnFilterOpts.IsChecked = true;
            btnAddActivity.IsChecked = false;

            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;

            m_lstActivitiesCount = m_lstActivities.Count();
            m_newlstActivitiesCount = m_lstActivitiesCount;
            Uncheck_All_Filters();
            SetTimer();



            txtSearch.Text = "";
            m_dateIsValid = false;

            // commit datagrid edits and return DataContext to show all records
            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            if (btnFilterOpts.IsChecked.GetValueOrDefault())
            {
                btnPanelNewActivity.Visibility = Visibility.Hidden;
                btnPanelAddActivity.Visibility = Visibility.Hidden;
            }




            if (dataGrid.Columns.Count > 1)
            {
                dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
                dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName

            }


            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            lblTableShown.Content = "Main View";

        }

        private void columnHeaderClick(object sender, RoutedEventArgs e)
        {
            var columnHeader = sender as DataGridColumnHeader;

           



            if (columnHeader != null)
            {

              
                if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    dataGrid.SelectedCells.Clear();
                }

                foreach (var item in dataGrid.Items)
                {
                    dataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
                }
            }

        }

        private void CopyDataGridtoClipboard(object sender, DataGridRowClipboardEventArgs e)
        {
            var grid = sender as DataGrid;
            // e.ClipboardRowContent.Clear();
            //e.ClipboardRowContent.Add(new DataGridClipboardCellContent(e.Item, grid.Columns[grid.CurrentColumn.DisplayIndex -1], ((DefaultTableRow)grid.CurrentItem).ToString()));
            //if (e.ClipboardRowContent.Count > 3)
            //{
            //    e.ClipboardRowContent.RemoveAt(4);
            //}

        }

        private void btnChart_Click(object sender, RoutedEventArgs e)
        {
            ChartWindow wndCharts = new ChartWindow(ref m_dbContext);
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



        private void btnNewRec_Click(object sender, RoutedEventArgs e)
        {




            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            dataGrid_prospect.UpdateLayout();

       

            string strdate;

            if (m_alistdateIsValid)
                strdate = m_alistDateSelected.ToString("MM-dd-yyyy");
            else
                strdate = "Date Not Valid";


         
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

                if (m_lstActivitiesCount != m_newlstActivitiesCount)
                {
                    Save_ChurchActivities_To_XMLFile();
                   
                }
#if (init_db)
                if (!m_dbContext.ChangeTracker.HasChanges() && isAttendedStatusChecked)
                {



                    MessageBoxResult res = MessageBox.Show("There are checked attendees in the attendee checklist that has not yet been added to the active attendance list.\n\nDiscard changes and exit anyway?", "Attendees not added yet", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    if (res == MessageBoxResult.OK)
                    {

                        //Discard_CheckListandSaveActiveList();
                        e.Cancel = false;
                        StopTimer();
                        // close all active threads
                        Environment.Exit(0);

                    }
                    else
                    {
                        e.Cancel = true;
                       

                    }
                        







                }
                else if (m_dbContext.ChangeTracker.HasChanges() && !isAttendedStatusChecked)
                {

                    MessageBoxResult res = MessageBox.Show("Changes has been made but not saved to the database yet, save changes?", "Changes not saved", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    
                    if (res == MessageBoxResult.Yes)
                    {

                        Cursor = Cursors.Wait;
                        SaveActiveList();
                        Cursor = Cursors.Arrow;

                        e.Cancel = false;

                        // close all active threads
                        Environment.Exit(0);
                    }
                    else if (res == MessageBoxResult.No)
                    {
                        e.Cancel = false;
                        StopTimer();
                        // close all active threads
                        Environment.Exit(0);

                    }
                    else if (res == MessageBoxResult.Cancel)
                        e.Cancel = true;

                  

                }
                else if (m_dbContext.ChangeTracker.HasChanges() && isAttendedStatusChecked)
                {



                    MessageBoxResult res = MessageBox.Show("Changes has been made but not saved to the database yet.\n\nThere are checked attendees in the attendee checklist that has not yet been added to the active attendance list.\n\nDiscard checklist changes and save active attendance changes to database?", "Save and discard checklist", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    if (res == MessageBoxResult.Yes)
                    {
                        Cursor = Cursors.Wait;
                        SaveActiveList();
                        StopTimer();
                        Cursor = Cursors.Arrow;
                        e.Cancel = false;
                        // close all active threads
                        Environment.Exit(0);

                    }
                    else if (res == MessageBoxResult.No)
                    {
                        e.Cancel = false;
                        StopTimer();
                        // close all active threads
                        Environment.Exit(0);

                    }
                    else if (res == MessageBoxResult.Cancel)
                        e.Cancel = true;







                }
                else
                {
                    Cursor = Cursors.Wait;

                    Environment.Exit(0);

                    Cursor = Cursors.Arrow;

                }
#endif

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

          


            if (m_dbContext.ChangeTracker.HasChanges())
            {
             
                // save contents to database
                m_dbContext.SaveChanges();

                              
              
            }
            else
            {
                MessageBox.Show("No changes to save.");
            }



        }

        private void btnGenerateFollowUps_Click(object sender, RoutedEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("Are you sure you want to generate follow-Ups now?", "Generate Follow-Up", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Cursor = Cursors.Wait;

                bool generate_one = GenerateDBFollowUps();

                Cursor = Cursors.Arrow;
                if (generate_one)
                {
                    MessageBox.Show("Successfully generated follow-ups!");
                }
                else
                {
                    MessageBox.Show("No follow-ups to generate, no follow-ups generated.", "Generate follow-ups", MessageBoxButton.OK, MessageBoxImage.Information);

                }

            }

            

        }


      

        private void chkActivityFilter_Checked(object sender, RoutedEventArgs e)
        {
            m_isActivityFilterChecked = true;
            trvActivities.IsEnabled = true;
            

        }

        private void chkActivityFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            m_isActivityFilterChecked = false;
         

            Cursor = Cursors.Wait;

            ClearTreeView();

           

            trvActivities.IsEnabled = false;
           
            Cursor = Cursors.Arrow;
        }


        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            //var selectedcells = dataGrid.SelectedCells;

           
            //DataGridRowClipboardEventArgs dataRowcellContent = new DataGridRowClipboardEventArgs(dataGrid, 2, 5, true);

            //CopyDataGridtoClipboard(dataGrid, dataRowcellContent);

            //DataGridCellClipboardEventArgs cells_to_copy = new DataGridCellClipboardEventArgs(dataGrid, dataGrid.CurrentColumn, dataGrid.CurrentItem);
            
            //DataGridClipboardCellContent cellContent = new DataGridClipboardCellContent(dataGrid.CurrentItem, dataGrid.CurrentColumn, dataGrid.CurrentItem.ToString());

            // dataRowcellContent.ClipboardRowContent.Add(cellContent);
           
            //if (e.ClipboardRowContent.Count > 3)
            //{
            //    e.ClipboardRowContent.RemoveAt(4);
            //}
            // DataGridCellClipboardEventArgs clip = new DataGridCellClipboardEventArgs()
            // CopyDataGridtoClipboard(dataGrid, );
        }

        private void ActivityTreeView_Checkbox_Checked(object sender, RoutedEventArgs e)
        {


            m_isActivityChecked = true;
            var checkbox = sender as CheckBox;
            m_ActivityName = checkbox.Content.ToString();



            //if (m_AttendanceView && m_IsActivePanelView)
            //{
            Do_treeview_ActiveView();
             //}
                //else if (m_IsActivityPanelView && m_AttendanceView)
                //{
                //    Do_treeview_ActivityView();
                //}

        


        }

        private void Do_treeview_ActivityView()
        {

            m_child_taskId = 0;
            m_parent_taskId = 0;


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
                                    AttendeeId = m_default_row_selected.AttendeeId,
                                    ParentTaskName = task.TaskName,
                                    ChildTaskName = subtask.TaskName,
                                    
                                };

                                m_child_taskId = subtask.ActivityId;
                                m_parent_taskId = task.ActivityId;

                                m_currentSelected_ActivityPair = selectedActivity;
                              


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
                                AttendeeId = m_default_row_selected.AttendeeId,
                                ParentTaskName = task.TaskName,
                                ChildTaskName = "",
                                                       
                            };
                            
                            m_parent_taskId = task.ActivityId;

                            m_currentSelected_ActivityPair = selectedActivity;
                            

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
            
                foreach (ActivityTask task in m_lstActivityTasks)
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

            m_child_taskId = 0;
            m_parent_taskId = 0;

           
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
                                        AttendeeId = 0,
                                        ParentTaskName = task.TaskName,
                                        ChildTaskName = subtask.TaskName,
                                         
                                        
                                    };

                                    m_child_taskId = subtask.ActivityId;
                                    m_parent_taskId = task.ActivityId;

                                    m_currentSelected_ActivityPair = selectedActivity;
                                    m_currentSelect_ActivityTask = subtask;


                                    if (m_activitychecked_count == 1)
                                    {
                                        m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;
                                    }

                                    txtblkTaskDescription.Text = subtask.Description;
                                    task.IsSelected = true; //check parent

                                    break;
                                }
                                else if  ((m_ActivityName == task.TaskName) && task.IsSelected) // user selected a task with subtasks underneath it
                                {

                                    if (m_currentSelected_ActivityPair != null)
                                         return; //prevent routine from executing twice

                                    m_activitychecked_count++;
                                    ActivityPair selectedActivity = new ActivityPair
                                    {
                                        ActivityGroup = activity_group.ActivityName,
                                        AttendeeId = 0,
                                        ParentTaskName = task.TaskName,
                                        ChildTaskName = "",


                                    };
                                    m_parent_taskId = task.ActivityId;

                                    m_currentSelected_ActivityPair = selectedActivity;
                                    m_currentSelect_ActivityTask = task;

                                    if (m_activitychecked_count == 1)
                                    {
                                        m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;

                                    }

                                    txtblkTaskDescription.Text = task.Description;
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
                                    AttendeeId = 0,
                                    ParentTaskName = task.TaskName,
                                    ChildTaskName = "",
                                   
                                   
                                };
                                m_parent_taskId = task.ActivityId;

                                m_currentSelected_ActivityPair = selectedActivity;
                                m_currentSelect_ActivityTask = task;

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
                ClearTreeViewCheckboxes_except_clickedOne(m_child_taskId, m_parent_taskId);
                m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;


           
            
        }
     
        private void BuildQuery_and_UpdateGrid()
        {





            IQueryable<DefaultTableRow> querylinq = null;
           
            
           
            string strChurchStatus = "";


            


            

            bool ChurchDate = m_dateIsValid && m_DateSelected !=null && m_isFilterByDateChecked;
            bool Status = m_isChurchStatusFilterChecked && m_DateSelected != null && (m_isFollowupChecked || m_isAttendedChecked || m_isRespondedChecked);
            bool Activity = m_isActivityFilterChecked && m_isActivityChecked && m_currentSelected_ActivityPair != null;
            bool ActivityDate = m_isActivityfilterByDateChecked && m_ActivityDateSelected != null;

            // display main View when no filter options are checked
            if (!ChurchDate && !Status && !Activity && !ActivityDate)
            {
                Display_DefaultTable_in_Grid();
                m_isQueryTableShown = false;
                btnDelete.IsEnabled = true;
                btnGenerateFollowUps.IsEnabled = true;
                lblTableShown.Content = "Main View";
                
                return;
            }

            if (ChurchDate || Status || Activity || ActivityDate)
            {
                DefaultTableRow[] array_defaultTableRows = new DefaultTableRow[m_lstdefaultTableRows.Count];
                m_lstdefaultTableRows.CopyTo(array_defaultTableRows);
                m_lstdefaultTableRowsCopy = new List<DefaultTableRow>(array_defaultTableRows);
            }

            if (m_lstQueryTableRows.Count > 0)
                m_lstQueryTableRows.Clear();

            if (ActivityDate && Status && Activity) //Activity date, Status, Activity
            {

                string strActivityDate = m_ActivityDateSelected?.ToString("MM-dd-yyyy");
                string strActivity = m_currentSelected_ActivityPair.ToString();
                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;

                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";

                

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {
                    

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Count > 0 && m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {
                        var query_status = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus).ToList().FirstOrDefault();
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0 and ActivityGroup == @1 and ParentTaskName == @2 and ChildTaskName == @3", strActivityDate, ActivityGroup, ActivityParentName, ActivityChildName).ToList().FirstOrDefault();

                        if (query_status != null && query_activity != null)
                        {
                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }
                        

                    


                    
                }

            }
            //Date , church status, activity
            else if (ChurchDate && Status && Activity)
            {
                string strChurchDate = m_DateSelected.ToString("MM-dd-yyyy");

                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";

                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Count > 0 && m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {
                        var query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and Status == @1 " , strChurchDate, strChurchStatus).ToList().FirstOrDefault();
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName).ToList().FirstOrDefault();

                        if (query_dateandstatus != null && query_activity != null)
                        {
                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;
                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if ( ChurchDate && Activity) //ChurchDate and Activity
            {
                string strChurchDate = m_DateSelected.ToString("MM-dd-yyyy");
                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Count > 0 && m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {
                        var query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0", strChurchDate).ToList().FirstOrDefault();
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName).ToList().FirstOrDefault();

                        if (query_dateandstatus != null && query_activity != null)
                        {
                            
                            m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;
                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }


            }
            else if (Activity && ActivityDate) // Activity, Activity Date
            {
                string strActivityDate = m_ActivityDateSelected?.ToString("MM-dd-yyyy");

                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;


                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {

                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0 and ActivityGroup == @1 and ParentTaskName == @2 and ChildTaskName == @3" , strActivityDate, ActivityGroup, ActivityParentName, ActivityChildName).ToList().FirstOrDefault();

                        if (query_activity != null)
                        {


                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;
                            m_lstdefaultTableRowsCopy[i].Activity_Last_Attended = strActivityDate;
                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if (Status && Activity) //Status and Activity
            {
                
                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;

                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Count > 0 && m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {
                        var query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus).ToList().FirstOrDefault();
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName).ToList().FirstOrDefault();

                        if (query_dateandstatus != null && query_activity != null)
                        {

                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if (ActivityDate && Status) //Activity Date, Status
            {
                string strActivityDate = m_ActivityDateSelected?.ToString("MM-dd-yyyy");
             

                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";



                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {


                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Count > 0 && m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {
                        var query_status = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus).ToList().FirstOrDefault();
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0", strActivityDate).ToList().FirstOrDefault();

                        if (query_status != null && query_activity != null)
                        {
                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].Activity_Last_Attended = strActivityDate;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }
                    
                }
            }
            else if (ChurchDate && Status) // ChurchDate, and church status
            {




                string strChurchDate = m_DateSelected.ToString("MM-dd-yyyy");


                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";

                //strChurchStatus = querystring.

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    var query_row = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and Status == @1", strChurchDate, strChurchStatus).ToList().FirstOrDefault();



                    if (query_row != null)
                    {
                        m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                        m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;

                        m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                    }
                }

            }
        
            else if (Status) //Status
            {

                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";

                //strChurchStatus = querystring.

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    var query_row = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus).ToList().FirstOrDefault();



                    if (query_row != null)
                    {
                        m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                        

                        m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                    }
                }

            }
            else if (ChurchDate) //DATE
            {
                
                string strChurchDate = m_DateSelected.ToString("MM-dd-yyyy");
                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    var query_row = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0", strChurchDate).ToList().FirstOrDefault();
                                                                                             


                    if (query_row != null)
                    {
                        
                        m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;

                        m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                    }
                }

            }
            else if (ActivityDate) //Activity Date
            {
                string strActivityDate = m_ActivityDateSelected?.ToString("MM-dd-yyyy");


              

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {


                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {
                        
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0", strActivityDate).ToList().FirstOrDefault();

                        if (query_activity != null)
                        {
                            
                            m_lstdefaultTableRowsCopy[i].Activity_Last_Attended = strActivityDate;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if ( Activity) //Activity
            {
                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;

               

                for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Count > 0)
                    {
                        
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName).ToList().FirstOrDefault();

                        if (query_activity != null)
                        {

                            
                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
         
               
            if (m_lstQueryTableRows.Count >= 0 )
            {
           
                dataGrid.DataContext = m_lstQueryTableRows;
                dataGrid.Items.Refresh();
                lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
                dataGrid.IsReadOnly = true;
                m_isQueryTableShown = true;
                btnGenerateFollowUps.IsEnabled = false;
                btnDelete.IsEnabled = false;
                lblTableShown.Content = "Query Results ...";
            }
            
                


            Cursor = Cursors.Arrow;
        }
        private void ActivityTreeView_Checkbox_UnChecked(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            m_isActivityChecked = false;
            if (m_currentSelected_ActivityPair != null)
            {
                ClearTreeView();
                m_currentSelected_ActivityPair = null;
                
            }

            Cursor = Cursors.Arrow;

        }

   
        private void GridsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Cursor = Cursors.Wait;

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

            
            if ((GridsTabControl.SelectedItem as TabItem).Name == "ActiveTab")
            {
               

                if (!m_AttendanceView) // prevent from executing twice when already on the same tab
                {

                    m_alistView = false;
                    m_AttendanceView = true;
                    m_activityView = false;

                    //delete last row if no new attendee firstname or lastname entered
#if (init_db)
                    if (m_lstattendanceTableRows.LastOrDefault().FirstName == "" ||
                        m_lstattendanceTableRows.LastOrDefault().LastName == "")
                    {
                        m_lstattendanceTableRows.RemoveAt(m_lstattendanceTableRows.Count - 1);
                    }
#endif

                    //SaveProspectPanelState();
                   


                    
                   

                   
                    ClearTreeView();
                  



                    btnNewRec.IsEnabled = false;
                    btnDelete.IsEnabled = true;
                    btnImportRecords.IsEnabled = false;


                    if (!m_IsActivePanelView)
                    {
                        LoadActivePanelState();
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

                 //   SaveActivePanelState();

                    
                    // load ProspectTab state from TabState class
                 //   LoadProspectPanelState();
                  


                    btnImportRecords.IsEnabled = true;
                    btnImportRecords.Content = "Update Changes";

                    btnNewRec.IsEnabled = true;
                    btnDelete.IsEnabled = false;


                    if (!m_IsPanelProspectView)
                    {
                        Show_prospectview_Panel();
                    }

                    Display_AttendeeListTable_in_Grid();
                }
            }

            Cursor = Cursors.Arrow;

        }

        private void LoadActivityPanelState()
        {

            btnNewRec.IsEnabled = false;
            btnImportRecords.IsEnabled = false;
            btnDelete.IsEnabled = false;

            chkActivityDateFilter.IsChecked = true;
            chkActivityFilter.IsChecked = true;
            btnPanelAddActivity.IsEnabled = true;
            btnPanelNewActivity.IsEnabled = true;
        }
        private void SaveActivityPanelState()
        {
            m_TabState.txtSearchActivityState = txtSearch.Text;
            m_TabState.ActivityPanel_isActivityDateChecked = chkActivityDateFilter.IsChecked;
            m_TabState.ActivityPanel_isActivityFilterChecked = chkActivityFilter.IsChecked;
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
            m_TabState.ActivePanel_isActivityChecked = m_isActivityChecked;
            m_TabState.ActivityPanel_isActivityFilterChecked = chkActivityFilter.IsChecked;
        }
        private void LoadProspectPanelState()
        {


            txtSearch.Text = "";// m_TabState.txtSearchProspectState;
            txtSearch.IsEnabled = false; /*change from true to false in v3.0.8 to workaround visualization bug that erase filtered prospect table checkboxes */
            chkChurchDateFilter.IsChecked = false;// m_TabState.ProspectPanel_isFilterbyDateChecked;
        }
        private void LoadActivePanelState()
        {


            btnPanelNewActivity.IsEnabled = false;
            btnPanelAddActivity.IsEnabled = false;

            txtSearch.Text = m_TabState.txtSearchActiveState;
            
            chkAttended.IsChecked = m_TabState.ActivePanel_isAttendedChecked;
            chkFollowup.IsChecked = m_TabState.ActivePanel_isFollowUpChecked;
            chkResponded.IsChecked = m_TabState.ActivePanel_isRespondedChecked;
            chkChurchDateFilter.IsChecked = m_TabState.ActivePanel_isFilterbyDateChecked;
            chkActivityDateFilter.IsChecked = m_TabState.ActivePanel_isFilterbyActivityDateChecked;
            chkActivityFilter.IsChecked = m_TabState.ActivityPanel_isActivityFilterChecked;

         
            chkChurchStatusFilter.IsChecked = m_TabState.ActivePanel_isChurchStatusChecked;
            
        }

        private void Show_activeview_Panel()
        {


           
                spFilterOptions.Children.Clear();



            btnAddActivity.IsEnabled = true;
            btnFilterOpts.IsEnabled = true;
                if (btnAddActivity.IsChecked.GetValueOrDefault() )
                {
                    Show_Activity_Panel();
                    btnPanelNewActivity.Visibility = Visibility.Visible;
                    btnPanelAddActivity.Visibility = Visibility.Visible;
                }
                else
                {
                    gbFilterOptions.Header = "Filter Options";
                    btnPanelNewActivity.Visibility = Visibility.Hidden;
                    btnPanelAddActivity.Visibility = Visibility.Hidden;

                    if (!DateStackPanel.Children.Contains(chkActivityDateFilter))
                    {
                        DateStackPanel.Children.Add(chkActivityDateFilter);

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

                   

                // spFilterOptions.Children.Add(spActifityFilter);
                spFilterOptions.Children.Add(CalendarExpander);
                    spFilterOptions.Children.Add(ChurchStatusExpander);
                    spFilterOptions.Children.Add(ActivityExpander);


                    txtblkTaskDescription.Text = "";

                    m_IsActivityPanelView = false;
                    m_IsActivePanelView = true;
                    m_IsPanelProspectView = false;

                    btnExecQuery.IsEnabled = true;
                    btnDelete.IsEnabled = true;
                    btnGenerateFollowUps.IsEnabled = true; ;
                }














        }

        private void Show_prospectview_Panel()
        {
            spFilterOptions.Children.Clear();
            gbFilterOptions.Header = "Edit Table";

            m_IsPanelProspectView = true;
            m_IsActivityPanelView = false;
            m_IsActivePanelView = false;


            btnFilterOpts.IsEnabled = false;
            btnAddActivity.IsEnabled = false;
           

            if (DateStackPanel.Children.Contains(chkActivityDateFilter))
                DateStackPanel.Children.Remove(chkActivityDateFilter);

            if (!DateStackPanel.Children.Contains(chkChurchDateFilter))
            {

                chkChurchDateFilter.Content = "Church Date";
            
                DateStackPanel.Children.Insert(0, chkChurchDateFilter);
            }
            else
            {
                chkChurchDateFilter.Content = "Church Date";
         
            }

           
            chkChurchDateFilter.IsChecked = true;
            // spFilterOptions.Children.Remove(spFilterOptions);
            spFilterOptions.Children.Add(CalendarExpander);
         
            txtblkTaskDescription.Text = "";

            btnGenerateFollowUps.IsEnabled = false;
            btnExecQuery.IsEnabled = false;
            btnDelete.IsEnabled = false;

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
        

        }

        private void chkActivityDateFilter_Checked(object sender, RoutedEventArgs e)
        {

            m_isActivityfilterByDateChecked = true;
            m_isFilterByDateChecked = false;

            chkChurchDateFilter.IsChecked = false;
            DateCalendar.IsEnabled = true;

           





        }

        private void chkActivityDateFilter_Unchecked(object sender, RoutedEventArgs e)
        {

            m_isActivityfilterByDateChecked = false;
            
           

                if (!m_isFilterByDateChecked)
                DateCalendar.IsEnabled = false;

            if (m_alistView)
                return;
           

         

        }

        private void chkChurchStatusFilter_Checked(object sender, RoutedEventArgs e)
        {
            m_isChurchStatusFilterChecked = true;
            
            chkAttended.IsEnabled = true;
            chkFollowup.IsEnabled = true;
            chkResponded.IsEnabled = true;
           

        



        }

        private void chkChurchStatusFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            m_isChurchStatusFilterChecked = false;
           

            if (chkAttended.IsChecked.Value )
                chkAttended.IsChecked = false;

            if (chkFollowup.IsChecked.Value)
               chkFollowup.IsChecked = false;

            if (chkResponded.IsChecked.Value)
                chkResponded.IsChecked = false;

            chkAttended.IsEnabled = false;
            chkFollowup.IsEnabled = false;
            chkResponded.IsEnabled = false;

       
            

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
            txtblkTaskDescription.Text = "";
            m_currentSelected_ActivityPair = null;
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

                               // BuildQuery_and_UpdateGrid();

                                txtblkTaskDescription.Text = subtask.Description;
                                task.IsSelected = true; //check parent
                                break;
                            }

                        }
                    }
                    else if ((m_ActivityName == task.TaskName) && task.IsSelected)
                    {
                        //BuildQuery_and_UpdateGrid();
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

            var latestAttRec = (from last_date in m_default_row_selected.ActivityList
                                orderby last_date.Date descending
                                select last_date).FirstOrDefault();

            //update default row with new latest activity
            m_default_row_selected.Activity_Last_Attended = latestAttRec.DateString;
            m_default_row_selected.Activity = latestAttRec.ToString();

            if (!m_default_row_selected.ActivityList.Any() )
            {
                m_default_row_selected.Activity = "n/a";
                m_default_row_selected.Activity_Last_Attended = "n/a";
            }
        
            Display_ActivityList_in_Grid();

        }
        private void DeleteRecordFromAttendanceInfoTable(IEnumerable<Attendance_Info> row_select)
        {

           
          
            List<Attendance_Info> rowsToBeDeleted = new List<Attendance_Info>(row_select);

          
            
            foreach (Attendance_Info dr in rowsToBeDeleted)
            {
            
                var AttInforec = m_dbContext.Attendance_Info.SingleOrDefault(id => id.Attendance_InfoId == dr.Attendance_InfoId);
                if (AttInforec != null)
                {
                    m_dbContext.Attendance_Info.Remove(AttInforec);
                }

                m_default_row_selected.AttendanceList.Remove(dr);

                var latestAttRec = (from last_date in m_default_row_selected.AttendanceList
                                    orderby last_date.Date descending
                                    select last_date).FirstOrDefault();

                m_default_row_selected.Church_Last_Attended = latestAttRec.DateString;
                m_default_row_selected.ChurchStatus = latestAttRec.Status;
            }

            if (!m_default_row_selected.AttendanceList.Any())
            {
                m_default_row_selected.ChurchStatus = "n/a";
                m_default_row_selected.Church_Last_Attended = "n/a";
            }
            Display_AttendanceList_in_Grid();

        }

        private void Display_ActivityList_in_Grid()
        {
            if (m_Activity_grid != null)
            {
                m_Activity_grid.DataContext = m_default_row_selected.ActivityList.OrderByDescending(rec=>rec.Date);
                m_Activity_grid.Items.Refresh();
            }
            
        }

        private void Display_AttendanceList_in_Grid()
        {
            if (m_AttendeeInfo_grid != null)
            {
                m_AttendeeInfo_grid.DataContext = m_default_row_selected.AttendanceList.OrderByDescending(rec=>rec.Date).ToList();
                m_AttendeeInfo_grid.Items.Refresh();
            }
            
        }
          

        private void btnExpandHistory_Click(object sender, RoutedEventArgs e)
        {
                      
            
            if (dataGrid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed)
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
               // dataGrid_LoadingRowDetails(null, null);

               // Display_AttendanceList_in_Grid();
               // Display_ActivityList_in_Grid();

                Disable_Filters();
                btnDelete.IsEnabled = false;
                btnExecQuery.IsEnabled = false;
                btnGenerateFollowUps.IsEnabled = false;

                //// user was on the add activity page and clicked the expander button
                //if (m_IsActivityPanelView)
                //{
                //    LoadActivePanelState();

                //    Show_activeview_Panel();
                //}
                //txtSearch.IsEnabled = false;

            }
            else
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
                Enable_Filters();
                btnDelete.IsEnabled = true;
                btnExecQuery.IsEnabled = true;
                btnGenerateFollowUps.IsEnabled = true;

                txtSearch.IsEnabled = true;
              
            }

         

        }


        private void dataGrid_LoadingRowDetails(object sender, DataGridRowDetailsEventArgs e)
        {

            Cursor = Cursors.Wait;

          
            // get GrdAttendee_InfoList element within the DataTemplate
            m_AttendeeInfo_grid = e.DetailsElement.FindName("GrdAttendee_InfoList") as DataGrid;

            //var Attendee_AttendeeInfoList = from rec in m_dbContext.Attendance_Info.Local
            //                                where (rec.AttendeeId == m_default_row_selected.AttendeeId)
            //                                select rec;

            //foreach (var attendance in Attendee_AttendeeInfoList)
            //{
            //    m_default_row_selected.AttendanceList.Add(attendance);
            //}

           // m_AttendeeInfo_grid.DataContext = m_default_row_selected.AttendanceList.OrderByDescending(rec => rec.Date);
            // get GrdAttendee_ActivityList element within the DataTemplate
            m_Activity_grid = e.DetailsElement.FindName("GrdAttendee_ActivityList") as DataGrid;

            //var Attendee_ActivityList = from rec in m_dbContext.Activities.Local
            //                            where rec.AttendeeId == m_default_row_selected.AttendeeId
            //                            select rec;

            //foreach (var activity in Attendee_ActivityList)
            //{
            //    m_default_row_selected.ActivityList.Add(activity);
            //}

            // m_Activity_grid.DataContext = m_default_row_selected.ActivityList.OrderByDescending(rec => rec.Date);

            Display_ActivityList_in_Grid();
            Display_AttendanceList_in_Grid();

            btnDelete.IsEnabled = false;
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

            Cursor = Cursors.Arrow;
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

            btnFilterOpts.IsChecked = false;
           

            
           

            if (!m_IsActivityPanelView && !m_isQueryTableShown)
            {
                SaveActivePanelState();
                LoadActivityPanelState();
                Show_Activity_Panel();
              

            }
            else if (m_IsActivityPanelView)
            {
                //do nothing
            }
            else
            {
                MessageBox.Show("Activities can only be added when no database queries are being displayed.\n\nPlease deselect all filter checkboxes and query the database again.", "Cannot add activity", MessageBoxButton.OK, MessageBoxImage.Stop);
            }

            

        }
        private void Show_Activity_Panel()
        {

                Enable_Filters();
          
                spFilterOptions.Children.Clear();

                gbFilterOptions.Header = "Activity";

            btnPanelNewActivity.Visibility = Visibility.Visible;
            btnPanelAddActivity.Visibility = Visibility.Visible;
            

            if (DateStackPanel.Children.Contains(chkChurchDateFilter))
                    DateStackPanel.Children.Remove(chkChurchDateFilter);



                if (!DateStackPanel.Children.Contains(chkActivityDateFilter))
                    DateStackPanel.Children.Add(chkActivityDateFilter);




              
            
            btnPanelAddActivity.IsEnabled = false;

            chkActivityDateFilter.IsChecked = true;
            chkChurchDateFilter.IsChecked = false;

           
            spFilterOptions.Children.Add(CalendarExpander);
                spFilterOptions.Children.Add(ActivityExpander);
                chkActivityFilter.IsChecked = true;
                
                txtblkTaskDescription.Text = "";

                m_IsActivePanelView = false;
                m_IsActivityPanelView = true;
                m_IsPanelProspectView = false;

                btnExecQuery.IsEnabled = false;
                btnDelete.IsEnabled = false;
                btnGenerateFollowUps.IsEnabled = false;


        }


        private void btnPanelAddActivity_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            m_currentSelected_ActivityPair.Date = m_ActivityDateSelected;
            
            if (m_currentSelected_ActivityPair.AttendeeId == 0)
            {
                m_currentSelected_ActivityPair.AttendeeId = m_default_row_selected.AttendeeId;
            }
            //if activity already exist in dbContext
            var queryifActivityExistList = m_default_row_selected.ActivityList.SingleOrDefault(rec => rec.ToString() == m_currentSelected_ActivityPair.ToString() &&rec.Date == m_currentSelected_ActivityPair.Date);
           
            if (queryifActivityExistList == null )
            {

             
                m_dbContext.Activities.Add(m_currentSelected_ActivityPair);

                var ActivityIsinList = m_default_row_selected.ActivityList.SingleOrDefault(rec => rec.ToString() == m_currentSelected_ActivityPair.ToString() && rec.Date == m_currentSelected_ActivityPair.Date);
                if (ActivityIsinList == null)
                {
                    m_default_row_selected.ActivityList.Add(m_currentSelected_ActivityPair);
                }
                    
               
                

               
                var lastActivity = (from rec in m_default_row_selected.ActivityList
                                    where rec.AttendeeId == m_currentSelected_ActivityPair.AttendeeId
                                    orderby rec.Date descending
                                    select rec).ToList().FirstOrDefault();
                if (lastActivity != null)
                {
                    m_default_row_selected.Activity = lastActivity.ToString();
                    m_default_row_selected.Activity_Last_Attended = lastActivity.DateString;
                }
             



                ClearTreeView();

                btnPanelAddActivity.IsEnabled = false;
                m_currentSelected_ActivityPair = null;
                m_activitychecked_count = 0;
                txtSearch.IsEnabled = true;
                Display_ActivityList_in_Grid();

            }
            else
            {
                MessageBox.Show("Activity already exist for this attendee, please choose another activity or date.", "Duplicate activity", MessageBoxButton.OK, MessageBoxImage.Error);
                Cursor = Cursors.Arrow;
                return;
            }




          
            
            
            Cursor = Cursors.Arrow;
        }

        private void cmbAttendanceInfo_LayoutUpdated(object sender, EventArgs e)
        {
            
        }

        private void dataGrid_prospect_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           // dataGrid_prospect.Items.Refresh();
        }


        private void MenuItem_AddNewActivity_Click(object sender, RoutedEventArgs e)
        {
            int WindowMode = 1;

            
            if (m_currentSelected_ActivityPair != null)
            {
                WndAddGroup AddgroupWin = new WndAddGroup(ref m_lstActivities, WindowMode, m_currentSelected_ActivityPair);
                AddgroupWin.ShowDialog();
                m_newlstActivitiesCount = m_newlstActivitiesCount + AddgroupWin.GetActivitiesCount;
                trvActivities.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Must select an activity first. Select an activity checking by left click on the activity.");
            }
            

           
           



        }

        private void MenuItem_DeleteActivity_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            if (m_currentSelected_ActivityPair !=null)
            {
                string childtask = m_currentSelected_ActivityPair.ChildTaskName;
                string activityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string parenttask = m_currentSelected_ActivityPair.ParentTaskName;

                var a_group = m_lstActivities.SingleOrDefault(at => at.ActivityName == activityGroup);
                var task = a_group.lstActivityTasks.SingleOrDefault(at => at.TaskName == parenttask);
                int task_idx = a_group.lstActivityTasks.IndexOf(task);

                ActivityTask subtask = null;

               if (task != null)
                {
                   subtask = task.lstsubTasks.SingleOrDefault(st => st.TaskName == childtask);
                }
                    





                // user selected a task with child tasks

                if (activityGroup != "" && parenttask != "" && childtask != "")
                {

                    if (subtask != null)
                    {
                        
                        a_group.lstActivityTasks[task_idx].lstsubTasks.Remove(subtask);
                    }
                }
                // user selected a task with no child tasks
                else if (activityGroup != "" && parenttask != "" && childtask == "") 
                {
                    if (task != null)
                    {
                        a_group.lstActivityTasks.Remove(task);
                    }
                }
                //user selected a group
                else if (activityGroup != "" && parenttask == "") 
                {
                    if (a_group != null)
                    {
                        m_lstActivities.Remove(a_group);
                    }

                }
              
             


                ClearTreeView();

                m_newlstActivitiesCount = m_lstActivitiesCount + 1;
                trvActivities.Items.Refresh();
                Cursor = Cursors.Arrow;

              

            }
            else
            {
                MessageBox.Show("Must select an activity first.", "Delete Activity", MessageBoxButton.OK, MessageBoxImage.Stop);
                Cursor = Cursors.Arrow;
            }
        }



        private void MenuItem_DeleteActivityGroup_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
           

            if (m_currentSelected_ActivityPair != null)
            {
                var deleteActivityGroup = m_lstActivities.SingleOrDefault(ag => ag.ActivityName == m_ActivityName);
                if (deleteActivityGroup != null)
                {
                    m_lstActivities.Remove(deleteActivityGroup);
                    m_newlstActivitiesCount = m_lstActivitiesCount + 1;
                    trvActivities.Items.Refresh();
                }
                Cursor = Cursors.Arrow;
            }
            else
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("Must select an activity to delete first.", "Delete Activity", MessageBoxButton.OK, MessageBoxImage.Stop);
                
            }

           
        }
        private void MenuItem_AddNewGroup_Click(object sender, RoutedEventArgs e)
        {
            int WindowMode = 0;

           
             
                WndAddGroup groupWin = new WndAddGroup(ref m_lstActivities, WindowMode, m_currentSelected_ActivityPair);
                groupWin.ShowDialog();
                m_newlstActivitiesCount = m_newlstActivitiesCount + groupWin.GetActivitiesCount;

                trvActivities.Items.Refresh();
           
            
           

           
        }

        private void TrvActivities_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue.GetType().Name == "ActivityGroup")
            {
                ActivityGroup activity_group = (ActivityGroup)e.NewValue;






                ActivityPair selectedActivity = new ActivityPair
                {
                    ActivityGroup = activity_group.ActivityName,
                    AttendeeId = 0,
                    ParentTaskName = "",
                    ChildTaskName = "",


                };

                m_child_taskId = 0;
                m_parent_taskId = 0;
                m_currentSelected_ActivityPair = selectedActivity;
                m_currentSelect_ActivityTask = null;


                if (m_activitychecked_count == 1)
                {
                    m_previousSelected_ActivityPair = m_currentSelected_ActivityPair;
                }

                txtblkTaskDescription.Text = "";
            }
            
           
          
        
        }

        private void BtnPanelNewActivity_Click(object sender, RoutedEventArgs e)
        {


            MenuItem_AddNewActivity_Click(null, null);

        

        }

        private void BtnExecQuery_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;

        }

        private void BtnFilterOpts_Click(object sender, RoutedEventArgs e)
        {
            btnAddActivity.IsChecked = false;
           

            if (!m_IsActivePanelView)
            {
                SaveActivityPanelState();
                LoadActivePanelState();
                Show_activeview_Panel();

                btnAddActivity.IsChecked = false;
                btnFilterOpts.IsChecked = true;
            }
                

        }

        private void GrdAttendee_InfoList_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void DataGrid_prospect_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var datagrid = sender as DataGrid;

            m_attendance_row_selected = (AttendanceTableRow)dataGrid_prospect.SelectedItem;

            m_default_row_selected = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == m_attendance_row_selected.AttendeeId);

            if (m_default_row_selected != null)
            {
                var queryAttRec = m_dbContext.Attendees.Local.SingleOrDefault(attrec => attrec.AttendeeId == m_default_row_selected.AttendeeId);

                var text = e.EditingElement as TextBox;

                if (e.Column.Header != null)
                {
                    if (e.Column.Header.ToString() == "First Name")
                    {
                        if (queryAttRec != null)
                        {
                            queryAttRec.FirstName = text.Text;

                        }

                        m_default_row_selected.FirstName = text.Text;
                        m_default_row_selected.FirstLastName = text.Text.ToUpper() + " " + m_default_row_selected.LastName.ToUpper();

                        m_attendance_row_selected.FirstName = text.Text;
                        m_attendance_row_selected.FirstLastName = text.Text.ToUpper() + " " + m_attendance_row_selected.LastName.ToUpper();
                    }
                    else if (e.Column.Header.ToString() == "Last Name")
                    {
                        if (queryAttRec != null)
                        {
                            queryAttRec.LastName = text.Text;
                        }

                        m_default_row_selected.LastName = text.Text;
                        m_default_row_selected.FirstLastName = m_default_row_selected.FirstName.ToUpper() + " " + text.Text.ToUpper();

                        m_attendance_row_selected.LastName = text.Text;
                        m_attendance_row_selected.FirstLastName = text.Text.ToUpper() + " " + m_attendance_row_selected.LastName.ToUpper();
                    }
                }
            }
        }
    }

}





