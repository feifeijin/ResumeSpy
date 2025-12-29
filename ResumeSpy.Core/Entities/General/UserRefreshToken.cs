using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeSpy.Core.Entities.General
{
    public class UserRefreshToken : Base<string>
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string JwtId { get; set; } = string.Empty;

        public bool IsRevoked { get; set; }
        public bool IsUsed { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
    }
}
