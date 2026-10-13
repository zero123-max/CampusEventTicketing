using System.Windows;

namespace CampusEventTicketing;

public partial class AdminDashboardWindow : Window
{
    public AdminDashboardWindow()
    {
        InitializeComponent();
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("You are on the Admin Dashboard.");

    private void Events_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Manage Events selected.");

    private void Students_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Students section selected.");

    private void Tickets_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Tickets section selected.");

    private void Reports_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Reports section selected.");

    private void AddEvent_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Add Event will be connected to the database in the next step.");

    private void EditEvent_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Edit Event will be connected to the database in the next step.");

    private void DeleteEvent_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Delete Event will be connected to the database in the next step.");

    private void ChatButton_Click(object sender, RoutedEventArgs e)
    {
        var bot = new ChatbotWindow { Owner = this };
        bot.Show();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }
}
