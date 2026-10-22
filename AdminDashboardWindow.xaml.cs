using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace CampusEventTicketing;

public partial class AdminDashboardWindow : Window
{
    readonly ObservableCollection<DashEvent> events = new();
    DashEvent? editing;
    string letter = "All";
    readonly Dictionary<string, (FrameworkElement Page, Button Nav)> pages = new();

    public AdminDashboardWindow()
    {
        InitializeComponent();

        pages["Dashboard"] = (DashboardPage, BtnDashboard);
        pages["Events"] = (EventsPage, BtnEvents);
        pages["Students"] = (StudentsPage, BtnStudents);
        pages["Tickets"] = (TicketsPage, BtnTickets);
        pages["Reports"] = (ReportsPage, BtnReports);

        EventsGrid.ItemsSource = events;
        cbBatchEvent.ItemsSource = events;
        dpEventDate.SelectedDate = DateTime.Today;
        BuildLetters();
        ShowReportTab("Problem Reports");

        try
        {
            Database.Initialize();   // creates campus.db + tables + sample data on first run
            ReloadEvents();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Database error: " + ex.Message);
        }

        ShowPage("Dashboard");
    }

    // run a database action and show a friendly message if it fails
    bool Safe(Action action)
    {
        try { action(); return true; }
        catch (Exception ex)
        {
            MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    void ReloadEvents()
    {
        events.Clear();
        foreach (var ev in Database.GetEvents()) events.Add(ev);
    }

    // ───────── navigation (sidebar buttons) ─────────
    void ShowPage(string key)
    {
        foreach (var (k, v) in pages)
        {
            v.Page.Visibility = k == key ? Visibility.Visible : Visibility.Collapsed;
            v.Nav.Tag = k == key ? "Active" : null;
        }
        RefreshAll();
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowPage("Dashboard");
    private void Events_Click(object sender, RoutedEventArgs e) => ShowPage("Events");
    private void Students_Click(object sender, RoutedEventArgs e) => ShowPage("Students");
    private void Tickets_Click(object sender, RoutedEventArgs e) => ShowPage("Tickets");
    private void Reports_Click(object sender, RoutedEventArgs e) => ShowPage("Reports");

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }

    private void ChatButton_Click(object sender, RoutedEventArgs e)
    {
        var bot = new ChatbotWindow { Owner = this };
        bot.Show();
    }

    // ───────── refresh the screen from the database ─────────
    void RefreshAll()
    {
        for (int i = 0; i < events.Count; i++) events[i].No = i + 1;
        EventsGrid.Items.Refresh();

        TicketsGrid.ItemsSource = events.Select((ev, i) => new DashTicketRow
        {
            No = i + 1,
            EventName = ev.Name,
            Total = ev.TotalTickets,
            Available = ev.TotalTickets - ev.Sold,
            Sold = ev.Sold,
            Event = ev
        }).ToList();

        EventReportGrid.ItemsSource = events.ToList();
        SalesGrid.ItemsSource = events.Select((ev, i) => new DashSalesRow
        {
            No = i + 1,
            EventName = ev.Name,
            Total = ev.TotalTickets,
            Sold = ev.Sold,
            Percent = (ev.TotalTickets == 0 ? 0 : 100.0 * ev.Sold / ev.TotalTickets).ToString("0.#") + "%"
        }).ToList();

        int studentCount = 0, problemCount = 0;
        Safe(() =>
        {
            studentCount = Database.CountStudents();
            StudentReportGrid.ItemsSource = Database.GetCourseCounts();

            var problems = Database.GetProblems();
            for (int i = 0; i < problems.Count; i++) problems[i].No = i + 1;
            ProblemGrid.ItemsSource = problems;
            problemCount = problems.Count;

            ApplyStudentFilter();
        });

        int sold = events.Sum(ev => ev.Sold);
        DashEvents.Text = RepEvents.Text = events.Count.ToString();
        DashStudents.Text = RepStudents.Text = studentCount.ToString();
        DashTickets.Text = RepSold.Text = sold.ToString();
        DashUpcoming.Text = events.Count(ev => ev.Status == "Upcoming").ToString();
        RepProblems.Text = problemCount.ToString();
    }

    // ───────── dashboard quick buttons ─────────
    private void AddEvent_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Events");
        ClearEventForm();
        txtEventName.Focus();
    }

    private void EditEvent_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Events");
        MessageBox.Show("Click the Edit button on the event you want to change.");
    }

