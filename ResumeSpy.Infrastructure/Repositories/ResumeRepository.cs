using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class ResumeRepository : BaseRepository<Resume>, IResumeRepository
    {
        public ResumeRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            
        }

        public async Task<List<Resume>> GetByAnonymousUserIdAsync(Guid anonymousUserId)
        {
            // AsNoTracking: these are read-only projections; no change-tracking overhead needed.
            // OrderByDescending: sort at the database level so the caller always receives
            // records in newest-first order without an extra in-memory pass.
            return await DbSet
                .AsNoTracking()
                .Where(r => r.AnonymousUserId == anonymousUserId)
                .OrderByDescending(r => r.EntryDate)
                .ToListAsync();
        }

        public async Task<List<Resume>> GetByUserIdAsync(string userId)
        {
            // AsNoTracking: read-only — skip the change-tracker overhead.
            // OrderByDescending: let the database (which already has an index on UserId)
            // perform the sort rather than doing it later in application memory.
            return await DbSet
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.EntryDate)
                .ToListAsync();
        }

        public async Task<int> CountGuestResumesBySessionsAsync(List<Guid> sessionIds)
        {
            if (sessionIds == null || sessionIds.Count == 0)
                return 0;

            return await DbSet
                .Where(r => r.AnonymousUserId.HasValue && sessionIds.Contains(r.AnonymousUserId.Value))
                .CountAsync();
        }
    }
}