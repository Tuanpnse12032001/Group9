using System;

namespace Group9.Models
{
    public class Document
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string StoragePath { get; set; } = string.Empty;

        public string PublicId { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int SubjectId { get; set; }

        public Subject? Subject { get; set; }

        public int UploadedByUserId { get; set; }

        public User? UploadedByUser { get; set; }
    }
}
