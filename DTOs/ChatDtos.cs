using System;

namespace Group9.DTOs
{
    public class CreateChatSessionRequest
    {
        public int? DocumentId { get; set; }
    }

    public class ChatSessionResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int? DocumentId { get; set; }

        public string? DocumentTitle { get; set; }
    }

    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class ChatMessageResponse
    {
        public int Id { get; set; }

        public string Sender { get; set; } = string.Empty; // "User" or "Bot"

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
