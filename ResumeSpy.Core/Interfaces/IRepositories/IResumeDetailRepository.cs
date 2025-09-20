using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    public interface IResumeDetailRepository:IBaseRepository<ResumeDetail>
    {
        Task<IEnumerable<ResumeDetail>>  GetResumeDetailsByResumeIdAsync(string resumeId);
    }
}