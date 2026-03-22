using Microsoft.EntityFrameworkCore;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Tests.Repositories;

internal static class RepositoryTestDbFactory
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ResumeSpyTests_{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
