using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WndActivityList : Window
    {
       
        public string GetFollowUpWeeks { get; set; } = "";

      
        public bool isListSaved { get; private set; } = false;
        public IEnumerable<ComboTreeNode> GetTree
        {
            get
            {
               return m_ActivitiesTreeView;
              
            }

        }

        public string GetFileName { get; private set; }
        public bool GetTreeChanged { get; private set; } = false;

        public string GetFilePath { get; private set; } = "";

       //public string GetOldPath { get; private set; } = "";
        public WndActivityList() { }

        private IEnumerable<ComboTreeNode> m_ActivitiesTreeView = new List<ComboTreeNode>();

        // variable that hold tree of headers read from XML file
      // private List<ActivityHeader> m_lstHeadersFromXML = new List<ActivityHeader>() { };


     
            
        public void InitTree(List<ComboTreeNode> tree)
        {
            ComboTreeNode parent_ptr = null;
            ComboTreeNode root_ptr = null;
            ComboTreeNode child_ptr = null;

            List<ComboTreeNode> tmp_tree = new List<ComboTreeNode>() { };

            // tree is already initialized
            //if (trvActivities.DataContext == null)
            //{
            //    trvActivities.DataContext = tree;
            //}

            //return;

           
          

            for (int i = 0; i <= tree.Count - 1; i++)
                {

                    

                    if (tree[i].Level == 0)
                    {
                        if (root_ptr != null)
                        {
                            tmp_tree.Add(root_ptr);
                        

                        }
                        root_ptr = tree[i];
                        
                    } 
                    
                    /* This node is a child of the root node pointed to by root_ptr */
                    else if (tree[i].Level == root_ptr.Level + 1)
                    {

                      if(tree[i].Parent == null )
                        root_ptr.Items.Add(tree[i]);
                    

                          parent_ptr = tree[i];

                    }
                    /* This node is a child of a parent node pointed to by parent_ptr */
                    else if (tree[i].Level == parent_ptr.Level + 1)
                    {
                    if (tree[i].Parent == null)
                        parent_ptr.Items.Add(tree[i]); 

                         child_ptr = tree[i];
                    }
                    /* This node is a child of a child node pointed to by child_ptr
                     * parent_ptr becomes the node
                     */
                    else if (tree[i].Level == child_ptr.Level + 1)
                    {

                    if (tree[i].Parent == null)
                        child_ptr.Items.Add(tree[i]); 

                        parent_ptr = tree[i];
                    }


                }

            if (!tmp_tree.Contains(root_ptr))
                tmp_tree.Add(root_ptr);

            trvActivities.ItemsSource = tmp_tree;

            m_ActivitiesTreeView = trvActivities.ItemsSource.Cast<ComboTreeNode>();
            
            

        }

        private Timer aTimer = null;

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
               
                if(isListSaved == false)
                {
                    btnSaveList.IsEnabled = true;
                   
                }
                else
                {
                    btnSaveList.IsEnabled = false;
                   
                }

                //if (trvActivities.SelectedItem == null)
                //{
                //    btnEditItem.IsEnabled = false;
                //    btnDeleteItem.IsEnabled = false;
                //    btnAddItem.IsEnabled = false;

                //}
                //else
                //{
                //    btnEditItem.IsEnabled = true;
                //    btnDeleteItem.IsEnabled = true;
                //    btnAddItem.IsEnabled = true;                
                //}
                   

            });


        }
        public WndActivityList(List<ComboTreeNode> aryTree,string ListPath,string fweeks)
        {
            InitializeComponent();

             if (aryTree.Any() )
                InitTree(aryTree);

            GetFollowUpWeeks = fweeks;
            GetFilePath = ListPath;
           // GetOldPath = GetFilePath;

            if (GetFilePath != "")
            {
                string baseName = GetFilePath.Split('\\').Last();
                string ListName = baseName.Substring(0, baseName.Length - 4);
                lblListFilename.Content = ListName;
            }
            else
                lblListFilename.Content = "No list";

            btnAddItem.IsEnabled = false;


        }


        private void BtnApply_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            

            if (!isListSaved)
            {
                MessageBoxResult res = MessageBox.Show("Changes has been made to the list but not saved. Save Changes?", "Save Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);

                if (res == MessageBoxResult.Yes)
                {
                    Cursor = Cursors.Wait;
                    btnSaveList_Click(null, null);

                   
                    StopTimer();
                    Cursor = Cursors.Arrow;
                    Close();


                }
                else if (res == MessageBoxResult.No)
                {
                    GetTreeChanged = false;
                    StopTimer();
                    Close();

                }
                else if (res == MessageBoxResult.Cancel)
                {
                    GetTreeChanged = false;
                    Close();
                }

            }
            else
            {
                
                Close();
            }
           

        }


        private void RtbDescription_ContextOpening(object sender, ContextMenuEventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            if (rtb == null) return;

            ContextMenu contextMenu = rtb.ContextMenu;
            contextMenu.PlacementTarget = rtb;

            // This uses HorizontalOffset and VerticalOffset properties to position the menu,
            // relative to the upper left corner of the parent element (RichTextBox in this case).
            contextMenu.Placement = PlacementMode.RelativePoint;

            // Compute horizontal and vertical offsets to place the menu relative to selection end.
            TextPointer position = rtb.Selection.End;

            if (position == null) return;

            Rect positionRect = position.GetCharacterRect(LogicalDirection.Forward);
            contextMenu.HorizontalOffset = positionRect.X;
            contextMenu.VerticalOffset = positionRect.Y;

            // Finally, mark the event has handled.
            contextMenu.IsOpen = true;
            e.Handled = true;
        }


        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GetTreeChanged = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isListSaved = true;
           
            btnAddItem.IsEnabled = false;
            btnDeleteItem.IsEnabled = false;
            btnEditItem.IsEnabled = false;
           
            rtbDescription.IsEnabled = false;
            txtActivityName.IsEnabled = false;

            //mnuAddItem.IsEnabled = false;
            //mnuEditItem.IsEnabled = false;
           
            //mnuSaveList.IsEnabled = false;
            //mnuDeleteItem.IsEnabled = false;

            SetTimer();

           rtbDescription.ContextMenuOpening += new ContextMenuEventHandler(RtbDescription_ContextOpening);

           
          

        }

        private List<ComboTreeNode> Find_and_addNode(IEnumerable<ComboTreeNode> iNode, ComboTreeNode selected_node)
        {

            bool itemfound = false;
            bool addToplevelItem = false;

            List<ComboTreeNode> new_tree = new List<ComboTreeNode>(iNode);
            ComboTreeNode new_item;

            if (selected_node == null) // user want to create a new toplevel item
            {
                new_item = new ComboTreeNode {};
                addToplevelItem = true;
            }
            else
            {
                new_item = new ComboTreeNode { Header = "<new item>", Level = selected_node.Level + 1 };
            }

            if (addToplevelItem)
            {
                new_tree.Add(new_item);
            }
            else
            {
                foreach (ComboTreeNode n in new_tree)
                {
                    if (itemfound) break;
                    if (n == selected_node)
                    {
                        n.Items.Add(new_item);
                        itemfound = true;
                        break;
                    }

                    foreach (ComboTreeNode i in n.Items)
                    {

                        if (i == selected_node)
                        {
                            i.Items.Add(new_item);
                            itemfound = true;
                            break;
                        }
                    }
                }
            }
            

            return new_tree;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
           

            Cursor = Cursors.Wait;

           
            ComboTreeNode selected_node = (ComboTreeNode)trvActivities.SelectedItem;

            List<ComboTreeNode> new_tree = Find_and_addNode(m_ActivitiesTreeView, selected_node);

            if (selected_node != null)
            {

               if (trvActivities.ItemsSource != null)
                {


                    trvActivities.BeginInit();
                    
                    trvActivities.ItemsSource = new_tree;
                    trvActivities.Items.Refresh();

                    trvActivities.EndInit();
                    GetTreeChanged = true;
                    isListSaved = false;


                    txtActivityName.Text = "";

                }



            }
            else
            {
                MessageBox.Show("Must select an item first.", "Select an item", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


         

            Cursor = Cursors.Arrow;
        }






        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            ComboTreeNode selected_node = (ComboTreeNode)trvActivities.SelectedItem;

            if (selected_node != null)
            {
                if (selected_node.Parent.GetType() == typeof(TreeView)) // is a toplevel node
                {
                    int idx = trvActivities.Items.IndexOf(selected_node);

                    trvActivities.BeginInit();
                    trvActivities.Items.RemoveAt(idx);
                    trvActivities.EndInit();
                    m_ActivitiesTreeView = trvActivities.Items.Cast<ComboTreeNode>();
                    GetTreeChanged = true;
                    isListSaved = false;
                }
                else if (selected_node.Parent.GetType() == typeof(ComboTreeNode))
                {
                    ComboTreeNode Parent = (ComboTreeNode)selected_node.Parent;
                    int idx = Parent.Items.IndexOf(selected_node);

                    trvActivities.BeginInit();
                    Parent.Items.RemoveAt(idx);
                    trvActivities.EndInit();
                    m_ActivitiesTreeView = trvActivities.Items.Cast<ComboTreeNode>();
                    GetTreeChanged = true;
                    isListSaved = false;
                }

              


            }


            

        }

        private void trvActivities_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = sender as TreeView;

            ComboTreeNode selitem = (ComboTreeNode)item.SelectedItem;

          
            if (selitem != null)
            {
                rtbDescription.IsEnabled = true;
                txtActivityName.IsEnabled = true;

                btnAddItem.IsEnabled = true;
                btnEditItem.IsEnabled = true;
                btnDeleteItem.IsEnabled = true;

                //mnuAddItem.IsEnabled = true;
                //mnuEditItem.IsEnabled = true;
                //mnuDeleteItem.IsEnabled = true;


                if ((string)selitem.Header == "<new item>")
                {
                    TextRange rtbRange = new TextRange(rtbDescription.Document.ContentStart, rtbDescription.Document.ContentEnd);

                    rtbRange.Text = "<new description...>";
                  
                    txtActivityName.Text = (string)selitem.Header;
                    
                }
                else
                {
                    LoadRTBContent(ref selitem);
                    txtActivityName.Text = (string)selitem.Header;

                    
                  
                }
                
            }
            else
            {
                // no selection
                rtbDescription.IsEnabled = false; ;
                txtActivityName.IsEnabled = false;

                btnAddItem.IsEnabled = false;
                btnEditItem.IsEnabled = false;
                btnDeleteItem.IsEnabled = false;

                //mnuAddItem.IsEnabled = false;
                //mnuEditItem.IsEnabled = false;
                //mnuDeleteItem.IsEnabled = false;
            }


        }


        private void btnNewFunctionalGrp_Click(object sender, RoutedEventArgs e)
        {

          

           
            //pass a null in the last argument of the function Find_and_addNode will add a toplevel item to the tree
            List<ComboTreeNode> new_tree = Find_and_addNode(m_ActivitiesTreeView, null);
            
            GetTreeChanged = true;

            trvActivities.ItemsSource = new_tree;
            trvActivities.Items.Refresh();
            txtActivityName.Text = "";
           
           
          
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            
            
            ComboTreeNode node = (ComboTreeNode)trvActivities.SelectedItem;
            trvActivities.BeginInit();

           
            if (node !=null)
                node.Header = txtActivityName.Text;

            TextRange range;
            range = new TextRange(rtbDescription.Document.ContentStart, rtbDescription.Document.ContentEnd);
            if (node.rtbDescriptionMStream != null)
            {
                node.rtbDescriptionMStream.Position = 0;

                range.Save(node.rtbDescriptionMStream, DataFormats.Rtf);
                node.rtbDescriptionMStream.Position = 0;

             
            }
                
          


            GetTreeChanged = true;
            isListSaved = false;

            m_ActivitiesTreeView = trvActivities.Items.Cast<ComboTreeNode>();


         
            trvActivities.EndInit();


        }

        private void ActivitymnuAdd_Click(object sender, RoutedEventArgs e)
        {
            _ = System.Windows.Forms.Cursors.WaitCursor;


         

            _ = System.Windows.Forms.Cursors.Arrow;

        }

        private void ActivitymnuEdit_Click(object sender, RoutedEventArgs e)
        {
           

        }

        private void ActivitymnuDelete_Click(object sender, RoutedEventArgs e)
        {
            
        }


        private byte[] FormatByteArray(byte[] payload, string node_header,int header_level )
        {

            //int i = 0;

            //read_buffer = [ STX_seq + node_level + header_byte_array + escape_seq + payload + ETX_seq)]


            byte[] header_byte_array = Encoding.UTF8.GetBytes(node_header);
            byte node_level = (byte)header_level;
            int node_level_length = 1;

            byte[] ETX_seq = new byte[2] { 0x03,0x30 };
            byte[] STX_seq = new byte[2] { 0x02, 0x20 };
            
            byte[] escape_seq = new byte[2] { 0x0A, 0xA0 };
              
               
                byte[] tmp_array = new byte[STX_seq.Length + node_level_length + header_byte_array.Length + escape_seq.Length + payload.Length +  ETX_seq.Length];

            //construct tmp_array
            STX_seq.CopyTo(tmp_array, 0);
            tmp_array[STX_seq.Length] = node_level;
            header_byte_array.CopyTo(tmp_array, STX_seq.Length + node_level_length);

            escape_seq.CopyTo(tmp_array, STX_seq.Length+ node_level_length + header_byte_array.Length);
            payload.CopyTo(tmp_array, STX_seq.Length + node_level_length + header_byte_array.Length + escape_seq.Length);
            ETX_seq.CopyTo(tmp_array, STX_seq.Length + node_level_length + header_byte_array.Length + escape_seq.Length + payload.Length);


          

                return tmp_array;                

        
        }


        private void CallRecursive(TreeView treeView, string filepath, string listname)
        {
            
            
            // Open or create new file that wil hold the node's descriptions            
            FileStream fsDescriptions = new FileStream(filepath, FileMode.Create, FileAccess.ReadWrite);
            
            
            // Print each node recursively.  
            ItemCollection items = treeView.Items;
           
            foreach (ComboTreeNode n in items)
            {
               
                PrintRecursive(n, ref fsDescriptions, listname);
              
               
            }
            fsDescriptions.Close();
        }
      

        private void PrintRecursive(ComboTreeNode node, ref FileStream fs, string listname)
        {

            //Print goodies


            string node_header_string = (string)node.Header + "_" + listname;
            int node_level = node.Level;

           //node.Parent;

            if (node_header_string != "" || node.rtbDescriptionMStream.ToArray().Any())
            {

                byte[] data = FormatByteArray(node.rtbDescriptionMStream.GetBuffer(), node_header_string, node_level);
                fs.Write(data, 0, data.Length);

            }
           
            //loop through the tree of activities
            foreach (ComboTreeNode tn in node.Items)
            {

                PrintRecursive(tn, ref fs, listname);
                
            }
        }
        public void Save_ChurchActivities_To_datFile(IEnumerable<ComboTreeNode> activityList, string filepath, string listname)
        {

            CallRecursive(trvActivities, filepath, listname);

           
        }
     
        private List<ComboTreeNode> Load_ChurchActivities_From_File(string filePath, Stream fstream)
        {

            List<ComboTreeNode> tree_array = new List<ComboTreeNode>() { };


            try
            {

               
                if (File.Exists($"{filePath}"))
                {

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
                    int idx= 0;
                    int bytes_read = 0;
                    string node_header;
                    int node_level = 0;
                    int payload_length = 0;


                    ComboTreeNode node;
                    MemoryStream payload_data;

                    while ( (bytes_read = fstream.Read(read_buffer,offset_size,read_buffer.Length-offset_size)) > 0 )
                    {
                        
                        //loop over read buffer
                        for (idx=0;idx<=read_buffer.Length-1;idx++)
                        {
                            // read untill get a STX
                            if (read_buffer[idx] == 0x02 && read_buffer[idx+1] == 0x20)
                                STXidx = idx; //STXidx is the beginning of the STX sequence index in the read buffer array
                                
                            // read until you get an ETX symbol
                            if (read_buffer[idx] == 0x03 && read_buffer[idx+1] == 0x30)
                                ETXidx = idx+1; //ETXidx is the end of the ETX sequence index in the read buffer array and at the end of 1 message

                            // if an STX and ETX is found decode the the data and find the next message (STX ETX)
                            if (read_buffer[STXidx] == (byte)ArrayFormat.STX && read_buffer[ETXidx] == 0x30)
                            {
                                
                                  
                                    //we found the beginning of the payload
                                for (int i=STXidx;i<=ETXidx-2;i++)
                                {
                                    if (read_buffer[i] == 0x0A && read_buffer[i + 1] == 0xA0)
                                    {

                                        int header_size = (i - 1) - (STXidx + 1 + node_level_length);

                                        node_level = Convert.ToInt16(read_buffer[STXidx + 2]);

                                        node_header = Encoding.UTF8.GetString(read_buffer, STXidx+3 /*beggining offset of node header*/, header_size);



                                        string[] ary_nodeHeader = node_header.Split('_');
                                        string activityName = ary_nodeHeader[0];
                                        string listname = ary_nodeHeader[1];

                                        node = new ComboTreeNode { Header = activityName, Level = node_level, activityList = listname };

                                        if (read_buffer[i + 2] == (byte)ArrayFormat.ETX)
                                        {
                                            payload_length = 0;
                                            payload_data = new MemoryStream();
                                            node.rtbDescriptionMStream = payload_data;
                                        }
                                        else

                                        {
                                            payload_data = new MemoryStream();

                                            payload_length = (ETXidx - 2) - (i + 2);

                                            payload_data.Write(read_buffer, i + 2, payload_length);


                                            node.rtbDescriptionMStream = payload_data;
                                            string data = Encoding.UTF8.GetString(payload_data.GetBuffer(),0,payload_length);
                                        }

                                        tree_array.Add(node);
                                       
                                        break;
                                    }
                                }


                                
                                STXidx = ETXidx+1 ;
                                ETXidx = 0;

                            }
                            else if (idx == read_buffer.Length-1 && ETXidx == 0)   //we read until the end of the buffer but cannot find an end of message sequence ETX
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

        private void btnLoadList_Click(object sender, RoutedEventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            bool? result = null;
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "dat files (*.dat)|*.dat",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            result = openFileDialog.ShowDialog();

            if (result == true)
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;
                string filename = (openFileDialog.SafeFileName).Substring(0, openFileDialog.SafeFileName.Length - 4);
                Stream fileStream = openFileDialog.OpenFile();
               // m_old_filepath = GetFilePath;

                List<ComboTreeNode> tree = Load_ChurchActivities_From_File(filePath, fileStream);
                
                GetFilePath = filePath;
                InitTree(tree);
                lblListFilename.Content = filename;
                GetTreeChanged = true;
                isListSaved = true;
                //set the GetTree property to the current tree
                m_ActivitiesTreeView = trvActivities.Items.Cast<ComboTreeNode>();
               

            }

        }

        private void btnCreateList_Click(object sender, RoutedEventArgs e)
        {

            trvActivities.ItemsSource = null;
            trvActivities.DataContext = null;
            GetTreeChanged = false;
            btnNewGroup.IsEnabled = true;
            btnSaveList.IsEnabled = false;
            lblListFilename.Content = "Untitled*";
            txtActivityName.Text = "";
            rtbDescription.Document.Blocks.Clear();

            m_ActivitiesTreeView = trvActivities.Items.Cast<ComboTreeNode>();
            
        }

        private void SaveSettings()
        {

            List<XNode> lstdocNodes = new List<XNode>() { };
            var doc_root = new XElement("XmlDocument");
            XDocument DOMdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), doc_root);

            Cursor = Cursors.Wait;


            XElement ProgramSettingsElement = new XElement("ProgramSettings");
            XElement FollowUpElement = new XElement("FollowUpWeeks", new XAttribute("Weeks", GetFollowUpWeeks));
            XElement ListActivityPath = new XElement("ActivityList", new XAttribute("Path", GetFilePath));

            ProgramSettingsElement.Add(FollowUpElement);
            ProgramSettingsElement.Add(ListActivityPath);

            lstdocNodes.Add(ProgramSettingsElement);

            doc_root.Add(lstdocNodes);

            string settingPath = Directory.GetCurrentDirectory();
            settingPath += "\\settings.xml";

            try
            {


                var fsXML = new FileStream(settingPath, FileMode.Create, FileAccess.ReadWrite);
                // save document
                DOMdoc.Save(fsXML);
                fsXML.Close();





            }
            catch (Exception)
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("Something went wrong with the save of the settings file!");
            }

            Cursor = Cursors.Arrow;
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            StopTimer();

        }

        private void btnSaveList_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                CheckPathExists = false,
                CheckFileExists = false,
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "dat files (*.dat)|*.dat",
                FilterIndex = 1,
            };

            string settingPath = Directory.GetCurrentDirectory();
            settingPath += "\\settings.xml";

            

            if (saveFileDialog.ShowDialog() == true)
            {
                //Get the path of specified file
                string filePath = saveFileDialog.FileName;
                string filename = (saveFileDialog.SafeFileName).Substring(0, saveFileDialog.SafeFileName.Length - 4);

               
                Save_ChurchActivities_To_datFile(GetTree, filePath, filename);
              
                lblListFilename.Content = filename;
                GetFilePath = filePath;


           
                //GetTreeChanged = true;
                isListSaved = true;
                SaveSettings(); // save the list path to the settings file
            }



        }

        void LoadRTBContent(ref ComboTreeNode selected_node)
        {
            TextRange range;


           if (selected_node.rtbDescriptionMStream != null)
            {
                if (selected_node.rtbDescriptionMStream.ToArray().Any())
                {

                    range = new TextRange(rtbDescription.Document.ContentStart, rtbDescription.Document.ContentEnd);

                    range.Load(selected_node.rtbDescriptionMStream, DataFormats.Rtf);
                    selected_node.rtbDescriptionMStream.Position = 0;
                }
                else
                {
                    rtbDescription.Document.Blocks.Clear();



                }
            }
            


        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            
           
        }

        private void txtActivityName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtActivityName.Text =="")
            {
                btnAddItem.IsEnabled = false;
                btnEditItem.IsEnabled = false;
                //mnuEditItem.IsEnabled = false;
                //mnuAddItem.IsEnabled = false;
            }
            else
            {
                btnAddItem.IsEnabled = true;
                btnEditItem.IsEnabled = true;
                //mnuEditItem.IsEnabled = true;
                //mnuAddItem.IsEnabled = true;
            }
            
            
        }

    }



    
}
