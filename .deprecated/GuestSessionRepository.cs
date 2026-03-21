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
        public GuestSessionRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<GuestSession>> GetExpiredSessionsAsync()
        {
            return await _dbContext.GuestSessions
                .AsNoTracking()
                .Where(x => x.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<GuestSession?> GetActiveSessionByFingerprintAsync(string ipAddress, string? userAgent)
        {
            var query = _dbContext.GuestSessions
                .AsNoTracking()
                .Where(x => x.ExpiresAt > DateTime.UtcNow && !x.IsConverted && x.IpAddress == ipAddress);

            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                query = query.Where(x => x.UserAgent == userAgent);
            }

            return await query
                .OrderByDescending(x => x.EntryDate)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetSessionCountByIpSinceAsync(string ipAddress, DateTime since)
        {
            return await _dbContext.GuestSessions
                .AsNoTracking()
                .Where(x => x.IpAddress == ipAddress && x.EntryDate >= since)
                .CountAsync();
        }

        public async Task<IEnumerable<GuestSession>> GetSessionsByIpSinceAsync(string ipAddress, DateTime since)
        {
            return await _dbContext.GuestSessions
                .AsNoTracking()
                .Where(x => x.IpAddress == ipAddress && x.EntryDate >= since)
                .OrderByDescending(x => x.EntryDate)
                .ToListAsync();
        }

        public async Task<GuestSession?> GetByIdForUpdateAsync(Guid sessionId)
        {
            return await _dbContext.GuestSessions
                .FromSqlInterpolated($"SELECT * FROM \"GuestSessions\" WHERE \"Id\" = {sessionId} FOR UPDATE")
                .FirstOrDefaultAsync();
        }
    }
}
