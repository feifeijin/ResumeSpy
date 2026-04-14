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
            var data = await _dbContext.Set<ResumeDetail>()
                .Where(rd => rd.ResumeId == resumeId)
                .OrderBy(rd => rd.SortOrder)
                .ThenBy(rd => rd.EntryDate)
                .AsNoTracking()
                .ToListAsync();
            if (data == null)
                throw new NotFoundException("No data found");
            return data;
        }

        public async Task<int> GetNextSortOrderAsync(string resumeId)
        {
            var maxOrder = await _dbContext.Set<ResumeDetail>()
                .Where(rd => rd.ResumeId == resumeId)
                .MaxAsync(rd => (int?)rd.SortOrder) ?? 0;
            return maxOrder + 1;
        }

        public async Task ReorderAsync(string resumeId, IEnumerable<string> orderedIds)
        {
            var idList = orderedIds.ToList();
            var details = await _dbContext.Set<ResumeDetail>()
                .Where(rd => rd.ResumeId == resumeId)
                .ToListAsync();

            foreach (var detail in details)
            {
                var idx = idList.IndexOf(detail.Id);
                if (idx >= 0)
                    detail.SortOrder = idx + 1;
            }
        }
    }
}