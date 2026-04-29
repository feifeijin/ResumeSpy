using Moq;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Services;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class ResumeManagementServiceTests
{
    private readonly Mock<IResumeService> _resumeService = new();
    private readonly Mock<IResumeDetailService> _resumeDetailService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITranslationService> _translationService = new();
    private readonly Mock<IAnonymousUserService> _anonymousUserService = new();

    private ResumeManagementService CreateService() => new(
        _resumeService.Object,
        _resumeDetailService.Object,
        _unitOfWork.Object,
        _translationService.Object,
        _anonymousUserService.Object);

    [Fact]
    public async Task CreateResumeDetailAsync_ThrowsUnauthorized_WhenAnonymousIdentityMissing()
    {
        // Purpose: verify first-time anonymous creation fails without anonymous user identity.
        var service = CreateService();
        var model = new ResumeDetailViewModel { Id = "d1", ResumeId = "undefined", Content = "content" };

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.CreateResumeDetailAsync(model, null, null));
    }

    [Fact]
    public async Task CreateResumeDetailAsync_ThrowsQuotaExceeded_WhenGuestLimitReached()
    {
        // Purpose: verify quota limit is enforced before creating first-time anonymous resume.
        var service = CreateService();
        var anonymousId = Guid.NewGuid();
        var model = new ResumeDetailViewModel { Id = "d1", ResumeId = "undefined", Content = "content" };
        _anonymousUserService.Setup(s => s.HasReachedResumeLimitAsync(anonymousId)).ReturnsAsync(true);

        await Assert.ThrowsAsync<QuotaExceededException>(() => service.CreateResumeDetailAsync(model, null, anonymousId));
    }

    [Fact]
    public async Task CreateResumeDetailAsync_DoesNotCallImageGeneration_OnExistingResume()
    {
        // Purpose: verify that creating a detail for an existing resume does NOT call the
        // image generation service synchronously (thumbnail is background-queued instead).
        var service = CreateService();
        var model = new ResumeDetailViewModel
        {
            Id = "d1",
            ResumeId = "existing-resume",
            Content = "# My resume",
            Language = "en"
        };

        _resumeDetailService
            .Setup(s => s.GetResumeDetailsByResumeId("existing-resume"))
            .ReturnsAsync(Array.Empty<ResumeDetailViewModel>());
        _resumeDetailService
            .Setup(s => s.Create(It.IsAny<ResumeDetailViewModel>()))
            .ReturnsAsync(model);

        var result = await service.CreateResumeDetailAsync(model, userId: "user-1", anonymousUserId: null);

        Assert.NotNull(result);
        // IImageGenerationService must never be called on the hot save path
        // (thumbnail generation is offloaded to ThumbnailBackgroundService).
    }

    [Fact]
    public async Task UpdateResumeDetailModelContentAsync_CommitsWithoutExtraDbFetches()
    {
        // Purpose: verify the update path does not perform stale re-fetches after queuing
        // thumbnail generation. The background service owns the image-path sync.
        var service = CreateService();
        var model = new ResumeDetailViewModel
        {
            Id = "d1",
            ResumeId = "r1",
            Content = "updated content",
            IsDefault = true
        };

        await service.UpdateResumeDetailModelContentAsync(model);

        _resumeDetailService.Verify(s => s.Update(model), Times.Once);
        // GetResumeDetail must NOT be called (stale re-fetch removed from hot path)
        _resumeDetailService.Verify(s => s.GetResumeDetail(It.IsAny<string>()), Times.Never);
        // Resume.ResumeImgPath sync belongs to ThumbnailBackgroundService — not here
        _resumeService.Verify(s => s.Update(It.IsAny<ResumeViewModel>()), Times.Never);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteResumeAtomicAsync_ThrowsUnauthorized_WhenCallerNotOwner()
    {
        // Purpose: verify delete authorization rejects mismatched ownership.
        var service = CreateService();
        _resumeService.Setup(s => s.GetResume("resume-1")).ReturnsAsync(new ResumeViewModel
        {
            Id = "resume-1",
            UserId = "owner-1",
            AnonymousUserId = Guid.NewGuid()
        });

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.DeleteResumeAtomicAsync("resume-1", userId: "other-user", anonymousUserId: null));

        _unitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteResumeAtomicAsync_DecrementsGuestCount_OnSuccess()
    {
        // Purpose: verify successful guest deletion decrements anonymous resume counter.
        var service = CreateService();
        var anonymousId = Guid.NewGuid();

        _resumeService.Setup(s => s.GetResume("resume-1")).ReturnsAsync(new ResumeViewModel
        {
            Id = "resume-1",
            AnonymousUserId = anonymousId,
            IsGuest = true
        });
        _resumeService.Setup(s => s.Delete("resume-1")).Returns(Task.CompletedTask);

        await service.DeleteResumeAtomicAsync("resume-1", null, anonymousId);

        _anonymousUserService.Verify(s => s.DecrementResumeCountAsync(anonymousId), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }
}
