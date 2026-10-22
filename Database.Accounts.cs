using System.Security.Cryptography;
using MySqlConnector;

namespace CampusEventTicketing;

public static partial class Database
{
    static void EnsureAccounts()
    {
        Initialize();
        using var c = Open();
        Cmd(c, @"CREATE TABLE IF NOT EXISTS Accounts (
                    Id INT PRIMARY KEY AUTO_INCREMENT,
                    Username VARCHAR(60) NOT NULL UNIQUE,
                    FullName VARCHAR(150) NOT NULL,
                    Email VARCHAR(150) NOT NULL UNIQUE,
                    PasswordHash VARCHAR(200) NOT NULL,
                    Role VARCHAR(20) NOT NULL DEFAULT 'Student',
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)").ExecuteNonQuery();
    }

    /// <summary>Returns null on success, or a message explaining why it failed.</summary>
    public static string? CreateAccount(string username, string fullName, string email, string password)
    {
        EnsureAccounts();
        using var c = Open();

        if (Convert.ToInt32(Cmd(c, "SELECT COUNT(*) FROM Accounts WHERE Username=@u", ("@u", username)).ExecuteScalar()) > 0)
            return "That username or school ID is already registered.";
        if (Convert.ToInt32(Cmd(c, "SELECT COUNT(*) FROM Accounts WHERE Email=@e", ("@e", email)).ExecuteScalar()) > 0)
            return "That email address is already registered.";

        Cmd(c, "INSERT INTO Accounts (Username, FullName, Email, PasswordHash) VALUES (@u,@n,@e,@p)",
            ("@u", username), ("@n", fullName), ("@e", email), ("@p", HashPassword(password))).ExecuteNonQuery();

        // also list the student on the admin Students page
        Cmd(c, "INSERT IGNORE INTO Students (StudentId, Name, Course, `Year`) VALUES (@s,@n,'Not set','Not set')",
            ("@s", username), ("@n", fullName)).ExecuteNonQuery();

        return null;
    }

    /// <summary>Login with username/school ID or email.</summary>
    public static bool CheckLogin(string login, string password)
    {
        EnsureAccounts();
        using var c = Open();
        var result = Cmd(c, "SELECT PasswordHash FROM Accounts WHERE (Username=@l OR Email=@l) AND Role='Student' LIMIT 1",
                         ("@l", login)).ExecuteScalar();
        return result is string stored && VerifyPassword(password, stored);
    }

    static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expected = Convert.FromBase64String(parts[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}