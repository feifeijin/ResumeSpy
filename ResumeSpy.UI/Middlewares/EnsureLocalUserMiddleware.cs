using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Interfaces.Services;

namespace ResumeSpy.UI.Middlewares
{
    /// <summary>
    /// For every authenticated request:
    ///   1. Extracts provider + Supabase sub from the JWT.
    ///   2. Calls IdentityLinkingService to resolve (or create/link) the local ApplicationUser.
    ///   3. Stores the resolved local user ID in HttpContext.Items["EffectiveUserId"].
    ///
    /// Controllers must call HttpContext.GetEffectiveUserId() instead of reading the JWT sub directly.
    /// This is the indirection that makes multi-provider identity linking transparent to the rest of the app.
    /// </summary>
    public class EnsureLocalUserMiddleware
    {
        private readonly RequestDelegate _next;

        public EnsureLocalUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IIdentityLinkingService identityLinkingService,
            IMemoryCache cache,
            ILogger<EnsureLocalUserMiddleware> logger)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var providerUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? context.User.FindFirstValue("sub");
                var email = context.User.FindFirstValue(ClaimTypes.Email)
                         ?? context.User.FindFirstValue("email");
                var provider = ExtractProvider(context);

                if (!string.IsNullOrEmpty(providerUserId))
                {
                    // Fast path: identity already resolved this session — skip DB hit
                    var cacheKey = $"effective_user:{provider}:{providerUserId}";
                    if (cache.TryGetValue(cacheKey, out string? cachedUserId) && cachedUserId != null)
                    {
                        context.Items["EffectiveUserId"] = cachedUserId;
                    }
                    else
                    {
                        try
                        {
                            var ctx = new AuthCallbackContext(
                                Provider: provider,
                                ProviderUserId: providerUserId,
                                Email: email,
                                EmailVerified: true, // Supabase only issues JWTs for verified accounts
                                DisplayName: ExtractDisplayName(context)
                            );

                            var result = await identityLinkingService.ResolveUserAsync(ctx);
                            context.Items["EffectiveUserId"] = result.User.Id;
                            cache.Set(cacheKey, result.User.Id, TimeSpan.FromMinutes(30));

                            if (result.IsNewUser)
                                logger.LogInformation("New user created: {UserId} via {Provider}", result.User.Id, provider);
                            else if (result.IsNewIdentityLinked)
                                logger.LogInformation("New {Provider} identity linked to existing user {UserId}", provider, result.User.Id);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "IdentityLinkingService failed for {Provider}/{ProviderUserId}", provider, providerUserId);
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }

        /// <summary>Extracts the auth provider from the Supabase JWT app_metadata claim.</summary>
        private static string ExtractProvider(HttpContext context)
        {
            var appMetadataJson = context.User?.FindFirstValue("app_metadata");
            if (!string.IsNullOrEmpty(appMetadataJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(appMetadataJson);
                    if (doc.RootElement.TryGetProperty("provider", out var providerEl))
                        return providerEl.GetString() ?? "email";
                }
                catch { /* malformed JSON — fall through to default */ }
            }
            return "email";
        }

        private static string? ExtractDisplayName(HttpContext context)
        {
            // Supabase puts display name in user_metadata.full_name for OAuth providers
            var userMetadataJson = context.User?.FindFirstValue("user_metadata");
            if (!string.IsNullOrEmpty(userMetadataJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(userMetadataJson);
                    if (doc.RootElement.TryGetProperty("full_name", out var nameEl))
                        return nameEl.GetString();
                    if (doc.RootElement.TryGetProperty("name", out var n))
                        return n.GetString();
                }
                catch { }
            }
            return null;
        }
    }

    public static class EnsureLocalUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnsureLocalUser(this IApplicationBuilder builder)
            => builder.UseMiddleware<EnsureLocalUserMiddleware>();

        /// <summary>
        /// Returns the local ApplicationUser ID for this request.
        /// Uses the identity-linked ID from middleware, falling back to the raw JWT sub.
        /// Always prefer this over User.FindFirstValue(ClaimTypes.NameIdentifier) in controllers.
        /// </summary>
        public static string? GetEffectiveUserId(this HttpContext context)
        {
            if (context.Items.TryGetValue("EffectiveUserId", out var id) && id is string userId)
                return userId;
            // Fallback: raw JWT sub (works for new users whose Id == ProviderUserId)
            return context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User?.FindFirstValue("sub");
        }
    }
}
