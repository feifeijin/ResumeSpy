using Microsoft.Extensions.Logging;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Services
{
    public class GuestSessionService : IGuestSessionService
    {
        private readonly IGuestSessionRepository _guestSessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GuestSessionService> _logger;
        private const int MAX_RESUME_COUNT = 1;
        private const int SESSION_EXPIRY_DAYS = 30;

        public GuestSessionService(
            IGuestSessionRepository guestSessionRepository,
            IUnitOfWork unitOfWork,
            ILogger<GuestSessionService> logger)
        {
            _guestSessionRepository = guestSessionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GuestSession> CreateGuestSessionAsync(string ipAddress, string? userAgent)
        {
            try
            {
                var session = new GuestSession
                {
                    Id = Guid.NewGuid(),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    ResumeCount = 0,
                    ExpiresAt = DateTime.UtcNow.AddDays(SESSION_EXPIRY_DAYS),
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

        public async Task<bool> ValidateGuestSessionAsync(Guid sessionId, string ipAddress)
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

                // Basic IP validation (can be enhanced)
                if (session.IpAddress != ipAddress)
                {
                    _logger.LogWarning($"Guest session IP mismatch: {sessionId}. Expected: {session.IpAddress}, Got: {ipAddress}");
                    // Note: You might want to be more lenient here for corporate networks with dynamic IPs
                    // For now, we'll allow it but log the mismatch
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
            return count >= MAX_RESUME_COUNT;
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
