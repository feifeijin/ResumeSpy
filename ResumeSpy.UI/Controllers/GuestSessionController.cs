using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Models;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/guest-session")]
    public class GuestSessionController : ControllerBase
    {
        private readonly IGuestSessionService _guestSessionService;
        private readonly ILogger<GuestSessionController> _logger;
        private const string GUEST_SESSION_COOKIE = "X-Guest-Session-Id";

        public GuestSessionController(IGuestSessionService guestSessionService, ILogger<GuestSessionController> logger)
        {
            _guestSessionService = guestSessionService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new guest session
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateGuestSession([FromBody] CreateGuestSessionRequest request)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = request?.UserAgent ?? HttpContext.Request.Headers["User-Agent"].ToString();

                var session = await _guestSessionService.CreateGuestSessionAsync(ipAddress, userAgent);

                // Set HTTP-only, Secure cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = session.ExpiresAt
                };

                Response.Cookies.Append(GUEST_SESSION_COOKIE, session.Id.ToString(), cookieOptions);

                return Ok(new CreateGuestSessionResponse
                {
                    SessionId = session.Id,
                    ExpiresAt = session.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating guest session: {ex.Message}");
                return StatusCode(500, new { error = "Failed to create guest session" });
            }
        }

        /// <summary>
        /// Checks resume quota for a guest session
        /// </summary>
        [HttpGet("check-quota")]
        public async Task<IActionResult> CheckResumeQuota()
        {
            try
            {
                var sessionId = GetGuestSessionIdFromCookie();
                if (!sessionId.HasValue)
                {
                    return BadRequest(new { error = "No active guest session" });
                }

                var isValid = await _guestSessionService.ValidateGuestSessionAsync(sessionId.Value, GetClientIpAddress());
                if (!isValid)
                {
                    ClearGuestSessionCookie();
                    return Unauthorized(new { error = "Guest session is invalid or expired" });
                }

                var count = await _guestSessionService.GetResumeCountAsync(sessionId.Value);
                var hasReachedLimit = await _guestSessionService.HasReachedResumeLimitAsync(sessionId.Value);

                return Ok(new CheckResumeQuotaResponse
                {
                    CurrentCount = count,
                    MaxAllowed = 1,
                    CanCreateResume = !hasReachedLimit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking resume quota: {ex.Message}");
                return StatusCode(500, new { error = "Failed to check resume quota" });
            }
        }

        /// <summary>
        /// Gets current guest session info
        /// </summary>
        [HttpGet("info")]
        public async Task<IActionResult> GetSessionInfo()
        {
            try
            {
                var sessionId = GetGuestSessionIdFromCookie();
                if (!sessionId.HasValue)
                {
                    return BadRequest(new { error = "No active guest session" });
                }

                var session = await _guestSessionService.GetGuestSessionAsync(sessionId.Value);
                if (session == null)
                {
                    ClearGuestSessionCookie();
                    return NotFound(new { error = "Guest session not found" });
                }

                return Ok(new
                {
                    sessionId = session.Id,
                    resumeCount = session.ResumeCount,
                    expiresAt = session.ExpiresAt,
                    isConverted = session.IsConverted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting session info: {ex.Message}");
                return StatusCode(500, new { error = "Failed to get session info" });
            }
        }

        /// <summary>
        /// Clears the guest session
        /// </summary>
        [HttpPost("logout")]
        public IActionResult LogoutGuest()
        {
            ClearGuestSessionCookie();
            return Ok(new { message = "Guest session cleared" });
        }

        #region Helper Methods

        private string GetClientIpAddress()
        {
            // Check for IP behind proxy
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0];
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        private Guid? GetGuestSessionIdFromCookie()
        {
            if (HttpContext.Request.Cookies.TryGetValue(GUEST_SESSION_COOKIE, out var sessionIdStr))
            {
                if (Guid.TryParse(sessionIdStr, out var sessionId))
                {
                    return sessionId;
                }
            }

            return null;
        }

        private void ClearGuestSessionCookie()
        {
            Response.Cookies.Delete(GUEST_SESSION_COOKIE);
        }

        #endregion
    }
}
