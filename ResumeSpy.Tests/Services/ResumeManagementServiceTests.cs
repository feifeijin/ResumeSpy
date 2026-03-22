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
    private readonly Mock<IImageGenerationService> _imageGenerationService = new();
    private readonly Mock<IAnonymousUserService> _anonymousUserService = new();

    private ResumeManagementService CreateService() => new(
        _resumeService.Object,
        _resumeDetailService.Object,
        _unitOfWork.Object,
        _translationService.Object,
        _imageGenerationService.Object,
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
