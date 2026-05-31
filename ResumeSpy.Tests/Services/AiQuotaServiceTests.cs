using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.UI.Services;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class AiQuotaServiceTests
{
    private static InMemoryAiQuotaService CreateService(int max)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var settings = Options.Create(new AnonymousUserSettings { MaxAiCallsPerDay = max });
        return new InMemoryAiQuotaService(cache, settings);
    }

    [Fact]
    public void TryConsume_AllowsCallsUntilLimit_ThenRejects()
    {
        // Purpose: verify the daily cap blocks the (max+1)-th call.
        var svc = CreateService(max: 3);

        var first = svc.TryConsume("user:abc");
        var second = svc.TryConsume("user:abc");
        var third = svc.TryConsume("user:abc");
        var fourth = svc.TryConsume("user:abc");

        Assert.True(first.Allowed);
        Assert.Equal(2, first.Remaining);
        Assert.True(second.Allowed);
        Assert.True(third.Allowed);
        Assert.False(fourth.Allowed);
        Assert.Equal(0, fourth.Remaining);
        Assert.Equal(3, fourth.Max);
    }

    [Fact]
    public void TryConsume_IsScopedPerIdentity()
    {
        // Purpose: ensure one identity hitting the cap doesn't lock out another.
        var svc = CreateService(max: 1);

        var userA = svc.TryConsume("user:a");
        var userB = svc.TryConsume("user:b");

        Assert.True(userA.Allowed);
        Assert.True(userB.Allowed);
    }

    [Fact]
    public void TryConsume_DisabledWhenMaxNonPositive()
    {
        // Purpose: MaxAiCallsPerDay <= 0 is the documented "disable" switch.
        var svc = CreateService(max: 0);

        for (int i = 0; i < 50; i++)
        {
            Assert.True(svc.TryConsume("user:x").Allowed);
        }
    }
}
