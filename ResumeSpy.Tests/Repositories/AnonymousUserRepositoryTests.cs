using ResumeSpy.Core.Entities.General;
using ResumeSpy.Infrastructure.Repositories;
using Xunit;

namespace ResumeSpy.Tests.Repositories;

public class AnonymousUserRepositoryTests
{
    [Fact]
    public async Task FindByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Purpose: verify repository returns null for missing anonymous user ids.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new AnonymousUserRepository(context);

        var result = await repo.FindByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Create_And_FindByIdAsync_PersistsEntity()
    {
        // Purpose: verify anonymous user record persists and can be fetched by id.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new AnonymousUserRepository(context);
        var id = Guid.NewGuid();

        await repo.Create(new AnonymousUser { Id = id, ResumeCount = 1 });
        await context.SaveChangesAsync();

        var result = await repo.FindByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(1, result!.ResumeCount);
    }

    [Fact]
    public async Task Update_And_Delete_Flow_IsPersisted()
    {
        // Purpose: verify update/delete behavior through real DbContext persistence.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new AnonymousUserRepository(context);
        var id = Guid.NewGuid();

        await repo.Create(new AnonymousUser { Id = id, ResumeCount = 0, IsConverted = false });
        await context.SaveChangesAsync();

        var entity = (await repo.FindByIdAsync(id))!;
        entity.ResumeCount = 2;
        entity.IsConverted = true;
        await repo.Update(entity);
        await context.SaveChangesAsync();

        var updated = (await repo.FindByIdAsync(id))!;
        Assert.Equal(2, updated.ResumeCount);
        Assert.True(updated.IsConverted);

        await repo.Delete(updated);
        await context.SaveChangesAsync();

        var missing = await repo.FindByIdAsync(id);
        Assert.Null(missing);
    }
}
