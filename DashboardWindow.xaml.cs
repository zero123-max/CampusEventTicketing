using System.Windows;
using System.Windows.Controls;

namespace CampusEventTicketing;

public partial class DashboardWindow : Window
{
    private ChatbotWindow? _bot;

    public DashboardWindow()
    {
        InitializeComponent();
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("You are on the Student Dashboard.");

    private void Events_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Events section selected.");

    private void Tickets_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("My Tickets section selected.");

    private void History_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("History section selected.");

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Settings section selected.");

    private void ViewAll_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Showing all campus events.");

    private void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        string name = (sender as Button)?.Tag?.ToString() ?? "this event";
        MessageBox.Show($"Details for {name} will be connected to the database in the next step.");
    }

    // Opens one chatbot window; clicking again brings it to the front
    private void CampusBot_Click(object sender, RoutedEventArgs e)
    {
        if (_bot == null || !_bot.IsLoaded)
        {
            _bot = new ChatbotWindow { Owner = this };
            _bot.Closed += (s, args) => _bot = null;
            _bot.Show();
        }
        else
        {
            _bot.Activate();
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }
}