using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ResumeSpy.UI.Middlewares;
using ResumeSpy.UI.Services;

namespace ResumeSpy.UI.Filters
{
    /// <summary>
    /// Gate for AI-backed endpoints (import, chat, tailor):
    ///   1. Rejects requests that have neither an authenticated user nor an
    ///      anonymous user GUID — closes the unauthenticated-DoS hole.
    ///   2. Enforces a per-identity daily call cap via <see cref="IAiQuotaService"/>.
    /// </summary>
    public class AiAccessFilter : IAsyncActionFilter
    {
        private readonly IAiQuotaService _quotaService;
        private readonly ILogger<AiAccessFilter> _logger;

        public AiAccessFilter(IAiQuotaService quotaService, ILogger<AiAccessFilter> logger)
        {
            _quotaService = quotaService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var userId = http.GetEffectiveUserId();
            var anonymousUserId = http.GetAnonymousUserId();

            string identityKey;
            if (!string.IsNullOrEmpty(userId))
            {
                identityKey = $"user:{userId}";
            }
            else if (anonymousUserId.HasValue)
            {
                identityKey = $"anon:{anonymousUserId.Value}";
            }
            else
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Authentication or anonymous user identity is required for AI endpoints."
                });
                return;
            }

            var quota = _quotaService.TryConsume(identityKey);
            if (!quota.Allowed)
            {
                _logger.LogWarning(
                    "AI daily quota exceeded for {Identity} (limit {Max}).",
                    identityKey, quota.Max);

                context.Result = new ObjectResult(new
                {
                    error = $"Daily AI call limit of {quota.Max} reached. Try again tomorrow or sign in for a higher quota."
                })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
                return;
            }

            await next();
        }
    }
}
