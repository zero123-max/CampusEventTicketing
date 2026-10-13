using System;
using System.Collections.Generic;

namespace CampusEventTicketing
{
    public enum TicketStatus
    {
        Open,
        Escalated,
        PendingAdmin,
        Resolved
    }

    public class SupportTicket
    {
        public string TicketId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        // Collected user details
        public string FullName { get; set; } = "";
        public string StudentOrUserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string EventOrTicketRef { get; set; } = "";
        public string IssueSummary { get; set; } = "";

        // Full chat transcript up to the point of handoff
        public List<string> ConversationHistory { get; set; } = new();
    }
}
