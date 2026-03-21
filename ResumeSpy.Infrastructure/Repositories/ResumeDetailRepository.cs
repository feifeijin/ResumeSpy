using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class ResumeDetailRepository : BaseRepository<ResumeDetail>, IResumeDetailRepository
    {
        public ResumeDetailRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<ResumeDetail>> GetResumeDetailsByResumeIdAsync(string resumeId)
        {
            var data = await _dbContext.Set<ResumeDetail>().Where(rd => rd.ResumeId == resumeId).AsNoTracking().ToListAsync();
            if (data == null)
                throw new NotFoundException("No data found");
            return data;
        }
    }
}