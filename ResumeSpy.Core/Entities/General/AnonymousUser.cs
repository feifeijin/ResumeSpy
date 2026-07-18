using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeSpy.Core.Entities.General
{
    [Table("AnonymousUsers")]
    public class AnonymousUser : Base<Guid>
    {
        public int ResumeCount { get; set; } = 0;

        public bool IsConverted { get; set; } = false;

        public string? ConvertedUserId { get; set; }

        // Navigation property
        public virtual ApplicationUser? ConvertedUser { get; set; }
    }
}
