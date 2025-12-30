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

        public async Task<List<Resume>> GetByGuestSessionIdAsync(Guid guestSessionId)
        {
            return await DbSet
                .Where(r => r.GuestSessionId == guestSessionId)
                .ToListAsync();
        }

        public async Task<List<Resume>> GetByUserIdAsync(string userId)
        {
            return await DbSet
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<int> CountGuestResumesBySessionsAsync(List<Guid> sessionIds)
        {
            if (sessionIds == null || !sessionIds.Any())
            {
                return 0;
            }

            return await DbSet
                .Where(r => r.GuestSessionId != null && sessionIds.Contains(r.GuestSessionId.Value))
                .CountAsync();
        }
    }
}