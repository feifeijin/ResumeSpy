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

        /// <summary>
        /// Gets the most recent active (non-converted, non-expired) guest session
        /// matching the IP address and optional user agent.
        /// </summary>
        /// <param name="ipAddress">Client IP address fingerprint.</param>
        /// <param name="userAgent">Optional user agent fingerprint. If null, match only by IP.</param>
        Task<GuestSession?> GetActiveSessionByFingerprintAsync(string ipAddress, string? userAgent);
    }
}
