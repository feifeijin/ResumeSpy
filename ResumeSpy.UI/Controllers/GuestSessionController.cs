using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Core.Entities.General;
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

                // Reuse existing valid session if present; otherwise create/reuse via service
                var existingSessionResult = await TryGetValidSessionAsync(ipAddress);
                if (existingSessionResult.IsValid && existingSessionResult.Session != null)
                {
                    SetGuestSessionCookie(existingSessionResult.Session.Id, existingSessionResult.Session.ExpiresAt);
                    return Ok(new CreateGuestSessionResponse
                    {
                        SessionId = existingSessionResult.Session.Id,
                        ExpiresAt = existingSessionResult.Session.ExpiresAt
                    });
                }

                // Create or reuse session via service (service reuses active session by fingerprint)
                var session = await _guestSessionService.CreateGuestSessionAsync(ipAddress, userAgent);
                SetGuestSessionCookie(session.Id, session.ExpiresAt);

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
                var validation = await GetValidatedSessionAsync();
                if (validation.ErrorResult != null)
                {
                    return validation.ErrorResult;
                }

                var count = await _guestSessionService.GetResumeCountAsync(validation.SessionId);
                var hasReachedLimit = await _guestSessionService.HasReachedResumeLimitAsync(validation.SessionId);

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
                var validation = await GetValidatedSessionAsync();
                if (validation.ErrorResult != null)
                {
                    return validation.ErrorResult;
                }

                var session = await _guestSessionService.GetGuestSessionAsync(validation.SessionId);
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
            // First check if middleware set it in context items (fresh session from same request)
            if (HttpContext.Items.TryGetValue("GuestSessionId", out var contextSessionId) && contextSessionId is Guid)
            {
                return (Guid)contextSessionId;
            }

            // Fall back to cookie for subsequent requests
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

        private void SetGuestSessionCookie(Guid sessionId, DateTime expiresAt)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, // align with middleware to support SPA cross-origin
                Expires = expiresAt,
                Path = "/"
            };

            Response.Cookies.Append(GUEST_SESSION_COOKIE, sessionId.ToString(), cookieOptions);
        }

        private async Task<(bool IsValid, GuestSession? Session)> TryGetValidSessionAsync(string ipAddress)
        {
            var existingSessionId = GetGuestSessionIdFromCookie();
            if (!existingSessionId.HasValue)
            {
                return (false, null);
            }

            var isValid = await _guestSessionService.ValidateGuestSessionAsync(existingSessionId.Value, ipAddress);
            if (!isValid)
            {
                ClearGuestSessionCookie();
                return (false, null);
            }

            var existingSession = await _guestSessionService.GetGuestSessionAsync(existingSessionId.Value);
            return existingSession != null
                ? (true, existingSession)
                : (false, null);
        }

        private async Task<(bool IsValid, Guid SessionId, IActionResult? ErrorResult)> GetValidatedSessionAsync()
        {
            var sessionId = GetGuestSessionIdFromCookie();
            if (!sessionId.HasValue)
            {
                return (false, Guid.Empty, BadRequest(new { error = "No active guest session" }));
            }

            var ipAddress = GetClientIpAddress();
            var isValid = await _guestSessionService.ValidateGuestSessionAsync(sessionId.Value, ipAddress);
            if (!isValid)
            {
                ClearGuestSessionCookie();
                return (false, Guid.Empty, Unauthorized(new { error = "Guest session is invalid or expired" }));
            }

            return (true, sessionId.Value, null);
        }

        #endregion
    }
}
