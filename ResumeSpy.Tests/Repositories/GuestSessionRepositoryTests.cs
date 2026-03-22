using ResumeSpy.Core.Entities.General;
using ResumeSpy.Infrastructure.Repositories;
using Xunit;

namespace ResumeSpy.Tests.Repositories;

public class GuestSessionRepositoryTests
{
    [Fact]
    public async Task GetExpiredSessionsAsync_ReturnsOnlyExpiredSessions()
    {
        // Purpose: verify expiration filter returns sessions strictly older than current UTC time.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new GuestSessionRepository(context);

        await repo.Create(new GuestSession
        {
            Id = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
            EntryDate = DateTime.UtcNow.AddHours(-2)
        });
        await repo.Create(new GuestSession
        {
            Id = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            EntryDate = DateTime.UtcNow.AddHours(-1)
        });
        await context.SaveChangesAsync();

        var expired = (await repo.GetExpiredSessionsAsync()).ToList();

        Assert.Single(expired);
        Assert.True(expired[0].ExpiresAt < DateTime.UtcNow);
    }

    [Fact]
    public async Task GetActiveSessionByFingerprintAsync_ReturnsLatestMatchingSession()
    {
        // Purpose: verify fingerprint query returns latest active non-converted session by EntryDate.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new GuestSessionRepository(context);
        var now = DateTime.UtcNow;

        await repo.Create(new GuestSession
        {
            Id = Guid.NewGuid(),
            IpAddress = "10.0.0.1",
            UserAgent = "agent-a",
            ExpiresAt = now.AddHours(1),
            EntryDate = now.AddMinutes(-10),
            IsConverted = false
        });
        var latest = new GuestSession
        {
            Id = Guid.NewGuid(),
            IpAddress = "10.0.0.1",
            UserAgent = "agent-a",
            ExpiresAt = now.AddHours(2),
            EntryDate = now,
            IsConverted = false
        };
        await repo.Create(latest);
        await context.SaveChangesAsync();

        var result = await repo.GetActiveSessionByFingerprintAsync("10.0.0.1", "agent-a");

        Assert.NotNull(result);
        Assert.Equal(latest.Id, result!.Id);
    }

    [Fact]
    public async Task GetSessionCountByIpSinceAsync_ReturnsWindowCount()
    {
        // Purpose: verify rate-limit counting only includes sessions at/after given time window.
        await using var context = RepositoryTestDbFactory.CreateContext();
        var repo = new GuestSessionRepository(context);
        var since = DateTime.UtcNow.AddMinutes(-30);

        await repo.Create(new GuestSession
        {
            Id = Guid.NewGuid(),
            IpAddress = "10.0.0.2",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            EntryDate = DateTime.UtcNow.AddMinutes(-20)
        });
        await repo.Create(new GuestSession
        {
            Id = Guid.NewGuid(),
            IpAddress = "10.0.0.2",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            EntryDate = DateTime.UtcNow.AddMinutes(-40)
        });
        await context.SaveChangesAsync();

        var count = await repo.GetSessionCountByIpSinceAsync("10.0.0.2", since);

        Assert.Equal(1, count);
    }
}
