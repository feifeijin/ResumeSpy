using ResumeSpy.Core.Entities.Business;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeVersionService
    {
        Task<List<ResumeVersionViewModel>> GetVersionsAsync(string resumeDetailId);
        Task<ResumeVersionViewModel> SaveVersionAsync(string resumeDetailId, string content, string? label = null);
        Task DeleteVersionAsync(Guid id);
        Task<string> GetVersionContentAsync(Guid id);
    }
}
