using ResumeSpy.Core.Entities.General;
using System;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IAnonymousUserService
    {
        /// <summary>
        /// Gets or creates an anonymous user record for the given ID.
        /// </summary>
        Task<AnonymousUser> GetOrCreateAsync(Guid anonymousUserId);

        /// <summary>
        /// Gets an anonymous user by ID, or null if not found.
        /// </summary>
        Task<AnonymousUser?> GetAsync(Guid anonymousUserId);

        /// <summary>
        /// Gets the current resume count for an anonymous user.
        /// </summary>
        Task<int> GetResumeCountAsync(Guid anonymousUserId);

        /// <summary>
        /// Checks if an anonymous user has reached the resume limit.
        /// </summary>
        Task<bool> HasReachedResumeLimitAsync(Guid anonymousUserId);

        /// <summary>
        /// Atomically acquires one resume creation slot when under limit.
        /// Must be executed inside an active transaction for concurrency safety.
        /// </summary>
        Task<bool> TryAcquireResumeSlotAsync(Guid anonymousUserId);

        /// <summary>
        /// Decrements the resume count for an anonymous user.
        /// </summary>
        Task DecrementResumeCountAsync(Guid anonymousUserId);

        /// <summary>
        /// Marks the anonymous user as converted to a registered user.
        /// </summary>
        Task ConvertToUserAsync(Guid anonymousUserId, string userId);
    }
}
