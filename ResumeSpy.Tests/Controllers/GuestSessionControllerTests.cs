using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Controllers;
using ResumeSpy.UI.Models;
using Xunit;

namespace ResumeSpy.Tests.Controllers;

public class GuestSessionControllerTests
{
    private readonly Mock<IGuestSessionService> _guestSessionService = new();
    private readonly Mock<ILogger<GuestSessionController>> _logger = new();

    private GuestSessionController CreateController(Guid? cookieSessionId = null)
    {
        var controller = new GuestSessionController(_guestSessionService.Object, _logger.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        if (cookieSessionId.HasValue)
        {
            context.Request.Headers.Cookie = $"X-Guest-Session-Id={cookieSessionId.Value}";
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    [Fact]
    public async Task CheckResumeQuota_ReturnsBadRequest_WhenNoSessionCookie()
    {
        // Purpose: verify quota endpoint rejects requests with no active guest session.
        var controller = CreateController();

        var result = await controller.CheckResumeQuota();

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CheckResumeQuota_ReturnsUnauthorized_WhenSessionInvalid()
    {
        // Purpose: verify invalid session is mapped to HTTP 401.
        var sessionId = Guid.NewGuid();
        var controller = CreateController(cookieSessionId: sessionId);
        _guestSessionService.Setup(s => s.ValidateGuestSessionAsync(sessionId, It.IsAny<string>())).ReturnsAsync(false);

        var result = await controller.CheckResumeQuota();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CheckResumeQuota_ReturnsOk_WhenSessionValid()
    {
        // Purpose: verify valid session returns quota payload and creation permission.
        var sessionId = Guid.NewGuid();
        var controller = CreateController(cookieSessionId: sessionId);

        _guestSessionService.Setup(s => s.ValidateGuestSessionAsync(sessionId, It.IsAny<string>())).ReturnsAsync(true);
        _guestSessionService.Setup(s => s.GetResumeCountAsync(sessionId)).ReturnsAsync(0);
        _guestSessionService.Setup(s => s.HasReachedResumeLimitAsync(sessionId)).ReturnsAsync(false);

        var result = await controller.CheckResumeQuota();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<CheckResumeQuotaResponse>(ok.Value);
        Assert.True(payload.CanCreateResume);
    }

    [Fact]
    public async Task GetSessionInfo_ReturnsNotFound_WhenSessionMissingAfterValidation()
    {
        // Purpose: verify response is 404 when validated session is no longer found.
        var sessionId = Guid.NewGuid();
        var controller = CreateController(cookieSessionId: sessionId);

        _guestSessionService.Setup(s => s.ValidateGuestSessionAsync(sessionId, It.IsAny<string>())).ReturnsAsync(true);
        _guestSessionService.Setup(s => s.GetGuestSessionAsync(sessionId)).ReturnsAsync((GuestSession?)null);

        var result = await controller.GetSessionInfo();

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
