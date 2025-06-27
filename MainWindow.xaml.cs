using System.Text;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;
using Path = System.IO.Path;
using System.Media;

namespace ChatbotCybersecurity_POE3
{

    public partial class MainWindow : Window
    {

        // Chatbot Core Fields
        private Dictionary<string, List<string>> keywordResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private string userName = "";
        private string lastTopic = "";
        private string favoriteTopic = "";
        private string logDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private string logFilePath = "";
        // Task Management Fields
        private List<CybersecurityTask> tasks = new List<CybersecurityTask>();
        private CybersecurityTask currentTaskForReminder;
        private DispatcherTimer reminderTimer;

        // Quiz Fields
        private List<QuizQuestion> quizQuestions = new List<QuizQuestion>();
        private int currentQuizQuestion = 0;
        private int quizScore = 0;
        private bool quizActive = false;

        // NLP Fields
        private Dictionary<string, string[]> keywordVariations = new Dictionary<string, string[]>
        {
            {"task", new[] {"task", "todo", "remind me to", "remember to", "add"}},
            {"reminder", new[] {"remind", "alert", "notify", "remember", "schedule"}},
            {"quiz", new[] {"quiz", "test", "game", "challenge", "questions"}},
            {"password", new[] {"password", "login", "credentials", "passphrase"}},
            {"phishing", new[] {"phishing", "scam", "fake email", "suspicious message"}},
            {"privacy", new[] {"privacy", "settings", "personal data", "information"}},
            {"2fa", new[] {"2fa", "two factor", "two-factor", "authentication"}},
            {"summary", new[] {"summary", "what have you done", "recent actions", "history"}}
        };

        // Activity Log Fields
        private List<ActivityLogEntry> activityLog = new List<ActivityLogEntry>();
        private const int MaxVisibleLogEntries = 5;
        private bool showFullLog = false;

        // Encouragement Messages
        private List<string> positiveEncouragements = new List<string>
        {
            "Don't worry, cybersecurity can seem tough at first, but I'm here to help!",
            "It's great that you're curious — asking questions is how you get stronger online!",
            "Even when it's frustrating, learning this stuff helps keep you and your info safe!"
        };
       

        public MainWindow()
        {
            InitializeComponent();
            InitializeComponent();
            InitializeChatbot();
            SetupTimers();
            InitializeQuiz();
            PlayWelcomeMessage();
        }

        #region Initialization Methods
        private void InitializeChatbot()
        {
            StoreReplies();
            Directory.CreateDirectory(logDirectory);
            AddChatMessage("WishiChatBot", "Welcome to the Cybersecurity Awareness Bot! 🔐", Colors.Blue);
            AddChatMessage("WishiChatBot", "Please enter your name to begin:", Colors.Blue);
        }

        private void SetupTimers()
        {
            // Status timer
            DispatcherTimer statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            statusTimer.Tick += (s, e) => { StatusText.Text = DateTime.Now.ToString("HH:mm:ss"); };
            statusTimer.Start();

            // Reminder timer
            reminderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            reminderTimer.Tick += CheckReminders;
            reminderTimer.Start();
        }

