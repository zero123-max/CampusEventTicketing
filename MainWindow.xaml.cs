using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CampusEventTicketing;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // ---------------- LOGIN ----------------

    // Demo accounts. Students who create an account are checked in the database.
    private const string AdminUser = "admin";
    private const string AdminPass = "admin123";
    private const string StudentUser = "student";
    private const string StudentPass = "student123";

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;

        if (username.Length == 0 || password.Length == 0)
        {
            MessageBox.Show("Please enter your username and password.", "Login");
            return;
        }

        try
        {
            if (AdminRadio.IsChecked == true)
            {
                if (username == AdminUser && password == AdminPass)
                {
                    new AdminDashboardWindow().Show();
                    Close();
                    return;
                }
            }
            else
            {
                bool ok = username == StudentUser && password == StudentPass;
                if (!ok)
                {
                    try { ok = Database.CheckLogin(username, password); }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database error: " + ex.Message, "Login");
                        return;
                    }
                }
                if (ok)
                {
                    new DashboardWindow().Show();
                    Close();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            // Shows why the dashboard window failed to open
            MessageBox.Show(ex.ToString(), "Could not open the dashboard");
            return;
        }

        MessageBox.Show("Invalid username or password for the selected role.", "Login failed");
    }

    private void Forget_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Please contact the campus administrator to reset your password.", "Forgot password");
    }

    private void CreateAccount_Click(object sender, RoutedEventArgs e)
    {
        var win = new CreateAccountWindow { Owner = this };
        if (win.ShowDialog() == true)
        {
            UsernameBox.Text = win.CreatedUsername;
            PasswordBox.Clear();
            StudentRadio.IsChecked = true;
            PasswordBox.Focus();
        }
    }

    // ---------------- CHATBOT BUTTON ----------------

    private ChatbotWindow? _bot;

    // Opens one chatbot window; clicking again brings the existing one to the front
    private void ChatButton_Click(object sender, RoutedEventArgs e)
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

    // ---------------- DRAGGABLE ROBOT BUTTON ----------------

    private const double EdgeGap = 8;      // keep the button this far from the window edge
    private const double HomeMargin = 24;  // must match Margin="0,0,24,24" in the XAML

    private bool _fabPressed;
    private bool _fabDragging;
    private Point _fabStartMouse;
    private double _fabStartX, _fabStartY;

    private TranslateTransform FabTransform => (TranslateTransform)ChatFab.RenderTransform;

    private void ChatFab_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _fabPressed = true;
        _fabDragging = false;
        _fabStartMouse = e.GetPosition(RootGrid);
        _fabStartX = FabTransform.X;
        _fabStartY = FabTransform.Y;
        ChatFab.CaptureMouse();
        e.Handled = true;
    }

    private void ChatFab_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_fabPressed) return;

        Point pos = e.GetPosition(RootGrid);
        double dx = pos.X - _fabStartMouse.X;
        double dy = pos.Y - _fabStartMouse.Y;

        // Only start dragging after a small movement, so a normal click still works
        if (!_fabDragging)
        {
            if (Math.Abs(dx) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(dy) < SystemParameters.MinimumVerticalDragDistance)
                return;
            _fabDragging = true;
        }

        double w = ChatFab.ActualWidth, h = ChatFab.ActualHeight;
        double minX = -(RootGrid.ActualWidth - HomeMargin - w) + EdgeGap;
        double maxX = HomeMargin - EdgeGap;
        double minY = -(RootGrid.ActualHeight - HomeMargin - h) + EdgeGap;
        double maxY = HomeMargin - EdgeGap;

        FabTransform.X = Math.Max(minX, Math.Min(maxX, _fabStartX + dx));
        FabTransform.Y = Math.Max(minY, Math.Min(maxY, _fabStartY + dy));
    }

    private void ChatFab_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_fabPressed) return;

        bool wasDrag = _fabDragging;
        _fabPressed = false;
        _fabDragging = false;
        ChatFab.ReleaseMouseCapture();
        e.Handled = true;

        // Plain click (no drag) opens the chatbot
        if (!wasDrag)
            ChatButton_Click(sender, e);
    }
}