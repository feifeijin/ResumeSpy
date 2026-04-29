using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Services
{
    public class ResumeManagementService : IResumeManagementService
    {
        private readonly IResumeService _resumeService;
        private readonly IResumeDetailService _resumeDetailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITranslationService _translationService;
        private readonly IAnonymousUserService _anonymousUserService;

        public ResumeManagementService(
            IResumeService resumeService,
            IResumeDetailService resumeDetailService,
            IUnitOfWork unitOfWork,
            ITranslationService translationService,
            IAnonymousUserService anonymousUserService)
        {
            _resumeService = resumeService;
            _resumeDetailService = resumeDetailService;
            _unitOfWork = unitOfWork;
            _translationService = translationService;
            _anonymousUserService = anonymousUserService;
        }

        public async Task<ResumeDetailViewModel> CreateResumeDetailAsync(ResumeDetailViewModel model, string? userId = null, Guid? anonymousUserId = null)
        {
            var isFirstTime = string.IsNullOrEmpty(model.ResumeId) || model.ResumeId == "undefined";

            // Validate anonymous user quota for first-time resume creation
            if (isFirstTime && string.IsNullOrEmpty(userId))
            {
                if (!anonymousUserId.HasValue)
                {
                    throw new UnauthorizedException("Anonymous user identity not found.");
                }

                var hasReachedLimit = await _anonymousUserService.HasReachedResumeLimitAsync(anonymousUserId.Value);
                if (hasReachedLimit)
                {
                    throw new QuotaExceededException("Resume limit reached. Please register to create more resumes.");
                }
            }

            // Auto-detect language if it's empty or null
            if (string.IsNullOrWhiteSpace(model.Language) && !string.IsNullOrWhiteSpace(model.Content))
            {
                try
                {
                    model.Language = await _translationService.DetectLanguageAsync(model.Content);
                }
                catch (Exception)
                {
                    model.Language = string.Empty;
                }
            }
            // Case 1: Resume exists (ResumeId is provided and not empty and not "undefined")
            if (!string.IsNullOrEmpty(model.ResumeId) && model.ResumeId != "undefined")
            {
                return await CreateResumeDetailForExistingResume(model);
            }

            // Case 2: Resume doesn't exist (ResumeId is null, empty, or "undefined")
            // Create Resume first, then ResumeDetail in a transaction
            return await CreateResumeDetailWithNewResume(model, userId, anonymousUserId);
        }

        private async Task<ResumeDetailViewModel> CreateResumeDetailForExistingResume(ResumeDetailViewModel model)
        {
            // Get existing details to determine the next ID
            var existingDetails = (await _resumeDetailService.GetResumeDetailsByResumeId(model.ResumeId)).ToList();
            model.Id = (existingDetails.Count + 1).ToString();

            // Thumbnail is generated asynchronously by ThumbnailBackgroundService
            // via the queue enqueued inside ResumeDetailService.Create — no blocking call here.
            var result = await _resumeDetailService.Create(model);
            return result;
        }

        private async Task<ResumeDetailViewModel> CreateResumeDetailWithNewResume(ResumeDetailViewModel model, string? userId = null, Guid? anonymousUserId = null)
        {
            var isAnonymous = anonymousUserId.HasValue && string.IsNullOrEmpty(userId);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var newResumeId = Guid.NewGuid().ToString();
                var newResumeDetailId = Guid.NewGuid().ToString();

                if (isAnonymous)
                {
                    var acquired = await _anonymousUserService.TryAcquireResumeSlotAsync(anonymousUserId!.Value);
                    if (!acquired)
                    {
                        throw new QuotaExceededException("Resume limit reached. Please register to create more resumes.");
                    }
                }

                // Create new Resume first with anonymous/user context.
                // ResumeImgPath starts with the default placeholder; ThumbnailBackgroundService
                // will update it once the thumbnail is ready.
                var newResume = new ResumeViewModel
                {
                    Id = newResumeId,
                    Title = model.Name ?? "New Resume",
                    ResumeDetailCount = 1,
                    ResumeImgPath = "/assets/default_resume.png",
                    UserId = userId,
                    AnonymousUserId = isAnonymous ? anonymousUserId : null,
                    IsGuest = isAnonymous,
                    ExpiresAt = isAnonymous ? DateTime.UtcNow.AddDays(30) : null
                };

                var createdResume = await _resumeService.Create(newResume);

                // Now create ResumeDetail with the new Resume ID.
                // ResumeDetailService.Create enqueues thumbnail generation in the background.
                model.ResumeId = createdResume.Id;
                model.Id = newResumeDetailId;
                var result = await _resumeDetailService.Create(model);

                // Save all changes within the transaction
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ResumeViewModel> CloneResumeAsync(string resumeId, string? userId = null, Guid? anonymousUserId = null)
        {
            var isAnonymous = anonymousUserId.HasValue && string.IsNullOrEmpty(userId);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (isAnonymous)
                {
                    var hasReachedLimit = await _anonymousUserService.HasReachedResumeLimitAsync(anonymousUserId!.Value);
                    if (hasReachedLimit)
                    {
                        throw new QuotaExceededException("Resume limit reached. Please register to create more resumes.");
                    }

                    var acquired = await _anonymousUserService.TryAcquireResumeSlotAsync(anonymousUserId!.Value);
                    if (!acquired)
                    {
                        throw new QuotaExceededException("Resume limit reached. Please register to create more resumes.");
                    }
                }

                // Get the original resume
                var originalResume = await _resumeService.GetResume(resumeId);

                // Create cloned resume — reuse the source image until background thumbnails are ready
                var clonedResume = new ResumeViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = originalResume.Title + " (Copy)",
                    ResumeDetailCount = originalResume.ResumeDetailCount,
                    ResumeImgPath = originalResume.ResumeImgPath,
                    UserId = userId,
                    AnonymousUserId = isAnonymous ? anonymousUserId : null,
                    IsGuest = isAnonymous,
                    ExpiresAt = isAnonymous ? DateTime.UtcNow.AddDays(30) : null
                };

                // Create the cloned resume
                var createdResume = await _resumeService.Create(clonedResume);

                // Get all ResumeDetails from the original resume
                var originalResumeDetails = await _resumeDetailService.GetResumeDetailsByResumeId(resumeId);

                // Clone each ResumeDetail and associate with the new resume.
                // ResumeDetailService.Create enqueues thumbnail generation per detail in the
                // background so the clone response is not blocked by rendering N thumbnails.
                foreach (var originalDetail in originalResumeDetails)
                {
                    var newDetailId = Guid.NewGuid().ToString();

                    var clonedDetail = new ResumeDetailViewModel
                    {
                        Id = newDetailId,
                        ResumeId = createdResume.Id,    // Associate with new resume
                        Name = originalDetail.Name,
                        Language = originalDetail.Language,
                        Content = originalDetail.Content,
                        ResumeImgPath = originalDetail.ResumeImgPath, // reuse until new one is rendered
                        IsDefault = originalDetail.IsDefault
                    };

                    // Create the cloned detail
                    await _resumeDetailService.Create(clonedDetail);
                }

                // Save all changes within the transaction
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return createdResume;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task SetDefaultResumeDetailAsync(string resumeDetailId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Get the ResumeDetail that will become the new default
                var newDefaultResumeDetail = await _resumeDetailService.GetResumeDetail(resumeDetailId);

                // Find the current default detail for this resume (if one exists)
                var allResumeDetails = (await _resumeDetailService.GetResumeDetailsByResumeId(newDefaultResumeDetail.ResumeId)).ToList();
                var currentDefaultDetail = allResumeDetails.FirstOrDefault(d => d.IsDefault && d.Id != resumeDetailId);

                // Unset the old default — flag-only change, no thumbnail regeneration needed
                if (currentDefaultDetail != null)
                {
                    currentDefaultDetail.IsDefault = false;
                    await _resumeDetailService.UpdateFlagsOnly(currentDefaultDetail);
                }

                // Set new default. ResumeDetailService.Update enqueues thumbnail regeneration;
                // ThumbnailBackgroundService will sync Resume.ResumeImgPath once done.
                newDefaultResumeDetail.IsDefault = true;
                await _resumeDetailService.Update(newDefaultResumeDetail);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task UpdateResumeDetailModelContentAsync(ResumeDetailViewModel model)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Update the detail. ResumeDetailService.Update persists content immediately
                // and enqueues thumbnail regeneration in the background.
                // ThumbnailBackgroundService handles syncing Resume.ResumeImgPath once done.
                await _resumeDetailService.Update(model);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<int> ConvertAnonymousToUserAsync(Guid anonymousUserId, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Step 1: Reassign all anonymous resumes to the user
                var resumeCount = await _resumeService.ReassignAnonymousResumesAsync(anonymousUserId, userId);

                // Step 2: Mark the anonymous user as converted
                await _anonymousUserService.ConvertToUserAsync(anonymousUserId, userId);

                await _unitOfWork.CommitTransactionAsync();
                return resumeCount;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Deletes a resume atomically, ensuring the anonymous user resume count is properly decremented.
        /// This maintains quota consistency by reducing the counter when an anonymous resume is deleted.
        /// </summary>
        public async Task DeleteResumeAtomicAsync(string resumeId, string? userId = null, Guid? anonymousUserId = null)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Fetch the resume to verify it exists and get its anonymous user
                var resume = await _resumeService.GetResume(resumeId);
                if (resume == null)
                {
                    throw new NotFoundException($"Resume with id {resumeId} not found.");
                }

                // Authorization check: user owns it or anonymous user matches
                bool isAuthorized =
                    (!string.IsNullOrEmpty(userId) && resume.UserId == userId) ||
                    (anonymousUserId.HasValue && resume.AnonymousUserId == anonymousUserId);

                if (!isAuthorized)
                {
                    throw new UnauthorizedException("Not authorized to delete this resume.");
                }

                // Delete the resume
                await _resumeService.Delete(resumeId);

                // Decrement anonymous user counter if this was an anonymous resume
                if (resume.IsGuest && resume.AnonymousUserId.HasValue)
                {
                    await _anonymousUserService.DecrementResumeCountAsync(resume.AnonymousUserId.Value);
                }

                // Commit the transaction
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
