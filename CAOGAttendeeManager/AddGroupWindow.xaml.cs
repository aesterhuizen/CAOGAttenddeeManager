using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WndAddGroup : Window
    {
        public IEnumerable<TreeNode> getTree
        {
            get
            {
                if (m_tree_changed)
                    return m_ActivitiesTreeView;
                else
                    return null;
            }

        }

        public bool getTreeChanged => m_tree_changed;


        private bool m_tree_changed = false;

        private IEnumerable<TreeNode> m_ActivitiesTreeView = null;

        private int m_header_idx = 0; //index of header
        private int m_group_idx = 0; //index of group
        private int m_task_idx = 0; //index of task
        private int m_subtask_idx = -1; //index of subtask
       
        private void InitTree(List<ActivityHeader> tree)
        {
            trvActivities.BeginInit();
            //Add ComboTreeNodes to ComboTreeBox Treeview
            foreach (var header in tree)
            {
                //ActivityHeader

                TreeNode parent = new TreeNode() { Header = header.Name };
                

                //ActivityGroups
                foreach (var group in header.Groups)
                {
                    TreeNode group_node = new TreeNode() { Header = group.ActivityName };

                    parent.Items.Add(group_node);
                    
                    //ActivityTask
                    foreach (var task in group.lstActivityTasks)
                    {
                        TreeNode taskNode = new TreeNode() { Header = task.TaskName, Description = task.Description };
                        group_node.Items.Add(taskNode);

                        //subTask
                        foreach (var subtask in task.lstsubTasks)
                        {
                            TreeNode subTaskNode = new TreeNode() { Header = subtask.TaskName, Description = subtask.Description };
                            taskNode.Items.Add(subTaskNode);
                    
                        }
                    }

                }
                trvActivities.Items.Add(parent);
            }
           
            trvActivities.EndInit();
            
        }
        public WndAddGroup(List<ActivityHeader> tvCurrent)
        {
            InitializeComponent();
            InitTree(tvCurrent);

            //trvActivities.ItemsSource = tvCurrent;
            //m_ActivitiesTreeView = tvCurrent;
            //m_tree_changed = false;
            btnAdd.IsEnabled = false;
            
         
        }

      
        private void BtnApply_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            
            Close();
            
        }


   
      

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m_tree_changed = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            btnApply.IsEnabled = false;
            btnAdd.IsEnabled = false;
            btnDelete.IsEnabled = false;
            btnNewFunctionalGrp.IsEnabled = false;
            txtActivityDescription.IsEnabled = false;
           
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            int idx = 0;
            Cursor = Cursors.Wait;

           
            TreeNode selected_node = (TreeNode)trvActivities.SelectedItem;
           


            if (selected_node != null)
            {
                
                if (!selected_node.Items.Contains(selected_node))
                {
                    TreeNode new_item = new TreeNode { Header = txtActivityName.Text, Description = txtActivityDescription.Text };
                    trvActivities.BeginInit();
                    selected_node.Items.Add(new_item);
                    trvActivities.EndInit();
                    m_tree_changed = true;

                    m_ActivitiesTreeView = trvActivities.Items.Cast<TreeNode>();
                    txtActivityName.Text = "";
                    txtActivityDescription.Text = "";
                }

            }
            else
            {
                MessageBox.Show("Must select an item first.", "Select an item", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
          
            if (m_tree_changed)
                btnApply.IsEnabled = true;
            else
                btnApply.IsEnabled = false;

            Cursor = Cursors.Arrow;
        }



      
        private void txtActivityName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtActivityName.Text != "")
            {
                btnAdd.IsEnabled = true;
                btnNewFunctionalGrp.IsEnabled = true;
                txtActivityDescription.IsEnabled = true;
            }
            else
            {
                txtActivityDescription.IsEnabled = false;
                btnAdd.IsEnabled = false;
                btnNewFunctionalGrp.IsEnabled = false;
            }
                

        }



        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected_node = (TreeNode)trvActivities.SelectedItem;

            if (selected_node != null)
            {
                if (selected_node.Parent.GetType() == typeof(System.Windows.Controls.TreeView) ) // is a toplevel node
                {
                    int idx = trvActivities.Items.IndexOf(selected_node);

                    trvActivities.BeginInit();
                    trvActivities.Items.RemoveAt(idx);
                    trvActivities.EndInit();
                    m_ActivitiesTreeView = trvActivities.Items.Cast<TreeNode>();
                    m_tree_changed = true;
                }
                else if (selected_node.Parent.GetType() == typeof(CAOGAttendeeManager.TreeNode) )
                {
                    TreeNode Parent = (TreeNode)selected_node.Parent;
                    int idx = Parent.Items.IndexOf(selected_node);

                    trvActivities.BeginInit();
                    Parent.Items.RemoveAt(idx);
                    trvActivities.EndInit();
                    m_ActivitiesTreeView = trvActivities.Items.Cast<TreeNode>();
                    m_tree_changed = true;
                }
                

                
                
            }


            if (m_tree_changed)
                btnApply.IsEnabled = true;
            else
                btnApply.IsEnabled = false;

        }

        private void trvActivities_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = sender as TreeView;

            TreeNode selitem = (TreeNode)item.SelectedItem;

            if (selitem != null )
            {
                txtTaskDescription.Text = selitem.Description;
                btnDelete.IsEnabled = true;
              
            }
            

        }


        private void btnNewFunctionalGrp_Click(object sender, RoutedEventArgs e)
        {
            
                TreeNode new_item = new TreeNode
                {
                    Header = txtActivityName.Text,
                    Description = txtActivityDescription.Text

                };
                trvActivities.Items.Add(new_item);
                m_tree_changed = true;

                m_ActivitiesTreeView = trvActivities.Items.Cast<TreeNode>();
                txtActivityName.Text = "";
                txtActivityDescription.Text = "";
        
        }

      
    }

    public class TreeNode : TreeViewItem
    {
        public string Description { get; set; }

        public TreeNode()
        {
            Description = "";

        }

    }
   
}
