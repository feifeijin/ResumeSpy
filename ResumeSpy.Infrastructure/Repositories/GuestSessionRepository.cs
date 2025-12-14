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
            return await _dbContext.GuestSessions
                .AsNoTracking()
                .Where(x => x.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();
        }
    }
}
