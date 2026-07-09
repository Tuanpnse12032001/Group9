using System;

namespace Group9.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int ChatSessionId { get; set; }

        public ChatSession? ChatSession { get; set; }

        public string Sender { get; set; } = string.Empty; // "User" or "Bot"

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
