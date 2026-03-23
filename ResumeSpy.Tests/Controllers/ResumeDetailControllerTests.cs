using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Controllers;
using Xunit;

namespace ResumeSpy.Tests.Controllers;

public class ResumeDetailControllerTests
{
    private readonly Mock<ILogger<ResumeDetailController>> _logger = new();
    private readonly Mock<IResumeDetailService> _resumeDetailService = new();
    private readonly Mock<IMemoryCache> _memoryCache = new();
    private readonly Mock<ITranslationService> _translationService = new();
    private readonly Mock<IResumeManagementService> _resumeManagementService = new();
    private readonly Mock<IResumeService> _resumeService = new();
    private readonly Mock<IPdfExportService> _pdfExportService = new();
    private readonly Mock<IResumeTailoringService> _tailoringService = new();

    private ResumeDetailController CreateController(string? userId = null, Guid? anonymousUserId = null)
    {
        var controller = new ResumeDetailController(
            _logger.Object,
            _resumeDetailService.Object,
            _memoryCache.Object,
            _translationService.Object,
            _resumeManagementService.Object,
            _resumeService.Object,
            _pdfExportService.Object,
            _tailoringService.Object);

        var context = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            "TestAuth"));
        }
        else
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        if (anonymousUserId.HasValue)
        {
            context.Items["AnonymousUserId"] = anonymousUserId.Value;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    [Fact]
    public async Task GetResumeDetailModelsAsync_ReturnsDefault_WhenNoDetailsExist()
    {
        // Purpose: verify API creates a default placeholder detail for empty result sets.
        var controller = CreateController();
        _resumeDetailService.Setup(s => s.GetResumeDetailsByResumeId("resume-1"))
            .ReturnsAsync(Array.Empty<ResumeDetailViewModel>());

        var result = await controller.GetResumeDetailModelsAsync("resume-1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<List<ResumeDetailViewModel>>(ok.Value);
        Assert.Single(payload);
        Assert.True(payload[0].IsDefault);
        Assert.Equal("resume-1", payload[0].ResumeId);
    }

    [Fact]
    public async Task CreateResumeDetailModelAsync_ReturnsUnauthorized_WhenServiceThrowsUnauthorized()
    {
        // Purpose: verify UnauthorizedException is mapped to HTTP 401.
        var controller = CreateController();
        _resumeManagementService
            .Setup(s => s.CreateResumeDetailAsync(It.IsAny<ResumeDetailViewModel>(), null, null))
            .ThrowsAsync(new UnauthorizedException("anonymous identity missing"));

        var result = await controller.CreateResumeDetailModelAsync(new ResumeDetailViewModel
        {
            Id = "d1",
            ResumeId = "undefined",
            Content = "content"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task CreateResumeDetailModelAsync_Returns403_WhenQuotaExceeded()
    {
        // Purpose: verify QuotaExceededException is mapped to HTTP 403.
        var controller = CreateController();
        _resumeManagementService
            .Setup(s => s.CreateResumeDetailAsync(It.IsAny<ResumeDetailViewModel>(), null, null))
            .ThrowsAsync(new QuotaExceededException("quota exceeded"));

        var result = await controller.CreateResumeDetailModelAsync(new ResumeDetailViewModel
        {
            Id = "d1",
            ResumeId = "undefined",
            Content = "content"
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateResumeDetailModelFromExisting_ReturnsForbid_WhenUserIsNotAuthorized()
    {
        // Purpose: verify copy endpoint enforces ownership before translation call.
        var controller = CreateController(userId: "user-1");

        _resumeDetailService.Setup(s => s.GetResumeDetail("detail-1")).ReturnsAsync(new ResumeDetailViewModel
        {
            Id = "detail-1",
            ResumeId = "resume-1",
            Content = "hello",
            Language = "en"
        });
        _resumeService.Setup(s => s.GetResume("resume-1"))
            .ReturnsAsync(new ResumeViewModel { Id = "resume-1", UserId = "user-2" });

        var result = await controller.CreateResumeDetailModelFromExisting(new CopyRequest
        {
            ExistingResumeDetailId = "detail-1",
            Language = "es"
        });

        Assert.IsType<ForbidResult>(result.Result);
        _translationService.Verify(
            s => s.TranslateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncResumeDetailTranslationsAsync_Returns500_OnUnhandledException()
    {
        // Purpose: verify unhandled exceptions produce stable HTTP 500 response.
        var controller = CreateController();
        _resumeDetailService.Setup(s => s.GetResumeDetail("detail-1"))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.SyncResumeDetailTranslationsAsync("detail-1");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }
}
