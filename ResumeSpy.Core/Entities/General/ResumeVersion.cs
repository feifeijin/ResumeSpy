using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeSpy.Core.Entities.General
{
    [Table("ResumeVersions")]
    public class ResumeVersion
    {
        [Key]
        public Guid Id { get; private set; }

        [Required]
        public string ResumeDetailId { get; private set; } = string.Empty;

        [Required]
        public string Content { get; private set; } = string.Empty;

        public string? Label { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public virtual ResumeDetail? ResumeDetail { get; private set; }

        private ResumeVersion() { }

        public static ResumeVersion Create(string resumeDetailId, string content, string? label = null)
        {
            return new ResumeVersion
            {
                Id = Guid.NewGuid(),
                ResumeDetailId = resumeDetailId,
                Content = content,
                Label = label,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
