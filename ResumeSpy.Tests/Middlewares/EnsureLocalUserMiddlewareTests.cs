using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.Services;
using ResumeSpy.UI.Middlewares;
using Xunit;

namespace ResumeSpy.Tests.Middlewares;

public class EnsureLocalUserMiddlewareTests
{
    private readonly Mock<IIdentityLinkingService> _identityService = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<ILogger<EnsureLocalUserMiddleware>> _logger = new();

    private HttpContext BuildContext(bool isAuthenticated, string? sub = "user-123", string? email = "user@example.com")
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (isAuthenticated)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, sub!),
                new("email", email ?? string.Empty),
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_PassesThrough_WhenUserIsUnauthenticated()
    {
        var nextCalled = false;
        var middleware = new EnsureLocalUserMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = BuildContext(isAuthenticated: false);

        await middleware.InvokeAsync(context, _identityService.Object, _cache, _logger.Object);

        Assert.True(nextCalled);
        _identityService.Verify(s => s.ResolveUserAsync(It.IsAny<AuthCallbackContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_SetsEffectiveUserId_WhenIdentityResolved()
    {
        var user = new ApplicationUser { Id = "local-user-1", Email = "user@example.com" };
        _identityService
            .Setup(s => s.ResolveUserAsync(It.IsAny<AuthCallbackContext>()))
            .ReturnsAsync(new IdentityLinkingResult(user, IsNewUser: false, IsNewIdentityLinked: false));

        var middleware = new EnsureLocalUserMiddleware(_ => Task.CompletedTask);
        var context = BuildContext(isAuthenticated: true, sub: "supabase-uuid-1");

        await middleware.InvokeAsync(context, _identityService.Object, _cache, _logger.Object);

        Assert.Equal("local-user-1", context.Items["EffectiveUserId"]);
    }

    [Fact]
    public async Task InvokeAsync_UsesCache_OnSecondRequest()
    {
        var user = new ApplicationUser { Id = "local-user-1", Email = "user@example.com" };
        _identityService
            .Setup(s => s.ResolveUserAsync(It.IsAny<AuthCallbackContext>()))
            .ReturnsAsync(new IdentityLinkingResult(user, IsNewUser: false, IsNewIdentityLinked: false));

        var middleware = new EnsureLocalUserMiddleware(_ => Task.CompletedTask);

        var ctx1 = BuildContext(isAuthenticated: true, sub: "supabase-uuid-1");
        await middleware.InvokeAsync(ctx1, _identityService.Object, _cache, _logger.Object);

        var ctx2 = BuildContext(isAuthenticated: true, sub: "supabase-uuid-1");
        await middleware.InvokeAsync(ctx2, _identityService.Object, _cache, _logger.Object);

        // Service called only once; second request served from cache
        _identityService.Verify(s => s.ResolveUserAsync(It.IsAny<AuthCallbackContext>()), Times.Once);
        Assert.Equal("local-user-1", ctx2.Items["EffectiveUserId"]);
    }

    [Fact]
    public async Task InvokeAsync_Returns500WithJsonBody_WhenIdentityResolutionFails()
    {
        _identityService
            .Setup(s => s.ResolveUserAsync(It.IsAny<AuthCallbackContext>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var nextCalled = false;
        var middleware = new EnsureLocalUserMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = BuildContext(isAuthenticated: true);

        await middleware.InvokeAsync(context, _identityService.Object, _cache, _logger.Object);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(body.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.NotEmpty(body.RootElement.GetProperty("errors").EnumerateArray());
    }
}
