using System;
using System.Collections.Generic;

namespace Group9.Models
{
    public class ChatSession
    {
        public int Id { get; set; }

        public string Title { get; set; } = "Trò chuyện mới";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }

        public User? User { get; set; }

        public int? DocumentId { get; set; }

        public Document? Document { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
