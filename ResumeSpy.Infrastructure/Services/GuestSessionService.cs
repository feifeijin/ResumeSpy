using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Services
{
    public class GuestSessionService : IGuestSessionService
    {
        private readonly IGuestSessionRepository _guestSessionRepository;
        private readonly IResumeRepository _resumeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GuestSessionService> _logger;
        private readonly GuestSessionSettings _settings;

        public GuestSessionService(
            IGuestSessionRepository guestSessionRepository,
            IResumeRepository resumeRepository,
            IUnitOfWork unitOfWork,
            ILogger<GuestSessionService> logger,
            IOptions<GuestSessionSettings> settings)
        {
            _guestSessionRepository = guestSessionRepository;
            _resumeRepository = resumeRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<GuestSession> CreateGuestSessionAsync(string ipAddress, string? userAgent)
        {
            try
            {
                // Always create a new session - each browser gets unique session via cookie
                // This prevents session collision when multiple users share same IP+UserAgent
                var session = new GuestSession
                {
                    Id = Guid.NewGuid(),
                    IpAddress = ipAddress,    // Stored for audit/security, not for reuse
                    UserAgent = userAgent,    // Stored for audit/security, not for reuse
                    ResumeCount = 0,
                    ExpiresAt = DateTime.UtcNow.AddDays(_settings.SessionExpiryDays),
                    IsConverted = false,
                    EntryDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow
                };

                await _guestSessionRepository.Create(session);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Guest session created: {session.Id} from IP: {ipAddress}");
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating guest session: {ex.Message}");
                throw;
            }
        }

        public async Task<GuestSession?> GetGuestSessionAsync(Guid sessionId)
        {
            return await _guestSessionRepository.GetById<Guid>(sessionId);
        }

        public async Task<bool> ValidateGuestSessionAsync(Guid sessionId, string? currentIpAddress = null)
        {
            try
            {
                var session = await GetGuestSessionAsync(sessionId);

                if (session == null)
                {
                    _logger.LogWarning($"Guest session not found: {sessionId}");
                    return false;
                }

                if (session.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Guest session expired: {sessionId}");
                    return false;
                }

                if (session.IsConverted)
                {
                    _logger.LogWarning($"Guest session already converted: {sessionId}");
                    return false;
                }

                // Log IP changes for security monitoring (but don't invalidate session)
                // This supports mobile users switching networks and VPN usage
                if (currentIpAddress != null && session.IpAddress != currentIpAddress)
                {
                    _logger.LogInformation($"Guest session {sessionId} IP changed from {session.IpAddress} to {currentIpAddress}. This is normal for mobile users.");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating guest session: {ex.Message}");
                return false;
            }
        }

        public async Task IncrementResumeCountAsync(Guid sessionId)
        {
            try
            {
                var session = await GetGuestSessionAsync(sessionId);
                if (session != null)
                {
                    session.ResumeCount++;
                    session.UpdateDate = DateTime.UtcNow;
                    await _guestSessionRepository.Update(session);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error incrementing resume count: {ex.Message}");
                throw;
            }
        }

        public async Task DecrementResumeCountAsync(Guid sessionId)
        {
            try
            {
                var session = await GetGuestSessionAsync(sessionId);
                if (session != null && session.ResumeCount > 0)
                {
                    session.ResumeCount--;
                    session.UpdateDate = DateTime.UtcNow;
                    await _guestSessionRepository.Update(session);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error decrementing resume count: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetResumeCountAsync(Guid sessionId)
        {
            try
            {
                var session = await GetGuestSessionAsync(sessionId);
                return session?.ResumeCount ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting resume count: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> HasReachedResumeLimitAsync(Guid sessionId)
        {
            var count = await GetResumeCountAsync(sessionId);
            return count >= _settings.MaxResumePerSession;
        }

        public async Task<bool> HasExceededSessionRateLimitAsync(string ipAddress)
        {
            if (!_settings.EnableRateLimiting)
            {
                return false;
            }

            try
            {
                var since = DateTime.UtcNow.AddDays(-1); // Last 24 hours
                var sessionCount = await _guestSessionRepository.GetSessionCountByIpSinceAsync(ipAddress, since);

                if (sessionCount >= _settings.MaxSessionsPerIpPerDay)
                {
                    _logger.LogWarning($"IP {ipAddress} has exceeded session rate limit: {sessionCount}/{_settings.MaxSessionsPerIpPerDay} sessions in 24h");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking session rate limit: {ex.Message}");
                // On error, allow the operation to proceed (fail open)
                return false;
            }
        }

        public async Task<bool> HasExceededResumeRateLimitAsync(string ipAddress)
        {
            if (!_settings.EnableRateLimiting)
            {
                return false;
            }

            try
            {
                var since = DateTime.UtcNow.AddDays(-1); // Last 24 hours
                var sessions = await _guestSessionRepository.GetSessionsByIpSinceAsync(ipAddress, since);
                var sessionIds = sessions.Select(s => s.Id).ToList();

                // Count total resumes created from this IP across all sessions
                var totalResumes = await _resumeRepository.CountGuestResumesBySessionsAsync(sessionIds);

                if (totalResumes >= _settings.MaxResumesPerIpPerDay)
                {
                    _logger.LogWarning($"IP {ipAddress} has exceeded resume rate limit: {totalResumes}/{_settings.MaxResumesPerIpPerDay} resumes in 24h across {sessions.Count()} sessions");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking resume rate limit: {ex.Message}");
                // On error, allow the operation to proceed (fail open)
                return false;
            }
        }

        public async Task ConvertGuestSessionAsync(Guid sessionId, string userId)
        {
            try
            {
                var session = await GetGuestSessionAsync(sessionId);
                if (session != null)
                {
                    session.IsConverted = true;
                    session.ConvertedUserId = userId;
                    session.UpdateDate = DateTime.UtcNow;
                    await _guestSessionRepository.Update(session);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation($"Guest session converted: {sessionId} to user: {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error converting guest session: {ex.Message}");
                throw;
            }
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            try
            {
                var expiredSessions = await _guestSessionRepository.GetExpiredSessionsAsync();
                
                if (expiredSessions.Any())
                {
                    foreach (var session in expiredSessions)
                    {
                        await _guestSessionRepository.Delete(session);
                    }
                    await _unitOfWork.SaveChangesAsync();
                    
                    _logger.LogInformation($"Cleaned up {expiredSessions.Count()} expired guest sessions");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up expired sessions: {ex.Message}");
                throw;
            }
        }
    }
}
