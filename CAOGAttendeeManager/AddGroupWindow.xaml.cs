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
using System.Windows.Shapes;

namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WndAddGroup : Window
    {
        public WndAddGroup(ref List<ActivityHeader> trv_activities, int windowMode, ActivityPair activity_pair)
        {
            InitializeComponent();

            m_ActivitiesTreeview = trv_activities;
            m_win_mode = windowMode;
           
            m_activityPair = activity_pair;

            
            switch (windowMode)
            {
               
                case 0: //add new activity group
                    Title = "Add new group...";
                    ShowNewGroup_Panel();
                    
                    break;
                case 1: //add activity to group
                    Title = $"Add New Activity";
            
                    Show_AddNewActivity_Panel();
                    break;
                case 2: // add new header
                    Title = "Add new header...";
                    ShowNewHeader_Pandel();
                    break;

                default:
                    lblActivity.Content = "Add New activity:";
                    break;

            }
         
        }

      
        private void ShowNewHeader_Pandel()
        {
            lblActivity.Content = "New activity group name:";
            lblDescription.Visibility = Visibility.Hidden;
            txtTaskDescription.Visibility = Visibility.Hidden;
        }
        private void ShowNewGroup_Panel()
        {

            lblActivity.Content = "New activity group name:";
            lblDescription.Visibility = Visibility.Hidden;
            txtTaskDescription.Visibility = Visibility.Hidden;

        }
        private void Show_AddNewActivity_Panel()
        {
            lblActivity.Content = "New activity name:";
            lblDescription.Visibility = Visibility.Visible;
            txtTaskDescription.Visibility = Visibility.Visible;
        }
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            //FIX ME
            //Cursor = Cursors.Wait;

            //switch (m_win_mode)
            //{
            //   case 0: //Add activityGroup
            //       // int idx = m_ActivitiesTreeview.IndexOf()
            //      //  var groupExist = m_ActivitiesTreeview[idx].Groups.SingleOrDefault(a => a.ActivityName == txtActivityName.Text);
            //        //if (groupExist == null)
            //        //{
            //        //   // m_ActivitiesTreeview.Add(new ActivityGroup { Parent = m_ActivitiesTreeview[idx].Name , ActivityName = txtActivityName.Text });
            //        //    GetActivitiesCount++;
                     
            //        //    Close();
            //        //}
            //        //else
            //        //{
            //        //    MessageBox.Show("Activity group already exist.","Activity already exist",MessageBoxButton.OK,MessageBoxImage.Stop);

            //        //}
                    
            //        break;
            //    case 1: //Add new activity (task)

            //       // var a_group = m_ActivitiesTreeview.SingleOrDefault(idx => idx.ActivityName == m_activityPair.ActivityGroup);

                    
            //        //add new task to group
            //        if (m_activityPair.ActivityGroup != "" && m_activityPair.ParentTaskName == "")
            //        {
                        
            //            // if task already exist
            //          // var task_exist = a_group.lstActivityTasks.SingleOrDefault(at => at.TaskName == txtActivityName.Text);
            //            if (task_exist == null)
            //            {
            //                //activity is a child so make it a task of the current selected group task in the activities treeview


            //                ActivityTask newTask = new ActivityTask() { Parent = a_group.ActivityName, TaskName = txtActivityName.Text, Description = txtTaskDescription.Text };
            //                a_group.lstActivityTasks.Add(newTask);
            //                GetActivitiesCount++;




            //                Close();

            //            }
            //            else
            //            {
            //                MessageBox.Show("Activity already exist.", "Activity already exist", MessageBoxButton.OK, MessageBoxImage.Stop);
            //            }

            //        }
            //        // add new sub task
            //        else if (m_activityPair.ActivityGroup != "" && m_activityPair.ParentTaskName != "" && m_activityPair.ChildTaskName == "")
            //        {
            //            var task = a_group.lstActivityTasks.SingleOrDefault(ast => ast.TaskName == m_activityPair.ParentTaskName);
            //            int task_idx = a_group.lstActivityTasks.IndexOf(task);

            //                //activity is a child so make it a subtask of the current selected task in the activities treeview
            //                var subtask = task.lstsubTasks.SingleOrDefault(at => at.TaskName == txtActivityName.Text);
            //            if (subtask == null)
            //            {

            //                ActivityTask newsubTask = new ActivityTask() { Parent = "", TaskName = txtActivityName.Text, Description = txtTaskDescription.Text };
            //                task.lstsubTasks.Add(newsubTask);
            //                GetActivitiesCount++;

            //                Close();
            //            }
            //            else
            //            {
            //                MessageBox.Show("Activity already exist.", "Activity already exist", MessageBoxButton.OK, MessageBoxImage.Stop);
            //            }
                        
            //        }
            //        else
            //        {
            //            var task = a_group.lstActivityTasks.SingleOrDefault(ast => ast.TaskName == m_activityPair.ParentTaskName);
            //            int task_idx = a_group.lstActivityTasks.IndexOf(task);

            //            //activity is a child so make it a subtask of the current selected task in the activities treeview
            //            var subtask = task.lstsubTasks.SingleOrDefault(at => at.TaskName == txtActivityName.Text);
            //            if (subtask == null)
            //            {

            //                ActivityTask newsubTask = new ActivityTask() { Parent = "", TaskName = txtActivityName.Text, Description = txtTaskDescription.Text };
            //                task.lstsubTasks.Add(newsubTask);
            //                GetActivitiesCount++;

            //                Close();
            //            }
            //            else
            //            {
            //                MessageBox.Show("Activity already exist.", "Activity already exist", MessageBoxButton.OK, MessageBoxImage.Stop);
            //            }
            //        }

            //        break;
            //    case 2: //Add header group
            //        var headerExist = m_ActivitiesTreeview.SingleOrDefault(a => a.Name == txtActivityName.Text);
            //        if (headerExist == null)
            //        {
            //            m_ActivitiesTreeview.Add(new ActivityHeader { Name = txtActivityName.Text });
            //            GetActivitiesCount++;

            //            Close();
            //        }







            //        break;

            //    default:
            //        Show_AddNewActivity_Panel();
            //        break;

            //}

            //Cursor = Cursors.Arrow;

            
            
        }

        public int GetActivitiesCount { get; private set; } = 0;

        // private variables
        private List<ActivityHeader> m_ActivitiesTreeview;
        private int m_win_mode = 0;
        private bool m_ActivityListChanged = false;
        private ActivityPair m_activityPair;

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

   
   
}
