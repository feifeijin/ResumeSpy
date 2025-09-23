using ResumeSpy.Core.Entities.Business;
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

        public ResumeManagementService(
            IResumeService resumeService,
            IResumeDetailService resumeDetailService,
            IUnitOfWork unitOfWork)
        {
            _resumeService = resumeService;
            _resumeDetailService = resumeDetailService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResumeDetailViewModel> CreateResumeDetailAsync(ResumeDetailViewModel model)
        {
            // Case 1: Resume exists (ResumeId is provided and not empty and not "undefined")
            if (!string.IsNullOrEmpty(model.ResumeId) && model.ResumeId != "undefined")
            {
                return await CreateResumeDetailForExistingResume(model);
            }

            // Case 2: Resume doesn't exist (ResumeId is null, empty, or "undefined")
            // Create Resume first, then ResumeDetail in a transaction
            return await CreateResumeDetailWithNewResume(model);
        }

        private async Task<ResumeDetailViewModel> CreateResumeDetailForExistingResume(ResumeDetailViewModel model)
        {
            // Get existing details to determine the next ID
            var existingDetails = (await _resumeDetailService.GetResumeDetailsByResumeId(model.ResumeId)).ToList();
            model.Id = (existingDetails.Count + 1).ToString();

            var result = await _resumeDetailService.Create(model);
            await _unitOfWork.SaveChangesAsync(); // Save immediately for single operation
            return result;
        }

        private async Task<ResumeDetailViewModel> CreateResumeDetailWithNewResume(ResumeDetailViewModel model)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Create new Resume first
                var newResume = new ResumeViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = model.Name ?? "New Resume",
                    ResumeDetailCount = 1,
                    ResumeImgPath = "/assets/default_resume.png"
                };

                var createdResume = await _resumeService.Create(newResume);

                // Now create ResumeDetail with the new Resume ID
                model.ResumeId = createdResume.Id;
                model.Id = Guid.NewGuid().ToString();
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

        public async Task<ResumeViewModel> CloneResumeAsync(string resumeId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Get the original resume
                var originalResume = await _resumeService.GetResume(resumeId);
                
                // Create cloned resume
                var clonedResume = new ResumeViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = originalResume.Title + " (Copy)",
                    ResumeDetailCount = originalResume.ResumeDetailCount,
                    ResumeImgPath = originalResume.ResumeImgPath
                };

                // Create the cloned resume (this will call SaveChanges, but within our transaction)
                var createdResume = await _resumeService.Create(clonedResume);

                // Get all ResumeDetails from the original resume
                var originalResumeDetails = await _resumeDetailService.GetResumeDetailsByResumeId(resumeId);

                // Clone each ResumeDetail and associate with the new resume
                foreach (var originalDetail in originalResumeDetails)
                {
                    var clonedDetail = new ResumeDetailViewModel
                    {
                        Id = Guid.NewGuid().ToString(), // New unique ID for cloned detail
                        ResumeId = createdResume.Id,    // Associate with new resume
                        Name = originalDetail.Name,
                        Language = originalDetail.Language,
                        Content = originalDetail.Content
                    };

                    // Create the cloned detail (this will call SaveChanges, but within our transaction)
                    await _resumeDetailService.Create(clonedDetail);
                }

                // Commit the transaction - all individual SaveChanges calls are part of this transaction
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
                
                // Get all ResumeDetails for this Resume to update the old default
                var allResumeDetails = await _resumeDetailService.GetResumeDetailsByResumeId(newDefaultResumeDetail.ResumeId);
                
                // Set all others to non-default
                foreach (var detail in allResumeDetails.Where(rd => rd.Id != resumeDetailId))
                {
                    if (detail.IsDefault)
                    {
                        detail.IsDefault = false;
                        await _resumeDetailService.Update(detail);
                    }
                }

                // Set the new default
                newDefaultResumeDetail.IsDefault = true;
                await _resumeDetailService.Update(newDefaultResumeDetail);

                // Update the Resume's image path to match the new default ResumeDetail
                var resume = await _resumeService.GetResume(newDefaultResumeDetail.ResumeId);
                resume.ResumeImgPath = newDefaultResumeDetail.ResumeImgPath ?? "/assets/default_resume.png";
                await _resumeService.Update(resume);

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
