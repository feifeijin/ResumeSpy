using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IResumeManagementService _resumeManagementService;
        private readonly ILogger<AuthController> _logger;
        private const string GUEST_SESSION_COOKIE = "X-Guest-Session-Id";

        public AuthController(
            IAuthService authService, 
            IResumeManagementService resumeManagementService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _resumeManagementService = resumeManagementService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var response = await _authService.RegisterAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                return BadRequest(response);
            }

            // Convert guest session to user if exists
            await TryConvertGuestSessionAsync(response.UserId);

            return Ok(response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var response = await _authService.LoginAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                return Unauthorized(response);
            }

            // Convert guest session to user if exists
            await TryConvertGuestSessionAsync(response.UserId);

            return Ok(response);
        }

        [HttpPost("magic/request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestEmailLink([FromBody] EmailLinkRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var response = await _authService.RequestEmailLinkAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("magic/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailLink([FromBody] ConfirmEmailLinkRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var response = await _authService.ConfirmEmailLinkAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                return BadRequest(response);
            }

            // Convert guest session to user if exists
            await TryConvertGuestSessionAsync(response.UserId);

            return Ok(response);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var response = await _authService.RefreshTokenAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpPost("external")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalAuthRequest request, CancellationToken cancellationToken)
        {
            var response = await _authService.ExternalLoginAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
            // Convert guest session to user if exists
            await TryConvertGuestSessionAsync(response.UserId);

                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Logout attempted without a user identifier in the token.");
                return Unauthorized();
            }

            await _authService.LogoutAsync(userId, request, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Attempts to convert a guest session to a registered user.
        /// Errors are logged but do not fail the authentication flow.
        /// </summary>
        private async Task TryConvertGuestSessionAsync(string userId)
        {
            try
            {
                if (Request.Cookies.TryGetValue(GUEST_SESSION_COOKIE, out var sessionIdStr) &&
                    Guid.TryParse(sessionIdStr, out var sessionGuid))
                {
                    var resumeCount = await _resumeManagementService.ConvertGuestToUserAsync(sessionGuid, userId);
                    
                    if (resumeCount > 0)
                    {
                        _logger.LogInformation($"Converted {resumeCount} guest resumes to user {userId} from session {sessionGuid}");
                    }
                    
                    // Clear the guest session cookie after conversion
                    Response.Cookies.Delete(GUEST_SESSION_COOKIE, new CookieOptions
                    {
                        Path = "/",
                        SameSite = SameSiteMode.None,
                        Secure = true
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the authentication
                _logger.LogError(ex, $"Failed to convert guest session for user {userId}. User can still log in.");
            }
        }
    }
}
