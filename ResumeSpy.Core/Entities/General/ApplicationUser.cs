using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ResumeSpy.Core.Entities.General
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? JobTitle { get; set; }
        public string? Organization { get; set; }
        public bool IsExternalLogin { get; set; }

        public ICollection<Resume> Resumes { get; set; } = new HashSet<Resume>();
        public ICollection<UserIdentity> Identities { get; set; } = new HashSet<UserIdentity>();
    }
}
