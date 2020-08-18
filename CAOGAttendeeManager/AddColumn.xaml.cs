using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;

namespace CAOGAttendeeManager
{
    /// <summary>
    /// Interaction logic for AddColumn.xaml
    /// </summary>
    public partial class AddColumnWindow : Window
    {
        public List<string> GetColumnNames { get; } = new List<string>() { };

        public AddColumnWindow()
        {
            InitializeComponent();

            btnAdd.IsEnabled = false;
            btnRemove.IsEnabled = false;
        }

        private void BtnAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            lstColNames.Items.Add(txtColAdd.Text);
            GetColumnNames.Add(txtColAdd.Text);
            txtColAdd.Text = "";

            btnRemove.IsEnabled = true;

        }

       
        private void BtnRemove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            lstColNames.Items.Remove(txtColAdd.Text);
        }

        private void BtnOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GetColumnNames.Clear();
            Close();
        }

     

       

        private void txtColAdd_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtColAdd.Text != "")
            {
                if (btnAdd != null)
                    btnAdd.IsEnabled = true;
            }
            else
            {
                if (btnAdd != null)
                    btnAdd.IsEnabled = false;

             
            }
                
        }


        private void LstColNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = sender as ListBox;

            if ((string)selection.SelectedItem != null)
            {
                txtColAdd.Text = (string)selection.SelectedItem;
                btnEdit.IsEnabled = true;
            }
            else
            {
                btnEdit.IsEnabled = false;
            }
                
         
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {


            lstColNames.Items[lstColNames.SelectedIndex] = txtColAdd.Text;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            btnEdit.IsEnabled = false;

        }
    }
}
