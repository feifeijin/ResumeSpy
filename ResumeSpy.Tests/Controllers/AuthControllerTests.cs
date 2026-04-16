using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Interfaces.Services;
using ResumeSpy.UI.Controllers;
using Xunit;

namespace ResumeSpy.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IIdentityLinkingService> _identityLinkingService = new();
    private readonly Mock<IResumeManagementService> _resumeManagementService = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();

    private AuthController CreateController(IEnumerable<Claim>? claims = null, string? anonymousIdHeader = null)
    {
        var controller = new AuthController(
            _identityLinkingService.Object,
            _resumeManagementService.Object,
            _logger.Object);

        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(claims ?? Array.Empty<Claim>(), "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        if (!string.IsNullOrWhiteSpace(anonymousIdHeader))
            context.Request.Headers["X-Anonymous-Id"] = anonymousIdHeader;

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    [Fact]
    public async Task SyncSession_ReturnsUnauthorized_WhenClaimsMissing()
    {
        // Purpose: missing NameIdentifier/sub claim → HTTP 401
        var controller = CreateController(claims: Array.Empty<Claim>());

        var result = await controller.SyncSession();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var payload = Assert.IsType<AuthSyncResponse>(unauthorized.Value);
        Assert.False(payload.Succeeded);
    }

    [Fact]
    public async Task SyncSession_Returns500_WhenIdentityResolutionFails()
    {
        // Purpose: IdentityLinkingService failure → HTTP 500
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "supabase-user-1"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };

        _identityLinkingService
            .Setup(s => s.ResolveUserAsync(It.IsAny<AuthCallbackContext>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var controller = CreateController(claims);
        var result = await controller.SyncSession();

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public async Task SyncSession_ReturnsOk_AndConvertsGuestResumes()
    {
        // Purpose: successful sync converts guest resumes and returns user info
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "supabase-user-1"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };

        var user = new ApplicationUser { Id = "supabase-user-1", Email = "user@example.com", DisplayName = "User" };
        var anonymousId = Guid.NewGuid();

        _identityLinkingService
            .Setup(s => s.ResolveUserAsync(It.IsAny<AuthCallbackContext>()))
            .ReturnsAsync(new IdentityLinkingResult(user, IsNewUser: false, IsNewIdentityLinked: false));

        _resumeManagementService
            .Setup(s => s.ConvertAnonymousToUserAsync(anonymousId, "supabase-user-1"))
            .ReturnsAsync(2);

        var controller = CreateController(claims, anonymousId.ToString());
        var result = await controller.SyncSession();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthSyncResponse>(ok.Value);
        Assert.True(payload.Succeeded);
        Assert.Equal(2, payload.ConvertedResumeCount);
        Assert.Equal("supabase-user-1", payload.UserId);
    }

    [Fact]
    public void Logout_ReturnsNoContent()
    {
        var controller = CreateController();
        Assert.IsType<NoContentResult>(controller.Logout());
    }
}
