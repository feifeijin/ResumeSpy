using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeSpy.Core.Entities.General
{
    [Table("GuestSessions")]
    public class GuestSession : Base<Guid>
    {
        [Required]
        public string IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public int ResumeCount { get; set; } = 0;

        [Required]
        [Column(TypeName = "timestamp")]
        public DateTime ExpiresAt { get; set; }

        public bool IsConverted { get; set; } = false;

        public string? ConvertedUserId { get; set; }

        // Navigation property
        public virtual ApplicationUser? ConvertedUser { get; set; }
    }
}
