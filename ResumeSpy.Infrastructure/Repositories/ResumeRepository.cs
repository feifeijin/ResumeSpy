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
    }
}