using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeSpy.UI.Filters;
using ResumeSpy.UI.Services;
using Xunit;

namespace ResumeSpy.Tests.Controllers;

public class AiAccessFilterTests
{
    private readonly Mock<IAiQuotaService> _quotaService = new();
    private readonly Mock<ILogger<AiAccessFilter>> _logger = new();

    private AiAccessFilter CreateFilter() => new(_quotaService.Object, _logger.Object);

    private static ActionExecutingContext CreateContext(string? userId = null, Guid? anonymousUserId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            httpContext.Items["EffectiveUserId"] = userId;
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                "TestAuth"));
        }
        if (anonymousUserId.HasValue)
        {
            httpContext.Items["AnonymousUserId"] = anonymousUserId.Value;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    [Fact]
    public async Task Returns401_WhenNoIdentityPresent()
    {
        // Purpose: closes the unauthenticated DoS hole — anonymous-anonymous requests must be rejected.
        var filter = CreateFilter();
        var ctx = CreateContext();
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.False(nextCalled);
        Assert.IsType<UnauthorizedObjectResult>(ctx.Result);
    }

    [Fact]
    public async Task Returns429_WhenDailyQuotaExceeded()
    {
        // Purpose: verify the per-identity daily cap is surfaced as HTTP 429.
        var filter = CreateFilter();
        var ctx = CreateContext(userId: "user-1");
        _quotaService.Setup(q => q.TryConsume("user:user-1"))
            .Returns(new AiQuotaResult(false, 0, 30));

        await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(null!));

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, result.StatusCode);
    }

    [Fact]
    public async Task CallsNext_WhenAnonymousIdentityWithinQuota()
    {
        // Purpose: anonymous GUID is an acceptable identity for AI endpoints.
        var filter = CreateFilter();
        var anonId = Guid.NewGuid();
        var ctx = CreateContext(anonymousUserId: anonId);
        _quotaService.Setup(q => q.TryConsume($"anon:{anonId}"))
            .Returns(new AiQuotaResult(true, 29, 30));

        var nextCalled = false;
        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.True(nextCalled);
        Assert.Null(ctx.Result);
    }
}
