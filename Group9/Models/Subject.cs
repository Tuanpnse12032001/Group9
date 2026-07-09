using System.Collections.Generic;

namespace Group9.Models
{
    public class Subject
    {
        public int Id { get; set; }

        public string SubjectCode { get; set; } = string.Empty;

        public string SubjectName { get; set; } = string.Empty;

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
