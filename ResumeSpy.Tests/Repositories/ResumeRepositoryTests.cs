using ResumeSpy.Core.Entities.General;
using ResumeSpy.Infrastructure.Repositories;
using Xunit;

namespace ResumeSpy.Tests.Repositories;

public class ResumeRepositoryTests
{
    [Fact]
    public async Task Create_And_GetById_PersistsResume()
    {
        // Purpose: verify repository persists and retrieves resume from in-memory database.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new ResumeRepository(context);
        var model = new Resume { Id = "r1", Title = "Resume 1", UserId = "u1" };

        await repo.Create(model);
        await context.SaveChangesAsync();

        var loaded = await repo.GetById("r1");
        Assert.Equal("Resume 1", loaded.Title);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyMatchingRows()
    {
        // Purpose: verify filtering by authenticated user id returns only owned resumes.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new ResumeRepository(context);

        await repo.Create(new Resume { Id = "r1", Title = "A", UserId = "u1" });
        await repo.Create(new Resume { Id = "r2", Title = "B", UserId = "u2" });
        await context.SaveChangesAsync();

        var result = await repo.GetByUserIdAsync("u1");

        Assert.Single(result);
        Assert.Equal("r1", result[0].Id);
    }

    [Fact]
    public async Task GetByAnonymousUserIdAsync_ReturnsOnlyMatchingRows()
    {
        // Purpose: verify filtering by anonymous user id returns expected guest resumes.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new ResumeRepository(context);
        var anonA = Guid.NewGuid();
        var anonB = Guid.NewGuid();

        await repo.Create(new Resume { Id = "r1", Title = "A", AnonymousUserId = anonA });
        await repo.Create(new Resume { Id = "r2", Title = "B", AnonymousUserId = anonB });
        await context.SaveChangesAsync();

        var result = await repo.GetByAnonymousUserIdAsync(anonA);

        Assert.Single(result);
        Assert.Equal("r1", result[0].Id);
    }

    [Fact]
    public async Task CountGuestResumesBySessionsAsync_ReturnsZero_WhenInputEmpty()
    {
        // Purpose: verify edge case of empty session list returns deterministic zero.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new ResumeRepository(context);

        var count = await repo.CountGuestResumesBySessionsAsync(new List<Guid>());

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Update_And_Delete_PersistChanges()
    {
        // Purpose: verify update and delete operations persist through DbContext save.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new ResumeRepository(context);
        var model = new Resume { Id = "r1", Title = "Old" };

        await repo.Create(model);
        await context.SaveChangesAsync();

        model.Title = "New";
        await repo.Update(model);
        await context.SaveChangesAsync();

        var updated = await repo.GetById("r1");
        Assert.Equal("New", updated.Title);

        await repo.Delete(updated);
        await context.SaveChangesAsync();

        var all = await repo.GetAll();
        Assert.Empty(all);
    }
}
