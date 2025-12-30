using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    public interface IResumeRepository:IBaseRepository<Resume>
    {
         Task<List<Resume>> GetByGuestSessionIdAsync(Guid guestSessionId);
         Task<List<Resume>> GetByUserIdAsync(string userId);
         
         /// <summary>
         /// Counts the total number of guest resumes across multiple sessions
         /// Used for IP-based rate limiting
         /// </summary>
         Task<int> CountGuestResumesBySessionsAsync(List<Guid> sessionIds);
    }
}