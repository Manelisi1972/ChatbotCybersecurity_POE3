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

namespace ChatbotCybersecurity_POE3
{
   
    public partial class TaskDialog : Window
    {

        public string TaskTitle => TitleBox.Text;
        public string TaskDescription => DescriptionBox.Text;
        public bool SetReminder => ReminderCheck.IsChecked ?? false;
        public int ReminderDays => int.TryParse(DaysBox.Text, out int days) ? days : 7;
        public TaskDialog()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                MessageBox.Show("Please enter a task title.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
