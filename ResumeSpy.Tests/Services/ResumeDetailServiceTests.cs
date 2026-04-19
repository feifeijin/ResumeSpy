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
    private readonly Mock<IThumbnailQueue> _thumbnailQueue = new();

    private ResumeDetailService CreateService() => new(
        _viewModelMapper.Object,
        _entityMapper.Object,
        _resumeDetailRepository.Object,
        _unitOfWork.Object,
        _imageGenerationService.Object,
        _thumbnailQueue.Object);

    [Fact]
    public async Task Create_EnqueuesThumbnail_WhenContentIsPresent()
    {
        // Purpose: verify that creating a resume detail with content queues thumbnail
        // generation in the background rather than leaving ResumeImgPath null forever.
        var service = CreateService();
        var vm = new ResumeDetailViewModel { Id = "d1", ResumeId = "r1", Content = "# My Resume" };
        var entity = new ResumeDetail { Id = "d1", ResumeId = "r1", Content = "# My Resume" };
        _entityMapper.Setup(m => m.MapModel(vm)).Returns(entity);
        _resumeDetailRepository.Setup(r => r.Create(entity)).ReturnsAsync(entity);
        _resumeDetailRepository.Setup(r => r.GetNextSortOrderAsync("r1")).ReturnsAsync(1);
        _viewModelMapper.Setup(m => m.MapModel(entity)).Returns(vm);

        await service.Create(vm);

        _thumbnailQueue.Verify(q => q.Enqueue(It.Is<ThumbnailTask>(t =>
            t.ResumeDetailId == "d1" &&
            t.ResumeId == "r1" &&
            t.Content == "# My Resume" &&
            t.OldImagePath == null)), Times.Once);
    }

    [Fact]
    public async Task Create_DoesNotEnqueueThumbnail_WhenContentIsEmpty()
    {
        // Purpose: verify no thumbnail task is queued when content is blank.
        var service = CreateService();
        var vm = new ResumeDetailViewModel { Id = "d2", ResumeId = "r1", Content = "" };
        var entity = new ResumeDetail { Id = "d2", ResumeId = "r1", Content = "" };
        _entityMapper.Setup(m => m.MapModel(vm)).Returns(entity);
        _resumeDetailRepository.Setup(r => r.Create(entity)).ReturnsAsync(entity);
        _resumeDetailRepository.Setup(r => r.GetNextSortOrderAsync("r1")).ReturnsAsync(1);
        _viewModelMapper.Setup(m => m.MapModel(entity)).Returns(vm);

        await service.Create(vm);

        _thumbnailQueue.Verify(q => q.Enqueue(It.IsAny<ThumbnailTask>()), Times.Never);
    }

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
    public async Task Update_SavesContentImmediately_AndEnqueuesThumbnailInBackground()
    {
        // Purpose: verify content update saves to DB right away and queues thumbnail
        // generation as a background task rather than blocking the save response.
        var service = CreateService();
        var entity = new ResumeDetail { Id = "d1", ResumeId = "r1", Content = "old", ResumeImgPath = "/thumb/old.png" };
        _resumeDetailRepository.Setup(r => r.GetById("d1")).ReturnsAsync(entity);

        await service.Update(new ResumeDetailViewModel
        {
            Id = "d1",
            ResumeId = "r1",
            Content = "new",
            Name = "Updated",
            Language = "en",
            IsDefault = true
        });

        // Content should be persisted synchronously
        Assert.Equal("new", entity.Content);
        _resumeDetailRepository.Verify(r => r.Update(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

        // Thumbnail should be queued, not generated inline
        _thumbnailQueue.Verify(q => q.Enqueue(It.Is<ThumbnailTask>(t =>
            t.ResumeDetailId == "d1" &&
            t.ResumeId == "r1" &&
            t.Content == "new" &&
            t.OldImagePath == "/thumb/old.png")), Times.Once);
        _imageGenerationService.Verify(s => s.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