        private void PlayWelcomeMessage()
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "welcome.wav");
                if (File.Exists(fullPath))
                {
                    SoundPlayer player = new SoundPlayer(fullPath);
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                AddChatMessage("System", $"Error playing welcome sound: {ex.Message}", Colors.Red);
            }


        }

        private void AddChatMessage(string sender, string message, Color color)
        {
            ChatItems.Items.Add(new ChatMessage
            {
                Message = $"[{sender}]: {message}",
                TextColor = new SolidColorBrush(color),
                BackgroundBrush = sender == "WishiChatBot"
                    ? new SolidColorBrush(Color.FromArgb(20, color.R, color.G, color.B))
                    : Brushes.Transparent
            });

            // Auto-scroll to bottom
            var border = (Border)VisualTreeHelper.GetChild(ChatItems, 0);
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToEnd();
        }

        #endregion

        #region Core Chatbot Functionality
        private void ProcessUserInput()
        {
            string userInput = UserInputBox.Text.Trim();
            UserInputBox.Clear();

            if (string.IsNullOrEmpty(userInput))
                return;

            // Check if we need to get user's name
            if (string.IsNullOrEmpty(userName))
            {
                SetUserName(userInput);
                return;
            }

            // Check for activity log command
            if (ContainsAnyVariation(userInput, "summary"))
            {
                ShowActivityLogSummary();
                ActivityLogTab.IsSelected = true;
                return;
            }

            // Check for quiz command
            if (ContainsAnyVariation(userInput, "quiz") && !quizActive)
            {
                StartQuiz_Click(null, null);
                return;
            }

            // Check for task-related commands
            if (TryProcessTaskRequest(userInput))
                return;
            // Process cybersecurity queries
            ProcessCybersecurityQuery(userInput);
        }

        private void SetUserName(string name)
        {
            userName = name;
            logFilePath = Path.Combine(logDirectory, $"ChatHistory_{userName}.txt");

            if (File.Exists(logFilePath))
            {
                AddChatMessage("WishiChatBot", $"Welcome back, {userName}!", Colors.DarkBlue);
                string history = File.ReadAllText(logFilePath);
                AddChatMessage("History", history, Colors.Gray);
            }
            else
            {
                File.WriteAllText(logFilePath, $"Conversation history for {userName} - Started on {DateTime.Now}\n\n");
                AddChatMessage("WishiChatBot", $"Hi {userName}, let's start a new cybersecurity chat!", Colors.DarkBlue);
            }

            AddChatMessage("WishiChatBot", "You can ask about: Purpose, Password Security, Phishing Scams, Privacy", Colors.Cyan);
            LogActivity($"User session started for {userName}");
        }

        private void ProcessCybersecurityQuery(string userInput)
        {
            DetectSentiment(userInput);
            bool found = false;

            foreach (var pair in keywordResponses)
            {
                if (ContainsAnyVariation(userInput, pair.Key))
                {
                    lastTopic = pair.Key;
                    if (string.IsNullOrEmpty(favoriteTopic))
                    {
                        favoriteTopic = pair.Key;
                        AddChatMessage("WishiChatBot", $"Great! I'll remember that you're interested in {favoriteTopic}.", Colors.Green);
                    }

                }
                string response = GetRandomResponse(pair.Value);
                AddChatMessage("WishiChatBot", response, Colors.Cyan);
                File.AppendAllText(logFilePath, $"User: {userInput}\nBot: {response}\n\n");
                LogActivity($"Responded to query about {pair.Key}");
                found = true;
                break;
            }

            if (!found)
            {
                AddChatMessage("WishiChatBot", "I'm not sure I understand. Try asking about cybersecurity topics, tasks, or the quiz.", Colors.Red);
            }
        }

        private void DetectSentiment(string userInput)
        {
            if (userInput.Contains("worried") || userInput.Contains("scared"))
            {
                AddChatMessage("WishiChatBot", positiveEncouragements[0], Colors.DarkMagenta);
            }
            else if (userInput.Contains("curious") || userInput.Contains("interested"))
            {
                AddChatMessage("WishiChatBot", positiveEncouragements[1], Colors.Magenta);
            }
            else if (userInput.Contains("frustrated") || userInput.Contains("confused"))
            {
                AddChatMessage("WishiChatBot", positiveEncouragements[2], Colors.Magenta);
            }
        }


        private void ShowActivityLogSummary()
        {
            var recentActions = activityLog.Take(MaxVisibleLogEntries).ToList();
            if (recentActions.Count == 0)
            {
                AddChatMessage("WishiChatBot", "No recent activities to display.", Colors.Blue);
                return;
            }

            AddChatMessage("WishiChatBot", "Here's a summary of recent actions:", Colors.Blue);

            for (int i = 0; i < recentActions.Count; i++)
            {
                string message = $"{i + 1}. {recentActions[i].ActionDescription}";
                if (!string.IsNullOrEmpty(recentActions[i].Details))
                    message += $" ({recentActions[i].Details})";

                AddChatMessage("Activity Log", message, Colors.Gray);
            }
        }

        private void StoreReplies()
        {
            if (keywordResponses.Count == 0)
            {
                keywordResponses.Add("purpose", new List<string>
                {
                    "I'm here to help raise awareness about cybersecurity in a fun and friendly way.",
                    "My purpose is to educate you on staying safe online and avoiding digital threats.",
                    "Think of me as your cybersecurity buddy, helping you understand key online safety topics."
                });

                keywordResponses.Add("password", new List<string>
                {
                    "Always use a mix of uppercase, lowercase, numbers, and symbols in your passwords.",
                    "Avoid using the same password across multiple accounts.",
                    "Consider using a password manager to generate and store strong passwords."
                });

                keywordResponses.Add("phishing", new List<string>
                {
                    "Phishing scams often come via emails pretending to be from legitimate sources—double-check URLs before clicking!",
                    "Never share personal info through links in unexpected messages.",
                    "When in doubt, contact the organization directly instead of replying to suspicious emails."
                });

                keywordResponses.Add("privacy", new List<string>
                {
                    "Check app permissions regularly and limit what data you share.",
                    "Avoid oversharing personal info on social media—it can be used for identity theft.",
                    "Use encrypted messaging apps and review privacy settings on your accounts often."
                });

            }


        }


        private bool TryProcessTaskRequest(string userInput)
        {
            if (ContainsAnyVariation(userInput, "task") ||
                (ContainsAnyVariation(userInput, "reminder") && !ContainsAnyVariation(userInput, "show")))
            {
                string taskTitle = ExtractTaskTitle(userInput);
                if (string.IsNullOrEmpty(taskTitle))
                {
                    AddChatMessage("WishiChatBot", "What would you like me to remind you about?", Colors.Cyan);
                    return true;
                }
                string description = GetDefaultDescription(taskTitle);
                var newTask = new CybersecurityTask
                {
                    Title = taskTitle,
                    Description = description,
                    CreatedDate = DateTime.Now
                };

                // Check for time references
                DateTime? reminderDate = ExtractReminderDate(userInput);
                if (reminderDate.HasValue)
                {
                    int days = (int)(reminderDate.Value - DateTime.Now).TotalDays;
                    newTask.SetReminder(days);
                    LogActivity($"Reminder set for '{taskTitle}'", $"Due on {reminderDate.Value.ToShortDateString()}");
                    AddChatMessage("WishiChatBot", $"Reminder set for '{taskTitle}' on {reminderDate.Value.ToShortDateString()}.", Colors.Green);
                }
                else if (ContainsAnyVariation(userInput, "reminder"))
                {
                    currentTaskForReminder = newTask;
                    AddChatMessage("WishiChatBot", $"Task added: '{taskTitle}'. Would you like to set a reminder?", Colors.Green);
                    LogActivity($"Task added: '{taskTitle}'", "Reminder pending");
                }
                else
                {
                    AddChatMessage("WishiChatBot", $"Task added: '{taskTitle}'.", Colors.Green);
                    LogActivity($"Task added: '{taskTitle}'");
                }

                tasks.Add(newTask);
                RefreshTaskList();
                SaveTasks();
                return true;
            }

            return false;
        }

        private string ExtractTaskTitle(string userInput)
        {
            string pattern = @"\b(add|create|set|new|remind me to|remember to)\b";
            string cleanedInput = Regex.Replace(userInput, pattern, "", RegexOptions.IgnoreCase).Trim();
            cleanedInput = Regex.Replace(cleanedInput, @"\b(tomorrow|today|next week|\d+ days?)\b", "", RegexOptions.IgnoreCase).Trim();
            return cleanedInput.TrimEnd('?', '.', '!');
        }

        private DateTime? ExtractReminderDate(string userInput)
        {
            if (userInput.Contains("tomorrow"))
                return DateTime.Now.AddDays(1);

            var match = Regex.Match(userInput, @"(\d+) days?");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
                return DateTime.Now.AddDays(days);
            if (userInput.Contains("next week"))
                return DateTime.Now.AddDays(7);

            return null;
        }
        private string GetDefaultDescription(string taskTitle)
        {
            if (ContainsAnyVariation(taskTitle, "privacy"))
                return "Review account privacy settings to ensure your data is protected.";
            if (ContainsAnyVariation(taskTitle, "password"))
                return "Update passwords and check for any compromised credentials.";
            if (ContainsAnyVariation(taskTitle, "backup"))
                return "Create backups of important data and verify their integrity.";
            if (ContainsAnyVariation(taskTitle, "2fa"))
                return "Enable two-factor authentication for added security.";

            return "Important cybersecurity maintenance task.";
        }
        private void CheckReminders(object sender, EventArgs e)
        {
            foreach (var task in tasks.Where(t => t.HasReminder && !t.IsCompleted))
            {
                if (task.ReminderDate <= DateTime.Now)
                {
                    AddChatMessage("Reminder", $"Task due: {task.Title}", Colors.Orange);
                    task.HasReminder = false;
                    LogActivity($"Reminder triggered for task: {task.Title}");
                    SaveTasks();
                }
            }
        }

        private void RefreshTaskList()
        {
            TasksList.ItemsSource = null;
            TasksList.ItemsSource = tasks;
        }

        private void SaveTasks()
        {
            if (string.IsNullOrEmpty(userName)) return;

            string taskFile = Path.Combine(logDirectory, $"{userName}_tasks.json");
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(tasks);
                File.WriteAllText(taskFile, json);
            }
            catch (Exception ex)
            {
                AddChatMessage("System", $"Error saving tasks: {ex.Message}", Colors.Red);
            }
        }

        private void LoadTasks()
        {
            if (string.IsNullOrEmpty(userName)) return;

            string taskFile = System.IO.Path.Combine(logDirectory, $"{userName}_tasks.json");
            if (File.Exists(taskFile))
            {
                try
                {
                    string json = File.ReadAllText(taskFile);
                    tasks = System.Text.Json.JsonSerializer.Deserialize<List<CybersecurityTask>>(json);
                    RefreshTaskList();
                }
                catch (Exception ex)
                {
                    AddChatMessage("System", $"Error loading tasks: {ex.Message}", Colors.Red);
                }
            }
        }

        private void InitializeQuiz()
        {
            // Multiple Choice Questions
            quizQuestions.Add(new QuizQuestion
            {
                Question = "What should you do if you receive an email asking for your password?",
                Options = new List<QuizOption>
                {
                    new QuizOption { Text = "Reply with your password", IsCorrect = false },
                    new QuizOption { Text = "Delete the email", IsCorrect = false },
                    new QuizOption { Text = "Report email as phishing", IsCorrect = true },
                    new QuizOption { Text = "Ignore it", IsCorrect = false }
                },
                Explanation = "Reporting phishing emails helps protect others from the same scam."
            });

            // Add 9 more questions following the same pattern...
        }

        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            StartQuiz.Visibility = Visibility.Collapsed;
            QuizPanel.Visibility = Visibility.Visible;
            currentQuizQuestion = 0;
            quizScore = 0;
            quizActive = true;
            ShowNextQuizQuestion();
            LogActivity("Cybersecurity quiz started");
        }

        private void ShowNextQuizQuestion()
        {
            if (currentQuizQuestion >= quizQuestions.Count)
            {
                EndQuiz();
                return;
            }

            var question = quizQuestions[currentQuizQuestion];
            QuizQuestion.Text = $"Question {currentQuizQuestion + 1}: {question.Question}";
            QuizOptions.ItemsSource = question.Options;
            QuizFeedback.Visibility = Visibility.Collapsed;
            SubmitQuizAnswer.IsEnabled = true;
        }

        private void SubmitQuizAnswer_Click(object sender, RoutedEventArgs e)
        {
            var currentQuestion = quizQuestions[currentQuizQuestion];
            var selectedOption = currentQuestion.Options.FirstOrDefault(o => o.IsSelected);

            if (selectedOption == null)
            {
                AddChatMessage("WishiChatBot", "Please select an answer before submitting.", Colors.Red);
                return;
            }

            SubmitQuizAnswer.IsEnabled = false;
            QuizFeedback.Visibility = Visibility.Visible;

            if (selectedOption.IsCorrect)
            {
                quizScore++;
                QuizFeedback.Text = "Correct! " + currentQuestion.Explanation;
                QuizFeedback.Foreground = Brushes.Green;
                LogActivity($"Quiz question {currentQuizQuestion + 1} answered correctly");
            }
            else
            {
                QuizFeedback.Text = "Incorrect. " + currentQuestion.Explanation;
                QuizFeedback.Foreground = Brushes.Red;
                LogActivity($"Quiz question {currentQuizQuestion + 1} answered incorrectly");
            }

            currentQuizQuestion++;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                ShowNextQuizQuestion();
            };
            timer.Start();
        }

        private void EndQuiz()
        {
            quizActive = false;
            QuizPanel.Visibility = Visibility.Collapsed;
            QuizResultsPanel.Visibility = Visibility.Visible;

            double percentage = (double)quizScore / quizQuestions.Count * 100;
            QuizScore.Text = $"Your Score: {quizScore}/{quizQuestions.Count} ({percentage:0}%)";

            string feedback;
            if (percentage >= 90) feedback = "Excellent! You're a cybersecurity expert!";
            else if (percentage >= 70) feedback = "Good job! You know quite a bit about cybersecurity.";
            else if (percentage >= 50) feedback = "Not bad! You have some cybersecurity knowledge.";
            else feedback = "Keep learning! Cybersecurity is important for staying safe online.";

            QuizFeedbackMessage.Text = feedback;
            LogActivity($"Quiz completed", $"Score: {quizScore}/{quizQuestions.Count} ({percentage:0}%) - {feedback}");
        }

      


        private void LogActivity(string action, string details = "")
        {
            var entry = new ActivityLogEntry
            {
                Timestamp = DateTime.Now,
                ActionDescription = action,
                Details = details
            };

            activityLog.Insert(0, entry);
            RefreshActivityLogDisplay();
        }

        private void RefreshActivityLogDisplay()
        {
            var itemsToShow = showFullLog ?
               activityLog :
               activityLog.Take(MaxVisibleLogEntries);

            ActivityLogList.ItemsSource = itemsToShow;
            ShowFullLog.Visibility = activityLog.Count > MaxVisibleLogEntries ?
                Visibility.Visible : Visibility.Collapsed;
        }

       


        private bool ContainsAnyVariation(string input, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (keywordVariations.TryGetValue(keyword.ToLower(), out string[] variations))
                {
                    if (variations.Any(v => input.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0))
                        return true;
                }
                else if (input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }



        private string GetRandomResponse(List<string> responses)
        {
            Random rand = new Random();
            return responses[rand.Next(responses.Count)];
        }

        private void UserInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessUserInput();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessUserInput();
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TaskDialog();
            if (dialog.ShowDialog() == true)
            {
                var newTask = new CybersecurityTask
                {
                    Title = dialog.TaskTitle,
                    Description = dialog.TaskDescription,
                    CreatedDate = DateTime.Now
                };

                if (dialog.SetReminder)
                {
                    newTask.SetReminder(dialog.ReminderDays);
                    LogActivity($"Task added with reminder: '{newTask.Title}'",
                              $"Due in {dialog.ReminderDays} days");
                }
                else
                {
                    LogActivity($"Task added: '{newTask.Title}'");
                }

                tasks.Add(newTask);
                RefreshTaskList();
                SaveTasks();
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var task = (CybersecurityTask)checkBox.DataContext;
            SaveTasks();
            LogActivity($"Task marked as completed: '{task.Title}'");
        }

        private void QuizOption_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            var option = (QuizOption)radioButton.DataContext;
            option.IsSelected = true;
        }

        private void RestartQuiz_Click(object sender, RoutedEventArgs e)
        {
            QuizResultsPanel.Visibility = Visibility.Collapsed;
            StartQuiz.Visibility = Visibility.Visible;
        }

        private void RefreshActivityLog_Click(object sender, RoutedEventArgs e)
        {
            RefreshActivityLogDisplay();
        }

        private void ShowFullLog_Click(object sender, RoutedEventArgs e)
        {
            showFullLog = !showFullLog;
            ShowFullLog.Content = showFullLog ? "Show Less" : "Show Full History";
            RefreshActivityLogDisplay();
        }
        #endregion
    }



}







          