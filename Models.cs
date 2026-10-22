namespace CampusEventTicketing;

public class DashEvent
{
    public int Id { get; set; }
    public int No { get; set; }
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public string Location { get; set; } = "";
    public string Status { get; set; } = "Upcoming";
    public int TotalTickets { get; set; }
    public int Sold { get; set; }
    public string DateText => Date.ToString("MMM dd, yyyy");
}

public class DashStudent
{
    public int Id { get; set; }
    public int No { get; set; }
    public string StudentId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Course { get; set; } = "";
    public string Year { get; set; } = "";
    public string Status { get; set; } = "Active";
}

public class DashTicketRow
{
    public int No { get; set; }
    public string EventName { get; set; } = "";
    public int Total { get; set; }
    public int Available { get; set; }
    public int Sold { get; set; }
    public string Status { get; set; } = "Active";
    public DashEvent? Event { get; set; }
}

public class DashSalesRow
{
    public int No { get; set; }
    public string EventName { get; set; } = "";
    public int Total { get; set; }
    public int Sold { get; set; }
    public string Percent { get; set; } = "";
}

public class DashCourseRow
{
    public int No { get; set; }
    public string Course { get; set; } = "";
    public int Count { get; set; }
}

public class DashProblem
{
    public int Id { get; set; }
    public int No { get; set; }
    public string When { get; set; } = "";
    public string Student { get; set; } = "";
    public string Issue { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open";
}
