using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    public interface IResumeRepository:IBaseRepository<Resume>
    {
         Task<List<Resume>> GetByGuestSessionIdAsync(Guid guestSessionId);
    }
}