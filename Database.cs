using MySqlConnector;

namespace CampusEventTicketing;

/// <summary>
/// All database work lives here (MySQL 8.4).
/// The database and tables are created automatically the first time the app runs.
/// </summary>
public static partial class Database
{
    const string DbName = "campus";
    const string Server = "Server=localhost;Port=3306;User ID=root;Password=mypass123;";

    static readonly string ConnStr = $"{Server}Database={DbName};";

    static MySqlConnection Open(bool withDatabase = true)
    {
        var c = new MySqlConnection(withDatabase ? ConnStr : Server);
        c.Open();
        return c;
    }

    static MySqlCommand Cmd(MySqlConnection c, string sql, params (string Name, object? Value)[] ps)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return cmd;
    }

    static int Count(MySqlConnection c, string table) =>
        Convert.ToInt32(Cmd(c, $"SELECT COUNT(*) FROM {table}").ExecuteScalar());

    // ───────── create database + tables + sample data ─────────
    public static void Initialize()
    {
        using (var s = Open(withDatabase: false))
            Cmd(s, $"CREATE DATABASE IF NOT EXISTS `{DbName}` CHARACTER SET utf8mb4").ExecuteNonQuery();

        using var c = Open();

        Cmd(c, @"CREATE TABLE IF NOT EXISTS Events (
                    Id INT PRIMARY KEY AUTO_INCREMENT,
                    Name VARCHAR(150) NOT NULL,
                    EventDate DATE NOT NULL,
                    Location VARCHAR(150) NOT NULL,
                    Status VARCHAR(30) NOT NULL DEFAULT 'Upcoming',
                    TotalTickets INT NOT NULL DEFAULT 0,
                    Sold INT NOT NULL DEFAULT 0)").ExecuteNonQuery();

        Cmd(c, @"CREATE TABLE IF NOT EXISTS Students (
                    Id INT PRIMARY KEY AUTO_INCREMENT,
                    StudentId VARCHAR(30) NOT NULL UNIQUE,
                    Name VARCHAR(150) NOT NULL,
                    Course VARCHAR(50) NOT NULL,
                    `Year` VARCHAR(30) NOT NULL,
                    Status VARCHAR(30) NOT NULL DEFAULT 'Active')").ExecuteNonQuery();

        Cmd(c, @"CREATE TABLE IF NOT EXISTS Problems (
                    Id INT PRIMARY KEY AUTO_INCREMENT,
                    WhenText VARCHAR(50) NOT NULL,
                    Student VARCHAR(150) NOT NULL,
                    Issue VARCHAR(100) NOT NULL,
                    Description TEXT NOT NULL,
                    Status VARCHAR(30) NOT NULL DEFAULT 'Open')").ExecuteNonQuery();

        if (Count(c, "Events") == 0)
        {
            (string, string, string, int, int)[] ev =
            {
                ("Tech Summit 2025", "2025-08-15", "University Hall", 500, 20),
                ("Computer Science Workshop", "2025-08-28", "Lab 1", 300, 0),
                ("Community Outreach", "2025-09-10", "Brgy. Pasil", 200, 0),
                ("Game Fest 2025", "2025-10-05", "Cafeteria", 400, 0),
                ("Christmas Program", "2025-12-12", "University Hall", 250, 0),
            };
            foreach (var (n, d, l, t, s) in ev)
                Cmd(c, "INSERT INTO Events (Name, EventDate, Location, Status, TotalTickets, Sold) VALUES (@n,@d,@l,'Upcoming',@t,@s)",
                    ("@n", n), ("@d", d), ("@l", l), ("@t", t), ("@s", s)).ExecuteNonQuery();
        }

        if (Count(c, "Students") == 0)
        {
            (string, string, string, string)[] st =
            {
                ("2024001", "Aaron Santos", "BSIT", "1st Year"),
                ("2024002", "Abigail Reyes", "BSCS", "1st Year"),
                ("2024003", "Alexander Cruz", "BSIT", "2nd Year"),
                ("2024004", "Althea Ramos", "BSCS", "1st Year"),
                ("2024005", "Bea Lopez", "BSIT", "3rd Year"),
                ("2024006", "Benjamin Garcia", "BSCS", "2nd Year"),
                ("2024007", "Catherine Dela Cruz", "BSIT", "1st Year"),
                ("2024008", "Christian Villanueva", "BSCS", "4th Year"),
                ("2024009", "Daniel Flores", "BSIT", "2nd Year"),
                ("2024010", "Diana Torres", "BSCS", "1st Year"),
            };
            foreach (var (id, n, co, y) in st)
                Cmd(c, "INSERT INTO Students (StudentId, Name, Course, `Year`) VALUES (@i,@n,@c,@y)",
                    ("@i", id), ("@n", n), ("@c", co), ("@y", y)).ExecuteNonQuery();
        }

        if (Count(c, "Problems") == 0)
        {
            (string, string, string, string, string)[] pr =
            {
                ("Aug 12, 2025 10:24 AM", "Juan Dela Cruz", "Technical Issue", "Cannot get ticket QR code.", "Open"),
                ("Aug 15, 2025 02:17 PM", "Maria Santos", "Event Access", "Late entry, no ticket found.", "In Progress"),
                ("Aug 18, 2025 11:03 AM", "Patrick Lim", "Payment", "Payment not reflected.", "Resolved"),
                ("Aug 22, 2025 04:32 PM", "Samantha Cruz", "General", "Need help with event schedule.", "Open"),
                ("Aug 25, 2025 08:45 AM", "Rafael Torres", "Technical Issue", "App keeps crashing.", "Resolved"),
                ("Aug 28, 2025 01:12 PM", "Alyssa Mendoza", "Event Access", "Can't find the venue.", "In Progress"),
                ("Sep 01, 2025 09:30 AM", "Daniel Reyes", "Ticket Refund", "Refund request for duplicate ticket.", "Open"),
                ("Sep 05, 2025 03:20 PM", "Bea Lopez", "Other", "Suggestion for next event.", "Resolved"),
            };
            foreach (var (w, s, i, d, st) in pr)
                Cmd(c, "INSERT INTO Problems (WhenText, Student, Issue, Description, Status) VALUES (@w,@s,@i,@d,@st)",
                    ("@w", w), ("@s", s), ("@i", i), ("@d", d), ("@st", st)).ExecuteNonQuery();
        }
    }

    // ───────── events ─────────
    public static List<DashEvent> GetEvents()
    {
        var list = new List<DashEvent>();
        using var c = Open();
        using var r = Cmd(c, "SELECT Id, Name, EventDate, Location, Status, TotalTickets, Sold FROM Events ORDER BY EventDate, Id").ExecuteReader();
        while (r.Read())
        {
            list.Add(new DashEvent
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Date = r.GetDateTime(2),
                Location = r.GetString(3),
                Status = r.GetString(4),
                TotalTickets = r.GetInt32(5),
                Sold = r.GetInt32(6)
            });
        }
        return list;
    }

    public static void AddEvent(DashEvent e)
    {
        using var c = Open();
        Cmd(c, "INSERT INTO Events (Name, EventDate, Location, Status, TotalTickets, Sold) VALUES (@n,@d,@l,@s,@t,@o)",
            ("@n", e.Name), ("@d", e.Date.Date), ("@l", e.Location),
            ("@s", e.Status), ("@t", e.TotalTickets), ("@o", e.Sold)).ExecuteNonQuery();
    }

    public static void UpdateEvent(DashEvent e)
    {
        using var c = Open();
        Cmd(c, "UPDATE Events SET Name=@n, EventDate=@d, Location=@l, Status=@s WHERE Id=@id",
            ("@n", e.Name), ("@d", e.Date.Date), ("@l", e.Location),
            ("@s", e.Status), ("@id", e.Id)).ExecuteNonQuery();
    }

    public static void DeleteEvent(int id)
    {
        using var c = Open();
        Cmd(c, "DELETE FROM Events WHERE Id=@id", ("@id", id)).ExecuteNonQuery();
    }

    public static void UpdateTickets(int eventId, int total, int sold)
    {
        using var c = Open();
        Cmd(c, "UPDATE Events SET TotalTickets=@t, Sold=@s WHERE Id=@id",
            ("@t", total), ("@s", sold), ("@id", eventId)).ExecuteNonQuery();
    }

    // ───────── students ─────────
    public static List<DashStudent> GetStudents(string letter, string query)
    {
        var list = new List<DashStudent>();
        using var c = Open();
        using var r = Cmd(c,
            @"SELECT Id, StudentId, Name, Course, `Year`, Status FROM Students
              WHERE (@l = 'All' OR Name LIKE CONCAT(@l, '%'))
                AND (@q = '' OR Name LIKE CONCAT('%', @q, '%') OR StudentId LIKE CONCAT('%', @q, '%'))
              ORDER BY Name",
            ("@l", letter), ("@q", query)).ExecuteReader();
        while (r.Read())
        {
            list.Add(new DashStudent
            {
                Id = r.GetInt32(0),
                StudentId = r.GetString(1),
                Name = r.GetString(2),
                Course = r.GetString(3),
                Year = r.GetString(4),
                Status = r.GetString(5)
            });
        }
        return list;
    }

    public static int CountStudents()
    {
        using var c = Open();
        return Count(c, "Students");
    }

    public static List<DashCourseRow> GetCourseCounts()
    {
        var list = new List<DashCourseRow>();
        using var c = Open();
        using var r = Cmd(c, "SELECT Course, COUNT(*) FROM Students GROUP BY Course ORDER BY Course").ExecuteReader();
        int n = 1;
        while (r.Read())
            list.Add(new DashCourseRow { No = n++, Course = r.GetString(0), Count = Convert.ToInt32(r.GetValue(1)) });
        return list;
    }

    // ───────── problem reports ─────────
    public static List<DashProblem> GetProblems()
    {
        var list = new List<DashProblem>();
        using var c = Open();
        using var r = Cmd(c, "SELECT Id, WhenText, Student, Issue, Description, Status FROM Problems ORDER BY Id").ExecuteReader();
        while (r.Read())
        {
            list.Add(new DashProblem
            {
                Id = r.GetInt32(0),
                When = r.GetString(1),
                Student = r.GetString(2),
                Issue = r.GetString(3),
                Description = r.GetString(4),
                Status = r.GetString(5)
            });
        }
        return list;
    }
}