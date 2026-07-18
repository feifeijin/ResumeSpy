using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class ResumeVersionRepository : IResumeVersionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ResumeVersionRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ResumeVersion>> GetByResumeDetailIdAsync(string resumeDetailId)
        {
            return await _dbContext.Set<ResumeVersion>()
                .Where(v => v.ResumeDetailId == resumeDetailId)
                .OrderByDescending(v => v.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ResumeVersion?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Set<ResumeVersion>()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task AddAsync(ResumeVersion version)
        {
            await _dbContext.Set<ResumeVersion>().AddAsync(version);
        }

        public async Task DeleteAsync(Guid id)
        {
            var version = await _dbContext.Set<ResumeVersion>().FindAsync(id);
            if (version != null)
                _dbContext.Set<ResumeVersion>().Remove(version);
        }
    }
}
