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

    /// <summary>
    /// Creates a controller backed by a real <see cref="MemoryCache"/> instance so that
    /// caching behaviour can be verified end-to-end within a single test.
    /// </summary>
    private ResumeController CreateControllerWithRealCache(string? userId = null, Guid? anonymousUserId = null)
    {
        var realCache = new MemoryCache(new MemoryCacheOptions());
        var controller = new ResumeController(
            _logger.Object,
            _resumeService.Object,
            _resumeManagementService.Object,
            _anonymousUserService.Object,
            _unitOfWork.Object,
            realCache);

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
            context.Items["AnonymousUserId"] = anonymousUserId.Value;

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    [Fact]
    public async Task GetResumes_ReturnsResumeListInServiceOrder()
    {
        // Purpose: verify the controller returns the resume list exactly as the service
        // provides it. Ordering is now the repository's responsibility (pushed to the
        // SQL query), so the controller should pass the list through unchanged.
        // Uses a real MemoryCache to avoid mock-CreateEntry NullReferenceException.
        var userId = "user-1";
        var controller = CreateControllerWithRealCache(userId: userId);

        // Service (and therefore the underlying repository) already returns newest-first.
        var source = new List<ResumeViewModel>
        {
            new() { Id = "2", EntryDate = "2026-03-01" },
            new() { Id = "3", EntryDate = "2026-02-01" },
            new() { Id = "1", EntryDate = "2026-01-01" }
        };

        _resumeService.Setup(s => s.GetResumes(userId, null)).ReturnsAsync(source);

        var result = await controller.GetResumes();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<ResumeViewModel>>(ok.Value);
        Assert.Equal(new[] { "2", "3", "1" }, payload.Select(x => x.Id));
    }

    [Fact]
    public async Task GetResumes_UsesCachedResult_OnSecondCall()
    {
        // Purpose: verify that the in-memory cache prevents a second database round-trip
        // when the same user calls GET /api/resume twice in quick succession.
        var userId = "user-cache";
        var controller = CreateControllerWithRealCache(userId: userId);
        var source = new List<ResumeViewModel> { new() { Id = "r1", EntryDate = "2026-01-01" } };

        _resumeService.Setup(s => s.GetResumes(userId, null)).ReturnsAsync(source);

        // First call — cache miss, hits the service.
        await controller.GetResumes();
        // Second call — should be served from cache without hitting the service again.
        await controller.GetResumes();

        _resumeService.Verify(s => s.GetResumes(userId, null), Times.Once);
    }

    [Fact]
    public async Task GetResumes_HitsServiceAgain_AfterCacheInvalidatedByCreate()
    {
        // Purpose: verify that creating a resume removes the cached list so the next
        // GET sees the fresh data from the database.
        var userId = "user-invalidate";
        var controller = CreateControllerWithRealCache(userId: userId);
        var source = new List<ResumeViewModel> { new() { Id = "r1" } };

        _resumeService.Setup(s => s.GetResumes(userId, null)).ReturnsAsync(source);
        _resumeService.Setup(s => s.Create(It.IsAny<ResumeViewModel>()))
            .ReturnsAsync((ResumeViewModel m) => m);

        // First GET — populates cache.
        await controller.GetResumes();
        // Create — must invalidate cache.
        await controller.CreateResume(new ResumeViewModel { Id = "r2", Title = "New" });
        // Second GET — cache was busted, service must be called again.
        await controller.GetResumes();

        _resumeService.Verify(s => s.GetResumes(userId, null), Times.Exactly(2));
    }

    [Fact]
    public async Task GetResumes_HitsServiceAgain_AfterCacheInvalidatedByDelete()
    {
        // Purpose: verify that deleting a resume removes the cached list.
        var userId = "user-del";
        var controller = CreateControllerWithRealCache(userId: userId);
        var source = new List<ResumeViewModel> { new() { Id = "r1" } };

        _resumeService.Setup(s => s.GetResumes(userId, null)).ReturnsAsync(source);
        _resumeManagementService
            .Setup(s => s.DeleteResumeAtomicAsync("r1", userId, null))
            .Returns(Task.CompletedTask);

        await controller.GetResumes();
        await controller.DeleteResume("r1");
        await controller.GetResumes();

        _resumeService.Verify(s => s.GetResumes(userId, null), Times.Exactly(2));
    }

    [Fact]
    public void BuildResumeCacheKey_ReturnsUserKey_WhenUserIdPresent()
    {
        // Purpose: verify deterministic cache key generation for authenticated users.
        var key = ResumeController.BuildResumeCacheKey("user-abc", null);
        Assert.Equal("resumes:user:user-abc", key);
    }

    [Fact]
    public void BuildResumeCacheKey_ReturnsAnonKey_WhenOnlyAnonymousIdPresent()
    {
        // Purpose: verify deterministic cache key generation for anonymous users.
        var anonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var key = ResumeController.BuildResumeCacheKey(null, anonId);
        Assert.Equal($"resumes:anon:{anonId}", key);
    }

    [Fact]
    public void BuildResumeCacheKey_ReturnsNull_WhenNoIdentity()
    {
        // Purpose: verify null is returned when no identity is available so caching is skipped.
        var key = ResumeController.BuildResumeCacheKey(null, null);
        Assert.Null(key);
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
