using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.UI.Middlewares
{
    /// <summary>
    /// Ensures authenticated Supabase users have a corresponding local ASP.NET Identity record.
    /// Uses in-memory caching to avoid a database lookup on every request.
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
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            ILogger<EnsureLocalUserMiddleware> logger)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = context.User.FindFirstValue(ClaimTypes.Email)
                         ?? context.User.FindFirstValue("email");

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
                {
                    var cacheKey = $"local_user_exists:{userId}";
                    if (!cache.TryGetValue(cacheKey, out _))
                    {
                        var user = await userManager.FindByIdAsync(userId);
                        if (user == null)
                        {
                            user = new ApplicationUser
                            {
                                Id = userId,
                                UserName = email,
                                Email = email,
                                EmailConfirmed = true,
                                DisplayName = email.Split('@')[0]
                            };

                            var result = await userManager.CreateAsync(user);
                            if (result.Succeeded)
                            {
                                logger.LogInformation("Auto-created local user {UserId} for {Email}", userId, email);
                            }
                            else
                            {
                                logger.LogWarning("Failed to auto-create local user {UserId}: {Errors}",
                                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                            }
                        }

                        cache.Set(cacheKey, true, TimeSpan.FromMinutes(30));
                    }
                }
            }

            await _next(context);
        }
    }

    public static class EnsureLocalUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnsureLocalUser(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnsureLocalUserMiddleware>();
        }
    }
}
