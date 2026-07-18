using ResumeSpy.Core.Entities.General;
using ResumeSpy.Infrastructure.Repositories;
using Xunit;

namespace ResumeSpy.Tests.Repositories;

public class ResumeDetailRepositoryTests
{
    [Fact]
    public async Task GetResumeDetailsByResumeIdAsync_ReturnsMatchingDetails()
    {
        // Purpose: verify query returns all details associated with a specific resume id.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new ResumeDetailRepository(context);

        await repo.Create(new ResumeDetail { Id = "d1", ResumeId = "r1", Content = "A" });
        await repo.Create(new ResumeDetail { Id = "d2", ResumeId = "r1", Content = "B" });
        await repo.Create(new ResumeDetail { Id = "d3", ResumeId = "r2", Content = "C" });
        await context.SaveChangesAsync();

        var result = (await repo.GetResumeDetailsByResumeIdAsync("r1")).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, x => x.ResumeId == "r2");
    }

    [Fact]
    public async Task Create_Update_Delete_Flow_PersistsInDatabase()
    {
        // Purpose: verify repository CRUD operations behave correctly with real in-memory persistence.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new ResumeDetailRepository(context);

        var detail = new ResumeDetail { Id = "d1", ResumeId = "r1", Content = "Old" };
        await repo.Create(detail);
        await context.SaveChangesAsync();

        detail.Content = "New";
        await repo.Update(detail);
        await context.SaveChangesAsync();

        var updated = await repo.GetById("d1");
        Assert.Equal("New", updated.Content);

        await repo.Delete(updated);
        await context.SaveChangesAsync();

        var all = await repo.GetAll();
        Assert.Empty(all);
    }
}
