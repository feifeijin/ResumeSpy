namespace ResumeSpy.Core.Entities.General
{
    /// <summary>
    /// Maps an external auth provider identity (Supabase sub) to a local ApplicationUser.
    /// One ApplicationUser can have many identities (magic link + Google + GitHub = same person).
    /// </summary>
    public class UserIdentity
    {
        public int Id { get; set; }

        /// <summary>FK to ApplicationUser.Id (the stable local user ID).</summary>
        public string UserId { get; set; } = null!;

        /// <summary>"email" | "google" | "github"</summary>
        public string Provider { get; set; } = null!;

        /// <summary>Supabase sub (UUID) for this provider session.</summary>
        public string ProviderUserId { get; set; } = null!;

        public string? Email { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}
