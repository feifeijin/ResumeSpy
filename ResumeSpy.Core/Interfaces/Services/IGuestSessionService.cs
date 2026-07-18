using ResumeSpy.Core.Entities.General;
using System;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IGuestSessionService
    {
        /// <summary>
        /// Creates a new guest session
        /// </summary>
        Task<GuestSession> CreateGuestSessionAsync(string ipAddress, string? userAgent);

        /// <summary>
        /// Gets a guest session by ID
        /// </summary>
        Task<GuestSession?> GetGuestSessionAsync(Guid sessionId);

        /// <summary>
        /// Validates if a guest session is still active (not expired, not converted)
        /// IP is optional and only used for security logging, not validation
        /// </summary>
        Task<bool> ValidateGuestSessionAsync(Guid sessionId, string? currentIpAddress = null);

        /// <summary>
        /// Increments the resume count for a guest session
        /// </summary>
        Task IncrementResumeCountAsync(Guid sessionId);

        /// <summary>
        /// Decrements the resume count for a guest session
        /// </summary>
        Task DecrementResumeCountAsync(Guid sessionId);

        /// <summary>
        /// Gets the current resume count for a guest session
        /// </summary>
        Task<int> GetResumeCountAsync(Guid sessionId);

        /// <summary>
        /// Converts a guest session to a registered user
        /// </summary>
        Task ConvertGuestSessionAsync(Guid sessionId, string userId);

        /// <summary>
        /// Cleans up expired guest sessions
        /// </summary>
        Task CleanupExpiredSessionsAsync();

        /// <summary>
        /// Checks if a guest session has reached the resume limit
        /// </summary>
        Task<bool> HasReachedResumeLimitAsync(Guid sessionId);

        /// <summary>
        /// Checks if an IP address has exceeded the rate limit for session creation
        /// Returns true if limit exceeded (should reject), false if within limit
        /// </summary>
        Task<bool> HasExceededSessionRateLimitAsync(string ipAddress);

        /// <summary>
        /// Checks if an IP address has exceeded the rate limit for resume creation across all sessions
        /// Returns true if limit exceeded (should reject), false if within limit
        /// </summary>
        Task<bool> HasExceededResumeRateLimitAsync(string ipAddress);
    }
}
