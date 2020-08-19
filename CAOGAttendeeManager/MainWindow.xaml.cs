﻿using System;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using System.Data;
using System.Windows.Threading;
using System.Runtime.Remoting.Contexts;

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
            

            m_version_string = "v3.1.26";




            dataGrid.CopyingRowClipboardContent += new EventHandler<DataGridRowClipboardEventArgs>(CopyDataGridtoClipboard);






            //open file with database credentials
            SplashScreen splashScreen = new SplashScreen("Resources/splashscreen.png");
            splashScreen.Show(true);
            TimeSpan timespan = new TimeSpan(0, 0, 1); // 1 seconds timespan



            splashScreen.Close(timespan);


            var executingPath = Directory.GetCurrentDirectory();

            try
            {



#if (DEBUG)
                this.Title = $"Attendee Manager " + m_version_string + "(Debug) - ";
#else
                    this.Title = "Attendee Manager " + m_version_string;
#endif

#if (init_db)

                   
                    if (m_dbContext == null)
                    {

                        
                            m_dbContext = new ModelDb();
                            m_dbContext.Configuration.ProxyCreationEnabled = false;
                            m_dbContext.Configuration.AutoDetectChangesEnabled = true;

                            //load db context
                            m_dbContext.Attendees.Load();
                            m_dbContext.Attendance_Info.Load();
                            m_dbContext.Activities.Load();
                        

                    }



                    InitDataSet();
                   // ConvertListToDataTable();
#endif

#if (db_errors)
                   correctDBerrors();
#endif



                    // display the attendee records in the table
                   Display_DefaultTable_in_Grid();

            }
            catch (Exception ex)
            {

                MessageBox.Show($"Exception occurred when performing database initialization { ex}!\n","Database Error", MessageBoxButton.OK, MessageBoxImage.Error );
            }












        }




        private ModelDb m_dbContext;

        private DateTime? m_DateSelected = null;
        private DateTime? m_alistDateSelected = null;
        private DateTime? m_ActivityDateSelected = null;
        private DateTime? m_ActivityDateSelectedPr = null;
        private DataSet m_DataSet = new DataSet();


        bool m_ActivityTreeChanged = false; //check if the user changed the Activity tree

        // current selected row in the data tables

        private DefaultTableRow m_default_row_selected;
       
        private AttendanceTableRow m_attendance_row_selected;

        // pointer to datagrid within RowDetailsTemplate
        private DataGrid m_AttendeeInfo_grid = null;
        private DataGrid m_Activity_grid = null;

        //List of query rows
        private List<DefaultTableRow> m_lstQueryTableRows = new List<DefaultTableRow>() { };

        //List that is a Copy of the default list for table building purposes
        private List<DefaultTableRow> m_lstdefaultTableRowsCopy = new List<DefaultTableRow>() { };

        //List of default Table rows
        private List<DefaultTableRow> m_lstdefaultTableRows = new List<DefaultTableRow>() { };

      
        //array holding the Default table column headers
        private string[] m_aryColumnHeaders = new string[50];

        // list holding the rows of the prospect table
        private List<AttendanceTableRow> m_lstattendanceTableRows = new List<AttendanceTableRow>() { };

        //list holding the rows that are selected in the prospect table
        private System.Collections.IList m_MultiAttendanceRow_Selected;

        //ComboTreeBox
        private ComboTreeBox m_ctbActivity = new ComboTreeBox();
        private ComboTreeBox m_ctbActivityProspect = new ComboTreeBox();

        //list of Activity headers
        private List<ActivityHeader> m_lstActivityHeaders = new List<ActivityHeader> { };
        private List<ActivityTask> m_lstActivityTasks = new List<ActivityTask> { };


     


        // the current selected activity Pair
        private ActivityPair m_currentSelected_ActivityPair = null;
       

       
        private Timer aTimer = null;

        private string m_version_string = "";
        private string _followUpWeeks = "4";


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
        private bool m_isFirstNamefiltered = false;
        private bool m_isLastNamefiltered = false;

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

        private bool m_loaded = false;

      

        private int m_NewAttendeeId = 0;


        private void StopTimer()
        {
            if (aTimer != null)
            {
                aTimer.Enabled = false;
            }

        }
        private void SetTimer()
        {
            aTimer = new Timer(500);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {

            Dispatcher.Invoke(() =>
           {
               if (m_currentSelected_ActivityPair != null && m_ActivityDateSelectedPr != null && m_MultiAttendanceRow_Selected != null)
               {

                   btnPanelAddActivity.IsEnabled = true;
               }
               else
               {
                   btnPanelAddActivity.IsEnabled = false;
               }

               foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
               {
                   if (dr.Attended == "1" && m_alistdateIsValid)
                   {
                       btnImport.IsEnabled = true;
                       break;
                   }
                   else
                   {
                       btnImport.IsEnabled = false;
                   }
               }

               if (m_dbContext.ChangeTracker.HasChanges() || m_ActivityTreeChanged)
               {
                   btnSave.IsEnabled = true;

               }
               else
               {
                   btnSave.IsEnabled = false;
               }

               if (m_isQueryTableShown)
                   btnDelete.IsEnabled = false;
            


           });

        
        }

        private void correctDBerrors()
        {

         
            //DateTime find_date = new DateTime(2019, 08, 18);
            //string datestring = find_date.ToString("MM-dd-yyyy");


            //List<Attendance_Info> lstAtt = new List<Attendance_Info>() { };
            //int i = 0;

            //////get latest attended date per attendee
            //for (i = 0; i <= m_lstdefaultTableRows.Count - 1; i++)
            //{
            //    //get last church date attended per attendee history
            //    var attendeRec = m_lstdefaultTableRows[i].AttendanceList.SingleOrDefault(rec => rec.Date == find_date && rec.Status == "Attended");


            //    if (attendeRec != null)
            //    {
            //        //var followup = m_lstdefaultTableRows[i].AttendanceList.SingleOrDefault(rec2 => rec2.Date == new DateTime(2019, 06, 09) && rec2.Status == "Follow-Up");

            //        lstAtt.Add(attendeRec);
            //    }
            //}


            ////foreach (int j= 0; j <= lstAtt.Count - 1;j++)
            ////{
            //    //    var followuprec = lstAtt[i].
            //    //}
            //    m_dbContext.Attendance_Info.RemoveRange(lstAtt);
            ////}
            //    //i++;
            //    //Console.WriteLine($"Total records: {i}");



            //m_dbContext.Attendees.RemoveRange(query);



            //m_dbContext.SaveChanges();

            //Console.WriteLine("DB changes successfully saved");
        }

        private void InitActivityTreeView()
        {

            Load_ChurchActivities_From_XMLFile();


        }

        private void Save_ChurchActivities_To_XMLFile()
        {

            List<XNode> lstdocNodes = new List<XNode>() { };
            var doc_root = new XElement("XmlDocument");

            int intId = 0;
            Cursor = Cursors.Wait;



            //  XElement DefaultTableColumnElement = new XElement("DefaultTableColumns");



            //Save default table columns
            //for (int i = 0; i <= m_aryColumnHeaders.Length - 1; i++)
            //{

            //    XElement TableColumn = new XElement("TableColumn", new XAttribute("Header", m_aryColumnHeaders[i]));

            //    if (m_aryColumnHeaders[i] == "0") { break; }

            //    DefaultTableColumnElement.Add(TableColumn);
            //}
            //lstdocNodes.Add(DefaultTableColumnElement);
            XElement ProgramSettingsElement = new XElement("ProgramSettings");
            XElement FollowUpElement = new XElement("FollowUpWeeks", new XAttribute("Weeks", _followUpWeeks) );
            ProgramSettingsElement.Add(FollowUpElement);

            lstdocNodes.Add(ProgramSettingsElement);
            foreach (ActivityHeader ahead in m_lstActivityHeaders)
            {
                XElement ActivityHeaderElement = new XElement("ActivityHeader", new XAttribute("Name", ahead.Name));

                //ActivityGroups in Activity headers
                foreach (ActivityGroup agroup in ahead.Groups)
                {
                    //make group element
                    XElement ActivityGroupElement = new XElement("ActivityGroup", new XAttribute("ActivityName", agroup.ActivityName));

                    //ActivityTask Element
                    foreach (ActivityTask task in agroup.lstActivityTasks)
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
                        // make Activity Task Element
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

                    } // end foreach activity task

                    ActivityHeaderElement.Add(ActivityGroupElement); // Add Groups to the Header tag
                } // end foreach activity group


                // add ActivityHeader elements to doc_root Element of the DOM document
              
                lstdocNodes.Add(ActivityHeaderElement);

            } // end activity header tag


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
            catch (Exception)
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("No Activities file found!");
            }

            Cursor = Cursors.Arrow;

        }

        private void Load_ChurchActivities_From_XMLFile()
        {



            // zero out the header array
            //for (int i = 0; i <= m_aryColumnHeaders.Length - 1; i++)
            //{
            //    m_aryColumnHeaders[i] = "0";
            //}

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

                        XElement XMLtag = (XElement)XNode.ReadFrom(xreader);
                        //int i = 0;

                        if (XMLtag.Name == "ProgramSettings")
                        {
                            foreach ( XElement settingsElement in XMLtag.Elements())
                            {
                                if (settingsElement.Name == "FollowUpWeeks")
                                {
                                    
                                    string followupnumber = settingsElement.FirstAttribute.Value;
                                    _followUpWeeks = followupnumber;
                                }
                            }
                        }
                        //if (XMLtag.Name == "DefaultTableColumns")
                        //{
                        //    foreach (XElement TablecolElement in XMLtag.Elements())
                        //    {
                        //        string value = "";
                        //        value = (string)TablecolElement.Attribute("Header");

                        //        m_aryColumnHeaders[i] = value.ToLower();
                        //        i++;
                        //    }
                        //}


                        while (xreader.Name == "ActivityHeader")
                        {

                            XElement tag = (XElement)XNode.ReadFrom(xreader);

                            if (tag.Name == "ActivityHeader")
                            {
                                //ActivityHeader
                                string ActivityHeaderName = (string)tag.Attribute("Name");
                                ActivityHeader aheader = new ActivityHeader();
                                aheader.Name = ActivityHeaderName;

                                //ActivityGroups   
                                foreach (XElement ActivityGroups in tag.Elements())
                                {
                                    string xmlAttName = (string)ActivityGroups.Attribute("ActivityName");
                                    ActivityGroup trv_activityGroup = new ActivityGroup { Parent = ActivityHeaderName, ActivityName = xmlAttName };
                                  

                                    //ActivityTasks
                                    foreach (XElement ActivityTaskElement in ActivityGroups.Elements())
                                    {
                                        int id = (int)ActivityTaskElement.Attribute("Id");
                                        string name = (string)ActivityTaskElement.Attribute("TaskName");
                                        string description = (string)ActivityTaskElement.Attribute("Description");

                                        ActivityTask trv_activityTask = new ActivityTask { Parent = trv_activityGroup.ActivityName, ActivityId = id, TaskName = name, Description = description };
                                    

                                        //if Activity Subtasks
                                        if (ActivityTaskElement.HasElements)
                                        {
                                            //Subtasks
                                            foreach (XElement ActivitySubTask in ActivityTaskElement.Elements())
                                            {
                                                int subtaskId = (int)ActivitySubTask.Attribute("Id");
                                                string subtaskName = (string)ActivitySubTask.Attribute("TaskName");
                                                string subtaskdescription = (string)ActivitySubTask.Attribute("Description");

                                                ActivityTask trv_activitysubTask = new ActivityTask { Parent = trv_activityTask.TaskName, ActivityId = subtaskId, TaskName = subtaskName, Description = subtaskdescription };
                                                //add subtask to lstActivityTask
                                                trv_activityTask.lstsubTasks.Add(trv_activitysubTask);
                                             
                                             

                                            }

                                        }

                                        //add activity tasks to activity group
                                        trv_activityGroup.lstActivityTasks.Add(trv_activityTask);
                                   

                                    }

                                    aheader.Groups.Add(trv_activityGroup);

                                }

                                //add groups to lstActivityHeaders list
                                m_lstActivityHeaders.Add(aheader);
                            } // end Activityheader

                            xreader.ReadEndElement();
                        } // end while
                      
                    }
                    
                    LoadNewComboTree(m_lstActivityHeaders);
                   


                }
                else // activities file does not exist
                {
                    Cursor = Cursors.Arrow;
                    MessageBox.Show("No Activities file found or file is in the wrong format! 'ChurchActivities.xml'");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(),"XML read error",MessageBoxButton.OK, MessageBoxImage.Error);

            }






        }

        private void Convert_and_SaveNewTreeToActivityHeardersTree(IEnumerable<TreeNode> tree)
        {
            List<ActivityHeader> ActivityTree = new List<ActivityHeader>() { };

            foreach (TreeNode header in tree)
            {
                ActivityHeader parent = new ActivityHeader { Name = (string)header.Header };
               
                //ActivityGroups
                foreach (TreeNode group in header.Items)
                {
                    ActivityGroup aGroup = new ActivityGroup { ActivityName = (string)group.Header };

                    parent.Groups.Add(aGroup);


                    //ActivityTask
                    foreach (TreeNode task in group.Items)
                    {
                        ActivityTask aTask = new ActivityTask { TaskName = (string)task.Header, Description = (string)task.Description };

                        aGroup.lstActivityTasks.Add(aTask);
                        //subTask
                        foreach (TreeNode subtask in task.Items)
                        {
                            ActivityTask subTask = new ActivityTask { TaskName = (string)subtask.Header, Description = (string)subtask.Description };

                            aTask.lstsubTasks.Add(subTask);
                        }
                    }
                }
                ActivityTree.Add(parent);
            }
            
            // save tree to private class variable
            m_lstActivityHeaders = ActivityTree;


            
        }
        private void LoadNewComboTree(List<ActivityHeader> tree)
        {
            if (m_ctbActivity.Nodes.Any() && m_ctbActivityProspect.Nodes.Any())
            {
                m_ctbActivity.Nodes.Clear();
                m_ctbActivityProspect.Nodes.Clear();
            }
            //Add ComboTreeNodes to ComboTreeBox Treeview
            foreach (var header in tree)
            {
                //ActivityHeader
                ComboTreeNode parent = m_ctbActivity.Nodes.Add(header.Name);
                ComboTreeNode parent2 = m_ctbActivityProspect.Nodes.Add(header.Name);
                //ActivityGroups
                foreach (var group in header.Groups)
                {
                    ComboTreeNode child = parent.Nodes.Add(group.ActivityName);
                    ComboTreeNode child2 = parent2.Nodes.Add(group.ActivityName);
                    //ActivityTask
                    foreach (var task in group.lstActivityTasks)
                    {
                        ComboTreeNode taskNode = child.Nodes.Add(task.TaskName);
                        ComboTreeNode taskNode2 = child2.Nodes.Add(task.TaskName);
                        //subTask
                        foreach (var subtask in task.lstsubTasks)
                        {
                            ComboTreeNode subTaskNode = taskNode.Nodes.Add(subtask.TaskName);
                            ComboTreeNode subTaskNode2 = taskNode2.Nodes.Add(subtask.TaskName);
                        }
                    }

                }
            }
        }
        private void LoadNewComboTree(IEnumerable<TreeNode> tree)
        {
            if (m_ctbActivity.Nodes.Any() && m_ctbActivityProspect.Nodes.Any())
            {
                m_ctbActivity.Nodes.Clear();
                m_ctbActivityProspect.Nodes.Clear();
            }
            //Add ComboTreeNodes to ComboTreeBox Treeview
            foreach (TreeNode header in tree)
            {
                ComboTreeNode node = new ComboTreeNode();
                node.Text = (string)header.Header;

                ComboTreeNode parent = m_ctbActivity.Nodes.Add(node.Text);
                ComboTreeNode parent2 = m_ctbActivityProspect.Nodes.Add(node.Text);
                //ActivityGroups
                foreach (TreeNode group in header.Items)
                {
                    ComboTreeNode node2 = new ComboTreeNode();
                    node2.Text = (string)group.Header;

                    ComboTreeNode child = parent.Nodes.Add(node2.Text);
                    ComboTreeNode child2 = parent2.Nodes.Add(node2.Text);
                    //ActivityTask
                    foreach (TreeNode task in group.Items)
                    {
                        ComboTreeNode node3 = new ComboTreeNode();
                        node3.Text = (string)task.Header;

                        ComboTreeNode taskNode = child.Nodes.Add(node3.Text);
                        ComboTreeNode taskNode2 = child2.Nodes.Add(node3.Text);
                        //subTask
                        foreach (TreeNode subtask in task.Items)
                        {
                            ComboTreeNode node4= new ComboTreeNode();
                            node4.Text = (string)subtask.Header;

                            ComboTreeNode subTaskNode = taskNode.Nodes.Add(node4.Text);
                            ComboTreeNode subTaskNode2 = taskNode2.Nodes.Add(node4.Text);
                        }
                    }

                }
            }
        }
        private bool GenerateDBFollowUps(string followUpWeeks)
        {
            int intFollowUpDays = int.Parse(followUpWeeks) * 7;
            
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
            TimeSpan? timespanSinceDate = new TimeSpan();
            List<Attendance_Info> lstAttendanceInfo = new List<Attendance_Info>() { };

            DateTime? greatest_date = null;


            ////get latest and greatest date 
            for (int i = 0; i < m_lstdefaultTableRows.Count; i++)
            {
                var latest_date_attened = (from d in m_lstdefaultTableRows[i].AttendanceList
                                           orderby d?.Date ascending
                                           select d).ToArray().LastOrDefault();

                if (latest_date_attened != null)
                {
                    if (i == 0)
                    {
                        greatest_date = latest_date_attened.Date;
                    }

                    if (latest_date_attened.Date > greatest_date)
                    {
                        greatest_date = latest_date_attened.Date;

                    }
                }




            }
            //get latest attended date per attendee
            for (int i = 0; i <= m_lstdefaultTableRows.Count - 1; i++)
            {

                //get last church date attended per attendee history
                var lstDateRec = (from rec in m_lstdefaultTableRows[i].AttendanceList
                                  where rec.Status == "Attended" || rec.Status == "Responded"
                                  orderby rec descending
                                  select rec).ToList().FirstOrDefault();

              
                if (lstDateRec != null)
                {
                    timespanSinceDate = greatest_date - lstDateRec?.Date;


                
                    if (timespanSinceDate?.Days < intFollowUpDays) // 4 weeks not present
                    {
                        
                        //// do nothing
                        ////Attendee already have a followUp sent so do not generate another followup unil 21 days has
                        ////lapsed since the last followUp        


                    }
                    else
                    {

                      
                        var HasAlreadyFollowUpDate = m_lstdefaultTableRows[i].AttendanceList.SingleOrDefault(rec => rec.Status == "Follow-Up" && rec.Date == greatest_date);


                        //if no follow-up record then generate a follow-up
                        if (HasAlreadyFollowUpDate == null)
                        {
                            Attendance_Info newfollowUpRecord = new Attendance_Info { };
                            newfollowUpRecord.AttendeeId = m_lstdefaultTableRows[i].AttendeeId;
                            newfollowUpRecord.Date = greatest_date;
                            newfollowUpRecord.Status = "Follow-Up";

                            lstAttendanceInfo.Add(newfollowUpRecord);
                            generate_one = true;

                        }

                    }

                } //end if

            }

            if (generate_one)
            {
                m_dbContext.Attendance_Info.AddRange(lstAttendanceInfo);
                InitDataSet();
                Display_DefaultTable_in_Grid();

            }


            return true;//generate_one;


        }


        private void Display_AttendeeListTable_in_Grid()
        {



            dataGrid_prospect.DataContext = m_lstattendanceTableRows.OrderBy(rec => rec.LastName).ToList();
            dataGrid_prospect.Items.Refresh();
            lblAttendenceMetrics.Text = dataGrid_prospect.Items.Count.ToString();
            dataGrid_prospect.IsReadOnly = false;




        }




        private void Display_DefaultTable_in_Grid()
        {

            
            dataGrid.DataContext = m_lstdefaultTableRows.OrderBy(rec => rec.LastName).ToList();


            dataGrid.Items.Refresh();
            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            m_isQueryTableShown = false;
            dataGrid.IsReadOnly = false;
            btnDelete.IsEnabled = true;
            btnGenerateFollowUps.IsEnabled = true;


            dataGrid.IsReadOnly = false;



        }



        private void InitDataSet()
        {

            if (m_lstdefaultTableRows.Any() )
            {
                m_lstdefaultTableRows.Clear();

            }
            if (m_lstattendanceTableRows.Any() )
            {
                m_lstattendanceTableRows.Clear();
            }
          
          
            


          

            try
            {


                string ldate = "";
                string lstatus = "";
                string adate = "";

                int i = 0;



                foreach (var AttendeeRec in m_dbContext.Attendees.Local)
                {
                    var queryLastDate = (from DateRec in AttendeeRec.AttendanceList
                                         where DateRec.Status == "Attended" || DateRec.Status == "Responded"
                                         orderby DateRec.Date ascending
                                         select DateRec).ToList().LastOrDefault();

                    var queryActivityLastDate = (from ActivityDateRec in AttendeeRec.ActivityList
                                                 orderby ActivityDateRec.Date ascending
                                                 select ActivityDateRec).ToList().LastOrDefault();

                


                    if (queryLastDate != null)
                    {
                        //----Construct AttendeeLisTable-------------------------------------------------------------------------------------

                        // fill Attendance table columns. Add to list for each row


                        AttendanceTableRow AttendanceTabledr = new AttendanceTableRow
                        {
                            AttendeeId = AttendeeRec.AttendeeId,
                            LastName = AttendeeRec.LastName,
                            FirstName = AttendeeRec.FirstName,
                            DateString = "Date Not Valid",
                            Attended = AttendeeRec.Checked ? "1" : "",
                            IsModifiedrow = AttendeeRec.Checked,


                        };

                        DefaultTableRow DefaultTabledr = new DefaultTableRow
                        {
                            AttendanceList = AttendeeRec.AttendanceList,
                            //Columns = m_aryColumnHeaders

                        };

                        DefaultTabledr.PropertyChanged += DefaultTabledr_PropertyChanged;
                        AttendanceTabledr.PropertyChanged += AttendanceTabledr_PropertyChanged;

                        ldate = queryLastDate.Date?.ToString("MM-dd-yyyy");
                        if (queryActivityLastDate != null)
                            adate = queryActivityLastDate.Date?.ToString("MM-dd-yyyy");

                        lstatus = queryLastDate.Status;

                        m_NewAttendeeId = AttendeeRec.AttendeeId;


                        DefaultTabledr.AttendeeId = AttendeeRec.AttendeeId;

                      
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

                        //Add to the rows to the datagirds
                        m_lstattendanceTableRows.Add(AttendanceTabledr);
                        m_lstdefaultTableRows.Add(DefaultTabledr);



                    }
                    else // There are no Attended status for attendee, look for any follow-up statuses
                    {
                        var queryLastDateFollowUp = (from DateRec in AttendeeRec.AttendanceList
                                                     where DateRec.Status == "Follow-Up"
                                                     orderby DateRec.Date ascending
                                                     select DateRec).ToList().LastOrDefault();

                        if (queryLastDateFollowUp != null)
                        {
                            //----Construct AttendeeLisTable-------------------------------------------------------------------------------------

                            // fill Attendance table columns. Add to list for each row


                            AttendanceTableRow AttendanceTabledr = new AttendanceTableRow
                            {
                                AttendeeId = AttendeeRec.AttendeeId,
                                LastName = AttendeeRec.LastName,
                                FirstName = AttendeeRec.FirstName,
                                DateString = "Date Not Valid",
                                Attended = AttendeeRec.Checked ? "1" : "",
                                IsModifiedrow = AttendeeRec.Checked,


                            };
                            DefaultTableRow DefaultTabledr = new DefaultTableRow
                            {
                                AttendanceList = AttendeeRec.AttendanceList,
                                //Columns = m_aryColumnHeaders

                            };

                            DefaultTabledr.PropertyChanged += DefaultTabledr_PropertyChanged;
                            AttendanceTabledr.PropertyChanged += AttendanceTabledr_PropertyChanged;

                            lstatus = queryLastDateFollowUp.Status;



                            m_NewAttendeeId = AttendeeRec.AttendeeId;
                         

                            DefaultTabledr.AttendeeId = AttendeeRec.AttendeeId;


                            DefaultTabledr.FirstName = AttendeeRec.FirstName;
                            DefaultTabledr.LastName = AttendeeRec.LastName;

                            DefaultTabledr.Church_Last_Attended = "n/a";
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

                            //Add to the rows to the datagirds
                            m_lstattendanceTableRows.Add(AttendanceTabledr);
                            m_lstdefaultTableRows.Add(DefaultTabledr);
                        }


                    }

                    // DefaultTabledr.MakeDynamicProperties();
#if (ONLY10)

                    if (i == 10)
                        break;
#endif
                    i++;



                  




                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred when performing database operation: {ex}");
            }




        }

        private void AttendanceTabledr_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (m_loaded)
            {

                var editRow = sender as AttendanceTableRow;


                m_attendance_row_selected = editRow;

                if (m_attendance_row_selected != null)
                {
                    m_default_row_selected = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == m_attendance_row_selected.AttendeeId);
                    if (m_default_row_selected != null)
                        m_default_row_selected.PropertyChanged -= DefaultTabledr_PropertyChanged; // temporary disable the property changed from the default table row record
                }
                    




                if (m_attendance_row_selected != null && m_default_row_selected != null)
                {
                    //Find the Attendee record in the database context
                    var queryAttRec = m_dbContext.Attendees.Local.SingleOrDefault(attrec => attrec.AttendeeId == m_default_row_selected.AttendeeId);
                    if (queryAttRec != null)
                    {
                        if (e.PropertyName == "FirstName")
                        {
                            queryAttRec.FirstName = editRow.FirstName;

                         
                            m_default_row_selected.FirstName = editRow.FirstName;
                          
                            
                        }
                        else if (e.PropertyName == "LastName")
                        {
                            queryAttRec.LastName = editRow.LastName;
                         
                            m_default_row_selected.LastName = editRow.LastName;
                          
                        }
                        else if (e.PropertyName == "Attended")
                        {
                            queryAttRec.Checked = true;
                        }
                    }

                    m_default_row_selected.PropertyChanged += DefaultTabledr_PropertyChanged;
                }

            }
        }

        private void DefaultTabledr_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (m_loaded)
            {

                var editRow = sender as DefaultTableRow;
                
                
                    m_default_row_selected = editRow;



                if (m_default_row_selected != null)
                {
                    m_attendance_row_selected = m_lstattendanceTableRows.SingleOrDefault(rec => rec.AttendeeId == m_default_row_selected.AttendeeId);
                    if (m_attendance_row_selected !=null)
                        m_attendance_row_selected.PropertyChanged -= AttendanceTabledr_PropertyChanged; // temporary disable the property changed from the attendance record
                }


                if (m_attendance_row_selected != null && m_default_row_selected != null)
                {
                    //Find the Attendee record in the database context
                    var queryAttRec = m_dbContext.Attendees.Local.SingleOrDefault(attrec => attrec.AttendeeId == m_default_row_selected.AttendeeId);
                    if (queryAttRec != null)
                    {
                        if (e.PropertyName == "FirstName")
                        {
                         
                            queryAttRec.FirstName = editRow.FirstName;
                            m_attendance_row_selected.FirstName = editRow.FirstName;

                            


                        }
                        else if (e.PropertyName == "LastName")
                        {
                          
                            queryAttRec.LastName = editRow.LastName;
                            m_attendance_row_selected.LastName = editRow.LastName;
                         
                        }
                      
                    }
                    // re-register PropertyChanged property to class
                    m_attendance_row_selected.PropertyChanged += AttendanceTabledr_PropertyChanged;
                }

            }

        }

        private void Disable_Filters()
        {
            cmbHeaderStatus.IsEnabled = false;
            m_ctbActivity.Enabled = false;
            dpChurchLastAttended.IsEnabled = false;
            dpHeaderActivityLastAttended.IsEnabled = false;

            m_ctbActivityProspect.Enabled = false;
            dpChurchLastAttendedPr.IsEnabled = false;
            dpHeaderActivityPr.IsEnabled = false;
            btnPanelAddActivity.IsEnabled = false;
            dpHeaderActivityPr.IsEnabled = false;
            btnPanelNewActivity.IsEnabled = false;



        }

        private void txtSearch_TextChanged_FName(object sender, TextChangedEventArgs e)
        {
            //if in followUp view, use query database else if in model list view filter table

            string text = txtHeaderFirstName.Text.ToUpper();


            if (txtHeaderFirstName.Text != "")
            {

                Disable_Filters();


            


                if (m_AttendanceView)
                {

                    if (m_isQueryTableShown)
                    {
                        var filterQueryTable = m_lstQueryTableRows.Where(row => row.FirstName.ToUpper().Contains(text)).ToList();
                        dataGrid.DataContext = filterQueryTable;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
                    }
                    else
                    {
                        var filteredDefaultTable = m_lstdefaultTableRows.Where(row => row.FirstName.ToUpper().Contains(text)).ToList();
                        dataGrid.DataContext = filteredDefaultTable;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();

                    }


                }
            




            }
            else // txt = ""
            {
             
                Enable_Filters();
                if (m_isQueryTableShown)
                {
                    Display_Query_Table(m_lstQueryTableRows.AsQueryable());

                }
                else
                {
                    Display_DefaultTable_in_Grid();
                }
            }
        }

        private void txtSearch_TextChanged_LName(object sender, TextChangedEventArgs e)
        {


            string text = txtHeaderLastName.Text.ToUpper();


            if (txtHeaderLastName.Text != "")
            {

                Disable_Filters();


             


                if (m_AttendanceView)
                {

                    if (m_isQueryTableShown)
                    {
                        var filterQueryTable = m_lstQueryTableRows.Where(row => row.LastName.ToUpper().Contains(text));
                        dataGrid.DataContext = filterQueryTable;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
                    }
                    else
                    {
                        var filteredDefaultTable = m_lstdefaultTableRows.Where(row => row.LastName.ToUpper().Contains(text));
                        //if (filteredDefaultTable.Any())
                        //{
                        //    m_isQueryTableShown = true;
                        //    //m_lstQueryTableRows = new List<DefaultTableRow>(filteredDefaultTable);
                        //}

                        dataGrid.DataContext = filteredDefaultTable;
                        lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();

                    }


                }
                else if (m_alistView)
                {
                    var filteredAttendeeListTable = m_lstattendanceTableRows.Where(row => row.LastName.ToUpper().Contains(text));
                    dataGrid_prospect.DataContext = filteredAttendeeListTable;
                    lblAttendenceMetrics.Text = dataGrid_prospect.Items.Count.ToString();
                }




            }
            else //tx == ""
            {
               
                Enable_Filters();
                if (m_isQueryTableShown)
                {
                    Display_Query_Table(m_lstQueryTableRows.AsQueryable());

                }
                else
                {
                    Display_DefaultTable_in_Grid();
                }
            }


        }


        private void Add_Blackout_Dates(ref DatePicker dp_cal)
        {
            var dates = new List<DateTime?>();
            DateTime? date = dp_cal.DisplayDate;

            DateTime? startDate = date?.AddMonths(-10);
            DateTime? endDate = date?.AddMonths(10);

            for (var dt = startDate; dt <= endDate; dt = dt?.AddDays(1))
            {

                if (dt?.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(dt);
                }


            }
            foreach (DateTime d in dates)
            {
                dp_cal.BlackoutDates.Add(new CalendarDateRange(d, d));
            }
        }

        void Save_Changes(object sender, System.Windows.RoutedEventArgs e)
        {

            Cursor = Cursors.Wait;


            //  // save last change to list and check for any checked attendees  

            List<AttendanceTableRow> Checklist = new List<AttendanceTableRow>() { };


            Checklist = getListOfCheckedAttendees();
            // Mark each attendee that has a checkmark next to it as checked in the dbcontext
            foreach (AttendanceTableRow dr in Checklist)
            {

                Attendee queryAttendeeInContext = m_dbContext.Attendees.Local.SingleOrDefault(rec => rec.AttendeeId == dr.AttendeeId);
                queryAttendeeInContext.Checked = true;
            }

            //if there is no attendees checked then make sure all attendees in the dbcontext is not checked (ie checked = false)
            if (Checklist.Any() )
            {
                foreach (Attendee at in m_dbContext.Attendees)
                {
                    if (at.Checked == true)
                    {
                        at.Checked = false;
                    }
                }
            }

            // save change to db
            SaveActiveList();
            m_ActivityTreeChanged = false;
            Cursor = Cursors.Arrow;

        }

        bool isAttendeeModified()
        {

            bool isAttendedStatusChecked = false;

            // save dataGrid edits
            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
            {
                if (dr.IsModifiedrow)
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

        List<AttendanceTableRow> getListOfCheckedAttendees()
        {

            //end all edits and update the datagrid with changes
            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            dataGrid_prospect.UpdateLayout();

            List<AttendanceTableRow> lstOfCheckedAttendees = new List<AttendanceTableRow>() { };



            foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
            {
                if (dr.Attended == "1")
                {


                    lstOfCheckedAttendees.Add(dr);

                }
                
            }

            return lstOfCheckedAttendees;
        }

        private void DeleteRecordInDefaultTable(System.Collections.IList row_select)
        {

            // copy default table to m_defaultTableRowsCopy
          //  Copy_ContentsOfDefaultTable();

            int idx = 0;

            for (int i = 0; i <= row_select.Count -1;i++)
            {
                DefaultTableRow tr = (DefaultTableRow)row_select[i];
                tr.PropertyChanged -= DefaultTabledr_PropertyChanged;

                idx = m_lstdefaultTableRows.IndexOf(tr);


                if (tr.AttendanceList.Any() )
                {
                  m_dbContext.Attendance_Info.RemoveRange(tr.AttendanceList);
                }

                if (tr.ActivityList.Any())
                {
                   m_dbContext.Activities.RemoveRange(tr.ActivityList);
                }

                m_lstdefaultTableRows.RemoveAt(idx);
            }

        }


        private void DeleteRecordInAttendeeListTable(System.Collections.IList row_select)
        {

            int idx = 0;

           
            for (int j =0; j <= row_select.Count -1; j++)
            {
                int attendeeId = ((DefaultTableRow)row_select[j]).AttendeeId;
                var AttendanceRow = m_lstattendanceTableRows.SingleOrDefault(x => x.AttendeeId == attendeeId);

                AttendanceRow.PropertyChanged -= AttendanceTabledr_PropertyChanged;
                idx = m_lstattendanceTableRows.IndexOf(AttendanceRow);

                m_lstattendanceTableRows.RemoveAt(idx);
            }
            


        }

        private void DeleteRecord(object sender, System.Windows.RoutedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;

            System.Collections.IList selectedRows = dataGrid.SelectedItems;

            DeleteRecordWindow delrec_win = new DeleteRecordWindow(selectedRows);
            delrec_win.ShowDialog();
            DateTime? date_to_be_deleted = delrec_win.getDateToDelete;
            List<Attendance_Info> lstDeleteRecs = new List<Attendance_Info>() { };

            Cursor = Cursors.Wait;


            var default_row_selected = selectedRows.Cast<DefaultTableRow>();
            int idx = 0;

            if (date_to_be_deleted != null && delrec_win.getDeleteRecs == true)
            {
                //find all date records with the 'date_to_be_deleted' date in all attendee's attendance_info history

                for (int i = 0; i <= m_lstdefaultTableRows.Count -1; i++)
                {
                   m_lstdefaultTableRows[i].PropertyChanged -= DefaultTabledr_PropertyChanged;  // temporary de-register property changed event

                    var attinforec = m_lstdefaultTableRows[i].AttendanceList.SingleOrDefault(x => x.Date == date_to_be_deleted);
                   

                    if (attinforec != null)
                    {
                        idx = m_lstdefaultTableRows[i].AttendanceList.IndexOf(attinforec);
                        // remove attendance_info recs from the attendee's attendance_info history
                        m_lstdefaultTableRows[i].AttendanceList.RemoveAt(idx);

                        // get the next latest attended date
                        var latestAttRec = (from rec in m_lstdefaultTableRows[i].AttendanceList
                                            where rec.Status == "Attended" || rec.Status == "Responded"
                                            orderby rec.Date descending
                                            select rec).FirstOrDefault();

                        if (latestAttRec !=null)
                        {
                            m_lstdefaultTableRows[i].Church_Last_Attended = latestAttRec.DateString;
                            m_lstdefaultTableRows[i].ChurchStatus = latestAttRec.Status;
                        }
                        else
                        {
                            m_lstdefaultTableRows[i].ChurchStatus = "n/a";
                            m_lstdefaultTableRows[i].Church_Last_Attended = "n/a";
                            // attendee does not have any attendance records
                            if (!m_lstdefaultTableRows[i].AttendanceList.Any())
                            {
                                m_lstdefaultTableRows[i].ChurchStatus = "n/a";
                                m_lstdefaultTableRows[i].Church_Last_Attended = "n/a";
                            }
                        }

                        
                        lstDeleteRecs.Add(attinforec);
                        
                    }
                   
                } //end for loop
                // remove all attendance_info records from the datastructure
                if (lstDeleteRecs.Any() )
                {
                    m_dbContext.Attendance_Info.RemoveRange(lstDeleteRecs);
                    MessageBox.Show("Records successfully deleted!", "Records deleted...", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No records found!, No records were deleted", "No records deleted...", MessageBoxButton.OK, MessageBoxImage.Information);
                }


            }
            else
            {

               if (selectedRows.Count != 0 && delrec_win.getDeleteRecs == true)
                {
                    DeleteRecordInDefaultTable(selectedRows);
                    DeleteRecordInAttendeeListTable(selectedRows);
                    Display_DefaultTable_in_Grid();
                    MessageBox.Show("Records successfully deleted!", "Records deleted...", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
            }


          

            Cursor = Cursors.Arrow;

           


        }

        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {


            var grid = sender as DataGrid;



            if (grid.SelectedItem != null)
            {

                m_default_row_selected = (DefaultTableRow)grid.SelectedItem;
                m_default_row_selected.PropertyChanged += DefaultTabledr_PropertyChanged;

            }


            if (m_isQueryTableShown)
                btnDelete.IsEnabled = false;
            else
            {
                if (dataGrid.RowDetailsVisibilityMode != DataGridRowDetailsVisibilityMode.Visible)
                    btnDelete.IsEnabled = true;
            }
                



            
            




        


        }
      
        private int check_date_bounds(DateTime SelectedDate)
        {

            DateTime curdate = DateTime.Now;
            DateTime datelimit;
            List<DateTime> lstsundays = new List<DateTime>();
           
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
                if ((SelectedDate > datelimit) )
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
                var lastAttInfoRec = defaultTableRec.AttendanceList.SingleOrDefault(rec => rec.Date == m_alistDateSelected);

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
        private void ImportRows_Click(object sender, System.Windows.RoutedEventArgs e)
        {


            Cursor = Cursors.Wait;

            bool haschanges = false;

            string date = m_alistDateSelected?.ToString("MM-dd-yyyy");

          
            //DateTime? date_t;
            int dupID = 1;
            // add all attendee status and date to database
          

            List<Attendee> attendeeList = new List<Attendee>() { };
            List<Attendance_Info> attendanceList = new List<Attendance_Info>() { };

            //first pass through list and make sure everything looks good before making any changes to the db context

            if (m_alistdateIsValid)
            {

                //end all edits and update the datagrid with changes
                dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid_prospect.UpdateLayout();


                List<AttendanceTableRow> Checklist = new List<AttendanceTableRow>() { };

                // get checked attendees
                Checklist = getListOfCheckedAttendees();




              
                foreach (AttendanceTableRow dr in Checklist)
                {
                    // attendee is not a new attendee
                    if (!dr.IsNewrow)
                    {
                        int attid = dr.AttendeeId;


                        //check for duplicate attendance record
                        bool bcheckdupInfo = Check_for_dup_AttendeeInfo_inDbase(dr);
                        if (bcheckdupInfo)
                        {
                            MessageBox.Show("A record with the same date already exist in the database, choose a difference date.", "Duplicate date record found", MessageBoxButton.OK, MessageBoxImage.Stop);
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



                            // get Default Table Row with Attendee Id
                            var defaultTableRec = m_lstdefaultTableRows.SingleOrDefault(rec => rec.AttendeeId == attid);

                            if (defaultTableRec != null)
                            {
                                //select the record with the most recent date for this attendee
                                var lastAttInfoRec = (from rec in defaultTableRec.AttendanceList
                                                      orderby rec.Date
                                                      select rec).ToList().LastOrDefault();



                                if (lastAttInfoRec != null)
                                {


                                    // If the most recent record is a 'Follow-up' then this record is a 'Responded' Status else it is an 'Attended' status
                                    if (lastAttInfoRec.Status == "Follow-Up")
                                        newRecord.Status = "Responded";
                                    else
                                        newRecord.Status = "Attended";


                                }
                                else
                                {
                                    newRecord.Status = "Attended";

                                }



                                ////if attendance info rec do not already exist in dbContext attendance list, add it
                                var queryAttendeeInfoExist = defaultTableRec.AttendanceList.SingleOrDefault(rec => rec.Status == newRecord.Status && rec.Date == newRecord.Date);
                                if (queryAttendeeInfoExist == null)
                                {
                                    m_dbContext.Attendance_Info.Add(newRecord);
                                    if (!defaultTableRec.AttendanceList.Contains(newRecord) )
                                      m_default_row_selected.AttendanceList.Add(newRecord);
                                    




                                    //change 'Status_Last_Attended' and 'date last attended' column in default table row to reflect the 
                                    //new record's status
                                    defaultTableRec.ChurchStatus = newRecord.Status;
                                    defaultTableRec.Church_Last_Attended = newRecord.Date?.ToString("MM-dd-yyyy");
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

                        //check if database already contain an attendee with first name and last name, if so append lastname with _1
                        bool bcheckdup = Check_for_dup_Attendee_inDbase(dr);
                        if (bcheckdup)
                        {
                            MessageBox.Show("A record with the same date already exist in the database, choose a difference date.", "Duplicate date record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                            return;
                        }


                        bool berror = Row_error_checking(dr);
                        if (berror)
                        {
                            MessageBox.Show("Please correct errors for attendee, check if first name, last name, date or status is valid?", "Attendee Status", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        dynamic Defaultdr = new DefaultTableRow();




                        // new Attendee
                        newAttendeeRec.AttendeeId = m_NewAttendeeId;
                        newAttendeeRec.FirstName = dr.FirstName.ToString().Trim();
                        newAttendeeRec.LastName = dr.LastName.ToString().Trim();
                        newAttendeeRec.Checked = false;

                        string flname = newAttendeeRec.FirstName.ToUpper() + " " + newAttendeeRec.LastName.ToUpper();
                     
                        //new Attendee Info record
                        newAttInfoRec.AttendeeId = newAttendeeRec.AttendeeId; // m_NewAttendeeId;
                        newAttInfoRec.Date = m_alistDateSelected;
                        newAttInfoRec.Status = "Attended";
                        //newAttInfoRec.Attendee = newAttendeeRec;
                        //build row in DefaultTableRow
                        Defaultdr.AttendeeId = newAttendeeRec.AttendeeId;

                        Defaultdr.FirstName = newAttendeeRec.FirstName;
                        Defaultdr.LastName = newAttendeeRec.LastName;
                        Defaultdr.AttendanceList.Add(newAttInfoRec);

                        Defaultdr.ChurchStatus = newAttInfoRec.Status;
                        Defaultdr.Church_Last_Attended = newAttInfoRec.Date?.ToString("MM-dd-yyyy");
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
                    ClearAttendeeListStatus(); // set checked status to false for all attendees;

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
            if (atr.LastName == "" || atr.FirstName == "" || atr.DateString == "" /*||*/ /*atr.Attended == false */)
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

                atr.Attended = "";
                atr.IsModifiedrow = false;
                atr.IsNewrow = false;
            }
            foreach (Attendee rec in m_dbContext.Attendees.Local)
            {
                rec.Checked = false;
            }
            Display_AttendeeListTable_in_Grid();
        }

        private void UpdateAttendeeListTableWithDateFilter()
        {
            //end all edits and update the datagrid with changes
            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            dataGrid_prospect.UpdateLayout();


            string date;

            if (m_alistdateIsValid)
                date = m_alistDateSelected?.ToString("MM-dd-yyyy");
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

            cmbHeaderStatus.IsEnabled = true;
            m_ctbActivity.Enabled = true;
            dpChurchLastAttended.IsEnabled = true;
            dpHeaderActivityLastAttended.IsEnabled = true;

            m_ctbActivityProspect.Enabled = true;
            dpChurchLastAttendedPr.IsEnabled = true;
            dpHeaderActivityPr.IsEnabled = true;
            btnPanelAddActivity.IsEnabled = true;
            dpHeaderActivityPr.IsEnabled = true;
            btnPanelNewActivity.IsEnabled = true;


        }

        private void Uncheck_All_Filters()
        {
            //chkFollowup.IsChecked = false;
            //chkResponded.IsChecked = false;
            //chkAttended.IsChecked = false;
            //chkChurchDateFilter.IsChecked = false;
            //chkActivityFilter.IsChecked = false;
            //chkChurchStatusFilter.IsChecked = false;
            // DateCalendar.IsEnabled = false;

            // FIXME // trvActivities.IsEnabled = false;
        }



        private void chkChurchDateFiler_Checked(object sender, System.Windows.RoutedEventArgs e)
        {


            m_isFilterByDateChecked = true;


            m_isActivityfilterByDateChecked = false;
            //chkActivityDateFilter.IsChecked = false;
            // DateCalendar.IsEnabled = true;




        }


        public void e_SelectedNodeChanged(object sender, EventArgs e)
        {
          

          
                
        }

      
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            dpChurchLastAttended.DisplayDate = DateTime.Today;
            
            Add_Blackout_Dates(ref dpChurchLastAttended);
            Add_Blackout_Dates(ref dpChurchLastAttendedPr);
            Add_Blackout_Dates(ref dpHeaderActivityLastAttended);
            Add_Blackout_Dates(ref dpHeaderActivityPr);

            // ComboTreeBox 1 and 2////////////////////////////////////////////////////////////////////////////////////////
            m_ctbActivity.DroppedDown = false;
            m_ctbActivity.Location = new System.Drawing.Point(0, 0);
            m_ctbActivity.Name = "ctbActivityCheckboxes";
            m_ctbActivity.SelectedNode = null;
            m_ctbActivity.ShowCheckBoxes = true;
            m_ctbActivity.Size = new System.Drawing.Size(200, 19);
            m_ctbActivity.DrawWithVisualStyles = true;
            m_ctbActivity.Visible = true;
            m_ctbActivity.ExpandAll();
            m_ctbActivity.DropDownWidth = 350;
            m_ctbActivity.DropDownHeight = 350;
            
           

            m_ctbActivity.DropDownClosed += M_ctbActivity_DropDownClosed;
            //m_ctbActivity.KeyUp += M_ctbActivity_KeyUp;
            m_ctbActivity.DropDown += M_ctbActivity_DropDown;
          
           // m_ctbActivityProspect.DroppedDown = false;
            m_ctbActivityProspect.Location = new System.Drawing.Point(0, 0);
            m_ctbActivityProspect.Name = "ctbActivityPrCheckboxes";
            m_ctbActivityProspect.SelectedNode = null;
            m_ctbActivityProspect.ShowCheckBoxes = true;
            m_ctbActivityProspect.Size = new System.Drawing.Size(200, 19);
            m_ctbActivityProspect.DrawWithVisualStyles = true;
            m_ctbActivityProspect.Visible = true;
            m_ctbActivityProspect.ExpandAll();
            m_ctbActivityProspect.DropDownWidth = 350;
            m_ctbActivityProspect.DropDownHeight = 350;
           
            

            m_ctbActivityProspect.DropDownClosed += M_ctbActivityProspect_DropDownClosed;
            m_ctbActivityProspect.DropDown += M_ctbActivityProspect_DropDown;
            

            WindowsFormsHost host = new WindowsFormsHost();
            host.Child = m_ctbActivity;
          //  host.KeyUp += Host_KeyUp;
            spActivityHeader.Children.Add(host);

            WindowsFormsHost host2 = new WindowsFormsHost();
            host2.Child = m_ctbActivityProspect;
           //host2.MouseRightButtonUp += Host2_MouseRightButtonUp;
            spActivityHeaderPr.Children.Add(host2);

            ///////////////////////////////////////////////////////////////////////////////////////////////
            m_alistView = false;
            m_isActivityfilterByDateChecked = false;
            m_AttendanceView = true;
            btnSave.IsEnabled = false;
        

            SetTimer();




            btnNewRec.IsEnabled = false;
           //btnImportRecords.IsEnabled = false;
            btnGenerateFollowUps.IsEnabled = true;
          
          
            Uncheck_All_Filters();
            



            m_dateIsValid = false;

            // commit datagrid edits and return DataContext to show all records
            if (dataGrid.Columns.Any() )
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }

        
            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            m_loaded = true;

        }

        private void M_ctbActivityProspect_DropDown(object sender, System.EventArgs e)
        {
            m_ctbActivityProspect.ExpandAll();
        }

        private void M_ctbActivity_DropDown(object sender, System.EventArgs e)
        {
            
            m_ctbActivity.ExpandAll();

        }

        private void M_ctbActivity_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            //{
            //    m_isActivityChecked = false;
            //    m_ctbActivity.UncheckAll();
            //    BuildQuery_and_UpdateGrid();
            //}

        }

        private void M_ctbActivityProspect_DropDownClosed(object sender, System.EventArgs e)
        {
            ComboTreeBox ctb = sender as ComboTreeBox;
            IEnumerable<ComboTreeNode> chkNodes = ctb.CheckedNodes;
            if (chkNodes.Any())
            {
                ComboTreeNode firstNode = chkNodes.First();
               

                if (firstNode != null && firstNode.Parent != null && firstNode.Parent.Parent != null)
                {
                    if (m_currentSelected_ActivityPair == null)
                    {
                        m_currentSelected_ActivityPair = new ActivityPair();

                    }

                    m_currentSelected_ActivityPair.ChildTaskName = firstNode.Text;
                    m_currentSelected_ActivityPair.ParentTaskName = firstNode.Parent.Text;
                    m_currentSelected_ActivityPair.ActivityGroup = firstNode.Parent.Parent.Text;

                }
                else if (firstNode != null && firstNode.Parent != null && firstNode.Parent.Parent == null)
                {

                    if (m_currentSelected_ActivityPair == null)
                    {
                        m_currentSelected_ActivityPair = new ActivityPair();

                    }

                    m_currentSelected_ActivityPair.ChildTaskName = "";
                    m_currentSelected_ActivityPair.ParentTaskName = firstNode.Text;
                    m_currentSelected_ActivityPair.ActivityGroup = firstNode.Parent.Text;
                }
                else
                {
                    m_currentSelected_ActivityPair = null;
                    m_ctbActivityProspect.UncheckAll();
                    MessageBox.Show("Activity is not an activity, check activity","Select an activity",MessageBoxButton.OK,MessageBoxImage.Error);

                }




                m_isActivityChecked = true;

            }
        }

        private void M_ctbActivity_DropDownClosed(object sender, System.EventArgs e)
        {
            
            ComboTreeBox ctb = sender as ComboTreeBox;
            IEnumerable<ComboTreeNode> chkNodes = ctb.CheckedNodes;
            if (chkNodes.Any() )
            {
                ComboTreeNode firstNode = chkNodes.First();
                if (firstNode != null && firstNode.Parent != null && firstNode.Parent.Parent != null)
                {
                    if (m_currentSelected_ActivityPair == null)
                    {
                        m_currentSelected_ActivityPair = new ActivityPair();

                    }

                    m_currentSelected_ActivityPair.ChildTaskName = firstNode.Text;
                    m_currentSelected_ActivityPair.ParentTaskName = firstNode.Parent.Text;
                    m_currentSelected_ActivityPair.ActivityGroup = firstNode.Parent.Parent.Text;
                    m_isActivityChecked = true;

                    BuildQuery_and_UpdateGrid();
                }
                else
                    m_isActivityChecked = false;

            }
            else
            {
                // nothing is selected, user pressed escape
                m_isActivityChecked = false;

               
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

        private void btnChart_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ChartWindow wndCharts = new ChartWindow(ref m_dbContext);
            wndCharts.Show();

        }

        private void RibbonApplicationMenuItem_Click_About(object sender, System.Windows.RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow(m_version_string);
            about.ShowDialog();

        }

        private void RibbonApplicationMenuItem_Click_Exit(object sender, System.Windows.RoutedEventArgs e)
        {

            this.Close();
        }



        private void btnNewRec_Click(object sender, System.Windows.RoutedEventArgs e)
        {




            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            dataGrid_prospect.UpdateLayout();



            string strdate;

            if (m_alistdateIsValid)
                strdate = m_alistDateSelected?.ToString("MM-dd-yyyy");
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
                    DateString = strdate,
                    Attended = ""
                };


                m_lstattendanceTableRows.Insert(last_rowindex, newrow);



                dataGrid_prospect.DataContext = m_lstattendanceTableRows;
                dataGrid_prospect.Items.Refresh();

                // highlight new row
                dataGrid_prospect.Focus();
                dataGrid_prospect.SelectedIndex = m_lstattendanceTableRows.Count - 1;
                dataGrid_prospect.ScrollIntoView(dataGrid_prospect.Items[m_lstattendanceTableRows.Count - 1]);

            }





        }

    

        private void btnPrint_Click(object sender, System.Windows.RoutedEventArgs e)
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
          
                dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid.UpdateLayout();
                dataGrid_prospect.UpdateLayout();

            


            
                  

            
#if (init_db)
                if (m_dbContext.ChangeTracker.HasChanges() || m_ActivityTreeChanged)
                {



                    MessageBoxResult res = MessageBox.Show("Changes has been made but not saved to the database yet. Save Changes?", "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
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
        private void ShowFiltered_Or_DefaultTable()
        {
            //if (m_isFilterByDateChecked || m_isActivityfilterByDateChecked || m_isActivityFilterChecked ||
            //    m_isAttendedChecked || m_isFollowupChecked || m_isRespondedChecked)

            //{
            //    if (txtSearch.Text != "")
            //    {
            //        m_DataSet.Tables["QueryTable"].DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
            //        dataGrid.DataContext = m_DataSet.Tables["QueryTable"];
            //        dataGrid.IsReadOnly = true;
            //    }
            //    else
            //    {
            //        dataGrid.DataContext = m_DataSet.Tables["QueryTable"];
            //        dataGrid.IsReadOnly = true;
            //    }

            //}
            //else
            //{

            //    if (txtSearch.Text != "")
            //    {
            //        m_DataSet.Tables["DefaultTable"].DefaultView.RowFilter = "FirstLastName LIKE '%" + txtSearch.Text + "%'";
            //        dataGrid.DataContext = m_DataSet.Tables["DefaultTable"];
            //        dataGrid.CanUserDeleteRows = false;
            //        dataGrid.CanUserAddRows = false;
            //        dataGrid.IsReadOnly = false;

            //    }
            //    else
            //    {
            //        // (dataGrid.DataContext as DataTable).DefaultView.Sort = "[Last Name] ASC";
            //        Display_DefaultTable_in_Grid();
            //    }

            //}

            //if (dataGrid.Columns.Count > 1)
            //{
            //   // dataGrid.Columns[0].Visibility = Visibility.Hidden; //AttendeeId
            //   // dataGrid.Columns[1].Visibility = Visibility.Hidden; // FirstNameLastName
            //}
        }
        private void SaveActiveList()
        {

            Save_ChurchActivities_To_XMLFile();
            // save contents to database
            m_dbContext.SaveChanges();
         

        }

        private void btnGenerateFollowUps_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            FollowUpWindow fw = new FollowUpWindow(_followUpWeeks);

            fw.ShowDialog();

            if (int.Parse(fw.GetFollowUpWeeks) != 0)
            {
                _followUpWeeks = fw.GetFollowUpWeeks;

                MessageBoxResult result = MessageBox.Show($"Are you sure you want to generate follow-Ups for every {_followUpWeeks } weeks now?", "Generate Follow-Up", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Cursor = Cursors.Wait;

                    bool generate_one = GenerateDBFollowUps(_followUpWeeks);

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
        }




        private void chkActivityFilter_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            m_isActivityFilterChecked = true;
            // trvActivities.IsEnabled = true; FIX ME


        }


        private void Copy_ContentsOfDefaultTable()
        {
            if (m_lstdefaultTableRowsCopy.Any())
            {
                m_lstdefaultTableRowsCopy.Clear();
            }
            // solution to not change the original list object when changing the copy
            foreach (var row in m_lstdefaultTableRows)
            {
                DefaultTableRow rowCopy = new DefaultTableRow() { };
                rowCopy.FirstName = row.FirstName;
                rowCopy.AttendeeId = row.AttendeeId;
                rowCopy.FirstName = row.FirstName;
                rowCopy.LastName = row.  LastName;
                rowCopy.Activity = row.Activity;
                rowCopy.Church_Last_Attended = row.Church_Last_Attended;
                rowCopy.Activity_Last_Attended = row.Activity_Last_Attended; ;
                rowCopy.ChurchStatus = row.ChurchStatus;
                rowCopy.ActivityList = row.ActivityList;
                rowCopy.AttendanceList = row.AttendanceList;

                m_lstdefaultTableRowsCopy.Add(rowCopy);

            }
        }

        private void BuildQuery_and_UpdateGrid()
        {

          



            string strChurchStatus = "";
            bool bChurchStatusAttended = false;
            string strChurchStatusResponded = "";


            bool ChurchDate = m_dateIsValid && m_DateSelected != null;
            bool Status = (m_isFollowupChecked || m_isAttendedChecked || m_isRespondedChecked);
            bool Activity = m_isActivityChecked && m_currentSelected_ActivityPair != null;
            bool ActivityDate = m_ActivityDateSelected != null;



            // display main View when no filter options are checked
            if (!ChurchDate && !Status && !Activity && !ActivityDate)
            {
                Display_DefaultTable_in_Grid();
                btnDelete.IsEnabled = true;
                btnGenerateFollowUps.IsEnabled = true;

                return;
            }
            else
            {
                Copy_ContentsOfDefaultTable();
            }



            if (m_lstQueryTableRows.Any())
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
                if (strChurchStatus == "Attended" || strChurchStatus == "Responded")
                {
                    bChurchStatusAttended = true;
                    strChurchStatusResponded = "Responded";
                }


                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {


                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any()  && m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {
                        IEnumerable<Attendance_Info> query_status;
                        IEnumerable<ActivityPair> query_activity;

                        if (bChurchStatusAttended)
                        {
                            query_status = from row in m_lstdefaultTableRowsCopy[i].AttendanceList
                                           where row.Status == strChurchStatus || row.Status == strChurchStatusResponded
                                           select row;

                            query_activity = from row_activity in m_lstdefaultTableRowsCopy[i].ActivityList
                                             where row_activity.DateString == strActivityDate &&
                                                   row_activity.ActivityGroup == ActivityGroup &&
                                                   row_activity.ParentTaskName == ActivityParentName &&
                                                   row_activity.ChildTaskName == ActivityChildName
                                             select row_activity;
                        }
                        else
                        {
                            query_status = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0 and ActivityGroup == @1 and ParentTaskName == @2 and ChildTaskName == @3", strActivityDate, ActivityGroup, ActivityParentName, ActivityChildName);
                        }


                        if (query_status.Count() != 0 && query_activity.Count() != 0)
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
                string strChurchDate = m_DateSelected?.ToString("MM-dd-yyyy");

                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";

                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;

                if (strChurchStatus == "Attended" || strChurchStatus == "Responded")
                {
                    bChurchStatusAttended = true;
                    strChurchStatusResponded = "Responded";
                }

                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any()  && m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {
                        IQueryable<Attendance_Info> query_dateandstatus;
                        IQueryable<ActivityPair> query_activity;
                        if (bChurchStatusAttended)
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and (Status == @1 or Status = @2 )", strChurchDate, strChurchStatus, strChurchStatusResponded);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName);
                        }
                        else
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and Status == @1", strChurchDate, strChurchStatus);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName);
                        }

                        if (query_dateandstatus.Count() != 0 && query_activity.Count() != 0)
                        {
                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;
                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if (ChurchDate && Activity) //ChurchDate and Activity
            {
                string strChurchDate = m_DateSelected?.ToString("MM-dd-yyyy");
                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;

                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any()  && m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {
                        var query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0", strChurchDate);
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName);

                        if (query_dateandstatus.Count() != 0 && query_activity.Count() != 0)
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


                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {

                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0 and ActivityGroup == @1 and ParentTaskName == @2 and ChildTaskName == @3", strActivityDate, ActivityGroup, ActivityParentName, ActivityChildName);

                        if (query_activity.Count() != 0)
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

                if (strChurchStatus == "Attended" || strChurchStatus == "Responded")
                {
                    bChurchStatusAttended = true;
                    strChurchStatusResponded = "Responded";
                }

                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any()  && m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {
                        IQueryable<Attendance_Info> query_dateandstatus;
                        IQueryable<ActivityPair> query_activity;

                        if (bChurchStatusAttended)
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0 or Status = @1", strChurchStatus, strChurchStatusResponded);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName);
                        }
                        else
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName);
                        }

                        if (query_dateandstatus.Count() != 0 && query_activity.Count() != 0)
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
                if (strChurchStatus == "Attended" || strChurchStatus == "Responded")
                {
                    bChurchStatusAttended = true;
                    strChurchStatusResponded = "Responded";
                }


                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {


                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any()  && m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {
                        IQueryable<Attendance_Info> query_status;
                        IQueryable<ActivityPair> query_activity;

                        if (bChurchStatusAttended)
                        {
                            query_status = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0 or Status == @1", strChurchStatus, strChurchStatusResponded);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0", strActivityDate);
                        }
                        else
                        {
                            query_status = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0", strActivityDate);
                        }

                        if (query_status.Count() != 0 && query_activity.Count() != 0)
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




                string strChurchDate = m_DateSelected?.ToString("MM-dd-yyyy");


                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";
                if (strChurchStatus == "Attended" || strChurchStatus == "Responded")
                {
                    bChurchStatusAttended = true;
                    strChurchStatusResponded = "Responded";
                }
                //strChurchStatus = querystring.

                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {


                    IQueryable<Attendance_Info> query_row;
                    if (bChurchStatusAttended)
                    {
                        query_row = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and (Status == @1 or Status == @2)", strChurchDate, strChurchStatus, strChurchStatusResponded);
                    }
                    else
                    {
                        query_row = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and Status == @1", strChurchDate, strChurchStatus);
                    }





                    if (query_row.Count() != 0)
                    {
                        m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                        m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;

                        m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                    }

                    //}
                }

            }
            else if (Status) //Status
            {

                strChurchStatus += (m_isFollowupChecked) ? "Follow-Up" : "";
                strChurchStatus += (m_isAttendedChecked) ? "Attended" : "";
                strChurchStatus += (m_isRespondedChecked) ? "Responded" : "";
                if (strChurchStatus == "Attended" || strChurchStatus == "Responded")
                {
                    bChurchStatusAttended = true;
                    strChurchStatusResponded = "Responded";
                }
                //strChurchStatus = querystring.

                if (bChurchStatusAttended)
                {
                    var linq_query = m_lstdefaultTableRowsCopy.AsQueryable().Where("ChurchStatus == @0 or ChurchStatus == @1", strChurchStatus, strChurchStatusResponded);
                    if (linq_query != null)
                    {
                        m_lstQueryTableRows.AddRange(linq_query);


                    }
                }

                else
                {
                    if (strChurchStatus == "Follow-Up" && !m_dateIsValid)
                    {

                        // Find all instances of the latest Follow-Up generated for each attendee from attendee's attendance list
                        // and add the attendee row to the queries table rows
                        for (int i = 0; i < m_lstdefaultTableRowsCopy.Count - 1; i++)
                        {


                            var query_row = (from row in m_lstdefaultTableRowsCopy[i].AttendanceList
                                             where row.Status == strChurchStatus
                                             orderby row.Date
                                             select row).ToArray().LastOrDefault();


                            if (query_row != null)
                            {
                                m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);
                                m_lstQueryTableRows.Last().ChurchStatus = strChurchStatus;
                                m_lstQueryTableRows.Last().Church_Last_Attended = query_row.DateString;
                            }


                        }



                    }

                }

            }
            else if (ChurchDate) //DATE
            {
                string strChurchDate = m_DateSelected?.ToString("MM-dd-yyyy");

                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    var query_row = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0", strChurchDate);



                    if (query_row.Any())
                    {

                        m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;

                        m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                    }
                }

            }
            else if (ActivityDate) //Activity Date
            {
                string strActivityDate = m_ActivityDateSelected?.ToString("MM-dd-yyyy");




                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {


                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {

                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0", strActivityDate);

                        if (query_activity.Any() )
                        {

                            m_lstdefaultTableRowsCopy[i].Activity_Last_Attended = strActivityDate;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if (Activity) //Activity
            {
                string strActivity = m_currentSelected_ActivityPair.ToString();

                string ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                string ActivityParentName = m_currentSelected_ActivityPair.ParentTaskName;
                string ActivityChildName = m_currentSelected_ActivityPair.ChildTaskName;



                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {

                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityGroup == @0 and ParentTaskName == @1 and ChildTaskName == @2", ActivityGroup, ActivityParentName, ActivityChildName);

                        if (query_activity.Any() )
                        {


                            m_lstdefaultTableRowsCopy[i].Activity = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }


            if (m_lstQueryTableRows.Any())
            {
                Display_Query_Table();
            }
            else
            {
                dataGrid.DataContext = "";
                lblAttendenceMetrics.Text = "0";
            }









            Cursor = Cursors.Arrow;
        }

        private void Display_Query_Table(IQueryable<DefaultTableRow> resultTable)
        {
            dataGrid.DataContext = resultTable.OrderBy(rec => rec.LastName).ToList();
            dataGrid.Items.Refresh();
            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            dataGrid.IsReadOnly = true;
            m_isQueryTableShown = true;
            btnGenerateFollowUps.IsEnabled = false;
            btnDelete.IsEnabled = false;



        }
        private void Display_Query_Table()
        {
            dataGrid.DataContext = m_lstQueryTableRows.OrderBy(rec => rec.LastName).ToList();
            dataGrid.Items.Refresh();
            lblAttendenceMetrics.Text = dataGrid.Items.Count.ToString();
            dataGrid.IsReadOnly = true;
            m_isQueryTableShown = true;
            btnGenerateFollowUps.IsEnabled = false;
            btnDelete.IsEnabled = false;


        }
    

        private void GridsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Cursor = Cursors.Wait;


            if (dataGrid_prospect.Columns.Any() )
            {
                dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
                dataGrid_prospect.UpdateLayout();

            }

           // commit datagrid edits and return DataContext to show all records
            if (dataGrid.Columns.Any() )
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
                    

                    if (m_lstattendanceTableRows != null)
                    {
                        //delete last row if no new attendee firstname or lastname entered
                        if (m_lstattendanceTableRows.Any() )
                        {
                            if (m_lstattendanceTableRows.LastOrDefault().FirstName == "" ||
                            m_lstattendanceTableRows.LastOrDefault().LastName == "")
                            {
                                m_lstattendanceTableRows.RemoveAt(m_lstattendanceTableRows.Count - 1);
                            }
                        }
                        
                    }



                   

                    btnNewRec.IsEnabled = false;
                    btnDelete.IsEnabled = true;

                    if (m_isAttendedChecked)
                        cmbHeaderStatus.SelectedIndex = 1;
                    else if (m_isFollowupChecked)
                        cmbHeaderStatus.SelectedIndex = 2;
                    else if (m_isRespondedChecked)
                        cmbHeaderStatus.SelectedIndex = 3;
                    else
                        cmbHeaderStatus.SelectedIndex = 0;

                    if (m_isQueryTableShown)
                        Display_Query_Table();
                    else
                       Display_DefaultTable_in_Grid();

                }
            }
            if ((GridsTabControl.SelectedItem as TabItem).Name == "ProspectListTab")
            {

                // only do this once, if the page is loaded already no need to run throught this code again
                if (!m_alistView)
                {
                    m_alistView = true;
                    m_AttendanceView = false;
                    

                    m_currentSelected_ActivityPair = null;
                    m_ActivityDateSelected = null;
                    btnPanelAddActivity.IsEnabled = false;
                    btnImport.IsEnabled = false;

                    Enable_Filters();

                   
                    


                    //btnImportRecords.IsEnabled = true;
                    //btnImportRecords.Content = "Update Changes";

                    btnNewRec.IsEnabled = true;
                    btnDelete.IsEnabled = false;


                    if (m_isQueryTableShown)
                        Display_Query_Table();
                    else
                        Display_AttendeeListTable_in_Grid();





                }
            }

            Cursor = Cursors.Arrow;

        }



        private void btnDeleteAttendanceInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            IEnumerable<Attendance_Info> AttendanceInfoRow_select = m_AttendeeInfo_grid.SelectedItems.Cast<Attendance_Info>();
            IEnumerable<ActivityPair> ActivityRow_select = m_Activity_grid.SelectedItems.Cast<ActivityPair>();

            if (AttendanceInfoRow_select.Any())
            {

                DeleteRecordFromAttendanceInfoTable(AttendanceInfoRow_select);




                MessageBox.Show("Attendance record removed successfully.\n\nChanges has not been saved to the database until the Save button is clicked.", "Records removed", MessageBoxButton.OK, MessageBoxImage.None);

            }
            else if (ActivityRow_select.Any())
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

            var latestAttRec = (from last_date in m_default_row_selected.AttendanceList
                                where last_date.Status == "Attended" || last_date.Status == "Responded"
                                orderby last_date.Date descending
                                select last_date).FirstOrDefault();

            //update default row with new latest activity
            if (latestAttRec != null)
            {
                m_default_row_selected.Activity_Last_Attended = latestAttRec.DateString;
                m_default_row_selected.Activity = latestAttRec.ToString();
            }
            

            if (!m_default_row_selected.ActivityList.Any())
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
                                    where last_date.Status == "Attended" || last_date.Status == "Responded"
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

                //var queryAttlist = m_lstdefaultTableRows.SingleOrDefault(x => x.AttendeeId == m_datarow_AttendeeId);

                m_Activity_grid.DataContext = m_default_row_selected.ActivityList.OrderByDescending(x => x.Date).ToList();
                m_Activity_grid.Items.Refresh();

                if (m_Activity_grid.Columns.Any())
                {
                    m_Activity_grid.Columns[0].Visibility = Visibility.Hidden; // AttendeeId
                    m_Activity_grid.Columns[2].Width = 300; // Activity column

                }
            }


        }


        private void Display_AttendanceList_in_Grid()
        {
            if (m_AttendeeInfo_grid != null)
            {
               
                m_AttendeeInfo_grid.DataContext = m_default_row_selected.AttendanceList.OrderByDescending(x => x.Date).ToList();
                m_AttendeeInfo_grid.Items.Refresh();

                //hide grid columns
                if (m_AttendeeInfo_grid.Columns.Any())
                {
                    m_AttendeeInfo_grid.Columns[0].Visibility = Visibility.Hidden; //AttedeeId

                }

            }




        }


        private void btnExpandHistory_Click(object sender, System.Windows.RoutedEventArgs e)
        {


            if (dataGrid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed)
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;

                Disable_Filters();
                btnDelete.IsEnabled = false;

                btnGenerateFollowUps.IsEnabled = false;

                Display_AttendanceList_in_Grid();
                Display_ActivityList_in_Grid();
            }
            else
            {
                dataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
                Enable_Filters();
                btnDelete.IsEnabled = true;

                btnGenerateFollowUps.IsEnabled = true;



            }



        }


        private void dataGrid_LoadingRowDetails(object sender, DataGridRowDetailsEventArgs e)
        {

            Cursor = Cursors.Wait;


            // get GrdAttendee_InfoList element within the DataTemplate
           
           if (e.DetailsElement != null)
           {
                m_AttendeeInfo_grid = e.DetailsElement.FindName("GrdAttendee_InfoList") as DataGrid;
                // get GrdAttendee_ActivityList element within the DataTemplate
                m_Activity_grid = e.DetailsElement.FindName("GrdAttendee_ActivityList") as DataGrid;
            }
            




           



            Display_ActivityList_in_Grid();
            Display_AttendanceList_in_Grid();

            btnDelete.IsEnabled = false;
          


            Cursor = Cursors.Arrow;
        }

   


        private void dataGrid_prospect_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var dg = sender as DataGrid;

            if (dg.SelectedItems != null)
            {

                m_MultiAttendanceRow_Selected = dg.SelectedItems;

            }
            
        }

      

        private void btnPanelAddActivity_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

           
            if (m_currentSelected_ActivityPair != null)
            {
                foreach (AttendanceTableRow dr in m_MultiAttendanceRow_Selected)
                {

                    ActivityPair new_ap = new ActivityPair();

                    new_ap.Date = m_ActivityDateSelectedPr;
                    new_ap.AttendeeId = dr.AttendeeId;
                    new_ap.ActivityGroup = m_currentSelected_ActivityPair.ActivityGroup;
                    new_ap.ParentTaskName = m_currentSelected_ActivityPair.ParentTaskName;
                    new_ap.ChildTaskName = m_currentSelected_ActivityPair.ChildTaskName;


                    // Find defaultrow that correspond to the attendance row attendeeID
                    m_default_row_selected = m_lstdefaultTableRows.SingleOrDefault(x => x.AttendeeId == dr.AttendeeId);

                    //if activity do not already exist in dbContext then add to attendee's activitylist
                    var queryifActivityExistList = m_default_row_selected.ActivityList.SingleOrDefault(rec => rec.ToString() == m_currentSelected_ActivityPair.ToString() && rec.Date == m_currentSelected_ActivityPair.Date);
                    if (queryifActivityExistList == null)
                    {

                        m_dbContext.Activities.Add(new_ap);
                        if (!m_default_row_selected.ActivityList.Contains(new_ap) ) // if new activity is not added then add it to the activity list
                             m_default_row_selected.ActivityList.Add(new_ap);
                        


                        var lastActivity = (from rec in m_default_row_selected.ActivityList
                                            orderby rec.Date descending
                                            select rec).ToList().FirstOrDefault();

                        if (lastActivity != null)
                        {
                            m_default_row_selected.Activity = lastActivity.ToString();
                            m_default_row_selected.Activity_Last_Attended = lastActivity.DateString;
                        }
                        else
                        {
                            m_default_row_selected.Activity = new_ap.ToString();
                            m_default_row_selected.Activity_Last_Attended = new_ap.DateString;
                        }

                    }
                    else
                    {
                        MessageBox.Show("Activity already exist for this attendee, please choose another activity or date.", "Duplicate activity", MessageBoxButton.OK, MessageBoxImage.Error);
                        Cursor = Cursors.Arrow;
                        return;
                    }

                }


                m_ctbActivityProspect.UncheckAll();
                dpHeaderActivityPr.Text = "";
                btnPanelAddActivity.IsEnabled = false;
                m_currentSelected_ActivityPair = null;
                m_ActivityDateSelectedPr = null;

                MessageBox.Show("Activity successfully added to selected attendee(s) profile", "Activity Added", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                
            }

            Cursor = Cursors.Arrow;
        }

        private void cmbAttendanceInfo_LayoutUpdated(object sender, System.EventArgs e)
        {

        }



        private void MenuItem_AddNewActivity_Click(object sender, System.Windows.RoutedEventArgs e)
        {
          



        }

        private void MenuItem_DeleteActivity_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //Cursor = Cursors.Wait;
            //FIX ME
            //if (m_currentSelected_ActivityPair !=null)
            //{
            //    string childtask = m_currentSelected_ActivityPair.ChildTaskName;
            //    string activityGroup = m_currentSelected_ActivityPair.ActivityGroup;
            //    string parenttask = m_currentSelected_ActivityPair.ParentTaskName;

            //    var a_group = m_lstActivities.SingleOrDefault(at => at.ActivityName == activityGroup);
            //    var task = a_group.lstActivityTasks.SingleOrDefault(at => at.TaskName == parenttask);
            //    int task_idx = a_group.lstActivityTasks.IndexOf(task);

            //    ActivityTask subtask = null;

            //   if (task != null)
            //    {
            //       subtask = task.lstsubTasks.SingleOrDefault(st => st.TaskName == childtask);
            //    }






            //    // user selected a task with child tasks

            //    if (activityGroup != "" && parenttask != "" && childtask != "")
            //    {

            //        if (subtask != null)
            //        {

            //            a_group.lstActivityTasks[task_idx].lstsubTasks.Remove(subtask);
            //        }
            //    }
            //    // user selected a task with no child tasks
            //    else if (activityGroup != "" && parenttask != "" && childtask == "") 
            //    {
            //        if (task != null)
            //        {
            //            a_group.lstActivityTasks.Remove(task);
            //        }
            //    }
            //    //user selected a group
            //    else if (activityGroup != "" && parenttask == "") 
            //    {
            //        if (a_group != null)
            //        {
            //            m_lstActivities.Remove(a_group);
            //        }

            //    }




            //    ClearTreeView();

            //    m_newlstActivitiesCount = m_lstActivitiesCount + 1;
            //    trvActivities.Items.Refresh();
            //    Cursor = Cursors.Arrow;



            //}
            //else
            //{
            //    MessageBox.Show("Must select an activity first.", "Delete Activity", MessageBoxButton.OK, MessageBoxImage.Stop);
            //    Cursor = Cursors.Arrow;
            //}
        }



        private void MenuItem_DeleteActivityGroup_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //Cursor = Cursors.Wait;


            if (m_currentSelected_ActivityPair != null)
            {

                //FIX ME
                //var deleteActivityGroup = m_lstActivityHeaders[idx].Groups.SingleOrDefault(ag => ag.ActivityName == m_ActivityName);
                //if (deleteActivityGroup != null)
                //{
                //    m_lstActivityHeaders[idx].Groups.Remove(deleteActivityGroup);
                //    m_newlstActivitiesCount = m_lstActivitiesCount + 1;
                //    trvActivities.Items.Refresh();
                //}
                //Cursor = Cursors.Arrow;
            }
            else
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("Must select an activity to delete first.", "Delete Activity", MessageBoxButton.OK, MessageBoxImage.Stop);

            }


        }


        private void BtnPanelNewActivity_Click(object sender, System.Windows.RoutedEventArgs e)
        {

          



                WndAddGroup AddgroupWin = new WndAddGroup(m_lstActivityHeaders);
                AddgroupWin.ShowDialog();
               
                m_ActivityTreeChanged = AddgroupWin.GetTreeChanged;

                if (m_ActivityTreeChanged) // tree has changed
                {
                    var new_tree = AddgroupWin.getTree;
                    LoadNewComboTree(new_tree); //Load the combo tree boxes with the new tree
                    Convert_and_SaveNewTreeToActivityHeardersTree(new_tree); //convert and save the new tree to the format m_lstActivityHeaders
                    
                }
           

        }

        private void BtnExecQuery_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            BuildQuery_and_UpdateGrid();
            Cursor = Cursors.Arrow;

        }




        private void BtnAddColumn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddColumnWindow AddColumnWindow = new AddColumnWindow();

            AddColumnWindow.ShowDialog();

            if (AddColumnWindow.GetColumnNames.Count > 0)
            {
                List<string> lst = AddColumnWindow.GetColumnNames;
            }
                

        }

        private void BtnClearQuery_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Display_DefaultTable_in_Grid();
            m_isQueryTableShown = false;

            btnDelete.IsEnabled = true;
            btnGenerateFollowUps.IsEnabled = true;

        }






        private void DataGridCell_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            var uiElement = e.OriginalSource as UIElement;



            DataGridCell cell = sender as DataGridCell;
            if (cell != null)
            {

                if (cell.IsEditing || cell.IsReadOnly)
                {
                    if (e.Key == Key.Down)
                    {


                        e.Handled = true;
                        uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));


                    }
                    else if (e.Key == Key.Up)
                    {


                        e.Handled = true;
                        uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));

                    }

                }


            }

        }

        private void MenuItem_AddNewColumn_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void MenuItem_DeleteColumn_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void TxtHeaderFirstName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {

                if (txtHeaderFirstName.Text == "")
                {
                    if (m_isQueryTableShown)
                    {
                        m_isFirstNamefiltered = false;
                        Display_Query_Table(m_lstQueryTableRows.AsQueryable());
                    }
                    else
                    {
                        Display_DefaultTable_in_Grid();
                    }
                }


            }
            else if (e.Key == Key.Escape)
            {
                txtHeaderFirstName.Text = "";
                m_isFirstNamefiltered = false;
                if (m_isQueryTableShown)
                {
                    Display_Query_Table();
                }
                else
                {
                    Display_DefaultTable_in_Grid();
                }

            }
        }

        private void TxtHeaderLastName_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Escape)
            {
                txtHeaderLastName.Text = "";
                m_isLastNamefiltered = false;
                if (m_isQueryTableShown)
                {
                    Display_Query_Table();
                }
                else
                {
                    Display_DefaultTable_in_Grid();
                }

            }
        }


        private void Dp_DateValidation(object sender, DatePickerDateValidationErrorEventArgs e)
        {
            DateTime newDate;
            var dp = sender as DatePicker;

            if (DateTime.TryParse(e.Text, out newDate))
            {
                if (dp.BlackoutDates.Contains(newDate))
                {
                    MessageBox.Show("The date you entered is not valid.");
                }
            }
            else
            {
                e.ThrowException = true; ;
            }
        }
        private void dpChurchLastAttended_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {


            var calender = sender as DatePicker;
            if (calender.SelectedDate != null)
            {
                DateTime date = calender.SelectedDate.Value;
                

                int ret_error = check_date_bounds(date);

                if (ret_error == 1)
                {
                    dpChurchLastAttended.Text = "";
                    return;
                }
                    

                if (date.DayOfWeek == DayOfWeek.Sunday)
                {

                    m_dateIsValid = true;
                    m_DateSelected = date;
                    m_isFilterByDateChecked = true;
                    BuildQuery_and_UpdateGrid();



                }
                else
                {
                    m_dateIsValid = false;

                }
            }

        }



        private void CmbHeaderStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmbStatus = (ComboBox)sender;

            int status_idx = cmbStatus.SelectedIndex;

            switch (status_idx)
            {

                case 1:
                    //Attended filter
                    m_isAttendedChecked = true;
                    m_isFollowupChecked = false;
                    m_isRespondedChecked = false;


                    BuildQuery_and_UpdateGrid();


                    break;
                case 2:
                    // Follow-Up filter
                    m_isAttendedChecked = false;
                    m_isFollowupChecked = true;
                    m_isRespondedChecked = false;

                    BuildQuery_and_UpdateGrid();

                    break;
                case 3:
                    //Responded filter
                    m_isAttendedChecked = false;
                    m_isFollowupChecked = false;
                    m_isRespondedChecked = true;

                    BuildQuery_and_UpdateGrid();

                    break;

                default: // Status no filter
                    m_isAttendedChecked = false;
                    m_isFollowupChecked = false;
                    m_isRespondedChecked = false;



                    BuildQuery_and_UpdateGrid();

                    break;


            }




        }

        private void Uncheck_all_status()
        {
            m_isAttendedChecked = false;
            m_isFollowupChecked = false;
            m_isRespondedChecked = false;

        }
        private void CmbHeaderStatus_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Uncheck_all_status();
                cmbHeaderStatus.SelectedIndex = 0; //Set combobox to display "Status"

                BuildQuery_and_UpdateGrid();

            }
        }


        private void AddColumns()
        {

            







        }

        private void ConvertListToDataTable()
        {
            DataTable defaultDT = new DataTable("defaultDT");
            DataTable attendanceDT = new DataTable("attendanceDT");
            DataTable activityDT = new DataTable("activityDT");

            defaultDT.Columns.Add(new DataColumn("AttendeeID", typeof(int)));
            defaultDT.Columns.Add(new DataColumn("Lastname"));
            defaultDT.Columns.Add(new DataColumn("Firstname"));
            defaultDT.Columns.Add(new DataColumn("Status"));
            defaultDT.Columns.Add(new DataColumn("Church date"));
            defaultDT.Columns.Add(new DataColumn("Activity"));
            defaultDT.Columns.Add(new DataColumn("Activity date"));

            attendanceDT.Columns.Add(new DataColumn("Attendance_InfoId", typeof(int)));
            attendanceDT.Columns.Add(new DataColumn("AttendeeId", typeof(int)));
            attendanceDT.Columns.Add(new DataColumn("Date"));
            attendanceDT.Columns.Add(new DataColumn("Status"));

            activityDT.Columns.Add(new DataColumn("ActivityPairId", typeof(int)));
            activityDT.Columns.Add(new DataColumn("AttendeeId", typeof(int)));
            activityDT.Columns.Add(new DataColumn("Activity"));
            activityDT.Columns.Add(new DataColumn("Date"));



            try
            {
                // Default DataTable
                foreach (DefaultTableRow row in m_lstdefaultTableRows)
                {
                    DataRow dr = defaultDT.NewRow();

                    dr["AttendeeID"] = row.AttendeeId;
                    dr["Lastname"] = row.LastName;
                    dr["Firstname"] = row.FirstName;
                    dr["Status"] = row.ChurchStatus;
                    dr["Church date"] = row.Church_Last_Attended;
                    dr["Activity"] = row.Activity;
                    dr["Activity date"] = row.Activity_Last_Attended;

                    defaultDT.Rows.Add(dr);

                    //AttendanceList
                    foreach (Attendance_Info attrow in row.AttendanceList)
                    {
                        DataRow attdr = attendanceDT.NewRow();

                        attdr["Attendance_InfoId"] = attrow.Attendance_InfoId;
                        attdr["AttendeeId"] = attrow.AttendeeId;
                        attdr["Date"] = attrow.DateString;
                        attdr["Status"] = attrow.Status;


                        attendanceDT.Rows.Add(attdr);
                    }

                    //ActivityList
                    foreach (ActivityPair activityrow in row.ActivityList)
                    {
                        DataRow activitydr = activityDT.NewRow();

                        activitydr["ActivityPairId"] = activityrow.ActivityPairId;
                        activitydr["AttendeeId"] = activityrow.AttendeeId;
                        activitydr["Activity"] = activityrow.ToString();
                        activitydr["Date"] = activityrow.DateString;

                        activityDT.Rows.Add(activitydr);
                    }

                }

                defaultDT.AcceptChanges();
                attendanceDT.AcceptChanges();
                activityDT.AcceptChanges();


                m_DataSet.Tables.Add(defaultDT);
                m_DataSet.Tables.Add(attendanceDT);
                m_DataSet.Tables.Add(activityDT);


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception occurred when performing database initialization { ex}!\n");
            }



        }
        private void AddColumns_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            AddColumns();


            //< DataGridTextColumn Binding = "{Binding LastName}" >

            //                     < DataGridTextColumn.HeaderStyle >

            //                         < Style TargetType = "{x:Type DataGridColumnHeader}" >

            //                              < Setter Property = "HorizontalContentAlignment" Value = "Stretch" />

            //                          </ Style >

            //                         </ DataGridTextColumn.HeaderStyle >

            //                         < DataGridTextColumn.Header >

            //                             < Grid >

            //                                 < Grid.ColumnDefinitions >

            //                                     < ColumnDefinition Width = "*" />

            //                                      < ColumnDefinition Width = "19" />

            //                                   </ Grid.ColumnDefinitions >


            //                                   < TextBox Grid.Column = "0"  Name = "txtHeaderLastName" Text = "Lastname"
            //                                     GotFocus = "TxtHeaderLastName_GotFocus"
            //                                     LostFocus = "TxtHeaderLastName_LostFocus"
            //                                     KeyUp = "TxtHeaderLastName_KeyUp"
            //                                     PreviewMouseLeftButtonUp = "TxtHeaderLastName_PreviewMouseLeftButtonUp"
            //                                     TextChanged = "txtSearch_TextChanged_LName" />
            //                            < Image Grid.Column = "1"  Name = "imgLastNameHeader" Source = "Resources\Search.ico" Height = "19" Width = "18" />

            //                                 </ Grid >

            //                             </ DataGridTextColumn.Header >

            //                         </ DataGridTextColumn >


        }

        private void dpChurchLastAttended_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var dp = sender as DatePicker;

            if (e.Key == Key.Escape)
            {
                if (dp.Text != "")
                {
                    dp.Text = "";
                    m_dateIsValid = false;
                    m_DateSelected = null;
                    BuildQuery_and_UpdateGrid();
                }

            }
        }


        private void dpHeaderActivityLastAttended_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as DatePicker;
            if (calender.SelectedDate != null)
            {
                DateTime date = calender.SelectedDate.Value;

                int ret_error = check_date_bounds(date);

                if (ret_error == 1)
                {
                    dpHeaderActivityLastAttended.Text = "";
                    return;

                }
                    

                if (date.DayOfWeek == DayOfWeek.Sunday)
                {

                   
                    m_ActivityDateSelected = date;
                    BuildQuery_and_UpdateGrid();



                }
                else
                {
                    m_dateIsValid = false;

                }
            }
        }

        private void TxtHeaderLastNamePr_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {

                if (txtHeaderFirstNamePr.Text == "")
                {
                    Display_AttendeeListTable_in_Grid();

                }


            }
            else if (e.Key == Key.Escape)
            {
                txtHeaderFirstNamePr.Text = "";

                Display_AttendeeListTable_in_Grid();

            }
        }

        private void TxtHeaderLastNamePr_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = txtHeaderLastNamePr.Text.ToUpper();


            if (txtHeaderLastNamePr.Text != "")
            {

                Disable_Filters();
                

                var filterQueryTable = m_lstattendanceTableRows.Where(row => row.LastName.ToUpper().Contains(text)).ToList();
                dataGrid_prospect.DataContext = filterQueryTable;
                dataGrid_prospect.Items.Refresh();
                lblAttendenceMetrics.Text = dataGrid_prospect.Items.Count.ToString();



            }
            else // txt = ""
            {

                Enable_Filters();
              
                Display_AttendeeListTable_in_Grid();

            }
        }

        private void TxtHeaderFirstNamePr_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                
                if (txtHeaderFirstNamePr.Text == "")
                {
                    dpChurchLastAttendedPr.IsEnabled = true;
                    Display_AttendeeListTable_in_Grid();

                }


            }
            else if (e.Key == Key.Escape)
            {
                txtHeaderFirstNamePr.Text = "";
                dpChurchLastAttendedPr.IsEnabled = true;
                Display_AttendeeListTable_in_Grid();

            }
        }

        private void TxtHeaderFirstNamePr_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if in followUp view, use query database else if in model list view filter table

            string text = txtHeaderFirstNamePr.Text.ToUpper();


            if (txtHeaderFirstNamePr.Text != "")
            {

                Disable_Filters();
                

                var filterQueryTable = m_lstattendanceTableRows.Where(row => row.FirstName.ToUpper().Contains(text)).ToList();
                dataGrid_prospect.DataContext = filterQueryTable;
                dataGrid_prospect.Items.Refresh();
                lblAttendenceMetrics.Text = dataGrid_prospect.Items.Count.ToString();
                  

            }
            else if (txtHeaderFirstNamePr.Text == "") // txt = ""
            {

                Enable_Filters();

                Display_AttendeeListTable_in_Grid();
              
            }

        }

        private void DpChurchLastAttendedPr_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

            Cursor = Cursors.Wait;

            var calender = sender as DatePicker;


            if (calender.SelectedDate != null)
            {
                DateTime datec = calender.SelectedDate.Value;



                m_alistDateSelected = datec;

                int ret_error = check_date_bounds(datec);

                if (ret_error == 1)
                {
                    dpChurchLastAttendedPr.Text = "";
                    return;
                }
                    


                string date = m_alistDateSelected?.ToString("MM-dd-yyyy");

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

            Cursor = Cursors.Arrow;

        }

        private void DpHeaderActivityLastAttended_KeyUp(object sender, KeyEventArgs e)
        {
            var dp = sender as DatePicker;

            if (e.Key == Key.Escape)
            {
                if (dp.Text != "")
                {
                    dp.Text = "";
                    m_dateIsValid = false;
                    m_ActivityDateSelected = null;
                    BuildQuery_and_UpdateGrid();
                }

            }
        }


        private void DpHeaderActivityPr_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var calender = sender as DatePicker;
            if (calender.SelectedDate != null)
            {
                DateTime date = calender.SelectedDate.Value;

                int ret_error = check_date_bounds(date);

                if (ret_error == 1)
                {
                    dpHeaderActivityPr.Text = "";
                    return;

                }


                if (date.DayOfWeek == DayOfWeek.Sunday)
                {

                    m_dateIsValid = true;
                    m_ActivityDateSelectedPr = date;



                }
                else
                {
                    m_dateIsValid = false;

                }
            }
        }

        private void colActivityHeaderGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                m_isActivityChecked = false;
                m_ctbActivity.UncheckAll();
                BuildQuery_and_UpdateGrid();
            }
        }

        private void RibbonMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }



        //private void DataGrid_prospect_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        //{
        //    m_attendance_row_selected.PropertyChanged += AttendanceTabledr_PropertyChanged;
        //}

        //private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        //{
        //    m_default_row_selected.PropertyChanged += DefaultTabledr_PropertyChanged;

        //}
    }

}