    private void DeleteEvent_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Events");
        MessageBox.Show("Click the Delete button on the event you want to remove.");
    }

    // ───────── manage events ─────────
    private void CreateEvent_Click(object sender, RoutedEventArgs e)
    {
        ClearEventForm();
        txtEventName.Focus();
    }

    void ClearEventForm()
    {
        editing = null;
        txtEventName.Clear();
        txtEventLocation.Clear();
        dpEventDate.SelectedDate = DateTime.Today;
        cbEventStatus.SelectedIndex = 0;
    }

    private void CancelEvent_Click(object sender, RoutedEventArgs e) => ClearEventForm();

    private void SaveEvent_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEventName.Text) || string.IsNullOrWhiteSpace(txtEventLocation.Text))
        {
            MessageBox.Show("Please enter the event name and location.");
            return;
        }
        var status = ((ComboBoxItem)cbEventStatus.SelectedItem).Content?.ToString() ?? "Upcoming";
        var date = dpEventDate.SelectedDate ?? DateTime.Today;

        bool ok = Safe(() =>
        {
            if (editing != null)
            {
                editing.Name = txtEventName.Text.Trim();
                editing.Location = txtEventLocation.Text.Trim();
                editing.Date = date;
                editing.Status = status;
                Database.UpdateEvent(editing);
            }
            else
            {
                Database.AddEvent(new DashEvent
                {
                    Name = txtEventName.Text.Trim(),
                    Location = txtEventLocation.Text.Trim(),
                    Date = date,
                    Status = status
                });
            }
        });
        if (!ok) return;

        ClearEventForm();
        ReloadEvents();
        RefreshAll();
    }

    private void EditRow_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not DashEvent ev) return;
        editing = ev;
        txtEventName.Text = ev.Name;
        txtEventLocation.Text = ev.Location;
        dpEventDate.SelectedDate = ev.Date;
        foreach (ComboBoxItem item in cbEventStatus.Items)
            if (item.Content?.ToString() == ev.Status) cbEventStatus.SelectedItem = item;
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not DashEvent ev) return;
        if (MessageBox.Show($"Delete \"{ev.Name}\"?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        if (!Safe(() => Database.DeleteEvent(ev.Id))) return;

        ClearEventForm();
        ReloadEvents();
        RefreshAll();
    }

    // ───────── students ─────────
    void BuildLetters()
    {
        foreach (var l in new[] { "All" }.Concat("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(c => c.ToString())))
        {
            var b = new Button
            {
                Content = l,
                Style = (Style)FindResource("LetterBtn"),
                Tag = l == "All" ? "Active" : null
            };
            if (l == "All") b.Width = 44;
            b.Click += (s, e) =>
            {
                letter = l;
                foreach (Button x in LetterPanel.Children)
                    x.Tag = (string)x.Content == l ? "Active" : null;
                ApplyStudentFilter();
            };
            LetterPanel.Children.Add(b);
        }
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyStudentFilter();

    void ApplyStudentFilter()
    {
        if (StudentsGrid is null || txtSearch is null) return;
        Safe(() =>
        {
            var list = Database.GetStudents(letter, txtSearch.Text.Trim());
            for (int i = 0; i < list.Count; i++) list[i].No = i + 1;
            StudentsGrid.ItemsSource = list;
        });
    }

    private void ViewStudent_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is DashStudent s)
            MessageBox.Show($"ID: {s.StudentId}\nName: {s.Name}\nCourse: {s.Course}\nYear: {s.Year}\nStatus: {s.Status}", "Student");
    }

    private void EditStudent_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Edit Student form can be added next.");

    // ───────── tickets ─────────
    private void CreateBatch_Click(object sender, RoutedEventArgs e) => cbBatchEvent.Focus();

    private void EditTicket_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not DashTicketRow row || row.Event is null) return;
        cbBatchEvent.SelectedItem = row.Event;
        txtTicketCount.Text = row.Event.TotalTickets.ToString();
    }

    private void DeleteTicket_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not DashTicketRow row || row.Event is null) return;
        if (MessageBox.Show($"Clear tickets for \"{row.EventName}\"?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        if (!Safe(() => Database.UpdateTickets(row.Event.Id, 0, 0))) return;

        ReloadEvents();
        RefreshAll();
    }

    private void GenerateTickets_Click(object sender, RoutedEventArgs e)
    {
        if (cbBatchEvent.SelectedItem is not DashEvent ev)
        {
            MessageBox.Show("Please select an event.");
            return;
        }
        if (!int.TryParse(txtTicketCount.Text, out int n) || n <= 0)
        {
            MessageBox.Show("Enter a valid number of tickets.");
            return;
        }
        if (n < ev.Sold)
        {
            MessageBox.Show("Total can't be lower than the tickets already sold.");
            return;
        }
        string name = ev.Name;
        if (!Safe(() => Database.UpdateTickets(ev.Id, n, ev.Sold))) return;

        txtTicketCount.Clear();
        ReloadEvents();
        RefreshAll();
        MessageBox.Show($"{n} tickets generated for {name}.");
    }

    // ───────── reports ─────────
    private void ReportTab_Click(object sender, RoutedEventArgs e) =>
        ShowReportTab(((Button)sender).Content?.ToString() ?? "");

    void ShowReportTab(string name)
    {
        foreach (var b in new[] { TabEvent, TabSales, TabStudent, TabProblem })
            b.Tag = b.Content?.ToString() == name ? "Active" : null;

        EventReportGrid.Visibility = name == "Event Reports" ? Visibility.Visible : Visibility.Collapsed;
        SalesGrid.Visibility = name == "Ticket Sales Reports" ? Visibility.Visible : Visibility.Collapsed;
        StudentReportGrid.Visibility = name == "Student Reports" ? Visibility.Visible : Visibility.Collapsed;
        ProblemGrid.Visibility = name == "Problem Reports" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ViewProblem_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is DashProblem p)
            MessageBox.Show($"{p.When}\n{p.Student} — {p.Issue}\n\n{p.Description}\nStatus: {p.Status}", "Problem Report");
    }
}