using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CampusEventTicketing;

public partial class ChatbotWindow : Window
{
    // Add or edit events here. The bot only calls an event "upcoming" if its date hasn't passed.
    private record CampusEvent(string Name, DateTime Date, string Venue, string Keyword);

    private static readonly List<CampusEvent> Events = new()
    {
        new("Campus Sports Festival", new DateTime(2026, 9, 25),  "University Gym",         "sports"),
        new("Student Tech Summit",    new DateTime(2026, 10, 5),  "ICT Building",           "tech"),
        new("Campus Music Night",     new DateTime(2026, 10, 15), "University Auditorium",  "music"),
    };

    // Words that signal the user is describing a problem ("not", "can't", "error", "stuck"...)
    private static readonly Regex ProblemRegex = new(
        @"\b(not|no|can't|cant|cannot|won't|wont|unable|error|problem|issue|trouble|fail\w*|stuck|wrong|missing|" +
        @"bug\w*|crash\w*|freez\w*|frozen|slow|broken|doesn't|doesnt|didn't|didnt|invalid|blank|lost)\b",
        RegexOptions.IgnoreCase);

    public ChatbotWindow()
    {
        InitializeComponent();

        // Greeting shown to whoever opens the chatbot (placed right under the welcome card)
        AddBotMessage(
            GetGreeting() + "! 👋 Welcome to CampusBot, your campus assistant. " +
            "I can help with events, tickets, and system problems such as login or dashboard issues. " +
            "What do you need today?",
            insertAt: 1);
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    private void MessageBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendMessage();
            e.Handled = true;
        }
    }

    private void Suggestion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            string text = button.Content?.ToString() ?? "";
            Suggestions.Visibility = Visibility.Collapsed;
            AddUserMessage(text);
            AddBotMessage(GetBotReply(text));
        }
    }

    private void SendMessage()
    {
        string message = MessageBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        Suggestions.Visibility = Visibility.Collapsed;
        AddUserMessage(message);
        AddBotMessage(GetBotReply(message));
        MessageBox.Clear();
    }

    private void AddUserMessage(string message)
    {
        var bubble = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(20, 110, 245)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(45, 5, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 285
        };

        bubble.Child = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        };

        ChatPanel.Children.Add(bubble);
        ChatScroll.ScrollToEnd();
    }

    private void AddBotMessage(string message, int insertAt = -1)
    {
        var bubble = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(215, 228, 245)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 5, 45, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 300
        };

        bubble.Child = new TextBlock
        {
            Text = "🤖 " + message,
            Foreground = new SolidColorBrush(Color.FromRgb(16, 42, 98)),
            TextWrapping = TextWrapping.Wrap
        };

        if (insertAt >= 0 && insertAt <= ChatPanel.Children.Count)
            ChatPanel.Children.Insert(insertAt, bubble);
        else
            ChatPanel.Children.Add(bubble);
        ChatScroll.ScrollToEnd();
    }

    // =====================================================================
    //  Reply logic: system problems first, then events, then greetings
    // =====================================================================
    private string GetBotReply(string input)
    {
        // Normalize: lowercase and turn curly apostrophes into normal ones
        string text = input.ToLowerInvariant().Replace('’', '\'');

        // True if the message contains any of these words/phrases (matched at the start of a word,
        // so "ticket" also matches "tickets", but "bot" does not match "robot")
        bool Mentions(params string[] words) =>
            words.Any(w => Regex.IsMatch(text, @"\b" + Regex.Escape(w)));

        bool HasProblem() => ProblemRegex.IsMatch(text);
        bool HasWord(params string[] words) => Regex.Split(text, @"\W+").Intersect(words).Any();

        // ---------- 1. Forgot / reset password ----------
        if (Mentions("password") && Regex.IsMatch(text, @"\b(forgot|forget|reset|change|recover|lost)"))
            return "To reset your password:\n" +
                   "1. On the login screen, click FORGET PASSWORD.\n" +
                   "2. If that doesn't help, contact the campus administrator and ask them to reset it.\n" +
                   "3. Log in again with your new password.";

        // ---------- 2. Logout ----------
        if (Mentions("logout", "log out", "sign out", "switch account"))
            return "To log out, click the Logout button at the bottom of the left menu. " +
                   "You will return to the login page, where you can sign in with another account.";

        // ---------- 3. Login problems ----------
        if (Mentions("login", "log in", "sign in", "username", "credential", "password", "account"))
            return "Login problems? Try these steps:\n" +
                   "1. Check your username and password for typing mistakes or extra spaces.\n" +
                   "2. Make sure Caps Lock is off.\n" +
                   "3. Under \"Login as\", choose the right role: Student / User or Admin. " +
                   "A student account will not work with Admin selected.\n" +
                   "4. Still stuck? Click FORGET PASSWORD or contact the campus administrator.";

        // ---------- 4. Dashboard / window won't open ----------
        if (Mentions("dashboard", "window", "page", "screen", "application", "program", "software") && HasProblem())
            return "If a page or the dashboard won't open or load:\n" +
                   "1. Make sure you picked the correct role on the login page (Student / User or Admin).\n" +
                   "2. Log out, close the application completely, and open it again.\n" +
                   "3. Log in again and wait a few seconds for the page to load.\n" +
                   "4. If it still fails, note the exact error message and send it to campus support.";

        // ---------- 5. Ticket problems ----------
        if (Mentions("ticket") && HasProblem())
            return "Ticket problem? Try these steps:\n" +
                   "1. Open My Tickets in the left menu and refresh the page by logging out and in again.\n" +
                   "2. Make sure the event hasn't already finished. Past events can't be booked.\n" +
                   "3. Open the event with View Details and check that registration is available.\n" +
                   "4. If your ticket is still missing, contact campus support with the event name and date.";

        // ---------- 6. Ticket how-to ----------
        if (Mentions("ticket"))
            return "You can view your tickets under My Tickets. Select an event and choose View Details to continue with ticket registration.";

        // ---------- 7. Event problems ----------
        if (Mentions("event", "schedule", "venue", "date") && HasProblem())
            return "If an event is missing or its details look wrong:\n" +
                   "1. Check the Upcoming Campus Events list on your dashboard. Events that already passed are removed.\n" +
                   "2. Click View All to see the full list.\n" +
                   "3. If the date or venue is wrong, report it to the campus administrator so they can correct it.";

        // ---------- 8. Admin tasks ----------
        if (Mentions("add event", "add an event", "add events", "create event", "create an event", "edit event", "delete event", "manage event", "report", "students"))
            return "Admins can manage events from the Admin Dashboard: use the Add Event, Edit Event, and Delete Event " +
                   "buttons under Event Management. Some admin features are still being connected to the database, " +
                   "so a message may say so for now.";

        // ---------- 9. Chatbot problems ----------
        if (Mentions("chatbot", "campusbot", "bot") && HasProblem())
            return "If I'm not answering properly, try short keywords such as \"login\", \"ticket\", \"events\", or " +
                   "\"password\". For anything else, please contact campus support.";

        // ---------- 10. Crashes, errors, slowness ----------
        if (Mentions("error", "crash", "freez", "frozen", "slow", "bug", "hang", "lag", "broken", "not working", "doesn't work", "doesnt work"))
            return "Sorry about that! Try this:\n" +
                   "1. Close the application and open it again.\n" +
                   "2. Log in again and repeat what you were doing.\n" +
                   "3. If the same problem happens, write down the exact error message and what you clicked, " +
                   "then send it to campus support.";

        // ---------- 11. General "problem / issue" ----------
        if (Mentions("problem", "issue", "trouble"))
            return "I'm sorry you're having trouble. Which part is the problem? I can help with:\n" +
                   "• Login or password\n• Dashboard not opening\n• Tickets not showing\n• Events missing or wrong\n" +
                   "• Errors, crashes, or slowness\nTell me a little more and I'll guide you.";

        // ---------- 12. Contact support ----------
        if (Mentions("contact", "support", "help desk", "complain", "report a"))
            return "You can contact the campus administrator or campus support office. " +
                   "Please include your name, what you were trying to do, and any error message you saw.";

        // ---------- 12b. About the chatbot ----------
        if (Mentions("what can you", "who are you", "what are you", "what is campusbot", "what is the chatbot"))
            return "I'm CampusBot, the campus assistant! I can tell you about upcoming events, explain how tickets work, " +
                   "and help fix problems such as login, password, dashboard, or missing tickets. Just type your question " +
                   "or tap one of the suggestions.";

        // ---------- 13. A specific event was mentioned ----------
        var named = Events.FirstOrDefault(ev => HasWord(ev.Keyword));
        if (named != null)
            return DescribeEvent(named);

        // ---------- 14. General events / schedule ----------
        if (Mentions("event", "coming", "upcoming", "schedule"))
            return UpcomingEventsReply();

        // ---------- 15. Greetings ----------
        // If the user says "good morning / afternoon / evening", answer with the same greeting
        if (text.Contains("good morning"))
            return "Good morning! 👋 How can I help you with the campus event system?";
        if (text.Contains("good afternoon"))
            return "Good afternoon! 👋 How can I help you with the campus event system?";
        if (text.Contains("good evening"))
            return "Good evening! 👋 How can I help you with the campus event system?";

        // "hi", "hello", "hey" -> the chatbot says hello
        if (HasWord("hello", "hi", "hey"))
            return "Hello! 👋 How can I help you with the campus event system?";

        // ---------- 16. Fallback ----------
        return "I can help with campus events, tickets, and system problems such as login, password, " +
               "dashboard, or tickets not showing. Try describing your problem, for example: " +
               "\"I can't login\" or \"my ticket is not showing\".";
    }

    // "Good morning / afternoon / evening" based on the computer's clock
    private static string GetGreeting()
    {
        int hour = DateTime.Now.Hour;
        return hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
    }

    private static string DescribeEvent(CampusEvent ev)
    {
        string date = ev.Date.ToString("MMMM d, yyyy");

        return ev.Date.Date >= DateTime.Today
            ? $"The {ev.Name} is scheduled for {date} at the {ev.Venue}."
            : $"The {ev.Name} was held on {date} at the {ev.Venue}, so it has already finished. Ask me about the upcoming events!";
    }

    private static string UpcomingEventsReply()
    {
        var upcoming = Events
            .Where(ev => ev.Date.Date >= DateTime.Today)
            .OrderBy(ev => ev.Date)
            .ToList();

        if (upcoming.Count == 0)
            return "There are no upcoming events right now. Please check back soon!";

        string list = string.Join(", ", upcoming.Select(ev => $"{ev.Name} on {ev.Date:MMMM d}"));
        return $"Upcoming events: {list}, {upcoming.Last().Date:yyyy}.";
    }
}