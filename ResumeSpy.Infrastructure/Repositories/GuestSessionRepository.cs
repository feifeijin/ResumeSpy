using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class GuestSessionRepository : BaseRepository<GuestSession>, IGuestSessionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public GuestSessionRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<GuestSession>> GetExpiredSessionsAsync()
        {
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            return await _dbContext.GuestSessions
                .AsNoTracking()
                .Where(x => x.ExpiresAt < now)
                .ToListAsync();
        }

        public async Task<GuestSession?> GetActiveSessionByFingerprintAsync(string ipAddress, string? userAgent)
        {
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var query = _dbContext.GuestSessions
                .AsNoTracking()
                .Where(x => x.ExpiresAt > now && !x.IsConverted && x.IpAddress == ipAddress);

            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                query = query.Where(x => x.UserAgent == userAgent);
            }

            return await query
                .OrderByDescending(x => x.EntryDate)
                .FirstOrDefaultAsync();
        }
    }
}
