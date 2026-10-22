using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CampusEventTicketing;

public partial class CreateAccountWindow : Window
{
    public string CreatedUsername { get; private set; } = "";

    public CreateAccountWindow()
    {
        InitializeComponent();
        UpdateHints();
        FullNameBox.Focus();
    }

    // ───────── placeholder text ─────────
    static string ReadPwd(PasswordBox pb, TextBox tb, ToggleButton tg) =>
        tg.IsChecked == true ? tb.Text : pb.Password;

    void UpdateHints()
    {
        if (FullNameHint == null || ConfirmHint == null) return;

        FullNameHint.Visibility = FullNameBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        UserHint.Visibility = UserBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmailHint.Visibility = EmailBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        PwdHint.Visibility = ReadPwd(PwdBox, PwdText, PwdToggle).Length == 0
                                    ? Visibility.Visible : Visibility.Collapsed;
        ConfirmHint.Visibility = ReadPwd(ConfirmBox, ConfirmText, ConfirmToggle).Length == 0
                                    ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Field_Changed(object sender, TextChangedEventArgs e) => UpdateHints();
    private void Pwd_Changed(object sender, RoutedEventArgs e) => UpdateHints();

    // ───────── show / hide password (eye buttons) ─────────
    private void Eye_Click(object sender, RoutedEventArgs e)
    {
        var (pb, tb, tg) = sender == PwdToggle
            ? (PwdBox, PwdText, PwdToggle)
            : (ConfirmBox, ConfirmText, ConfirmToggle);

        if (tg.IsChecked == true)
        {
            tb.Text = pb.Password;
            pb.Visibility = Visibility.Collapsed;
            tb.Visibility = Visibility.Visible;
            tb.Focus();
            tb.CaretIndex = tb.Text.Length;
        }
        else
        {
            pb.Password = tb.Text;
            tb.Visibility = Visibility.Collapsed;
            pb.Visibility = Visibility.Visible;
            pb.Focus();
        }
        UpdateHints();
    }

    // ───────── create account ─────────
    static bool IsEmail(string s)
    {
        try { return new MailAddress(s).Address == s && s.Contains('.'); }
        catch { return false; }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        string name = FullNameBox.Text.Trim();
        string user = UserBox.Text.Trim();
        string email = EmailBox.Text.Trim();
        string pass = ReadPwd(PwdBox, PwdText, PwdToggle);
        string confirm = ReadPwd(ConfirmBox, ConfirmText, ConfirmToggle);

        if (name.Length == 0 || user.Length == 0 || email.Length == 0 || pass.Length == 0 || confirm.Length == 0)
        {
            MessageBox.Show("Please fill in all the fields.", "Create Account");
            return;
        }
        if (user.Length < 3)
        {
            MessageBox.Show("Username or School ID must be at least 3 characters.", "Create Account");
            return;
        }
        if (!IsEmail(email))
        {
            MessageBox.Show("Please enter a valid email address.", "Create Account");
            return;
        }
        if (pass.Length < 6)
        {
            MessageBox.Show("Password must be at least 6 characters.", "Create Account");
            return;
        }
        if (pass != confirm)
        {
            MessageBox.Show("The two passwords do not match.", "Create Account");
            return;
        }

        string? error;
        try
        {
            error = Database.CreateAccount(user, name, email, pass);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Database error: " + ex.Message, "Create Account");
            return;
        }

        if (error != null)
        {
            MessageBox.Show(error, "Create Account");
            return;
        }

        CreatedUsername = user;
        MessageBox.Show("Account created! You can now log in.", "Create Account");
        DialogResult = true;
    }

    private void BackToLogin_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}