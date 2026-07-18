using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using System;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    public interface IAnonymousUserRepository : IBaseRepository<AnonymousUser>
    {
        /// <summary>
        /// Gets an anonymous user by ID, returning null if not found.
        /// </summary>
        Task<AnonymousUser?> FindByIdAsync(Guid anonymousUserId);

        /// <summary>
        /// Gets an anonymous user by ID and acquires a row-level update lock for transactional consistency.
        /// </summary>
        Task<AnonymousUser?> GetByIdForUpdateAsync(Guid anonymousUserId);
    }
}
