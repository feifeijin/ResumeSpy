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
            // Case 1: Resume exists (ResumeId is provided and not empty)
            if (!string.IsNullOrEmpty(model.ResumeId))
            {
                return await CreateResumeDetailForExistingResume(model);
            }

            // Case 2: Resume doesn't exist (ResumeId is null or empty)
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
                model.Id = "1"; // First detail for this resume

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
    }
}
