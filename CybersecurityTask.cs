namespace ChatbotCybersecurity_POE3
{
    public class CybersecurityTask
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasReminder { get; set; }
        public DateTime ReminderDate { get; set; }
        public string ReminderText { get; set; }

        public RelayCommand DeleteCommand => new RelayCommand(DeleteTask);

        public void SetReminder(int days)
        {
            ReminderDate = DateTime.Now.AddDays(days);
            ReminderText = $"Reminder: Due on {ReminderDate:MMM dd}";
            HasReminder = true;
        }

        private void DeleteTask(object parameter)
        {
            // Handled by parent window
        }
    }


}