using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CampusEventTicketing;

public partial class DashboardWindow : Window
{
    // ───────── sample data (replace with database calls later) ─────────
    readonly List<CampusEventItem> allEvents = new()
    {
        CampusEventItem.Make("Sports",   "#2563EB", "⚽", "#74B6F2", "Campus Sports Festival", "September 25, 2026", "University Gym",        "Free"),
        CampusEventItem.Make("Tech",     "#0F766E", "💻", "#243F66", "Student Tech Summit",    "October 5, 2026",    "ICT Building",         "Free"),
        CampusEventItem.Make("Music",    "#9333EA", "🎵", "#6B2A9E", "Campus Music Night",     "October 15, 2026",   "University Auditorium","₱150"),
        CampusEventItem.Make("Arts",     "#059669", "🎨", "#E8A33D", "Campus Art Exhibit",     "October 20, 2026",   "Student Center",       "₱100"),
        CampusEventItem.Make("Food",     "#EA580C", "🍜", "#D9602B", "Food Festival",          "October 28, 2026",   "Campus Grounds",       "₱50"),
        CampusEventItem.Make("Wellness", "#0D9488", "🧘", "#5FAF72", "Wellness & Fitness Day", "November 5, 2026",   "Open Field",           "Free"),
    };

    readonly List<MyTicket> upcomingTickets = new()
    {
        MyTicket.Make("Campus Sports Festival", "⚽", "#74B6F2", "September 25, 2026 • 9:00 AM",  "University Gym",         "EVT-001234", "General Admission", "Confirmed"),
        MyTicket.Make("Campus Music Night",     "🎵", "#6B2A9E", "October 15, 2026 • 6:00 PM",    "University Auditorium",  "EVT-001235", "General Admission", "Confirmed"),
        MyTicket.Make("Student Tech Summit",    "💻", "#243F66", "October 5, 2026 • 1:00 PM",     "ICT Building",           "EVT-001236", "VIP Pass",          "Confirmed"),
        MyTicket.Make("Food Festival",          "🍜", "#D9602B", "October 28, 2026 • 10:00 AM",   "Campus Grounds",         "EVT-001237", "General Admission", "Confirmed"),
    };

    readonly List<MyTicket> pastTickets = new()
    {
        MyTicket.Make("Campus Art Exhibit", "🎨", "#E8A33D", "July 22, 2026 • 10:15 AM", "Student Center", "EVT-000987", "Student Pass", "Used"),
        MyTicket.Make("Open Mic Night",     "🎤", "#6B2A9E", "July 10, 2026 • 7:00 PM",  "Cafeteria",      "EVT-000954", "General Admission", "Used"),
    };

    readonly List<PurchaseRow> purchases = new()
    {
        new() { EventName = "Campus Sports Festival", PurchaseDate = "Sep 10, 2026 • 3:30 PM", TicketType = "General Admission", Amount = "₱0.00",   Status = "Used" },
        new() { EventName = "Student Tech Summit",    PurchaseDate = "Sep 2, 2026 • 11:20 AM", TicketType = "VIP Pass",          Amount = "₱100.00", Status = "Used" },
        new() { EventName = "Campus Music Night",     PurchaseDate = "Aug 18, 2026 • 5:10 PM", TicketType = "General Admission", Amount = "₱150.00", Status = "Used" },
        new() { EventName = "Food Festival",          PurchaseDate = "Aug 5, 2026 • 2:30 PM",  TicketType = "General Admission", Amount = "₱50.00",  Status = "Used" },
        new() { EventName = "Campus Art Exhibit",     PurchaseDate = "Jul 22, 2026 • 10:15 AM",TicketType = "Student Pass",      Amount = "₱100.00", Status = "Used" },
    };

    readonly Dictionary<string, (FrameworkElement Page, Button Nav)> pages = new();
    bool showingPast;
    bool ready;   // stops filter events from firing while the window is still loading

