using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IResumeManagementService _resumeManagementService;
        private readonly ILogger<AuthController> _logger;
        private const string ANONYMOUS_ID_HEADER = "X-Anonymous-Id";

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IResumeManagementService resumeManagementService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _resumeManagementService = resumeManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Syncs Supabase auth session with the local user database.
        /// Called by the frontend after Supabase login to ensure a local user record exists.
        /// </summary>
        [HttpPost("sync")]
        [Authorize]
        public async Task<IActionResult> SyncSession()
        {
            var supabaseUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");
            var email = User.FindFirstValue(ClaimTypes.Email)
                     ?? User.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(supabaseUserId) || string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new AuthSyncResponse
                {
                    Succeeded = false,
                    Errors = new[] { "Invalid token: missing user ID or email." }
                });
            }

            // Try to find existing user by Supabase ID first, then by email
            var user = await _userManager.FindByIdAsync(supabaseUserId)
                    ?? await _userManager.FindByEmailAsync(email);

            var isNewUser = false;

            if (user == null)
            {
                // Create a new local user record
                user = new ApplicationUser
                {
                    Id = supabaseUserId,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = email.Split('@')[0]
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return BadRequest(new AuthSyncResponse
                    {
                        Succeeded = false,
                        Errors = createResult.Errors.Select(e => e.Description)
                    });
                }

                isNewUser = true;
                _logger.LogInformation("Created local user {UserId} for {Email}", supabaseUserId, email);
            }

            // Convert guest session to user if exists
            var convertedCount = await TryConvertGuestSessionAsync(user.Id);

            return Ok(new AuthSyncResponse
            {
                Succeeded = true,
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsNewUser = isNewUser,
                ConvertedResumeCount = convertedCount
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // Stateless — token revocation is handled by the Supabase client.
            return NoContent();
        }

        /// <summary>
        /// Attempts to convert an anonymous user to a registered user.
        /// Errors are logged but do not fail the authentication flow.
        /// </summary>
        /// <summary>
        /// Returns the number of converted resumes, or -1 if conversion failed.
        /// </summary>
        private async Task<int> TryConvertGuestSessionAsync(string? userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return 0;
                }

                if (Request.Headers.TryGetValue(ANONYMOUS_ID_HEADER, out var anonymousIdStr) &&
                    Guid.TryParse(anonymousIdStr.ToString(), out var anonymousUserId))
                {
                    var resumeCount = await _resumeManagementService.ConvertAnonymousToUserAsync(anonymousUserId, userId);
                    
                    if (resumeCount > 0)
                    {
                        _logger.LogInformation("Converted {ResumeCount} anonymous resumes to user {UserId} from anonymous user {AnonymousUserId}", resumeCount, userId, anonymousUserId);
                    }
                    return resumeCount;
                }
                return 0;
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the authentication
                _logger.LogError(ex, "Failed to convert anonymous user for user {UserId}. User can still log in.", userId);
                return -1;
            }
        }
    }
}
