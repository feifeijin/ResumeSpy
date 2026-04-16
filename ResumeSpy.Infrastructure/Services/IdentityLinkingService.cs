using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.Repositories;
using ResumeSpy.Core.Interfaces.Services;

namespace ResumeSpy.Infrastructure.Services
{
    /// <summary>
    /// Resolves a Supabase auth callback to a local ApplicationUser using a three-step strategy:
    ///
    ///   1. Identity lookup   — (provider, providerUserId) already known → return its user.
    ///   2. Email merge       — new provider, verified email matches existing user → link + return.
    ///   3. New user          — no match found → create ApplicationUser + identity.
    ///
    /// Security rule: email is used as a merge hint ONLY when EmailVerified = true.
    /// </summary>
    public class IdentityLinkingService : IIdentityLinkingService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserIdentityRepository _identityRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IdentityLinkingService> _logger;

        public IdentityLinkingService(
            UserManager<ApplicationUser> userManager,
            IUserIdentityRepository identityRepo,
            IUnitOfWork unitOfWork,
            ILogger<IdentityLinkingService> logger)
        {
            _userManager = userManager;
            _identityRepo = identityRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IdentityLinkingResult> ResolveUserAsync(AuthCallbackContext ctx)
        {
            // ── Step 1: known identity ────────────────────────────────────────────────
            var identity = await _identityRepo.FindByProviderAsync(ctx.Provider, ctx.ProviderUserId);
            if (identity != null)
            {
                await _identityRepo.UpdateLastLoginAsync(identity.Id);
                _logger.LogDebug("Identity resolved via provider lookup: {Provider}/{ProviderUserId} → {UserId}",
                    ctx.Provider, ctx.ProviderUserId, identity.UserId);
                return new IdentityLinkingResult(identity.User, IsNewUser: false, IsNewIdentityLinked: false);
            }

            // ── Step 2: email merge (verified emails only) ────────────────────────────
            ApplicationUser? user = null;
            bool isNewUser = false;
            bool isNewIdentityLinked = false;

            if (ctx.EmailVerified && !string.IsNullOrWhiteSpace(ctx.Email))
            {
                user = await _userManager.FindByEmailAsync(ctx.Email);
                if (user != null)
                {
                    isNewIdentityLinked = true;
                    _logger.LogInformation(
                        "Linking new identity {Provider}/{ProviderUserId} to existing user {UserId} via verified email {Email}",
                        ctx.Provider, ctx.ProviderUserId, user.Id, ctx.Email);
                }
            }

            // ── Step 3: create user if still not resolved ─────────────────────────────
            if (user == null)
            {
                user = new ApplicationUser
                {
                    // Use Supabase sub as the local Id for brand-new users so the JWT sub
                    // and local Id stay in sync on first login (no EffectiveUserId indirection needed).
                    Id = ctx.ProviderUserId,
                    UserName = ctx.ProviderUserId, // unique; email may not be set for unverified
                    Email = ctx.EmailVerified ? ctx.Email : null,
                    EmailConfirmed = ctx.EmailVerified,
                    DisplayName = ctx.DisplayName ?? ctx.Email?.Split('@')[0],
                    AvatarUrl = ctx.AvatarUrl,
                    IsExternalLogin = ctx.Provider != "email",
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create ApplicationUser for {Provider}/{ProviderUserId}: {Errors}",
                        ctx.Provider, ctx.ProviderUserId, errors);
                    throw new InvalidOperationException($"Could not create local user: {errors}");
                }

                isNewUser = true;
                _logger.LogInformation("Created new ApplicationUser {UserId} for {Provider}/{Email}",
                    user.Id, ctx.Provider, ctx.Email);
            }

            // ── Create identity link ──────────────────────────────────────────────────
            var newIdentity = new UserIdentity
            {
                UserId = user.Id,
                Provider = ctx.Provider,
                ProviderUserId = ctx.ProviderUserId,
                Email = ctx.Email,
                EmailVerified = ctx.EmailVerified,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };

            await _identityRepo.CreateAsync(newIdentity);
            await _unitOfWork.SaveChangesAsync();

            return new IdentityLinkingResult(user, isNewUser, isNewIdentityLinked);
        }
    }
}