    public DashboardWindow()
    {
        InitializeComponent();

        pages["Dashboard"] = (PageDash, BtnDash);
        pages["Events"] = (PageEvents, BtnEvents);
        pages["Tickets"] = (PageTickets, BtnTickets);
        pages["History"] = (PageHistory, BtnHistory);
        pages["Settings"] = (PageSettings, BtnSettings);

        // dashboard
        StatEvents.Text = allEvents.Count.ToString();
        StatTickets.Text = upcomingTickets.Count.ToString();
        StatUpcoming.Text = allEvents.Count.ToString();
        DashCards.ItemsSource = allEvents.Take(3).ToList();

        // history filter list
        var names = new List<string> { "All Events" };
        names.AddRange(purchases.Select(p => p.EventName).Distinct());
        HistoryEventBox.ItemsSource = names;
        HistoryEventBox.SelectedIndex = 0;

        ready = true;
        ApplyEventFilter();
        ApplyTicketFilter();
        ApplyHistoryFilter();
        ShowPage("Dashboard");
    }

    // ───────── navigation ─────────
    void ShowPage(string key)
    {
        foreach (var (k, v) in pages)
        {
            v.Page.Visibility = k == key ? Visibility.Visible : Visibility.Collapsed;
            v.Nav.Tag = k == key ? "Active" : null;
        }
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowPage("Dashboard");
    private void Events_Click(object sender, RoutedEventArgs e) => ShowPage("Events");
    private void Tickets_Click(object sender, RoutedEventArgs e) => ShowPage("Tickets");
    private void History_Click(object sender, RoutedEventArgs e) => ShowPage("History");
    private void Settings_Click(object sender, RoutedEventArgs e) => ShowPage("Settings");
    private void ViewAll_Click(object sender, RoutedEventArgs e) => ShowPage("Events");

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }

    private void CampusBot_Click(object sender, RoutedEventArgs e)
    {
        new ChatbotWindow { Owner = this }.Show();
    }

    // ───────── events page ─────────
    private void EventFilter_Changed(object sender, RoutedEventArgs e) => ApplyEventFilter();

    void ApplyEventFilter()
    {
        if (!ready) return;

        string q = EventSearch.Text.Trim();
        string cat = (EventCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";

        var list = allEvents.Where(ev =>
            (cat == "All Categories" || ev.Category == cat) &&
            (q.Length == 0 ||
             ev.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
             ev.Location.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();

        EventCards.ItemsSource = list;
        EventsEmpty.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not CampusEventItem ev) return;
        MessageBox.Show(
            $"{ev.Title}\n\nCategory: {ev.Category}\nDate: {ev.DateText}\nLocation: {ev.Location}\nPrice: {ev.Price}",
            "Event Details");
    }

    // ───────── my tickets page ─────────
    private void TabUpcoming_Click(object sender, RoutedEventArgs e)
    {
        showingPast = false;
        TabUpcoming.Tag = "Active";
        TabPast.Tag = null;
        ApplyTicketFilter();
    }

    private void TabPast_Click(object sender, RoutedEventArgs e)
    {
        showingPast = true;
        TabPast.Tag = "Active";
        TabUpcoming.Tag = null;
        ApplyTicketFilter();
    }

    private void TicketSearch_Changed(object sender, TextChangedEventArgs e) => ApplyTicketFilter();

    void ApplyTicketFilter()
    {
        if (!ready) return;

        string q = TicketSearch.Text.Trim();
        var source = showingPast ? pastTickets : upcomingTickets;
        var list = source.Where(t =>
            q.Length == 0 ||
            t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.TicketId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Location.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        TicketCards.ItemsSource = list;
        TicketsEmpty.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ViewTicket_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not MyTicket t) return;
        MessageBox.Show(
            $"{t.Title}\n\nTicket ID: {t.TicketId}\nType: {t.TicketType}\nWhen: {t.DateTimeText}\nWhere: {t.Location}\nStatus: {t.Status}\n\n(The QR code image will be shown here.)",
            "Ticket");
    }

    // ───────── history page ─────────
    private void HistoryFilter_Changed(object sender, RoutedEventArgs e) => ApplyHistoryFilter();

    void ApplyHistoryFilter()
    {
        if (!ready) return;

        string q = HistorySearch.Text.Trim();
        string ev = HistoryEventBox.SelectedItem as string ?? "All Events";

        var list = purchases.Where(p =>
            (ev == "All Events" || p.EventName == ev) &&
            (q.Length == 0 ||
             p.EventName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
             p.TicketType.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();

        HistoryGrid.ItemsSource = list;
        HistoryCount.Text = $"Showing {list.Count} of {purchases.Count} records";
    }

    private void ViewPurchase_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not PurchaseRow p) return;
        MessageBox.Show(
            $"{p.EventName}\n\nPurchased: {p.PurchaseDate}\nTicket type: {p.TicketType}\nAmount: {p.Amount}\nStatus: {p.Status}",
            "Purchase Details");
    }

    // ───────── settings page ─────────
    private void EditProfile_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Edit profile form can be added next.", "Edit Profile");

    private void UpdatePassword_Click(object sender, RoutedEventArgs e)
    {
        string cur = PwdCurrent.Password, nw = PwdNew.Password, cf = PwdConfirm.Password;

        if (cur.Length == 0 || nw.Length == 0 || cf.Length == 0)
        {
            MessageBox.Show("Please fill in all three password fields.", "Change Password");
            return;
        }
        if (nw.Length < 6)
        {
            MessageBox.Show("The new password must be at least 6 characters.", "Change Password");
            return;
        }
        if (nw != cf)
        {
            MessageBox.Show("The new passwords do not match.", "Change Password");
            return;
        }

        // TODO: verify the current password and save the new one in the Accounts table.
        MessageBox.Show("Password checks passed. Saving it to the database is not connected yet.", "Change Password");
        PwdCurrent.Clear();
        PwdNew.Clear();
        PwdConfirm.Clear();
    }

    private void DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "Are you sure you want to delete your account? This cannot be undone.",
            "Delete Account", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        // TODO: delete the row from the Accounts table.
        MessageBox.Show("Account deletion is not connected to the database yet.", "Delete Account");
    }
}

// ───────── simple data classes used by the pages ─────────
static class Ui
{
    public static Brush Hex(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}

public class CampusEventItem
{
    public string Category { get; set; } = "";
    public Brush CategoryBrush { get; set; } = Brushes.Gray;
    public string Banner { get; set; } = "";
    public Brush BannerBrush { get; set; } = Brushes.LightBlue;
    public string Title { get; set; } = "";
    public string DateText { get; set; } = "";
    public string Location { get; set; } = "";
    public string Price { get; set; } = "";
    public Brush PriceBrush { get; set; } = Brushes.Green;

