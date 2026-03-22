using Moq;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IMapper;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Services;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class ResumeServiceTests
{
    private readonly Mock<IBaseMapper<Resume, ResumeViewModel>> _resumeViewModelMapper = new();
    private readonly Mock<IBaseMapper<ResumeViewModel, Resume>> _resumeMapper = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ResumeService CreateService() => new(
        _resumeViewModelMapper.Object,
        _resumeMapper.Object,
        _resumeRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Create_SetsTimestamps_AndPersistsResume()
    {
        // Purpose: verify service assigns UTC timestamps and persists a created resume.
        var service = CreateService();
        var model = new ResumeViewModel { Id = "resume-1", Title = "Title" };

        _resumeMapper.Setup(m => m.MapModel(model)).Returns(new Resume { Id = "resume-1", Title = "Title" });
        _resumeRepository.Setup(r => r.Create(It.IsAny<Resume>())).ReturnsAsync((Resume r) => r);
        _resumeViewModelMapper.Setup(m => m.MapModel(It.IsAny<Resume>()))
            .Returns((Resume r) => new ResumeViewModel { Id = r.Id, Title = r.Title });

        var result = await service.Create(model);

        Assert.Equal("resume-1", result.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetResume_ThrowsNotFoundException_WhenMissing()
    {
        // Purpose: verify missing resume lookup throws domain not-found exception.
        var service = CreateService();
        _resumeRepository.Setup(r => r.GetById("missing")).Returns(Task.FromResult<Resume>(null!));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetResume("missing"));
    }

    [Fact]
    public async Task GetResumes_ReturnsEmpty_WhenNoIdentityProvided()
    {
        // Purpose: verify unauthenticated/no-anonymous request yields empty result.
        var service = CreateService();
        _resumeViewModelMapper.Setup(m => m.MapList(It.IsAny<IEnumerable<Resume>>())).Returns(new List<ResumeViewModel>());

        var result = await service.GetResumes();

        Assert.Empty(result);
        _resumeRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<string>()), Times.Never);
        _resumeRepository.Verify(r => r.GetByAnonymousUserIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Update_UpdatesTrackedFields_AndSaves()
    {
        // Purpose: verify update flow mutates expected fields and saves once.
        var service = CreateService();
        var existing = new Resume { Id = "resume-1", Title = "Old", ResumeDetailCount = 1, ResumeImgPath = "old.png" };
        _resumeRepository.Setup(r => r.GetById("resume-1")).ReturnsAsync(existing);

        await service.Update(new ResumeViewModel
        {
            Id = "resume-1",
            Title = "New",
            ResumeDetailCount = 2,
            ResumeImgPath = "new.png"
        });

        Assert.Equal("New", existing.Title);
        Assert.Equal(2, existing.ResumeDetailCount);
        Assert.Equal("new.png", existing.ResumeImgPath);
        _resumeRepository.Verify(r => r.Update(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
