using System;
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
using System.Text.RegularExpressions;

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


           


            m_version_string = "v3.1.31";




            dataGrid.CopyingRowClipboardContent += new EventHandler<DataGridRowClipboardEventArgs>(CopyDataGridtoClipboard);






            //open file with database credentials
            SplashScreen splashScreen = new SplashScreen("Resources/splashscreen.png");
            splashScreen.Show(true);
            TimeSpan timespan = new TimeSpan(0, 0, 1); // 1 seconds timespan



            splashScreen.Close(timespan);


            var executingPath = Directory.GetCurrentDirectory();

            try
            {



#if Debug
                this.Title = $"Attendee Manager " + m_version_string + "(Debug)";
#else
                    this.Title = "Attendee Manager " + m_version_string;
#endif

#if init_db

                   
                    if (m_dbContext == null)
                    {

                        
                            m_dbContext = new AttendeeManagerDBModel();
                          

                            m_dbContext.Configuration.ProxyCreationEnabled = false;
                            m_dbContext.Configuration.AutoDetectChangesEnabled = true;
                            m_dbContext.Configuration.LazyLoadingEnabled = false;

                   // CopyOverContent();

                    //load db context
                            m_dbContext.Attendees.Load();
                            m_dbContext.Attendance_Info.Load();
                            m_dbContext.Activities.Load();
                        

                    }

              


                    InitDataSet();
                   // ConvertListToDataTable();
#endif

#if db_errors
                   correctDBerrors();
#endif



                    // display the attendee records in the table
                   Display_DefaultTable_in_Grid();
                //Loaded the program settings

                LoadSettings();
              

                if (m_ActivityListPath != ""  )
                {
                   if (File.Exists(m_ActivityListPath))
                    {
                        List<ComboTreeNode> tmp_list = Load_ChurchActivities_From_File_to_Array(m_ActivityListPath); // load tree as a list of nodes from the file                    

                        m_lstCurrActivityListNodes = Array_to_Tree(tmp_list); //Transfor array to tree structure
                        LoadActivityProspectComboTree(m_lstCurrActivityListNodes); // load tree into prospect tab's dropdown

                        txtbActivityListName.Text = GetListName();
                    }
                        
                    
                }

                



                if (txtbActivityListName.Text == "")
                    txtbActivityListName.Text = "No list";






                m_lstContextActivities = AddALLActivitiesFromContextToActivityTree(); 
              

            }
            catch (Exception ex)
            {

                MessageBox.Show($"Exception occurred when performing database initialization { ex}!\n","Database Error", MessageBoxButton.OK, MessageBoxImage.Error );
            }












        }



      
        private AttendeeManagerDBModel m_dbContext;
        //private AttendeeManagerDBModel m_dbContext;
        private DateTime? m_DateSelected = null;
        private DateTime? m_alistDateSelected = null;
        private DateTime? m_ActivityDateSelected = null;
        private DateTime? m_ActivityDateSelectedPr = null;
        private DataSet m_DataSet = new DataSet();


        private bool m_ActivityTreeChanged = false; //check if the user changed the Activity tree
        private bool m_alist_tree_changed = false;
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

        //list of TreeNodes in the currently loaded list
        private List<ComboTreeNode> m_lstCurrActivityListNodes = new List<ComboTreeNode> { };

        //list of TreeNodes in the activity context table
        private List<ComboTreeNode> m_lstContextActivities = new List<ComboTreeNode> { };




     


        // the current activity to be added 
        private Activity m_currentAdded_Activity = null;

        // the current activity selected for filtering
        private Activity m_currentSelected_Activity = null;

       
        private Timer aTimer = null;

        private string m_version_string = "";
        private string m_followUpWeeks ="" ;
        //Store the list name that is in use
        private string m_ActivityListPath = "";

     
        private bool m_isAttendedChecked = false;
        private bool m_isFollowupChecked = false;
        private bool m_isRespondedChecked = false;
    
        private bool m_isActivityChecked = false;
        private bool m_isQueryTableShown = false;
      
        // view state
        private bool m_alistView = false;
        private bool m_AttendanceView = false;
       
      
     
        private bool m_dateIsValid = false;
        private bool m_alistdateIsValid = false;

        private bool m_loaded = false;


        private List<ComboTreeNode> AddALLActivitiesFromContextToActivityTree()
        {
            List<ComboTreeNode> fmt_lstNodes = new List<ComboTreeNode>(); ;

            List<ComboTreeNode> tmp = GetListOfTreeNodesFromActivityContext(); // Load all the Activities in the activity context Table into a list of nodes

            if (tmp.Any() )
            {
                fmt_lstNodes = Array_to_Tree(tmp); //Transform the list into a tree of nodes
                LoadActivityComboTree(fmt_lstNodes); //load tree into activity combo box
            }
            

            return fmt_lstNodes;;
        }

        private void LoadActivityComboTree(List<ComboTreeNode> list)
        {
            if (m_ctbActivity.Nodes.Any())
            {

                m_ctbActivity.Nodes.Clear();
            }

            
            //Add ComboTreeNodes to ComboTreeBox Treeview
            foreach (ComboTreeNode header in list)
            {
                global::ComboTreeNode node = new global::ComboTreeNode();
                node.Text = $"(List: {header.activityList})" + " " + (string)header.Header;


                global::ComboTreeNode parent2 = m_ctbActivity.Nodes.Add(node.Text);
                parent2.Expanded = true;
                
                //ActivityGroups
                foreach (ComboTreeNode group in header.Items)
                {
                    global::ComboTreeNode node2 = new global::ComboTreeNode();
                    node2.Text = (string)group.Header;


                    global::ComboTreeNode child2 = parent2.Nodes.Add(node2.Text);
                    //ActivityTask
                    foreach (ComboTreeNode task in group.Items)
                    {
                        global::ComboTreeNode node3 = new global::ComboTreeNode();
                        node3.Text = (string)task.Header;


                        global::ComboTreeNode taskNode2 = child2.Nodes.Add(node3.Text);
                        //subTask
                        foreach (ComboTreeNode subtask in task.Items)
                        {
                            global::ComboTreeNode node4 = new global::ComboTreeNode();
                            node4.Text = (string)subtask.Header;


                            global::ComboTreeNode subTaskNode2 = taskNode2.Nodes.Add(node4.Text);
                        }
                    }

                }
            }

            
            

        }
        private List<ComboTreeNode> GetListOfTreeNodesFromActivityContext ()
        {

            List<ComboTreeNode> tmp_listNodes = new List<ComboTreeNode>();
          
            // see if any activity entries changed, only itterate through the activities that has state = "unchanged" and "added"
            var activity_entry_changed = m_dbContext.ChangeTracker.Entries<Activity>();

            foreach (var a in activity_entry_changed)
            {
                if (a.State == EntityState.Unchanged || a.State == EntityState.Added)
                {

                    if (a.Entity.ActivityText != "")
                    {


                        string alist = a.Entity.ListName;

                        var activity = new string(

                                       (from c in a.Entity.ActivityText
                                        where char.IsWhiteSpace(c) || char.IsLetterOrDigit(c) || !char.IsSymbol(c)
                                        select c).ToArray());
                        string[] split_str = activity.Split('-');

                        for (int i = 0; i <= split_str.Length - 1; i++)
                        {
                            ComboTreeNode new_node = new ComboTreeNode
                            {
                                Header = split_str[i],
                                Level = i,
                                activityList = alist
                            };
                            tmp_listNodes.Add(new_node);
                        }

                    }
                   
                }
            }

          
            return tmp_listNodes;
        }

    
        private List<ComboTreeNode> Array_to_Tree(List<ComboTreeNode> tree)
        {

            /*
             * Input: tree - this is all the activities in an array form that is in the context. The idea is to put this in tree form
             * 1. Iterate through the passed in tree
             * 2. Build tmp_tree from passed in tree 
             * 3. If a node already exist in the tmp_tree and the node level is '0' then add the node to the tmp_tree and set the root_ptr to tree[i]
             * 4. If the node already exist in the tmp_tree and the level is NOT '0' then do not add the node to the tmp_tree and just set the    
             * 5. If a node do not exist in the tmp_tree being built then ADD the node to tmp_tree being built
             */
            ComboTreeNode parent_ptr = null;
            ComboTreeNode root_ptr = null;
            ComboTreeNode child_ptr = null;

            List<ComboTreeNode> tmp_tree = new List<ComboTreeNode>() { };



            bool NodeExist = false;
            bool node_found = false;
            ComboTreeNode ctn;
            ComboTreeNode find_node;

            for (int i = 0; i <= tree.Count - 1; i++)
            {

                if (tree[i].Level == 0)
                {
                    if (root_ptr != null)
                    {

                        // there is already a node with the same name as tree[i] node so dont add the node
                        if ((string)root_ptr.Header == (string)tree[i].Header)
                        {

                            // do nothing

                        }
                        else /* tree[i].Header is different than root_ptr.Header */
                        {
                           
                            ctn = tmp_tree.SingleOrDefault(f => (string)f.Header == (string)root_ptr.Header);
                          
                            if (ctn == null) /*node does not exist*/
                            {
                                tmp_tree.Add(root_ptr);
                                node_found = false;

                                /* if tree[i] has the same name as an element already in the list then the new root becomes the list item with Header=tree[i] header
                                 * else root_ptr = tree[i]*/

                                find_node = tmp_tree.SingleOrDefault(f => (string)f.Header == (string)tree[i].Header);
                                if (find_node !=null) /*node found */
                                {
                                    root_ptr = find_node;
                                }
                                else
                                  root_ptr = tree[i];
                            }
                            else 
                            {
                                root_ptr = ctn;
                                NodeExist = false;
                            }

                        }

                    }
                    else /* root_ptr is null point it too the current tree node that is iterated */
                        root_ptr = tree[i];


                }

                /* This node is a child of the root node pointed to by root_ptr */
                else if (tree[i].Level == root_ptr.Level + 1)
                {

                    if (tree[i].Parent == null)
                    {

                        
                        foreach (ComboTreeNode node in root_ptr.Items)
                        {
                            // there is already a node with the same name as tree[i] node so dont add the node
                            if ((string)node.Header == (string)tree[i].Header)
                            {
                                NodeExist = true;
                                //parent_ptr = tree[i];
                                
                                break;


                            }
                        }
                            // Item exits, do nothing, otherwise add node to root_ptr 
                            if (NodeExist)
                            {
                            // do nothing keep pointer where it is
                                NodeExist = false; 
                            }
                            else
                            {

                                root_ptr.Items.Add(tree[i]);
                                parent_ptr = tree[i];
                                NodeExist = false;
                            }
                        

                    }
                        
                }
                /* This node is a child of a parent node pointed to by parent_ptr */
                else if (tree[i].Level == parent_ptr.Level + 1)
                {
                    if (tree[i].Parent == null)
                    {

                        foreach (ComboTreeNode node in parent_ptr.Items)
                        {
                            // there is already a node attached with the same name as tree[i] node so dont add the node
                         
                            if ((string)node.Header == (string)tree[i].Header)
                            {
                                NodeExist = true;
                               // parent_ptr = tree[i];
                                break;

                            }

                        }
                            // Item exist, do nothing, otherwise, add node to parent
                            if (NodeExist)
                            {
                            // Set NodeExist to false;
                                NodeExist = false;
                                
                            }
                            else
                            {
                                parent_ptr.Items.Add(tree[i]);
                                child_ptr = tree[i];
                                NodeExist = false;
                            }
                        


                      
                      
                    }

                 
                }
                /* This node is a child of a child node pointed to by child_ptr
                 * parent_ptr becomes the node
                 */
                else if (tree[i].Level == child_ptr.Level + 1)
                {
                    if (tree[i].Parent == null)
                    {
                        foreach (ComboTreeNode node in child_ptr.Items)
                        {
                            // there is already a node with the same name as tree[i] node so dont add the node
                       

                            if ((string)node.Header == (string)tree[i].Header)
                            {
                                NodeExist = true;
                                //child_ptr = tree[i];
                                break;
                            }
                        }


                        // Item exist, do nothing, otherwise, add node to parent
                        if (NodeExist)
                        {
                            // do nothing keep pointer where it is
                            NodeExist = false;
                        }
                        else
                        {
                            child_ptr.Items.Add(tree[i]);
                            parent_ptr = tree[i];

                        }
                        
                        
                    }
                  
                }


            }

            if (!tmp_tree.Contains(root_ptr))
                tmp_tree.Add(root_ptr);

            return tmp_tree;


        }
        private void copy_db_tofile()
        {
            FileStream fs = new FileStream("AttendeesFile.txt", FileMode.Create, FileAccess.Write);

            using (StreamWriter sw = new StreamWriter(fs, Encoding.ASCII))
            {



                foreach (Attendee at in m_dbContext.Attendees)
                {

                    sw.WriteLine($"{at.AttendeeId} {at.Checked} {at.LastName} {at.FirstName}");

                }
            }

            FileStream fs2 = new FileStream("AttendeeInfo.txt", FileMode.Create, FileAccess.Write);

            using (StreamWriter sw2 = new StreamWriter(fs2, Encoding.ASCII))
            {
                foreach (Attendance_Info atinfo in m_dbContext.Attendance_Info)
                {
                    sw2.WriteLine($"{atinfo.Attendance_InfoId} {atinfo.AttendeeId} {atinfo.DateString}");
                }
            }    
            
            
}
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


                   if (dr.ActivityChecked == "1" && m_ActivityDateSelectedPr != null && m_currentAdded_Activity != null)
                   {
                       btnPanelAddActivity.IsEnabled = true;
                      
                       break;
                   }
                   else
                   {
                       btnPanelAddActivity.IsEnabled = false;
                      

                   }

                  
                   
               }


               
               if (m_dbContext.ChangeTracker.HasChanges() || m_ActivityTreeChanged || m_alist_tree_changed )
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
        private void SaveSettings()
        {

            List<XNode> lstdocNodes = new List<XNode>() { };
            var doc_root = new XElement("XmlDocument");
            XDocument DOMdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), doc_root);

            Cursor = Cursors.Wait;


            XElement ProgramSettingsElement = new XElement("ProgramSettings");

            if (m_followUpWeeks == "")
                m_followUpWeeks = "4";


            XElement FollowUpElement = new XElement("FollowUpWeeks", new XAttribute("Weeks", m_followUpWeeks));
            XElement ListActivityPath = new XElement("ActivityList", new XAttribute("Path",m_ActivityListPath));

            ProgramSettingsElement.Add(FollowUpElement);
            ProgramSettingsElement.Add(ListActivityPath);

            lstdocNodes.Add(ProgramSettingsElement);

            doc_root.Add(lstdocNodes);

            string settingPath = Directory.GetCurrentDirectory();
            settingPath += "\\settings.xml";

            
            try
            {
               

                    var fsXML = new FileStream(settingPath, FileMode.OpenOrCreate, FileAccess.Write);
                    // save document
                    DOMdoc.Save(fsXML);
                    fsXML.Close();
             

            }
            catch (Exception)
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("Something went wrong accessing the 'settings.xml' file, either the file has wrong access permissions or the file is missing.", "settings.xml", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Cursor = Cursors.Arrow;
        }
        private void LoadSettings()
        {
            //Open new file that contains all the descriptions
            string descriptions_pathname = Directory.GetCurrentDirectory() + "\\settings.xml";

            // Open or create new file that wil hold the node's descriptions            
            //FileStream fsDescriptions = new FileStream(descriptions_pathname, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            XmlReaderSettings reader_settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true
            };




            try
            {


                if (File.Exists(descriptions_pathname) )
                {
                    using (XmlReader xreader = XmlReader.Create(descriptions_pathname, reader_settings))
                    {
                        xreader.ReadStartElement("XmlDocument");

                        XElement XMLtag = (XElement)XNode.ReadFrom(xreader);
                        //int i = 0;

                        if (XMLtag.Name == "ProgramSettings")
                        {
                            foreach (XElement settingsElement in XMLtag.Elements())
                            {
                                if (settingsElement.Name == "FollowUpWeeks")
                                {

                                    string followupnumber = settingsElement.FirstAttribute.Value;
                                    m_followUpWeeks = followupnumber;
                                }
                                if (settingsElement.Name == "ActivityList")
                                {

                                    m_ActivityListPath = settingsElement.FirstAttribute.Value;


                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("File does not exist 'settings.xml'.", "settings.xml", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
            catch
            {
                MessageBox.Show("Something went wrong accessing the 'settings.xml' file, either the file has wrong access permissions or the file is missing.", "settings.xml", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
      
        private string GetListName()
        {
            string[] splitstring = m_ActivityListPath.Split('\\');
            string strlast = splitstring.Last();
           string listname = strlast.Substring(0, strlast.Length - 4);
            return listname;
        }
     

        private void LoadActivityProspectComboTree(IEnumerable<ComboTreeNode> tree)
        {
            if (m_ctbActivityProspect.Nodes.Any())
            {
             
                m_ctbActivityProspect.Nodes.Clear();
            }



            //Add ComboTreeNodes to ComboTreeBox Treeview
            foreach (ComboTreeNode header in tree)
            {
                global::ComboTreeNode node = new global::ComboTreeNode();
                node.Text = (string)header.Header;

               
                global::ComboTreeNode parent2 = m_ctbActivityProspect.Nodes.Add(node.Text);
                parent2.Expanded = true;
                //ActivityGroups
                foreach (ComboTreeNode group in header.Items)
                {
                    global::ComboTreeNode node2 = new global::ComboTreeNode();
                    node2.Text = (string)group.Header;

                   
                    global::ComboTreeNode child2 = parent2.Nodes.Add(node2.Text);
                    //ActivityTask
                    foreach (ComboTreeNode task in group.Items)
                    {
                        global::ComboTreeNode node3 = new global::ComboTreeNode();
                        node3.Text = (string)task.Header;

                       
                        global::ComboTreeNode taskNode2 = child2.Nodes.Add(node3.Text);
                        //subTask
                        foreach (ComboTreeNode subtask in task.Items)
                        {
                            global::ComboTreeNode node4= new global::ComboTreeNode();
                            node4.Text = (string)subtask.Header;

                          
                            global::ComboTreeNode subTaskNode2 = taskNode2.Nodes.Add(node4.Text);
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

            if (m_lstdefaultTableRows.Any() )
                dataGrid.DataContext = m_lstdefaultTableRows.OrderBy(rec => rec.LastName).ToList();

            // change column header 'follow-up last generated' to 'church last attended' to reflect default header for the table
            if (lblChurchLastAttended.Content == "Follow-Up last generated")
                lblChurchLastAttended.Content = "Church last attended";

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

             
                foreach (var AttendeeRec in m_dbContext.Attendees)
                {
                    var queryLastDate = (from DateRec in AttendeeRec.AttendanceList
                                         where DateRec.Status == "Attended" || DateRec.Status == "Responded"
                                         orderby DateRec.Date ascending
                                         select DateRec).ToList().LastOrDefault();

                    var queryActivityLastDate = (from ActivityDateRec in AttendeeRec.ActivityList
                                                 where ActivityDateRec.ActivityText != "1"
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
                            ActivityChecked = AttendeeRec.IsActivityChecked ? "1" : "",

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
                            DefaultTabledr.ActivityText = queryActivityLastDate.ActivityText;

                        }
                        else
                        {
                            DefaultTabledr.Activity_Last_Attended = "n/a";
                            DefaultTabledr.ActivityText = "n/a";
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
                                ActivityChecked = AttendeeRec.IsActivityChecked ? "1" : "",

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

                                DefaultTabledr.ActivityText = queryActivityLastDate.ToString();

                            }
                            else
                            {
                                DefaultTabledr.Activity_Last_Attended = "n/a";
                                DefaultTabledr.ActivityText = "n/a";
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
                        else if (e.PropertyName == "Activity")
                        {

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

            //m_ctbActivityProspect.Enabled = false;
            //dpChurchLastAttendedPr.IsEnabled = false;
            //dpHeaderActivityPr.IsEnabled = true;
            //btnPanelAddActivity.IsEnabled = true;
            //dpHeaderActivityPr.IsEnabled = true;
            //btnPanelNewActivity.IsEnabled = true;



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

        void SaveProspectCheckmarks()
        {
            List<AttendanceTableRow> AttendedChecklist = getListOfCheckedAttendees("Attended");
            List<AttendanceTableRow> ActivityCheckList = getListOfCheckedAttendees("Activity");

            // Mark each attendee that has a checkmark next to it as checked in the dbcontext

            if (AttendedChecklist.Any() )
            {
                foreach (AttendanceTableRow dr in AttendedChecklist)
                {

                    Attendee queryAttendeeInContext = m_dbContext.Attendees.Local.SingleOrDefault(rec => rec.AttendeeId == dr.AttendeeId);
                    if ( queryAttendeeInContext != null)
                        queryAttendeeInContext.Checked = true;
                }
            }
            else
            {
                var querySelectAttendees = m_dbContext.Attendees.Local.Where(rec => rec.Checked == true);
                foreach (Attendee at in querySelectAttendees)
                {
                    at.Checked = false;
                }
            }
    
            if (ActivityCheckList.Any() )
            {
                foreach (AttendanceTableRow adr in ActivityCheckList)
                {
                    Attendee queryActivityinContext = m_dbContext.Attendees.Local.SingleOrDefault(rec => rec.AttendeeId == adr.AttendeeId);
                    if (queryActivityinContext != null)
                        queryActivityinContext.IsActivityChecked = true;


                }
            }
           else
           {
                var querySelectedActivities = m_dbContext.Attendees.Local.Where(rec => rec.IsActivityChecked == true);
                foreach (Attendee at in querySelectedActivities)
                {
                    at.IsActivityChecked = false;
                }
           }
            


        }
        void Save_Changes(object sender, System.Windows.RoutedEventArgs e)
        {

            Cursor = Cursors.Wait;


            //  // save last change to list and check for any checked attendees  



            SaveProspectCheckmarks();

            ////if there is no attendees checked then make sure all attendees in the dbcontext is not checked (ie checked = false)
            //if (Checklist.Any() )
            //{
            //    foreach (Attendee at in m_dbContext.Attendees)
            //    {
            //        if (at.Checked == true)
            //        {
            //            at.Checked = false;
            //        }
            //    }
            //}

            // save change to db
            SaveActiveList();
            m_ActivityTreeChanged = false;
            m_alist_tree_changed = false;

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

        
        List<AttendanceTableRow> getListOfCheckedAttendees(string Column_Header)
        {

            //end all edits and update the datagrid with changes
            dataGrid_prospect.CommitEdit(DataGridEditingUnit.Row, true);
            dataGrid_prospect.UpdateLayout();

            List<AttendanceTableRow> lstOfCheckedAttendees = new List<AttendanceTableRow>() { };

            if (Column_Header == "Attended")
            {
                foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
                {
                    if (dr.Attended == "1")
                    {


                        lstOfCheckedAttendees.Add(dr);

                    }

                }
            }
            else if (Column_Header == "Activity")
            {
                foreach (AttendanceTableRow dr in m_lstattendanceTableRows)
                {
                    if (dr.ActivityChecked == "1")
                    {


                        lstOfCheckedAttendees.Add(dr);

                    }

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

                    m_lstContextActivities = AddALLActivitiesFromContextToActivityTree(); // rebuild activity dropdown

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
                Checklist = getListOfCheckedAttendees("Attended");




              
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
                        DefaultTableRow Defaultdr = new DefaultTableRow();




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
                        Defaultdr.ActivityText = "n/a";
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


      
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            dpChurchLastAttended.DisplayDate = DateTime.Today;
           // btnAddColumn.IsEnabled = false;
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
           
            m_ctbActivity.DropDownWidth = 600;
            m_ctbActivity.DropDownHeight = 350;
            
           

            m_ctbActivity.DropDownClosed += M_ctbActivity_DropDownClosed;
       
          
           // m_ctbActivityProspect.DroppedDown = false;
            m_ctbActivityProspect.Location = new System.Drawing.Point(0, 0);
            m_ctbActivityProspect.Name = "ctbActivityPrCheckboxes";
            m_ctbActivityProspect.SelectedNode = null;
            m_ctbActivityProspect.ShowCheckBoxes = true;
            m_ctbActivityProspect.Size = new System.Drawing.Size(200, 19);
            m_ctbActivityProspect.DrawWithVisualStyles = true;
            m_ctbActivityProspect.Visible = true;
            
            m_ctbActivityProspect.DropDownWidth = 600;
            m_ctbActivityProspect.DropDownHeight = 350;
           
            

            m_ctbActivityProspect.DropDownClosed += M_ctbActivityProspect_DropDownClosed;
          
            

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
           // m_isActivityfilterByDateChecked = false;
            m_AttendanceView = true;
            btnSave.IsEnabled = false;
            btnImport.IsEnabled = false;
            btnPanelNewActivity.IsEnabled = false;
        

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
        private List<ComboTreeNode> Load_ChurchActivities_From_File_to_Array(string listPath)
        {



            List<ComboTreeNode> tree_array = new List<ComboTreeNode>() { };


            try
            {


                if (File.Exists($"{listPath}"))
                {
                    FileStream fs = new FileStream(listPath, FileMode.Open, FileAccess.Read);
                    // STX = byte 0x02
                    // ETX = byte 0x03
                    //message = [STX(1byte) + header_byte_array (n bytes) + escape_seq (2 bytes)+payload + ETX(1byte)]



                    byte[] read_buffer = new byte[1024];
                    byte[] tmp_buf = new byte[1024];


                    byte[] ETX_seq = new byte[2] { 0x03, 0x30 };
                    byte[] STX_seq = new byte[2] { 0x02, 0x20 };

                    byte[] escape_seq = new byte[2] { 0x0A, 0xA0 };

                    int node_level_length = 1;

                    //byte[] tmp_array = new byte[STX_seq.Length + node_level_length + header_byte_array.Length + escape_seq.Length + payload.Length + ETX_seq.Length];

                    int STXidx = 0;
                    int ETXidx = 0;

                    int offset_size = 0;
                    int idx = 0;
                    int bytes_read = 0;
                    string node_header;
                    int node_level = 0;
                    int payload_length = 0;


                    ComboTreeNode node;
                    MemoryStream payload_data;

                    while ((bytes_read = fs.Read(read_buffer, offset_size, read_buffer.Length - offset_size)) > 0)
                    {

                        //loop over read buffer
                        for (idx = 0; idx <= read_buffer.Length - 1; idx++)
                        {
                            // read untill get a STX
                            if (read_buffer[idx] == 0x02 && read_buffer[idx + 1] == 0x20)
                                STXidx = idx; //STXidx is the beginning of the STX sequence index in the read buffer array

                            // read until you get an ETX symbol
                            if (read_buffer[idx] == 0x03 && read_buffer[idx + 1] == 0x30)
                                ETXidx = idx + 1; //ETXidx is the end of the ETX sequence index in the read buffer array and at the end of 1 message

                            // if an STX and ETX is found decode the the data and find the next message (STX ETX)
                            if (read_buffer[STXidx] == (byte)ArrayFormat.STX && read_buffer[ETXidx] == 0x30)
                            {


                                //we found the beginning of the payload
                                for (int i = STXidx; i <= ETXidx - 2; i++)
                                {
                                    if (read_buffer[i] == 0x0A && read_buffer[i + 1] == 0xA0)
                                    {

                                        int header_size = (i - 1) - (STXidx + 1 + node_level_length);

                                        node_level = Convert.ToInt16(read_buffer[STXidx + 2]);

                                        node_header = Encoding.UTF8.GetString(read_buffer, STXidx + 3 /*beggining offset of node header*/, header_size);

                                        string[] ary_nodeHeader = node_header.Split('_');
                                        string activityName = ary_nodeHeader[0];
                                        string listname = ary_nodeHeader[1];

                                        node = new ComboTreeNode { Header = activityName, Level = node_level, activityList = listname };

                                        if (read_buffer[i + 2] == (byte)ArrayFormat.ETX)
                                        {
                                            payload_length = 0;
                                        }
                                        else

                                        {
                                            payload_data = new MemoryStream();

                                            payload_length = (ETXidx - 2) - (i + 2);

                                            payload_data.Write(read_buffer, i + 2, payload_length);


                                            node.rtbDescriptionMStream = payload_data;
                                            string data = Encoding.UTF8.GetString(payload_data.GetBuffer(), 0, payload_length);
                                        }

                                        tree_array.Add(node);

                                        break;
                                    }
                                }



                                STXidx = ETXidx + 1;
                                ETXidx = 0;

                            }
                            else if (idx == read_buffer.Length - 1 && ETXidx == 0)   //we read until the end of the buffer but cannot find an end of message sequence ETX
                            {
                                // if this is the last byte in the buffer and we have not found an ETX symbol

                                //cal new size of offset into read_buffer where the new data will go
                                offset_size = (read_buffer.Length - 1) - STXidx;


                                Buffer.BlockCopy(read_buffer, STXidx, tmp_buf, 0, offset_size);

                                //clear the read_buffer
                                for (int i = 0; i <= read_buffer.Length - 1; i++)
                                {
                                    read_buffer[i] = 0;
                                }

                                //copy the bytes from tmp_buffer back to the read_buffer at the 0 index in read buffer
                                Buffer.BlockCopy(tmp_buf, 0, read_buffer, 0, offset_size);
                                //clear the tmp_buffer
                                for (int i = 0; i <= read_buffer.Length - 1; i++)
                                {
                                    tmp_buf[i] = 0;
                                }

                            }




                        }





                    } // while readbytes



                }
                else
                {

                }

                //else // activities file does not exist
                //{
                //    Cursor = Cursors.Arrow;
                //    MessageBox.Show("No Activities file found or file is in the wrong format! '*.xml'");
                //}

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "XML read error", MessageBoxButton.OK, MessageBoxImage.Error);

            }



            return tree_array;


        }

        private void M_ctbActivityProspect_DropDownClosed(object sender, System.EventArgs e)
        {
            ComboTreeBox ctb = sender as ComboTreeBox;
            IEnumerable<global::ComboTreeNode> chkNodes = ctb.CheckedNodes;

            if (chkNodes.Any())
            {
                global::ComboTreeNode firstNode = chkNodes.First();
                m_currentAdded_Activity = new Activity
                {
                    ActivityText = firstNode.GetFullPath("->", false),
                    ListName = txtbActivityListName.Text
                };

               
            }





        }
        

        private void M_ctbActivity_DropDownClosed(object sender, System.EventArgs e)
        {
            _ = Cursors.Wait;

            ComboTreeBox ctb = sender as ComboTreeBox;
            IEnumerable<global::ComboTreeNode> chkNodes = ctb.CheckedNodes;
            if (chkNodes.Any() )
            {
                global::ComboTreeNode firstNode = chkNodes.First();
                if (firstNode != null)
                {
                    string path = firstNode.GetFullPath("->", false);

                    Regex pattern = new Regex(@"^\(List:\s(.+)\)\s+(.+)");

                    Match match = pattern.Match(path);
                    GroupCollection groups = match.Groups;

                    string listname = groups[1].Value;
                    string activity_name = groups[2].Value;

                                     
                    m_isActivityChecked = true;

                  if (m_currentSelected_Activity == null) //create an activity with activity text if non exist
                  {

                        m_currentSelected_Activity = new Activity { ActivityText = activity_name, ListName=listname};

                        
                  }
                  else
                  {
                        // if this is a different activity than previous selected change the ActivityText of the activity
                       m_currentSelected_Activity.ActivityText = activity_name;
                        m_currentSelected_Activity.ListName = listname;
                   
                      
                  }
                   
                   
                   
                    
                    
                    BuildQuery_and_UpdateGrid();
                   
                }
            }
            else
            {
                // nothing is selected, user pressed escape
                m_isActivityChecked = false;

               
            }

            _ = Cursors.Arrow;
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



                    MessageBoxResult res = MessageBox.Show("Changes have been made but not saved to the database yet. Save Changes?", "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                    if (res == MessageBoxResult.Yes)
                    {
                        Cursor = Cursors.Wait;
                        SaveProspectCheckmarks();
                        SaveActiveList();
                        SaveSettings();
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
                        SaveSettings();
                        // close all active threads
                        Environment.Exit(0);

                    }
                    else if (res == MessageBoxResult.Cancel)
                        e.Cancel = true;







                }
                else
                {
                    Cursor = Cursors.Wait;
                    SaveSettings();
                    Environment.Exit(0);

                    Cursor = Cursors.Arrow;

                }
#endif

        }
     
        private void SaveActiveList()
        {

           
            // save contents to database
            m_dbContext.SaveChanges();
         

        }

        private void btnGenerateFollowUps_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            FollowUpWindow fw = new FollowUpWindow(m_followUpWeeks);
           

            fw.ShowDialog();


          


          

            if (fw.GetFollowUpWeeks != "0")
            {

               
                MessageBoxResult result = MessageBox.Show($"Are you sure you want to generate follow-Ups for every {fw.GetFollowUpWeeks } weeks now?", "Generate Follow-Up", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Cursor = Cursors.Wait;
                    m_followUpWeeks = fw.GetFollowUpWeeks;
                    bool generate_one = GenerateDBFollowUps(m_followUpWeeks);

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
            else if (fw.GetFollowUpWeeks == "0")
            {
                // user pressed cancel so do nothing
            }
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
                rowCopy.ActivityText = row.ActivityText;
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
            bool Activity = m_isActivityChecked && m_currentSelected_Activity != null;
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
                string strActivity = m_currentSelected_Activity.ActivityText;
              

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


                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any() && m_lstdefaultTableRowsCopy[i].ActivityList.Any())
                    {
                        IEnumerable<Attendance_Info> query_status;
                        IEnumerable<Activity> query_activity;

                        if (bChurchStatusAttended)
                        {
                            query_status = from row in m_lstdefaultTableRowsCopy[i].AttendanceList
                                           where row.Status == strChurchStatus || row.Status == strChurchStatusResponded
                                           select row;

                            query_activity = from row_activity in m_lstdefaultTableRowsCopy[i].ActivityList
                                             where row_activity.DateString == strActivityDate &&
                                                   row_activity.ActivityText == m_currentSelected_Activity.ActivityText
                                             select row_activity;
                        }
                        else
                        {
                            query_status = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0 and ActivityText == @1", strActivityDate, strActivity);
                        }


                        if (query_status.Count() != 0 && query_activity.Count() != 0)
                        {
                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].ActivityText = strActivity;

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

                string strActivity = m_currentSelected_Activity.ActivityText;

            
                if (strChurchStatus == "Attended" || strChurchStatus == "Responded")
                {
                    bChurchStatusAttended = true;
                    strChurchStatusResponded = "Responded";
                }

                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any() && m_lstdefaultTableRowsCopy[i].ActivityList.Any())
                    {
                        IQueryable<Attendance_Info> query_dateandstatus;
                        IQueryable<Activity> query_activity;
                        if (bChurchStatusAttended)
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and (Status == @1 or Status = @2 )", strChurchDate, strChurchStatus, strChurchStatusResponded);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityText == @0", strActivity);
                        }
                        else
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0 and Status == @1", strChurchDate, strChurchStatus);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityText == @0",  strActivity);
                        }

                        if (query_dateandstatus.Count() != 0 && query_activity.Count() != 0)
                        {
                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;
                            m_lstdefaultTableRowsCopy[i].ActivityText = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if (ChurchDate && Activity) //ChurchDate and Activity
            {
                string strChurchDate = m_DateSelected?.ToString("MM-dd-yyyy");
                string strActivity = m_currentSelected_Activity.ActivityText;

               

                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].AttendanceList.Any()  && m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {
                        var query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("DateString == @0", strChurchDate);
                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("Activity == @0", strActivity);

                        if (query_dateandstatus.Count() != 0 && query_activity.Count() != 0)
                        {

                            m_lstdefaultTableRowsCopy[i].Church_Last_Attended = strChurchDate;
                            m_lstdefaultTableRowsCopy[i].ActivityText = strActivity;

                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }


            }
            else if (Activity && ActivityDate) // Activity, Activity Date
            {
                string strActivityDate = m_ActivityDateSelected?.ToString("MM-dd-yyyy");

                string strActivity = m_currentSelected_Activity.ActivityText;
            


                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {

                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("DateString == @0 and ActivityText == @1", strActivityDate, strActivity);

                        if (query_activity.Count() != 0)
                        {


                            m_lstdefaultTableRowsCopy[i].ActivityText = strActivity;
                            m_lstdefaultTableRowsCopy[i].Activity_Last_Attended = strActivityDate;
                            m_lstQueryTableRows.Add(m_lstdefaultTableRowsCopy[i]);

                        }
                    }

                }
            }
            else if (Status && Activity) //Status and Activity
            {

                string strActivity = m_currentSelected_Activity.ActivityText;

              

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
                        IQueryable<Activity> query_activity;

                        if (bChurchStatusAttended)
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0 or Status = @1", strChurchStatus, strChurchStatusResponded);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityText == @0", strActivity);
                        }
                        else
                        {
                            query_dateandstatus = m_lstdefaultTableRowsCopy[i].AttendanceList.AsQueryable().Where("Status == @0", strChurchStatus);
                            query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityText == @0", strActivity);
                        }

                        if (query_dateandstatus.Count() != 0 && query_activity.Count() != 0)
                        {

                            m_lstdefaultTableRowsCopy[i].ChurchStatus = strChurchStatus;
                            m_lstdefaultTableRowsCopy[i].ActivityText = strActivity;

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
                        IQueryable<Activity> query_activity;

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
                        // change column header(church last attended) to reflect the last date a follow-up was generated for attendee
                        lblChurchLastAttended.Content = "Follow-Up last generated";

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
                string strActivity = m_currentSelected_Activity.ActivityText;

                string listname = m_currentSelected_Activity.ListName;



                for (int i = 0; i <= m_lstdefaultTableRowsCopy.Count - 1; i++)
                {

                    if (m_lstdefaultTableRowsCopy[i].ActivityList.Any() )
                    {

                        var query_activity = m_lstdefaultTableRowsCopy[i].ActivityList.AsQueryable().Where("ActivityText == @0 and ListName == @1", strActivity, listname);

                        if (query_activity.Any() )
                        {


                            m_lstdefaultTableRowsCopy[i].ActivityText = strActivity;
                           
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


            if ((GridsTabControl.SelectedItem as TabItem).Name == "Overview")
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
                    

                    m_currentSelected_Activity = null;
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



        private void BtnDeleteInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            IEnumerable<Attendance_Info> AttendanceInfoRow_select = m_AttendeeInfo_grid.SelectedItems.Cast<Attendance_Info>();
            IEnumerable<Activity> ActivityRow_select = m_Activity_grid.SelectedItems.Cast<Activity>();

            if (AttendanceInfoRow_select.Any())
            {

                DeleteRecordFromAttendanceInfoTable(AttendanceInfoRow_select);




                MessageBox.Show("Attendance record removed successfully.\n\nChanges have not been saved to the database until the Save button is clicked.", "Records removed", MessageBoxButton.OK, MessageBoxImage.None);

            }
            else if (ActivityRow_select.Any())
            {


                DeleteRecordFromActivitiesTable(ActivityRow_select);




                MessageBox.Show("Activity record removed successfully.\n\nChanges have not been saved to the database until the Save button is clicked.", "Records removed", MessageBoxButton.OK, MessageBoxImage.None);
            }
            else
            {
                MessageBox.Show("Must select at least one row", "Select one record", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Cursor = Cursors.Arrow;
        }

        private void DeleteRecordFromActivitiesTable(IEnumerable<Activity> row_select)
        {
            List<Activity> rowsToBeDeleted = new List<Activity>(row_select);
            //Activity queryActivity, AttActivityInforec;

            foreach (Activity dr in rowsToBeDeleted)
            {

                m_dbContext.Activities.Local.Remove(dr);
                m_default_row_selected.ActivityList.Remove(dr);
            }
                
            

              

                // find the activity state as deleted and get the ActivityText from it.
           

            m_lstContextActivities = AddALLActivitiesFromContextToActivityTree();

            var lastActivity = (from rec in m_default_row_selected.ActivityList
                                orderby rec.Date descending
                                select rec).ToList().FirstOrDefault();

          

            //update default row with new latest activity
            if (lastActivity != null)
            {
                m_default_row_selected.Activity_Last_Attended = lastActivity.DateString;
                m_default_row_selected.ActivityText = lastActivity.ActivityText;
            }
            

            if (!m_default_row_selected.ActivityList.Any())
            {
                m_default_row_selected.ActivityText = "n/a";
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


        bool Check_for_dub_ActivityRec_inDBase(AttendanceTableRow dr)
        {
            var queryifActivityExistList = m_default_row_selected.ActivityList.SingleOrDefault(rec => rec.ActivityText == m_currentAdded_Activity.ActivityText && rec.Date == m_ActivityDateSelectedPr);
            if (queryifActivityExistList != null)
            {

                    dataGrid_prospect.Focus();
                    int id = dr.AttendeeId;
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
            else
                return false;

        }
        private void btnPanelAddActivity_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            List<AttendanceTableRow> Checklist = new List<AttendanceTableRow>() { };

            // get checked mark in activity column
            Checklist = getListOfCheckedAttendees("Activity");


            if (m_currentAdded_Activity != null)
            {
                foreach (AttendanceTableRow dr in Checklist)
                {
                    // Find defaultrow that correspond to the attendance row attendeeID
                    m_default_row_selected = m_lstdefaultTableRows.SingleOrDefault(x => x.AttendeeId == dr.AttendeeId);

                    bool bcheckdupInfo = Check_for_dub_ActivityRec_inDBase(dr);
                    if (bcheckdupInfo)
                    {
                        MessageBox.Show("A record with the same date already exist in the database, choose a difference date.", "Duplicate date record found", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }
                    else
                    {
                      

                            // create new activity
                            Activity new_ap = new Activity();

                            new_ap.Date = m_ActivityDateSelectedPr;
                            new_ap.AttendeeId = dr.AttendeeId;
                            new_ap.ActivityText = m_currentAdded_Activity.ActivityText;
                            new_ap.ListName = m_currentAdded_Activity.ListName;

                            //modify activity text before written to database context
                            string tmp_text = FormatActivityText(new_ap.ActivityText);
                            new_ap.ActivityText = tmp_text;

                           if (!m_dbContext.Activities.Contains(new_ap))
                            m_dbContext.Activities.Add(new_ap); // add activity to database context

                            // change activity string back to original string
                            new_ap.ActivityText = m_currentAdded_Activity.ActivityText;

                            if (!m_default_row_selected.ActivityList.Contains(new_ap))
                                m_default_row_selected.ActivityList.Add(new_ap); //add activtiy to attendee Activity list

                            var lastActivity = (from rec in m_default_row_selected.ActivityList
                                                orderby rec.Date descending
                                                select rec).ToList().FirstOrDefault();

                                                        //Show Attendee's latest activity under the Activity in the Default Table
                            if (lastActivity != null)
                            {
                                m_default_row_selected.ActivityText = lastActivity.ActivityText;
                                m_default_row_selected.Activity_Last_Attended = lastActivity.DateString;
                            }
                            else
                            {
                                m_default_row_selected.ActivityText = new_ap.ActivityText;
                                m_default_row_selected.Activity_Last_Attended = new_ap.DateString;
                            }
                        


                     
                    }


                    //clean up all placeholders
                    dr.ActivityChecked = "";
                    foreach (Attendee at in m_dbContext.Attendees.Local)
                    {
                        if (at.IsActivityChecked == true)
                            at.IsActivityChecked = false;
                    }
                    //Add any new activities not in the current activity tree
                    m_lstContextActivities = AddALLActivitiesFromContextToActivityTree();

                } // end foreach


                m_ctbActivityProspect.UncheckAll();
                dpHeaderActivityPr.Text = "";
                btnPanelAddActivity.IsEnabled = false;
                m_currentAdded_Activity = null;
                m_ActivityDateSelectedPr = null;

                MessageBox.Show("Activity successfully added to selected attendee(s) profile", "Activity Added", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                Display_AttendeeListTable_in_Grid();
            }

            

            Cursor = Cursors.Arrow;
        }

        private List<ComboTreeNode> GetListOfTreeNodesFromAttendeeActivityList(DefaultTableRow attendee)
        {
            List<ComboTreeNode> tmp_listNodes = new List<ComboTreeNode>();

            foreach (Activity dc_activity in attendee.ActivityList)
            {
                if (dc_activity.ActivityText != "")
                {


                    string tmp_text = FormatActivityText(dc_activity.ActivityText);

                    string new_str = tmp_text.Split('-').LastOrDefault();

                    ComboTreeNode new_node = new ComboTreeNode
                        {
                            Header = new_str,
                            
                        };

                        tmp_listNodes.Add(new_node);

                   

                }
            }


            return tmp_listNodes;
        }
        private void FindandAddRecursively(ComboTreeNode node, IEnumerable<global::ComboTreeNode> node_to_find)
        {
            var lst = new List<global::ComboTreeNode>(node_to_find);

            if ((string)node.Header == lst.FirstOrDefault().Parent.Text) // Parent found in tree
            {
                ComboTreeNode ctn = new ComboTreeNode { Header = node_to_find.FirstOrDefault().Text };

                node.Items.Add(ctn); // add to parent as child item

            }

            foreach (ComboTreeNode subitem in node.Items)
            {
                FindandAddRecursively(subitem, node_to_find);
            }
        }
        private void Find_and_addNode(ref List<ComboTreeNode> tree,IEnumerable<global::ComboTreeNode> selected_node)
        {
            //1. find selected_node's parent in tree
            //2. add selected node to index as child

            //1.
            string selected_node_Text = "";
            List<ComboTreeNode> lstNodes = new List<ComboTreeNode>();

            int i = 0;

            if (tree[0] == null)
            {
                var ptrNode = new global::ComboTreeNode();
                //1.  tree is empty, add selected node and its relationships
                //2. find top level parent of selected node

                
                for ( ptrNode = selected_node.FirstOrDefault(); ptrNode !=null; ptrNode = ptrNode.Parent)
                {
                    if (ptrNode == null) break;// found parent

                    selected_node_Text = ptrNode.Text;
                    ComboTreeNode newnode = new ComboTreeNode { Header = selected_node_Text };

                   lstNodes.Add(newnode);

                    
                    
                }
                lstNodes.Reverse();
                // swap arry around so that levels are Parent->child
                for (int j = 0; j <= lstNodes.Count() - 1;j++)
                {

                    lstNodes[j].Level = j;
                }

                m_lstContextActivities = Array_to_Tree(lstNodes); // put list in correct relationship
                return;
            }


            foreach (ComboTreeNode node in tree)
            {
                FindandAddRecursively(node, selected_node);
            }

             
        
        }
        private void AddAttendeeActivityToActivityTree()
        {

            // 1. Get Added activity nodes and the relationships between them.
            // 2. Itterate through the filter activity tree and add the (Added activity) to the list with relationships

            IEnumerable<global::ComboTreeNode> chkNodes = m_ctbActivityProspect.CheckedNodes;

            List<ComboTreeNode> tmp_tree = new List<ComboTreeNode>();

            Find_and_addNode(ref m_lstContextActivities, chkNodes);

            // load the tree structure into the dropdown
            LoadActivityComboTree(m_lstContextActivities);

         

        }
        private string FormatActivityText(string activityText)
        {
             string new_str = "";
          

            new_str = new string((from c in activityText
                              where char.IsWhiteSpace(c) || char.IsLetterOrDigit(c) || !char.IsSymbol(c)
                              select c
                             ).ToArray());
          

            return new_str;
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


            if (m_currentSelected_Activity != null)
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

        private void BtnAddColumn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddColumnWindow AddColumnWindow = new AddColumnWindow();

            AddColumnWindow.ShowDialog();

            if (AddColumnWindow.GetColumnNames.Count > 0)
            {
                List<string> lst = AddColumnWindow.GetColumnNames;
            }
                

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
                       // m_isFirstNamefiltered = false;
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
              //  m_isFirstNamefiltered = false;
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
               //m_isLastNamefiltered = false;
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
                   // m_isFilterByDateChecked = true;
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
                    dr["Activity"] = row.ActivityText;
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
                    foreach (Activity activityrow in row.ActivityList)
                    {
                        DataRow activitydr = activityDT.NewRow();

                        activitydr["ActivityId"] = activityrow.ActivityId;
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
                m_currentSelected_Activity = null;
                m_ctbActivity.UncheckAll();
                BuildQuery_and_UpdateGrid();
            }
        }


        private void dataGrid_prospect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BtnPanelActivityList_Click(object sender, RoutedEventArgs e)
        {
             WndActivityList activity_win = new WndActivityList(m_lstCurrActivityListNodes, m_ActivityListPath, m_followUpWeeks);
            activity_win.ShowDialog();

            if (activity_win.GetTreeChanged)
            {
                if (activity_win.GetTree != null)
                    m_lstCurrActivityListNodes = new List<ComboTreeNode>(activity_win.GetTree);

                if (m_lstCurrActivityListNodes.Any() )
                {
                    //save full path for next time the user load the program
                    m_ActivityListPath = activity_win.GetFilePath;
                    txtbActivityListName.Text = GetListName();
                    LoadActivityProspectComboTree(m_lstCurrActivityListNodes); //Load the combo tree boxes with the new tree
                }
                
                    

            }

        }

        private void dataGrid_prospect_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            m_alist_tree_changed = true;
        }
    }

}