    public static CampusEventItem Make(string cat, string catColor, string banner, string bannerColor,
                                       string title, string date, string location, string price) => new()
                                       {
                                           Category = cat,
                                           CategoryBrush = Ui.Hex(catColor),
                                           Banner = banner,
                                           BannerBrush = Ui.Hex(bannerColor),
                                           Title = title,
                                           DateText = date,
                                           Location = location,
                                           Price = price,
                                           PriceBrush = Ui.Hex(price == "Free" ? "#15803D" : "#D97706")
                                       };
}

public class MyTicket
{
    public string Title { get; set; } = "";
    public string Banner { get; set; } = "";
    public Brush BannerBrush { get; set; } = Brushes.LightBlue;
    public string DateTimeText { get; set; } = "";
    public string Location { get; set; } = "";
    public string TicketId { get; set; } = "";
    public string TicketType { get; set; } = "";
    public string Status { get; set; } = "";
    public Brush StatusBack { get; set; } = Brushes.LightGreen;
    public Brush StatusFore { get; set; } = Brushes.DarkGreen;

    public static MyTicket Make(string title, string banner, string bannerColor, string when,
                                string location, string id, string type, string status) => new()
                                {
                                    Title = title,
                                    Banner = banner,
                                    BannerBrush = Ui.Hex(bannerColor),
                                    DateTimeText = when,
                                    Location = location,
                                    TicketId = id,
                                    TicketType = type,
                                    Status = status,
                                    StatusBack = Ui.Hex(status == "Confirmed" ? "#D1FAE5" : "#E5E7EB"),
                                    StatusFore = Ui.Hex(status == "Confirmed" ? "#15803D" : "#475569")
                                };
}

public class PurchaseRow
{
    public string EventName { get; set; } = "";
    public string PurchaseDate { get; set; } = "";
    public string TicketType { get; set; } = "";
    public string Amount { get; set; } = "";
    public string Status { get; set; } = "";
}