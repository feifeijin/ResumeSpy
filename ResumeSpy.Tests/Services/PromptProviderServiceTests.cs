using Moq;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Services;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class PromptProviderServiceTests
{
    private readonly Mock<IPromptRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PromptProviderService CreateService() =>
        new(_repo.Object, _unitOfWork.Object);

    [Fact]
    public async Task GetSystemMessageAsync_ReturnsFallback_WhenNoDbRecord()
    {
        _repo.Setup(r => r.GetByKeyAsync("chat")).ReturnsAsync((PromptTemplate?)null);
        var service = CreateService();

        var result = await service.GetSystemMessageAsync("chat", "default-system-prompt");

        Assert.Equal("default-system-prompt", result);
    }

    [Fact]
    public async Task GetSystemMessageAsync_ReturnsDbValue_WhenRecordExists()
    {
        _repo.Setup(r => r.GetByKeyAsync("chat")).ReturnsAsync(new PromptTemplate
        {
            Key = "chat",
            Category = "chat",
            SystemMessage = "custom-system-prompt",
            IsActive = true
        });
        var service = CreateService();

        var result = await service.GetSystemMessageAsync("chat", "default-system-prompt");

        Assert.Equal("custom-system-prompt", result);
    }

    [Fact]
    public async Task UpsertAsync_CreatesNew_WhenKeyDoesNotExist()
    {
        _repo.Setup(r => r.GetByKeyAsync("import")).ReturnsAsync((PromptTemplate?)null);
        _repo.Setup(r => r.Create(It.IsAny<PromptTemplate>())).ReturnsAsync((PromptTemplate t) => t);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = CreateService();
        var template = new PromptTemplate { Key = "import", Category = "import", SystemMessage = "new-prompt" };

        await service.UpsertAsync(template);

        _repo.Verify(r => r.Create(It.Is<PromptTemplate>(t => t.Key == "import" && t.EntryDate != null)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExisting_WhenKeyExists()
    {
        var existing = new PromptTemplate
        {
            Id = 1,
            Key = "tailoring",
            Category = "tailoring",
            SystemMessage = "old-prompt",
            Version = 1,
            IsActive = true
        };
        _repo.Setup(r => r.GetByKeyAsync("tailoring")).ReturnsAsync(existing);
        _repo.Setup(r => r.Update(It.IsAny<PromptTemplate>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = CreateService();
        var updated = new PromptTemplate { Key = "tailoring", Category = "tailoring", SystemMessage = "updated-prompt" };

        await service.UpsertAsync(updated);

        Assert.Equal("updated-prompt", existing.SystemMessage);
        Assert.Equal(2, existing.Version);
        _repo.Verify(r => r.Update(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTemplate_WhenExists()
    {
        var existing = new PromptTemplate { Key = "translation", Category = "translation", SystemMessage = "old", IsActive = true };
        _repo.Setup(r => r.GetByKeyAsync("translation")).ReturnsAsync(existing);
        _repo.Setup(r => r.Delete(It.IsAny<PromptTemplate>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = CreateService();

        await service.DeleteAsync("translation");

        _repo.Verify(r => r.Delete(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenKeyNotFound()
    {
        _repo.Setup(r => r.GetByKeyAsync("nonexistent")).ReturnsAsync((PromptTemplate?)null);

        var service = CreateService();

        await service.DeleteAsync("nonexistent");

        _repo.Verify(r => r.Delete(It.IsAny<PromptTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
