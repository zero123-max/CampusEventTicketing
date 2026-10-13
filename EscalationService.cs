using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CampusEventTicketing
{
    /// <summary>
    /// Tracks where a single chat session is in the escalation flow and
    /// drives the "ask one field at a time" data collection sequence.
    /// One instance per open ChatbotWindow / session.
    /// </summary>
    public class EscalationService
    {
        private enum Step
        {
            None,           // normal automated support
            AskingName,
            AskingEmail,
            AskingEventOrTicket,
            AskingSummary,
            Done
        }

        private Step _step = Step.None;
        private readonly SupportTicket _ticket = new();
        private readonly List<string> _transcript = new();

        public bool IsEscalating => _step != Step.None && _step != Step.Done;
        public bool IsDone => _step == Step.Done;

        // Keywords/phrases that trigger a handoff.
        private static readonly string[] EscalationTriggers =
        {
            "agent", "admin", "human", "support", "representative",
            "real person", "talk to someone", "speak to someone",
            "manual override", "override", "not working", "still not working",
            "didn't work", "doesn't work", "this is broken", "bug",
            "escalate"
        };

        public void LogUserMessage(string text) => _transcript.Add($"User: {text}");
        public void LogBotMessage(string text) => _transcript.Add($"Bot: {text}");

        /// <summary>
        /// Call this on every user message BEFORE running normal
        /// keyword/LLM troubleshooting. Returns true if the message
        /// triggered escalation mode (caller should stop and use the
        /// returned prompt instead of the normal bot reply).
        /// </summary>
        public bool CheckTrigger(string userText, out string botReply)
        {
            string text = userText.ToLowerInvariant();

            foreach (var trigger in EscalationTriggers)
            {
                if (text.Contains(trigger))
                {
                    _step = Step.AskingName;
                    botReply = "I'll connect you with an administrator. First, could you give me your full name and Student/User ID?";
                    return true;
                }
            }

            botReply = "";
            return false;
        }

        /// <summary>
        /// Call this instead of normal troubleshooting once IsEscalating
        /// is true. Feeds the user's answer into the current field and
        /// returns the next prompt (or the final confirmation).
        /// </summary>
        public string ContinueCollection(string userText)
        {
            switch (_step)
            {
                case Step.AskingName:
                    _ticket.FullName = userText.Trim();
                    _step = Step.AskingEmail;
                    return "Thanks. What's the email address on your account?";

                case Step.AskingEmail:
                    _ticket.Email = userText.Trim();
                    _step = Step.AskingEventOrTicket;
                    return "Got it. Is this related to a specific event or ticket/order ID? (Type \"N/A\" if not applicable.)";

                case Step.AskingEventOrTicket:
                    _ticket.EventOrTicketRef = userText.Trim();
                    _step = Step.AskingSummary;
                    return "Last thing — could you briefly describe the issue?";

                case Step.AskingSummary:
                    _ticket.IssueSummary = userText.Trim();
                    return FinalizeAndConfirm();

                default:
                    return "";
            }
        }

        private string FinalizeAndConfirm()
        {
            _ticket.Status = TicketStatus.PendingAdmin;
            _ticket.ConversationHistory.AddRange(_transcript);

            SupportTicketRepository.Save(_ticket);

            _step = Step.Done;

            return "I have logged your issue and transferred your request to our admin team. " +
                   "An administrator will review your ticket and contact you shortly via email.";
        }
    }

    /// <summary>
    /// Minimal persistence layer. Swap the body of Save() for a real DB
    /// call (EF Core, SQL, REST API to your ticket system, etc.) — the
    /// rest of the escalation flow doesn't need to change.
    /// </summary>
    public static class SupportTicketRepository
    {
        private static readonly string StorageFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SupportTickets");

        public static void Save(SupportTicket ticket)
        {
            Directory.CreateDirectory(StorageFolder);

            var path = Path.Combine(StorageFolder, $"{ticket.TicketId}.json");
            var json = JsonSerializer.Serialize(ticket, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, json);

            // TODO: replace/augment with a real call, e.g.:
            // await httpClient.PostAsJsonAsync("https://your-api/support-tickets", ticket);
            // or an EF Core DbContext.SupportTickets.Add(ticket); SaveChanges();
        }
    }
}
