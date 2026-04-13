using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    public interface IResumeVersionRepository
    {
        Task<List<ResumeVersion>> GetByResumeDetailIdAsync(string resumeDetailId);
        Task<ResumeVersion?> GetByIdAsync(Guid id);
        Task AddAsync(ResumeVersion version);
        Task DeleteAsync(Guid id);
    }
}
