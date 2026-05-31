using Microsoft.AspNetCore.Authorization;
using ResumeSpy.UI.Middlewares;

namespace ResumeSpy.UI.Authorization
{
    /// <summary>
    /// Authorization requirement satisfied when the request carries either a
    /// validated JWT principal (authenticated user) or an anonymous-user GUID
    /// resolved by <see cref="AnonymousUserMiddleware"/>. Used as the global
    /// FallbackPolicy so endpoints are protected by default and only opt out
    /// with <c>[AllowAnonymous]</c>.
    /// </summary>
    public sealed class IdentifiedUserRequirement : IAuthorizationRequirement
    {
    }

    public sealed class IdentifiedUserAuthorizationHandler : AuthorizationHandler<IdentifiedUserRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IdentifiedUserRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // AuthorizationMiddleware passes the HttpContext as the resource.
            if (context.Resource is HttpContext httpContext &&
                httpContext.GetAnonymousUserId().HasValue)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
