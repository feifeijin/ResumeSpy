using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class ResumeDetailRepository :  BaseRepository<ResumeDetail>, IResumeDetailRepository
    {
        public ResumeDetailRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            
        }

        public Task<IEnumerable<ResumeDetail>> GetResumeDetailsByResumeId(string resumeId)
        {
            throw new NotImplementedException();
        }
    }
}