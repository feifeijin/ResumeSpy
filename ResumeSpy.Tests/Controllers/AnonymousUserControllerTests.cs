using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.UI.Controllers;
using ResumeSpy.UI.Models;
using Xunit;

namespace ResumeSpy.Tests.Controllers;

public class AnonymousUserControllerTests
{
    private readonly Mock<IAnonymousUserService> _anonymousUserService = new();
    private readonly Mock<ILogger<AnonymousUserController>> _logger = new();

    private AnonymousUserController CreateController(Guid? anonymousUserId = null)
    {
        var settings = Options.Create(new AnonymousUserSettings { MaxResumePerUser = 3 });
        var controller = new AnonymousUserController(_anonymousUserService.Object, _logger.Object, settings);

        var context = new DefaultHttpContext();
        if (anonymousUserId.HasValue)
        {
            context.Items["AnonymousUserId"] = anonymousUserId.Value;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    [Fact]
    public async Task CheckResumeQuota_ReturnsDefaults_WhenAnonymousIdMissing()
    {
        // Purpose: verify quota endpoint handles unknown anonymous identity gracefully.
        var controller = CreateController();

        var result = await controller.CheckResumeQuota();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<CheckResumeQuotaResponse>(ok.Value);
        Assert.Equal(0, payload.CurrentCount);
        Assert.Equal(3, payload.MaxAllowed);
        Assert.True(payload.CanCreateResume);
    }

    [Fact]
    public async Task CheckResumeQuota_Returns500_WhenServiceThrows()
    {
        // Purpose: verify defensive error handling produces HTTP 500.
        var anonymousId = Guid.NewGuid();
        var controller = CreateController(anonymousId);
        _anonymousUserService.Setup(s => s.GetResumeCountAsync(anonymousId)).ThrowsAsync(new Exception("db down"));

        var result = await controller.CheckResumeQuota();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetInfo_ReturnsNotFound_WhenAnonymousIdMissing()
    {
        // Purpose: verify info endpoint returns 404 if anonymous context is absent.
        var controller = CreateController();

        var result = await controller.GetInfo();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetInfo_ReturnsOk_WhenAnonymousUserExists()
    {
        // Purpose: verify info endpoint returns successful payload for existing anonymous user.
        var anonymousId = Guid.NewGuid();
        var controller = CreateController(anonymousId);
        _anonymousUserService.Setup(s => s.GetAsync(anonymousId)).ReturnsAsync(new AnonymousUser
        {
            Id = anonymousId,
            ResumeCount = 2,
            IsConverted = false
        });

        var result = await controller.GetInfo();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }
}
