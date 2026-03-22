using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Controllers;
using Xunit;

namespace ResumeSpy.Tests.Controllers;

public class ResumeControllerTests
{
    private readonly Mock<ILogger<ResumeController>> _logger = new();
    private readonly Mock<IResumeService> _resumeService = new();
    private readonly Mock<IResumeManagementService> _resumeManagementService = new();
    private readonly Mock<IAnonymousUserService> _anonymousUserService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMemoryCache> _memoryCache = new();

    private ResumeController CreateController(string? userId = null, Guid? anonymousUserId = null)
    {
        var controller = new ResumeController(
            _logger.Object,
            _resumeService.Object,
            _resumeManagementService.Object,
            _anonymousUserService.Object,
            _unitOfWork.Object,
            _memoryCache.Object);

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
    public async Task GetResumes_ReturnsOrderedListDescendingByEntryDate()
    {
        // Purpose: verify the controller sorts resume response by EntryDate descending.
        var userId = "user-1";
        var controller = CreateController(userId: userId);
        var source = new List<ResumeViewModel>
        {
            new() { Id = "1", EntryDate = "2026-01-01" },
            new() { Id = "2", EntryDate = "2026-03-01" },
            new() { Id = "3", EntryDate = "2026-02-01" }
        };

        _resumeService.Setup(s => s.GetResumes(userId, null)).ReturnsAsync(source);

        var result = await controller.GetResumes();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<ResumeViewModel>>(ok.Value);
        Assert.Equal(new[] { "2", "3", "1" }, payload.Select(x => x.Id));
    }

    [Fact]
    public async Task GetResume_ReturnsForbid_WhenUserIsNotOwner()
    {
        // Purpose: verify ownership authorization for single-resume retrieval.
        var controller = CreateController(userId: "user-1");
        _resumeService.Setup(s => s.GetResume("resume-1"))
            .ReturnsAsync(new ResumeViewModel { Id = "resume-1", UserId = "user-2" });

        var result = await controller.GetResume("resume-1");

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateResume_ReturnsCreatedAtAction_ForAuthenticatedUser()
    {
        // Purpose: verify authenticated creation path sets non-guest fields and returns HTTP 201.
        var controller = CreateController(userId: "user-1");
        _resumeService.Setup(s => s.Create(It.IsAny<ResumeViewModel>()))
            .ReturnsAsync((ResumeViewModel model) => model);

        var request = new ResumeViewModel { Id = "resume-1", Title = "My Resume" };

        var result = await controller.CreateResume(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var payload = Assert.IsType<ResumeViewModel>(created.Value);
        Assert.False(payload.IsGuest);
        Assert.Equal("user-1", payload.UserId);
        Assert.Null(payload.AnonymousUserId);
        Assert.Null(payload.ExpiresAt);
    }

    [Fact]
    public async Task CreateResume_Returns403_WhenAnonymousLimitReached()
    {
        // Purpose: verify quota enforcement returns HTTP 403 for anonymous users at limit.
        var anonymousUserId = Guid.NewGuid();
        var controller = CreateController(anonymousUserId: anonymousUserId);

        _anonymousUserService.Setup(s => s.HasReachedResumeLimitAsync(anonymousUserId)).ReturnsAsync(true);

        var request = new ResumeViewModel { Id = "resume-1", Title = "Guest Resume" };
        var result = await controller.CreateResume(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteResume_ReturnsNotFound_WhenServiceThrowsNotFound()
    {
        // Purpose: verify domain not-found exception maps to API 404 response.
        var controller = CreateController(userId: "user-1");

        _resumeManagementService
            .Setup(s => s.DeleteResumeAtomicAsync("resume-404", "user-1", null))
            .ThrowsAsync(new NotFoundException("missing"));

        var result = await controller.DeleteResume("resume-404");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task DeleteResume_ReturnsNoContent_OnSuccess()
    {
        // Purpose: verify successful deletion returns HTTP 204 and no body.
        var controller = CreateController(userId: "user-1");

        _resumeManagementService
            .Setup(s => s.DeleteResumeAtomicAsync("resume-1", "user-1", null))
            .Returns(Task.CompletedTask);

        var result = await controller.DeleteResume("resume-1");

        Assert.IsType<NoContentResult>(result);
    }
}
