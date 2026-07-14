using System;

namespace Group9.DTOs
{
    // ── Session ──────────────────────────────────────────────────────────────────

    public class ChatSessionResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int? DocumentId { get; set; }

        public string? DocumentTitle { get; set; }
    }

    // ── Message ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Request gửi tin nhắn. Session được tự động tạo nếu SessionId không cung cấp.
    /// </summary>
    public class SendMessageRequest
    {
        /// <summary>
        /// Nội dung tin nhắn của người dùng.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// ID phiên trò chuyện hiện có. Nếu null → server tự tạo session mới.
        /// </summary>
        public int? SessionId { get; set; }

        /// <summary>
        /// Liên kết với tài liệu (chỉ dùng khi tạo session mới).
        /// </summary>
        public int? DocumentId { get; set; }
    }

    /// <summary>
    /// Phản hồi sau khi gửi tin nhắn (bao gồm cả thông tin session).
    /// </summary>
    public class SendMessageResponse
    {
        public int SessionId { get; set; }

        public string SessionTitle { get; set; } = string.Empty;

        public bool IsNewSession { get; set; }

        public ChatMessageResponse BotMessage { get; set; } = new();
    }

    public class ChatMessageResponse
    {
        public int Id { get; set; }

        public string Sender { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
