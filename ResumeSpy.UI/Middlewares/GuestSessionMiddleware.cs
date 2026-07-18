using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Middlewares
{
    /// <summary>
    /// Middleware to extract and validate guest session from cookies
    /// </summary>
    public class GuestSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GuestSessionMiddleware> _logger;
        private const string GUEST_SESSION_COOKIE = "X-Guest-Session-Id";
        private const string GUEST_SESSION_CONTEXT_KEY = "GuestSessionId";
        private const string GUEST_IP_CONTEXT_KEY = "GuestIpAddress";

        public GuestSessionMiddleware(RequestDelegate next, ILogger<GuestSessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IGuestSessionService guestSessionService)
        {
            // Only manage guest sessions for unauthenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            Guid? sessionId = null;
            var ipAddress = GetClientIpAddress(context);
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Validate existing cookie if present
            if (context.Request.Cookies.TryGetValue(GUEST_SESSION_COOKIE, out var sessionIdStr) &&
                Guid.TryParse(sessionIdStr, out var parsedId))
            {
                // Pass IP for security logging only, not for validation
                var isValid = await guestSessionService.ValidateGuestSessionAsync(parsedId, ipAddress);
                if (isValid)
                {
                    sessionId = parsedId;
                    _logger.LogInformation($"Guest session validated: {sessionId}");
                }
                else
                {
                    _logger.LogWarning($"Guest session validation failed: {parsedId}");
                    context.Response.Cookies.Delete(GUEST_SESSION_COOKIE);
                }
            }

            // Auto-create if no valid session
            if (!sessionId.HasValue)
            {
                // Check rate limiting before creating new session
                var exceededLimit = await guestSessionService.HasExceededSessionRateLimitAsync(ipAddress);
                if (exceededLimit)
                {
                    _logger.LogWarning($"Session creation blocked - IP {ipAddress} exceeded rate limit");
                    context.Response.StatusCode = 429; // Too Many Requests
                    await context.Response.WriteAsJsonAsync(new 
                    { 
                        error = "Too many guest sessions created from your network. Please sign up for an account.",
                        code = "SESSION_RATE_LIMIT_EXCEEDED"
                    });
                    return;
                }

                var newSession = await guestSessionService.CreateGuestSessionAsync(ipAddress, userAgent);
                sessionId = newSession.Id;

                context.Response.Cookies.Append(GUEST_SESSION_COOKIE, sessionId.Value.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None, // allow cross-site requests from SPA origin
                    Expires = newSession.ExpiresAt,
                    Path = "/"
                    // Domain = "your-domain.com" // set if API uses a subdomain and you need sharing
                });

                _logger.LogInformation($"Created new guest session: {sessionId}");
            }

            // Store in context for later use
            context.Items[GUEST_SESSION_CONTEXT_KEY] = sessionId.Value;
            context.Items[GUEST_IP_CONTEXT_KEY] = ipAddress;

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for IP behind proxy
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }
    }

    public static class GuestSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGuestSessionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GuestSessionMiddleware>();
        }

        /// <summary>
        /// Gets the guest session ID from context items
        /// </summary>
        public static Guid? GetGuestSessionId(this HttpContext context)
        {
            if (context.Items.TryGetValue("GuestSessionId", out var sessionId) && sessionId is Guid)
            {
                return (Guid)sessionId;
            }
            return null;
        }

        /// <summary>
        /// Gets the guest IP address from context items
        /// </summary>
        public static string? GetGuestIpAddress(this HttpContext context)
        {
            if (context.Items.TryGetValue("GuestIpAddress", out var ipAddress) && ipAddress is string)
            {
                return (string)ipAddress;
            }
            return null;
        }
    }
}
