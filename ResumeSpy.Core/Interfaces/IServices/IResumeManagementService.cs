using ResumeSpy.Core.Entities.Business;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeManagementService
    {
        /// <summary>
        /// Creates a ResumeDetail. If ResumeId is provided, creates only the detail.
        /// If ResumeId is null/empty, creates a new Resume first, then the detail.
        /// </summary>
        /// <param name="model">The ResumeDetail to create</param>
        /// <returns>The created ResumeDetail with proper ResumeId assigned</returns>
        Task<ResumeDetailViewModel> CreateResumeDetailAsync(ResumeDetailViewModel model);

        /// <summary>
        /// Clones a Resume and all its associated ResumeDetails in a single transaction.
        /// </summary>
        /// <param name="resumeId">The ID of the Resume to clone</param>
        /// <returns>The cloned Resume with new ID</returns>
        Task<ResumeViewModel> CloneResumeAsync(string resumeId);

        /// <summary>
        /// Sets a ResumeDetail as the default and updates the Resume's image path accordingly.
        /// </summary>
        /// <param name="resumeDetailId">The ID of the ResumeDetail to set as default</param>
        /// <returns>Task representing the operation</returns>
        Task SetDefaultResumeDetailAsync(string resumeDetailId);
    }
}
