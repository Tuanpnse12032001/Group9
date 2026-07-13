namespace Group9.Models
{
    public class CloudFile
    {
        public int Id { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string PublicId { get; set; } = string.Empty;

        public string SecureUrl { get; set; } = string.Empty;

        public string ResourceType { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long Size { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UploadedAt { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}