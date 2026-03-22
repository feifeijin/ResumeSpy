using Moq;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IMapper;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Services;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class ResumeDetailServiceTests
{
    private readonly Mock<IBaseMapper<ResumeDetail, ResumeDetailViewModel>> _viewModelMapper = new();
    private readonly Mock<IBaseMapper<ResumeDetailViewModel, ResumeDetail>> _entityMapper = new();
    private readonly Mock<IResumeDetailRepository> _resumeDetailRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IImageGenerationService> _imageGenerationService = new();

    private ResumeDetailService CreateService() => new(
        _viewModelMapper.Object,
        _entityMapper.Object,
        _resumeDetailRepository.Object,
        _unitOfWork.Object,
        _imageGenerationService.Object);

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenDetailMissing()
    {
        // Purpose: verify deleting unknown detail throws domain not-found exception.
        var service = CreateService();
        _resumeDetailRepository.Setup(r => r.GetById("missing")).Returns(Task.FromResult<ResumeDetail>(null!));

        await Assert.ThrowsAsync<NotFoundException>(() => service.Delete("missing"));
    }

    [Fact]
    public async Task Delete_DeletesThumbnail_AndEntity_ThenSaves()
    {
        // Purpose: verify delete flow calls thumbnail cleanup and persistence behavior.
        var service = CreateService();
        var entity = new ResumeDetail { Id = "d1", ResumeId = "r1", ResumeImgPath = "/thumb/d1.png" };
        _resumeDetailRepository.Setup(r => r.GetById("d1")).ReturnsAsync(entity);

        await service.Delete("d1");

        _imageGenerationService.Verify(s => s.DeleteThumbnailAsync("/thumb/d1.png"), Times.Once);
        _resumeDetailRepository.Verify(r => r.Delete(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Update_RegeneratesThumbnail_WhenContentChanges()
    {
        // Purpose: verify content update regenerates thumbnail and updates persisted fields.
        var service = CreateService();
        var entity = new ResumeDetail { Id = "d1", ResumeId = "r1", Content = "old", ResumeImgPath = "/thumb/old.png" };
        _resumeDetailRepository.Setup(r => r.GetById("d1")).ReturnsAsync(entity);
        _imageGenerationService.Setup(s => s.GenerateThumbnailAsync("new", "r1_d1")).ReturnsAsync("/thumb/new.png");

        await service.Update(new ResumeDetailViewModel
        {
            Id = "d1",
            ResumeId = "r1",
            Content = "new",
            Name = "Updated",
            Language = "en",
            IsDefault = true
        });

        Assert.Equal("/thumb/new.png", entity.ResumeImgPath);
        Assert.Equal("new", entity.Content);
        _resumeDetailRepository.Verify(r => r.Update(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
