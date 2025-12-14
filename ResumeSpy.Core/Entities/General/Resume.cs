using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Entities.General
{
    [Table("Resumes")]
    public class Resume : Base<string>
    {

        [Required]
        public string Title { get; set; }
        
        public int ResumeDetailCount { get; set; }
        
        public string? ResumeImgPath { get; set; }

        /// <summary>
        /// Foreign key to AspNetUsers table. Null for guest resumes.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Unique identifier for guest sessions. Null for authenticated user resumes.
        /// </summary>
        public Guid? GuestSessionId { get; set; }

        /// <summary>
        /// Flag to indicate if this resume belongs to a guest user.
        /// </summary>
        public bool IsGuest { get; set; } = false;

        /// <summary>
        /// IP address of the guest who created this resume (for security/audit).
        /// </summary>
        public string? CreatedIpAddress { get; set; }

        /// <summary>
        /// Expiration date for guest resumes. After this date, the resume may be deleted.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual GuestSession? GuestSession { get; set; }
       
        public virtual ICollection<ResumeDetail> ResumeDetails { get; set; } = new List<ResumeDetail>();
    }
}