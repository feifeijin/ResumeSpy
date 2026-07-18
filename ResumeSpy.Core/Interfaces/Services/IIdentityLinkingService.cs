using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.Services
{
    /// <summary>
    /// Input from the Supabase JWT callback (magic link or OAuth).
    /// </summary>
    public record AuthCallbackContext(
        string Provider,       // "email" | "google" | "github"
        string ProviderUserId, // Supabase sub (UUID)
        string? Email,
        bool EmailVerified,
        string? DisplayName = null,
        string? AvatarUrl = null
    );

    /// <summary>
    /// Result of resolving a callback to a local ApplicationUser.
    /// </summary>
    public record IdentityLinkingResult(
        ApplicationUser User,
        bool IsNewUser,
        bool IsNewIdentityLinked   // true when an existing user gained a new provider
    );

    /// <summary>
    /// Resolves an external auth callback to a local ApplicationUser, creating or
    /// linking identities as needed. This is the single source of truth for auth.
    /// </summary>
    public interface IIdentityLinkingService
    {
        Task<IdentityLinkingResult> ResolveUserAsync(AuthCallbackContext context);
    }
}
