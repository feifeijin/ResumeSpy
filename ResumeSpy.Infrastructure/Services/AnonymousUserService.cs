using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using System;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Services
{
    public class AnonymousUserService : IAnonymousUserService
    {
        private readonly IAnonymousUserRepository _anonymousUserRepository;
        private readonly IResumeRepository _resumeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AnonymousUserService> _logger;
        private readonly AnonymousUserSettings _settings;

        public AnonymousUserService(
            IAnonymousUserRepository anonymousUserRepository,
            IResumeRepository resumeRepository,
            IUnitOfWork unitOfWork,
            ILogger<AnonymousUserService> logger,
            IOptions<AnonymousUserSettings> settings)
        {
            _anonymousUserRepository = anonymousUserRepository;
            _resumeRepository = resumeRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<AnonymousUser> GetOrCreateAsync(Guid anonymousUserId)
        {
            var user = await _anonymousUserRepository.FindByIdAsync(anonymousUserId);
            if (user != null)
                return user;

            user = new AnonymousUser
            {
                Id = anonymousUserId,
                ResumeCount = 0,
                IsConverted = false,
                EntryDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            };

            try
            {
                await _anonymousUserRepository.Create(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Anonymous user created: {AnonymousUserId}", anonymousUserId);
                return user;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            {
                // Race condition: another concurrent request already inserted this user.
                // Detach the failed entity and fetch the existing one.
                _logger.LogDebug("Anonymous user {AnonymousUserId} already exists (concurrent insert), fetching existing record", anonymousUserId);
                _unitOfWork.DetachEntity(user);
                return await _anonymousUserRepository.FindByIdAsync(anonymousUserId)
                    ?? throw new InvalidOperationException($"Anonymous user {anonymousUserId} not found after concurrent insert");
            }
        }

        public async Task<AnonymousUser?> GetAsync(Guid anonymousUserId)
        {
            return await _anonymousUserRepository.FindByIdAsync(anonymousUserId);
        }

        public async Task<int> GetResumeCountAsync(Guid anonymousUserId)
        {
            var user = await _anonymousUserRepository.FindByIdAsync(anonymousUserId);
            return user?.ResumeCount ?? 0;
        }

        public async Task<bool> HasReachedResumeLimitAsync(Guid anonymousUserId)
        {
            try
            {
                var user = await _anonymousUserRepository.FindByIdAsync(anonymousUserId);
                if (user == null)
                    return false; // New user hasn't created anything yet

                if (user.IsConverted)
                {
                    _logger.LogWarning("Anonymous user {AnonymousUserId} already converted, cannot create new resumes", anonymousUserId);
                    return true;
                }

                // Reconcile against actual resume count to detect counter corruption
                var actualResumes = await _resumeRepository.GetByAnonymousUserIdAsync(anonymousUserId);
                var actualCount = actualResumes?.Count ?? 0;
                var storedCount = user.ResumeCount;

                if (storedCount != actualCount)
                {
                    _logger.LogWarning("Counter mismatch for anonymous user {AnonymousUserId}: stored={StoredCount}, actual={ActualCount}. Auto-correcting.",
                        anonymousUserId, storedCount, actualCount);
                    user.ResumeCount = actualCount;
                    user.UpdateDate = DateTime.UtcNow;
                    await _anonymousUserRepository.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                    storedCount = actualCount;
                }

                return storedCount >= _settings.MaxResumePerUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking resume limit for anonymous user {AnonymousUserId}", anonymousUserId);
                return true; // Fail secure
            }
        }

        public async Task<bool> TryAcquireResumeSlotAsync(Guid anonymousUserId)
        {
            try
            {
                // Row-level lock for concurrency safety — must run inside an active transaction.
                var user = await _anonymousUserRepository.GetByIdForUpdateAsync(anonymousUserId);

                if (user == null)
                {
                    // First time: create the record under lock via a separate path.
                    // The middleware should have already created the record, but handle edge case.
                    user = new AnonymousUser
                    {
                        Id = anonymousUserId,
                        ResumeCount = 0,
                        IsConverted = false,
                        EntryDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow
                    };
                    await _anonymousUserRepository.Create(user);
                    await _unitOfWork.SaveChangesAsync();

                    // Re-acquire with lock
                    user = await _anonymousUserRepository.GetByIdForUpdateAsync(anonymousUserId);
                    if (user == null)
                    {
                        _logger.LogError("Failed to create and lock anonymous user {AnonymousUserId}", anonymousUserId);
                        return false;
                    }
                }

                if (user.IsConverted)
                {
                    _logger.LogWarning("Anonymous user {AnonymousUserId} is converted, cannot acquire slot", anonymousUserId);
                    return false;
                }

                // Reconcile against actual resume count
                var actualCount = (await _resumeRepository.GetByAnonymousUserIdAsync(anonymousUserId)).Count;
                var effectiveCount = user.ResumeCount;

                if (user.ResumeCount != actualCount)
                {
                    effectiveCount = actualCount;
                    user.ResumeCount = actualCount;
                }

                if (effectiveCount >= _settings.MaxResumePerUser)
                    return false;

                user.ResumeCount = effectiveCount + 1;
                user.UpdateDate = DateTime.UtcNow;
                await _anonymousUserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring resume slot for anonymous user {AnonymousUserId}", anonymousUserId);
                throw;
            }
        }

        public async Task DecrementResumeCountAsync(Guid anonymousUserId)
        {
            try
            {
                var user = await _anonymousUserRepository.FindByIdAsync(anonymousUserId);

                if (user == null)
                {
                    _logger.LogError("Anonymous user {AnonymousUserId} not found for decrement operation", anonymousUserId);
                    return;
                }

                if (user.IsConverted)
                {
                    _logger.LogWarning("Attempted to decrement count for converted anonymous user {AnonymousUserId}. Skipping.", anonymousUserId);
                    return;
                }

                if (user.ResumeCount <= 0)
                {
                    _logger.LogWarning("Resume count already at 0 for anonymous user {AnonymousUserId}. Cannot decrement further.", anonymousUserId);
                    return;
                }

                user.ResumeCount--;
                user.UpdateDate = DateTime.UtcNow;

                await _anonymousUserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Anonymous user {AnonymousUserId} resume count decremented to {ResumeCount}", anonymousUserId, user.ResumeCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrementing resume count for anonymous user {AnonymousUserId}", anonymousUserId);
                throw;
            }
        }

        public async Task ConvertToUserAsync(Guid anonymousUserId, string userId)
        {
            try
            {
                var user = await _anonymousUserRepository.FindByIdAsync(anonymousUserId);
                if (user != null)
                {
                    user.IsConverted = true;
                    user.ConvertedUserId = userId;
                    user.UpdateDate = DateTime.UtcNow;
                    await _anonymousUserRepository.Update(user);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Anonymous user converted: {AnonymousUserId} to user: {UserId}", anonymousUserId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting anonymous user {AnonymousUserId}", anonymousUserId);
                throw;
            }
        }
    }
}
