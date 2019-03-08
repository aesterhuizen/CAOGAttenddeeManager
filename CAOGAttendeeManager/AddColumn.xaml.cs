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
    /// Interaction logic for AddColumn.xaml
    /// </summary>
    public partial class AddColumn : Window
    {
        public AddColumn()
        {
            InitializeComponent();

            btnAdd.IsEnabled = false;
            btnRemove.IsEnabled = false;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
           
        }

       
        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
