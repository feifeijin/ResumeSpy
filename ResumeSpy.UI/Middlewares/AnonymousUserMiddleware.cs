using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Middlewares
{
    /// <summary>
    /// Middleware to extract anonymous user identity from the X-Anonymous-Id header.
    /// If the header contains a valid GUID and the user is unauthenticated,
    /// the anonymous user record is ensured to exist and the ID is stored in HttpContext.Items.
    /// </summary>
    public class AnonymousUserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AnonymousUserMiddleware> _logger;
        private const string ANONYMOUS_ID_HEADER = "X-Anonymous-Id";
        private const string ANONYMOUS_USER_CONTEXT_KEY = "AnonymousUserId";

        public AnonymousUserMiddleware(RequestDelegate next, ILogger<AnonymousUserMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAnonymousUserService anonymousUserService)
        {
            // Only process anonymous identity for unauthenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // Extract anonymous user ID from header
            if (context.Request.Headers.TryGetValue(ANONYMOUS_ID_HEADER, out var headerValue) &&
                Guid.TryParse(headerValue.ToString(), out var anonymousUserId))
            {
                // Ensure the anonymous user record exists in the database
                await anonymousUserService.GetOrCreateAsync(anonymousUserId);

                // Store in context for use by controllers
                context.Items[ANONYMOUS_USER_CONTEXT_KEY] = anonymousUserId;
                _logger.LogDebug("Anonymous user identity set: {AnonymousUserId}", anonymousUserId);
            }

            await _next(context);
        }
    }

    public static class AnonymousUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseAnonymousUserMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AnonymousUserMiddleware>();
        }

        /// <summary>
        /// Gets the anonymous user ID from context items
        /// </summary>
        public static Guid? GetAnonymousUserId(this HttpContext context)
        {
            if (context.Items.TryGetValue("AnonymousUserId", out var id) && id is Guid)
            {
                return (Guid)id;
            }
            return null;
        }
    }
}
