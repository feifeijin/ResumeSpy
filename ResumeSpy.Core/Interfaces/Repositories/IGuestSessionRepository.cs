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

        /// <summary>
        /// Gets the count of guest sessions created from a specific IP address within a time window
        /// Used for rate limiting to prevent abuse via incognito mode
        /// </summary>
        /// <param name="ipAddress">Client IP address</param>
        /// <param name="since">Start of time window (e.g., 24 hours ago)</param>
        Task<int> GetSessionCountByIpSinceAsync(string ipAddress, DateTime since);

        /// <summary>
        /// Gets all guest sessions created from a specific IP address within a time window
        /// </summary>
        /// <param name="ipAddress">Client IP address</param>
        /// <param name="since">Start of time window</param>
        Task<IEnumerable<GuestSession>> GetSessionsByIpSinceAsync(string ipAddress, DateTime since);
    }
}
