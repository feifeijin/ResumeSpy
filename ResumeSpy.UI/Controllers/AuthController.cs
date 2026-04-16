using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Interfaces.Services;
using ResumeSpy.UI.Middlewares;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityLinkingService _identityLinkingService;
        private readonly IResumeManagementService _resumeManagementService;
        private readonly ILogger<AuthController> _logger;
        private const string AnonymousIdHeader = "X-Anonymous-Id";

        public AuthController(
            IIdentityLinkingService identityLinkingService,
            IResumeManagementService resumeManagementService,
            ILogger<AuthController> logger)
        {
            _identityLinkingService = identityLinkingService;
            _resumeManagementService = resumeManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Called by the frontend after every Supabase login.
        /// Ensures the local user exists, links the identity provider if new,
        /// and converts any guest resumes to the authenticated user.
        /// </summary>
        [HttpPost("sync")]
        [Authorize]
        public async Task<IActionResult> SyncSession()
        {
            var providerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");
            var email = User.FindFirstValue(ClaimTypes.Email)
                     ?? User.FindFirstValue("email");
            var provider = ExtractProvider();

            if (string.IsNullOrWhiteSpace(providerUserId))
                return Unauthorized(new AuthSyncResponse
                {
                    Succeeded = false,
                    Errors = new[] { "Invalid token: missing user ID." }
                });

            IdentityLinkingResult result;
            try
            {
                result = await _identityLinkingService.ResolveUserAsync(new AuthCallbackContext(
                    Provider: provider,
                    ProviderUserId: providerUserId,
                    Email: email,
                    EmailVerified: true,
                    DisplayName: ExtractDisplayName()
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Identity resolution failed for {Provider}/{ProviderUserId}", provider, providerUserId);
                return StatusCode(500, new AuthSyncResponse
                {
                    Succeeded = false,
                    Errors = new[] { "Failed to resolve user identity." }
                });
            }

            var user = result.User;
            var convertedCount = await TryConvertGuestSessionAsync(user.Id);

            if (result.IsNewIdentityLinked)
                _logger.LogInformation("Provider {Provider} linked to existing user {UserId}", provider, user.Id);

            return Ok(new AuthSyncResponse
            {
                Succeeded = true,
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsNewUser = result.IsNewUser,
                ConvertedResumeCount = convertedCount
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return NoContent();
        }

        private async Task<int> TryConvertGuestSessionAsync(string userId)
        {
            try
            {
                if (Request.Headers.TryGetValue(AnonymousIdHeader, out var anonymousIdStr) &&
                    Guid.TryParse(anonymousIdStr.ToString(), out var anonymousUserId))
                {
                    var count = await _resumeManagementService.ConvertAnonymousToUserAsync(anonymousUserId, userId);
                    if (count > 0)
                        _logger.LogInformation("Converted {Count} guest resumes to user {UserId}", count, userId);
                    return count;
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guest conversion failed for user {UserId}", userId);
                return -1;
            }
        }

        private string ExtractProvider()
        {
            var appMetadataJson = User.FindFirstValue("app_metadata");
            if (!string.IsNullOrEmpty(appMetadataJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(appMetadataJson);
                    if (doc.RootElement.TryGetProperty("provider", out var p))
                        return p.GetString() ?? "email";
                }
                catch { }
            }
            return "email";
        }

        private string? ExtractDisplayName()
        {
            var userMetadataJson = User.FindFirstValue("user_metadata");
            if (!string.IsNullOrEmpty(userMetadataJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(userMetadataJson);
                    if (doc.RootElement.TryGetProperty("full_name", out var n)) return n.GetString();
                    if (doc.RootElement.TryGetProperty("name", out var n2)) return n2.GetString();
                }
                catch { }
            }
            return null;
        }
    }
}
