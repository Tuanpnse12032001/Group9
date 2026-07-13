namespace Group9.DTOs
{
    public class CloudFileResponse
    {
        public int Id { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string PublicId { get; set; } = string.Empty;

        public string SecureUrl { get; set; } = string.Empty;

        public string ResourceType { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long Size { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UploadedAt { get; set; }
    }

    public class PreviewFileResponse
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string PreviewUrl { get; set; } = string.Empty;
    }
}