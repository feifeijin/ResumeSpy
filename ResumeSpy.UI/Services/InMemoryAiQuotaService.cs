using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ResumeSpy.Infrastructure.Configuration;

namespace ResumeSpy.UI.Services
{
    /// <summary>
    /// In-memory daily AI quota. Counters reset on UTC day rollover via cache
    /// expiry. Per-instance — adequate for single-node deployments; for
    /// horizontally-scaled deployments back this with Redis or the database.
    /// </summary>
    public class InMemoryAiQuotaService : IAiQuotaService
    {
        private readonly IMemoryCache _cache;
        private readonly AnonymousUserSettings _settings;
        private readonly object _lock = new();

        public InMemoryAiQuotaService(IMemoryCache cache, IOptions<AnonymousUserSettings> settings)
        {
            _cache = cache;
            _settings = settings.Value;
        }

        public AiQuotaResult TryConsume(string identityKey)
        {
            var max = _settings.MaxAiCallsPerDay;
            if (max <= 0)
                return new AiQuotaResult(true, int.MaxValue, max);

            var cacheKey = $"ai_quota:{identityKey}:{DateTime.UtcNow:yyyyMMdd}";

            // Lock to make the read-modify-write atomic. Per-key locking would be
            // lower-contention but a single lock is fine given the low call rate
            // of AI endpoints.
            lock (_lock)
            {
                _cache.TryGetValue<int>(cacheKey, out var count);
                if (count >= max)
                    return new AiQuotaResult(false, 0, max);

                count++;
                _cache.Set(cacheKey, count, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(36)
                });
                return new AiQuotaResult(true, max - count, max);
            }
        }
    }
}
