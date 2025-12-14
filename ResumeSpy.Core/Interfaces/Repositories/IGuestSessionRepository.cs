using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    public interface IGuestSessionRepository : IBaseRepository<GuestSession>
    {
        /// <summary>
        /// Gets all expired guest sessions
        /// </summary>
        Task<IEnumerable<GuestSession>> GetExpiredSessionsAsync();
    }
}
