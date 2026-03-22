using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Controllers;
using Xunit;

namespace ResumeSpy.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IResumeManagementService> _resumeManagementService = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();

    public AuthControllerTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private AuthController CreateController(IEnumerable<Claim>? claims = null, string? anonymousIdHeader = null)
    {
        var controller = new AuthController(_userManager.Object, _resumeManagementService.Object, _logger.Object);

        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(claims ?? Array.Empty<Claim>(), "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        if (!string.IsNullOrWhiteSpace(anonymousIdHeader))
        {
            context.Request.Headers["X-Anonymous-Id"] = anonymousIdHeader;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    [Fact]
    public async Task SyncSession_ReturnsUnauthorized_WhenClaimsMissing()
    {
        // Purpose: verify missing user/email claims return HTTP 401 with error payload.
        var controller = CreateController(claims: Array.Empty<Claim>());

        var result = await controller.SyncSession();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var payload = Assert.IsType<AuthSyncResponse>(unauthorized.Value);
        Assert.False(payload.Succeeded);
    }

    [Fact]
    public async Task SyncSession_ReturnsBadRequest_WhenUserCreationFails()
    {
        // Purpose: verify identity creation failures are mapped to HTTP 400.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "supabase-user-1"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };

        var controller = CreateController(claims);
        _userManager.Setup(m => m.FindByIdAsync("supabase-user-1")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User creation failed" }));

        var result = await controller.SyncSession();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<AuthSyncResponse>(badRequest.Value);
        Assert.False(payload.Succeeded);
        Assert.Contains("User creation failed", payload.Errors);
    }

    [Fact]
    public async Task SyncSession_ReturnsOk_AndConvertsAnonymousResumes_WhenHeaderValid()
    {
        // Purpose: verify session sync succeeds and anonymous resumes convert when header carries valid GUID.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "supabase-user-1"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };

        var anonymousId = Guid.NewGuid();
        var existingUser = new ApplicationUser { Id = "supabase-user-1", Email = "user@example.com", DisplayName = "user" };

        var controller = CreateController(claims, anonymousId.ToString());
        _userManager.Setup(m => m.FindByIdAsync("supabase-user-1")).ReturnsAsync(existingUser);
        _resumeManagementService.Setup(s => s.ConvertAnonymousToUserAsync(anonymousId, "supabase-user-1")).ReturnsAsync(2);

        var result = await controller.SyncSession();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthSyncResponse>(ok.Value);
        Assert.True(payload.Succeeded);
        Assert.Equal(2, payload.ConvertedResumeCount);
    }

    [Fact]
    public void Logout_ReturnsNoContent()
    {
        // Purpose: verify stateless logout contract returns HTTP 204.
        var controller = CreateController();

        var result = controller.Logout();

        Assert.IsType<NoContentResult>(result);
    }
}
